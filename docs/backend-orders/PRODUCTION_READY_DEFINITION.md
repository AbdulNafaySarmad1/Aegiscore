# Backend Production-Ready Definition

AegisCore is production-ready only when all release-blocking gates below pass for the candidate commit and approved deployment profiles.

## Required Gates

| Gate | Required evidence |
|---|---|
| Source baseline | Clean restore/build/test from the canonical root solution; no stale duplicate tree included. |
| Migrations | Fresh PostgreSQL 17 apply, supported-state upgrade, deterministic backfill, pending-model check, and required rollback/compatibility rehearsal. |
| Tenancy | Tenant A/Tenant B negative reads and writes across every domain, plus raw SQL, cache, file, worker, webhook, and public routes. |
| Authorization | Role, ownership, relationship, revoked privilege, forged actor, workflow state, and sensitive-read tests. |
| Finance | Balanced posting, closed periods, immutable ledger, duplicate webhook, allocation, reversal/refund, approval limit, and report reconciliation tests. |
| Academics | Registration race, timetable/Space conflict, attendance ownership, OBE reproduction, grade finalization, exam conflict, and transcript verification tests. |
| Sensitive data | Clinical/counseling/service-desk separation, attachment protection, protected logging checks, and audit evidence. |
| Library | Copy identity, concurrent checkout, holds, renewal, return, fine policy, history, and tenant tests. |
| Facilities | Space identity, range exclusion, meeting/exam/timetable/maintenance integration, floor-plan approval, and rollback tests. |
| Providers | Exercised Redis and Valkey; Docker and Podman; supported storage, identity, mail, notification, and payment adapters or test doubles. |
| Operability | Correct liveness/readiness, dependency outage behavior, bounded jobs, multi-replica ownership, graceful shutdown, and migration coordination. |
| Recovery | Encrypted backup where configured, clean restore drill, measured RTO/RPO, release metadata, and rollback. |
| Performance | Representative seeded data and concurrency; order-specific p95/p99/error/CPU/memory/query targets; no unexplained regression. |
| Supply chain | Reproducible images, pinned dependencies/lockfiles, provenance, SBOM, vulnerability scan, and no unresolved release-blocking advisory. |

## Automatic Release Blockers

- Tenant escape or tenant ownership ambiguity.
- Authentication/authorization bypass or forged actor attribution.
- Clinical/counseling confidentiality breach.
- Financial imbalance, silent posted-record mutation, or duplicate payment effect.
- Data-loss migration or non-reproducible schema.
- Registration/room/copy double allocation despite a required database guarantee.
- Unrestorable backup or unsupported-profile boot failure.
- SAML assertion accepted without complete cryptographic/protocol validation.
- Required test, migration, security, or rollback evidence missing.

## Completion Rule

An endpoint, entity, controller, generated PDF, or green project build is not completion. Completion requires the end-to-end workflow, invariants, negative tests, operability, and rollback evidence specified by its order and `qa/QA_MANIFEST.md`.
