# AegisCore API_CATALOG.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
Backend is ASP.NET Core 10 (`AegisCore.API`). Auth default: `FallbackPolicy = RequireAuthenticatedUser` (everything requires auth unless `[AllowAnonymous]`).

Legend: `[A]` = `[Authorize]`; `[R=...]` = `[Authorize(Roles=...)]`; `[Anon]` = `[AllowAnonymous]`.

## 1. Auth / Identity

| Method + Route | Class | Auth | Notes |
|---|---|---|---|
| POST `/api/auth/login` | `AuthController:26` | `[Anon]` | Email/password → access+refresh tokens (15 min / 14-day family in Redis) |
| POST `/api/auth/refresh` | `AuthController:78` | `[Anon]` | Rotates refresh token; reuse → 401 |
| POST `/api/auth/logout` | `AuthController:136` | `[Anon]` | Revokes family |
| GET `/api/session` | `AuthController` (implicit) | `[A]` | Identity used by portal shell |
| GET/POST `/api/identity/*` | `IdentityController:14` | `[A]`, mutating `[R=Admin,SuperAdmin]` | Users/roles/permissions CRUD |
| `scim/v2/*` | `ScimController:14` | `[R=Admin,SuperAdmin]` | SCIM provisioning |
| `api/identity/saml/*` | `SamlController:12` | `[R=Admin,SuperAdmin]` | SAML config |
| `/api/u14/*` | `SsoHitlController:10` | `[A]`, some `[Anon]` | Multi-tenant SSO, 2FA, HITL exam pipeline |
| `/api/security/*` | `LoginAuditController:12` | `[R=Admin,SuperAdmin]` | Login audit, GeoIP, anomaly |

## 2. Licensing

| Method + Route | Class | Auth |
|---|---|---|
| GET `/api/license/status` | `LicenseController:19` | `[Anon]` |
| POST `/api/admin/license/activate` | `LicenseController:23` | `[R=SuperAdmin]` |
| POST `/api/admin/license/override` | `LicenseController:31` | `[R=SuperAdmin]` |
| POST `/api/license/heartbeat` | `LicenseController:45` | `[Anon]` (static token) |
| `/api/admin/licenses/*` | `PlatformControllers.cs:31` (ExtendedPlatform) | `[R=Admin,SuperAdmin]` (marketplace license issuance, separate service) |

## 3. Student portal

| Route | Class | Auth |
|---|---|---|
| `/api/student-portal` (me/transcript/timetable/exams/obe/attendance/finance) | `StudentPortalController:21` | `[R=Student,SuperAdmin,Admin]`; some `[Anon]` (verify) |
| `/api/student-portal/registration` | `StudentRegistrationController:15` | `[R=Student,SuperAdmin,Admin]` |
| `/api/student-attendance` (history/check-in) | `StudentAttendanceController:15` | `[R=Student]` |
| `/api/student-documents` (transcript official/download/verify) | `StudentDocumentsController:17` | generate/download `[R=Student,...]`; verify `[Anon]` |
| `/api/student-profile-media` | `StudentProfileMediaController:12` | `[R=Student,...]` |
| `/api/student-portal/obe` | `StudentObeController:13` | `[R=Student,...]` |

## 4. Faculty / Academic

