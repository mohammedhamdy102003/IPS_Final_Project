from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from typing import List, Dict, Any
import pandas as pd
import numpy as np
import tensorflow as tf
import joblib
import re
import unicodedata
import urllib.parse
from contextlib import asynccontextmanager

MODEL = None
ARTIFACTS = None

def parse_http_payload(raw_payload: str) -> str:
    """Strips normal HTTP wrappers to isolate the malicious URI/Body."""
    if pd.isna(raw_payload) or not isinstance(raw_payload, str) or not raw_payload.strip():
        return ""
        
    normalized_payload = raw_payload.replace('\r\n', '\n')
    lines = normalized_payload.strip().split('\n')
    
    first_line = lines[0]
    first_word = first_line.split(' ')[0].upper()
    http_methods = ['GET', 'POST', 'PUT', 'DELETE', 'HEAD', 'OPTIONS', 'PATCH']
    
    if first_word not in http_methods:
        return urllib.parse.unquote(normalized_payload.strip())
    
    extracted = []
    if len(first_line.split(' ')) >= 2:
        uri = first_line.split(' ')[1]
        extracted.append(urllib.parse.unquote(uri))
            
    if "\n\n" in normalized_payload:
        body = normalized_payload.split("\n\n", 1)[1]
        extracted.append(urllib.parse.unquote(body.strip()))
            
    return " ".join(extracted)

def secure_text_normalization(text_array: np.ndarray) -> np.ndarray:
    """Standardizes payload text to isolate malicious punctuation/commands."""
    cleaned_array = []
    for text in text_array:
        stripped_text = parse_http_payload(text)
        normalized = unicodedata.normalize('NFKC', str(stripped_text))
        ascii_str = normalized.encode('ascii', errors='replace').decode('ascii').lower()
        spaced_str = re.sub(r'([^\w\s])', r' \1 ', ascii_str)
        final_str = re.sub(r'\s+', ' ', spaced_str).strip()
        cleaned_array.append(final_str)
        
    return np.array(cleaned_array)

