# PROMPT 07 — HR, FACULTY, SERVICE DESK

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as principal HR/faculty/service-management architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/07-hr-service-desk/`. Do not implement production code.

## HR/faculty depth

Audit and complete where required:

- employee/faculty profile;
- department/designation/appointment;
- employment history/contracts;
- qualifications/credentials;
- teaching assignments and workload;
- office/workspace link to Facilities;
- leave types, balances, requests and approval;
- employee attendance distinct from class attendance;
- payroll integration;
- benefits where product scope supports them;
- appraisal/performance review;
- promotion/training;
- research/publications/supervision where appropriate;
- disciplinary workflow with restricted access;
- delegation/acting authority;
- resignation/retirement/offboarding;
- clearance and asset return;
- documents and notifications.

Faculty-to-course and HOD-to-department relationships should be modeled explicitly enough to support reliable authorization, grading and academic workflow checks.

## Unified service desk

Audit existing specialized ticket/request systems first. Build orders for a configurable **university service desk**, not a ServiceNow clone.

Core target:

- tenant-scoped ticket;
- requester;
- department/queue/team;
- configurable category/subcategory;
- subject/description;
- priority;
- assignment;
- comments;
- attachments through common storage;
- status/history;
- SLA targets;
- escalation;
- notifications;
- audit;
- workflow/approval integration;
- reporting.

Potential queues include HR, IT, Finance, Registrar, Examinations, Admissions, Facilities, Transport, Library and Student Affairs.

## Specialized systems

Do **not** blindly merge mental-health, clinical, grievance or other confidential case systems. Reuse common primitives only where confidentiality and workflow invariants remain intact.

## Approval integration

Some tickets become approval workflows: salary correction, fee correction/refund, privileged IT access, student-record correction. Do not put arbitrary approval logic in one ticket controller; integrate with the existing workflow/approval infrastructure after source verification.

## SLA/configuration

SLA/category/team definitions must be tenant configurable. Define overdue/escalation semantics, idempotent schedulers and multi-replica-safe background processing.

## Tests

Orders must include queue visibility, requester ownership, confidential-category restrictions, assignment authorization, SLA/escalation timing, attachment access, workflow handoff, tenant isolation and audit history.

## Final chat response

Return only:

`ORDERS_CREATED`, `HR_COMPLETENESS_CURRENT`, `SERVICE_DESK_CURRENT`, `SPECIALIZED_SYSTEMS_TO_KEEP_SEPARATE`, `P0_GAPS`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.