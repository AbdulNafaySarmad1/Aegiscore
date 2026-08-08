# AegisCore Codebase Knowledge

> Canonical tree of reference: `FILE_TREE.md` (auto-generated, do not modify).
> This document is the shared onboarding contract for backend and frontend agents.
> Scope of truth: the ROOT-LEVEL source directories only (see §2). Sibling `aegiscore-*` directories are legacy duplicates (see §26).
> Confidence labels: **HIGH** (verified in code), **MEDIUM** (inferred from adjacent code/docs), **LOW** (documented but unverified). Unknowns explicitly marked `UNKNOWN / NEEDS VERIFICATION`.

---

## 1. Executive Architecture Summary

AegisCore is a **multi-tenant, modular-monolith university ERP** delivered as a monorepo.

- **Backend:** ASP.NET Core (`.NET 10`, C# 14) Web API at `AegisCore.API/` with Clean-Architecture-style layering: `AegisCore.Domain` → `AegisCore.Contracts` → `AegisCore.Application` → `AegisCore.Infrastructure` → `AegisCore.API`.
- **Frontend (active):** `AegisCore.Portal/` — Next.js **16.2** (App Router) + React **19** portal.
- **Database:** PostgreSQL 17 (`pgvector/pgvector:pg17`) via EF Core 10 with a single monolithic `AegisDbContext` (3,479 lines, ~400+ entities), global-query-filter multi-tenancy, and Postgres enums.
- **Cache/transport:** Redis 7 (rate limiting, sessions, queueing, PQC replay store).
- **Architecture decision record:** `docs/architecture/0001-modular-monolith.md`.

Request flow (production mode):

```
HTTP → [GlobalException, RequestLogging, RateLimiting, RateLimitAuth, AuthenticatedOriginPulls,
        DemoExpiry, HybridPqcTransport, SessionBinding] → CORS("CloudflarePages") → Authentication
→ [DemoIdentity, GlobalRateLimit, LicenseLockout, TenantContext] → Authorization
→ [Turnstile, Abac, CasbinAuthorization] → Controller / MinimalAPI → Application service → Repository → AegisDbContext → PostgreSQL / Redis / external
```

`AegisCore.API/Program.cs` is the single composition root: it branches on `InstallationStateService.HasPersistentMarker(...)`. Uninstalled ⇒ "setup mode" (registers only `AddCoreServices` + `AddSetupServices`, requires setup endpoints). Installed ⇒ full register of `AddInfrastructure`, `AddProductionServices`, `AddAuthenticationServices`, `AddBackgroundWorkers`.

Two `Program.cs`-wired endpoint styles coexist:
1. **Attribute-routed controllers** (`AegisCore.API/Controllers/*.cs`, 50 files, `[Route("api/...")]`).
2. **Minimal APIs** grouped into route-mapper extension classes in `AegisCore.API/Finance/*.cs` (`MapFinanceModuleEndpoints`, `MapDefaultPredictionEndpoints`, `MapVisitingFacultyEndpoints`, `MapAccreditationEndpoints`, `MapBoardPortalEndpoints`, `MapVoiceAssistantEndpoints`, `MapGamificationEndpoints`, `MapMentalHealthEndpoints`, `MapForeignStudentEndpoints`, `MapElectionEndpoints`, `MapCafeteriaEndpoints`, `MapTransportEndpoints`).

Backend is vast (roughly: 60+ `*Service`s, ~45 repositories, 90 EF migrations) with a long tail of platform/extension modules (P87–P127) beyond the core SIS/FMS/HRMS.

---

## 2. Repository Layout

Active (authoritative) root directories — **only these build/deploy**:

| Path | Role | Evidence |
|---|---|---|
| `AegisCore.API/` | Web API composition root, controllers, middleware, hubs, minimal-API mappers | entry, `Dockerfile`, `docker-compose.yml` build root |
| `AegisCore.Application/` | Application contracts (service interfaces, DTOs, commands) + some pure services | csproj references |
| `AegisCore.Contracts/` | Shared cross-layer contract interfaces (mirrors `Application/Common/Interfaces`) | csproj |
| `AegisCore.Domain/` | Entities (`Entities/` ~250 files), `Enums.cs`, `ITenantScoped` | csproj |
| `AegisCore.Infrastructure/` | EF `AegisDbContext`, migrations (90), repositories, service impls, cloud/security/workers | csproj |
| `AegisCore.Portal/` | **Active Next.js frontend** | `Dockerfile`, `ci.yml`, `docker-compose.yml` |
| `AegisCore.Mobile/` | **Active React Native (Expo) app** | docs `S13`, deployment |
| `AegisCore.Tests/` | xUnit test project (43 test classes) | `ci.yml` |
| `AegisCore.Setup/` | Blazor (Razor) standalone **setup-wizard UI** (separate deployable) | own csproj (net10.0) |
| `AegisCore.Web/` | Minimal Blazor shell / landing page (small, largely placeholder) | own csproj |
| `AegisCore.PluginSdk/` | Plugin contracts (packable NuGet `AegisCore.PluginSdk` 1.0.0) | csproj |
| `AegisCore.PluginCli/` | CLI tool `aegiscore-plugin` (packable dotnet tool) | csproj |
| `AegisCore.LedgerVerifier/` | Console verifier for the finance ledger hash-chain/PQC signatures (warnings-as-errors) | csproj, `Program.cs` |
| `AegisCore.Native/` | Native Android (Kotlin) + iOS (Swift) skeletons (3 files each) | tree |

Supporting top-level files: `AegisCore.slnx` (solution), `Directory.Build.props` (disables NuGet audit), `Dockerfile` (API image), `docker-compose*.yml`, `Dockerfile` + `nginx.conf` inside `AegisCore.Portal/`, `.github/workflows/{ci,deploy-cloud,deploy,release}.yml`, `k8s/*.yaml`, `deploy/`, `scripts/`, `docs/` (spec & reports), `md/` (prompt specifications), `formats/` (CSV templates), `manuals/` (HTML).

Legacy duplicates that are **NOT** built: `aegiscore-backend/` (581 tracked files — older full backend copy), `aegiscore-frontend/` (Vite React), `aegiscore-mobile/`, `aegiscore-mobile-app/`, `aegiscore-android/` (older mobile/native copies). See §26.

---

## 3. Technology Stack

### Backend (verified via `.csproj` files)
- .NET **10.0**, C# implicit usings, nullable enabled.
- EF Core **10.0** + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0 (`AegisCore.Infrastructure`, `AegisCore.API`).
- JWT auth: `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.0, `System.IdentityModel.Tokens.Jwt` 8.18.0.
- Policy/Casbin: `Casbin.NET` 2.21.2 (ABAC via `Casbin.NET` policy files).
- Redis: `StackExchange.Redis` 2.8.16.
- Validation: `FluentValidation` 11.x (Application), `FluentValidation.AspNetCore` 11.3.0 (API).
- Swagger: `Swashbuckle.AspNetCore` 6.7.3.
- Logging: `Serilog.AspNetCore` 9.0.0 + `Serilog.Sinks.PostgreSQL` 2.3.0.
- Metrics/health: `prometheus-net.AspNetCore` 8.2.1, `AspNetCore.HealthChecks.{NpgSql,Redis}` 9.0.0, `Microsoft.Extensions.Diagnostics.HealthChecks`.
- Rate limiting: `AspNetCoreRateLimit` 5.0.0.
- Problem details: `Hellang.Middleware.ProblemDetails` 6.5.1.
- MediatR **12.4.1**, AutoMapper 16.2.0, Quartz 3.13.0 (+ `Quartz.Extensions.Hosting`, `Cronos`).
- Cloud SDKs (Infrastructure): `AWSSDK.S3`, `AWSSDK.SecretsManager`, `Azure.{AI.DocumentIntelligence, Identity, ResourceManager.PostgreSql, Security.KeyVault.Secrets}`, `Google.Cloud.SecretManager.V1`, `Microsoft.Graph` 6.2.0, `NEST` (Elasticsearch) 7.17.5.
- Crypto: `BouncyCastle.Cryptography` 2.7.0, `Konscious.Security.Cryptography.Argon2` 1.3.1, `System.Security.Cryptography.Xml`.
- Utilities: `PdfSharpCore`, `SixLabors.ImageSharp`, `QRCoder`, `Lextm.SharpSnmpLib`.
- Misc infra: `Microsoft.Extensions.Http.Polly` (resilient HTTP), `Microsoft.Extensions.Configuration.Json`.

### Frontend (`AegisCore.Portal/package.json`)
- Next.js **16.2.12** (App Router, `output: "standalone"`), React **19.2.3**.
- `next-intl` **4.13.4** (i18n), `@microsoft/signalr` 10 (hubs), `@noble/post-quantum` 0.6.1 (client-side PQC verification for finance vouchers).
- Charts: `recharts` 3.10.1; motion: `framer-motion` 12.43.0; icons: `lucide-react`; maps: `d3-geo`.
- Dev/test: TypeScript 5, ESLint 9 (next config), Playwright 1.62.1, `sharp`/`postcss`/`minimatch` pinned via `overrides`.
- Styling: custom **CSS design system** in `src/app/globals.css` (CSS variables, light/dark themes, no Tailwind).

### Mobile (`AegisCore.Mobile/package.json`)
- Expo **57** / React Native, React 19, `@navigation/native-stack`, `@reduxjs/toolkit`, `@tanstack/react-query`, `axios`, `watermelondb` (offline SQLite), `expo-sqlite`, `expo-secure-store`, `expo-local-authentication`, `expo-notifications`, Jest, Detox.

### Database / services in Docker
PostgreSQL **17** w/ pgvector, Redis **7**, both on internal `backend` network; portal + api on `edge` and `backend`.

---

## 4. Solution / Project Dependency Map

`AegisCore.slnx` contains 9 projects (**backend API+libs only; Portal/Mobile are Node projects**):

```
AegisCore.Domain      (0 deps; EF Core.Abstractions only)
AegisCore.Contracts   → Domain
AegisCore.Application → Contracts
AegisCore.Infrastructure → Application, Contracts, Domain
AegisCore.API         → Application, Infrastructure, Domain, Contracts
AegisCore.Tests       → Application, API, Domain, Infrastructure
AegisCore.PluginSdk   (standalone, packs NuGet)
AegisCore.PluginCli   (standalone console, packs dotnet tool)
AegisCore.LedgerVerifier → Infrastructure
```

- `AegisCore.Setup` (`Web SDK`) → Application, Infrastructure (NOT in the `.slnx`).
- `AegisCore.Web` (Blazor shell) NOT in `.slnx`.
- The Contracts project is a **copy/mirror** of many Application `Common/Interfaces` (both define e.g. `IStudentRepository`). See §25 debt.

Verification commands: `dotnet build AegisCore.slnx`, `dotnet test AegisCore.Tests/AegisCore.Tests.csproj` (CI, net10.0, xunit, Moq, EF Sqlite for tests).

---

## 5. Backend Architecture

### 5.1 Composition root — `AegisCore.API/Program.cs`
- Kestrel hard limits (1000 conns, 50 MB body, 120s keep-alive).
- **Demo-mode guard:** throws if `Runtime:DemoMode=true` in `Production`.
- Serilog configured from appsettings with `SensitiveDataDestructuringPolicy` (`API/Logging/`).
- **Setup vs Production dual mode** (see §1).
- `AddCoreServices` (API/Extensions/ApiServicesExtensions.cs) likely registers MVC, Swagger, Serilog, CORS; `AddProductionServices` (ProductionServicesExtensions.cs) registers security middleware + real DI; `AddAuthenticationServices` adds JWT/Casbin/ABAC; `AddBackgroundWorkers` adds hosted services. `AegisCore.Infrastructure/InfrastructureServiceExtensions.cs` + per-module `*Extensions.cs` implement `AddInfrastructure`.

### 5.2 Dependency-injection pattern
- Interface-per-service registered via static extension classes per folder: `InfrastructureServiceExtensions.cs`, `DbContextServiceExtensions.cs`, module extensions (`CampusServiceExtensions.cs`, `FinanceModuleServiceExtensions.cs`, `IdentityServiceExtensions.cs`, `SearchServiceExtensions.cs`, etc.), and `AegisCore.Application/ServiceCollectionExtensions.cs`.
- Repositories implement interfaces from `Application/Common/Interfaces`; `UnitOfWork.cs` in Infrastructure.

### 5.3 Persistence pattern
- Single `AegisDbContext` (Infrastructure/AegisDbContext.cs) exposing ~450 `DbSet`s. One context, heavily denormalized by module groups with doc comments (HR, Research, OBE, Accreditation, SIS, Library, Labs, Campus Assets, Maintenance, Security, Transport, Hostel, Student Success, Career, Admissions CRM, BI, Workflow, ApiGateway, Marketplace, Identity, DR, Compliance, i18n, DegAudit, Forms, Digital Twin, Config, Rules, Platform P87–P127, Finance ~150 sets, Elections, Cafeteria, etc.).
- **Multi-tenancy:** every entity implementing `ITenantScoped` (adds `Guid TenantId`) gets a global query filter `e.TenantId == this.CurrentTenantId`. `CurrentTenantId` reads `HttpContext.Items["TenantId"]` set by `TenantContextMiddleware`. `TenantSessionInterceptor.cs` also manages tenant/audit columns on `SaveChanges`.
- **Postgres enums:** `modelBuilder.HasPostgresEnum<...>()` for ~40 enums (EnrollmentStatus, AttendanceStatus, ExamType, Designation, AccountType, ...). Additive enum changes require EF migrations.
- Migrations: `Infrastructure/Migrations/` — 90 files (`V1.0` baseline → `V2_2_UserRefreshNotification`; finance migrations `20260730xxxxx`–`2026080116xxxx`, platform `V2.0`). SQL baseline `V1.0_AegisCoreBaseline.sql` + per-migration `Designer.cs` + `AegisDbContextModelSnapshot.cs`.
- Auto-migrate on boot when `Database:AutoMigrate=true` (`ApplyDatabaseMigrationsAsync`).

### 5.4 Application layer
- Primarily **service-interface contracts** (`AegisCore.Application/*/I*Service.cs` + `*Contracts.cs` DTOs) implemented in Infrastructure — NOT a full CQRS/vertical-slice; MediatR is referenced by the Application project and used directly in a handful of places (e.g. finance command records in `API/Finance/FinanceModuleEndpoints.cs`, `FinanceContracts.cs`).
- Pure logic lives in Application too: `Grading/GradingSchemeResolver.cs`, `Grading/ResultService.cs`, `Maintenance/*` (`TicketService`, `WorkOrderService`, `SLAEngineService`, `PreventiveMaintenanceService`), `ResearchLabs/*`, `Security/*` (guard, incident, lost-found, vehicle-pass, visitor services).

### 5.5 Infrastructure services by module
`AegisCore.Infrastructure/` folders each contain a concrete service implementing the Application contract plus a `*Extensions.cs` DI registrar: AI, Admissions, ApiGateway, AssetInventory, Backup, Campus (incl. `ConsolidatedReportingService`), CampusOperations (+`ElectionService`), Compliance, Config (`TenantConfigService`), ConfigCenter, Curriculum, DataImport, DegreeAudit, Deployment (readiness, seed, health, alerts), DigitalTwin, DocumentVault, Examination (`GradingService`), Finance (~20 services), Forms, Foundation (storage providers), Governance (accreditation/board), Hr (+`PayrollService`, `VisitingFacultyService`), I18n, Identity (+`SamlService`), Installation, License, Marketplace, MicrosoftEcosystem, Obe, Onboarding (wizard + setup), Payments/Raast, Platform (5 `ExtendedPlatformServices` files), ProfilePictures, Replication, RuleEngine, Search, Security (audit, brute-force, jwt, casbin, PQC, upload validation), SsoHitl, StudentLifecycle (+foreign/gamification/mental-health/voice), Support, SyncLag, Workflow (state-machine, approvals, workflow engine, email, template seeder).

---

## 6. Domain Model

- Entities stored one-per-file OR grouped in bulk files (`CoreErpEntities1..4.cs`, `PlatformEntitiesP*.cs`, dozens of `Finance{X}Entities.cs`, `CommunicationEntities.cs`, `NotificationEntities.cs`, `CafeteriaEntities.cs` + `CafeteriaExtendedEntities.cs`).
- Notable aggregate areas (verified DbSets):
  - **Core SIS:** Department, Batch, Student, Faculty, Course, CourseAssignment, Enrollment, Attendance, ExamSchedule, ExamSeating, Result, Timetable.
  - **Identity/RBAC:** User, Role, Permission, RolePermission, UserRole, SamlConfiguration, UniversitySsoProfile, UserSecondFactor, SsoSession.
  - **Finance:** ChartOfAccount, JournalEntry(+Line), FiscalPeriod/Year, FinanceVoucher(+Line/Approval/ApprovalRule/Attachment/Reversal/Sequence), CostCenter, FeeStructure/Component, StudentFeeDue, StudentFeePayment, FeeDemand/Installment, FinancePayment(+)Plan(+Configuration), PaymentGatewayConfiguration/Event, ExchangeRateSnapshot, + ~150 `Finance{Subdomain}Entities` (payroll settlement, treasury, grants/scholarships, assets, insurance, inventory, fleet, investments, tax, petty cash, library, vendor/procurement POs, regulatory, audit seals/SoD, PQC keys/ceremonies, ledger signatures/hash chain, report profiles/artifacts).
  - **OBE/Accreditation:** ~30 sets (UniversityMission, PEO, PO, CLO, CLOPOMapping, Rubric(+Criterion), CLOAttainment, POAttainment, GapAnalysis, ContinuousImprovementPlan, AccreditationBody/Cycle/Evidence/Report, SelfStudyReport/SSRSection, FacultyCV, ReadinessScore…).
  - **Campus ops:** Asset(+Category/Transfer/Maintenance/Disposal/Verification/VerificationCycle), Buildings/Floors/Rooms, Visitor/VehiclePass/GateEntry/SecurityGuard/Shift/Round, TransportRoute/Vehicle/Driver/…, Hostel* (~15), Library (~12), IoT/DigitalTwin (~6), Maintenance/WorkOrders (~10), Cafeteria (~12), Elections (~6).
  - **Platform/extension (P87–P127):** QueueDefinition/Job, DomainEvent(+SchemaVersion), Diagnostics (SlowQuery, findings), ServiceRegistration/Endpoint/HealthScore, Integrations, Benchmarks, AIProviders/Models/TokenUsage/Prompt(+Version/ABTest)/Guardrail, License(+Heartbeat), SupportTicket, ReleaseDownload/UpdateDeployment, OfficialModule, Misconduct, FacultyWorkload, DegreeVerification, RegistrationWindow, CurriculumProposal, GradeAppeals/Evidence, GradeDistribution/Anomaly, PlagiarismScan(+Api), Convocation(+Eligibility/Certificate/Attendance), SemesterFreeze(+Document), Proctoring(+Records/Events), PrerequisiteConflict/ChangeImpact, DefaulterRiskScore/Intervention/CashFlowForecast, VisitingFaculty(+Profile/SlotMatch/Contract), Readiness(+Gap/Alert), AchievementBadge/StudentBadge/DeansList/Leaderboard/Roadmap, MentalHealth(+MandatoryReport/WellnessCheckIn/CounselorProfile), ForeignStudentRegistry/Visa*/Mora, VoiceSession, ExecutiveDashboard/BoardMeetingPack/BoardAlert/StrategicPlanGoal/BenchmarkComparison.
- **Value objects/enums:** `Domain/Enums.cs` (single shared enums site).
- **Cross-cutting:** `ITenantScoped` (`Domain/Common/ITenantScoped.cs`), `FeatureFlag`, `PolicyConfig`, `AcademicYear/Semester/Holiday`, `PersonalityConfig`, `ChartConfiguration`, `OcrProviderConfig`.
- Duplicate/overlapping domain concepts exist — see §26.

---

## 7. Database / EF Core Architecture

- Provider: **Npgsql EF Core 10** (`AegisCore.Infrastructure/AegisDbContext.cs`, `AegisDbContextFactory.cs` for design-time, `DbContextServiceExtensions.cs` for DI).
- One context + global tenant query filters + Postgres enum conversions (§5.3). No second DbContext; the app is a single-database monolith (Finance ledgers used to be isolated — see `20260730180000_LedgerIsolation.cs` — then consolidated back into `AegisDbContext`).
- `UnitOfWork.cs` wraps commits; repositories (`Infrastructure/Repositories/*.cs`, ~45 files with `Repository.cs` base + `IUnitOfWork.cs`).
- Finance integrity features in SQL: append-only ledger guards (`20260731143000_FinanceLedgerAppendOnlyGuards.cs`), voucher posting guards (`...VoucherPostingGuards`), ledger **hash chain** (`FinanceLedgerHashChain`), PQC key ceremonies, audit seals, idempotent webhook events (`FinanceWebhookIdempotency`), performance indexes (`FinancePerformanceIndexes`, `FinanceGatewayEventIndex`).
- Backup/DR: `Backup/` services + `BackupConfiguration`/`BackupRecord`/`ReadReplicaConfig` entities; `Replication/*` for replicas & scale events; `SyncLag/*` for delayed-write window.
- Demo/seed data: `Infrastructure/Deployment/DemoDataSeeder.cs`, `DemoSeeder.cs`, `SeedPipeline.cs`; SQL dumps `scripts/demo-university-seed.sql`, `scripts/wipe-demo-data.sql`; `docker/postgres/init/01-init.sql`.
- SQLite used only by `AegisCore.Tests` (in-memory EF testing).

---

## 8. API Architecture

- **Base convention:** `[ApiController]` + `[Route("api/<area>")]` + `[Authorize]` per controller; a handful `[Authorize(Roles = "Admin,SuperAdmin")]`. Controllers are thin: DI a service, query-string/body params, return `Ok(_service.MethodAsync(...))`.
- **50 controller files** (`AegisCore.API/Controllers/`) — route map includes (sample, non-exhaustive): `api/branding`, `api/profile-pictures`, `api/accreditation`, `api/achievements`, `api/admin/{ai-operations,architecture,benchmarks,diagnostics,guardrails,hardcoding-audit,health-analyzer,integrations,licenses,prompts,queues,replication,sync-lag,updates,zero-trust}`, `api/admissions-crm`, `api/ai/analytics`, `api/assets`, `api/bi`, `api/board`, `api/cafeteria`, `api/career`, `api/catalog/ingest`, `api/charts`, `api/classes`, `api/command-center`, `api/convocations`, `api/curriculum-governance`, `api/customer`, `api/data-import`, `api/defaulter-ml`, `api/defaulter-prediction`, `api/degree-verification`, `api/document-vault`, `api/elections`, `api/events`, `api/exam`, `api/export`, `api/faculty-workload`, `api/feature-flags`, `api/fee-defaulter`, `api/foreign-students`, `api/freezes`, `api/grade-appeals`, `api/grade-distributions`, `api/hostel`, `api/import`, `api/library`, `api/mental-health`, `api/misconduct`, `api/notifications`, `api/obe`, `api/ocr`, `api/official-marketplace`, `api/personality`, `api/plagiarism-api`, `api/readiness`, `api/replication`, `api/rule-engine`, `api/saml`, `api/scim`, `api/search`, `api/setup(+enhanced)`, `api/sso-hitl`, `api/student-lifecycle`, `api/support`, `api/sync-lag`, `api/timetable`, `api/transport`, `api/voice`, `api/visiting-faculty`, `api/wizard`, plus finance `api/finance/v2/...` (minimal API), `api/default-prediction`, `api/attendance`, `api/auth`, `api/identity`, `api/i18n`, `api/login-audit`, `api/config-center`, `api/curriculum`, `api/degree-audit`, `api/digital-twin`, `api/disaster-recovery`, `api/forms`, `api/governance`, `api/hr`, `api/marketplace`, `api/microsoft-ecosystem`, `api/campus`, `api/campus-operations`, `api/compliance`, `api/communications`, `api/branding`.
- **Auth endpoints:** `AuthController`: `POST api/auth/login`, `POST api/auth/refresh`, `POST api/auth/logout`. `IdentityController` (base likely `api/identity`): user CRUD + `roles`,`permissions`,`matrix`, assign/remove role, `check-permission`.
- **Minimal API group:** `MapFinanceModuleEndpoints` = `group "api/finance/v2"` exposing `/dashboard`, `/dashboard/executive`, `/dashboard/operations`, `/dashboard/department/{id}`, `/student-portal/{id}`, `/configuration`, `/accounting/*` (standards, restatements, consolidation), `/reports/*` (trial-balance, cash-position, SFP, P&L, receivables-aging) with **signed output** (`/reports/{type}/signed-{json,csv,xlsx,pdf,xbrl,ixbrl}`) verified via PQC + R2, `/regulatory/*` schemas/generations, `/report-profiles`, `/security-posture`, `/security/ledger-archive`, `/security/transport-key` (HybridKEM), `/security/key-ceremonies*`.
- **Health:** `/health` (plain), `/health/live`, `/health/ready` (`DeploymentReadinessChecker`), `/health/installation`, `/health/deep` (POSTGRES+REDIS+resources; **RequireAuthorization**), `/health/detail` (`MapHealthChecks`, RequireAuthorization), `/metrics` (Prometheus, anonymous).
- **Session/self:** `GET /api/session` (RequireAuthorization) returns `{userId, tenantId, studentId, departmentId, name, roles}`.
- **Swagger** at `/swagger`.
- **SignalR hubs:** `/hubs/statemachine` (`StateMachineHub`), `/hubs/notifications` (`NotificationHub`), `/hubs/finance` (`FinanceHub`).
- Rate limiting & global middleware order: see §1. Additional middleware classes (RequestLogging, AuthenticatedOriginPulls, HybridPqcTransport, SessionBinding, Turnstile, Abac) all defined in one file `AegisCore.API/Middleware/InfrastructureMiddleware.cs`.

---

## 9. Authentication & Authorization

- **Primary:** JWT Bearer (`AddAuthenticationServices`). `Infrastructure/Security/JwtTokenService.cs` issues access + refresh (refresh-token entities migrated in `V2_1_UserRefreshTokens`, `V2_2_UserRefreshNotification`). Claims include `tenant_id`/`tid`, `department_id`, `student_id`, role claim (`ClaimTypes.Role`), nameidentifier.
- **Credentials:** Argon2 password hashing (`Security/PasswordHashingService.cs` via Konscious.Argon2), Brute-force protection (`Security/BruteForceProtectionService.cs` + Redis), login audit (`Security/LoginAuditService.cs`, `LoginAuditRecord` entity, `LoginAuditController`).
- **RBAC model:** `User`–`UserRole`–`Role`–`RolePermission`–`Permission`; `RequirePermissionAttribute` + `PermissionAuthorizationHandler` (`API/Authorization/`) for per-permission enforcement; permission matrix endpoint in IdentityController. Banner roles used in the frontend include SuperAdmin, VC, Admin, Registrar, ExamController, Finance/CFO/Finance Controller, HR/HRHead/HRManager, IT/ITHead, HOD, Dean, Faculty, Employee, Student.
- **Casbin ABAC:** `Infrastructure/Security/CasbinServiceExtensions.cs` + `API/Middleware/CasbinAuthorizationMiddleware.cs` + `RequirePermissionAttribute`; policy store persists to DB; `Infrastructure/Security/Abac/AbacPolicyService.cs` + `AbacMiddleware` for attribute-based decisions.
- **SSO/SAML/SCIM:** `API/Controllers/{SamlController,ScimController,SsoHitlController}.cs`; `Infrastructure/Identity/SamlService.cs`; `Infrastructure/SsoHitl/SsoHitlServices.cs` (SAML→JWT HITL flows, `UniversitySsoProfile`, `SsoSession`, `UserSecondFactor`).
- **Minimum-verification demo path:** `DemoIdentityMiddleware` + `DemoExpiryMiddleware` supply a fake identity in demo mode (see §24 for the frontend gap). Production rejects demo.
- **API keys:** `Infrastructure/ApiGateway/ApiKeyService.cs` + `ApiKey`/`WebhookSubscription` entities for partner/webhook auth.
- **CSRF/browser protections:** cookie-based auth (same-site) via frontend, `AuthenticatedOriginPullsMiddleware` (Cloudflare), `TurnstileMiddleware` (Cloudflare Turnstile), `SessionBindingMiddleware` (binds JWT to client binding data).

---

## 10. Security Architecture

- **Post-quantum cryptography** (`Infrastructure/Security/Pqc/`): `HybridKemService` (KEM transport keys), `PqcSignatureService` (dilithium-style signatures), `PqcCryptographyService`, `PqcKeyCeremonyService` (custodian approvals, rotation via appsettings `Pqc` section), `PqcSigningQueue`, `HybridPqcReplayStore` (Redis). Exposed via `HybridPqcTransportMiddleware` (edge proof HSK, config `HybridPqcTransport__Enforce`+`EdgeProofKeyBase64`), `/api/finance/v2/security/transport-key`, and signed finance reports.
- **Finance ledger integrity:** append-only ledger, hash chain (`FinanceLedgerHashChain`), audit seals (`AuditSealService`, `FinanceAuditSeal`), SoD rules (`FinanceContinuousControlService`, `FinanceSodRule`), webhook idempotency, PQC-signed ledger blocks verified by `AegisCore.LedgerVerifier` console.
- **Vault/secret management:** `Infrastructure/Cloud/Infisical/InfisicalSecretService.cs` + `VaultConfigurationExtensions.cs`; secret providers: FILE/AWS/Azure/GCP/Infisical (`AEGISCORE_SECRET_PROVIDER` env).
- **Encryption at rest:** `AEGISCORE_ENCRYPTION_KEY` (backups via `BackupEncryptionService`, `EncryptedAccountReference` columns), ASP.NET Data Protection appliances (`DataProtection__KeyPath`).
- **Uploads:** `Security/UploadValidationService.cs` (validate/doc-vault uploads).
- **Logging hygiene:** `SensitiveDataDestructuringPolicy`; audit logs (`AuditLog`/`AuditLogService`, `AccessAuditLog`, `LoginAuditRecord`).
- **License/demo enforcement:** `LicenseLockoutMiddleware`, `LicenseService` + heartbeat (`LicenseHeartbeatHostedService`, `AutoLockBackgroundService`), `DemoExpiryMiddleware`.
- **Infra hardening:** `upload` size caps, Kestrel limits, `SetupRequiredMiddleware`, `GlobalExceptionHandlerMiddleware` + `ValidationExceptionMiddleware`, global + per-auth rate limiting.
- Rate limits, CORS restricted to `CloudflarePages` policy; HSTS/HTTPS-redirect only in Production.

---

## 11. Major ERP Modules

Backend coverage by area (controller / service / entity anchors):

| Module | Backend anchors | Notes |
|---|---|---|
| **Admissions & Merit** | `S2AdmissionsController`, `AdmissionsServices`, `AdmissionsContracts`, `AdmissionApplication/MeritList(+Entry)/SeatAllocation/AdmissionLead` | SHAP-style merit (MlNet? see `defaulter-ml`), JazzCash portal placeholders |
| **Examination / Grading / Results** | `S3ExaminationController`, `ExamController`, `GradingService`, `ResultService`, `GradingSchemeResolver`, entities `AssessmentScheme/Component`, `CourseMark`, `GradeScale/Band`, `CourseGrade`, `Transcript`, `ExamRegistration`, `GradeAppeal*`, `GradeDistribution*` | Result workflow submit→moderate→approve; UG/PG schemes |
| **OBE / CLO-PLO / CIS** | `S5ObeController`, `ObeServices`, `CurriculumService`, entities `CLOPOMapping`, `CLOAttainment`, `POAttainment`, `CisUpload`, `CisUpload` file scan, `CloPlomapping` (older duplicate) | HEC accreditation toolkit |
| **HR / Payroll** | `HrController`, `PayrollService`, `VisitingFacultyService`, entities `Employee(+)`, `PayrollRun/Payslip`, `LeaveApplication`, `PerformanceReview`, `TrainingAssignment`, `JobPosting/Application`, `TaxSlab` | Pakistan-compliant payroll (EOBI/PF/tax), PIFRA posting (`FinancePayrollPosting`) |
| **Finance & Fees** | `FinanceController`, `FinanceModuleEndpoints`, `FinanceWebhookController`, ~20 `Finance/*` services, `PaymentGatewayAdapters`, `RaastApiClient`, `VoucherImportService`, `AuditSealService`, entities §6 | GL + vouchers + payment plans + gateway adapters (raast/stripe/plaid-ach/gocardless/billplz-fpx/duitnow-qr) + PQC-signed reports §10 |
| **Attendance / Timetable** | `AttendanceController`, `TimetableController`, `CourseAssignment`, `Attendance`, `Timetable`, `OfflineAttendanceEvent` | offline mobile sync |
| **Campus Ops (library/transport/hostel/visitor/security/asset)** | `CampusController`, `CampusOperationsController`, `AssetInventoryController`, `Security/*` services, entities §6 | wide scope |
| **Governance / Compliance / Accreditation / Board** | `GovernanceController`, `ComplianceController`, `AccreditationService`, `BoardPortalService`, `ComplianceService`, entities §6 | |
| **Student Lifecycle** | `StudentLifecycleController`, `ForeignStudentService`, `GamificationService`, `MentalHealthService`, `VoiceAssistantService`, `Placement`, `ElectionService`, `CafeteriaService`, `TransportService` | |
| **Academics/Curriculum engine** | `CurriculumController`, `CatalogIngestion*`, `ClassGeneration*`, `DegreeAudit`, `DegreeVerification`, `CourseTrees/Workload` | degree-tree computation, LmsProvision |
| **Platform/Ext (P87–P127)** | `ExtendedPlatformControllers*.cs`, `PlatformServices*.cs`, many controllers | AI ops, admin/diagnostics, zero-trust tunnels, queue mgmt, health analyzer, official marketplace |
| **Identity & Equity** | `IdentityController`, `AuthController`, `SamlController`, `ScimController`, `SsoHitlController`, I18n, Branding, ProfilePictures | |
| **Incidentals** | `MarkAttendance`, `FeeChallan` etc. see `CoreErpControllers.cs`, `PlatformControllers.cs`, `DeveloperControllers.cs` | "flash" feature controllers |

Frontend parity lives under §22–24.

---

## 12. External Integrations

| Integration | Implementation | Config (appsettings / env) |
|---|---|---|
| **Payment gateways** | `Infrastructure/Finance/Gateways/PaymentGatewayAdapters.cs` (per-gateway adapters), `FinanceWebhookController` idempotent events | `PaymentGateways__{raast,stripe,plaid-ach,gocardless,billplz-fpx,duitnow-qr}` BaseUrls |
| **SBP Raast** | `Infrastructure/Payments/Raast/RaastApiClient.cs` | `RAAST_BASE_URL` |
| **Microsoft 365** | `Infrastructure/MicrosoftEcosystem/MicrosoftEcosystemServices.cs` (Microsoft.Graph 6, Teams class provisioning) | `Microsoft*` keys |
| **Moodle LMS** | same MicrosoftEcosystem project (roster sync, grade passback, `LmsProvision`, `LmsRosterSyncLog`, `ClassGeneration`) | `LmsConfiguration` entity |
| **Power BI** | `PowerBiReport` entities + teams ecosystem | AAD creds |
| **Communications** | SMTP via `Workflow/EmailNotificationService.cs` + `appsettings Email`; `Twilio` SMS; `WhatsApp Business API` (templates `API/wwwroot/templates/whatsapp-templates.json`); in-app SignalR `NotificationHub` | `Email`, `Twilio`, `WhatsApp` sections |
| **Search** | `Infrastructure/Search/ElasticsearchIndex.cs` (NEST 7) + `SearchService` | `Search:ElasticsearchUrl`, `IndexPrefix` |
| **AI / LLM / OCR** | `Infrastructure/AI/`: `OpenAiLlmProvider`, `OllamaLlmProvider`, `LlmProviderFactory`, `OpenAiEmbeddingProvider`, `OllamaEmbeddingProvider`, `InMemoryVectorStore`, `DocumentOcrService`, `AzureDocumentIntelligenceService`, agents (`DocumentAiAgent`, `StudentAiAgent`, `SupportBotQueryService`) | `AI` section, `AI__Provider` |
| **Cloud storage/providers** | `Infrastructure/Cloud/{Aws,Azure,Gcp,R2,OnPremise,Infisical}` (`AwsProvider` S3+Secrets, `AzureProvider`, `GcpProvider`, `R2StorageService` for document vault + signed finance artifacts, FSS/FSS+Foundation `StorageProviders`) | `AEGISCORE_STORAGE_PROVIDER`, `AEGISCORE_CLOUD_PROVIDER`, `AEGISCORE_SECRET_PROVIDER` |
| **Infisical** | `Cloud/Infisical/InfisicalSecretService.cs` | `AEGISCORE_SECRET_PROVIDER=INFISICAL` |
| **Biometric / mobile** | mobile apps (React Native + Expo local-auth); native Android/iOS skeletons | `EXPO_PUBLIC_API_BASE_URL` |

---

## 13. Background Processing

- **Hosted services (API):** `AutoLockBackgroundService.cs`, `LicenseHeartbeatHostedService.cs` (`AegisCore.API/Background/`).
- **Quartz jobs** (Quartz.Extensions.Hosting): `AutomatedBackupJob`, `PaymentPlanWorker`, `SyncLagService`, replication health checks; `ScheduledJob`/`JobExecution` entities for job bookkeeping.
- **SignalR publishing:** `SignalRNotificationRealtimePublisher.cs` + `FinanceHub` push; `StateMachineHub` for workflow realtime.
- **Workflow engine:** `Infrastructure/Workflow/WorkflowEngine.cs`, `ApprovalService.cs`, `StateMachine/StateMachineService.cs`, `WorkflowTemplateSeeder.cs` (seeds template definitions), `EmailNotificationService.cs`.
- **Cron parsing:** `Cronos` 0.13.

---

## 14. Caching / Redis

- Redis connection via `IConnectionMultiplexer` (StackExchange.Redis 2.8.16); `ConnectionStrings:Redis` default `localhost:6379`; docker passes `AEGISCORE_REDIS_CONNECTION`.
- Verified usages: rate limiting (`AspNetCoreRateLimit` + `AspNetCore.HealthChecks.Redis`), `HybridPqcReplayStore`, brute-force locking, session/refresh-token store, `InfisicalSecretService` cache (RedisException-guarded), finance realtime refresh payloads.
- No app-wide distributed cache abstraction (no `IDistributedCache` pattern found) — cache concerns are ad-hoc per service. `UNKNOWN / NEEDS VERIFICATION` for full key inventory.

---

## 15. Storage

- **Abstraction:** `Infrastructure/Foundation/StorageProviders.cs` + `Application/Abstractions/Cloud/CloudAbstractions.cs` (provider registry). Storage driver chosen by `AEGISCORE_STORAGE_PROVIDER` (local disk default; S3 / Azure / R2 / on-prem).
- **Local default path:** `/app/storage` volume; profile pictures (`ProfilePictureService`, `ProfilePictureRecord`); Document Vault (tiered: `DocumentTier`, `DocumentVaultEntry`, retention policies) stored via `R2StorageService` with PQC-signed vault entries.
- **Backups:** `BackupService`, `AutomatedBackupJob`, `BackupEncryptionService` (encrypts dumps at rest); deploy scripts `deploy/scripts/backup.sh`, `rollback.sh`, `warm-cache.sh`.
- **Finance signed artifacts:** R2 + PQC-countersigned files (JSON/CSV/XLSX/PDF/XBRL/iXBRL) with on-request verification.

---

## 16. Observability

- **Logs:** Serilog → console (JSON in Production) and optional PostgreSQL sink; request logging middleware; structured destructuring policy.
- **Metrics:** `prometheus-net.AspNetCore` (`UseHttpMetrics` with `tenant` label, `MapMetrics()` at `/metrics`, anonymous).
- **Health:** `/health`, `/health/live`, `/health/ready` (deployment-readiness checks), `/health/installation`, `/health/detail` (ASP.NET health-checks, authorized), `/health/deep` (authorized), `DeploymentReadinessChecker`, `ExtendedHealthChecks` (Postgres, Redis, storage).
- **Alerting:** `SystemAlertService` (from readiness/DB checks), `ReplicationLagAlert`/`DistributionAnomalyAlert`/`KPIAlert` entities.
- **Portal health route:** `AegisCore.Portal/src/app/portal-health/route.ts` (server-side scrape of API health for the UI).

---

## 17. Frontend Architecture

**Location:** `AegisCore.Portal/`. Next.js **16.2** App Router (`src/app/`), TypeScript, standalone output, i18n via next-intl.

- **Routing scheme:** flat top-level routes, most feature pages (see FILE_TREE `app/` block) are plain `page.tsx`; group routes with `layout.tsx` exist for: `audit`, `board`, `cafeteria`, `catalog`, `classes`, `convocations`, `data-import`, `defaulter-prediction`, `document-vault`, `elections`, `finance/`, `finance/department/[departmentId]`, `foreign-students`, `freezes`, `grade-appeals`, `grade-distributions`, `imports`, `mental-health`, `placement`, `plagiarism`, `prerequisite-conflicts`, `proctoring`, `readiness`, `replication`, `sync-lag`, `transport`, `visiting-faculty`, `voice`, plus flat pages `achievements`, `attendance`, `completion`, `examinations`, `login`, `onboarding`, `setup`, `student/dashboard`, `student/profile`, `student/transcript`, `student/finance/[studentId]`.
- **Root layout** (`src/app/layout.tsx`): `<html lang dir>` + `NextIntlClientProvider` + `PersonalityLayoutWrapper`; `globals.css` = full design system (CSS vars `--ink/--surface/--primary/...`, light/dark `data-theme`, font-scale, high-contrast `data-*` attrs, domain colors).
- **Shell:** `components/layout/AppShell.tsx` — sidebar grouped by domain (academic/finance/operations/compliance/welfare), topbar with theme/font/contrast toggles, ⌘K search (UI only), avatar menu, notifications bell. Responsive (collapses to overlay drawer <1024px). `components/portal-shell.tsx` is an alternate/simpler shell.
- **Dashboard:** `src/app/page.tsx` — role-driven command center (hero greeting, KPI cards, quick actions, module grid). KPIs pull definitions from `lib/roles.ts` but **do not execute their `source` API calls** (display `"--"`; see §24).
- **Charting:** custom `components/nchart/*` (nchart + gauge, heatmap, data-table, tooltip, legend, insight-chip, zoom layer); recharts also available/used.
- **Wizard:** `components/wizard/WizardShell.tsx`, `components/setup-wizard.tsx` — drives `/api/setup/*` and `/api/wizard/*`.
- **PWA/misc:** `public/manifest.webmanifest`, `wrangler.toml` (Cloudflare), `nginx.conf`, `Dockerfile`, Playwright e2e (`tests/aegiscore.spec.ts`), ESLint + typecheck scripts.

---

## 18. Routing Structure

App Router `src/app/` route inventory (page → purpose):

- `/(home)` — Role dashboard (Command Center)
- `/login` — Demo credential login (localStorage session, no API call — see §24)
- `/student/dashboard`, `/student/profile`, `/student/transcript`, `/student/finance/[studentId]` — student self-service
- `/finance(/layout)`, `/finance/operations`, `/finance/gateways`, `/finance/department/[departmentId]` — finance suite
- `/attendance`, `/classes`, `/examinations`, `/grade-distributions`, `/grade-appeals`, `/freezes`, `/proctoring`, `/prerequisite-conflicts`, `/plagiarism`, `/convocations`, `/catalog`, `/defaulter-prediction`, `/completion` — academic
- `/audit`, `/auditor`, `/readiness`, `/board` — compliance/governance
- `/cafeteria`, `/elections`, `/transport`, `/placement`, `/achievements`, `/mental-health`, `/voice`, `/foreign-students`, `/visiting-faculty` — ops/welfare
- `/data-import`, `/imports`, `/setup`, `/onboarding` — administration/onboarding
- `/replication`, `/sync-lag`, `/portal-health` (/health) — IT observability
- `/document-vault` — storage UI

`next.config.ts`: rewrites `/api/:path*`, `/health/:path*`, `/hubs/:path*` → `AEGISCORE_API_INTERNAL_URL` (`http://api:8080` default). No `middleware.ts` — locale from cookie via `i18n/server.ts`.

---

## 19. UI / Component Architecture

- Design system = hand-rolled CSS (no Tailwind/UI lib): `globals.css` variable tokens + utility `.card/.btn/.badge/.table/.skeleton/.error-state` etc. (used inline in components, e.g. login page `<style>`).
- Reusable components in `src/components/`:
  - `layout/AppShell.tsx`, `portal-shell.tsx`
  - `academic/*` — `grade-appeal-form`, `grade-appeals-dashboard`, `module-dashboard`
  - `finance/*` — `approval-queue`, `audit-dashboard`, `auditor-portal`, `department-finance-dashboard`, `finance-dashboard`, `finance-realtime-refresh` (PQC verify + SignalR), `student-payment-action`
  - `nchart/*` — chart library
  - `wizard/WizardShell.tsx`, `setup-wizard.tsx`
  - `personality-*` — locale/personality toggle + translator wrappers, `locale-switcher.tsx`, `paginated-table.tsx`
- Patterns: client components (`"use client"`), hooks (`hooks/use-pagination.ts`), `fetch` via `lib/api-client.ts` (`apiGet/apiPost/apiPut` with JSON+FormData and `credentials:"include"`), server fetch via `lib/server-api.ts` (cookie-passthrough).
- Accessibility: font-scale + high-contrast toggles, `ACCESSIBILITY.md`; responsive breakpoints in CSS.
- i18n: `src/i18n/messages.ts`, `server.ts` (locale from cookie), `request.ts`, `personality-messages.ts`; locales: `en-PK`, `en-US`, `en-GB`, `ar-AE` (RTL), `ms-MY`.

---

## 20. Frontend State & API Layer

- **No global state store** in the Portal; component-local `useState` + `localStorage` (demo session, font scale). Realtime finance/notification refresh via **SignalR** (`@microsoft/signalr`) — see `components/finance/finance-realtime-refresh.tsx` and referenced hubs (`/hubs/finance`, `/hubs/notifications`).
- **API client** `lib/api-client.ts`: thin `fetch` wrapper over the Next rewrite (i.e. path `/api/...` hits backend `/api/...`); `content-type` JSON or `FormData`; typed `ApiError` with server `userMessage` extraction.
- **Server-side calls** `lib/server-api.ts`: `serverGet` forwards the browser cookie to the API (enables SSR server components to read authed endpoints).
- **Backend contract conventions shown by usage:** endpoints return `Ok(new { total, items=... })`-style envelopes (e.g. `{ total, achievements }`); frontend unpacks into typed arrays.

---

## 21. Role-Based UI Behavior

- `src/lib/roles.ts` defines `RoleProfile`s (allowedRoutes, dashboardModules, kpis, quickActions) and role-priority ordering. `getRoleProfile(roles)` + `canAccessRoute(roles, route)` are the client-side gate. Roles map to backend JWT role claims (`ClaimTypes.Role`, from `/api/session`).
- SuperAdmin inherits Admin+VC; VC inherits Admin; Finance Controller/CFO→Finance; HRManager/HRHead→HR; ITHead→IT; Employee→Faculty profile; unknown→Student fallback.
- **Menus are NOT server-driven** — sidebar renders `dashboardModules` from the client-side profile. Permission enforcement MUST still be enforced server-side by Casbin/ABAC; the UI profile is cosmetic.
- Role names used on the backend seed/demo: `Super Admin`, `Admin`, `VC`, `Registrar`, `Exam Controller`, `CFO`, `HOD`, `Dean`, `Faculty`, `Student`.

---

## 22. Backend ↔ Frontend Contract Map

HIGH-confidence proven mappings (page → API call → controller/endpoint):

| Frontend page/component | API call (verified) | Backend target |
|---|---|---|
| `/catalog` | `/api/catalog/ingest/jobs`, `/api/catalog/ingest` (POST), `/api/catalog/ingest/jobs/{id}/review`, `/api/catalog/publish`, `/api/catalog/degree-tree/compute` | `CatalogIngestionController`, `ClassGenerationController` |
| `/classes` | `/api/classes/generate`, `/api/classes/jobs/*`, `/api/classes/offering/*`, `/api/classes/*/provision-lms` | `ClassGenerationController` |
| `/finance` (dashboard) | `/api/finance/v2/dashboard/executive`, `/api/finance/v2/vouchers/{id}/verification` | `FinanceModuleEndpoints` minimal API |
| `/finance/gateways` | `/api/finance/v2/configuration`, gateway test | `FinanceModuleEndpoints`, `FinanceController`, `PaymentGatewayAdapters` |
| `/defaulter-prediction` | `/api/defaulter-prediction?summary`, predict/intervene/forecast | `DefaulterPredictionController`, `DefaulterMlController` |
| `/grade-appeals` | `/api/grade-appeals?status=submitted`, POST create, classify, committee-brief, decision | `GradeAppealsController` |
| `/grade-distributions` | `/api/grade-distributions/analyze`, `/course/*`, `/anomalies` | `GradeDistributionsController` |
| `/proctoring` | `/api/proctoring/*` | `ProctoringController` |
| `/freezes` | `/api/freezes/*` (apply, approve, return) | `SemesterFreezesController` |
| `/foreign-students` | `/api/foreign-students/*` (register, expiry-alerts, mora, renewals, breaches, compliance) | `ForeignStudentsController` |
| `/board` | `/api/board/dashboard`, `/board-pack`, `/alerts`, `/strategic-goals`, `/benchmarks` | `BoardPortalController` / `MapBoardPortalEndpoints` |
| `/achievements` | `/api/achievements/badges`, `/deans-list`, `/leaderboard`, `/roadmap`, `/catalog` | `AchievementsController` |
| `/elections` | `/api/elections/*` (create, nominate, tokens, vote, tally, certify) | `ElectionsController` |
| `/cafeteria` | `/api/cafeteria/units`, `/menu-items`, `/orders`, `/forecast`, `/vendors/*/evaluate` | `CafeteriaController` |
| `/mental-health` | `/api/mental-health/tickets/*` | `MentalHealthController` |
| `/convocations` | `/api/convocations/*` (create, eligibility, certs, check-in, upcoming) | `ConvocationsController` |
| `/visiting-faculty` | `/api/visiting-faculty/*` | `MapVisitingFacultyEndpoints` |
| `/voice` | `/api/voice/*` | `MapVoiceAssistantEndpoints` |
| `/readiness` | `/api/readiness*` | `ReadinessController`, `AccreditationService` |
| `/replication`, `/sync-lag` | `/api/admin/replication*`, `/api/admin/sync-lag*` | `ReplicationController`, `SyncLagController` |
| `/audit` | `/api/audit`, `/api/login-audit` | `AuditLogService`, `LoginAuditController` |
| `/document-vault` | `/api/document-vault/*` | `DocumentVaultController` |
| `/attendance`, `/examinations`, `/timetable` | `/api/attendance*`, `/api/exam*`, `/api/timetable*` | `AttendanceController`, `ExamController`/`S3ExaminationController`, `TimetableController` |
| `portal-health/route.ts` (SSR) | serverGet `/health/ready` | `DeploymentReadinessChecker` |
| KPI `source` hints (unwired) | `/api/finance/v2/dashboard/executive`, `/api/classes?summary`, `/api/attendance/report/student`, `/api/obe/attainment`, `/api/grades/cgpa`, `/api/student/finance`, `/api/timetable/faculty`, `/api/placement?summary`, `/api/portal-health`, `/api/imports?status=failed` | mapped as declared (see §24 caveat) |

Pages without any API wiring: `/imports`, `/auditor`, `/setup`, `/completion`, `/login`, `/onboarding` (either static or server-rendered differently).

---

## 23. Existing Feature Coverage

**Backend:** effectively every sprint area from the README + `docs/2x` reports has backend surface: S1 RBAC/audit/calendar; S2 admissions; S3 exams/grading; S4 HR/payroll; S5 OBE/CIS; S6 Microsoft; S7 finance; S9 comms; S10 campus ops; S11 lifecycle; S12 asset/HEMIS; plus a large P87–P127 platform extension tail (AI ops, diagnostics, zero-trust, queues, licenses, official marketplace, updates).

**Frontend (Portal):** ~45 routes exist; most connect to the API (see §22). Built pages are wide but shallow for many features (list/detail views consuming the total-array envelopes).

**Tests:** `AegisCore.Tests/` — 43 xUnit classes (Admissions, Finance, Payroll, Governance, OBE, CampusOps, MarketplaceRevenue, PQC signature/HybridKEM, License, Wizard activation, Identity, Tenant middleware, Setup routes, SsoHitl, StudentLifecycle, AchievementController, BoardController, ElectionController, ForeignStudent, MentalHealth, Plagiarism, Proctoring, GradeAppeals, SemesterFreeze, DefaulterPrediction, Convocation, EnhancedSetupAndSupport, SecurityHardening, CompleteSuiteArchitectureTests, OperationalRuntime...). E2E: Portal Playwright (`AegisCore.Portal/tests/aegiscore.spec.ts`), mobile Detox (`AegisCore.Mobile/e2e`), plus `tests/run-all.sh` (HTTP abuse/limit harness against a running instance).

---

## 24. Missing / Partial Wiring

- **[HIGH] Login is a demo stub, not the real auth flow.** `AegisCore.Portal/src/app/login/page.tsx` only sets `localStorage["aegiscore_demo_session"]` and routes to `/`. It never calls `POST /api/auth/login`. Consequently the whole Portal session comes from `localStorage` (see `AppShell.tsx` and home `page.tsx`), NOT from backend JWT cookies. Backend fully supports cookie+JWT login (`AuthController`, `IdentityController`, `/api/session`) but the current UI bypasses it.
  - Implication: cookie-driven server requests (`serverGet` with cookie passthrough) and `/api/session` are effectively unused by the Portal today. Real-time authenticated calls, refresh-token rotation, brute-force protection history, and login-audit UI are therefore not exercised end-to-end from this frontend.
- **[HIGH] Dashboard KPIs are static placeholders.** Home page renders `kpi.value` (always `"--"`); the `source` endpoints declared in `roles.ts` are **not fetched**. KPI tiles show placeholder dashes.
- **[HIGH] Demo identity mismatch with production identity.** Portal session uses hardcoded `iobm-demo-tenant`/`STU-2024-001`/`CS-DEPT-001`; real tenants/JWT provide these from claims. Demo Mode is backend-guarded (rejected in Production), but the frontend demo path is not removed by the Production build unless `NEXT_PUBLIC_DEMO_MODE` toggles (docker passes `NEXT_PUBLIC_DEMO_MODE=true`).
- **[HIGH] Several routes are placeholders/static:** `/imports`, `/auditor`, `/setup`, `/completion`, `/onboarding` contain no API calls (setup/onboarding likely server-rendered vs `/api/wizard`, but no fetch-path proven). `UNKNOWN` whether `/completion` or `/imports` are reachable from any menu.
- **[MEDIUM] Native mobile (Kotlin/Swift) skeletons are thin**: `AegisCore.Native/{android,ios}` (~3 files each) vs the richer Expo app `AegisCore.Mobile`. `OfflineAttendance.kt`, `Api.kt`, `MainActivity.kt` only.
- **[MEDIUM] Backend-only modules with no Portal UI:** many controllers (`/api/hostel`, `/api/library`, `/api/bi`, `/api/career`, `/api/export`, `/api/bi`, `/api/events`, `/api/ocr`, `/api/rule-engine`, `/api/search`, `/api/i18n`, `/api/degree-audit`, `/api/digital-twin`, `/api/disaster-recovery`, `/api/forms`, `/api/governance`, `/api/compliance`, `/api/communications`, `/api/branding`, `/api/config-center`, `admin/*` suite) have no matching Portal page.
- **[MEDIUM] `AegisCore.Setup` (Blazor wizard UI) vs `API/wwwroot/setup/index.html` vs Portal `/setup`:** three setup surfaces coexist — roles/unbounded. Need verification which is actually served (`SetupController` + wizard endpoints power them).
- **[MEDIUM] Frontend role KPI `source` paths are a declared contract but unverified against backend** (e.g. `/api/grades/cgpa`, `/api/student/finance` plausibly served by CoreErpControllers/Finance controller — mark `NEEDS VERIFICATION` before relying).
- **[LOW] Signals:** the notification bell in `AppShell` is non-functional UI (no SignalR subscription attached there).

---

## 25. Known Technical Debt

1. **[HIGH] God/DbContext monolith:** `AegisDbContext.cs` 3,479 lines; single massive context with ~450 sets; single `OnModelCreating` doing global tenant filtering + ~40 enum mappings. Entity/config churn forced a "FixPendingModelChanges" migration (`20260805100112`) and `FinanceModelPendingChanges` — sign of model drift.
2. **[HIGH] Duplicated contract interfaces:** `AegisCore.Contracts/Common/Interfaces/*` mirrors `AegisCore.Application/Common/Interfaces/*` (same filenames ~45 each). Two sources of truth, likely legacy from a repo split. `docs/26-repository-split-report.md` explains history.
3. **[HIGH] Whole legacy backend copy retained:** `aegiscore-backend/` (581 tracked files) duplicates the active solution with an older, smaller set (34 controllers, ~5 migrations). Dangerous — a build tool scanning all `*.csproj` could compile the old stack; agents must build from `AegisCore.slnx` at repo root only.
4. **[MEDIUM] Multiple legacy frontend/mobile copies:** `aegiscore-frontend/` (Vite React), `aegiscore-mobile/`, `aegiscore-mobile-app/`, `aegiscore-android/` duplicate Portal/Mobile trees (see §26).
5. **[HIGH] Duplicate CLO-PO concepts:** `DbSet<CloPlomapping> CloPlomappings` (entity `CloPlomapping`, table `CloPlomappings`, referenced by `Course.CloPlomappings`) AND `DbSet<CLOPOMapping> CLOPOMappings` (entity in `CoreErpEntities1.cs`, table `CLOPOMappings`) both in the same context — two modelling generations for the same domain concept.
6. **[MEDIUM] Naming inconsistency:** `CampusEntity`/`Campuses` vs `Campus`/`Campuses` (older `Campus.cs` entity in Domain also exists); `TransportEntities.cs` vs `TransportService`; duplicate `VisitingFaculty*` and `ForeignStudent*` entity families (e.g. `ForeignStudentRegistry` + `ForeignStudentRecord`; `Election`/`ElectionCandidate` vs `CandidateNomination` also present).
7. **[MEDIUM] Controller sprawl:** consolidated files (`ProductControllers`-style) hold many services each — e.g. `CoreErpControllers.cs`, `DeveloperControllers.cs`, `PlatformControllers.cs`, `ExtendedPlatformControllers[234].cs`, `S5ObeController`, `S2AdmissionsController`, `S3ExaminationController` — mixed feature controllers; endpoint findability suffers.
8. **[MEDIUM] Two styles of endpoint authoring** (attribute controllers vs minimal APIs in `API/Finance/*`) with no migration plan — inconsistent conventions.
9. **[MEDIUM] `docs/03-software-architecture-document.md`, `docs/04-database-design-document.md`, `docs/05-api-specification.yaml` predate the P87–P127 + finance-v2 expansions** and are partially stale for current schema/API.
10. **[LOW] Hard-coded demo identity/universities** appear in seeders/demo mode files (intentional for demo, but flagged by `HardcodingAuditController` and `docs/DEPENDENCY_AUDIT.md`).
11. **[LOW] `NuGetAudit=false`** in `Directory.Build.props` disables NuGet vulnerability auditing (deliberate but a compliance trade-off).
12. No TODO/FIXME/HACK markers or `NotImplementedException` stubs found in the active root code (`grep` returned zero) — the debt is structural, not sprinkled in comments.

---

## 26. Suspected Duplicate / Legacy Areas

Evidence: root `Dockerfile`, `docker-compose.yml`, and `.github/workflows/{ci,deploy}.yml` build **only** root `AegisCore.API` and `AegisCore.Portal`; root `AegisCore.slnx` drives CI. Confidence is **HIGH**.

| Legacy path | What it is | Evidence |
|---|---|---|
| `aegiscore-backend/` (581 tracked files) | Older full backend copy (its own `.slnx`, `AegisCore.API`/`Application`/`Contracts`/`Domain`/`Infrastructure`/`Tests`, k8s). Stale: 34 controllers vs 50, 5 migrations vs 90 | `git ls-files` counts; migration/controller diff; not referenced by any root build file |
| `aegiscore-frontend/` | Vite/React SPA (no Next App Router), Cypress e2e, `.storybook`. Older generation of the portal | root CI/Docker only reference `AegisCore.Portal` |
| `aegiscore-mobile/` | Mirror of the Expo `react-native` app + native shells | identical tree to `AegisCore.Mobile`'s `react-native/` |
| `aegiscore-mobile-app/` | Another RN+android+ios bundle (explicit proguard file) | duplicate native + RN trees |
| `aegiscore-android/` | Older Kotlin-only android tree with its own GitHub workflows | duplicate of `AegisCore.Native/android` content |
| `AegisCore.Web/` | Minimal Blazor shell likely superseded by Portal | tiny (4 files); not in `.slnx`, no deployment reference found |

`docs/26-repository-split-report.md`, `docs/27-git-cleanup-audit.md`, `docs/28-android-cleanup-report.md` describe prior reconciliation/cleanup; legacy dirs remain on disk and in git.

Inside the **active** backend, genuine near-duplicates (verified grep):
- `CloPlomapping` (DbSet line 92) vs `CLOPOMapping` (DbSet line 261) — `Domain/Entities/CloPlomapping.cs` vs `Domain/Entities/CoreErpEntities1.cs:88`.
- `CampusEntity` (DbSet `Campuses`, infra) vs legacy `Campus` entity (`Domain/Entities/Campus.cs`).
- `CafeteriaEntities.cs` vs `CafeteriaExtendedEntities.cs`; overlapping `Elect*` sets (`Election`/`ElectionCandidate`/`Ballot` + `CandidateNomination`/`EncryptedBallot`/`ElectionPosition`/`ElectionResult`).
- `ForeignStudentRegistry`/`VisaDocument`/`MoraSubmission` + `ForeignStudentRecord`/`DocumentExpiryTracker`/`EnrollmentLetter`/`VisaRenewalWorkflow`.
- `AchievementBadge`/`StudentBadge` + `BadgeDefinition`/`BadgeAward` (both gamification families).
- `TransportEntities.cs` (TransportRoute/Vehicle/Driver) + `TransportService` consuming its own sets vs `FleetVehicle`/`TransportRegistration`/`BoardingLog`/`TransportIncident` (older family).

---

## 27. Important Entry Points

Backend (all `AegisCore.API/`):
- `Program.cs` — composition root, mode switch, middleware pipeline, endpoint mapping.
- `Extensions/ApiServicesExtensions.cs`, `Extensions/ProductionServicesExtensions.cs` — service registration.
- `Middleware/*.cs` (+ `Middleware/InfrastructureMiddleware.cs` housing RequestLogging / AuthenticatedOriginPulls / HybridPqcTransport / SessionBinding / Turnstile / Abac).
- `Controllers/AuthController.cs`, `Controllers/IdentityController.cs` — auth/identity.
- `Controllers/*.cs` — 50 controller files.
- `Finance/*.cs` — minimal-API mappers (finance v2, default-prediction, visiting-faculty, accreditation, board, voice, gamification, mental-health, foreign-students, elections, cafeteria, transport).
- `Hubs/*.cs` — SignalR.
- `AegisCore.Infrastructure/AegisDbContext.cs`, `Infrastructure/AegisDbContextFactory.cs`, `Infrastructure/InfrastructureServiceExtensions.cs`, `Infrastructure/UnitOfWork.cs`.
- `Infrastructure/Repositories/Repository.cs` + per-domain repositories.

Frontend (`AegisCore.Portal/`):
- `next.config.ts` (rewrites), `src/app/layout.tsx` (root layout/i18n), `src/app/page.tsx` (dashboard), `src/app/login/page.tsx` (auth stub), `src/components/layout/AppShell.tsx` (shell/nav), `src/lib/api-client.ts` + `src/lib/server-api.ts` (API), `src/lib/roles.ts` (roles), `src/app/globals.css` (design system).

Others: `scripts/bootstrap-env.sh`, `docker-compose.yml`, `Dockerfile`, `AegisCore.LedgerVerifier/Program.cs`.

---

## 28. Important Files by Responsibility

- Multi-tenancy: `Infrastructure/AegisDbContext.cs`, `API/Middleware/TenantContextMiddleware.cs`, `Infrastructure/TenantSessionInterceptor.cs`, `Domain/Common/ITenantScoped.cs`.
- Identity/auth: `Infrastructure/Security/JwtTokenService.cs`, `Infrastructure/Identity/IdentityService.cs`, `Services` `Infrastructure/Identity/SamlService.cs`, `API/Controllers/{Auth,Identity,Saml,Scim,SsoHitl}Controller.cs`, `API/Authorization/{RequirePermissionAttribute,PermissionAuthorizationHandler}.cs`.
- Authorization policy: `Infrastructure/Security/CasbinServiceExtensions.cs`, `API/Middleware/CasbinAuthorizationMiddleware.cs`, `Infrastructure/Security/Abac/AbacPolicyService.cs`.
- Finance: `Infrastructure/Finance/FinanceModuleService.cs`, `FinanceModuleServiceExtensions.cs`, `FinanceServices.cs`, `Finance/Services*.cs`, `API/Finance/FinanceModuleEndpoints.cs`, `Finance/Gateways/PaymentGatewayAdapters.cs`, `Payments/Raast/RaastApiClient.cs`, `Finance/AccountingStandardsEngine.cs`, `Finance/AuditSealService.cs`, `Finance/FinanceLedgerIntegrityService` (used in endpoints).
- Grading: `Infrastructure/Examination/GradingService.cs`, `Application/Grading/{ResultService,GradingSchemeResolver}.cs`.
- DB migrations: `Infrastructure/Migrations/` (90 files) + `AegisDbContextModelSnapshot.cs`.
- DB context factory: `Infrastructure/AegisDbContextFactory.cs`.
- Search: `Infrastructure/Search/{SearchService,ElasticsearchIndex}.cs`.
- Notifications: `API/Hubs/NotificationHub.cs`, `Infrastructure/Workflow/EmailNotificationService.cs`, entities `NotificationEntities.cs`.
- Backups/DR: `Infrastructure/Backup/*`, `API/Controllers/DisasterRecoveryController.cs`.
- License: `Infrastructure/License/LicenseService.cs`, `API/Controllers/LicenseController.cs`, `API/Background/LicenseHeartbeatHostedService.cs`.
- Onboarding: `Infrastructure/Onboarding/*` (EnhancedSetup/Wizard* services), `API/Controllers/{SetupController,EnhancedSetupController,WizardController}.cs`, `AegisCore.Setup/` (Blazor UI).

---

## 29. Build / Test Commands

From repo root (CI-verified, see `.github/workflows/ci.yml` and `Directory.Build.props`):

```bash
# Backend (root solution only)
dotnet restore AegisCore.slnx
dotnet build  AegisCore.slnx --no-restore -m:1 -p:BuildInParallel=false --configuration Release
dotnet test   AegisCore.Tests/AegisCore.Tests.csproj --no-build --configuration Release -m:1 -p:BuildInParallel=false

# Frontend (Portal)
cd AegisCore.Portal && npm ci && npx tsc --noEmit && npm run build
# dev server
cd AegisCore.Portal && npm run dev
# e2e
cd AegisCore.Portal && npm run test:e2e

# Local full stack
./scripts/bootstrap-env.sh           # writes .env (TENANT_ID, secrets, license keys)
docker compose --env-file .env up -d --build --wait   # portal:8080
curl --fail http://localhost:8080/health/ready

# Docker images (CI)
docker build -t aegiscore-api:ci-test .                # root Dockerfile = API
docker build -t aegiscore-portal:ci-test AegisCore.Portal

# HTTP abuse/limit harness
BASE_URL=http://127.0.0.1:18080 ./tests/run-all.sh
```

Mobile: `cd AegisCore.Mobile && npm start` / `npm run typecheck` / `npm test`.
Tools: `dotnet-tools.json`; Plugin CLI pack: `dotnet pack AegisCore.PluginCli` → tool `aegiscore-plugin`.

---

## 30. Environment / Configuration Map

`AegisCore.API/appsettings.json` sections: `Pqc` (key rotation), `Logging`, `AllowedHosts`, `AllowedOrigins`, `ConnectionStrings:Redis`, `Cors`, `Auth` (Authority/Audience), `License`, `Admissions` (TrackingTokenSecret), `AI{Provider,OpenAI,Ollama}`, `Email` (SMTP), `Twilio`, `WhatsApp`, `Search` (ElasticsearchUrl/IndexPrefix), `PaymentGateways` (6 gateways). Secrets live in env, not appsettings.

`docker-compose.yml` injects (primary env contract):
`ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT` (default Production), `Runtime__DemoMode` (`AEGISCORE_DEMO_MODE`), `AEGISCORE_INSTALLATION_COMPLETED`, `Database__AutoMigrate`, `Deployment__FailOnReadinessError`, `AEGISCORE_CLOUD_PROVIDER` (default ONPREMISE), `AEGISCORE_SECRET_PROVIDER` (default FILE), `AEGISCORE_LOCAL_SECRET_PATH`, `TENANT_ID`, `AEGISCORE_DB_CONNECTION`, `AEGISCORE_REDIS_CONNECTION`, `AEGISCORE_STORAGE_PROVIDER` (local), `AEGISCORE_STORAGE_LOCAL_PATH`, `License__ServerUrl`/`HmacSecret`, `AEGISCORE_ENCRYPTION_KEY`, `AEGISCORE_PQC_KEY_BASE64`, `AEGISCORE_JWT_SECRET`, `Governance__FeedbackEncryptionKeyBase64`, `HybridPqcTransport__*`, `DataProtection__*`, `PaymentGateways__*`, `AI__*`.

Portal env: `AEGISCORE_API_INTERNAL_URL` (default `http://api:8080`; `http://localhost:8080` for local dev), `NEXT_PUBLIC_DEMO_MODE` (passed to build as true in compose). See `AegisCore.Portal/.env.example`.

`AegisCore.Portal/wrangler.toml`, `README-DEPLOY.md`, `docs/07-deployment-guide.md` cover Cloudflare Pages / k8s / VPS (aws/azure/gcp/vps in `docs/deployment/`).

---

## 31. Agent Safety Boundaries

- **Build/test only the ROOT tree:** use `AegisCore.slnx` + `AegisCore.Portal`. Never build under `aegiscore-backend/` or add it to tooling paths.
- **Do NOT edit `FILE_TREE.md`** (auto-generated).
- Don't create new top-level projects; the layer contract is fixed (Domain/Contracts/Application/Infrastructure/API).
- EF Core model changes require additive migrations under `AegisCore.Infrastructure/Migrations/`; Postgres enums need enum migration care.
- Auth is real on the backend: don't "fix" demo gaps by relaxing backend authorization; demo middleware is gated by environment (`DemoMode` rejected in Production).
- Follow existing conventions: per-module `*Extensions.cs` DI, `I{...}Service` in Application/.../Contract + impl in Infrastructure, `[Route("api/...")]` controllers or minimal-API mapper classes under `API/Finance/`.
- Secrets are env-driven; never hardcode (a `HardcodingAuditController` + `docs/DEPENDENCY_AUDIT.md` track violations).
- Frontend work: keep the App Router structure, `lib/api-client.ts` for client calls, `lib/server-api.ts` for SSR; CSS-token system in `globals.css` (no Tailwind).

---

## 32. Backend Agent Quick Start

1. Read `AegisCore.API/Program.cs` (composition + mode switch), `Extensions/ApiServicesExtensions.cs` + `ProductionServicesExtensions.cs`, `Infrastructure/InfrastructureServiceExtensions.cs`.
2. Read `AegisCore.Infrastructure/AegisDbContext.cs` (entity inventory + tenant filter) and `Infrastructure/Migrations/AegisDbContextModelSnapshot.cs` for current schema.
3. To implement a feature: find the relevant `Application/*/I*Service.cs` contract + `Application/*/*Contracts.cs` DTOs; implement/verify in `Infrastructure/<Module>/<Service>.cs`; add DI in the module `*Extensions.cs`; expose via a controller in `API/Controllers/` or a minimal-API mapper in `API/Finance/` if finance-adjacent; annotate `[Authorize]`/permissions matching the Casbin matrix.
4. Run `dotnet build AegisCore.slnx && dotnet test AegisCore.Tests/AegisCore.Tests.csproj`.
5. Reference architecture doc: `docs/03-software-architecture-document.md` (partially stale for P87+), `docs/06-security-architecture-document.md`, `docs/04-database-design-document.md`.

---

## 33. Frontend Agent Quick Start

1. Read `AegisCore.Portal/next.config.ts` (rewrites), `src/app/layout.tsx`, `src/components/layout/AppShell.tsx`, `src/lib/roles.ts`, `src/lib/api-client.ts`, `src/lib/server-api.ts`.
2. Map a requested feature to its backend endpoint via §22 (and verify backend route in `AegisCore.API`).
3. Add pages under `src/app/<feature>/page.tsx` (+ `layout.tsx` for grouped areas); use `apiGet/apiPost/apiPut`; use de-enveloping (`{ total, items }`) pattern; add the route to the role profile in `src/lib/roles.ts` (allowedRoutes + dashboardModules) so it appears in nav.
4. **Do not rely on the demo login for real work** — wire to `POST /api/auth/login`/cookies if targeting real auth sessions (current demo path is §24 gap).
5. Run `npm run typecheck`, `npm run lint`, `npm run build`; e2e via Playwright.
6. Reference `AegisCore.Portal/README.md`, `ACCESSIBILITY.md`, `AEGISCORE_CAPABILITIES.md`, `TECHNICAL_SPEC_SHEET.md`.

---

## 34. Unknowns Requiring Verification

- **[Contract]** Exact `Casbin` policy storage/definition source and full permission-matrix contents (policy file vs DB seed) — `NEEDS VERIFICATION`.
- **[Contract]** Which endpoint authority serves `/api/grades/cgpa`, `/api/student/finance`, `/api/timetable/faculty`, `/api/obe/attainment` (referenced as KPI sources in `roles.ts`) — controller mapping unverified.
- **[Behavior]** Which setup surface is actually served in production: `AegisCore.Setup` Blazor app, `API/wwwroot/setup/index.html`, or Portal `/setup` — `NEEDS VERIFICATION`.
- **[Behavior]** Redis key inventory & TTL strategy; whether dynamic caches (finance realtime, session) are properly invalidated.
- **[Contract]** `z-TurnstileMiddleware` config sources (site key) and whether it is enforced outside docker.
- **[Deployment]** `AegisCore.Portal` Cloudflare Pages (`wrangler.toml`) vs self-hosted `nginx.conf` vs container — which is the live path.
- **[Behavior]** Mobile→backend auth contract: `AegisCore.Mobile/src/api/client.ts` uses `baseURL 'https://aegiscore.ssuet.edu.pk/api'` — token storage & refresh not verified.
- **[Model]** Which finance ledger tables are truly append-only & protected by DB guards vs service-level only (see `FinanceLedgerAppendOnlyGuards` migration).
- **[Data]** Whether legacy seeded demo users/tenants (`iobm-demo-tenant`) exist in default `demo-university-seed.sql` and how they map to frontend demo accounts.