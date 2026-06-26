FROM python:3.11-slim

# ضبط بيئة العمل
WORKDIR /app

# نسخ المتطلبات وتثبيتها
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# نسخ ملفات الموديل (تأكد إنها في نفس المجلد مع الـ Dockerfile في الريبو)
COPY multimodal_ids_2head_model_v4.keras .
COPY multimodal_inference_artifacts_v4.pkl .

# نسخ ملف الـ API الجديد
COPY Final_API_v4.py .

# تحسين أداء TensorFlow
ENV TF_CPP_MIN_LOG_LEVEL=3
ENV OMP_NUM_THREADS=1
ENV TF_NUM_INTRAOP_THREADS=1
ENV TF_NUM_INTEROP_THREADS=1

EXPOSE 8000

# تشغيل الـ API باستخدام ملف الـ Final_API_v4
# ملاحظة: الـ FastAPI app جوه الملف مسميها "app"
CMD ["uvicorn", "Final_API_v4:app", "--host", "0.0.0.0", "--port", "8000"]