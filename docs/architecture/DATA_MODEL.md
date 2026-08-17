# AegisCore DATA_MODEL.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.

## 1. Scale

- `AegisCore.Domain/Entities/` — 173 files, **601 entity classes**, flat layout.
- `AegisDbContext` (`AegisCore.Infrastructure/AegisDbContext.cs`) — **599 `DbSet` properties** (lines 32–1938), all EF config inline in `OnModelCreating` (no `IEntityTypeConfiguration` files).
- Migrations: **61 EF migrations** under `AegisCore.Infrastructure/Migrations/` (baseline `20260726160353_V1_0_AegisCoreBaseline` → `20260810122000_LinkUserStaffDepartment`).

## 2. Tenant model

- Interface: `AegisCore.Domain/Common/ITenantScoped.cs:6` — single `Guid TenantId`.
- Global query filter: `AegisDbContext.cs:1949-1962` — every `ITenantScoped` entity filtered `e.TenantId == CurrentTenantId`.
- `CurrentTenantId` ← `HttpContext.Items["TenantId"]` (`AegisDbContext.cs:17`), set by `TenantContextMiddleware` from JWT `tenant_id`/`tid`/`tenantid` claim.
- Connection interceptor `TenantSessionInterceptor.cs:10-28` runs `SELECT set_config('app.tenant_id', ...)` on every connection open (chain: Items → claims → config `TENANT_ID`). Deploy-time `010-harden-app-role.sh` creates a `NOBYPASSRLS` app role (k8s + scripts) so RLS is enforceable.
- ~172 `ITenantScoped` classes across 48 files.

### Critical tenant-scoping gaps (documented, not fixed)

| Category | Count | Examples | Implication |
|---|---|---|---|
| Entities with `TenantId` column but **no** `ITenantScoped` (no EF filter) | ~145 files | `User` (`User.cs:50`), `Employee`, `StudentFee`, `FeePayment`, `StudentProfile`, `License`, `ApiKey`, `WebhookSubscription`, `FormSubmission`, `Building`, `Room`, `SecurityIncident`, `Visitor`, `DocumentVaultEntry`, `ConfigAuditLog`, `SyncLagEntries`, `ReplicaHealthLogs` | Tenant isolation depends on application code (`Where(TenantId==...)`) — risk of missing predicate = cross-tenant read/write |
| Core SIS entities with **no TenantId at all** | ~40 | `Student`, `Faculty`, `Course`, `Enrollment`, `Attendance`, `Timetable`, `CloPlomapping`, `Batch`, `ExamSchedule`, `CourseMark`, `CourseGrade`, `GradeScale`, `CLOAttainment`, `AdmissionApplication`, `MeritList`, `GradeOverrideRequest`, `AcademicYear`, `Semester` | Scoped only indirectly (Student→Batch→Department; Course→Department) — no DB-enforced tenant boundary |
| Global (correctly unscoped) | ~6 | `Tenant`, `LicenseState`, `Role`(config), `Permission`(config), `PolicyConfig` | Intended global |

## 3. Core relationships

- `Student → Batch` (`Student.cs:26-32`); collections `Enrollments`, `ExamSeatings`.
- `Course → Department`; self `PrerequisiteCourseId/DependentCourses`; collections `CourseAssignments`, `Enrollments`, `Attendances`, `ExamSchedules`, `Results`, `Timetables`, `CloPlomappings` (`Course.cs:22-50`).
- `Enrollment → Student, Course`; collections `Attendances`, `Results` (`Enrollment.cs:13-32`).
- `Attendance → Enrollment, Course, Faculty` (`Attendance.cs:13-40`).
- `Timetable → Batch, Course, Faculty` (`Timetable.cs:13-35`).
- `Result → Enrollment, Course` (`Result.cs:22-27`).
- Exams: `AssessmentScheme.Components`; `CourseMark → AssessmentComponent`; `GradeScale.Grades` (`ExaminationEntities.cs`).
- OBE: `CLOPOMapping → CourseLearningOutcome/ProgramOutcome`; `Rubric → RubricCriterion`; `CLOAttainment` per student/CLO/course (`CoreErpEntities1.cs:88-110`).
- Finance: `JournalEntry.Lines`; `JournalEntryLine → JournalEntry/ChartOfAccount/CostCenter/ReportingEntity`; `FiscalYear → FiscalPeriod`; `FinanceVoucher.Lines/.Approvals`; voucher→journal bridge (`FinanceModuleService.cs:152-160`).

## 4. High-cardinality tables (target ~10k users/institution)

