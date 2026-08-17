# Backend Completeness Matrix

Classification reflects verified source at `d53b4b564661f0b990c59674386287c50364848f`, not endpoint or entity presence alone.

| Capability | Current state | Main verified evidence | Target orders |
|---|---|---|---|
| Build/test baseline | Release-blocked | Production projects compile; test project has 10 missing analytics-symbol errors. | MST-001, OPS-008 |
| Migration reproducibility | Release-blocked | EF reports pending model changes; identity and health models conflict with migrations/snapshot. | MST-002, OPS-005 |
| Tenant isolation | Critical partial | Global filters cover only `ITenantScoped`; core SIS roots lack direct tenant ownership; RLS is incomplete. | PLAT-001, PLAT-003, PLAT-004 |
| Configuration/productization | Partial | `TenantConfigService` ignores tenant during resolution; branding/config remain instance-global. | PLAT-005 through PLAT-010 |
| Identity/authentication | Local partial | Local JWT/refresh exists; linked identity, atomic refresh, federation, and SCIM are incomplete. | SEC-002, SEC-004 through SEC-006 |
| Authorization | Critical partial | Coarse roles dominate; ABAC is header comparison; many actor/ownership gaps exist. | SEC-003, SEC-007, SEC-008 |
| SAML | Unsafe/incomplete | Signature is loaded but not verified; replay is process-local; ACS does not issue JWT. | SEC-001 |
| Student lifecycle | Partial | Admissions and portal slices exist; no durable accepted-applicant conversion. | ACAD-001, ACAD-002 |
| Curriculum/registration | Demo/fragmented | Static registration offerings and parallel curriculum/prerequisite representations. | ACAD-003 through ACAD-005 |
| Timetable/attendance | Partial | Free-text room, service-only conflict prechecks, duplicate attendance paths and missing unique guarantees. | ACAD-006, ACAD-007, FAC-003 |
| Grading/results | Strong partial | Mark entry/import exists; competing grade stores, actor gaps, moderation collisions, and repeat defects remain. | ACAD-008, ACAD-009 |
| OBE/accreditation | Fragmented partial | Parallel models; CLO calculation ignores assessment mappings; N+1/in-memory aggregation. | ACAD-010, ACAD-011 |
| Examinations | Skeleton | Schedule/seating entities exist; operational schedule, allocation, invigilation, and admit-card flows do not. | ACAD-012, ACAD-013 |
| Transcript/degree lifecycle | One strong slice | Durable signed transcript path exists; duplicate paths, broken degree verification, and incomplete audit/graduation remain. | ACAD-014, DOC-004 |
| Finance/accounting | Broad partial | Finance-v2 is substantial; manual payment, period close, ledger immutability, allocation, and legacy split are unsafe. | FIN-001 through FIN-011 |
| Health/clinical | High-risk skeleton | Health entities and a read route exist; tenant/privacy/workflow/migration protections are insufficient. | HLTH-001 through HLTH-005 |
| Counseling/welfare | Unsafe fragmented | Two services, migration drift, broad access, cross-tenant counselor selection, weak crisis workflow. | HLTH-006, HLTH-007 |
| Library | Model/read-only partial | Catalogue search exists; circulation writes do not persist and return/renew/holds are missing. | LIB-001 through LIB-005 |
| Document platform | Fragmented/unsafe | Transcript path is credible; generic vault is non-persistent, unscoped, and backed by stubbed storage. | DOC-001 through DOC-004, SEC-009 |
| HR/faculty | Broad schema, narrow API | Salary exposure, leave impersonation, weak links, schema-only appraisal/offboarding. | HR-001 through HR-006 |
| Service desk | Not unified | Runtime-DDL AI support, dead EF support, grievance, maintenance, and mental-health systems diverge. | SD-001 through SD-003 |
| Facilities/space | Fragmented | Campus/digital-twin entities exist; no canonical Space hierarchy or authoritative occupancy kernel. | FAC-001 through FAC-007 |
| Integrations/providers | Partial/stubbed | Payments are strongest; storage/email/identity/BI implementations and routing are inconsistent. | DOC-001, PLAT-008, OPS-007 |
| Workers/async | Replica-unsafe | Hosted workers, Quartz, migrations, and in-memory queues run in each replica without ownership. | PLAT-009, OPS-003, OPS-005 |
| Health/readiness | Misleading partial | Some endpoints are static/hard-coded; extended dependency checks are not fully registered. | OPS-002 |
| Backup/restore | Present but unproven/broken | API image lacks PostgreSQL tools; encryption verification and restore validation are incomplete. | OPS-004 |
| Observability/performance | Partial | Prometheus/Serilog exist; no distributed tracing; measured API CPU/document bottlenecks remain. | OPS-001, OPS-009 |
| CI/release | Fragmented | Competing workflows can publish without one authoritative green release gate. | OPS-008, OPS-010 |

## Current Readiness Estimate

Backend production readiness is approximately 35 percent. Feature breadth is materially higher than production correctness. The estimate is constrained by release-blocking build/migration drift, systemic tenant gaps, sensitive authorization failures, and unproven restore/deployment behavior.
