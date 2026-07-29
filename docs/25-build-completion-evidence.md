# Nocturne Build Completion Evidence

Date: 2026-07-29

## Local Verification

- Backend solution build: `dotnet build Nocturne.slnx -m:1 -p:BuildInParallel=false`
  - Result: passed, 0 warnings, 0 errors.
- Backend tests: `dotnet test AegisCore.Tests/AegisCore.Tests.csproj -m:1 -p:BuildInParallel=false`
  - Result: 351 passed, 0 failed.
- Solution test pass: `dotnet test Nocturne.slnx -m:1 -p:BuildInParallel=false`
  - Result: 351 passed, 0 failed.
- Omitted root projects:
  - `dotnet build AegisCore.Setup/AegisCore.Setup.csproj --no-restore -p:BuildInParallel=false`: passed, 0 warnings, 0 errors.
  - `dotnet build AegisCore.Web/AegisCore.Web.csproj --no-restore -p:BuildInParallel=false`: passed, 0 warnings, 0 errors.
- Plugin projects:
  - Root and split `Nocturne.PluginCli` and `Nocturne.PluginSdk` builds passed with 0 warnings and 0 errors.
- Portal build: `npm run build` in `Nocturne.Portal`
  - Result: passed.
- Portal E2E: `npm run dev` plus `npm run test:e2e` in `Nocturne.Portal`
  - Result: Cypress `portals.cy.ts` passed, 1 passed, 0 failed.
- Portal production dependency audit: `npm audit --omit=dev`
  - Result: 0 vulnerabilities.
- Portal full dependency audit: `npm audit` in `Nocturne.Portal`
  - Result: 0 vulnerabilities after bumping Vite, Storybook, Cypress, and `@vitejs/plugin-react`.
- Mobile typecheck: `npm run typecheck` in `Nocturne.Mobile`
  - Result: passed.
- Mobile tests: `npm test` in `Nocturne.Mobile`
  - Result: 1 passed, 0 failed.
- Mobile Expo export: `EXPO_NO_TELEMETRY=1 npx expo export --platform all --output-dir dist-check`
  - Result: Android and iOS bundles exported.
- Mobile production dependency audit: `npm audit --omit=dev`
  - Result: 0 vulnerabilities.
- Native mobile source checks:
  - Android and iOS source/config inspected under `Nocturne.Native`.
  - Android Compose build config now applies `org.jetbrains.kotlin.plugin.compose` and enables Compose/build config.
  - Native API base URL is configurable through `NOCTURNE_API_BASE_URL`; no university-specific API URL remains in native source.
  - Local binary build not run: `gradle` and `swift` are not installed in this workspace image.
- Plugin packages:
  - `dotnet pack Nocturne.PluginSdk/Nocturne.PluginSdk.csproj -c Release -o /tmp/nocturne-pack-check`: produced `Nocturne.PluginSdk.1.0.0.nupkg` (6,055 bytes).
  - `dotnet pack Nocturne.PluginCli/Nocturne.PluginCli.csproj -c Release -o /tmp/nocturne-pack-check`: produced `Nocturne.PluginCli.1.0.0.nupkg` (15,734 bytes).
- Deployment manifest checks:
  - GitHub Actions deployment step now gates on `DEPLOY_ENABLED` and applies runtime ConfigMap/dependencies before the API deployment.
  - Kubernetes NetworkPolicy now allows DNS egress on TCP/UDP 53 plus HTTP/HTTPS egress.
  - Local server-side/client-side Kubernetes dry-run not run: `kubectl` is not installed in this workspace image.
- Docker images:
  - Root: `docker build --network=host -t aegiscore-api:alpine-verified .`
  - Split: `docker build --network=host -t aegiscore-api:split-alpine-verified .` from `nocturne-backend`.
  - Result: both passed using `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`; runtime installs no external Alpine packages.
- Docker smoke:
  - Root and split containers ran as the configured non-root `appuser`.
  - Endpoint: `GET /health` returned `Healthy` for both images.
  - Root image metadata: `user=appuser`, 109,831,744 bytes.
  - Disposable smoke-test containers were stopped after verification.
