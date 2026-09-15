# AegisCore scoped pilot readiness report

Date: 2026-09-15
Supported scope: local Windows Docker Desktop/WSL2 rehearsal with PostgreSQL. This report does not claim that the whole ERP is production-ready.

## Verified

- A clean PostgreSQL database applied the complete migration chain and completed the seven-step browser installation wizard. The wizard-created administrator then signed in through the normal login flow.
- The opt-in `pakistan-bscs-v1` scenario created 50 tenant-consistent applicants, users and students with real password hashes and role grants. Calling the seeder again preserves the cohort. A same-service-scope repetition defect involving shared permissions was found by the PostgreSQL integration test and fixed.
- Final database assertions returned: 50 students, 50 applicants, one student tenant, one applicant tenant, zero duplicate tenant/email identities, four Aisha Khan documents, an 84% passing admission test, 43 enrollments, 130 earned credits, a credit-weighted 3.30 CGPA, zero outstanding balance, four clearance records, and one alumni profile for 2030.
- The real 1920×1080 Playwright capture passed in 2.1 minutes. It used the portal and PostgreSQL-backed API, verified a 50-row cohort, signed in as the real administrator and student, checked persisted admission/academic/finance records, rejected three student-owned graduation writes with HTTP 403, rejected premature staff approval with HTTP 409, approved three clearances and graduation, accepted a repeated approval safely, then reloaded the persisted alumni profile.
- The resulting MP4 is H.264/yuv420p, 1920×1080 at 25 fps, 119.96 seconds, 10,580,429 bytes. Longer page holds, natural scrolling and paced graduation decisions replaced the initial hurried capture. Five 1920×1080 review frames and a whole-video contact sheet were visually inspected for admission, semester progress, final-year transcript, graduation review and alumni.
- Backend regression: 668 passed, two documented skips, zero failed. The separate live PostgreSQL/scenario run passed four of four tests. Frontend typecheck and the final production Docker build passed.
- The application upgraded from `aegiscore:cohort-v11` to `aegiscore:cohort-v12` against the populated database. No migrations were pending, persisted data remained, and readiness passed after API startup completed.
- Final local readiness reported PostgreSQL 17.11 and Redis connectivity, required environment/JWT/cloud checks passing, and runtime readiness `Ready`. One measured sample used 351.9 MiB for the app, 109.1 MiB for PostgreSQL, 8.43 MiB for Redis and 25.89 MiB for Mailpit. These figures are a single local sample, not capacity certification.
- PostgreSQL is the executed provider. The MySQL, SQL Server and Oracle migration projects still compile in the application/test builds; no live non-PostgreSQL database was tested.

## What the recording proves

The video is a repeatable demonstration of one focal student, Aisha Khan, inside a 50-student fictional cohort. The scenario API creates the historical records in PostgreSQL, and the UI reads those records through normal authenticated endpoints. The recording then performs the graduation and alumni transition live. Chapter cards clearly label the 2026–2030 academic time jump.

The recording does not show a person manually typing the original application, reading an OTP from Mailpit, uploading each document, sitting the admission test, entering four years of attendance and marks, or entering all 50 students individually. Those historical facts are scenario data, not mocked browser responses. `/demo/student-journey`, `capture:visual-demo`, and client-side synthetic fixtures are not used.

## Implemented but not verified end to end

- Existing applicant OTP, document upload/review, admission-test, admission/account activation, course, attendance, assessment, fees and email workflows remain available, but a single continuous browser test has not authored every historical record through those screens.
- The scenario stores document metadata and paths; this capture did not upload and restore a real applicant document payload through the browser.
- Manual SMTP through Mailpit, manual institutional-email provisioning and manual/sandbox payment are the supported demo choices. Their full operator workflows were not all performed in this recording.
- Snapshot/reset covers the database, storage directory, runtime configuration, data-protection certificate and key material together. The final rehearsal result is recorded below after execution.
- Client packaging is build-verified locally. Installer distribution from a private registry and a remote client upgrade have not been tested.

## Blocked by unavailable credentials

- The user confirmed that no signed offline pilot license and no license-authority URL/credentials are available. Licensed production behavior and license heartbeat cannot be verified.
- Private container registry access, production payment gateways, Google Workspace/Microsoft 365 provisioning, production SMTP, DNS/TLS and remote hosting credentials are unavailable.

No authentication or licensing bypass was enabled. `Runtime__DemoMode` remains false; demo data is enabled separately and explicitly. AI is disabled, so no local 7B model is required and no AI/OCR result is presented in the recording.

## Commands executed

```powershell
# Backend regression
dotnet test AegisCore.Tests\AegisCore.Tests.csproj -c Release --no-restore -m:1 -nr:false

# Live PostgreSQL/scenario tests used a dedicated aegiscore_test_cohort database
$env:PILOT_POSTGRES_CONNECTION='Host=127.0.0.1;Port=55432;Database=aegiscore_test_cohort;Username=aegiscore_pilot;Password=<local-demo-password>'
dotnet test AegisCore.Tests\AegisCore.Tests.csproj -c Release --no-restore -m:1 -nr:false --filter 'FullyQualifiedName~DemoScenarioTests|FullyQualifiedName~PostgresPilotTests'

npm.cmd --prefix AegisCore.Portal run typecheck
docker.exe build --tag aegiscore:cohort-v12 --progress plain .
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Start -Image aegiscore:cohort-v12 -EnableDemoSeed -Scenario pakistan-bscs-v1 -DatabaseName aegiscore_cohort_v1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Check -Image aegiscore:cohort-v12 -DatabaseName aegiscore_cohort_v1
```

## Artifacts

- `recordings/aegiscore-50-student-lifecycle.mp4` — requested shareable recording.
- `recordings/aegiscore-50-student-lifecycle.webm` — original Playwright recording, retained locally.
- `recordings/lifecycle-run.json` — machine-readable run identity and completion state.
- `recordings/chapters/` — five visually checked 1920×1080 frames.
- `.local/test-results/cohort-regression.trx` — full backend regression result.
- `.local/test-results/cohort-final.trx` — focused PostgreSQL/scenario result.
- `docs/WINDOWS_PILOT_RUNBOOK.md` — repeatable operator commands.

Generated secrets and local snapshots remain under ignored `.local/` paths and must not be published.
