# AegisCore Deployment Decision Tree
>
> Choose the right deployment guide for your university infrastructure.

## Which Guide Should You Follow?

Answer these questions in order. When you hit a "Yes", follow the linked guide.

1. **Are you running a quick local/demo evaluation?**
   → [README Quick Start](../README.md) — `docker compose up` in 3 commands.

2. **Are you deploying to a bare Linux VPS (e.g. Hetzner, DigitalOcean, Linode)?**
   → [VPS Deployment Guide](./vps.md) — Docker Compose + Nginx + Certbot.

3. **Are you deploying to Microsoft Azure?**
   → [Azure Deployment Guide](./azure.md) — Container Apps + Azure DB + Key Vault.

4. **Are you deploying to Amazon Web Services (AWS)?**
   → [AWS Deployment Guide](./aws.md) — ECS Fargate + RDS + Secrets Manager.

5. **Are you deploying to Google Cloud Platform (GCP)?**
   → [GCP Deployment Guide](./gcp.md) — Cloud Run + Cloud SQL + Secret Manager.

## Provider Comparison

| Provider           | Monthly Cost (est.) | Complexity | Managed Services Included             | Guide          |
|--------------------|---------------------|------------|---------------------------------------|----------------|
| Local (Docker)     | $0                  | Low        | None (self-hosted PostgreSQL, Redis)  | [README](../README.md) |
| VPS (bare Linux)   | $30 – $200          | Medium     | None (you manage everything)          | [vps.md](./vps.md) |
| Azure              | $150 – $800         | Low        | PostgreSQL Flexible Server, Redis Cache, Key Vault, Container Apps | [azure.md](./azure.md) |
| AWS                | $150 – $800         | Medium     | RDS, ElastiCache, Secrets Manager, ECS Fargate | [aws.md](./aws.md) |
| GCP                | $150 – $800         | Low-Medium | Cloud SQL, Memorystore, Secret Manager, Cloud Run | [gcp.md](./gcp.md) |

## Quick Decision Flowchart

```
Start
 │
 ├─ Local evaluation? ─── YES ──▶ docker compose up (README)
 │
 ├─ Own a Linux VPS? ──── YES ──▶ vps.md
 │
 ├─ Microsoft ecosystem? ─ YES ──▶ azure.md
 │
 ├─ AWS shop? ─────────── YES ──▶ aws.md
 │
 └─ GCP / Cloud Run? ──── YES ──▶ gcp.md
```

## Common Across All Providers

- **Container images:** `ghcr.io/<owner>/aegiscore-api` and `aegiscore-portal`
- **Required services:** PostgreSQL 17+ with pgvector, Redis 7+, TLS termination
- **Secrets provider:** `FILE` (`.env`), `AZURE_KEY_VAULT`, `AWS_SECRETS_MANAGER`, `GCP_SECRET_MANAGER`, or `INFISICAL`
- **Health endpoints:** `/health/live` (liveness), `/health/ready` (readiness), `/portal-health` (portal)
