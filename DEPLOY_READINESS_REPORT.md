# Deployment readiness — local pilot work

Date: 2026-09-14. Starting commit: `bc601492b4bb0a2eb3a75520b4c4afb9884e596f`.
User work preserved: untracked `DEPLOY_READINESS_PROMPT.md`. No commit, push,
publication, or remote deployment was performed.

**The requested complete lifecycle and pilot acceptance are not yet achieved.**
This report separates executed evidence from code changes and remaining work.
The supported scope is a local PostgreSQL engineering rehearsal, not a claim
that the ERP or even the whole student lifecycle is production-ready.

## Verified

- Docker Desktop/WSL2 engine started; Docker 29.7.2. No existing containers were
  present before creating the dedicated PostgreSQL container.
- PostgreSQL 17.11 (`postgres:17-alpine`, pulled digest
  `sha256:18cfe3ef5e6815560c98237d6216d1e5119702fb0f3894c8785dd58b8bbe5d73`)
  accepted connections on localhost:55432. Idle measurement: 36.45 MiB / 1 GiB,
  CPU 0.03%. This is database-only usage, not whole-system performance.
- Full PostgreSQL migration chain applied to an empty `aegiscore_pilot` database;
  no pending migrations remained. The new test initially failed on Npgsql's
  retry/transaction contract; the seeder now executes its transaction through
  the provider execution strategy, and the PostgreSQL seed test passed.
- Real PostgreSQL seeding twice preserved row counts, authenticated the generated
  password with the normal Argon2 hasher, confirmed account/student/role links,
  and checked tenant filtering and rejection of a mismatched tenant context.
- Backend regression run: **664 passed, 1 existing skip, 0 failed**; activation
  replay and wizard-schema checks passed separately.
- Frontend dependencies installed with `npm.cmd ci --ignore-scripts --no-audit
  --no-fund`; `npm.cmd --prefix AegisCore.Portal run typecheck` passed.
- PowerShell script parser and `node --check` for the capture launcher passed.
  `git diff --check` passed (Windows line-ending notices only).

## Implemented; verification limits

- Admission now links the authentication subject to `Student.Id`, requires a
  configured Student role, approved required documents and a passed configured
  test. Existing admitted accounts with separate IDs are not silently rewritten.
- `/activate` and `/api/auth/activation/*` provide first-password setup for newly
  admitted accounts through their recovery email. Tokens expire after 20 minutes;
  an atomic conditional database update prevents replay from resetting passwords.
  Live SMTP delivery and browser completion remain to be verified.
- Alumni creation requires `Graduation.Approve`, a persisted Graduated student
  and an attributable Graduation clearance. Identity, year and tenant are server
  derived. Stable IDs and conflict recovery handle repeated creation. `/alumni`
  reads the signed-in user's profile. This is a gate, **not a completed graduation
  approval engine**; no SQL-created graduation is claimed as lifecycle evidence.
- Degree audit uses credit-weighted CGPA including failed attempts, recognizes
  passing grades explicitly, avoids duplicate earned course credits, preserves
  tenant ID, and rejects eligibility from empty/missing audits. Transfer/repeat
  policy, elective grouping, assessment-to-final-grade propagation and project/
  internship requirements still need end-to-end validation.
- One opt-in `DemoSeeder` replaces independent dangling-reference seeders. It uses
  tenant-derived stable IDs, real hashing and explicit grants, creates linked
  accounts, a small history, fees and a draft applicant. Fresh client registration
  excludes demo data and logging-only placeholders. `/api/admin/demo-seed` requires
  normal administrator authorization and explicit configuration.
- The invalid SQL seed now fails immediately with instructions instead of writing
  `DEMO_HASH`, invalid UUIDs, or schema-inconsistent data.
- Client compose requires explicit application/sidecar image references and disables
  automatic updates. No database provider or migration assembly was removed.
- `scripts/pilot.ps1` supplies Start/Check/Snapshot/Reset/Stop for dedicated local
  containers. Restore checks ownership, state path bounds, image identity and file
  hashes; database, uploads, configuration and keys travel together. Execution of
  the complete startup/backup/restore sequence remains an acceptance requirement.
