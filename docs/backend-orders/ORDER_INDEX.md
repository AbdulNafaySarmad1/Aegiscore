# Backend Order Index

## Scheduling Authority

This file and `DEPENDENCY_DAG.md` are the single scheduling truth for the backend transformation. An order may be executed only when its status is `READY`, its order file exists, and its `EXPECTED_BASE_COMMIT` matches current source or has been explicitly refreshed.

- Repository: `/home/abdul-nafay-sarmad/Music/Aegiscore/Aegiscore-demo-integration`
- Planning branch: `agent/prod-hardening`
- Verified planning commit: `d53b4b564661f0b990c59674386287c50364848f`
- Prompt pack consumed: `00` through `11`
- Intelligence consumed: `graphify-out`, all current `docs/architecture`, `docs/performance`, `docs/security`, backend reports, and audits
- Canonical source: root `AegisCore.*` projects and `AegisCore.Portal`
- Stale source excluded: `aegiscore-backend/` and `aegiscore-frontend/`

## Current Baseline

- Production projects compile at the verified commit.
- `dotnet build AegisCore.slnx -c Release --no-restore` fails with 10 test-project compile errors caused by deleted analytics symbols still referenced by `AnalyticsIntelligenceTests`, `StatisticalAnalyticsTests`, and `GoldenDemoSeederTests`.
- `dotnet ef migrations has-pending-model-changes` reports model drift.
- The analytics/demo/identity lineage and migration ownership decisions are not safe for a worker to improvise.
- The first executable order is therefore a standalone tenant inventory/regression guard that does not alter production code or depend on the broken test project.

## Status Definitions

