#!/usr/bin/env bash
# =============================================================================
# setup-vm.sh
# تجهيز ماشين Ubuntu جديدة (فاضية) عشان تستضيف مشروع IPS كامل:
# Docker + kubectl + Minikube + فتح الـ SSH عشان GitHub Actions يقدر يديبلوي.
#
# الاستخدام:
#   chmod +x setup-vm.sh
#   ./setup-vm.sh
#
# لازم تشغله بيوزر عادي (مش root) وعنده صلاحية sudo.
# =============================================================================
set -e

echo "=== 1) تحديث النظام ==="
sudo apt-get update -y
sudo apt-get upgrade -y

echo "=== 2) تركيب Docker ==="
if ! command -v docker &> /dev/null; then
  curl -fsSL https://get.docker.com -o get-docker.sh
  sudo sh get-docker.sh
  rm get-docker.sh
fi
sudo usermod -aG docker "$USER"
echo "تمت إضافة $USER لجروب docker. (لازم تعمل logout/login أو 'newgrp docker' عشان يسري)"

echo "=== 3) تركيب kubectl ==="
if ! command -v kubectl &> /dev/null; then
  curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
  sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
  rm kubectl
fi

echo "=== 4) تركيب Minikube ==="
if ! command -v minikube &> /dev/null; then
  curl -LO https://storage.googleapis.com/minikube/releases/latest/minikube-linux-amd64
  sudo install minikube-linux-amd64 /usr/local/bin/minikube
  rm minikube-linux-amd64
fi

echo "=== 5) تشغيل Minikube بالـ Docker driver ==="
# لازم تشغل minikube بنفس اليوزر اللي جوه جروب docker، مش بـ root.
# sg docker بتشغل الأمر وكأنك بالفعل جوه جروب docker من غير ما تعمل logout/login.
sg docker -c "minikube start --driver=docker --cpus=2 --memory=4096"

echo "=== 6) تجهيز الـ hostPath بتاع MSSQL جوه minikube ==="
sg docker -c "minikube ssh -- sudo mkdir -p /mnt/data/mssql"

echo "=== 7) تفعيل SSH server (مهم عشان GitHub Actions يقدر يتصل) ==="
sudo apt-get install -y openssh-server
sudo systemctl enable ssh
sudo systemctl start ssh

echo ""
echo "================================================================"
echo "✅ الماشين جاهزة!"
echo "================================================================"
echo "minikube IP:   $(sg docker -c 'minikube ip')"
echo "VM public/local IP للـ SSH: $(hostname -I | awk '{print $1}')"
echo ""
echo "الخطوة الجاية: ضيف الـ Secrets دي في GitHub repo settings → Secrets → Actions:"
echo "  VM_IP        = IP الماشين دي (اللي تقدر توصله من بره عبر SSH)"
echo "  VM_USER      = $USER"
echo "  VM_SSH_KEY   = الباسورد بتاع اليوزر ده (الـ workflow بيستخدمه كـ password مش private key)"
echo "  DOCKER_USERNAME / DOCKER_PASSWORD = حساب Docker Hub بتاعك"
echo ""
echo "لو الماشين على شبكة محلية مش عندها IP عام، لازم تعمل port-forward من الراوتر"
echo "لبورت 22 (SSH) على الأقل عشان GitHub Actions (سيرفر بره) يقدر يوصلها."
echo "================================================================"
