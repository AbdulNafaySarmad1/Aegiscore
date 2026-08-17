# PROMPT 03 — STUDENT, ACADEMICS, OBE, EXAMS

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as principal academic-domain architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/03-academics/`. Do not implement production code.

## Standard

Do not ask merely whether an endpoint exists. Determine whether a real university can operate the full student academic lifecycle against the backend.

## Audit and complete where required

### Student lifecycle
- onboarding and identity linking;
- profile/contact/emergency information;
- program/department/batch/semester association;
- status changes, holds, suspension/withdrawal/transfer where justified;
- student documents and requests;
- self-service ownership rules.

### Curriculum and registration
- course catalogue;
- prerequisites/co-requisites;
- course offerings and sections;
- faculty/section choices, including multiple eligible teachers where configured;
- capacity and wait-list policy where justified;
- registration windows;
- credit limits and academic holds;
- add/drop/withdrawal;
- conflicts and prerequisite validation;
- auditable overrides/approvals.

### Timetable and attendance
- canonical section/faculty/course relationships;
- student/faculty timetable;
- classroom/Space integration instead of permanent free-text room identifiers;
- student attendance;
- employee/faculty attendance as a separate concept;
- attendance corrections/requests;
- extracurricular/authorized absence workflows;
- short-attendance warnings/eligibility;
- SQL aggregates/indexes rather than load-all calculations.

### OBE / HEC / accreditation
- CLO;
- PLO;
- CLO↔PLO mapping;
- course/assessment mapping;
- rubric/threshold configuration;
- student attainment;
- course attainment;
- cohort/program attainment;
- drill-down evidence;
- historical/versioned mappings where policy changes;
- HEC/accreditation reporting/export evidence;
- explainability of calculations;
- remove confirmed N+1/in-memory aggregate paths via correct SQL/batch design.

### Assessment / grading
- assessment schemes;
- manual entry;
- Excel import;
- PDF/import boundary only where deterministic extraction is safe;
- validation and row-level errors;
- sensitive change detection;
- faculty ownership of taught sections;
- HOD/authority approvals;
- examination-controller/finalization workflow;
- immutable audit of before/after values;
- GPA/CGPA calculation;
- result publication/freeze/reopen policy;
- recheck/re-evaluation/appeal where supported.

### Examinations
- exam definitions/types;
- schedules;
- canonical Space/room FK integration;
- student conflict detection;
- capacity;
- seating generation/allocation;
- invigilators and invigilator conflicts;
- admit cards/hall tickets;
- exam attendance;
- result processing;
- verifiable/PQC/QR documents where applicable.

### Transcripts / degree lifecycle
- unofficial/official transcript distinctions;
- configurable templates;
- verification UUID/QR/signature metadata;
- degree audit;
- graduation eligibility;
- academic history integrity;
- ownership/privacy.

## Anti-hallucination requirements

For every current entity/route/service, verify it in current source. If sections, rubrics, degree-audit objects or other concepts do not exist, mark them `TO_BE_CREATED`; do not pretend they already exist.

Preserve existing working demo behavior through migrations/compatibility, but retire structurally unsafe free-text identities and duplicated logic where the target architecture requires it.

## Required order qualities

Every generated order must state:

- student/faculty/HOD/registrar/exam-controller authorization;
- tenant rules;
- data migration/backfill;
- academic invariants;
- concurrent registration/seat-capacity behavior;
- exact negative tests;
- query/index expectations;
- audit requirements;
- API contracts suitable for later frontend wiring.

## Final chat response

Return only:

`ORDERS_CREATED`, `ACADEMIC_COMPLETENESS_CURRENT`, `P0_GAPS`, `OBE_GAPS`, `EXAM_GAPS`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.