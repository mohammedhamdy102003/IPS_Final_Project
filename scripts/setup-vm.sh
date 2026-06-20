#!/usr/bin/env bash
# =============================================================================
# setup-vm.sh
# تجهيز ماشين Ubuntu جديدة (فاضية) باستخدام k3s (Kubernetes خفيف جدًا في الموارد)
# بدل Minikube — أنسب لماشين محدودة الـ RAM زي t3.medium/t3.large.
#
# ملاحظة: مع k3s مش محتاج تركّب Docker على الماشين خالص — k3s بيستخدم
# containerd جوّاه ويسحب الـ images من Docker Hub مباشرة.
#
# الاستخدام:
#   chmod +x setup-vm.sh
#   ./setup-vm.sh
# =============================================================================
set -e

echo "=== 1) تحديث النظام ==="
sudo apt-get update -y
sudo apt-get upgrade -y

echo "=== 2) تركيب k3s ==="
if ! command -v k3s &> /dev/null; then
  curl -sfL https://get.k3s.io | sh -
fi

echo "=== 3) استنى الـ node يبقى Ready ==="
sudo k3s kubectl wait --for=condition=Ready node --all --timeout=120s

echo "=== 4) تجهيز kubeconfig لليوزر العادي (بدون sudo) ==="
mkdir -p "$HOME/.kube"
sudo cp /etc/rancher/k3s/k3s.yaml "$HOME/.kube/config"
sudo chown "$(id -u)":"$(id -g)" "$HOME/.kube/config"
chmod 600 "$HOME/.kube/config"
# k3s بيركب kubectl تلقائي كـ symlink، التأكد إنه شغال:
kubectl get nodes

echo "=== 5) تجهيز الـ hostPath بتاع MSSQL ==="
sudo mkdir -p /mnt/data/mssql

echo "=== 6) تفعيل SSH server (مهم عشان GitHub Actions يقدر يتصل) ==="
sudo apt-get install -y openssh-server
sudo systemctl enable ssh
sudo systemctl start ssh

echo ""
echo "================================================================"
echo "✅ الماشين جاهزة (k3s)!"
echo "================================================================"
VM_IP=$(hostname -I | awk '{print $1}')
echo "VM IP (محلي):  $VM_IP"
echo ""
echo "لو الماشين على Cloud (AWS/Azure)، استخدم الـ Public IP بتاعها مش الـ IP المحلي ده."
echo ""
echo "الخطوة الجاية: ضيف الـ Secrets دي في GitHub repo settings → Secrets → Actions:"
echo "  VM_IP        = الـ Public IP بتاع الماشين"
echo "  VM_USER      = $USER"
echo "  VM_SSH_KEY   = الباسورد بتاع اليوزر ده (الـ workflow بيستخدمه كـ password مش private key)"
echo "  DOCKER_USERNAME / DOCKER_PASSWORD = حساب Docker Hub بتاعك"
echo ""
echo "لو الماشين Cloud instance، تأكد إن Security Group بتاعها فاتح للبورتات دي:"
echo "  22 (SSH), 30500 (App), 30090 (Prometheus), 30300 (Grafana)"
echo "================================================================"
