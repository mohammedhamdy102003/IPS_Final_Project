from nfstream import NFStreamer
import requests
import time

# إعدادات الاتصال والشبكة
# ⚠️ غيّر القيمة دي بالـ Public IP بتاع الـ EC2 instance بعد ما تعمله.
#    (localhost كان شغال بس وقت إن السيرفر شغال على نفس جهازك في التطوير المحلي)
API_URL = "http://100.48.192.105:30500/api/Traffic/ProcessTraffic"
INTERFACE = r"\Device\NPF_{B04BEC53-E7CB-4320-86E7-489017156448}"
DEVICE_ID = "Esraa-PC"

def main():
    print("[+] Windows Agent Started and Listening... ")

    streamer = NFStreamer(
        source=INTERFACE,
        statistical_analysis=True,
        active_timeout=60,
        idle_timeout=30
    )

    for flow in streamer:
        # بناء كائن الـ data الداخلي متضمناً كافة المقاييس بنفس الترتيب والمسميات
        flow_data = {
            "bidirectional_duration_ms": flow.bidirectional_duration_ms,
            "bidirectional_packets": flow.bidirectional_packets,
            "bidirectional_bytes": flow.bidirectional_bytes,
            
            "src2dst_duration_ms": flow.src2dst_duration_ms,
            "src2dst_packets": flow.src2dst_packets,
            "src2dst_bytes": flow.src2dst_bytes,
            
            "dst2src_duration_ms": flow.dst2src_duration_ms,
            "dst2src_packets": flow.dst2src_packets,
            "dst2src_bytes": flow.dst2src_bytes,
            
            # --- Packet Size Stats ---
            "bidirectional_min_ps": flow.bidirectional_min_ps,
            "bidirectional_mean_ps": flow.bidirectional_mean_ps,
            "bidirectional_stddev_ps": flow.bidirectional_stddev_ps,
            "bidirectional_max_ps": flow.bidirectional_max_ps,
            
            "src2dst_min_ps": flow.src2dst_min_ps,
            "src2dst_mean_ps": flow.src2dst_mean_ps,
            "src2dst_stddev_ps": flow.src2dst_stddev_ps,
            "src2dst_max_ps": flow.src2dst_max_ps,
            
            "dst2src_min_ps": flow.dst2src_min_ps,
            "dst2src_mean_ps": flow.dst2src_mean_ps,
            "dst2src_stddev_ps": flow.dst2src_stddev_ps,
            "dst2src_max_ps": flow.dst2src_max_ps,
            
            # --- Inter-arrival Time (piat) ---
            "bidirectional_min_piat_ms": flow.bidirectional_min_piat_ms,
            "bidirectional_mean_piat_ms": flow.bidirectional_mean_piat_ms,
            "bidirectional_stddev_piat_ms": flow.bidirectional_stddev_piat_ms,
            "bidirectional_max_piat_ms": flow.bidirectional_max_piat_ms,
            
            "src2dst_min_piat_ms": flow.src2dst_min_piat_ms,
            "src2dst_mean_piat_ms": flow.src2dst_mean_piat_ms,
            "src2dst_stddev_piat_ms": flow.src2dst_stddev_piat_ms,
            "src2dst_max_piat_ms": flow.src2dst_max_piat_ms,
            
            "dst2src_min_piat_ms": flow.dst2src_min_piat_ms,
            "dst2src_mean_piat_ms": flow.dst2src_mean_piat_ms,
            "dst2src_stddev_piat_ms": flow.dst2src_stddev_piat_ms,
            "dst2src_max_piat_ms": flow.dst2src_max_piat_ms,
            
            # --- TCP Flags (Bidirectional) ---
            "bidirectional_syn_packets": flow.bidirectional_syn_packets,
            "bidirectional_cwr_packets": flow.bidirectional_cwr_packets,
            "bidirectional_ece_packets": flow.bidirectional_ece_packets,
            "bidirectional_ack_packets": flow.bidirectional_ack_packets,
            "bidirectional_psh_packets": flow.bidirectional_psh_packets,
            "bidirectional_rst_packets": flow.bidirectional_rst_packets,
            "bidirectional_fin_packets": flow.bidirectional_fin_packets,
            
            # --- TCP Flags (Source to Destination) ---
            "src2dst_syn_packets": flow.src2dst_syn_packets,
            "src2dst_cwr_packets": flow.src2dst_cwr_packets,
            "src2dst_ece_packets": flow.src2dst_ece_packets,
            "src2dst_ack_packets": flow.src2dst_ack_packets,
            "src2dst_psh_packets": flow.src2dst_psh_packets,
            "src2dst_rst_packets": flow.src2dst_rst_packets,
            "src2dst_fin_packets": flow.src2dst_fin_packets,
            
            # --- TCP Flags (Destination to Source) ---
            "dst2src_syn_packets": flow.dst2src_syn_packets,
            "dst2src_cwr_packets": flow.dst2src_cwr_packets,
            "dst2src_ece_packets": flow.dst2src_ece_packets,
            "dst2src_ack_packets": flow.dst2src_ack_packets,
            "dst2src_psh_packets": flow.dst2src_psh_packets,
            "dst2src_rst_packets": flow.dst2src_rst_packets,
            "dst2src_fin_packets": flow.dst2src_fin_packets,
            
            # بروتوكول مكرر بالداخل كما طلبت في نموذجك
            "protocol": int(flow.protocol)
        }

        # الهيكل الرئيسي الخارجي (Top-level Payload)
        payload = {
            "source_ip": flow.src_ip,
            "destination_ip": flow.dst_ip,
            "protocol": int(flow.protocol),
            "data": flow_data
        }

        try:
            # إرسال الطلب بصيغة JSON
            r = requests.post(API_URL, json=payload, timeout=3)
            if r.status_code == 200:
                print(f"[+] [{flow.src_ip} -> {flow.dst_ip}] Sent successfully! Response: {r.json()}")
            else:
                print(f"[-] [{flow.src_ip} -> {flow.dst_ip}] Failed with Status {r.status_code}: {r.text}")
        except requests.exceptions.RequestException as e:
            print(f"[-] Connection failed to C# Backend: {e}")
            time.sleep(1)

if __name__ == "__main__":
    main()