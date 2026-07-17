# DefendX — AI-Based Intrusion Prevention System (IPS)

DefendX is a full-stack, cloud-native Intrusion Prevention System that combines a Web Application Firewall (WAF), an AI-powered traffic inference service, and a real-time monitoring dashboard — deployed as a containerized microservice architecture on Kubernetes (k3s).

Built as a graduation project (Communications & Electronics Engineering, Sohag University, 2026), DefendX demonstrates end-to-end DevOps ownership: from infrastructure provisioning and CI/CD to observability and AI-driven threat detection.

## 🏗️ Architecture

```
Internet → WAF (ModSecurity/Nginx) → k3s Cluster (AWS EC2)
                                          ├── ASP.NET Core MVC Dashboard
                                          ├── FastAPI AI Inference Service (Keras multimodal model)
                                          ├── AWS RDS (persistent storage)
                                          └── Prometheus + Grafana (monitoring)
```

*(Add your architecture diagram image here — e.g. `docs/architecture.png`)*

## ⚙️ Key Components

| Component | Description | Tech Stack |
|---|---|---|
| **WAF Layer** | Filters and inspects incoming traffic before it reaches the application | ModSecurity, Nginx |
| **AI Inference Service** | Analyzes traffic patterns and flags/blocks malicious requests in real time | FastAPI, Keras (multimodal model), Python |
| **Dashboard** | Admin interface for monitoring alerts, managing users, and reviewing traffic decisions | ASP.NET Core MVC, Identity |
| **Orchestration** | Container orchestration and service scheduling | Kubernetes (k3s) on AWS EC2 |
| **CI/CD** | Automated build, test, and deployment pipeline | GitHub Actions |
| **Monitoring** | Metrics collection and visualization for system health and threat activity | Prometheus, Grafana |
| **Database** | Persistent storage for users, logs, and detection history | AWS RDS |

## ✨ Features

- Real-time traffic inspection and AI-based threat classification
- WAF-enforced HTTPS with request filtering before traffic hits the application layer
- Admin dashboard with user management and approval workflows
- Live monitoring dashboards (Prometheus metrics + Grafana visualizations)
- Fully containerized services deployed via Kubernetes manifests
- Automated CI/CD pipeline for build and deployment
- Standalone Windows-packaged distributable (PyInstaller + self-contained .NET executable + launcher) for offline demos

## 🚀 Getting Started

### Prerequisites
- Docker & Docker Compose
- k3s (or any Kubernetes cluster)
- .NET 8 SDK
- Python 3.10+

### Local Setup (Docker Compose)
```bash
git clone https://github.com/mohammedhamdy102003/IPS_Final_Project.git
cd IPS_Final_Project
docker-compose up --build
```

### Kubernetes Deployment
```bash
kubectl apply -f k8s/
```

*(Fill in environment variables / secrets setup instructions here — appsettings.json, DB connection string, etc.)*

## 📊 Monitoring

Grafana dashboards are pre-configured under `monitoring/` to visualize:
- Request/traffic volume
- Blocked vs. allowed requests
- AI inference latency
- System resource usage (CPU/Memory)

## 🧠 Why This Project

Most IPS solutions rely purely on static rule sets. DefendX combines traditional WAF filtering with a machine learning inference layer, allowing it to catch patterns that static rules miss — while keeping full observability into every decision made.

## 👤 Author

**Mohammed Hamdy Mahmoud**
DevOps Lead & Full-Stack Developer
[LinkedIn](https://www.linkedin.com/in/mohamed-hamdy-384714316) · [GitHub](https://github.com/mohammedhamdy102003)

## 📄 License

*(Add a license if you want the repo to be reusable — MIT is a common choice for portfolio projects)*