| Table | Growth driver | Tenant-filtered? |
|---|---|---|
| `Attendances` | 1/enrollment/session | NO (no TenantId) |
| `Enrollments` | unique (StudentId, CourseId) | NO |
| `CourseMarks` | 1/student/assessment component | NO |
| `CourseGrades` | 1/student/course/semester | NO |
| `CLOAttainments` | 1/student/CLO/course | NO |
| `AuditLogs` | every critical action | YES |
| `JournalEntryLines` / `FinanceVoucherLines` | GL postings | YES |
| `PaymentGatewayEvents` | every webhook | YES |
| `ExamPaperScans` | per-student scan/exam | NO |
| `SyncLagEntries`, `ReplicaHealthLogs`, `DocumentAccessLogs` | operational telemetry | YES |

## 5. Indexes — existing (AegisDbContext.cs:2099-3428)

Notable: `Student.UniversityId` UNIQUE (:3030); `Course.Code` UNIQUE (:3036); `Enrollment(StudentId,CourseId)` UNIQUE (:3044); `ExamSeating(ExamScheduleId,SeatNumber)` UNIQUE (:3050); `Department(TenantId,Code)` UNIQUE (:3024); `AuditLog(Timestamp,UserId,TenantId)` (:3425-3427); `CourseGrade(CourseId,StudentId)` UNIQUE + `(StudentId,SemesterId)` (:2238-2240); `CourseMark(CourseId,StudentId,AssessmentComponentId)` UNIQUE (:2216); `AssessmentScheme(CourseId,SemesterId)` UNIQUE (:2205); `Transcript.VerificationHash` UNIQUE (:2250); finance composite indexes everywhere (TenantId-prefixed); `Role.Name`/`Permission(Module,Action,Resource)`/`UserRole(UserId,RoleId)` UNIQUE (:2885-2900); `PolicyConfig(Module,Key,EffectiveFrom)` UNIQUE (:2101).

## 6. Missing indexes / likely slow paths (NEEDS_RUNTIME_MEASUREMENT)

| Location | Query pattern | Missing index |
|---|---|---|
| `Repositories/AttendanceRepository.cs:26-33` | `WHERE Date >= x AND Date <= y` | `Attendance(Date)` |
| `Repositories/AttendanceRepository.cs:13-24` | per-enrollment status count (loads all rows in memory) | `Attendance(EnrollmentId, Status)` |
| `Obe/ObeServices.cs:89-92` | CLO attainment: all `CourseGrades` for (CourseId,SemesterId) materialized in memory for AVG | `CourseGrades(CourseId, SemesterId, PercentageObtained)` + aggregate query |
| OBE program report | P×M×2 queries (see PERFORMANCE_MAP #3) | composite CLO/PO attainment |
| `Result` | by EnrollmentId/CourseId lookups | `Result(CourseId, StudentId)` |
| `CLOAttainment` | per course/semester scans | `CLOAttainment(CourseLearningOutcomeId, StudentId, CourseId, Semester)` |
| `Timetable` | day/time/room lookups | day+time+room composite |
| 145 non-filtered tables | app-code tenant predicate | only FK-convention indexes |

## 7. Migration themes (latest)

- `LinkUserStaffDepartment`, `LinkAdmissionApplicant`, `LinkUserStudentIdentity` (unique filtered index on `Users.StudentId`) — recent identity-linking work.
- `V2_1_UserRefreshTokens`, `V2_2_UserRefreshNotification` — refresh-token columns on Users.
- Finance stream (44 migrations): ledger isolation, PQC keys, audit seals, hash-chain, append-only guards, cost-center/tenant scoping, compliance tenant isolation.
- Extended platform: gamification, appeals, benchmarks, board alerts, elections, sync-lag/replication tables.

## 8. N+1 / query-hygiene findings (all confirmed by code)

1. **OBE program report** — `ObeServices.cs:120-126` loops POs → `:97-118` loops mappings → `:85-95` loads all grades in memory. ~1 + P + P·M queries; grades materialized per CLO. `CONFIRMED_BY_CODE`.
2. **Grading grace marks** — `GradingService.ApplyGracemarksAsync` per-entry `CalculateCourseGradeAsync` re-queries scheme/marks/band/grade (`GradingService.cs:93-116`, `:12-70`). `CONFIRMED_BY_CODE`.
3. **Attendance percentage** — full row set loaded to count Present (`AttendanceRepository.cs:13-24`). `CONFIRMED_BY_CODE`.
4. **Generic `Repository<T>`** (`Repositories/Repository.cs:34-49`) returns raw entities with no Include → silent N+1 on navigation access. `CONFIRMED_BY_CODE`.
5. **Finance payment-plan sweep** — per-installment `JournalEntries.AnyAsync` + per-account `AccountAsync` inside loop (`FinanceModuleService.cs:558-587`). `CONFIRMED_BY_CODE`.
6. Positive: `IdentityRepository.cs:35-38` uses 4-level Include correctly.
