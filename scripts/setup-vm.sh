#!/usr/bin/env bash
# =============================================================================
# setup-vm.sh
# Provision a fresh Ubuntu VM with k3s (lightweight Kubernetes distribution).
# Chosen over Minikube for better performance on resource-constrained
# instances (e.g. t3.medium / t3.large).
#
# Note: k3s does not require a separate Docker installation — it ships
# with containerd built in and pulls images directly from Docker Hub.
#
# Usage:
#   chmod +x setup-vm.sh
#   ./setup-vm.sh
# =============================================================================
set -e

echo "=== 1) Updating system packages ==="
sudo apt-get update -y
sudo apt-get upgrade -y

echo "=== 2) Installing k3s ==="
if ! command -v k3s &> /dev/null; then
  curl -sfL https://get.k3s.io | sh -
fi

echo "=== 3) Waiting for node to become Ready ==="
sudo k3s kubectl wait --for=condition=Ready node --all --timeout=120s

echo "=== 4) Configuring kubeconfig for the current user (no sudo required) ==="
mkdir -p "$HOME/.kube"
sudo cp /etc/rancher/k3s/k3s.yaml "$HOME/.kube/config"
sudo chown "$(id -u)":"$(id -g)" "$HOME/.kube/config"
chmod 600 "$HOME/.kube/config"
# k3s installs kubectl as a symlink automatically; verify it's working:
kubectl get nodes

echo "=== 5) Preparing hostPath directory for MSSQL ==="
sudo mkdir -p /mnt/data/mssql

echo "=== 6) Enabling SSH server (required for GitHub Actions connectivity) ==="
sudo apt-get install -y openssh-server
sudo systemctl enable ssh
sudo systemctl start ssh

echo ""
echo "================================================================"
echo "VM provisioning complete (k3s)."
echo "================================================================"
VM_IP=$(hostname -I | awk '{print $1}')
echo "Local VM IP: $VM_IP"
echo ""
echo "If this VM is running on a cloud provider (AWS/Azure), use its"
echo "public IP instead of the local IP shown above."
echo ""
echo "Next step: add the following secrets under"
echo "GitHub repo -> Settings -> Secrets -> Actions:"
echo "  VM_IP        = VM public IP address"
echo "  VM_USER      = $USER"
echo "  VM_SSH_KEY   = password for this user (the workflow authenticates"
echo "                 via password, not a private key)"
echo "  DOCKER_USERNAME / DOCKER_PASSWORD = Docker Hub credentials"
echo ""
echo "If this is a cloud instance, ensure the security group allows"
echo "inbound traffic on the following ports:"
echo "  22    (SSH)"
echo "  30500 (Application)"
echo "  30090 (Prometheus)"
echo "  30300 (Grafana)"
