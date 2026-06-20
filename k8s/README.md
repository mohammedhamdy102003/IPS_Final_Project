# K8s Manifests — IPS App + AI Model on k3s (DB على RDS)

## ترتيب الفايلات

```
k8s/
├── 00-namespace.yaml             # namespace ips-app
├── 01-secret.yaml                # connection string بتاع RDS
├── 04-app-deployment.yaml        # .NET app (Deployment + NodePort :30500)
├── 05-ai-model-deployment.yaml   # AI model (Deployment + ClusterIP :8000)
└── legacy-local-db/              # ملفات MSSQL محلي قديمة، مش بتتطبق دلوقتي
```

---

## خطوة 1 — إنشاء AWS RDS (مرة واحدة بس)

1. AWS Console → **RDS → Create database → Standard create**
2. Engine type: **Microsoft SQL Server** → Edition: **SQL Server Express Edition**
3. Templates: **Free tier**
4. Settings:
   - DB instance identifier: `ips-project-db`
   - Master username: `ipsadmin` (تجنب `sa`، محجوز)
   - Master password: اختار باسورد قوي واحفظه
5. Storage: 20 GiB، قفل **Storage autoscaling**
6. Connectivity:
   - لو AWS وراك خيار **"Connect to an EC2 compute resource"** → اختاره وحدد الـ EC2 بتاعك
     (هيظبط الـ Security Group لوحده)
   - لو مش موجود: نفس **VPC** بتاع الـ EC2، و **Public access = No**
7. Additional configuration → **Initial database name: `IPS_project`** (لازم بالظبط بالاسم ده)
8. عطّل Performance Insights / Enhanced monitoring (مش لازمة)
9. Create database → استنى ~5-10 دقايق للحالة **Available**

### لو محتاج تظبط الـ Security Group يدوي
RDS instance → **Connectivity & security** → دوس على الـ VPC security group بتاعها
→ **Edit inbound rules** → Add rule: Type **MS SQL** (بورت 1433 تلقائي)، Source = الـ
Security Group بتاع الـ EC2 (مش IP).

### خد الـ Endpoint
من نفس التاب، انسخ قيمة **Endpoint** (شكلها `ips-project-db.xxxxx.us-east-1.rds.amazonaws.com`)

---

## خطوة 2 — قبل ما تطبق المانفستس

### 1) حدّث `01-secret.yaml` بمعلومات الـ RDS الحقيقية:
```yaml
connection-string: "Server=<RDS-ENDPOINT>,1433;Database=IPS_project;User Id=ipsadmin;Password=<PASSWORD>;TrustServerCertificate=True;"
```

### 2) تأكد من اسم الـ Docker images في:
- `04-app-deployment.yaml` → `image: YOUR_DOCKERHUB_USERNAME/ips-app:latest`
- `05-ai-model-deployment.yaml` → `image: YOUR_DOCKERHUB_USERNAME/ips-ai-model:latest`

---

## خطوات التطبيق

```bash
# k3s بيقوم تلقائي كـ systemd service بعد setup-vm.sh، مش محتاج "start" زي minikube

kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-secret.yaml
kubectl apply -f k8s/04-app-deployment.yaml
kubectl apply -f k8s/05-ai-model-deployment.yaml

# أو كلهم مرة واحدة (الـ pipeline بيعمل كده تلقائي)
kubectl apply -f k8s/
```

---

## إزاي تعرف الـ IP:Port بتاعك

مع k3s، الـ NodePort بيتعرض مباشرة على IP الماشين نفسها (مفيش طبقة إضافية زي
`minikube ip`):

```bash
# IP الماشين (private، لو إنت جوه نفس الشبكة)
hostname -I

# لو الماشين على AWS، استخدم الـ Public IP بتاعها من EC2 console
# الـ URL: http://<PUBLIC_IP>:30500
```

---

## أوامر مفيدة للـ debugging

```bash
# شوف حالة كل حاجة
kubectl get all -n ips-app

# لو في pod مش بيقوم
kubectl describe pod -n ips-app <pod-name>

# شوف الـ logs
kubectl logs -n ips-app deployment/ips-app
kubectl logs -n ips-app deployment/ai-model

# دخل جوه الـ app container
kubectl exec -it -n ips-app deployment/ips-app -- /bin/sh

# تأكد إن التطبيق قادر يوصل للـ RDS (من جوه pod الـ app)
kubectl exec -it -n ips-app deployment/ips-app -- sh -c "nc -zv <RDS-ENDPOINT> 1433"
```

---

## ملاحظات مهمة

| موضوع | التفاصيل |
|-------|----------|
| **الـ DB** | على AWS RDS، مش جوه الكلستر — Public access = No، متاحة من الـ EC2 بس عن طريق الـ Security Group |
| **الموديل** | متاح داخليًا بس عن طريق `ai-model-service:8000` (ClusterIP، مش NodePort) |
| **الـ App** | متعرضة على port `30500` |
| **migrations** | بتتطبق تلقائي وقت ما الـ app pod يقوم (`db.Database.Migrate()` في `Program.cs`) |
| **الـ Password** | محفوظ في k8s Secret مش hardcoded في الـ deployment |
