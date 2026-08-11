# AegisCore DOMAIN_MAP.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.

Domain decomposition of the AegisCore modular monolith. Entities live flat in `AegisCore.Domain/Entities/` (601 classes, 173 files). Services live in `AegisCore.Infrastructure/` by module folder. Controllers in `AegisCore.API/Controllers/`; finance minimal-API groups in `AegisCore.API/Finance/*.cs`.

## Domain → module folder → controllers → entity files

| Domain | Infrastructure module folder | Key controllers / endpoints | Entity source files |
|---|---|---|---|
| **Student lifecycle** | `StudentLifecycle/`, `Student/` | `StudentPortalController`, `StudentLifecycleController`, `StudentDocumentsController`, `/api/student/v2/*` | `Student.cs`, `StudentLifecycleEntities.cs`, `ForeignStudentEntities.cs`, `MentalHealthEntities.cs` |
| **Admissions** | `Admissions/`, `CatalogIngestion/` | `S2AdmissionsController`, `AdmissionsCrmController`, `/api/admissions*`, `/api/catalog/ingest` | `AdmissionsEntities.cs`, `DataImportEntities.cs`, `CatalogIngestionEntities.cs` |
| **Faculty** | `Hr/` (VisitingFaculty), `Faculty/` | `FacultyCoursesController`, `FacultyGradeEntryController`, `FacultyObeController`, `/api/hr/v2/faculty` | `Faculty.cs`, `VisitingFacultyEntities.cs`, `CoreErpEntities2.cs` |
| **Registrar / Academics** | `Foundation/`, `Registrar/` (via controllers) | `TimetableController`, `CampusController`, `CurriculumController`, `DegreeAuditController` | `Batch.cs`, `DegreeProgram.cs`, `DegreeAudit.cs`, `FoundationEntities.cs`, `CurriculumManagementEntities.cs` |
| **Course registration** | `Registration/` | `StudentRegistrationController`, `/api/registration` | `Course.cs`, `Enrollment.cs`, `GovernanceEntities.cs` |
| **Timetable** | `Scheduler/` | `TimetableController`, `/api/scheduler` | `Timetable.cs`, `SchedulerEntities.cs` |
| **Attendance** | `Repositories/AttendanceRepository`, `Attendance/` | `AttendanceController`, `StudentAttendanceController` | `Attendance.cs`, `CoreErpFinanceHrSis.cs` |
| **Examinations** | `Examination/` (via controllers) | `S3ExaminationController`, `ExamController`, `ConvocationsController`, `ProctoringController` | `ExaminationEntities.cs`, `ExamSchedule.cs`, `ExamSeating.cs`, `PlatformEntitiesP111_120.cs` |
| **Grades** | `Grading/` (Application), `Governance/` | `GradeApprovalDiscoveryController`, `FacultyGradeEntryController`, `GradeAppeals` | `Result.cs`, `ExaminationEntities.cs` (CourseMark/CourseGrade/GradeScale), `GovernanceEntities.cs` (GradeOverrideRequest) |
| **Grade change / approval** | `Governance/` | `GradeApprovalDiscoveryController` (`/api/grade-approvals`), approval review endpoints | `MarkModeration` (in `ExaminationEntities.cs`), `GradeOverrideRequest` |
| **OBE / CLO/PLO** | `Obe/` | `S5ObeController` (`/api/obe/v2`), `OBEController`, `StudentObeController`, `FacultyObeController` | `CoreErpEntities1.cs`, `CloPlomapping.cs` |
| **Finance** | `Finance/` (largest module) | `FinanceController`, `FinanceWebhookController`, `FinanceModuleEndpoints` (`/api/finance/v2` ~130 endpoints, `/api/auditor`), `FinanceAudit*` | `FinanceEntities.cs`, `FinanceModuleEntities.cs`, `FinanceAccountingEngineEntities.cs`, + 20 more `Finance*.cs`, `CoreErpFinanceHrSis.cs` |
| **HR** | `Hr/`, `Payroll/` | `HrController`, `/api/hr/v2/faculty` | `CoreErpFinanceHrSis.cs`, `PayrollEntities.cs`, `FinancePayrollEntities.cs` |
| **Welfare** | `StudentLifecycle/` | `/api/student/v2/mental-health`, `StudentLifecycleController` | `MentalHealthEntities.cs` |
| **Cafeteria** | `Campus/CafeteriaService` | `CafeteriaController`, `/api/campus/v2/cafeteria` | `CafeteriaEntities.cs`, `CafeteriaExtendedEntities.cs` |
| **Documents** | `DocumentVault/`, `ProfilePictures/` | `DocumentVaultController`, `StudentDocumentsController`, `ProfilePictureController`, `StudentProfileMediaController` | `DocumentVaultEntities.cs`, `ProfilePictureRecord.cs` |
| **Reporting** | `Finance/` (report builders) | `/api/finance/v2/reports/*` (CSV/XLSX/PDF/XBRL), `BiController` | Finance entities |
| **PQC / signing** | `Security/Pqc/` | `FacultyObeController` (signed reports), `FinanceModuleEndpoints` (signed docs), `LedgerVerifier` exe | `FinancePqcKey`, `FinanceLedgerSignature`, `FinanceAuditSeal` |
| **QR verification** | (QRCoder in Application) | `StudentDocumentsController.verify`, `FinanceModuleEndpoints` | `Transcript.VerificationHash`, document entities |
| **Audit** | `Security/` (AuditLogService, LoginAuditService) | `AuditController` (UI), `/api/finance/v2/audit/*` | `AuditLog.cs`, `LoginAuditRecord.cs`, `AccessAuditLog.cs`, `ConfigAuditLog.cs` |
| **Multi-tenancy** | `TenantContextMiddleware`, `AegisDbContext` | All | `WizardSession.cs` (Tenant), `Common/ITenantScoped` |
| **RBAC/ABAC** | `Security/` (Casbin, Abac, IdentityRepository) | `IdentityController`, `ScimController`, `SamlController`, `LoginAuditController` | `User.cs`, `Role.cs`, `Permission.cs`, `UserRole.cs`, `RolePermission.cs`, `SsoHitlEntities.cs` |
| **License** | `License/`, `API/Background/` | `LicenseController`, `/api/admin/licenses` (platform) | `LicenseState.cs`, `PlatformEntitiesP93_98.cs` |
| **Governance / Overrides** | `Governance/` | `GovernanceController`, `ConfigCenterController`, `RuleEngineController` | `GovernanceEntities.cs`, `PolicyConfig` |
| **Accreditation / HEC** | `Governance/Accreditation*` | `AccreditationController`, `ComplianceController`, `S5ObeController.Hemis` | `AccreditationEntities.cs`, `CoreErpEntities1.cs`, `Compliance*.cs` |
| **Campus operations** | `Campus/`, `CampusOperations/`, `Transport` | `CampusController`, `CampusOperationsController`, `/api/transport`, `/api/campus/v2/*` | `Campus.cs`, `CoreErpEntities3.cs`, `TransportEntities.cs`, `SecurityIncident.cs`, `Visitor.cs` |
| **Digital twin / AI** | `AI/`, `DigitalTwin/` | `DigitalTwinController`, `/api/admin/ai-operations`, `/api/ai/analytics`, `VoiceController`, `PlagiarismController` | `PlatformEntitiesP93_98.cs` (AI), `DefaultPredictionEntities.cs` |
| **Communications** | `Workflow/` (Email/WhatsApp/SMS), `Platform/` | `CommunicationsController` | Notification entities |