- Docker Compose local stack:
  - `docker compose --env-file .env.example config`: rendered API/Postgres/Redis stack with `NOCTURNE_DB_CONNECTION`, `NOCTURNE_STORAGE_LOCAL_PATH`, install-completed flag, and Postgres health dependency.
  - `docker compose --env-file .env.example up -d --build`: built and started API, PostgreSQL, and Redis.
  - Compose API status: `healthy`.
  - Endpoint: `GET /health` returned `200 Healthy`.
  - OpenAPI: `GET /swagger/v1/swagger.json` returned `200`, OpenAPI `3.0.1`, 280 paths, including admissions, curriculum, license, marketplace, and setup test routes.
  - Compose PostgreSQL migration history contained `20260726160353_V1_0_NocturneBaseline`.
  - Cleanup: `docker compose --env-file .env.example down` stopped and removed containers/network.
- Security dependency audit:
  - `dotnet list AegisCore.API/AegisCore.API.csproj package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`: no vulnerable packages.
  - `npm audit` in `Nocturne.Portal`, `nocturne-frontend`, `Nocturne.Mobile`, and `nocturne-mobile/react-native`: 0 vulnerabilities.
  - Snyk CLI authentication: `snyk whoami` succeeded, but authenticated `snyk code test --json` did not return within the bounded scan window and was stopped; no Snyk finding was available to triage.
- Split repository validation:
- `nocturne-backend`: `dotnet test Nocturne.slnx -m:1 -p:BuildInParallel=false` passed, 351 tests; NuGet vulnerability audit found no vulnerable packages.
  - `nocturne-frontend`: `npm run build` passed; full `npm audit` found 0 vulnerabilities.
  - `nocturne-mobile/react-native`: `npm run typecheck` passed; `npm test` passed, 1 test; full `npm audit` found 0 vulnerabilities.
- Secret scan:
  - `rg` scan found no dummy keys/passwords in runtime source/manifests outside example env documentation.
  - Compose now uses environment interpolation instead of hardcoding dummy runtime secrets in `docker-compose.yml`.
- Database migration artifacts:
  - `AegisCore.Infrastructure/Migrations/20260726160353_V1_0_NocturneBaseline.cs`
  - `AegisCore.Infrastructure/Migrations/AegisDbContextModelSnapshot.cs`
  - `AegisCore.Infrastructure/Migrations/V1.0_NocturneBaseline.sql`
  - `dotnet tool run dotnet-ef migrations add V1_0_NocturneBaseline ... --no-build`: generated baseline migration.
  - `dotnet tool run dotnet-ef migrations script ... --idempotent`: generated 364,127-byte PostgreSQL SQL script.
  - Disposable `postgres:17-alpine` on `localhost:15432`: `database update` applied `20260726160353_V1_0_NocturneBaseline`; `migrations list` returned the applied migration.

## Latest Corrections

