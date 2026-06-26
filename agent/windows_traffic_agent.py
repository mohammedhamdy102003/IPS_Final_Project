"""
Windows IDS Agent
------------------
Captures live network flows with NFStreamer and forwards them to the
multimodal IDS prediction API.

Improvements over the original version:
  * Configurable via environment variables / CLI args instead of hardcoded constants.
  * Capture and HTTP I/O are decoupled with a background sender thread + queue,
    so a slow/unreachable API no longer blocks flow capture.
  * Flows are sent in batches (the API already accepts a list payload),
    which drastically cuts down the number of HTTP requests.
  * A requests.Session with retry/backoff handles transient network errors.
  * Bounded queue with drop-oldest-on-full behavior prevents unbounded
    memory growth if the API is down for a while.
  * Graceful shutdown on Ctrl+C / SIGTERM (drains the queue before exiting).
  * Proper logging (rotating-friendly) instead of bare print().
"""

import argparse
import logging
import os
import queue
import signal
import sys
import threading
import time

import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

try:
    from nfstream import NFStreamer
except ImportError:
    NFStreamer = None  # handled at runtime so --help / config still work


# ======================= CONFIG (env-var overridable) =======================

def _env(name: str, default: str) -> str:
    return os.environ.get(name, default)


DEFAULT_API_URL = _env("IDS_API_URL", "http://192.168.157.151:8000/predict")
DEFAULT_INTERFACE = _env(
    "IDS_INTERFACE", r"\Device\NPF_{7389ADBB-9C23-447C-A4DF-3C7EAF6709B0}"
)
DEFAULT_DEVICE_ID = _env("IDS_DEVICE_ID", "DESKTOP-GSHOI10")
DEFAULT_BATCH_SIZE = int(_env("IDS_BATCH_SIZE", "20"))
DEFAULT_BATCH_INTERVAL = float(_env("IDS_BATCH_INTERVAL", "2.0"))  # seconds
DEFAULT_REQUEST_TIMEOUT = float(_env("IDS_REQUEST_TIMEOUT", "5"))
DEFAULT_QUEUE_MAX_SIZE = int(_env("IDS_QUEUE_MAX_SIZE", "5000"))
DEFAULT_LOG_LEVEL = _env("IDS_LOG_LEVEL", "INFO")

# Flow attributes pulled from each NFStreamer flow object (order matters for readability,
# not for correctness). device_id is injected separately since it isn't a flow attribute.
FLOW_FIELDS = [
    # --- Metadata ---
    "src_ip", "dst_ip", "src_port", "dst_port", "protocol", "ip_version",
    # --- Durations ---
    "bidirectional_duration_ms", "src2dst_duration_ms", "dst2src_duration_ms",
    # --- Packets & Bytes ---
    "bidirectional_packets", "bidirectional_bytes",
    "src2dst_packets", "src2dst_bytes",
    "dst2src_packets", "dst2src_bytes",
    # --- Packet Size Stats ---
    "bidirectional_min_ps", "bidirectional_mean_ps", "bidirectional_stddev_ps", "bidirectional_max_ps",
    "src2dst_min_ps", "src2dst_mean_ps", "src2dst_stddev_ps", "src2dst_max_ps",
    "dst2src_min_ps", "dst2src_mean_ps", "dst2src_stddev_ps", "dst2src_max_ps",
    # --- Inter-arrival Time ---
    "bidirectional_min_piat_ms", "bidirectional_mean_piat_ms",
    "bidirectional_stddev_piat_ms", "bidirectional_max_piat_ms",
    "src2dst_min_piat_ms", "src2dst_mean_piat_ms",
    "src2dst_stddev_piat_ms", "src2dst_max_piat_ms",
    "dst2src_min_piat_ms", "dst2src_mean_piat_ms",
    "dst2src_stddev_piat_ms", "dst2src_max_piat_ms",
    # --- TCP Flags ---
    "bidirectional_syn_packets", "bidirectional_ack_packets", "bidirectional_rst_packets",
    "bidirectional_fin_packets", "bidirectional_cwr_packets", "bidirectional_ece_packets",
    "bidirectional_psh_packets", "bidirectional_urg_packets",
    "src2dst_syn_packets", "src2dst_ack_packets", "src2dst_rst_packets",
    "src2dst_fin_packets", "src2dst_cwr_packets", "src2dst_ece_packets",
    "src2dst_psh_packets", "src2dst_urg_packets",
    "dst2src_syn_packets", "dst2src_ack_packets", "dst2src_rst_packets",
    "dst2src_fin_packets", "dst2src_cwr_packets", "dst2src_ece_packets",
    "dst2src_psh_packets", "dst2src_urg_packets",
]

logger = logging.getLogger("ids_agent")


# ============================== Helpers ====================================

def setup_logging(level: str) -> None:
    logging.basicConfig(
        level=getattr(logging, level.upper(), logging.INFO),
        format="%(asctime)s [%(levelname)s] %(message)s",
        datefmt="%H:%M:%S",
    )


def build_session(timeout: float) -> requests.Session:
    session = requests.Session()
    retries = Retry(
        total=3,
        backoff_factor=0.5,
        status_forcelist=[500, 502, 503, 504],
        allowed_methods=["POST"],
    )
    adapter = HTTPAdapter(max_retries=retries, pool_connections=10, pool_maxsize=10)
    session.mount("http://", adapter)
    session.mount("https://", adapter)
    session.headers.update({"Content-Type": "application/json"})
    return session