| Status | Meaning |
|---|---|
| `READY` | Pin-precise order exists and can be executed at its expected base commit. |
| `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | Task is in the DAG, but exact symbols and base commit must be refreshed after dependencies land. |
| `BLOCKED_CLIENT_INFO` | Institutional policy, credentials, mappings, templates, or operational targets are required. |
| `BLOCKED_ARCH_DECISION` | A human-approved source-of-truth or migration decision is required before materialization. |

## Backlog Summary

| Metric | Count |
|---|---:|
| Total backlog items | 90 |
| Ready orders | 1 |
| Planned reverify after dependencies | 79 |
| Blocked client information | 5 |
| Blocked architecture decision | 5 |
| P0 | 49 |
| P1 | 41 |

## Complete Backlog

| ID | Priority | Status | Dependencies | Deliverable |
|---|---|---|---|---|
| MST-001 | P0 | `BLOCKED_ARCH_DECISION` | None | Reconcile analytics, identity-link, admission-ownership, and demo-seeder lineage without deleting tests or suppressing model checks. |
| MST-002 | P0 | `BLOCKED_ARCH_DECISION` | None | Approve canonical legacy/v2 table ownership and a data-preserving EF snapshot/migration reconciliation. |
| MST-003 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001, MST-002 | Define and implement explicit transaction/unit-of-work semantics for generic repository-backed writes. |
| PLAT-001 | P0 | `READY` | None | Add a standalone tenant-enforcement inventory and regression gate. |
| PLAT-002 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001 | Remove per-request license database reads with bounded, fail-closed cache/lease semantics. |
| PLAT-003 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-001, MST-002 | Add tenant ownership, deterministic backfill, tenant-qualified keys, and negative tests to core identity/config/document roots. |
| PLAT-004 | P0 | `BLOCKED_ARCH_DECISION` | PLAT-001 | Approve the PostgreSQL RLS/GUC strategy compatible with PgBouncer transaction pooling and non-owner runtime roles. |
| PLAT-005 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-003, PLAT-004 | Make tenant configuration resolution, cache keys, invalidation, and fallback precedence tenant-safe. |
| PLAT-006 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-005 | Add a versioned tenant manifest schema and validation service with secret references only. |
| PLAT-007 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-006 | Normalize secret-provider references, namespaces, rotation, redaction, and readiness checks. |
| PLAT-008 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-005 | Introduce a protocol-neutral distributed-state boundary verified against Redis and Valkey. |
| PLAT-009 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-004, PLAT-008 | Add multi-replica-safe job ownership, atomic claims, idempotency, and graceful recovery. |
| PLAT-010 | P1 | `BLOCKED_CLIENT_INFO` | PLAT-006, PLAT-007, PLAT-009 | Build the deployment factory and registry against approved tenant profiles, DNS, RPO/RTO, and secret namespaces. |
| SEC-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001 | Fail closed and then complete SAML signature, audience, destination, replay, tenant, and JWT issuance checks. |
| SEC-002 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001, PLAT-008 | Hash stored refresh tokens and make family rotation/reuse handling atomic across replicas. |
| SEC-003 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-003 | Introduce the unified authorization facade over RBAC, ABAC, relationship, ownership, and workflow checks. |
| SEC-004 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001, PLAT-003 | Make User-to-Student, User-to-Employee/Faculty, role expiry, and department authority authoritative. |
| SEC-005 | P1 | `BLOCKED_CLIENT_INFO` | SEC-004, PLAT-006 | Implement and activate generic OIDC/Entra/Google/Okta adapters using approved tenant claims and credentials. |
| SEC-006 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | SEC-004, SEC-005 | Implement tenant-scoped SCIM lifecycle, disable propagation, conflict review, and account linking. |
| SEC-007 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | SEC-003, SEC-004, MST-003 | Remove caller-supplied actor identities from sensitive mutations and audit authenticated actors. |
| SEC-008 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | SEC-003, PLAT-003 | Apply ownership and relationship authorization to ID-addressed resources and sensitive read paths. |
| SEC-009 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | DOC-001 | Centralize upload policy, magic validation, parser isolation, malware hooks, quotas, and audit. |
| SEC-010 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-008 | Add targeted origin controls for auth, verification, uploads, webhooks, imports, and expensive generation. |
| ACAD-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | SEC-004, PLAT-003 | Make linked student identity the only self-service ownership source and remove shared-UUID assumptions. |
| ACAD-002 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-001, MST-003 | Implement accepted-applicant conversion to user/student/program/batch state with idempotent provisioning. |
| ACAD-003 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-003, MST-003 | Select one versioned curriculum/prerequisite catalogue and retire parallel placeholder truth stores. |
| ACAD-004 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-003, HR-001 | Implement canonical offerings, classes, sections, faculty assignments, and effective-dated capacity. |
| ACAD-005 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-001, ACAD-003, ACAD-004, FIN-003 | Replace demo registration with transactional windows, holds, prerequisites, clashes, capacity, waitlist, and overrides. |
| ACAD-006 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-004, FAC-003 | Move timetable scheduling to canonical Space occupancy and enforce cohort/faculty conflicts. |
| ACAD-007 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-001, ACAD-004, SEC-003 | Unify attendance paths, enforce taught-section ownership, unique marks, corrections, warnings, and SQL aggregates. |
| ACAD-008 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-003, ACAD-004, MST-003 | Select CourseMark/CourseGrade as canonical, support attempts/semesters, and remove conflicting grade stores. |
| ACAD-009 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-008, HR-003, SEC-003 | Separate grade-change and course-finalization workflows with immutable before/after audit and authority checks. |
| ACAD-010 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-003, ACAD-008 | Consolidate normalized CLO/PLO mappings, assessment evidence, rubrics, versioning, and explainable attainment. |
| ACAD-011 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-010, OPS-001 | Replace OBE N+1/in-memory aggregation with measured SQL/batch queries and supporting indexes. |
| ACAD-012 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-004, FAC-003 | Implement exam definitions, schedules, capacity, student conflicts, seating, and invigilator assignment. |
| ACAD-013 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-009, ACAD-012, DOC-004 | Add eligibility, exam attendance, signed admit cards, result freeze, publish, reopen, recheck, and appeal. |
| ACAD-014 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | ACAD-008, ACAD-009, DOC-004, LIB-004 | Consolidate transcript, degree audit, transfer, clearance, graduation eligibility, and verifiable awards. |
| FIN-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001, SEC-003 | Restrict manual payments, derive actor identity, and post balanced ledger entries atomically. |
| FIN-002 | P0 | `BLOCKED_ARCH_DECISION` | MST-002 | Approve legacy finance to finance-v2 ownership, migration, compatibility, and deprecation boundaries. |
| FIN-003 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-002, PLAT-003, PLAT-004 | Make COA, journals, fee accounts, fiscal periods, and keys tenant-scoped with database guarantees. |
| FIN-004 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-003, SEC-003 | Implement fiscal period close/reopen with maker-checker control and posting rejection. |
| FIN-005 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-003, FIN-004 | Cover journal lines in append-only/hash guarantees and prevent post-posting line insertion or mutation. |
| FIN-006 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-001, FIN-003 | Add explicit payment allocation, receipts, reversals, refunds, chargebacks, and no-double-credit invariants. |
| FIN-007 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-006, PLAT-008 | Make webhook signature, replay window, payload binding, durable idempotency, and outage behavior correct. |
| FIN-008 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-003, FIN-004 | Implement versioned budgets, approvals, encumbrance, transfer, release, and budget-period reporting. |
| FIN-009 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-003, FIN-008 | Complete purchase request, PO, receipt, invoice, AP settlement, remittance, and separation-of-duties links. |
| FIN-010 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FIN-003, HR-005 | Integrate approved HR payroll with finance posting, encrypted bank instructions, payment files, and reconciliation. |
| FIN-011 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | DOC-004, OPS-003, FIN-003 | Move finance reports, challans, receipts, XBRL, and PQC artifacts to typed asynchronous generation. |
| HLTH-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-002, PLAT-003, MST-003 | Reconcile health/counseling entity, migration, snapshot, and persistence drift without data loss. |
| HLTH-002 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HLTH-001, SEC-003, SEC-008 | Enforce clinical, counseling, welfare, and service-desk confidentiality with sensitive-read audit. |
| HLTH-003 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HLTH-001, HLTH-002, ACAD-001 | Implement patient profile, emergency contacts, allergies, conditions, medications, and protected attachments. |
| HLTH-004 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HLTH-003, HR-001 | Implement appointments, walk-ins, triage, vitals, encounters, clinical notes, and practitioner ownership. |
| HLTH-005 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HLTH-004 | Add diagnoses, prescriptions, referrals, justified lab/vaccine workflows, and safe notifications. |
| HLTH-006 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HLTH-001, HLTH-002, PLAT-009 | Consolidate counseling tickets with anonymity, consent, crisis acknowledgement, mandatory reporting, and escalation. |
| HLTH-007 | P1 | `BLOCKED_CLIENT_INFO` | HLTH-002, DOC-004 | Configure medical certificates, retention, export/deletion, break-glass, dispensing, and jurisdiction policy. |
| LIB-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-003, MST-003 | Normalize title/edition/copy/member identity, tenant-qualified identifiers, status, shelf, and search. |
| LIB-002 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | LIB-001, SEC-008 | Implement atomic checkout/return so one copy cannot be issued twice and ownership is enforced. |
| LIB-003 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | LIB-002 | Implement renewals, ordered holds, pickup expiry, cancellation, due dates, and patron policy limits. |
| LIB-004 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | LIB-002, LIB-003, FIN-006 | Implement overdue, fines, waivers, lost/damaged/replacement, settlement, and audited adjustments. |
| LIB-005 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | LIB-001, LIB-002 | Add stocktake, acquisitions linkage, e-resources, notifications, circulation history, and analytics. |
| DOC-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-006, PLAT-007 | Select and implement one real storage contract across local/S3/R2/Azure/GCS/MinIO with tenant prefixes. |
| DOC-002 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | DOC-001, SEC-008 | Replace the unsafe vault with tenant-safe metadata, ownership, audit, retention state, and real integrity checks. |
| DOC-003 | P1 | `BLOCKED_ARCH_DECISION` | DOC-001 | Approve the renderer/sandbox boundary and versioned tenant template format for institutional documents. |
| DOC-004 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | DOC-002, DOC-003, OPS-003 | Add asynchronous generation, durable verification registry, template/version metadata, supersession, and PQC/QR checks. |
| HR-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | SEC-004, PLAT-003 | Make User, Employee, Faculty, department, designation, and effective employment links authoritative. |
| HR-002 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HR-001, SEC-003, SEC-008 | Protect employee salary, contracts, documents, qualifications, and self/manager/HR views. |
| HR-003 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HR-001, ACAD-004 | Model faculty-section assignments and effective HOD/dean/acting authority relationships. |
| HR-004 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HR-001, HR-003, MST-003 | Implement leave balances/approval and distinct employee attendance with concurrency-safe corrections. |
| HR-005 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HR-002, HR-004 | Add an approved HR payroll source, appraisals, training, promotion, and restricted disciplinary workflows. |
| HR-006 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | HR-002, HR-005, DOC-004, SD-002 | Implement resignation/retirement/offboarding, clearance, asset return, role revocation, and final settlement. |
| SD-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-002, MST-003, PLAT-003 | Consolidate runtime-DDL, EF support, grievance, and maintenance ticket truth stores through migration. |
| SD-002 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | SD-001, SEC-003, DOC-002 | Implement tenant-scoped service desk tickets, queues, categories, assignment, comments, attachments, and history. |
| SD-003 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | SD-002, PLAT-009 | Add configurable SLA/escalation, workflow handoff, confidential categories, notifications, and reports. |
| FAC-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-003, MST-002 | Establish canonical Tenant/Campus/Building/Floor/Space identity, capabilities, accessibility, and assignments. |
| FAC-002 | P1 | `BLOCKED_CLIENT_INFO` | FAC-001, ACAD-004 | Backfill timetable/exam free-text rooms to Space using approved campus inventory and exception mapping. |
| FAC-003 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FAC-001, PLAT-004 | Add the PostgreSQL range/exclusion-backed occupancy ledger and stable conflict-to-HTTP mapping. |
| FAC-004 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FAC-003, SEC-008 | Implement meeting availability, reserve/modify/cancel, recurrence, approval, participants, and audit. |
| FAC-005 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FAC-003, SD-002 | Link maintenance work to Space/capability blocks and relocation/notification workflows. |
| FAC-006 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | DOC-001, DOC-002, OPS-003, FAC-001 | Add secure immutable drawing upload, extraction jobs, normalized draft geometry, and provenance. |
| FAC-007 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | FAC-003, FAC-006 | Add engineer review/version publish/rollback, live room boards, and asynchronous utilization read models. |
| OPS-001 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001 | Add OpenTelemetry traces/metrics/log correlation and capture repeatable baseline evidence. |
| OPS-002 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-002, DOC-001, OPS-001 | Correct liveness/readiness/deep health semantics and dependency-specific degraded/fail-closed states. |
| OPS-003 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-008, PLAT-009, OPS-001 | Add bounded durable asynchronous jobs, cancellation, backpressure, retries, and idempotent recovery. |
| OPS-004 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | DOC-001, OPS-001 | Make backup tooling executable, encrypted when configured, restore-verified, timed, and auditable. |
| OPS-005 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-002, PLAT-009 | Replace multi-replica startup migrations with coordinated pre-deploy migration and rollback rehearsal. |
| OPS-006 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | OPS-002, OPS-005 | Repair Kubernetes service/ingress/storage/runtime defects and prove Docker/Podman OCI compatibility. |
| OPS-007 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | PLAT-006, PLAT-007, OPS-001 | Standardize provider timeouts, cancellation, safe retries, circuits, health, and redacted telemetry. |
| OPS-008 | P0 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | MST-001, MST-002, OPS-005 | Make one CI release gate authoritative for build, tests, migrations, contracts, images, SBOM, and scans. |
| OPS-009 | P1 | `PLANNED_REVERIFY_AFTER_DEPENDENCIES` | OPS-001, ACAD-011, FIN-011, DOC-004 | Re-run representative performance suites with order-specific thresholds and rollback conditions. |
| OPS-010 | P1 | `BLOCKED_CLIENT_INFO` | PLAT-010, OPS-004, OPS-006, OPS-008, OPS-009 | Certify approved provider/deployment profiles, RPO/RTO, smoke tests, release metadata, and rollback. |

## Materialized Order Files

| ID | Path | Expected base |
|---|---|---|
| PLAT-001 | `docs/backend-orders/01-platform/PLAT-001_TENANT_ENFORCEMENT_INVENTORY.md` | `d53b4b564661f0b990c59674386287c50364848f` |

All other backlog rows must be materialized only after their dependencies complete and current symbols are re-verified.