| Route | Class | Auth |
|---|---|---|
| `/api/courses` (my courses) | `FacultyCoursesController:12` | `[A]` |
| `/api/faculty-grade-entry` (entry, OCR upload, import preview/commit, template.csv, approvals/:id/review) | `FacultyGradeEntryController:18` | `[R=Faculty,HOD,ExamController,Admin,SuperAdmin]` |
| `/api/grade-approvals` (HOD/Exam queue) | `GradeApprovalDiscoveryController:13` | `[R=HOD,ExamController,Admin,SuperAdmin]` |
| `/api/attendance` (roster/mark) | `AttendanceController:14` | `[R=Faculty,HOD,Admin,SuperAdmin]` |
| `/api/timetable` | `TimetableController:14` | `[R=Faculty,HOD,Registrar,Admin,SuperAdmin]` |
| `/api/examination` (incl. `transcripts/verify` `[Anon]`) | `S3ExaminationController:13` | `[R=Faculty,ExamController,HOD,Admin,SuperAdmin]` |
| `/api/obe/v2` (CIS, CLO/PO attainment, HEMIS) | `S5ObeController:14` | `[R=Faculty,OBECoordinator,Dean,Registrar,Admin,SuperAdmin]` |
| `/api/obe/faculty` (signed reports) | `FacultyObeController:20` | `[A]` |
| `/api/obe` (legacy) | `CoreErpControllers.cs:28` | `[A]` |
| `/api/exam`, `/api/research`, `/api/library`, `/api/asset`, `/api/transport`, `/api/hostel` | `CoreErpControllers.cs` | `[A]` |
| `/api/degree-audit` | `DegreeAuditController:13` | `[R=Admin,Registrar,SuperAdmin]` |
| `/api/curriculum`, `/api/curriculum-governance`, `/api/classes`, `/api/catalog*` | Platform/Extended controllers | `[A]` |
| `/api/grade-appeals*`, `/api/grade-distributions*`, `/api/freezes*`, `/api/prerequisite-conflicts*` | `ExtendedPlatformControllers2.cs` | `[A]` |

## 5. Finance (minimal-API, `AegisCore.API/Finance/FinanceModuleEndpoints.cs`)

- `/api/finance/v2` group — `.RequireAuthorization()` — ~130 endpoints: dashboard (executive/operations/department), accounting standards, reports (`signed-csv/xlsx/pdf/xbrl/ixbrl`), regulatory, vouchers (CRUD/decision/attachments/verification), fee-demands, payment plans, security deposits, fixed assets, petty cash, library, utilities, audit, vendors (+documents/sanctions), procurement, payroll, treasury, grants, insurance, inventory, fleet, investments, tax, gateways (+health-check), bank feeds, foreign currency, payments/request-to-pay.
- `/api/auditor` group (line 1700) — **opaque token auth** (`X-AegisCore-Auditor-Token`), not JWT: session, controls, samples, ledger, evidence.
- `/api/finance` (controller) — `[R=Student,Finance,CFO,Admin,SuperAdmin]`: fee-structures, dues, payments, scholarship, pifra XML, variance, requisitions, vendors.
- `/api/webhooks/{tenantId}/{gatewaySlug}` — `FinanceWebhookController:17` — **anonymous** but HMAC-verified + optional PQC signature, idempotent via Redis.
- `/api/finance/v2/predictions` — `DefaultPredictionEndpoints.cs:10`.
- `/api/audit/dashboard`, `/api/finance/v2/audit/*` (SoD rules, engagements, SOC2 evidence).

## 6. HR / Ops / Welfare / Campus

| Route | Class | Auth |
|---|---|---|
| `/api/hr` | `HrController:16` | `[R=Employee,Faculty,HOD,HR,HRManager,Finance,Admin,SuperAdmin]`; one `[Anon]` |
| `/api/hr/v2/faculty` | `VisitingFacultyEndpoints.cs:9` | `[A]` |
| `/api/campus`, `/api/campus/transfer` | `CampusController:13` / `TransferController:105` | `[A]`; admin `[R=Admin,SuperAdmin]` |
| `/api/campus-operations` | `CampusOperationsController:10` | `[A]` |
| `/api/campus/v2/*` (elections/cafeteria/transport) | `CampusModuleEndpoints.cs` | `[A]` |
| `/api/student/v2/*` (voice/achievements/mental-health/foreign) | `StudentLifecycleModuleEndpoints.cs` | `[A]` |
| `/api/compliance` | `ComplianceController:14` | `[R=Auditor,Finance,Finance Controller,CFO,Admin,SuperAdmin]` |
| `/api/governance` | `GovernanceController:12` | `[A]` |
| `/api/cafeteria`, `/api/personality`, `/api/charts`, `/api/ocr`, `/api/plagiarism*`, `/api/proctoring*`, `/api/defaulter-*`, `/api/visiting-faculty`, `/api/readiness`, `/api/achievements`, `/api/mental-health`, `/api/voice`, `/api/board`, `/api/elections` | `ExtendedPlatformControllers4.cs` etc. | `[A]` |