def preprocess_multimodal_pipeline(df: pd.DataFrame) -> pd.DataFrame:
    """Applies all evidence-driven feature engineering from the training pipeline."""
    df = df.copy()
    eps = 1e-5

    TLS_PORTS = {443, 8443, 4433}
    if 'dst_port' in df.columns:
        df['dst_port'] = pd.to_numeric(df['dst_port'], errors='coerce').fillna(-1)
        df['dst_port_is_tls'] = df['dst_port'].isin(TLS_PORTS).astype(int)
        df['dst_port_is_ephemeral'] = (df['dst_port'] > 1023).astype(int)
        df['dst_port_is_wellknown'] = ((df['dst_port'] > 0) & (df['dst_port'] <= 1023)).astype(int)

    leaky_and_l7_cols = [
        'id', 'expiration_id', 'src_ip', 'dst_ip', 'src_mac', 'dst_mac',
        'tunnel_id', 'vlan_id', 'src_oui', 'dst_oui', 'src_port', 'dst_port',
        'bidirectional_first_seen_ms', 'bidirectional_last_seen_ms',
        'src2dst_first_seen_ms', 'src2dst_last_seen_ms',
        'dst2src_first_seen_ms', 'dst2src_last_seen_ms', 'timestamp',
        'application_name', 'application_category_name', 'application_is_guessed',
        'application_confidence', 'requested_server_name', 'client_fingerprint',
        'server_fingerprint', 'user_agent', 'content_type',
        'splt_direction', 'splt_ps', 'splt_piat_ms'
    ]
    df = df.drop(columns=[c for c in leaky_and_l7_cols if c in df.columns])

    if 'src2dst_bytes' in df.columns and 'bidirectional_bytes' in df.columns:
        df['c2s_byte_ratio'] = df['src2dst_bytes'] / (df['bidirectional_bytes'] + eps)
    if 'bidirectional_syn_packets' in df.columns:
        df['syn_packet_ratio'] = df['bidirectional_syn_packets'] / (df['bidirectional_packets'] + eps)
    if 'bidirectional_rst_packets' in df.columns:
        df['rst_packet_ratio'] = df['bidirectional_rst_packets'] / (df['bidirectional_packets'] + eps)
    if 'bidirectional_fin_packets' in df.columns:
        df['fin_packet_ratio'] = df['bidirectional_fin_packets'] / (df['bidirectional_packets'] + eps)
    if 'bidirectional_ack_packets' in df.columns:
        df['ack_packet_ratio'] = df['bidirectional_ack_packets'] / (df['bidirectional_packets'] + eps)
    if 'bidirectional_psh_packets' in df.columns:
        df['psh_packet_ratio'] = df['bidirectional_psh_packets'] / (df['bidirectional_packets'] + eps)
    if 'bidirectional_duration_ms' in df.columns:
        df['packets_per_second'] = df['bidirectional_packets'] / (df['bidirectional_duration_ms'] / 1000 + 1e-3)
        df['bytes_per_second']   = df['bidirectional_bytes']   / (df['bidirectional_duration_ms'] / 1000 + 1e-3)
    if 'bidirectional_stddev_ps' in df.columns and 'bidirectional_mean_ps' in df.columns:
        df['packet_size_cv'] = df['bidirectional_stddev_ps'] / (df['bidirectional_mean_ps'] + eps)
    if 'protocol' in df.columns:
        df['is_nontcp'] = (df['protocol'] != 6).astype(int)

    if 'dst2src_bytes' in df.columns and 'bidirectional_bytes' in df.columns:
        df['server_byte_dominance'] = df['dst2src_bytes'] / (df['bidirectional_bytes'] + eps)
    if 'dst2src_bytes' in df.columns and 'src2dst_bytes' in df.columns:
        df['log_s2c_byte_ratio'] = np.log1p(df['dst2src_bytes'] / (df['src2dst_bytes'] + eps))
        
    if 'dst2src_max_ps' in df.columns:
        df['log_dst2src_max_ps']  = np.log1p(df['dst2src_max_ps'])
    if 'dst2src_mean_ps' in df.columns:
        df['log_dst2src_mean_ps'] = np.log1p(df['dst2src_mean_ps'])
        
    if 'bidirectional_max_ps' in df.columns:
        df['log_bidirectional_max_ps'] = np.log1p(df['bidirectional_max_ps'])

    if 'bidirectional_duration_ms' in df.columns:
        df['is_zero_duration'] = (df['bidirectional_duration_ms'] == 0).astype(int)
        if 'packets_per_second' in df.columns:
            df['log_pps'] = np.log1p(df['packets_per_second'])
        if 'bytes_per_second' in df.columns:
            df['log_bps'] = np.log1p(df['bytes_per_second'])
    if 'bidirectional_bytes' in df.columns:
        df['log_bytes']   = np.log1p(df['bidirectional_bytes'])
    if 'bidirectional_packets' in df.columns:
        df['log_packets'] = np.log1p(df['bidirectional_packets'])

    if 'protocol' in df.columns:
        df['is_tcp']  = (df['protocol'] == 6).astype(int)
        df['is_udp']  = (df['protocol'] == 17).astype(int)
        df['is_icmp'] = (df['protocol'] == 1).astype(int)
    if 'dst2src_max_ps' in df.columns and 'src2dst_max_ps' in df.columns:
        df['log_src2dst_max_ps'] = np.log1p(df['src2dst_max_ps'])
        df['server_response_ps_ratio'] = df['dst2src_max_ps'] / (df['src2dst_max_ps'] + eps)
    if 'dst2src_rst_packets' in df.columns and 'bidirectional_packets' in df.columns:
        df['dst2src_rst_ratio'] = df['dst2src_rst_packets'] / (df['bidirectional_packets'] + eps)
    if 'src2dst_psh_packets' in df.columns and 'bidirectional_packets' in df.columns:
        df['src2dst_psh_ratio'] = df['src2dst_psh_packets'] / (df['bidirectional_packets'] + eps)
    if 'dst2src_ack_packets' in df.columns and 'bidirectional_packets' in df.columns:
        df['dst2src_ack_ratio'] = df['dst2src_ack_packets'] / (df['bidirectional_packets'] + eps)

    if 'script_payload' not in df.columns:
        df['script_payload'] = ""
    df['script_payload'] = df['script_payload'].fillna("")

    non_text = [c for c in df.columns if c != 'script_payload']
    df[non_text] = df[non_text].apply(pd.to_numeric, errors='coerce').fillna(-1)

    return df

