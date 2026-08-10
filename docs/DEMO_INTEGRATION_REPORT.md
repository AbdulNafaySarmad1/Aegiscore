# Demo Integration Report

## Baseline

- Starting milestone: `55188851ce09cbae7077945f36e49ad7eafec715`.
- Working branch: `agent/demo-integration`.
- Frozen recovery tag: `telemetry-demo-ready-20260810` (not modified).

## Integration Blockers

### Student identity linkage

- Added nullable one-to-one `Users.StudentId` with a unique index and restrictive FK.
- Login and refresh load the relationship through Student -> Batch -> Department.
- JWT `student_id` and `department_id` are emitted only when the full persisted relationship resolves to the user's tenant.
- Missing and wrong-tenant links fail closed. External/client claim JSON cannot override the authoritative link.
- Cookie and Bearer token contracts, refresh rotation, and existing session behavior remain intact.

### Discoverability

- Added bounded, caller-scoped discovery for linked-student courses, published grades, genuine stored prediction snapshots, owned applications, and admissions-staff applications.
- Student and applicant queries authorize ownership in trusted tenant joins. Staff prediction queries preserve existing tenant/department scope.
- Changed object IDs still return `404` when not owned or authorized.
- The existing telemetry overview now presents natural links without adding a generic ID picker or redesigning the UI.

### Comparison semantics

- Preserved snapshot-defined `comparisonPopulation` and server-generated metrics/statuses.
- No selector was added because the backend has no safe arbitrary-population contract.

### Model honesty

- No predictive model, prediction, quality, global explanation, local SHAP, or fake feature attribution is seeded.
- Deterministic grade and admissions explanations remain fully available.

## Golden Universe

- 12 normal-login personas in one demo tenant, documented in `docs/GOLDEN_DEMO_USERS.md`.
- Existing Ali Hassan and Dr. Imran Siddiqui identities/emails are preserved and extended.
- 35 supporting students plus Ali produce a deterministic 36-person showcase cohort.
- Three supporting faculty and four CS courses provide surrounding context.
- All identifiers derive deterministically from tenant + logical key; natural email lookup preserves pre-existing demo users.
- Passwords use the production Argon2 password service and a demo-only environment variable.

## Student Lifecycle Coverage

- Historical application, published merit list, seat allocation, accepted offer status, authoritative student link, program/cohort context, settled fee demand, course enrollment, timetable, attendance, assessment marks, published grade, CLO/PO evidence, telemetry, degree audit, graduated state, and alumni profile.
- Admissions arithmetic: `26.40 + 25.20 + 32.80 = 84.40`, rank 7, 40 seats, allocated.
- Grade arithmetic: five components reproduce `78.92` under the stored 100% scheme; B+ boundary is 77-79 and A begins at 80, giving a 1.08-point distance.
- Cohort context contains 36 published course grades, above the default privacy minimum of 10; matching overall and DSA telemetry snapshots support overview and drill-down navigation.

## Teacher Lifecycle Coverage

- Faculty and HR employee identities, CS department/designation, historical workload, DSA assignment, timetable, 36-person roster, attendance actor, marks actor, assessment scheme, exam schedules, CLO/PO mapping, published grade distribution, and resolved review case.

## Database And Reset

- Existing migration `20260809120000_AddAnalyticsIntelligencePlatform` remains unchanged.
- Added schema migrations `20260810120000_LinkUserStudentIdentity`, `20260810121000_LinkAdmissionApplicant`, and `20260810122000_LinkUserStaffDepartment`; their metadata is represented in the existing snapshot without concealing unrelated legacy drift.
- Environment-variable configuration remains canonical; no real connection string is stored.
- `scripts/reset-golden-demo.sh` parses the environment file without executing it, is hard-bound to a local Unix-socket Docker engine, loopback publishing, a non-production Compose project/database, and rejects external connection strings.
- Golden seeding occurs only when both `Runtime:DemoMode=true` and `Demo:SeedGoldenUniverse=true`, and PostgreSQL seeding additionally verifies the active database is `aegiscore_golden_demo`.
- The golden environment binds the portal to `127.0.0.1`; legacy unauthenticated demo-administrator injection is separately gated and disabled.

## Validation Results

- `dotnet build AegisCore.slnx`: succeeded with 0 errors.
- `dotnet test AegisCore.Tests/AegisCore.Tests.csproj`: 568 passed, 1 skipped, 0 failed.
- `npm run typecheck`: passed.
- `npm run lint`: passed with 0 errors and 122 pre-existing warnings.
- `npm run build`: production build passed.
- `npm run test:e2e`: 40 passed, 0 failed.
- A clean PostgreSQL 17 database migrated through `20260809120000_AddAnalyticsIntelligencePlatform`, `20260810120000_LinkUserStudentIdentity`, `20260810121000_LinkAdmissionApplicant`, and `20260810122000_LinkUserStaffDepartment`.
- Two consecutive PostgreSQL seed runs produced the same tenant totals and student password hash: 12 users, 36 students, 36 course enrollments, 36 published grades, 2 telemetry snapshots, and 0 model prediction snapshots.
- Fresh data confirmed that the HOD and Dean resolve to the CS staff department, the QA persona carries the `OBECoordinator` role, and `CS-201` maps `CS201-CLO-2` to `PO-CS-2` with high strength.
- `GOLDEN_DEMO_ENV_FILE=.env.demo.example ./scripts/reset-golden-demo.sh --check`: safeguards passed for project `aegiscore-golden-demo` and database `aegiscore_golden_demo`.
- An attempted update to a persisted analytics snapshot failed with `ERROR: analytics snapshots are immutable`.
- Live Bearer-token checks passed for authoritative session claims, student telemetry and discovery, the grade explanation (`78.92`, B+, `1.08` points to A, cohort 36), and the admissions explanation (`84.40`, rank 7 of 40, allocated).

## Known Limitations

- The legacy SIS model lacks a direct student-program relationship, section/semester enrollment, and multiple course attempts.
- Admissions has no historical policy-version or structured eligibility record; explanations retain explicit limitations.
- The complete 130-credit degree history cannot be represented without inventing data, so the seed records one truthful showcase requirement and documents the gap.
- Faculty-to-user linkage is a coherent deterministic demo convention but is not yet an authoritative production identity relationship.
- Several broad ERP pages predate this integration and may not surface every seeded record; the telemetry navigation and APIs are the validated end-to-end path.

## Manual Demo Steps

- Create `.env.demo` from `.env.demo.example`.
- Run the guarded reset command.
- Follow `docs/GOLDEN_DEMO_RUNBOOK.md`; no UUID copying or ID guessing is required.
