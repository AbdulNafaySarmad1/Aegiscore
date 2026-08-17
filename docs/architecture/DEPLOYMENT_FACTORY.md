# AegisCore DEPLOYMENT_FACTORY.md

Design of an AegisCore Deployment Factory for repeatable, zero-fork tenant deployment. Generated 2026-08-12 on branch `agent/prod-hardening`.
Design ONLY. No implementation.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

---

## 1. Current deployment state (audited)

| Asset | Status | Evidence |
|---|---|---|
| Dockerfile (single API image) | IMPLEMENTED | `Dockerfile` (builds Release) |
| docker-compose (local / cloud / hybrid / uni / local-demo) | IMPLEMENTED | `docker-compose*.yml` |
| k8s manifests (api deployment/HPA/PDB/network-policy, portal, postgres, redis, ingress, configmap) | IMPLEMENTED | `k8s/` |
| GitHub Actions CI/CD (ci, deploy, release, deploy-cloud, prod-hardening-ci) | IMPLEMENTED | `.github/workflows/*.yml` |
| Deploy/rollback/backup/warm-cache scripts | IMPLEMENTED | `deploy/scripts/*.sh` |
| Caddy / pgbouncer / prometheus config | IMPLEMENTED | `deploy/config/*` |
| Onboarding wizard (license, DB/Redis/SMTP test, DNS instructions, GoLive, .env.generated) | IMPLEMENTED | `Infrastructure/Onboarding/OnboardingService.cs`, `WizardController.cs` |
| Per-tenant provisioning automation | NOT_FOUND | — |
| Deployment record / version tracking | PARTIAL (git tags?) — `DOCUMENTED_NOT_VERIFIED` | `release.yml` exists |
| Tenant registry | NOT_FOUND | — |

**Verdict:** CI/CD builds and ships a working single-instance deployment. What's missing is the **tenant lifecycle layer**: validate → provision infra → inject manifest+secrets → migrate → bootstrap → health-check → record. This is what turns "deploy the software" into "onboard a university."

---

## 2. Target: Deployment Factory

Conceptual interface:

```
deploy-tenant <tenant-profile>
```

Where `<tenant-profile>` = manifest (`TENANT_CONFIGURATION.md`) + asset bundle (branding, templates) + secrets references.

### Orchestration pipeline (idempotent, step-checked)

```
1.  Validate tenant manifest (schema, required refs, uniqueness of slug/domain)
2.  Resolve secrets (Infisical/KeyVault) → inject as env / mounted files, never into manifest
3.  Provision infrastructure
       - DNS/subdomain (erp.{uni-domain}) + TLS (Caddy/Cloudflare)
       - Database (provision DB user + schema; NOBYPASSRLS app role already scripted:
         scripts/postgres-init, 010-harden-app-role.sh)
       - Redis instance/db
       - Object storage bucket/prefix
4.  Apply tenant manifest → config-center / TenantConfigService (runtime config)
5.  Deploy identical immutable images (api, portal) with tenant env
6.  Run database migrations (Database:AutoMigrate or migrate job)
7.  Bootstrap data (departments/programs/batches from manifest academic_structure;
       GoldenDemoSeeder-style seed for reference data only, NOT demo data)
8.  Identity configuration (register OIDC/SAML provider rows from manifest)
9.  Branding + document templates install (from asset bundle)
10. License registration (validate key → issue lease; record)
11. Health checks (/health/ready, /health/detail, /health/deep) + smoke tests
12. Monitoring wiring (prometheus target, alerts)
13. Backup schedule wiring
14. Write deployment record (tenant, release version, image digests, config hash, timestamps)
15. Rollback snapshot (pre-migration pg_dump or image pin) recorded
16. LIVE
```

### What uses identical immutable images (all tenants)
- `aegiscore-api`, `aegiscore-portal` — the SAME images for every tenant. `CONFIRMED` feasible: images are env/config-driven, not code-forked. (This is already true: `Dockerfile` + compose/env are the only per-deploy differences.)

