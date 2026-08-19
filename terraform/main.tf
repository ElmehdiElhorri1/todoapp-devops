terraform {
  required_providers {
    virtualbox = {
      source  = "terra-farm/virtualbox"
      version = "0.2.2-alpha.1"
    }
  }
}

provider "virtualbox" {}

variable "ubuntu_image" {
  description = "Path to the Ubuntu Server ISO/box used to provision the VMs"
  type        = string
  default     = "C:/ISO/ubuntu-22.04-live-server-amd64.iso"
}

resource "virtualbox_vm" "jenkins" {
  name   = "jenkins-vm"
  image  = var.ubuntu_image
  cpus   = 2
  memory = "2048 mib"

  network_adapter {
    type = "nat"
  }
}

resource "virtualbox_vm" "kubernetes" {
  name   = "kubernetes-vm"
  image  = var.ubuntu_image
  cpus   = 2
  memory = "4096 mib"

  network_adapter {
    type = "nat"
  }
}

output "jenkins_ip" {
  value = virtualbox_vm.jenkins.network_adapter[0].ipv4_address
}

output "kubernetes_ip" {
  value = virtualbox_vm.kubernetes.network_adapter[0].ipv4_address
}
