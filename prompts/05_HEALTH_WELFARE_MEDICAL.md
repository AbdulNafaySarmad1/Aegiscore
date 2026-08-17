# PROMPT 05 — HEALTH, WELFARE, MEDICAL

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as principal university health/welfare backend architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/05-health/`. Do not implement production code.

## Standard

The university clinic/medical domain should be operationally serious enough to function as a real campus health service, without pretending to be a national hospital/EHR product.

## Audit and complete where required

- student/patient medical profile;
- emergency contacts;
- allergies;
- conditions;
- medications;
- appointments;
- walk-ins;
- triage;
- vitals;
- encounters;
- clinical notes;
- diagnoses;
- prescriptions;
- dispensing only if the university clinic actually supports it;
- referrals;
- lab-test requests/results where justified;
- vaccination records;
- medical/sick/fitness certificates;
- injury/incident records;
- emergency-response references/workflows;
- attached documents;
- doctor/nurse/counselor roles;
- consent;
- retention/privacy/audit.

## Domain separation

Explicitly separate and verify boundaries between:

1. general student welfare;
2. mental-health/counseling;
3. clinical medical records;
4. service-desk/general support.

Do not weaken existing anonymity, crisis, mandatory-reporting or restricted-visibility requirements merely to reuse generic infrastructure.

## Privacy/security

Generated orders must define:

- least-privilege roles;
- field/resource-level confidentiality where needed;
- who can see clinical vs counseling vs welfare data;
- emergency/break-glass behavior if justified;
- explicit audit events for sensitive reads/writes;
- prohibition on logging protected clinical content;
- attachment authorization;
- tenant isolation;
- retention/export/deletion behavior consistent with configured policy.

Do not invent local medical regulations. Where client/jurisdiction policy is required, mark `CLIENT_INFORMATION_REQUIRED` and make policy configurable.

## Workflow/integration

Reuse common notification/document/workflow/storage primitives where appropriate, but do not send sensitive diagnosis/mental-health content through insecure notification channels. Certificates should use the common document/verification pipeline where appropriate.

## Tests

Orders must include negative confidentiality tests, role-matrix tests, cross-tenant access attempts, audit verification, attachment access tests, and boundary tests proving generic service-desk staff cannot see clinical/counseling records.

## Final chat response

Return only:

`ORDERS_CREATED`, `HEALTH_COMPLETENESS_CURRENT`, `CONFIDENTIALITY_BOUNDARIES`, `P0_GAPS`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.