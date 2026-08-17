# Decision And Client Input Register

## Architecture Decisions Required

| ID | Blocks | Decision |
|---|---|---|
| ADR-BE-001 | MST-001 | Choose canonical identity/demo/analytics lineage: restore authoritative linked identities and analytics specifications, or deliberately redesign tests/contracts around the current shared-ID demo. Tests may not be deleted merely to obtain green CI. |
| ADR-BE-002 | MST-002, FIN-002, HLTH-001, SD-001 | Approve canonical legacy/v2 table ownership and data transition before changing the EF snapshot or generating migrations. |
| ADR-BE-003 | PLAT-004 | Approve RLS context strategy compatible with PgBouncer transaction pooling and a non-owner `NOBYPASSRLS` runtime role. |
| ADR-BE-004 | DOC-003 | Approve the document template language/renderer and hostile-template isolation boundary. |
| ADR-BE-005 | FIN-002 | Approve finance-v2 as canonical and define compatibility/deprecation for legacy dues, payments, procurement, and payroll. |

## Client Information Required

| Area | Required input |
|---|---|
| Tenant/deployment | Institution names/slugs/UUIDs, domains, locale/timezone/currency, isolation profile, cloud/on-prem responsibility, maintenance windows. |
| Identity | Required IdPs, metadata/credentials/certificates, redirect URIs, claim/group mappings, JIT/linking/disable policy, break-glass owners. |
| Academics | Departments/programs/batches, calendars, grading/OBE policy, registration windows, credit/capacity/waitlist/hold rules, exam policy. |
| Branding/documents | Logos/colors/text, approved templates, merge fields, signing/verification wording, version/approval process. |
| Finance | Chart of accounts, fiscal calendar, currency, fee/refund/penalty rules, approval limits, gateways/banks, settlement/reconciliation rules. |
| Health/welfare | Jurisdiction policy, retention, consent, confidentiality matrix, crisis/mandatory-report contacts, dispensing/lab scope. |
| Library | Member classes, loan periods/limits, fine/waiver/lost policy, barcode/location scheme, e-resource scope. |
| HR/service desk | Organization/authority hierarchy, leave/payroll/appraisal policy, queues/categories/teams, confidentiality, SLA/escalation rules. |
| Facilities | Campus/building/floor/room inventory, drawings, naming/mapping exceptions, capacities, operating hours, accessibility/capability taxonomy. |
| Operations | SLOs, representative volumes, RPO/RTO, backup retention/residency, monitoring recipients, offline-license tolerance, supported provider matrix. |

Implementation may build configuration mechanisms before these values exist, but activation/certification orders remain blocked and must not fabricate defaults.