## Domain cross-cutting services

| Service | File | Purpose |
|---|---|---|
| Tenant resolution | `AegisCore.API/Middleware/TenantContextMiddleware.cs`, `Infrastructure/TenantSessionInterceptor.cs` | JWT claim → Items["TenantId"] → EF filter + Postgres GUC |
| Audit | `Security/AuditLogService.cs`, `Foundation/FoundationServices.cs:158` | Structured audit entries, PQC-sealed finance audit |
| License | `License/LicenseService.cs` + `API/Middleware/LicenseLockoutMiddleware.cs` | Daily heartbeat + per-request lockout check (see LICENSE section) |
| PQC | `Security/Pqc/*` | SLH-DSA / ML-DSA signatures, ML-KEM hybrid transport, key ceremony, signing queue |
| Storage abstraction | `Cloud/*`, `Foundation/StorageProviders.cs` | Local / S3 / R2 / Azure / GCP / on-prem via providers |
| Casbin | `Security/CasbinServiceExtensions.cs` | In-memory RBAC enforcer (role, path, method) |
| ABAC | `Security/Abac/AbacPolicyService.cs` | Tenant-header equality check (minimal) |

## License enforcement — priority finding (confirmed)

- **Middleware**: `LicenseLockoutMiddleware` (`AegisCore.API/Middleware/LicenseLockoutMiddleware.cs:28`) runs on **every request** in installed non-demo mode and calls `ILicenseService.GetStatusAsync`.
- **DB per request**: `GetStatusAsync` → `CurrentAsync` → `db.LicenseStates.ToListAsync()` (`AegisCore.Infrastructure/License/LicenseService.cs:238-242`). **Full table load, no filter, no cache, every request.** Confirmed by direct inspection.
- **External call**: only in `SendHeartbeatAsync` (daily 08:00 background, `LicenseHeartbeatHostedService.cs`); timeout default 10s; two consecutive failures → global lockout.
- **Classification**: `LIKELY_BOTTLENECK` (DB round trip per request; severity depends on LicenseStates row count and request rate). Mitigation is a cheap cached/signed lease check. See `ARCHITECTURE_RISKS.md` #1 and `PERFORMANCE_MAP.md`.
