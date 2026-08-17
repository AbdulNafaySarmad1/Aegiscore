# PROMPT 01 — PLATFORM, TENANCY, PROVIDERS, DEPLOYMENT

Act as the senior architect for AegisCore platform foundations. Read `prompts/00_MASTER_BACKEND_ARCHITECT.md` first and obey it fully.

## Goal

Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/01-platform/` that make AegisCore safely repeatable across many universities from one canonical release.

Do not implement code yourself.

## Verify current source

Audit and verify:

- tenant resolution from JWT/request to EF/Postgres;
- `ITenantScoped`, global query filters, GUC/RLS/session interceptor behavior;
- all core SIS entities lacking safe tenant scoping;
- `IgnoreQueryFilters` and direct/raw SQL bypasses;
- tenant configuration services and environment fallback;
- branding/configuration coupling;
- secrets vs ordinary config;
- license middleware/service/heartbeat;
- Redis usage: caching, lockouts, pub/sub, distributed locks, revocation/state;
- Docker/Compose manifests, Kubernetes manifests, runtime assumptions;
- storage provider abstraction;
- backup/restore and migration workflow;
- background workers and multi-replica duplicate execution;
- health/readiness endpoints;
- deployment docs/scripts.

## Target architecture

Design orders for:

1. **Tenant invariant hardening** — every tenant-owned row/query/write must have an unambiguous tenant. No `Guid.Empty` shortcuts.
2. **Tenant manifest** — institution identity, domains, modules, academic structure references, locale/timezone, provider selections, integration settings, branding/template references. Secrets are references, not embedded secrets.
3. **Configuration resolver** — precedence must be explicit, testable and tenant-safe.
4. **Provider registry/adapters** — only real substitution boundaries.
5. **Cache/distributed state portability** — Redis and Valkey should work where protocol/feature compatibility is valid. Business code must not depend on product branding.
6. **OCI runtime portability** — application images should run under Docker or Podman where operationally appropriate. Avoid Docker-socket assumptions in application code.
7. **Storage portability** — retain/use provider-neutral storage across local/S3/R2/Azure/GCS/MinIO when supported.
8. **Deployment factory** — validate profile, resolve secrets, provision/configure dependencies, migrate, bootstrap, smoke test, record release, rollback.
9. **License hot path** — remove avoidable per-request DB work while preserving fail-closed/lease semantics and operational escape paths.
10. **Multi-replica workers** — idempotency/distributed ownership/leader-lock strategy so scheduled workers do not duplicate work.
11. **Backup/restore** — explicit RPO/RTO inputs, restore drill, tenant deployment metadata, migration compatibility.
12. **Health/readiness** — distinguish process liveness from readiness for DB/cache/storage/provider dependencies.

## Ten deployment matrix

Orders must prove a same-release matrix can support examples like:

- Redis + Docker + AWS + Entra + S3;
- Valkey + Podman + on-prem + SAML + MinIO;
- Redis/Valkey + Kubernetes + Azure + Entra + Blob;
- GCP + Google identity + GCS + SMTP.

Do not promise unsupported combinations; classify each as `SUPPORTED_TARGET`, `NEEDS_ADAPTER`, `NOT_JUSTIFIED`.

## Order-writing rules

Each order must use the atomic order format from the master prompt and include migration/backfill for tenant changes, negative tenant-isolation tests, provider contract tests, failure behavior when a selected provider is unavailable, and rollback.

Do not solve client-specific DNS/IdP credentials in code. Mark those `CLIENT_INFORMATION_REQUIRED`.

## Final chat response

Return only:

`ORDERS_CREATED`, `P0`, `P1`, `PROVIDER_MATRIX`, `TENANT_RISKS`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.