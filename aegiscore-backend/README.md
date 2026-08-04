# AegisCore Backend

Standalone backend workspace for the AegisCore university ERP API.

## Contents

- `AegisCore.API`: .NET 10 HTTP API.
- `AegisCore.Application`: application contracts and use cases.
- `AegisCore.Domain`: domain entities.
- `AegisCore.Infrastructure`: EF Core, PostgreSQL, Redis, integrations.
- `AegisCore.Tests`: backend tests.
- `k8s`: Kubernetes manifests.

## Local Checks

```bash
dotnet test AegisCore.slnx -m:1 -p:BuildInParallel=false
docker compose --env-file .env.example up -d --build
curl http://localhost:8080/health
docker compose --env-file .env.example down
```

## Secrets

Use a real `.env` for local development. `.env.example` is disposable smoke-test input only.