- Capture npm commands work with Windows syntax. The new `capture:lifecycle`
  suite covers alumni rejection boundaries with real login/API calls at 1920x1080.
  It is not a complete lifecycle recording. No successful MP4 is claimed.

## Lifecycle trace and unfinished work

Graduation evaluation, clearance and approval endpoints plus a connected review
page now fail closed on missing requirements, failed or ungraded work,
unreconciled fees and unattributed clearances. Positive browser approval remains
unverified.

| Stage | Existing/changed entry points | Remaining acceptance |
|---|---|---|
| Application, OTP, documents | `/apply`, ApplicantPortalController, ApplicantPortal services, ApplicantProfiles/Documents | Real local-inbox OTP, uploads, document review and refresh/restart |
| Admission test | `/terminal`, admissions test-config/question-bank/proctor, AdmissionTest services | Approved manual question pool, actual exam/scoring and persisted answer comparison |
| Admit/account | Admissions pipeline, AdmissionDecisionService, AdmissionConfirmedHandler, new activation route | Concurrent admission, welcome/activation delivery, institutional login; existing legacy ID compatibility |
| Fees and registration | Existing finance/enrollment services; coherent seed due | Manual payment verification, fee prerequisites and course registration UI |
| Teaching and results | Existing classes/attendance/examinations; DegreeAuditService | Timetable ownership, actual attendance/marks, released results and record-level CGPA reconciliation |
| Degree/project/internship | Existing degree requirements and clearance entities | Connected completion/approval workflow with supported curriculum rules |
| Graduation/alumni | StudentLifecycleController, new `/alumni` | Graduation approval engine and UI, complete clearance, alumni role/access migration |

Missing engineering work is not attributed to unavailable credentials. The five
version-matched chapters of the same student (application, semester, final year,
graduation, alumni), complete UI capture and MP4s, successful upgrade and failed-
upgrade recovery rehearsals are unfinished. No synthetic fixture is presented as
a real backend run.

## Commands and artifacts

Backend environment used `DOTNET_CLI_HOME=.local/dotnet` and
`NUGET_PACKAGES=.local/nuget`. Builds use `-m:1 -nr:false` to limit concurrency.

```powershell
dotnet test AegisCore.Tests/AegisCore.Tests.csproj -c Release --no-restore -m:1 -nr:false --logger 'trx;LogFileName=pilot-backend.trx' --results-directory .local/test-results
# For PostgreSQL tests, set PILOT_POSTGRES_CONNECTION to the dedicated local DB.
dotnet test AegisCore.Tests/AegisCore.Tests.csproj -c Release --no-restore -m:1 -nr:false --filter FullyQualifiedName~PostgresPilotTests
npm.cmd --prefix AegisCore.Portal run typecheck
docker build --tag aegiscore:pilot-local --progress plain .
```

Local artifacts: `.local/test-results/pilot-backend.trx`,
`.local/test-results/pilot-postgres.trx`, `.local/test-results/pilot-final.trx`
(latest run), `.local/pilot-image-build.log` (initial failed build using the
pre-fix seeder), and `.local/pilot-image-build-2.log` (corrected build).
Generated/private artifacts are excluded from Git and Docker build context.
Run instructions: `docs/WINDOWS_PILOT_RUNBOOK.md`.

## External checks and environment issues

- User confirmed no pilot license/authority credentials are available. No licensing
  or authentication bypass was enabled. Trial activation behavior still needs
  execution before deciding which business operations are blocked by licensing.
- Private registry pull, paid license heartbeat, Google Workspace/Microsoft 365,
  external payment gateways, and remote deployment are unverified. Manual local
  email/payment workflows are the intended demo scope.
- AI/OCR is not included in claimed verified camera footage. No 7B model was installed.
- Initial sandbox attempts could not access the Docker engine and .NET user cache;
  engine access was retried with approved access and caches moved into the workspace.
  Automatic review rejected terminating suspected orphaned build workers because
  their ownership was not sufficiently established. They were left alone; a safe
  single-worker restore succeeded without termination.