### What requires tenant-specific infrastructure
- Database (separate PG database/user — already per-deploy via `AEGISCORE_DB_CONNECTION`)
- Redis (shared instance w/ per-tenant prefix, or per-tenant instance — decide by profile)
- Object storage bucket/prefix
- DNS/subdomain + TLS cert
- Secrets namespaces
- (Optional, profile-gated) Elasticsearch, MinIO, Cloudflare tunnel (uni profile)

### Idempotency design
- Every step keyed by tenant slug + release version + a run token; step result recorded in a deployment-state store.
- DB provision: `CREATE DATABASE IF NOT EXISTS`-style guard via a provisioning script with owner checks.
- Migrations: EF migrations are versioned and re-runnable (idempotent).
- Bootstrap: upsert reference data by code; do NOT re-seed demo data into production.
- Config: config-center SetConfig is upsert.
- Re-run is safe: steps detect "already applied" via recorded state.

### Rollback strategy
- **Before migrate:** record image digest + DB dump (fast).
- **On failure at any step:** halt, surface step + state, rollback to recorded image pin + DB restore (`deploy/scripts/rollback.sh` <30s image re-pin; `BackupService.RestoreFromBackupAsync` for DB — note PITR unsupported, full restore only).
- **After live:** blue/green or re-pin previous image; restore DB from pre-release dump.
- Deployment record enables "what changed, when, by which release" auditability.

---

## 3. Classification: what's shared vs tenant-specific

| Resource | Shared | Tenant-specific | Notes |
|---|---|---|---|
| API image | YES | — | immutable release |
| Portal image | YES | — | immutable release |
| DB | — | YES (per-tenant DB) | strongest isolation; keeps tenant-filter gaps non-fatal at deployment level |
| Redis | profile | YES | per-tenant prefix at minimum |
| Object storage | — | YES (bucket/prefix) | |
| DNS/TLS | — | YES | Caddy/Cloudflare |
| Secrets | — | YES | namespaces |
| Config-center | YES (runtime) | YES (scoped rows) | manifest → scoped rows |
| License | YES (authority) | YES (lease) | |

---

## 4. Relationship to tenant isolation work

Deployment-level isolation (separate DBs) **mitigates** the tenant-scoping gaps (~185 entities, `DATA_MODEL.md` §2) at the infrastructure layer. It does NOT remove the need to fix them:
- Same-tenant data correctness relies on app filters.
- If a future model moves to shared DB, the filter gaps become critical.
- Recommendation: keep per-tenant DBs for the pilot generation (defense-in-depth), AND fix the filter gaps in parallel (P0/P1 security) so both models are safe.

---

## 5. What already exists vs what must be built

| Capability | Exists today | Build for factory |
|---|---|---|
| Image build/release | YES (Dockerfile + Actions) | versioned release tagging + SBOM |
| Infra provisioning | partial (compose/k8s templates, scripts) | parameterized per-tenant provisioning |
| Migration | YES (EF, `Database:AutoMigrate`) | run as deploy step + pre/post checks |
| Bootstrap | partial (seeders) | manifest-driven reference-data bootstrap |
| Health checks | YES (`/health*`) | smoke-test suite per module |
| DNS/TLS | partial (Caddy/Cloudflare instructions) | automation (scriptable) |
| Secrets | YES (ISecretService, Infisical) | per-tenant namespaces |
| License | YES (validate/heartbeat) | registration + lease in factory |
| Deployment record | NO | new: deployment-state store |
| Tenant registry | NO | new: registry of tenants + versions |
| Rollback | partial (script re-pin + full restore) | integrated pre-deploy snapshot |
| Monitoring wiring | partial (prometheus config) | automated per-tenant targets |
| Backup | YES (BackupService + backup.sh) | wire schedule per tenant |

---

## 6. Sequencing

1. **P0:** Deployment record + rollback snapshot (without this, factories are blind).
2. **P0:** Tenant manifest schema + validation (`TENANT_CONFIGURATION.md`).
3. **P1:** Parameterized provisioning script for the primary topology (compose or k8s) driven by manifest.
4. **P1:** Migrations + bootstrap as explicit idempotent factory steps.
5. **P2:** Secrets namespace automation; license lease registration; smoke-test suite.
6. **P2:** DNS/TLS automation (Caddy already present; Cloudflare API optional).
7. **P3:** Tenant registry UI, monitoring/backup automation, multi-topology (onprem/aws/azure).
