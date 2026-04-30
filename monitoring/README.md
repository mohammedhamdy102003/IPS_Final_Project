# Monitoring Stack — Prometheus + Grafana
## ترتيب الفايلات

```
monitoring/
├── 00-namespace.yaml           # namespace اسمه monitoring
├── 01-prometheus-rbac.yaml     # صلاحيات قراءة K8s API
├── 02-prometheus-configmap.yaml # إعدادات الـ scraping
├── 03-prometheus-deployment.yaml # Prometheus pod + service
└── 04-grafana-deployment.yaml   # Grafana pod + service
```

---

## ⚠️ خطوة مهمة — لازم تعملها في الـ .NET App

Prometheus محتاج endpoint اسمه `/metrics` في الـ app بتاعك.

### 1. أضف الـ NuGet package

```bash
cd IPS_PROJECT
dotnet add package prometheus-net.AspNetCore
```

### 2. عدّل `Program.cs`

```csharp
using Prometheus;   // ← أضف الـ using ده

var builder = WebApplication.CreateBuilder(args);
// ... باقي الكود ...

var app = builder.Build();

// ← أضف السطرين دول قبل app.Run()
app.UseRouting();
app.UseHttpMetrics();          // بيجمع HTTP request metrics تلقائي
app.MapMetrics();              // بيعمل endpoint على /metrics

app.Run();
```

### 3. أضف annotation للـ K8s pod

في فايل `k8s/04-app-deployment.yaml` أضف جوه `template.metadata`:

```yaml
template:
  metadata:
    labels:
      app: ips-app
    annotations:                                    # ← أضف الـ annotations دي
      prometheus.io/scrape: "true"
      prometheus.io/path: "/metrics"
      prometheus.io/port: "8080"
```

---

## تطبيق الـ Monitoring

```bash
# طبّق كل الـ monitoring manifests
kubectl apply -f monitoring/

# تأكد إن كل حاجة شغالة
kubectl get all -n monitoring
```

---

## الـ URLs بعد التشغيل

```bash
MINIKUBE_IP=$(minikube ip)

echo "Prometheus : http://$MINIKUBE_IP:30090"
echo "Grafana    : http://$MINIKUBE_IP:30300"
```

| Service    | Port  | Login              |
|------------|-------|--------------------|
| Prometheus | 30090 | لا يحتاج login     |
| Grafana    | 30300 | admin / admin123   |

---

## Grafana Dashboards — استيراد جاهز

بعد ما تفتح Grafana:
**Dashboards → Import → اكتب الـ ID → Load**

| Dashboard                        | ID    | بيعرض إيه                          |
|----------------------------------|-------|-------------------------------------|
| Kubernetes cluster overview      | 7249  | Nodes, Pods, CPU, Memory            |
| .NET ASP.NET Core                | 10427 | Requests, Errors, Response time     |
| Kubernetes pod metrics           | 6417  | تفاصيل كل pod لوحده                 |

---

## أهم الـ Metrics اللي هتشوفها

### .NET App
- `http_requests_total` — عدد الـ requests
- `http_request_duration_seconds` — وقت الاستجابة
- `process_cpu_seconds_total` — CPU usage
- `dotnet_gc_collections_total` — Garbage Collection

### Kubernetes
- `container_cpu_usage_seconds_total` — CPU لكل container
- `container_memory_usage_bytes` — Memory لكل container
- `kube_pod_status_phase` — حالة الـ pods
