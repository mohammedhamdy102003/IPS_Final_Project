# IPS Project — DevOps & Monitoring Guide

نظام Full DevOps لمشروع الـ AI-Based Intrusion Prevention System:
Docker → Kubernetes (k3s) → GitHub Actions CI/CD → Prometheus + Grafana Monitoring → AWS RDS.

---

## 1) المعمارية

```
GitHub Actions (workflow_dispatch)
   │
   ├─ test          → dotnet build
   ├─ build-and-scan→ docker build (app + ai-model) + Trivy scan + push لـ Docker Hub
   └─ deploy        → SSH للـ EC2 → kubectl apply (k8s/ + monitoring/)

EC2 (t3.small, Public Subnet + IGW, k3s):
   namespace ips-app
     ├─ ips-app      (Deployment + NodePort :30500)  ← التطبيق نفسه
     └─ ai-model     (Deployment + ClusterIP :8000)  ← FastAPI + TensorFlow model
   namespace monitoring
     ├─ prometheus   (Deployment + NodePort :30090)
     └─ grafana      (Deployment + NodePort :30300)

AWS RDS (SQL Server Express, db.t3.micro, Private — مش متاحة للإنترنت):
   IPS_project database

Windows Agent (جهاز منفصل):
   agent/windows_traffic_agent.py → بيلتقط الـ network traffic الحقيقي ويبعته
   لـ /api/Traffic/ProcessTraffic على الـ EC2
```

التطبيق بيعرض `/metrics` (عن طريق `prometheus-net.AspNetCore`)، وProteus بيجمعها
تلقائيًا بفضل annotations على الـ pod (`prometheus.io/scrape: "true"`).
الـ AI model API بيتكلم معاه التطبيق داخليًا بس عن طريق `ai-model-service` (مش متاح للبرة).

---

## 2) AWS — الترتيب اللي محتاجه

1. **EC2 t3.small** في **Public Subnet** متوصلة بـ Internet Gateway، مع Public IP مفعّل.
   Security Group مفتوحة على: `22` (SSH)، `30500` (App)، `30090` (Prometheus)، `30300` (Grafana).
2. **RDS** (SQL Server Express, db.t3.micro, Free tier, **Public access = No**) في نفس الـ VPC،
   مع Security Group بتاعتها بتسمح بدخول من Security Group بتاع الـ EC2 على بورت `1433` بس.

تفاصيل خطوة بخطوة في `k8s/README.md`.

---

## 3) تجهيز الماشين (مرة واحدة، بعد ما الـ EC2 تكون شغالة)

```bash
git clone <repo-url>
cd IPS_Final_Project/scripts
chmod +x setup-vm.sh
./setup-vm.sh
```

السكربت بيركّب **k3s** (مش Minikube — أخف بكتير في الـ RAM ومحتاجش Docker على الماشين خالص)،
وبيفعّل SSH.

---

## 4) إعداد GitHub Secrets

من `Settings → Secrets and variables → Actions` ضيف:

| Secret            | القيمة                                                     |
|--------------------|-------------------------------------------------------------|
| `DOCKER_USERNAME`  | يوزر Docker Hub بتاعك                                       |
| `DOCKER_PASSWORD`  | Access Token من Docker Hub (مش الباسورد العادي)             |
| `VM_IP`            | الـ Public IP بتاع الـ EC2                                  |
| `VM_USER`          | اليوزر بتاع الماشين دي                                      |
| `VM_SSH_KEY`       | باسورد اليوزر ده (الـ workflow بيستخدمه كـ SSH password)     |

⚠️ تأكد إن `k8s/04-app-deployment.yaml` و`k8s/05-ai-model-deployment.yaml`
فيهم اسم Docker Hub image بتاعك صح (حاليًا `mohammed102003/...`).

⚠️ قبل أول تشغيل، لازم تملأ `k8s/01-secret.yaml` بمعلومات الـ RDS الحقيقية بتاعتك
(Endpoint, username, password) — التفاصيل في `k8s/README.md`.

---

## 5) تشغيل الديبلوي

من تاب **Actions** → اختار **"Professional Manual CI/CD"** → **Run workflow**.
الـ pipeline هيعمل build + scan + push لصورتين (app + ai-model)، وبعدين يديبلوي كل حاجة
(app + ai-model + monitoring) أوتوماتيك مع بعض.

---

## 6) اللينكات بعد الديبلوي

```
http://<EC2_PUBLIC_IP>:30500   → التطبيق نفسه
http://<EC2_PUBLIC_IP>:30090   → Prometheus
http://<EC2_PUBLIC_IP>:30300   → Grafana (admin / admin123)
```

---

## 7) تشغيل الإيجنت (محاكاة/التقاط traffic حقيقي)

شوف `agent/README.md`. السكربت بيشتغل على جهاز Windows منفصل، ولازم تحط فيه
الـ Public IP بتاع الـ EC2 بعد ما تاخده.

---

## 8) ملاحظات أمان مهمة (للمستقبل، مش لازم تعملها دلوقتي)

- `appsettings.json` فيه إيميل وباسورد Gmail App Password مكتوبين صريح وبيترفعوا عالريبو العام.
  الأفضل تتنقل لـ k8s Secret + environment variable زي ما اتعمل بالظبط مع باسورد RDS.
- بعد التسليم، روح RDS واعمل Delete للـ instance عشان متترفعش عليك فلوس بعد ما الـ Free tier تخلص.