- Removed startup `BuildServiceProvider` usage from `Program.cs`.
- Replaced course semester placeholder logic with curriculum-semester lookup.
- Added portal plugin marketplace/developer earnings screen.
- Replaced backup/restore command logging with real PostgreSQL process execution using `PGPASSWORD` instead of logging secrets.
- Removed Infrastructure build warnings.
- Added design-time `AegisDbContextFactory`, EF baseline migration, idempotent SQL script, and opt-in runtime migration hook.
- Fixed PostgreSQL filtered-index SQL for `ResearchPatents.PatentNumber` migration generation.
- Refreshed portal build, Cypress E2E, mobile typecheck/test, and production dependency audit evidence.
- Fixed native Android Compose Gradle wiring and moved native API endpoint selection to `NOCTURNE_API_BASE_URL`.
- Repacked plugin SDK/CLI and fixed Kubernetes deployment ordering plus DNS egress.
- Fixed documented DB connection env resolution by supporting `NOCTURNE_DB_CONNECTION` in runtime and design-time DbContext creation.
- Fixed local Compose startup env, Postgres health dependency, auto-migration, and storage path naming.
- Fixed production startup crashes from unresolved search URL placeholders and duplicate infrastructure registration.
- Made public `/health` a top-of-pipeline liveness endpoint; authenticated `/health/detail` remains for dependency health checks.
- Removed dummy secrets from `docker-compose.yml`; local secrets come from `.env` or explicit `--env-file`.
- Registered Swagger generation in shared core services, using full-name schema IDs and conflict resolution so the production OpenAPI document renders.
- Added setup wizard route aliases for the browser UI and removed Redis/SMTP skipped-test success paths; setup now runs the real connection checks before reporting success.
- Split backend, frontend, Expo/mobile, and standalone Android working trees for repository separation prompts.
- Pinned the Expo mobile Jest runner to the Expo-compatible Jest 29 line so source and split mobile tests run cleanly.
- Added CORS origin configuration with wildcard subdomain support for local portal and hosted university domains.
- Added eight self-contained interactive HTML manuals under `manuals/`.
- Hardened S1 by adding named S3/Azure Blob/SFTP storage provider classes, provider-portable policy lookups, audit-log RLS migration SQL, and SQLite-backed tests for policy, calendar, audit migration evidence, and permission authorization.
- Remediated dependency advisories by bumping AutoMapper, Microsoft Graph/Kiota transitive graph, IdentityModel packages, ImageSharp, Vite, Storybook, Cypress, and `@vitejs/plugin-react`.
- Added U6 regression coverage for malformed signed licenses and automatic re-lock after an expired override; protected the heartbeat authority with `License:HeartbeatServerToken`.
- Added S2 admissions validation and workflow hardening: enquiry CRM endpoints, role-protected admin operations, HMAC tracking tokens, safe document signatures, CNIC/score/quota validation, and merit calculation guards.
- Added S3 examination hardening: semester-aware `CourseGrade` persistence with `V1_1_ExaminationSemesterGrades`, assessment scheme invariants, moderation locks, grace-mark bounds and audit events, protected result publication, and transcript verification API.
- Added S5 OBE/CIS hardening: protected CIS upload/process/approval and CLO-PO mapping endpoints, OCR-backed extraction status handling, attainment reports, and HEMIS XML export; root and split backend tests pass 53/53.
- Added S4 HR workflow endpoints for employee master, job postings, calendar-aware leave approval with attendance marking, payroll execution, bank export, and gratuity calculation; root and split backend tests pass 53/53.
- Added S7 finance hardening: validated fee/payment/scholarship APIs, idempotent double-entry fee posting, PIFRA XML export, budget variance, and procurement requisition APIs; root and split backend tests pass 53/53.
- Added S9 communications hardening: protected notification/template administration, authenticated notices/grievances, safer channel validation, configurable WhatsApp Graph versioning, SignalR integration, and five WhatsApp template definitions; root and split backend tests pass 53/53.
- Verified S8 student/faculty portal copies build successfully with the existing screen inventory, Redux state, SignalR client, responsive shell, dark mode, and accessibility labels.
- Verified S6 Microsoft ecosystem, S10 campus operations, S11 student lifecycle, S12 asset/HEMIS, S13 Expo mobile, U10 governance, U11 curriculum/offline attendance, and U14 SSO/HITL hardening; root and split backend suites remained 53/53.
- Added global authenticated fallback authorization, removed the production JWT fallback secret, and validated the backend Docker image with a successful multi-stage build.
- Final verification on 2026-07-29: root and split backend suites each passed 53/53; the final API image rebuilt successfully as `aegiscore-api:security-verified`; source hygiene and `git diff --check` passed; the final NuGet audit reported no vulnerable packages.
- License heartbeat hardening: both backend copies require the resolved heartbeat endpoint to be an absolute HTTPS URL before posting; root and split backend suites passed 53/53 after the change.
- Replaced the Debian API runtime with `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` in both backend trees; both images built successfully, the final image measured 113,152,000 bytes, and it returned `Healthy` from `GET /health` as a non-root container.
- Fixed the Kubernetes deployment secret wiring so production `NOCTURNE_JWT_SECRET` is supplied in both synchronized GitHub Actions workflows and documented in the deployment runbook.
- Fixed the license heartbeat configuration mismatch by allowing `License:ServerUrl` as the documented fallback endpoint and supplied `License__HmacSecret` through both production deployment workflows.
- Final Docker correction: removed runtime `apk add curl shadow` dependency and switched the image healthcheck to Alpine BusyBox `wget`, keeping both synchronized Dockerfiles buildable when Alpine package mirrors are unavailable.

## External Gates

These cannot be proven inside this local workspace and need real credentials/accounts:

- App Store and Play Store submission.
- Android APK/AAB and iOS IPA signing with real toolchains.
- Native Android/iOS local compilation until Gradle/Android SDK and Swift/Xcode toolchains are available.
- Production Kubernetes deployment with `KUBE_CONFIG` and runtime secrets.
- Kubernetes dry-run/apply validation until `kubectl` is available locally or in CI.
- Live payment gateways: JazzCash and EasyPaisa.
- Meta-approved WhatsApp templates and Business API send tests.
- Microsoft Graph, Teams, Power BI, Moodle LTI, SSO provider, Azure OCR, and cloud storage credential tests.
- HEC/PIFRA acceptance testing against live regulatory endpoints or official validators.
