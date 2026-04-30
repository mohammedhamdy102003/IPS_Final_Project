# K8s Manifests — IPS App + MSSQL on Minikube
## ترتيب الفايلات

```
k8s/
├── 00-namespace.yaml        # الـ namespace عشان كل حاجة تتجمع
├── 01-secret.yaml           # الـ passwords وconnection string
├── 02-mssql-pv-pvc.yaml     # storage للـ database
├── 03-mssql-deployment.yaml # الـ MSSQL pod + ClusterIP service
└── 04-app-deployment.yaml   # الـ .NET app pod + NodePort service
```

---

## قبل ما تطبق — خطوة مهمة

في `04-app-deployment.yaml` غيّر السطر ده:
```yaml
image: YOUR_DOCKERHUB_USERNAME/ips-app:latest
```
لاسم الـ image بتاعك الحقيقي على Docker Hub.

---

## خطوات التطبيق

```bash
# 1. ابدأ minikube لو مش شغال
minikube start

# 2. طبّق الـ manifests بالترتيب
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-secret.yaml
kubectl apply -f k8s/02-mssql-pv-pvc.yaml
kubectl apply -f k8s/03-mssql-deployment.yaml
kubectl apply -f k8s/04-app-deployment.yaml

# أو كلهم مرة واحدة (بالترتيب التلقائي بالاسم)
kubectl apply -f k8s/
```

---

## إزاي تعرف الـ IP:Port بتاعك

```bash
# الـ Minikube IP
minikube ip
# مثال: 192.168.49.2

# الـ NodePort هو 30500 زي ما اتحدد في الـ manifest
# إذن الـ URL هيكون:
# http://192.168.49.2:30500
```

> في الـ Electron app بتاعك حط القيمة دي كـ base URL

---

## أوامر مفيدة للـ debugging

```bash
# شوف حالة كل حاجة
kubectl get all -n ips-app

# لو في pod مش بيقوم
kubectl describe pod -n ips-app <pod-name>

# شوف الـ logs
kubectl logs -n ips-app deployment/ips-app
kubectl logs -n ips-app deployment/mssql

# دخل جوه الـ app container
kubectl exec -it -n ips-app deployment/ips-app -- /bin/sh
```

---

## ملاحظات مهمة

| موضوع | التفاصيل |
|-------|----------|
| **الـ DB** | مش متعرضة للبرا (ClusterIP فقط) — أكثر أمان |
| **الـ App** | متعرضة على port `30500` |
| **initContainer** | الـ app بينتظر الـ MSSQL يكون جاهز الأول تلقائياً |
| **الـ Password** | محفوظ في Secret مش hardcoded في الـ deployment |
| **الـ Data** | محفوظة على hostPath في الـ Minikube VM — مش هتتمسح لو الـ pod راح |
