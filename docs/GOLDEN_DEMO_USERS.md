# Golden Demo Users

> **ISOLATED DEMO TENANT ONLY.** These accounts and the disposable password must never be used outside the local `aegiscore_golden_demo` database. They are not production credentials.

All accounts use the password configured by `AEGISCORE_DEMO_PASSWORD`. The checked-in `.env.demo.example` uses `GoldenDemo!2026` for local demonstration only.

| Persona | Display name | Demo login | Backend role | Purpose and main flow |
|---|---|---|---|---|
| Golden Student | Ali Hassan | `student@iobm.demo` | Student | Historical applicant, admitted BSCS student, DSA enrollee, fee payer, published B+ recipient, graduate and alumni. Opens telemetry, course detail, grade explanation, and owned admissions explanation. |
| Golden Teacher | Dr. Imran Siddiqui | `faculty@iobm.demo` | Faculty | DSA teacher with faculty, employee, workload, timetable, roster, attendance, marks, CLO/PO, and completed teaching evidence. |
| Admissions Officer | Sana Mirza | `admissions@iobm.demo` | AdmissionsHead | Published Ali's internally consistent merit-list decision and allocated offer. |
| Registrar | Hira Nawaz | `registrar@iobm.demo` | Registrar | Student identity linkage, cohort/program context, enrollment and graduation audit provenance. |
| Examination Officer | Ayesha Qureshi | `exams@iobm.demo` | ExamController | Assessment scheme, exam schedule, published grade, and resolved re-totaling appeal. |
| Finance Officer | Khalid Hussain | `finance@iobm.demo` | Finance | PKR 145,000 demand, settlement, reconciliation, and zero outstanding balance. |
| HOD / Program Head | Farhan Ahmed | `hod@iobm.demo` | HOD | CS department, DSA assignment/workload, and department-scoped academic oversight. |
| Dean | Nadia Rahman | `dean@iobm.demo` | Dean | Broader academic oversight of the same CS cohort and course. |
| OBE / QA Officer | Maham Iqbal | `qa@iobm.demo` | OBECoordinator | DSA CLO, BSCS PO, strong mapping, and attainment evidence. |
| HR Officer | Nadia Shah | `hr@iobm.demo` | HR | Imran's permanent employee record, assistant-professor designation, and service/workload evidence. |
| Auditor / Compliance | Usman Tariq | `auditor@iobm.demo` | Auditor | Read-oriented provenance across admissions, finance, grade, OBE, HR, and identity events. |
| System Administrator | Bilal Raza | `it@iobm.demo` | Admin | Tenant identity and RBAC administration. This account is not required to power student, teacher, or officer flows. |

## Shared Relationships

- Showcase tenant: AegisCore Golden University (`ACGU`).
- Program/cohort: BS Computer Science, BSCS 2022 Golden Cohort.
- Showcase course: `CS-201 Data Structures & Algorithms`, Spring 2026.
- Ali and 35 deterministic supporting students are enrolled in the same course.
- Imran is the assigned teacher and marks/attendance actor.
- All explanation and telemetry links are obtained through caller-scoped discovery APIs, not copied UUIDs.
- No model, prediction, model-quality, global SHAP, or local SHAP snapshot is seeded.
