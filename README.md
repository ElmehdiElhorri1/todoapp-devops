# TodoApp — Déploiement DevOps

Application web de gestion de tâches (TODO list) déployée via un pipeline CI/CD complet :
Ansible/Terraform → Jenkins → Docker → Kubernetes.

Projet réalisé dans le cadre du cours *DevOps et Intégration Continue* (FSAC, 2025/2026).

## Stack

- **Application** : ASP.NET Core 10 (MVC) + Entity Framework Core + SQL Server
- **Infrastructure** : Terraform (VMs) + Ansible (provisioning Docker/Git/Kubernetes)
- **CI/CD** : Jenkins (`Jenkinsfile`) — build, tests, image Docker, push DockerHub, déploiement `kubectl`
- **Orchestration** : Kubernetes (Deployment, Service NodePort, PVC, Secret)

## Structure

```
app/            Application ASP.NET Core (MVC)
tests/          Tests xUnit
terraform/      Provisionnement des VMs Jenkins + Kubernetes
ansible/        Configuration des VMs (Docker, Git, Jenkins, kubectl, Minikube)
k8s/            Manifests Kubernetes (Deployment, Service, PVC, Secret)
Dockerfile      Image multi-stage de l'application
Jenkinsfile     Pipeline CI/CD
```

## Développement local

Nécessite une instance SQL Server accessible. Deux options :

```bash
# Option A — SQL Server via Docker
docker run -d --name todo-mssql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=Todoapp_2026 \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# Option B — SQL Server Express / LocalDB déjà installé sur la machine
# → adapter ConnectionStrings:Default dans app/appsettings.json (ex. "Server=localhost\SQLEXPRESS;...;Trusted_Connection=True;TrustServerCertificate=true")

# Restaurer, builder, lancer
dotnet restore
dotnet run --project app
```

L'application écoute sur `https://localhost:5001` (ou le port indiqué par `dotnet run`).
Endpoint de santé : `GET /healthz`.

## Tests

```bash
dotnet test
```

## Build & run avec Docker

```bash
docker build -t todoapp:local .
docker run -p 8080:8080 -e ConnectionStrings__Default="Server=host.docker.internal,1433;Database=todoapp;User Id=sa;Password=Todoapp_2026;TrustServerCertificate=true" todoapp:local
```

## Déploiement Kubernetes (manuel)

```bash
kubectl apply -f k8s/mssql-secret.yaml
kubectl apply -f k8s/mssql-pvc.yaml
kubectl apply -f k8s/mssql-deployment.yaml
kubectl apply -f k8s/app-deployment.yaml
kubectl apply -f k8s/app-service.yaml
minikube service todoapp-service --url
```

## Infrastructure (Terraform + Ansible)

```bash
cd terraform
terraform init
terraform apply

cd ../ansible
ansible-playbook -i hosts.ini setup-jenkins.yml
ansible-playbook -i hosts.ini setup-kubernetes.yml
```

## Workflow Git

- `main` : branche stable, protégée
- `dev` : intégration des fonctionnalités
- `feature/*` : une branche par fonctionnalité, fusionnée dans `dev` via pull request
