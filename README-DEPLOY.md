# AegisCore — Deployment Guide
>
> Everything you need to deploy AegisCore: from a 3-command local demo to production on your cloud of choice.

---

## Section 1: Quick Start (LOCAL Demo)

Run AegisCore locally with Docker Compose in three commands:

```bash
./scripts/bootstrap-env.sh
docker compose --env-file .env up -d --build --wait
curl --fail http://localhost:8080/health/ready
```

Open **http://localhost:8080**. The stack includes the Next.js portal, .NET API, PostgreSQL 17 (with pgvector), and Redis 7 — all configured for local demo mode.

To stop without losing data:

```bash
docker compose --env-file .env down
```

To use prebuilt images instead of building locally:

```bash
./scripts/bootstrap-env.sh
printf '\nAEGISCORE_API_IMAGE=ghcr.io/<owner>/aegiscore-api:latest\n' >> .env
printf 'AEGISCORE_PORTAL_IMAGE=ghcr.io/<owner>/aegiscore-portal:latest\n' >> .env
docker compose --env-file .env pull api portal
docker compose --env-file .env up -d --no-build --wait
```

---

## Section 2: Production Deployment Decision Tree

Answer these questions in order. When you reach a "Yes", follow the linked guide.

1. **Are you running a quick local/demo evaluation?**
   → [Section 1](#section-1-quick-start-local-demo) above — 3 commands.

2. **Are you deploying to a bare Linux VPS?** (Hetzner, DigitalOcean, Linode, Vultr)
   → [VPS Deployment Guide](docs/deployment/vps.md)

3. **Are you deploying to Microsoft Azure?**
   → [Azure Deployment Guide](docs/deployment/azure.md)

4. **Are you deploying to Amazon Web Services (AWS)?**
   → [AWS Deployment Guide](docs/deployment/aws.md)

5. **Are you deploying to Google Cloud Platform (GCP)?**
   → [GCP Deployment Guide](docs/deployment/gcp.md)

### Comparison Matrix

| Provider           | Monthly Cost (est.) | Complexity | Managed Services                                             | Guide                         |
|--------------------|---------------------|------------|--------------------------------------------------------------|-------------------------------|
| Local (Docker)     | $0                  | Low        | None (self-hosted PostgreSQL, Redis)                         | [Section 1](#section-1-quick-start-local-demo) |
| VPS (bare Linux)   | $30 – $200          | Medium     | None (you manage everything)                                 | [vps.md](docs/deployment/vps.md) |
| Azure              | $150 – $800         | Low        | PostgreSQL Flexible Server, Redis Cache, Key Vault, Container Apps | [azure.md](docs/deployment/azure.md) |
| AWS                | $150 – $800         | Medium     | RDS, ElastiCache, Secrets Manager, ECS Fargate               | [aws.md](docs/deployment/aws.md) |
| GCP                | $150 – $800         | Low-Medium | Cloud SQL, Memorystore, Secret Manager, Cloud Run            | [gcp.md](docs/deployment/gcp.md) |

---

## Section 3: Environment Variables Reference

| Var                                              | Required            | Mode               | Description                                                            | Example                                                       |
|--------------------------------------------------|---------------------|--------------------|------------------------------------------------------------------------|---------------------------------------------------------------|
| `ASPNETCORE_ENVIRONMENT`                         | Yes                 | All                | .NET runtime environment (`Development` / `Production`)                | `Production`                                                  |
| `ASPNETCORE_URLS`                                | No                  | All                | Kestrel listen URLs                                                    | `http://+:8080`                                               |
| `AEGISCORE_CLOUD_PROVIDER`                       | Yes                 | Production         | Cloud provider identifier                                              | `AZURE` / `AWS` / `GCP` / `ONPREMISE`                         |
| `AEGISCORE_SECRET_PROVIDER`                      | Yes                 | All                | Secret backend (`FILE` / `INFISICAL` / `AZURE_KEY_VAULT` / `AWS_SECRETS_MANAGER` / `GCP_SECRET_MANAGER`) | `FILE`                                                        |
| `AEGISCORE_DEMO_MODE`                            | No                  | All                | Enable demo login identity (must be `false` in production)             | `true` / `false`                                              |
| `AEGISCORE_INSTALLATION_COMPLETED`               | Yes                 | Production         | Marks the onboarding wizard as completed                               | `true` / `false`                                              |
| `AEGISCORE_DB_CONNECTION`                        | Yes                 | All                | PostgreSQL connection string                                           | `Host=...;Port=5432;Database=aegiscore;Username=...;Password=...;SslMode=Require` |
| `DB_HOST` / `DB_PORT`                            | Yes                 | Production         | Database host and numeric port                                         | `db.internal` / `5432`                                       |
| `DB_NAME` / `DB_USER` / `DB_PASS`                | Yes                 | Production         | Database name and application credentials                              | `aegiscore` / `aegiscore_app` / `<secret>`                   |
| `AEGISCORE_REDIS_CONNECTION`                     | Yes                 | All                | Redis connection string                                                | `redis:6379,password=...`                                     |
| `AEGISCORE_JWT_SECRET`                           | Yes                 | All                | JWT signing key (64-char base64 minimum)                               | `l4fN8...aQ==`                                                |
| `AEGISCORE_ENCRYPTION_KEY`                       | Yes                 | Production         | AES-256 encryption key (32-byte base64)                                | `kY7mP...wQ==`                                                |
| `AEGISCORE_PQC_KEY_BASE64`                       | Yes                 | Production         | Post-quantum cryptography key (32-byte base64)                         | `pQx3Z...rA==`                                                |
| `LICENSE_HMAC_SECRET`                            | Yes                 | Production         | License HMAC signing secret (64-char base64)                           | `aB9cD...zW==`                                                |
| `GOVERNANCE_FEEDBACK_KEY_BASE64`                 | Yes                 | Production         | Governance feedback encryption key (32-byte base64)                    | `vX8rL...kM==`                                                |
| `TENANT_ID`                                      | Yes                 | All                | Multi-tenant identifier (UUID)                                         | `550e8400-e29b-41d4-a716-446655440000`                        |
| `POSTGRES_DB`                                    | Yes                 | Docker Compose     | PostgreSQL database name                                               | `aegiscore`                                                   |
| `POSTGRES_USER`                                  | Yes                 | Docker Compose     | PostgreSQL application user                                            | `aegiscore`                                                   |
| `POSTGRES_PASSWORD`                              | Yes                 | Docker Compose     | PostgreSQL application password                                        | `<strong password>`                                           |
| `POSTGRES_ADMIN_USER`                            | Yes                 | Docker Compose     | PostgreSQL admin user (owns database)                                  | `aegiscore_admin`                                             |
| `POSTGRES_ADMIN_PASSWORD`                        | Yes                 | Docker Compose     | PostgreSQL admin password                                              | `<strong password>`                                           |
| `REDIS_PASSWORD`                                 | Yes                 | Docker Compose     | Redis AUTH password                                                    | `<strong password>`                                           |
| `AEGISCORE_STORAGE_PROVIDER`                     | No                  | All                | Storage backend (`local` / `s3` / `azureblob` / `sftp`)                | `local`                                                       |
| `AEGISCORE_STORAGE_LOCAL_PATH`                   | No                  | All                | Local storage root (if provider=local)                                 | `/app/storage`                                                |
| `AEGISCORE_LOCAL_SECRET_PATH`                    | No                  | All                | Local vault file path (if SECRET_PROVIDER=FILE)                        | `/app/config/secrets/vault.bin`                               |
| `Database__AutoMigrate`                          | No                  | All                | Run EF Core migrations on startup                                      | `true` / `false`                                              |
| `License__ServerUrl`                             | No                  | Production         | License validation server URL                                          | `https://license.aegiscore.systems`                           |
| `License__OfflineMode`                           | No                  | All                | Validate the signed licence locally and send no heartbeat payload       | `true` / `false`                                             |
| `Runtime__DemoMode`                              | No                  | All                | Runtime demo mode flag                                                 | `true` / `false`                                              |
| `Deployment__FailOnReadinessError`               | No                  | Production         | Exit on readiness probe failure                                        | `true` / `false`                                              |
| `DataProtection__KeyPath`                        | No                  | Production         | ASP.NET Data Protection key ring path                                  | `/app/config/data-protection`                                 |
| `DataProtection__CertificatePath`                | No                  | Production         | Data Protection X.509 certificate path                                 | `/app/config/certs/dp.pfx`                                    |
| `DataProtection__CertificatePassword`            | No                  | Production         | Certificate password                                                   | `<secure password>`                                           |
| `HybridPqcTransport__Enforce`                    | No                  | Production         | Require post-quantum hybrid transport at edge                          | `true` / `false`                                              |
| `HybridPqcTransport__EdgeProofKeyBase64`         | No                  | Production         | Edge proof key for PQC transport                                       | 32-byte base64                                                |
| `PaymentGateways__raast__BaseUrl`                | No                  | Production         | RAAST payment gateway base URL                                         | `https://api.raast.gov.pk/v1/`                                |
| `PaymentGateways__stripe__BaseUrl`               | No                  | Production         | Stripe payment gateway base URL                                        | `https://api.stripe.com/`                                     |
| `PaymentGateways__plaid-ach__BaseUrl`            | No                  | Production         | Plaid ACH payment gateway base URL                                     | `https://production.plaid.com/`                               |
| `PaymentGateways__gocardless__BaseUrl`           | No                  | Production         | GoCardless payment gateway base URL                                    | `https://api.gocardless.com/`                                 |
| `PaymentGateways__billplz-fpx__BaseUrl`          | No                  | Production         | Billplz FPX payment gateway base URL                                   | `https://www.billplz.com/`                                    |
| `PaymentGateways__duitnow-qr__BaseUrl`           | No                  | Production         | DuitNow QR payment gateway base URL                                    | `https://...`                                                 |
| `AI_PROVIDER`                                    | Yes                 | Production         | AI provider (`ollama` / `openai` / `granite` / `custom`)               | `ollama`                                                      |
| `AI_ENDPOINT`                                    | Yes                 | Production         | Provider endpoint URL                                                  | `http://host.docker.internal:11434/`                          |
| `AI_MODEL`                                       | Yes                 | Production         | Provider model identifier                                              | `qwen2.5:7b`                                                  |
| `AI_API_KEY`                                     | Conditional         | Production         | Required for non-local AI providers                                    | `<secret>`                                                    |
| `AEGISCORE_PORT`                                 | No                  | Docker Compose     | Host port for portal                                                   | `8080`                                                        |
| `AEGISCORE_API_IMAGE`                            | No                  | Docker Compose     | Prebuilt API image (overrides local build)                             | `ghcr.io/<owner>/aegiscore-api:latest`                        |
| `AEGISCORE_PORTAL_IMAGE`                         | No                  | Docker Compose     | Prebuilt portal image (overrides local build)                          | `ghcr.io/<owner>/aegiscore-portal:latest`                     |
| `AEGISCORE_API_INTERNAL_URL`                     | Yes*                | Portal container   | Internal URL to the API (*only needed by the portal container)         | `http://api:8080`                                             |
| `AEGISCORE_REQUIRE_EDGE_PQC`                     | No                  | Production         | Enforce post-quantum crypto at the edge                                | `true` / `false`                                              |
| `AEGISCORE_EDGE_PQC_PROOF_KEY_BASE64`            | No                  | Production         | Edge PQC proof key (if enforcement enabled)                            | 32-byte base64                                                |
| `AEGISCORE_DATA_PROTECTION_CERTIFICATE_PATH`     | No                  | Production         | Path to Data Protection certificate                                    | `/app/config/certs/dp.pfx`                                    |
| `AEGISCORE_DATA_PROTECTION_CERTIFICATE_PASSWORD` | No                  | Production         | Password for Data Protection certificate                               | `<secure password>`                                           |

---

## Section 4: Secrets Provider Configuration

AegisCore supports five secret providers. Set `AEGISCORE_SECRET_PROVIDER` to choose one. All keys must exist in the provider for the application to start when `Deployment__FailOnReadinessError=true`.

### 4.1 FILE (local `.env` file — default for Docker Compose)

```ini
AEGISCORE_SECRET_PROVIDER=FILE
AEGISCORE_LOCAL_SECRET_PATH=/app/config/secrets/vault.bin
AEGISCORE_DB_CONNECTION=Host=postgres;Port=5432;Database=aegiscore;Username=aegiscore;Password=<password>
AEGISCORE_REDIS_CONNECTION=redis:6379,password=<password>
# ... all other secrets as plain env vars or in vault.bin
```

The `FILE` provider reads secrets from environment variables directly. For production, consider using the vault file at `AEGISCORE_LOCAL_SECRET_PATH` which encrypts secrets at rest in a binary format.

### 4.2 AZURE_KEY_VAULT

```ini
AEGISCORE_SECRET_PROVIDER=AZURE_KEY_VAULT
AEGISCORE_AZURE_VAULT_URI=https://aegiscore-kv.vault.azure.net/
```

Secrets are read from the Key Vault using the container's managed identity. The identity must have the `Key Vault Secrets User` RBAC role. Secret names map 1:1 to application needs (e.g. secret `AegiscoreDbConnection` maps to `AEGISCORE_DB_CONNECTION`).

### 4.3 AWS_SECRETS_MANAGER

```ini
AEGISCORE_SECRET_PROVIDER=AWS_SECRETS_MANAGER
AWS_SECRET_ARN=arn:aws:secretsmanager:us-east-1:123456789012:secret:aegiscore/production-abc123
```

The secret is expected as a single JSON object with key-value pairs. The ECS task role or EC2 instance profile must have `secretsmanager:GetSecretValue` for the ARN.

### 4.4 GCP_SECRET_MANAGER

```ini
AEGISCORE_SECRET_PROVIDER=GCP_SECRET_MANAGER
GCP_PROJECT_ID=my-project
```

Each secret is a separate Secret Manager secret. The Cloud Run service account or GCE instance service account must have `roles/secretmanager.secretAccessor` on each secret. Environment variables ending in `_NAME` (e.g. `AEGISCORE_DB_CONNECTION_NAME`) specify the secret name.

### 4.5 INFISICAL (external secrets platform)

```ini
AEGISCORE_SECRET_PROVIDER=INFISICAL
INFISICAL_URL=https://app.infisical.com
INFISICAL_TOKEN=st.abc123...
INFISICAL_WORKSPACE_ID=comp_xyz789
INFISICAL_ENVIRONMENT=production
```

Infisical is the recommended external secrets manager for multi-environment, multi-university deployments. All secrets are injected at process startup via the Infisical SDK.

---

## Section 5: GitHub Actions Setup

Add these secrets to your GitHub repository (**Settings → Secrets and variables → Actions → New repository secret**):

### Required for CI/CD Pipelines

| Secret Name                               | Used In               | Description                                   |
|-------------------------------------------|-----------------------|-----------------------------------------------|
| `GITHUB_TOKEN`                            | All workflows         | Auto-provided by GitHub; no setup needed       |
| `GHCR_TOKEN`                              | `release.yml`, `deploy-cloud.yml` | PAT with `write:packages` (release) / `read:packages` (deploy) scope |

### Required for Cloud Deploy (`deploy-cloud.yml`)

| Secret Name       | Description                              |
|-------------------|------------------------------------------|
| `SSH_PRIVATE_KEY` | Private key for SSH to the target server |
| `SSH_USER`        | SSH username (e.g. `ubuntu`, `root`)     |

### Required for Kubernetes Deploy (`deploy.yml`)

| Secret Name                                 | Description                                   |
|---------------------------------------------|-----------------------------------------------|
| `KUBE_CONFIG`                               | Base64-encoded kubeconfig                     |
| `AEGISCORE_DB_CONNECTION`                   | PostgreSQL connection string                   |
| `AEGISCORE_REDIS_CONNECTION`                | Redis connection string                       |
| `AEGISCORE_JWT_SECRET`                      | JWT signing key                               |
| `LICENSE_HMAC_SECRET`                       | License validation key                        |
| `AEGISCORE_PQC_KEY_BASE64`                  | Post-quantum crypto key                       |
| `GOVERNANCE_FEEDBACK_KEY_BASE64`            | Feedback encryption key                       |
| `POSTGRES_USER` / `POSTGRES_PASSWORD`       | Database credentials                          |
| `POSTGRES_ADMIN_USER` / `POSTGRES_ADMIN_PASSWORD` | Admin database credentials              |
| `REDIS_PASSWORD`                            | Redis auth password                           |
| `TENANT_ID`                                 | Tenant UUID                                   |
| `INFISICAL_URL` / `INFISICAL_TOKEN`         | Infisical config (if using Infisical)         |
| `INFISICAL_WORKSPACE_ID` / `INFISICAL_ENVIRONMENT` | Infisical workspace (if using Infisical) |
| `AEGISCORE_DATA_PROTECTION_PFX_BASE64`      | Data Protection certificate (PKCS#12 base64)   |
| `AEGISCORE_DATA_PROTECTION_CERTIFICATE_PASSWORD` | Certificate password                     |
| `AEGISCORE_EDGE_PQC_PROOF_KEY_BASE64`       | Edge PQC proof key                            |

---

## Section 6: Health Checks & Monitoring

AegisCore exposes the following health endpoints:

| Endpoint          | Port  | Purpose                    | Success Response |
|-------------------|-------|----------------------------|------------------|
| `/health/live`    | 8080  | Liveness probe             | HTTP 200         |
| `/health/ready`   | 8080  | Readiness probe (all deps) | HTTP 200 + JSON  |
| `/portal-health`  | 8080  | Portal health (Next.js)    | HTTP 200         |
| `/metrics`        | 8080  | Prometheus metrics endpoint | HTTP 200 + text  |
| `/healthchecks-ui`| 8080  | Health Checks UI (if enabled) | HTML page     |

### Readiness Probe Response (example)

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1250000",
  "entries": {
    "postgres": { "status": "Healthy", "duration": "00:00:00.0450000" },
    "redis":    { "status": "Healthy", "duration": "00:00:00.0120000" },
    "license":  { "status": "Healthy", "duration": "00:00:00.0080000" }
  }
}
```

### Monitoring

- **Prometheus**: Scrape `/metrics` on the API container (port 8080).
- **Grafana**: Import dashboard JSON for AegisCore (included in `monitoring/`).
- **Docker health checks**: Both API and Portal Dockerfiles include `HEALTHCHECK` instructions.

---

## Section 7: Upgrading AegisCore

### Migration Strategy

AegisCore uses Entity Framework Core migrations stored in `AegisCore.Infrastructure/Migrations/`.

1. **Review the changelog** — Read [docs/16-release-notes-changelog.md](docs/16-release-notes-changelog.md) for breaking changes.
2. **Backup the database** — Always take a full `pg_dump` before upgrading.
3. **Pull new images** — `docker compose --env-file .env pull api portal`
4. **Apply migrations** — Set `Database__AutoMigrate=true` for the first startup only, then revert to `false`.
5. **Verify health** — `curl -f http://localhost:8080/health/ready`

### Zero-Downtime Deployment Process

For Kubernetes (see `k8s/` manifests):

1. **Rolling update**: `kubectl -n aegiscore set image deploy/aegiscore-api api=ghcr.io/<owner>/aegiscore-api:<new-sha>`
2. **Wait for rollout**: `kubectl -n aegiscore rollout status deploy/aegiscore-api --timeout=300s`
3. **Verify new pods**: `kubectl -n aegiscore get pods -l app=aegiscore-api`
4. **Rollback if needed**: `kubectl -n aegiscore rollout undo deploy/aegiscore-api`

For Docker Compose (VPS):

1. Pull new images: `docker compose --env-file .env pull`
2. Recreate only changed services: `docker compose --env-file .env up -d --no-deps api portal`
3. The portal and API have health checks; Docker Compose waits (with `--wait`) for healthy status.

### Database Migration Policy

- **Forward-only**: Migrations are idempotent and forward-only. No `DOWN` migrations.
- **Auto-migration**: Set `Database__AutoMigrate=true` on exactly one pod/window, then disable.
- **Manual migration**: Run the SQL directly: `psql "$AEGISCORE_DB_CONNECTION" -f AegisCore.Infrastructure/Migrations/V1.0_AegisCoreBaseline.sql`
- **Rollback plan**: Restore from the pre-upgrade `pg_dump` backup.

---

## Section 8: Troubleshooting

### 1. Containers fail to start (`docker compose up` exits immediately)

**Cause:** Missing required environment variables or incorrect `.env` file.
**Fix:** Run `./scripts/bootstrap-env.sh` to regenerate `.env`, then review all required variables. Check container logs: `docker compose --env-file .env logs api`.

### 2. API container is unhealthy (health check fails)

**Cause:** PostgreSQL or Redis not reachable, or database migrations pending.
**Fix:** Check `docker compose --env-file .env logs api` for connection errors. Ensure `Database__AutoMigrate=true` on first run. Verify PostgreSQL is healthy: `docker compose --env-file .env exec postgres pg_isready -U aegiscore_admin`.

### 3. `curl: (52) Empty reply from server` on health endpoint

**Cause:** API process crashed or failed to bind port 8080.
**Fix:** Check `docker logs aegiscore-api-1`. Look for ASP.NET startup errors. Ensure no port conflict.

### 4. Portal loads but API calls fail with 502/504

**Cause:** Portal cannot reach API. Incorrect `AEGISCORE_API_INTERNAL_URL`.
**Fix:** In Docker Compose, use `http://api:8080`. In production, verify the reverse proxy passes `/api` to the correct API container address.

### 5. Database "role 'aegiscore' does not exist"

**Cause:** Application database user not created, or PostgreSQL init script didn't run.
**Fix:** Connect with admin credentials and run: `CREATE ROLE aegiscore LOGIN PASSWORD '<password>' NOSUPERUSER NOREPLICATION NOBYPASSRLS; GRANT ALL ON DATABASE aegiscore TO aegiscore; GRANT ALL ON SCHEMA public TO aegiscore;`

### 6. `Npgsql.PostgresException: 28P01: password authentication failed`

**Cause:** Password mismatch between `.env` and PostgreSQL container.
**Fix:** Rebuild with consistent credentials. Delete the `postgres_data` volume and restart: `docker compose --env-file .env down -v && docker compose --env-file .env up -d --build --wait`

### 7. Redis authentication failed (`NOAUTH Authentication required`)

**Cause:** Redis is configured with `--requirepass` but the connection string lacks a password.
**Fix:** Ensure `AEGISCORE_REDIS_CONNECTION=redis:6379,password=<password>` matches `REDIS_PASSWORD` in `.env`.

### 8. License validation fails (`License__HmacSecret` mismatch)

**Cause:** Wrong or missing `LICENSE_HMAC_SECRET` in `.env`.
**Fix:** Set `LICENSE_HMAC_SECRET` to the value provided by AegisCore license server. For local development, leave `Runtime__DemoMode=true`.

### 9. Portal builds fail with `npm ERR! missing script: build`

**Cause:** Running `npm run build` outside the Portal directory or Node version mismatch.
**Fix:** Run `npm ci && npm run build` inside `AegisCore.Portal/`. Ensure Node.js 22 is installed (`node --version`). Use `npx tsc --noEmit` first to catch TypeScript errors.

### 10. Docker Compose "port is already allocated"

**Cause:** Another process is using port 8080.
**Fix:** Change `AEGISCORE_PORT` in `.env` to an unused port (e.g. `AEGISCORE_PORT=3000`), then restart.

### 11. (Bonus) Storage uploads fail with "Access Denied"

**Cause:** `AEGISCORE_STORAGE_PROVIDER` is set to `s3` or `azureblob` but cloud credentials are not configured.
**Fix:** For local VPS deployments, keep `AEGISCORE_STORAGE_PROVIDER=local`. For cloud providers, ensure the storage credentials or managed identity policies are correctly set.