## 7. Platform / Admin / Integration

| Route | Class | Auth |
|---|---|---|
| `/api/config`, `/api/rules`, `/api/forms`, `/api/i18n` (anon read) | `ConfigCenterController`, `RuleEngineController`, `FormsController`, `I18nController` | `[A]`; admin `[R=Admin,SuperAdmin]` |
| `/api/admin/dr`, `/api/admin/replication`, `/api/admin/sync-lag` | DR/Replication/SyncLag controllers | `[R=Admin,SuperAdmin]` |
| `/api/admin/queues`, `/api/events`, `/api/admin/diagnostics`, `/api/admin/architecture`, `/api/admin/health-analyzer`, `/api/admin/integrations`, `/api/admin/benchmarks`, `/api/admin/ai-operations`, `/api/admin/prompts`, `/api/admin/guardrails`, `/api/admin/zero-trust`, `/api/admin/hardcoding-audit` | `ExtendedPlatformControllers.cs` | `[A]`/`[R=Admin,SuperAdmin]` |
| `/api/developer/keys`, `/api/developer/webhooks` | `DeveloperControllers.cs` | `[R=Admin,SuperAdmin]` |
| `/api/developer/info` | `DeveloperControllers.cs:147` | `[Anon]` |
| `/api/marketplace`, `/api/official-marketplace` | `MarketplaceController`, ExtendedPlatform | `[A]`; writes `[R=Admin,SuperAdmin]` |
| `/api/branding` | `BrandingController:8` | `[A]`; public read `[Anon]` |
| `/api/communications` | `CommunicationsController:13` | `[A]`; one `[Anon]` |
| `/api/me/profile`, `/api/profile-pictures`, `/api/document-vault`, `/api/data-import` | SelfProfile/ProfilePicture/DocumentVault/DataImport | `[A]` |
| `/api/setup`, `/api/wizard` | `SetupController`, `WizardController` | `[Anon]` (setup mode) |
| `/api/microsoft-ecosystem` | `MicrosoftEcosystemController:11` | `[A]` |
| `/api/notifications`, `/api/feature-flags`, `/api/scheduler`, `/api/import`, `/api/export`, `/api/command-center` | `PlatformControllers.cs` | `[A]`; feature-flags `[R=Admin,SuperAdmin]` |

## 8. SignalR hubs

| Hub | File | Purpose |
|---|---|---|
| `/hubs/statemachine` | StateMachineHub | group subscribe/notify |
| `/hubs/notifications` | NotificationHub | joins `user-{id}` group |
| `/hubs/finance` | FinanceHub | joins `finance-{tenantId}` group; publisher whitelists `voucher-posted`, `payment-settled`, `voucher-updated` |

## 9. Cross-cutting observations

- `[RequirePermission(module, action, resource)]` attribute + `PermissionPolicyProvider` + `PermissionAuthorizationHandler` exist but are **unused** (grep confirmed zero usages). RBAC is role-string based.
- Authorization is `[Authorize(Roles=...)]` on the vast majority of endpoints; `PermissionAuthorizationHandler` would do a per-request DB query if ever enabled (UserRoles→RolePermissions→Permission).
- Casbin enforces in-memory role/path/method after `UseAuthorization`; SuperAdmin/admin bypass to `/*`.
- ABAC is only a tenant-header equality check (`X-Tenant-Id` vs claim), not a policy engine.
- Several endpoints use `IgnoreQueryFilters()` (e.g., `AuthController.cs:43`, `FinanceWebhookController.cs:53`) — cross-tenant reads must be reviewed for IDOR potential (see SECURITY_BOUNDARIES.md).
