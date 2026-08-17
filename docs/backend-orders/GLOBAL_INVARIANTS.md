# Global Backend Invariants

Every materialized order must preserve these invariants and repeat the relevant subset in its acceptance criteria.

## Tenant And Data

1. Every tenant-owned row has one non-empty, immutable tenant owner.
2. Tenant ownership is derived from trusted context or deterministic backfill, never caller input or first-row inference.
3. Tenant A cannot read, mutate, enumerate, infer, or reference Tenant B data through EF, raw SQL, background jobs, caches, files, webhooks, or public verification.
4. Application predicates and PostgreSQL constraints/RLS are defense in depth; one does not excuse absence of the other.
5. Runtime database roles are not superusers, do not have `BYPASSRLS`, and are not table owners where owner bypass would weaken policy.
6. GUC/RLS behavior must be correct with the deployed connection pooler mode.
7. No migration silently invents ownership for ambiguous rows. Ambiguity produces a report and blocks cutover.

## Identity And Authorization

1. Actor identity comes from the authenticated principal.
2. Student, employee, faculty, department, HOD, dean, requester, owner, and assignee relationships are explicit and effective-dated where authority changes over time.
3. RBAC grants broad capability only; ownership, relationship, tenant, and workflow state remain mandatory.
4. Role removal and other high-impact revocations have a defined freshness bound.
5. Anonymous callbacks are cryptographically authenticated, replay-resistant, tenant-bound, rate-controlled, and idempotent.
6. Break-glass access is explicit, time-bounded, strongly authenticated, and audited.

## Transactions And Concurrency

1. A successful mutation is durably committed before success is returned.
2. Multi-row business invariants are protected in one transaction.
3. Uniqueness, balance, capacity, occupancy, and append-only guarantees use database constraints/locks where races exist.
4. Retried commands and background jobs do not duplicate side effects.
5. Multi-replica workers atomically claim work and recover abandoned claims.
6. Posted finance, finalized grades, published documents, and approved floor plans are changed by reversal/supersession, not silent rewrite.

## Finance

1. Journal debits equal credits in deterministic decimal/currency semantics.
2. Posted journals and their lines are append-only and hash/signature coverage includes financially meaningful content.
3. Reversals reference originals.
4. Payment duplicates cannot double-credit, and allocations reconcile to invoice/demand balances.
5. Closed periods reject postings unless an authorized reopen workflow completes.
6. Financial reports derive from the canonical ledger and state their period, currency, standard, and sandbox treatment.

## Academics

1. Registration validates the authoritative curriculum, offering, window, holds, prerequisites, conflicts, and capacity in one transaction.
2. Faculty can mutate attendance/marks only for explicitly assigned sections.
3. Published grades have immutable before/after audit and controlled reopen/reversal.
4. OBE calculations are reproducible from versioned mappings, assessment evidence, rubric/threshold policy, and canonical grades.
5. Timetable and examinations use stable Space identity and the shared occupancy guarantee.

## Sensitive Domains

1. Clinical, counseling, grievance, payroll, grade-change, confidential ticket, and document-vault data use least privilege and sensitive-read audit.
2. Protected content is not logged, placed in insecure notification bodies, or sent to external AI providers without approved policy.
3. Clinical, counseling, welfare, and generic service-desk records remain separate authorization domains even when sharing primitives.
4. Retention, legal hold, export, and deletion are policy-driven and auditable.

## Documents And Providers

1. Stored bytes, metadata, hash/signature, template version, issuer, subject, tenant, and verification state remain consistent.
2. Public verification reveals only the minimum result and is abuse-controlled.
3. Provider-specific code remains behind an adapter selected by tenant/deployment configuration.
4. Unsupported provider combinations fail validation before deployment; they never silently fall back to a different vendor.
5. Secret values are referenced, resolved through approved secret providers, redacted, and never embedded in tenant manifests.

## Operability

1. Liveness means the process can run; readiness means required dependencies and migration state permit safe traffic.
2. Retry is used only for transient and idempotent operations.
3. Logs, traces, and metrics share correlation and do not expose secrets or protected content.
4. Backup success requires a restorable artifact, not just a file checksum.
5. Every supported profile has a tested upgrade, smoke, rollback, and restore path.
