# PROMPT 09 — INTEGRATIONS, OPERABILITY, RELEASE

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as principal integration/SRE/release architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/09-operability/`. Do not implement production code.

## Goal

Turn AegisCore from a working application into a backend that can be operated, observed, upgraded, recovered and deployed repeatedly across universities.

## Integration boundaries to audit

Verify current adapters/coupling for:

- object storage;
- cache/distributed state;
- identity providers;
- SMTP/Graph mail;
- SMS/WhatsApp/Teams;
- payment gateways;
- Power BI/external BI;
- Microsoft Graph/Teams;
- Google/calendar/resource integration;
- external data imports;
- AI providers;
- search/Elasticsearch;
- webhooks;
- licensing authority/heartbeat.

Generate orders to make real substitution points provider-driven and tenant-configurable. Domain logic emits intent/contracts; provider adapters perform vendor-specific I/O.

## Operability requirements

Audit and plan orders for:

- OpenTelemetry or equivalent traces/metrics/log correlation;
- structured logging and secret/PII/medical-data redaction;
- liveness vs readiness;
- dependency health;
- timeouts/cancellation;
- retry only where safe/idempotent;
- circuit breaking where justified;
- connection-pool sizing/measurement;
- async/background workloads;
- bounded queues/backpressure;
- multi-replica worker dedup/ownership;
- graceful shutdown;
- migration coordination;
- backup/restore drills;
- RPO/RTO configuration;
- audit/log retention;
- release metadata;
- rollback;
- smoke tests;
- disaster-recovery runbooks.

## Performance regression gates

Read the existing performance baseline before authoring optimization orders. Re-verify high-risk paths including login/Argon2/GeoIP, transcript/PDF/PQC generation, OBE aggregation, attendance/grading query patterns, finance reports and license middleware.

Do not optimize from taste. Every performance order must identify evidence, target metric, test method and rollback condition.

## Deployment/runtime profiles

Target OCI-compatible deployments that can support Docker and Podman where the same image semantics apply. Kubernetes is optional orchestration, not a reason to fork app code.

Provider/deployment profiles must distinguish:

- required dependency;
- optional dependency;
- degraded-mode behavior;
- startup/readiness behavior;
- secret requirements;
- backup implications.

## CI/CD

Generate orders for a deterministic release gate covering:

- restore/build;
- unit/integration tests;
- migrations validation;
- tenant-isolation/security tests;
- API contract tests;
- container image build;
- image provenance/SBOM/vulnerability scan where feasible;
- Docker/Podman smoke compatibility;
- deployment-profile validation;
- database migration + rollback rehearsal;
- post-deploy smoke/health checks.

## QA handoff

Generate `docs/backend-orders/qa/QA_MANIFEST.md` defining the independent QA model's black-box and source-invariant test matrix. It must reference `prompts/11_QA_BACKEND_RELEASE_GATE.md`.

## Release definition

A build is not production-ready merely because it compiles. Define explicit release criteria for correctness, tenant safety, authz, observability, backup/restore, provider substitution, concurrency, migration safety, performance and rollback.

## Final chat response

Return only:

`ORDERS_CREATED`, `INTEGRATION_GAPS`, `OBSERVABILITY_GAPS`, `RELEASE_GAPS`, `QA_MANIFEST_CREATED`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.