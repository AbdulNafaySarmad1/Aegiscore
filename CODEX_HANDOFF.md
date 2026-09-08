# Codex Handoff — AegisCore Deployment Prompts

Updated: 2026-09-08. Repository: `/home/ubuntu/Aegiscore`, branch `main`.

## Prompt status

Completed prompts: **P1–P17**. **P18–P19** code complete and pushed (`bcf49bf0`); the
18A/18B capture artifacts remain blocked on a live install with real role credentials.

Current prompt: **P22 — Post-Admission Migration — code complete (see "P22 verified
implementation" below). P20–P22 (admissions) are all implemented.**

Remaining execution order: nothing outstanding for P20–P22. Still outstanding from
P18–P19: run `capture:screenshots` +
`capture:demo` against a live `APP_URL` with `ADMIN/FACULTY/STUDENT_*` +
`AEGISCORE_SETUP_TOKEN` + `DEMO_COURSE_ID`/`DEMO_STUDENT_NUMBER`/`DEMO_MARKS` (see
`docs/CAPTURE_SUITE.md`), then validate the private-client deployment.

## P20 verified implementation

- Entities (`AegisCore.Domain/Entities/ApplicantPortalEntities.cs`): `AdmissionIntake`
  (year + `IntakeSemester` F/S/W, `IntakeStatus`, optional `BatchId`), `ApplicantProfile`
  (unique `ApplicantId`, `ApplicantStatus` enum Draft→…→Admitted/Rejected/Withdrawn,
  `ConvertedStudentId`), `AdmissionDocumentRequirement` (admin-set per intake+program:
  required/optional, `AdmissionDocumentFormat` Pdf|Image, `MaxFileSizeMb`),
  `ApplicantDocument` (`StoragePath`, `FileSize`, `MimeType`,
  `AdmissionDocumentValidationStatus`, `ValidationConfidence`, `ValidationMethod`). All
  `ITenantScoped`; enum strategies registered in `AegisDbContext.OnModelCreating`.
- Migration `AddApplicantPortal` added to all four assemblies
  (`AegisCore.Infrastructure` + MsSql/MySql/Oracle); `has-pending-model-changes` clean
  for all four dialects.
- APP ID: `IApplicantIdGenerator` → `APP-{YEAR}{F|S|W}-{PROGRAM_CODE}-{NANOID8}` using
  `Nanoid` (added package) over a no-vowel/no-ambiguous 32-char alphabet, DB
  collision-checked with bounded retries. `DegreeProgram.Code` is the program code,
  editable at `PUT /api/admissions/config/programs/{id}/code` (regex `^[A-Za-z0-9]{2,8}$`).
- Applicant auth: second JWT bearer scheme `Applicant` (audience `applicant`, issuer
  `https://aegiscore.pk`) wired in `AddAuthenticationServices`; `OnTokenValidated`
  checks a Redis revocation key. Passwordless: `IApplicantOtpService` (6-digit code in
  Redis, 10-min TTL, 5-attempt cap) + `INotificationService` email delivery.
  `ApplicantPortalController` (`/api/applicant/*`): `[AllowAnonymous]` signup/verify +
  `intakes/open`; everything else `[Authorize(AuthenticationSchemes = "Applicant")]`.
  Applicant tokens cannot satisfy the default (`aegiscore-api`) audience and vice-versa.
- Portal proxy `src/app/api/applicant/[...path]/route.ts` carries its own httpOnly
  `aegiscore_applicant` cookie, minted from `/auth/verify`.
- Document upload: enforces the admin-configured format + `MaxFileSizeMb` + magic-byte
  signature; stored via existing `IStorageProvider` at
  `tenants/{tenantId}/applicants/{applicantId}/{docType}.{ext}`.
- CLIP: `services/aegiscore-clip/` (python:3.11-slim, FastAPI, port 8100, container
  `aegiscore-clip`). `POST /validate-photo` → CLIP zero-shot with the exact
  positive/negative prompt pair, threshold 0.82, `{passed,confidence,reason}`.
  `POST /ocr` → digital PDF text with Tesseract fallback (reused by P21). Published as
  `ghcr.io/nocturne-systems/aegiscore-clip` via a new `publish-clip` job in
  `client-release.yml`; added to `docker-compose.client.yml` as an internal service
  (`expose: [8100]`, no host ports) + `docker-compose.yml` (dev, `build:`).
  `IPhotoValidationService`/`ClipPhotoValidationService` calls it via `CLIP_SERVICE_URL`;
  **fails open** — unreachable/timeout/5xx ⇒ `ValidationStatus = ManualReview`,
  `ValidationMethod = "failopen"`, admin in-app alert, applicant not blocked.
- Admin: `AdmissionsAdminController` (`/api/admissions/intakes`, `/config/documents`,
  `/config/programs/{id}/code`, `/applicants…`) all behind `[RequirePermission("Admissions",
  …)]` — actions `Read`/`Approve` reused, new action `Configure` (tenant-granted via the
  dynamic RBAC matrix; no hardcoded catalog). Document download streams from
  `IStorageProvider`; per-doc approve/reject; guarded status transitions + bulk move.
- Portal: `/apply` (6-step public wizard, no AppShell), `/admin/admissions` (list +
  filters + bulk), `/admin/admissions/[applicantId]` (docs + pipeline), and
  `/admin/admissions/document-config` (intakes + per-program document requirements +
  program codes).
- Validators: `AegisCore.API/Validation/AdmissionsValidators.cs` — `SecureValidator<T>`
  for every new request DTO.
- Tests: `AegisCore.Tests/ApplicantPortalTests.cs` (7) — APP ID format/uniqueness +
  collision retry, CLIP fail-open ⇒ ManualReview + non-blocking submit, document
  size/format enforcement from DB config, illegal pipeline-transition rejection, CLIP
  client fail-open with no `CLIP_SERVICE_URL`, applicant token audience.
- Verification: `dotnet build AegisCore.slnx -c Release` PASS; `dotnet test` **645
  passed / 1 skipped / 0 failed**; `has-pending-model-changes` clean ×4 dialects;
  Portal `tsc --noEmit` + `eslint` clean; `next build` compiles the new routes.

## P21 verified implementation

- Entities (`AegisCore.Domain/Entities/AdmissionTestEntities.cs`, all `ITenantScoped`):
  `AdmissionSubject` (admin master list — no subject hardcoded), `ProgramTestProfile` +
  `ProgramTestSubject` (per intake+program: subjects, question count, marks each, pass
  mark, time limit), `SyllabusDocument` (uploaded PDF + `ExtractedText` + `SyllabusOcrStatus`),
  `QuestionGenerationPrompt` (DB-stored, admin-editable LLM system prompt, seeded from
  `AdmissionQuestionPrompt.Default` — the exact brief text), `AdmissionQuestion`
  (OptionA-D + `CorrectOption` + `SourceChunk` + `AdmissionQuestionStatus` Draft/Approved/
  Rejected — named to avoid the legacy `QuestionBanks` table), `ApplicantTest`
  (`PaperJson` = per-applicant shuffled option order, `PaperHash` = SHA-256 of the
  ordered question ids, `ResponsesJson`, `Score`, `Passed`, `ApplicantTestStatus`).
- Migration `AddAdmissionTestEngine` in all four assemblies (pure additive — the three
  new enum types were already declared by P20's migration); `has-pending-model-changes`
  clean for postgres/mysql/mssql/oracle.
- Syllabus OCR: `ISyllabusOcrService`/`SyllabusOcrService` calls the aegiscore-clip
  sidecar `POST /ocr` (`CLIP_SERVICE_URL`); digital text with a Tesseract fallback,
  entirely in the sidecar — the existing `IDocumentOcrService` is untouched.
- Question generation: `AdmissionQuestionGenerator` uses the tenant's configured LLM via
  the existing `LlmProviderFactory.GetLlmProvider(tenantId)` (no new AI config).
  `{N}` in the stored prompt is substituted with the count. `AdmissionQuestionGenerator.Parse`
  is a tested static that tolerates markdown fences and both list- and letter-keyed JSON
  shapes. `QuestionBankService.IngestSyllabusAsync` stores the PDF via `IStorageProvider`,
  OCRs, chunks (~3k chars), generates, and inserts **Draft** questions.
  `/admin/admissions/question-bank` approves/rejects/edits; **only Approved questions
  enter live papers**.
- Randomized paper: `TestSchedulingService.ScheduleAsync` pulls `QuestionCount` random
  Approved questions per subject slot (422-style error when the pool is short), shuffles
  option order per applicant (Fisher-Yates over A-D, `RandomNumberGenerator`), interleaves
  subjects, computes `PaperHash`, persists `ApplicantTest` (Scheduled), and moves the
  applicant to `TestScheduled`.
- Terminal: `/terminal` — anonymous kiosk page (tenant from `IApplicantTenantResolver`),
  full-screen via the Fullscreen API with an exit warning, one question at a time, no
  back navigation, server-authoritative countdown, **auto-submit on expiry**. Options
  carry their true key in shuffled display order; server scores against `CorrectOption`,
  stores score/pass, and returns **pass/fail only**. Server also finalizes if an answer
  arrives past the deadline. Endpoints `POST /api/terminal/start|{id}/answer|{id}/submit`.
- Proctor: `/admin/admissions/proctor` + `GET /api/admissions/proctor` — live in-progress
  list (no questions/answers), force-submit, flag. `TestScoring.FinalizeAsync` is the
  shared scoring path.
- Permissions: reuses `Admissions` resource; new actions `Configure` and
  `ApproveQuestions` and `Proctor` (tenant-granted via dynamic RBAC — no hardcoded
  catalog). All staff endpoints `[RequirePermission("Admissions", …)]`; every new
  request DTO has a `SecureValidator<T>`.
- Portal: `/admin/admissions/test-config` (subjects, per-program profile, syllabus
  upload, prompt editor), `/admin/admissions/question-bank`, `/admin/admissions/proctor`,
  and `/terminal`.
- Tests: `AegisCore.Tests/AdmissionTestEngineTests.cs` (6) — per-program subject
  isolation (MBBS ≠ BCS), approved-only sampling + short-pool failure, terminal scoring +
  auto-submit + pass/fail-only, start rejected after the window (status → Expired), LLM
  JSON parsing (fenced / varied shapes), ingest saves Draft questions from the configured
  prompt.
- Verification: `dotnet build AegisCore.slnx -c Release` PASS; `dotnet test` **651
  passed / 1 skipped / 0 failed**; `has-pending-model-changes` clean ×4; Portal `eslint`
  clean and `next build` compiles `/terminal` + the six `/admin/admissions/*` routes.

## P22 verified implementation

- Entities: `Student` gains `RollNumber`, `ApplicantId` (retained for audit — never
  cleared), `AdmissionApplicantProfileId`; `User` gains `RecoveryEmail`. New
  `ProgramIntakeSequence` (per intake+program counter with a `ConcurrencyStamp` EF
  concurrency token) and `EmailProvisioningConfig` (per-tenant, unique; `Mode` +
  `DomainName` + encrypted `CredentialsCipher`). Migration `AddAdmissionMigration` in all
  four assemblies; `has-pending-model-changes` clean ×4.
- Roll number: `RollNumberService.AllocateAsync` → `{YEAR}{F|S|W}-{PROGRAM_CODE}-{SEQ}`
  (e.g. `2025F-BCS-1`). The increment reads the row, bumps `NextValue` + a fresh
  `ConcurrencyStamp`, and retries on `DbUpdateException` — collision-safe under concurrent
  admits (tested across independent `DbContext`s).
- `AdmissionDecisionService.AdmitAsync` (one `SaveChanges` = one transaction): creates the
  `Student` (from `ApplicantProfile`, `BatchId` from `AdmissionIntake.BatchId` — admit
  fails if unset), creates the `User` (personal email as both `Email` and `RecoveryEmail`
  for now), assigns the tenant's `Student` role, sets `ApplicantProfile.Status = Admitted`
  + `ConvertedStudentId`, persists a `DomainEvent` row (`EventType = "AdmissionConfirmed"`),
  then `await IPublisher.Publish(AdmissionConfirmedNotification)`. Re-admitting is
  rejected.
- Domain event: MediatR is now also scanned over the Infrastructure assembly.
  `AdmissionConfirmedHandler` (the one `INotificationHandler`): resolves
  `EmailProvisioningConfig`, provisions the institutional address, sets
  `User.Email` = institutional / `User.RecoveryEmail` = personal, revokes the applicant
  session token (`IApplicantTokenService.RevokeAsync`), and sends the two templated emails
  (institutional "Welcome to {University}…"; personal "…your personal email is now your
  recovery address only"). Wrapped in try/catch — the committed admission is never undone
  by a provisioning failure.
- Provisioning: `IInstitutionalEmailProvisioner` × 3 —
  `GoogleWorkspaceEmailProvisioner` (added `Google.Apis.Admin.Directory.directory_v1`;
  service account + domain-wide delegation, `Users.Insert`, `409` → append sequence),
  `Microsoft365EmailProvisioner` (existing `Microsoft.Graph` + `Azure.Identity` client
  credentials, `Users.PostAsync`, `400/409` → append), `ManualEmailProvisioner` (no API
  call). `InstitutionalEmailProvisionerFactory` picks by mode. Credentials encrypted with
  the ASP.NET Data Protection key ring (`EmailCredentialProtector`).
- `/admin/settings/email-provisioning` (mode + domain + credentials; credentials never
  returned) → `GET/PUT /api/admin/settings/email-provisioning`
  (`[RequirePermission("Settings", …)]`).
- Pipeline board (22D): `/admin/admissions/pipeline` — Kanban with the eight columns
  (Applied → … → Enrolled → Rejected) mapped from `ApplicantStatus` (+ "Enrolled" derived
  once `ConvertedStudentId` is set, "Test Completed" derived from a submitted
  `ApplicantTest`), native HTML5 drag-and-drop, confirm dialog on Admitted/Enrolled/
  Rejected, bulk select + bulk move. `GET /api/admissions/pipeline`,
  `POST /api/admissions/pipeline/move|bulk-move`. Moving to Admitted/Enrolled runs the
  admit flow; "Test Completed" is read-only.
- New `Admissions` action `Admit`; `Settings` actions `Read`/`Write` for the provisioning
  settings. Every new request DTO has a `SecureValidator<T>`.
- Tests: `AegisCore.Tests/AdmissionMigrationTests.cs` (6) — roll-number format +
  per-program/intake increment, collision-safe allocation across contexts, admit creates
  Student+User (APP ID retained, personal → `RecoveryEmail`) + persists the DomainEvent +
  publishes the notification + rejects re-admit, handler migrates the account (Manual
  mode) and invalidates the applicant session and sends both emails, provisioning config
  encrypts credentials at rest, pipeline column mapping.
- Verification: `dotnet build AegisCore.slnx -c Release` PASS; `dotnet test` **657
  passed / 1 skipped / 0 failed**; `has-pending-model-changes` clean ×4; Portal
  `next build` (lint included) compiles `/admin/admissions/pipeline` and
  `/admin/settings/email-provisioning`.

## P20–P22 known limitations / not verified end-to-end

These are environment/credential gaps, not code defects. The .NET side is fully built
and tested; the items below were exercised only through mocks/stubs or compile
verification.

1. **`aegiscore-clip` Docker image not built or run here.** `services/aegiscore-clip/`
   (Dockerfile + FastAPI `app/main.py` for `/validate-photo` and `/ocr`) is complete and
   the `publish-clip` job in `.github/workflows/client-release.yml` builds/pushes
   `ghcr.io/nocturne-systems/aegiscore-clip`, but no local `docker build` / container run
   / endpoint smoke test was performed. `IPhotoValidationService` and `ISyllabusOcrService`
   are covered in tests with fakes (including the CLIP-unreachable fail-open path).
   *To close:* `docker build ./services/aegiscore-clip`, run it, and curl
   `POST /validate-photo` (real JPEG/PNG) and `POST /ocr` (real PDF). Needs Docker +
   outbound network to PyPI and Hugging Face (CLIP weights are downloaded at image-build
   time). First build is slow (torch CPU + transformers + ~600 MB model).

2. **Live LLM question generation not run.** `AdmissionQuestionGenerator` calls the
   existing `LlmProviderFactory.GetLlmProvider(tenantId)`; the JSON-parsing layer
   (`AdmissionQuestionGenerator.Parse`) is unit-tested with sample completions (fenced /
   list- and letter-keyed shapes), and `QuestionBankService.IngestSyllabusAsync` is
   tested with a stub generator. No real syllabus PDF → question bank run was done.
   *To close:* configure an AI provider — either `AI_API_KEY` + `AI_ENDPOINT`
   (OpenAI-compatible), or `AI_PROVIDER=ollama` + reachable `AI_ENDPOINT` + a pulled
   `AI_MODEL` — then run `POST /api/admissions/syllabus/{subjectId}` with a real PDF.

3. **Google Workspace / Microsoft 365 provisioning are compile-verified only.**
   `GoogleWorkspaceEmailProvisioner` (`Google.Apis.Admin.Directory.directory_v1`) and
   `Microsoft365EmailProvisioner` (`Microsoft.Graph` + `Azure.Identity`) implement account
   creation with `409/400` → sequence-append collision handling, but no call was made to a
   real directory. `ManualEmailProvisioner` and the full `AdmitAsync` →
   `AdmissionConfirmedHandler` account-migration path (personal email → `RecoveryEmail`,
   applicant-session revocation, dual welcome emails) **are** tested end-to-end in Manual
   mode. *To close:*
   - Google: a service-account JSON with domain-wide delegation for scope
     `https://www.googleapis.com/auth/admin.directory.user`, the delegated admin email
     (`adminEmail` field in the JSON), and a Workspace domain to create a throwaway user
     in.
   - M365: an app registration `tenantId` / `clientId` / `clientSecret` with the
     **application** permission `User.ReadWrite.All` (admin-consented) and a verified
     domain.
   Configure the mode + domain + credentials JSON at `/admin/settings/email-provisioning`,
   then admit an applicant and confirm the mailbox is created.

4. **Pre-existing, unrelated to P20–P22:** the P18/P19 `capture:screenshots` /
   `capture:demo` artifacts still need a live `APP_URL` + `ADMIN/FACULTY/STUDENT_*`
   credentials + `AEGISCORE_SETUP_TOKEN` (see `docs/CAPTURE_SUITE.md`).

## Completed prompt commits

- P1: `57e4b56b` (compile fixes), `a1032a24` (schema-aligned tests)
- P2: `6a6ecf05`
- P11: `187970a2`
- P10: `dd657541`
- P3A: `e5daf47a`
- P3B: `b7b314bf`
- P5: `6480fba1`
- P4: `3a2f1014`
- P6: `f32f7d16`
- P7: `318ac5df`
- P8: `56f397b4`
- P9: `16e6dc7f`
- P12: `b9d16168`
- P13: `9ffd7caf`
- P14: `cdfe8c90`
- P15: `bf8779ec`
- P16: `c5be80d4` (core compliance)
- P16 appendix: `202008ce`
- P17: `dacb1435`
- P20: `a958ec5f` (feat), `9062ecc2` (graph + handoff)
- P21: `af550533` (feat), `27072758` (graph + handoff)
- P22: `87659941` (feat), `094f0527` (graph + handoff)

## P11 verified implementation

- Added `DB_DIALECT` and PostgreSQL/MySQL/SQL Server/Oracle adapters for runtime registration, design-time context creation, enum/JSON/binary/text mapping, health/readiness, and wizard connections.
- Implemented per-dialect migration assemblies and stable generated baselines for MySQL, SQL Server, and Oracle; PostgreSQL retains its existing migration history.
- Added the four-provider wizard selector, persisted dialect/connection formats, exact provider version queries, validation, environment/deployment settings, unit tests, and a real-container CI migration matrix.
- All 46 historical Finance migrations and all 40 historical RLS migrations contain the P11 portability note. PostgreSQL RLS remains defense in depth through a PostgreSQL-only adapter interceptor; global `ITenantScoped` filters are active for every provider.
- All non-PostgreSQL snapshots contain zero `jsonb` and zero `bytea`; all four providers report no pending model changes. SQL Server cascades use `NO ACTION`, and Oracle default decimals have stable canonical metadata.
- Focused dialect/environment coverage passes 21/21, including adapters, exact version queries, model mappings, aliases, connection formats, SQL Server cascades, Oracle decimals, and MySQL non-TLS authentication.
- Real-engine verification passes: PostgreSQL 17 applies the complete historical chain; MySQL 8.4, SQL Server 2022, and Oracle Free 23 apply their baselines from empty databases.
- The PostgreSQL run exposed and verified a repair to `AddTenantId_PolicyConfig`: add nullable, backfill system-template rows to `Guid.Empty`, then enforce non-null.
- A security gate rejected generating a migration that would remove already-deployed PostgreSQL RLS. Do not bypass that gate. The implemented defense-in-depth compatibility preserves security and cross-provider behavior.

No P11 implementation work remains.

P13 includes tenant-defined wizard roles/permission matrix, two-argument `RequirePermission`, tenant-scoped Casbin policy sync with five-minute cache, dynamic JWT lifetime, shared read-only SQL allow-list, and tests. There are zero `[Authorize(Roles=...)]` controller attributes and 105 `RequirePermission(...)` controller attributes after P16.

## P14 verified implementation

- Added root/page error infrastructure:
  - `src/components/ErrorBoundary.tsx`
  - `src/components/ErrorDisplay.tsx` with `WizardError`, retry, fix guidance, and technical-details toggle
  - `src/app/error.tsx`, `global-error.tsx`, `not-found.tsx`
  - high-risk boundaries under `finance/`, `examinations/`, `admissions/`, and `grades/`
  - root layout wraps all 43 page roots in `ErrorBoundary`
- Added `TenantPreferencesProvider`/formatter, backed by authenticated `GET /api/config/portal-preferences`.
- Extended `TenantConfigService` defaults/getters for locale, currency, payment gateway, accreditor name, and identity-document name.
- Added a 768–1023px tablet CSS tier: persistent collapsed sidebar, two-column dashboards, one-column forms, card-style tables, 90%-width dialogs, and 44px touch targets. Mobile behavior now starts below 768px. `AppShell` defaults the tablet sidebar to collapsed and exposes a mobile menu button.
- Replaced tenant-specific formatting/configuration in finance dashboard, department finance, finance operations, student finance, payment gateway selection, cafeteria, placement, freezes, transport, defaulter forecast, and visiting-faculty labels/amounts.
- Removed the literal student fee balance and replaced the transcript's country-specific sample course.
- Replaced generic page-load failures with `ErrorDisplay`, actionable retry behavior, module-specific copy, descriptive loading messages, and informative empty states that point to their associated page action.
- Added `tests/tablet-responsive.spec.ts` and `docs/P14_VIEWPORT_RESULTS.md`; all three required viewport checks pass.
- Portal lint now has zero errors. Generated Playwright reports are excluded from the lint surface.

## P15 verified implementation

- Added encrypted-at-rest attendance coordinates, tenant-scoped campus polygons, location statuses, manual overrides, and migration `AddGeolocationToAttendance`.
- Added boundary-inclusive ray casting and six focused tests, including a SQLite-backed check-in/roster/override integration path.
- Added permission-protected check-in, teacher override, roster, student-course, and campus-polygon management APIs.
- Added student mobile check-in, teacher status/filter/override UI, and an administrator map-based campus editor.
- Fixed the enum persistence sentinel found by the integration test, plus onboarding template/RBAC regression tests exposed by the full suite.

No P15 implementation work remains.

## P16 verified implementation

- Added tenant-scoped FERPA audit, consent-ledger, and erasure-request schemas plus migration `AddFerpaGdprCompliance`; IP addresses are encrypted at rest.
- Added `RequireFerpaAudit` enforcement on grade, attendance, transcript, and admission record endpoints. It audits allowed and denied access, validates admin/self/assigned-faculty/applicant relationships, and returns the specified 403 denial message.
- Added the permission-protected, filterable, paginated `GET /api/admin/ferpa-audit` report with human-readable summaries.
- Added student directory opt-out enforcement in search results, indexing, and full index rebuilds.
- Added consent record/revoke APIs, user-or-admin erasure requests with 30-day deadlines, transactional cascade anonymization, active-session/profile-photo cleanup, and retained anonymized academic facts.
- Added on-demand five-file DSAR ZIP exports with 24-hour expiry metadata and no location coordinates or encrypted IP values.
- Added pending-erasure processing and tenant-local 02:00 retention with a configurable six-year default.
- Added focused coverage for allowed/denied FERPA access, encrypted IP storage, complete erasure, DSAR contents/expiry, retention windows, and directory opt-out.
- Added `docs/FERPA_GDPR_COMPLIANCE.md` documenting the modules as institutional opt-ins, operational prerequisites, and legal limitations.
- Disclosed the exact licence-heartbeat payload and added locally signed offline validation via `License__OfflineMode`/`LICENSE_OFFLINE_MODE`; verified that offline mode creates no HTTP client.
- Made Cloudflare Tunnel an explicit setup choice that defaults off; direct university ingress/DNS remains supported and no empty tunnel variable is emitted.

No P16 implementation work remains.

## P17 verified implementation

- Added `@axe-core/playwright`, a PR/push CI accessibility job, exact coverage for the ten required high-risk URLs, and uploaded JSON/Playwright evidence.
- Audited all 45 current `page.tsx` roots using WCAG 2.1 A/AA tags; the final no-retry scan passes 45/45 with zero automated violations.
- Documented and fixed 20 pre-remediation page-rule findings: 4 critical and 16 serious findings across 39 affected nodes.
- Added global skip navigation, visible focus styles, reduced-motion handling, landmark/control semantics, table scope, form error relationships, and corrected AA colour tokens.
- Added a reusable modal focus trap with initial focus, Tab/Shift+Tab wrapping, Escape close, and trigger restoration.
- Added six Playwright keyboard workflows covering login, dashboard, grade entry, attendance, all seven wizard steps, and data import.
- Replaced the inaccurate accessibility statement and added complete axe and keyboard audit evidence.

No P17 implementation work remains.

## Current verification status

- `dotnet test AegisCore.slnx -c Release --no-build`: **PASS**, 626 passed, 1 skipped, 0 failed. The skip is `SecurityHardeningTests.OversizedFileUpload_RejectedAtBoundary`.
- P16 focused compliance tests: **PASS**, 4/4.
- P16 appendix licensing/infrastructure tests: **PASS**, 7/7.
- Portal `npm run typecheck`: **PASS**.
- Full no-retry WCAG audit: **PASS**, 45/45 current Portal page roots.
- Exact CI accessibility command: **PASS**, 10/10 required axe routes and 6/6 keyboard workflows.
- `@axe-core/playwright` reports: **PASS**, zero violations after remediation.
- Portal `npm run build`: **PASS**, 46 generated routes including `/admin/campus` and `/student/attendance`.
- Portal `npm run lint -- --quiet`: **PASS**, 0 errors.
- P11 focused dialect/environment tests: **PASS**, 21/21.
- Four real database migration runs: **PASS** (PostgreSQL 17 full history; MySQL 8.4, SQL Server 2022, Oracle Free 23 baselines).
- Four `migrations has-pending-model-changes` checks: **PASS**.
- CI workflow YAML parse: **PASS**, 5 jobs including the four-dialect matrix.
- Graphify refresh/query: **PASS**, 26,714 nodes and 51,852 edges.
- Portal tablet Playwright suite: **PASS**, 3/3 required viewport tests.
- `git diff --check`: **PASS**.
- EF `migrations has-pending-model-changes`: **PASS**, no pending model changes.

## Architecture and workflow constraints

- Root `AegisCore.slnx` is canonical; `aegiscore-backend/` was deleted in P10.
- Read/query `graphify-out/` before each prompt. The verified installed-CLI refresh command is `graphify update .`.
- Finish one prompt, run Release build/tests, refresh Graphify, commit, and push before starting the next.
- Preserve client-owned infrastructure and introduce no Nocturne-hosted dependency.
- `AEGISCORE_FINAL_17_PROMPTS.md` is user-confirmed source of truth and must be committed with the repository.
- P17 is verified in `dacb1435`; P11 is verified in `187970a2`.

## Exact next action

P1–P17 remain complete. P18–P19 changes are uncommitted and must not be marked complete:

- User approved adding `client-release.yml` because `release.yml` already exists and existing workflows must remain unchanged. Also approved production login, missing UI routes, and redundant client deployment work.
- Added a cookie-based Portal API proxy, production session loading, students/admissions/reports/settings/dashboard routes, grade submission limited to edited cells, and an admin geolocation check-in control.
- Extended the existing Playwright config with screenshot/demo modes. See `docs/CAPTURE_SUITE.md` for environment inputs and real fixture requirements. No role credentials or APP_URL were available; `screenshots/` and `recordings/` have not been generated.
- Current wizard order remains identity (including branding/templates), database, SSO, permissions, AI, import, review. This differs from the requested conceptual ordering; captures faithfully use the implemented seven-step wizard.
- Added `docker-compose.client.yml`, combined runtime packaging, health-checked ingress, two application replicas, Watchtower peer checks/draining/readiness waits, and client registry/deployment docs. Database/Redis/TLS are client-owned prerequisites prepared by Nocturne; schema changes and ingress maintenance are not automatically orchestrated by Watchtower.
- Licensing now validates on startup and every 24 hours, isolates installation lookup, restores valid status, consumes Basic/Standard/Premium tiers, and gates AI/proctoring/plagiarism. Middleware permits reads during lockdown; an EF interceptor blocks business SaveChanges from requests and background work. Existing offline validation/overrides are preserved.
- Final backend verification: 638 passed / 1 skipped / 0 failed (`dotnet test AegisCore.slnx -c Release --no-build -v quiet`); focused licensing 18 passed. Portal typecheck and lint pass. Client compose parses, has zero build directives, and existing workflows have no diff. DEPLOY.md prints on one A4 page at 11pt.
- Final Docker image build: PASS (`aegiscore-client:verification`). Image runs as non-root, contains API and standalone Portal, and inspection found no first-party C# source, PDBs, source maps, or installed .NET SDK. HAProxy configuration validates. Packaged Portal smoke checks: login 200, readiness without backend 503, cross-origin mutation 403. The isolated smoke container was removed.
- Final combined browser check: PASS, 10/10 with no retries (`portal-auth`, `keyboard-accessibility`, `grade-entry`, CLI `--timeout=120000`). The preceding run passed 9/10 but cold dashboard compilation exceeded 60 seconds on this 4 GB host; the final dashboard check completed in 2.6 seconds. This is isolated mocked UI regression evidence, not the requested real-role capture evidence.
- Graphify code refresh: PASS, 26,830 nodes, 52,085 edges, 1,332 communities. Existing community labels may need a separate semantic relabel; no LLM relabel was performed. Final `git diff --check` passes.
- Do not claim successful screenshots, a sub-four-minute demo, private registry publication/pull, or uninterrupted live updates until those artifacts/checks actually exist. As of `bcf49bf0` the P18–P19 code is pushed to `origin/main`; no registry publication (tagged image build/push) has been performed.

### P18–P19 follow-up fixes (session 01S3NT3haQuhMacUnpd7XTfe)

- `19A` — `release.yml` stays untouched (pre-existing push-to-`main` internal build: `aegiscore-api`/`aegiscore-portal` images + GitHub Release). The `v*.*.*` tag publisher is `.github/workflows/client-release.yml`, now hardened: header comment on the deliberate rename, `workflow_dispatch` with a regex-validated `tag` input and an `allow_bootstrap` boolean, one tag-resolution step feeding `:{tag}` + `:latest`, and the "require private package" guard skipped only when `allow_bootstrap=true`. `docs/CLIENT_REGISTRY_ACCESS.md` has a new "Release automation" section mapping this file to 19A and the first-publish bootstrap.
- `18A/18B` routes — `/grades` and `/timetable` have no page root; `next.config.ts` already redirects them to `/examinations` and `/classes` (now commented as intentional, kept in sync with the suite `aliases` map). `screenshots.spec.ts` now positively asserts the alias redirect and records it in an `Alias` column in `INDEX.md`; `demo.spec.ts` asserts `/grades → /examinations` before grade entry (flow otherwise unchanged). `fixture-server.mjs` needed no change — the alias target pages' on-load endpoints are all already stubbed.
- Synthetic capture quarantined — `CAPTURE_MODE=visual-demo` (`visual-demo.spec.ts`) now writes only to `screenshots/_synthetic/` and `recordings/_synthetic/` (`aegiscore-demo-synthetic.mp4`); it no longer overwrites `screenshots/INDEX.md`, `recordings/aegiscore-demo.*`, `run.json`, or `README.md`. `docs/CAPTURE_SUITE.md` documents the alias behaviour and the synthetic/real split. Stale local `screenshots/*` and `recordings/*` artifacts (incl. the old `aegiscore-demo.mp4` and `aegiscore-student-lifecycle.webm`) were deleted; both dirs are gitignored.
- Verification: `client-release.yml` YAML parses; `ci.yml`/`release.yml`/`deploy.yml`/`deploy-cloud.yml` have zero diff; `docker-compose.client.yml` still has zero `build:` directives; no new backend `.cs` changes; Portal `npm run typecheck` passes; `playwright test --list` resolves for `screenshots`, `demo`, and `visual-demo` modes. The full synthetic `visual-demo` e2e and the real role-credential capture run were not executed here.
- Committed and pushed: `bcf49bf0` `feat(p18-p19): capture suite, closed-source client delivery, license tiers` → `origin/main` (`f91b2ba4..bcf49bf0`). 65 files.
- Graphify refreshed with `graphify update .`: **26,862 nodes · 52,114 edges · 1,369 communities**, built from `bcf49bf0`, backup written to `graphify-out/2026-09-08/`. 281 communities were auto-renamed by hub; `graphify label` (LLM relabel) was not run — no `GEMINI_API_KEY`/`GOOGLE_API_KEY` available. This handoff-doc refresh follows in a second commit.

### Completion-checklist status (P18–P19)

| Item | Status |
| --- | --- |
| CODEX_HANDOFF.md read before code | done |
| Graphify queried + summarised | done |
| All paths verified before use | done |
| 18A screenshots `screenshots/{role}/{page}.png` (3 roles × 10+ pages) | **blocked** — suite complete (`tests/capture/screenshots.spec.ts`); needs live `APP_URL` + `ADMIN/FACULTY/STUDENT_*`; run externally ordered |
| 18A `screenshots/INDEX.md` | **blocked** — generated by the same run |
| 18B `recordings/aegiscore-demo.webm` (<4 min) | **blocked** — suite complete (`tests/capture/demo.spec.ts`); same prerequisites |
| 19A tag-triggered release workflow | done **as `client-release.yml`** (documented deviation; `release.yml` frozen per "add only") |
| 19B `docs/CLIENT_REGISTRY_ACCESS.md` | done |
| 19C `docker-compose.client.yml` zero `build:` | done (grep-verified) |
| 19D heartbeat wired (not assumed) + `LicenseTier` | done — `ProductionServicesExtensions.cs:247`, `LicenseService.cs` (heartbeat URL + tier parse), `LicenseLockoutMiddleware.cs` (premium gate), `LicenseContracts.cs` (`LicenseTier` enum) |
| 19E `DEPLOY.md` one page | done |
| No existing CI workflows modified | done (zero diff on `ci`/`release`/`deploy`/`deploy-cloud`) |
| No dev services / source mounts in client compose | done |
