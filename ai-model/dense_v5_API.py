from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Dict, Any
import pandas as pd
import numpy as np
import tensorflow as tf
import joblib
from tensorflow.keras.models import load_model

app = FastAPI(
    title="AI-based IDS API",
    description="Intrusion Detection System Inference API",
    version="1.0"
)

artifacts = {}

@app.on_event("startup")
async def load_artifacts():
    """Loads the model and preprocessing artifacts at startup."""
    try:
        artifacts['model'] = load_model("dense_v5.keras")
        artifacts['scaler'] = joblib.load("scaler_dense_v5.joblib")
        artifacts['le'] = joblib.load("encoder_dense_v5.joblib")
        artifacts['log_cols'] = joblib.load("log_cols_dense_v5.joblib")
        
        feature_cols = joblib.load("features_dense_v5.joblib")
        artifacts['feature_cols'] = [col for col in feature_cols if col != 'label']
        
        print("Model and artifacts loaded successfully.")
    except Exception as e:
        print(f"Error loading artifacts: {e}")
        raise RuntimeError("Failed to load model artifacts. Check file paths.")

class InferenceRequest(BaseModel):
    data: List[Dict[str, Any]]

def transform_ids_features(X_raw: pd.DataFrame, log_cols: list) -> pd.DataFrame:
    """Applies feature engineering with fault tolerance for missing raw columns."""
    X = X_raw.copy()

    def get_col(col_name):
        if col_name in X.columns:
            return X[col_name].astype(float)
        return pd.Series([0.0] * len(X), index=X.index)

    # Calculate packet densities safely
    flag_cols = ['syn', 'cwr', 'ece', 'ack', 'psh', 'rst', 'fin']
    for flag in flag_cols:
        X[f'bi_{flag}_density']  = get_col(f'bidirectional_{flag}_packets') / (get_col('bidirectional_packets') + 1e-6)
        X[f's2d_{flag}_density'] = get_col(f'src2dst_{flag}_packets')       / (get_col('src2dst_packets')       + 1e-6)
        X[f'd2s_{flag}_density'] = get_col(f'dst2src_{flag}_packets')       / (get_col('dst2src_packets')       + 1e-6)

    # Intensity and Ratio Features
    X['byte_ratio_s2d_d2s']    = get_col('src2dst_bytes')   / (get_col('dst2src_bytes')    + 1e-6)
    X['packet_ratio_s2d_d2s']  = get_col('src2dst_packets') / (get_col('dst2src_packets')  + 1e-6)
    X['s2d_payload_intensity'] = get_col('src2dst_bytes')   / (get_col('src2dst_packets')  + 1e-6)
    X['d2s_payload_intensity'] = get_col('dst2src_bytes')   / (get_col('dst2src_packets')  + 1e-6)
    X['bi_avg_ps']             = get_col('bidirectional_bytes') / (get_col('bidirectional_packets') + 1e-6)
    X['bi_piat_jitter']        = get_col('bidirectional_stddev_piat_ms') / (get_col('bidirectional_mean_piat_ms') + 1e-6)
    X['bi_ps_jitter']          = get_col('bidirectional_stddev_ps')      / (get_col('bidirectional_mean_ps')      + 1e-6)

    # Log1p transformation for volumetric columns
    vol_cols = ['bidirectional_bytes', 'bidirectional_duration_ms', 'src2dst_bytes', 'dst2src_bytes']
    for col in vol_cols:
        X[col] = np.log1p(get_col(col))

    # Packets per second
    X['bi_pps'] = np.log1p(
        get_col('bidirectional_packets') / (get_col('bidirectional_duration_ms') / 1000.0 + 1e-6)
    )
    X['is_unidirectional'] = (get_col('dst2src_packets') < 1).astype(float)

    # Apply the SAME log_cols learned from the training data
    for col in log_cols:
        if col in X.columns and X[col].min() >= 0:
            X[col] = np.log1p(X[col].astype(float))

    # Drop low information columns safely
    low_info_cols = [col for col in X.columns if 'cwr' in col or 'ece' in col or 'fin' in col]
    X = X.drop(columns=low_info_cols, errors='ignore')

    # Drop uninformative/leaky packet columns
    cols_to_drop = [col for col in X.columns if '_packets' in col and 'density' not in col]
    X = X.drop(columns=cols_to_drop, errors='ignore')
    
    # Drop features bad for generalization
    drop_for_generalization = [
        'protocol', 'is_unidirectional', 'bidirectional_min_ps',
        'src2dst_min_ps', 'dst2src_min_ps',
        'bidirectional_min_piat_ms', 'src2dst_min_piat_ms', 'dst2src_min_piat_ms',
        'bidirectional_max_piat_ms', 'src2dst_max_piat_ms', 'dst2src_max_piat_ms'
    ]
    X = X.drop(columns=drop_for_generalization, errors='ignore')

    return X

@app.get("/health")
async def health_check():
    """يستخدمه Kubernetes (liveness/readiness probes) للتأكد إن الموديل اتحمل فعلاً."""
    if not artifacts:
        raise HTTPException(status_code=503, detail="Model not loaded yet")
    return {"status": "ok"}

@app.post("/predict")
async def predict_ids(request: InferenceRequest):
    if not artifacts:
        raise HTTPException(status_code=500, detail="Model artifacts are not loaded.")

    try:
        df_raw = pd.DataFrame(request.data)
        
        df_transformed = transform_ids_features(df_raw, artifacts['log_cols'])
        
        feature_cols = artifacts['feature_cols']
        df_reindexed = df_transformed.reindex(columns=feature_cols, fill_value=0)
        df_final = df_reindexed[feature_cols]
        
        X_scaled = artifacts['scaler'].transform(df_final.astype(float))
        
        class_logits, anomaly_probs = artifacts['model'].predict(X_scaled)
        
        class_probs = tf.nn.softmax(class_logits, axis=-1).numpy()
        predicted_class_indices = np.argmax(class_probs, axis=1)
        
        predicted_class_names = artifacts['le'].inverse_transform(predicted_class_indices)
        
        results = []
        for i in range(len(df_raw)):
            results.append({
                "predicted_class": predicted_class_names[i],
                "class_confidence": round(float(class_probs[i][predicted_class_indices[i]])*100, 2),
                "is_anomaly": bool(anomaly_probs[i][0] > 0.5),
                "anomaly_score": round(float(anomaly_probs[i][0])*100, 2)
            })
            
        return results

    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))