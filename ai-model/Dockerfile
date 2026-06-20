
FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt


COPY dense_v5_API.py .
COPY dense_v5.keras .
COPY encoder_dense_v5.joblib .
COPY features_dense_v5.joblib .
COPY log_cols_dense_v5.joblib .
COPY scaler_dense_v5.joblib .

ENV TF_CPP_MIN_LOG_LEVEL=3
ENV OMP_NUM_THREADS=1
ENV TF_NUM_INTRAOP_THREADS=1
ENV TF_NUM_INTEROP_THREADS=1

EXPOSE 8000

CMD ["uvicorn", "dense_v5_API:app", "--host", "0.0.0.0", "--port", "8000"]
