# Independent Backend QA Manifest

This manifest implements `prompts/11_QA_BACKEND_RELEASE_GATE.md`. QA is independent, does not trust worker reports, does not modify production code, and attempts to disprove the release candidate.

## Preconditions

- QA has read `prompts/00_MASTER_BACKEND_ARCHITECT.md`, `prompts/10_DEEPSEEK_V4_PRO_EXECUTION_CONTRACT.md`, every implemented order and worker completion report, and current Graphify/architecture/performance/security intelligence.
- Candidate commit and immutable image digests are recorded.
- All implemented order files and worker completion reports are present.
- `ORDER_INDEX.md` contains no implemented order still marked `READY` or `PLANNED_REVERIFY_AFTER_DEPENDENCIES`.
- Supported deployment profiles and required client inputs are frozen for the candidate.
- QA uses only authorized local/test infrastructure and synthetic protected data.

## Evidence Outputs

- `docs/backend-orders/qa/QA_RUN_<UTC timestamp>.md`
- `docs/backend-orders/qa/QA_FINDINGS_<UTC timestamp>.md`
- Narrow correction requests under `docs/backend-orders/corrections/`

Each run records exact commands, environment/profile identifiers, fixtures, timestamps, pass/fail evidence, pre-existing failures, and candidate regressions.

## Gate Matrix

| Gate | Required execution | Release blocker |
|---|---|---|
| Source/build | Clean restore, root Release build, all tests, portal typecheck/build, stale-tree exclusion. | Any required project cannot build/test. |
| Migration | Fresh PostgreSQL 17 apply; supported-state upgrade; backfill verification; pending-model check; required rollback. | Data loss, model drift, or unreproducible schema. |
| Tenancy | Tenant A/B cross-tenant read/write tests across every domain, raw SQL, cache, files, jobs, webhooks, and public verification. | Any unauthorized cross-tenant success or ambiguous owner. |
| Identity/authz | Local and configured test IdPs, malformed tokens, refresh replay, revoked role, forged actor, object ownership, relationship and workflow state. | Auth bypass, forged actor, stale critical privilege beyond bound. |
| Academics | Registration prerequisites/holds/capacity races, timetable conflicts, attendance correction, OBE reproduction, grade approval/finalization, exams and transcript. | Double allocation, unauthorized grade, incorrect final result, unverifiable transcript. |
| Finance | Balanced journals, closed periods, duplicate payment/webhook, allocation, reversal/refund, approval limits, immutable ledger and report reconciliation. | Imbalance, duplicate credit, silent posted mutation, cross-tenant finance access. |
| Library | Catalogue/copy identity, concurrent checkout, return, renewal, hold order, overdue/fines, history, ownership. | Same copy issued twice or cross-tenant/private history access. |
| Health/privacy | Clinical/counseling/welfare separation, role matrix, sensitive reads, attachments, audit, protected logging. | Confidentiality breach or protected content in logs. |
| HR/service desk | Employee/leave/workload/payroll boundaries, requester/queue/assignment, comments/attachments, confidential categories, SLA/escalation. | Salary/confidential case exposure or unauthorized decision. |
| Facilities | Space identity, office/workspace assignment and visibility, occupancy exclusion under concurrency, timetable/exam/meeting/maintenance integration, floor-plan review/publish/rollback. | Double booking, private presence exposure, or extraction silently publishes geometry. |
| Providers | Redis and Valkey; Docker and Podman; at least two claimed storage implementations or test doubles; IdP, mail, notification, payment adapters/test doubles and outage behavior. | Claimed supported profile cannot boot or preserve semantics. |
| Operability | Liveness/readiness, structured logs, correlation IDs, traces/metrics, dependency failures, bounded jobs, multi-replica ownership, graceful shutdown, migration coordination. | Unsafe traffic readiness or duplicate irreversible job effect. |
| Recovery | Backup creation, independent restore, integrity/application smoke, measured RTO/RPO, rollback and release metadata. | Unrestorable backup or failed supported rollback. |
| Performance | Existing baseline method plus representative seeded login, dashboards, registration, grading, OBE, finance, documents, facilities and mutations. | Order threshold breach without accepted exception/rollback. |
| Supply chain | Lockfile/package advisory checks, SBOM, image scan, provenance and secret scan. | Unaccepted critical/high exploitable issue or leaked secret. |

## Failure Injection

Exercise, where safe:

- PostgreSQL unavailable, slow, and transaction conflict;
- Redis/Valkey unavailable and restarted;
- object storage unavailable, partial upload, and stale download reference;
- duplicate and concurrent HTTP requests;
- duplicate webhooks with same and altered payloads;
- worker crash after claim and before/after side effect;
- process shutdown during queued work;
- IdP, mail, payment, calendar, and AI provider timeout/error;
- backup interruption and restore to a clean instance.

Verify the exact fail-open, fail-closed, degraded, retry, and recovery behavior specified by each order.

## Required Fixtures

- Tenant A and Tenant B with overlapping human-readable codes to expose missing tenant-qualified uniqueness.
- Student, faculty, HOD, registrar, exam controller, finance maker/checker, HR, clinician, counselor, librarian, service-desk agent, facilities engineer, tenant admin, and revoked user.
- Representative academic history, OBE mappings/evidence, fee ledger, circulation, clinical/counseling data, employee records, tickets, Spaces, and floor plans.
- High-cardinality datasets sized from approved pilot volumes, not empty demo tables.
- Redis and Valkey profiles plus at least Docker and Podman smoke paths.

## Security And Privacy Rules

- Do not place raw clinical, counseling, grievance, payroll, credential, or payment content in QA output.
- Use synthetic values and redact tokens/secrets.
- Keep security testing inside authorized local/test scope.
- Public verification tests assert minimum disclosure and rate controls.

## Finding Severity

- `RELEASE_BLOCKER`: tenant escape, auth bypass, financial integrity failure, protected-data breach, data-loss migration, required DB-concurrency failure, duplicate payment, unrestorable backup, or supported-profile boot failure.
- `HIGH`: exploitable or operationally severe issue that must be corrected before release unless explicitly accepted by the accountable owner.
- `MEDIUM`, `LOW`, `OBSERVATION`: use definitions in the QA run and include evidence and affected orders.

## Correction Flow

1. QA records the finding and reproduction.
2. QA does not redesign or patch production code.
3. The architect creates a narrow correction order under `corrections/` with a refreshed base commit and symbols.
4. DeepSeek executes the correction order.
5. QA re-runs the failed gate plus regression gates.

## Final Verdict Format

Return exactly `BACKEND_RELEASE_APPROVED` or `BACKEND_RELEASE_REJECTED`, followed by the field block required in `prompts/11_QA_BACKEND_RELEASE_GATE.md`.
