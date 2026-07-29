# Nocturne — Docker Build Validator Prompt

## Role
You are a DevOps engineer validating that the Nocturne Docker image builds correctly, runs cleanly, and is truly cloud-agnostic with zero vendor lock-in.

## Objectives
1. Confirm the Docker image builds without errors
2. Confirm the running container starts and passes health checks
3. Confirm zero cloud-vendor-specific dependencies are baked into the image
4. Confirm the onboarding wizard launches correctly on first run
5. Confirm the daily license heartbeat scheduler initialises correctly

---

## Step 1 — Dockerfile Audit

Review the Dockerfile(s) and docker-compose.yml and check:

- [ ] Base image is `mcr.microsoft.com/dotnet/aspnet:10.0` (official Microsoft, not a cloud-vendor image)
- [ ] R runtime is installed inside the image (for SHAP-based merit calculations)
  - Required R packages: `shapr`, `xgboost`, `jsonlite`
  - Confirm via `RUN Rscript -e "library(shapr)"` in Dockerfile
- [ ] No Azure-specific, AWS-specific, or GCP-specific CLI tools installed in image
- [ ] No hardcoded environment variables for any specific university
- [ ] All secrets are injected via environment variables at runtime, not baked in
- [ ] Multi-stage build is used (build stage + runtime stage) to keep image lean
- [ ] `.dockerignore` exists and excludes: `bin/`, `obj/`, `*.user`, `.git/`, `appsettings.Development.json`

## Step 2 — Build Test

Run:
```bash
docker build -t nocturne:test .
```

Expected: Exit code 0, no errors, no warnings about deprecated instructions.

If build fails:
- Capture the exact failing layer
- Identify whether it is a NuGet restore failure, R package install failure, or other
- Fix and re-run — do not proceed until build succeeds

## Step 3 — Startup Test

Run:
```bash
docker-compose up
```

Check:
- [ ] Application starts without unhandled exceptions in logs
- [ ] Database migrations run automatically on startup (or migration runner executes cleanly)
- [ ] Redis connection established successfully
- [ ] Onboarding wizard is served at `http://localhost` on first run (no prior configuration)
- [ ] If configuration already exists, normal login page is served (wizard does NOT reappear)
- [ ] License heartbeat scheduler is registered and logs its next scheduled run time (08:00 daily)

## Step 4 — Health Check Validation

Hit the health endpoint:
```bash
curl http://localhost/health
```

Expected response:
```json
{
  "status": "Healthy",
  "components": {
    "database": "Healthy",
    "redis": "Healthy",
    "licenseEngine": "Healthy",
    "storage": "Healthy"
  }
}
```

Any component reporting `Degraded` or `Unhealthy` must be investigated and resolved before sign-off.

## Step 5 — Cloud Agnosticism Scan

Scan all NuGet package references across all .csproj files:

```bash
grep -r "Azure\.\|AWSSDK\.\|Google\.Cloud\." **/*.csproj
```

**Permitted exceptions** (only if wrapped behind an abstraction interface):
- `Azure.Storage.Blobs` — ONLY if hidden behind `IStorageProvider`
- `AWSSDK.S3` — ONLY if hidden behind `IStorageProvider`

**Zero tolerance** (fail immediately if found unabstracted):
- Any direct Azure Service Bus, Azure Key Vault, or Azure AD SDK calls not behind an interface
- Any AWS-specific SDK calls not behind an interface
- Any GCP-specific SDK calls not behind an interface

## Step 6 — Environment Variable Completeness Check

Document every environment variable the container requires at runtime.
Produce a `.env.example` file listing all required variables with placeholder values and descriptions.

Required variables must include at minimum:
```
NOCTURNE_DB_CONNECTION=          # PostgreSQL connection string
NOCTURNE_REDIS_CONNECTION=       # Redis connection string
NOCTURNE_STORAGE_PROVIDER=       # local | s3 | azure-blob | sftp
NOCTURNE_STORAGE_CONNECTION=     # Provider-specific connection string
NOCTURNE_LICENSE_SERVER_URL=     # URL of Nocturne Systems license validation server
NOCTURNE_ENCRYPTION_KEY=         # AES-256 key for data encryption at rest
NOCTURNE_JWT_SECRET=             # JWT signing secret
```

## Step 7 — Sign-Off Checklist

```
DOCKER BUILD VALIDATION REPORT
-------------------------------
Build Result:           PASS / FAIL
Startup Result:         PASS / FAIL
Health Check Result:    PASS / FAIL
R Runtime Verified:     PASS / FAIL
Cloud Lock-in Found:    YES (blocker) / NO (clean)
Onboarding Wizard:      PASS / FAIL
License Scheduler Init: PASS / FAIL
.env.example Generated: YES / NO
Image Size:             [X MB]
Build Time:             [X seconds]
Overall:                READY TO SHIP / BLOCKED — [reason]
```
