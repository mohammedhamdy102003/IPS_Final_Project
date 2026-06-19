# IPS Project — DevOps & Monitoring Guide

نظام Full DevOps لمشروع الـ AI-Based Intrusion Prevention System:
Docker → Kubernetes (Minikube) → GitHub Actions CI/CD → Prometheus + Grafana Monitoring.

---

## 1) المعمارية

```
GitHub Actions (workflow_dispatch)
   │
   ├─ test          → dotnet build
   ├─ build-and-scan→ docker build + Trivy scan + push لـ Docker Hub
   └─ deploy        → SSH للماشين → kubectl apply (k8s/ + monitoring/)

الماشين (Minikube):
   namespace ips-app
     ├─ mssql        (Deployment + PVC + ClusterIP service)
     └─ ips-app      (Deployment + NodePort :30500)  ← التطبيق نفسه
   namespace monitoring
     ├─ prometheus   (Deployment + NodePort :30090)
     └─ grafana      (Deployment + NodePort :30300)
```

التطبيق نفسه بيعرض `/metrics` (عن طريق `prometheus-net.AspNetCore`)، وProteus بيجمعها
تلقائيًا بفضل الـ annotations على الـ pod (`prometheus.io/scrape: "true"`).

---

## 2) تجهيز الماشين الجديدة (مرة واحدة بس)

```bash
git clone <repo-url>
cd IPS_Final_Project/scripts
chmod +x setup-vm.sh
./setup-vm.sh
```

السكربت بيركّب Docker + kubectl + Minikube، بيشغل minikube، وبيفعّل SSH.
في الآخر هيطبعلك الـ IP والمعلومات اللي محتاجها للخطوة الجاية.

---

## 3) إعداد GitHub Secrets

من `Settings → Secrets and variables → Actions` ضيف:

| Secret            | القيمة                                              |
|--------------------|------------------------------------------------------|
| `DOCKER_USERNAME`  | يوزر Docker Hub بتاعك                                |
| `DOCKER_PASSWORD`  | Access Token من Docker Hub (مش الباسورد العادي)      |
| `VM_IP`            | IP الماشين اللي هتعمله بالسكربت فوق                  |
| `VM_USER`          | اليوزر بتاع الماشين دي                                |
| `VM_SSH_KEY`       | باسورد اليوزر ده (الـ workflow بيستخدمه كـ SSH password) |

⚠️ تأكد إن `k8s/04-app-deployment.yaml` فيه اسم Docker Hub image بتاعك صح
(حاليًا `mohammed102003/ips-app` — غيّره لو يوزرك مختلف).

---

## 4) تشغيل الديبلوي

من تاب **Actions** في الريبو → اختار workflow **"Professional Manual CI/CD"** → **Run workflow**.
الـ pipeline هيعمل build وscan وpush، وبعدين يديبلوي التطبيق + المونيتورينج مع بعض أوتوماتيك.

---

## 5) اللينكات بعد الديبلوي

```
http://<VM_IP أو minikube ip>:30500   → التطبيق نفسه
http://<VM_IP أو minikube ip>:30090   → Prometheus
http://<VM_IP أو minikube ip>:30300   → Grafana (admin / admin123)
```

لو الماشين بره الشبكة المحلية بتاعتك، استخدم `VM_IP` العام. لو جوه نفس الشبكة بس، استخدم
`minikube ip` وانت متصل على نفس الـ VM أو نفس الشبكة.

تفاصيل أكتر عن كل جزء موجودة في `k8s/README.md` و`monitoring/README.md`.

---

## 6) ملاحظات أمان مهمة (للمستقبل، مش لازم تعملها دلوقتي)

- `appsettings.json` فيه إيميل وباسورد Gmail App Password مكتوبين صريح وبيترفعوا عالريبو العام.
  الأفضل تتنقل لـ k8s Secret + environment variable زي ما اتعمل بالظبط مع باسورد الداتابيز.
- لو غيرت الباسورد الموجود حاليًا (`coxh smzp isiw zxhc`) — هو فعليًا App Password شغال على
  Gmail، يفضل تعمله revoke من حساب الـ Gmail بعد التسليم.
