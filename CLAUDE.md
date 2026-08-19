# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A TODO-list web app deployed end-to-end via a DevOps pipeline (Terraform/Ansible → Jenkins → Docker → Kubernetes), built for the FSAC "DevOps et Intégration Continue" course project. The assignment spec (`Projet en DevOps et Intégration Continue.pdf`) grades: infra provisioning (3pts), a working app (2pts), a full Jenkins CI/CD pipeline (5pts), Kubernetes manifests (5pts), Git workflow (3pts), docs (2pts).

Note: the spec names Node.js/Java/Python Flask as the expected app stack and PostgreSQL as the example DB; this project uses ASP.NET Core + SQL Server instead (deliberate choice, not an oversight) — flag this if grading is stack-strict.

## Repo layout

- `app/` — ASP.NET Core 10 **MVC** app (`Controllers/` + `Views/`, not Razor Pages), EF Core + SQL Server. `TodoController` (`Controllers/TodoController.cs`) has actions `Index`/`Add`/`Toggle`/`Delete`/`Error`; the single view lives at `Views/Todo/Index.cshtml`. `Program.cs` runs `db.Database.Migrate()` on startup — no manual migration step needed when the container boots against a reachable SQL Server.
- `tests/` — xUnit tests against `TodoDbContext` using the EF Core InMemory provider (no live DB needed to test).
- `Dockerfile` — multi-stage build (SDK → aspnet runtime), listens on `:8080` (`ASPNETCORE_URLS`), no HTTPS redirection inside the container (TLS terminates outside, e.g. at the Service/Ingress).
- `k8s/` — SQL Server (`mssql-secret.yaml`, `mssql-pvc.yaml`, `mssql-deployment.yaml`) and the app (`app-deployment.yaml`, `app-service.yaml`, NodePort 30080). App gets its connection string via `ConnectionStrings__Default` (ASP.NET Core's `__` = config section separator), `sa` password injected from the Secret.
- `terraform/` — provisions 2 VMs (Jenkins host + Kubernetes host) via the `terra-farm/virtualbox` provider, matching the course's Windows/VirtualBox setup.
- `ansible/` — `setup-jenkins.yml` (Docker, Git, JDK, Jenkins) and `setup-kubernetes.yml` (Docker, kubectl, Minikube), targeting `hosts.ini` inventory groups `[jenkins]` / `[kubernetes]`.
- `Jenkinsfile` — checkout → `dotnet test` → `docker build` → push to DockerHub (credential id `dockerhub-credentials`) → `kubectl apply`/`kubectl set image` deploy, tagged by `${env.BUILD_NUMBER}`.

## Commands

Run from the repo root unless noted.

```bash
# restore / build / run locally (needs a reachable SQL Server — see README)
dotnet restore
dotnet build
dotnet run --project app

# tests (whole suite)
dotnet test

# a single test
dotnet test tests/TodoApp.Tests.csproj --filter "FullyQualifiedName~TogglingTask_FlipsDoneState"

# EF Core migrations (run from app/, requires the dotnet-ef global tool)
cd app && dotnet ef migrations add <Name> --output-dir Data/Migrations
cd app && dotnet ef database update

# container build
docker build -t todoapp:local .

# k8s manifests (apply in this order — app depends on the mssql Service/Secret existing)
kubectl apply -f k8s/mssql-secret.yaml -f k8s/mssql-pvc.yaml -f k8s/mssql-deployment.yaml
kubectl apply -f k8s/app-deployment.yaml -f k8s/app-service.yaml
```

## Gotchas

- Building/testing while the app is running under a debugger (Visual Studio/IIS Express) locks `app/bin/**/TodoApp.dll` and `dotnet build`/`dotnet test` from the terminal will fail with MSB3026/MSB3027. Stop the debugger first.
- `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.SqlServer`, and the test project's `Microsoft.EntityFrameworkCore.InMemory` package must stay on the same EF Core line (currently 10.0.4) — bumping one alone reintroduces an assembly version conflict (CS1705) between the app and test projects. Keep all three in lockstep when upgrading.
- A local connection string needs `Trusted_Connection=True` (Windows Auth) or an explicit `User Id=...;Password=...` — omitting both gives `Login failed for user ''` since SqlClient doesn't default to Windows Auth on its own.
- `k8s/app-deployment.yaml` has a placeholder image (`<DOCKERHUB_USER>/todoapp:latest`) — the Jenkinsfile overwrites it via `kubectl set image`, but a manual `kubectl apply` needs that placeholder replaced first.
- SQL Server's container image runs as a non-root user (uid 10001) and needs write access to the PVC — `k8s/mssql-deployment.yaml` sets `securityContext.fsGroup: 10001` on the pod for this; dropping it causes the container to fail mounting `/var/opt/mssql`.
- The app was originally scaffolded as Razor Pages, then converted to classic MVC (`Controllers/` + `Views/`) at the user's request — if you see stray references to `Pages/` or `.cshtml.cs` PageModels anywhere (docs, old notes), they're stale; the current source of truth is `Controllers/TodoController.cs` + `Views/Todo/*.cshtml`.