@asynccontextmanager
async def lifespan(app: FastAPI):
    global MODEL, ARTIFACTS
    
    print("Loading inference artifacts...")
    try:
        ARTIFACTS = joblib.load('multimodal_inference_artifacts_v4.pkl')
    except Exception as e:
        raise RuntimeError(f"Failed to load artifacts: {e}")
        
    print("Loading Keras model...")
    try:
        MODEL = tf.keras.models.load_model('multimodal_ids_2head_model_v4.keras')
    except Exception as e:
        raise RuntimeError(f"Failed to load model: {e}")
        
    print("API is ready to accept traffic.")
    yield
    print("Shutting down API...")
    MODEL = None
    ARTIFACTS = None

app = FastAPI(title="Multimodal IDS API", lifespan=lifespan)

class PredictionRequest(BaseModel):
    flows: List[Dict[str, Any]] = Field(..., description="List of raw network flow logs and script payloads.")

class PredictionResult(BaseModel):
    predicted_class: str
    class_confidence: float
    anomaly_probability: float
    is_anomaly: bool

class PredictionResponse(BaseModel):
    results: List[PredictionResult]

@app.post("/predict", response_model=PredictionResponse)
async def predict(request: PredictionRequest):
    if not request.flows:
        raise HTTPException(status_code=400, detail="Empty request payload.")
    
    df_raw = pd.DataFrame(request.flows)
    
    scaler = ARTIFACTS['scaler']
    le_target = ARTIFACTS['le_target']
    le_protocol = ARTIFACTS['le_protocol']
    le_ip_version = ARTIFACTS['le_ip_version']
    expected_columns = ARTIFACTS['expected_columns']
    
    try:
        df_processed = preprocess_multimodal_pipeline(df_raw)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Preprocessing error: {str(e)}")

    for col, le in [('protocol', le_protocol), ('ip_version', le_ip_version)]:
        if le and col in df_processed.columns:
            known_classes = set(le.classes_)
            df_processed[col] = df_processed[col].astype(str).apply(
                lambda x: x if x in known_classes else str(le.classes_[0])
            )
            df_processed[col] = le.transform(df_processed[col])
            
    for col in expected_columns:
        if col not in df_processed.columns:
            df_processed[col] = -1.0 
            
    X_num = df_processed[expected_columns].copy()
    
    try:
        X_scaled = scaler.transform(X_num).astype(np.float32)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Scaling error: {str(e)}")
        
    if 'script_payload' not in df_processed.columns:
        df_processed['script_payload'] = ""
        
    X_text_raw = df_processed['script_payload'].values
    X_text_clean = secure_text_normalization(X_text_raw)
    X_text_tf = tf.convert_to_tensor(X_text_clean, dtype=tf.string)
    
    try:
        outputs = MODEL.predict(
            {"network_flow_input": X_scaled, "script_text_input": X_text_tf},
            verbose=0
        )
        
        if isinstance(outputs, dict):
            class_logits = outputs["class_output"]
            anomaly_probs = outputs["anomaly_output"]
        else:
            class_logits = outputs[0]
            anomaly_probs = outputs[1]
            
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Model prediction error: {repr(e)}")
        
    class_probs = tf.nn.softmax(class_logits, axis=-1).numpy()
    predicted_class_indices = np.argmax(class_probs, axis=1)
    
    predicted_labels = le_target.inverse_transform(predicted_class_indices)
    
    results = []
    for i in range(len(request.flows)):
        conf = float(class_probs[i, predicted_class_indices[i]])
        anomaly_p = float(anomaly_probs[i][0])
        
        results.append(PredictionResult(
            predicted_class=predicted_labels[i],
            class_confidence=conf,
            anomaly_probability=anomaly_p,
            is_anomaly=(anomaly_p > 0.5)
        ))
        
    return PredictionResponse(results=results)