def build_flow_dict(flow, device_id: str) -> dict:
    data = {}
    for field in FLOW_FIELDS:
        data[field] = getattr(flow, field, None)
    data["device_id"] = device_id
    return data


# ============================ Sender thread =================================

class FlowSender:
    """Background worker: batches queued flows and POSTs them to the API."""

    def __init__(self, api_url: str, session: requests.Session,
                 batch_size: int, batch_interval: float, timeout: float):
        self.api_url = api_url
        self.session = session
        self.batch_size = batch_size
        self.batch_interval = batch_interval
        self.timeout = timeout
        self.queue: "queue.Queue[dict]" = queue.Queue(maxsize=DEFAULT_QUEUE_MAX_SIZE)
        self.stop_event = threading.Event()
        self.thread = threading.Thread(target=self._run, daemon=True)

    def start(self):
        self.thread.start()

    def submit(self, flow_data: dict):
        try:
            self.queue.put_nowait(flow_data)
        except queue.Full:
            # Drop the oldest item to make room rather than blocking capture
            try:
                self.queue.get_nowait()
            except queue.Empty:
                pass
            try:
                self.queue.put_nowait(flow_data)
            except queue.Full:
                logger.warning("Queue still full, dropping flow")

    def stop_and_join(self, join_timeout: float = 10.0):
        self.stop_event.set()
        self.thread.join(timeout=join_timeout)

    def _run(self):
        batch = []
        last_send = time.monotonic()
        while not self.stop_event.is_set() or not self.queue.empty() or batch:
            try:
                item = self.queue.get(timeout=0.5)
                batch.append(item)
            except queue.Empty:
                pass

            now = time.monotonic()
            should_flush = batch and (
                len(batch) >= self.batch_size
                or (now - last_send) >= self.batch_interval
                or (self.stop_event.is_set() and self.queue.empty())
            )
            if should_flush:
                self._send_batch(batch)
                batch = []
                last_send = now

    def _send_batch(self, batch: list):
        try:
            # =================================================================
            # 🔥 التعديل المطلوب: تغليف الـ batch كقيمة لمفتاح "flows"
            # =================================================================
            payload = {
                "flows": batch
            }
            resp = self.session.post(self.api_url, json=payload, timeout=self.timeout)
            resp.raise_for_status()
            
            # استخراج النتائج من القاموس الذي أرسله الـ API لتجنب أي أخطاء برمجية
            results = resp.json().get("results", [])
            
            for flow_data, result in zip(batch, results):
                tag = "ANOMALY" if result.get("is_anomaly") else "benign"
                logger.info(
                    "[%s -> %s] %s | class=%s conf=%s%% anomaly=%s%%",
                    flow_data.get("src_ip"), flow_data.get("dst_ip"), tag,
                    result.get("predicted_class"),
                    result.get("class_confidence"),
                    result.get("anomaly_probability"), # تم تغيير الاسم هنا ليطابق ما يرسله الخادم أيضاً
                )
        except requests.exceptions.RequestException as exc:
            logger.warning("Failed to send batch of %d flows: %s", len(batch), exc)
        except (ValueError, KeyError) as exc:
            logger.warning("Malformed response for batch of %d flows: %s", len(batch), exc)


# ================================ Main ======================================

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Windows IDS network agent")
    parser.add_argument("--api-url", default=DEFAULT_API_URL)
    parser.add_argument("--interface", default=DEFAULT_INTERFACE)
    parser.add_argument("--device-id", default=DEFAULT_DEVICE_ID)
    parser.add_argument("--batch-size", type=int, default=DEFAULT_BATCH_SIZE)
    parser.add_argument("--batch-interval", type=float, default=DEFAULT_BATCH_INTERVAL)
    parser.add_argument("--request-timeout", type=float, default=DEFAULT_REQUEST_TIMEOUT)
    parser.add_argument("--log-level", default=DEFAULT_LOG_LEVEL)
    parser.add_argument("--active-timeout", type=int, default=60)
    parser.add_argument("--idle-timeout", type=int, default=30)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    setup_logging(args.log_level)

    if NFStreamer is None:
        logger.error("nfstream is not installed. Run: pip install nfstream")
        sys.exit(1)

    session = build_session(args.request_timeout)
    sender = FlowSender(
        api_url=args.api_url,
        session=session,
        batch_size=args.batch_size,
        batch_interval=args.batch_interval,
        timeout=args.request_timeout,
    )
    sender.start()

    def handle_shutdown(signum, frame):
        logger.info("Shutdown signal received, draining remaining flows...")
        sender.stop_and_join()
        sys.exit(0)

    signal.signal(signal.SIGINT, handle_shutdown)
    if hasattr(signal, "SIGTERM"):
        signal.signal(signal.SIGTERM, handle_shutdown)

    logger.info("Windows Agent started, listening on interface: %s", args.interface)
    logger.info("Forwarding predictions to: %s", args.api_url)

    try:
        streamer = NFStreamer(
            source=args.interface,
            statistical_analysis=True,
            active_timeout=args.active_timeout,
            idle_timeout=args.idle_timeout,
        )
    except Exception as exc:
        logger.error("Failed to start NFStreamer on '%s': %s", args.interface, exc)
        sys.exit(1)

    try:
        for flow in streamer:
            flow_data = build_flow_dict(flow, args.device_id)
            sender.submit(flow_data)
    except KeyboardInterrupt:
        logger.info("Interrupted by user")
    except Exception as exc:
        logger.exception("Unexpected error while capturing flows: %s", exc)
    finally:
        sender.stop_and_join()
        logger.info("Agent stopped.")


if __name__ == "__main__":
    main()
