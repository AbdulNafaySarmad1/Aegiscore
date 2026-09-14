# AegisCore: Complete, Deploy, and Record the Student Lifecycle

## Objective
Make the real ERP ready for a scoped university pilot and a repeatable admission-to-alumni demo. Implement fixes and execute verification—not just an audit or plan. Target Windows with Docker Desktop/WSL2, 16 GB RAM, and an Intel i5 11th-gen H-series CPU. Verify PostgreSQL first; preserve other database providers.

## Start Here
Read `graphify-out/`, `CODEX_HANDOFF.md`, deployment/capture documentation, and relevant source. Recheck historical claims against current code and execution. Preserve existing user work. Do not commit, push, publish, or deploy remotely without explicit permission.

## Required Work
1. **Complete the real lifecycle.** Map and exercise UI → API → authorization → service → database for one fictional student: application/OTP/documents → admission test/scoring → admission/roll number/account migration → fees/course registration → timetable/attendance/assessments → results/CGPA/degree progress → project/internship requirements → clearance/graduation → alumni profile/access. Reuse existing components; implement missing connections and screens. Enforce prerequisites, ownership, tenant isolation, and safe repeated requests. Alumni creation must require approved graduation; students must not self-declare graduation. Verify calculations against underlying records.

2. **Replace unreliable demo seeding.** Investigate invalid UUIDs and `DEMO_HASH` in `scripts/demo-university-seed.sql`, logging-only seeders and `ISeedRunner` registration in `SeedPipeline.cs`, incomplete account relationships, and dangling references in demo seeders. Build one repeatable, idempotent, schema-aligned seeder using real password hashing and permissions. Include coherent fictional accounts, academic history, fees, and applicants. Keep all records tenant-consistent. Demo records must be opt-in and absent from fresh client installations.

3. **Make Windows startup repeatable.** Add documented PowerShell start, readiness-check, snapshot-reset, and stop commands. Align URLs, ports, environment settings, migrations, setup, storage, and keys. Use built images and real role logins; do not rely on administrator identity injection or mocked business APIs. Separate demo labeling from authentication bypass. Measure resource use; avoid requiring a local 7B model. Use a local email inbox, supported manual/sandbox payments, and manual email provisioning for the demo. Rehearse any AI/OCR feature included on camera.

4. **Prove deployment and recovery.** Test installation from an empty database, wizard completion, role login, persisted uploads, readiness, and restart. Establish versioned application/schema compatibility and controlled updates; do not let automatic image updates outrun migrations. Execute one upgrade and a failed-upgrade recovery rehearsal. Restore database, uploaded files, and required configuration/key material together. Verify client packaging locally; report registry, license, and external-service checks blocked by unavailable credentials separately.

5. **Record through Playwright.** Extend the real capture suite under `AegisCore.Portal/tests/capture/` to the complete lifecycle. Use actual UI actions and verify successful responses plus persisted outcomes. Prepare version-matched chapter snapshots of the same student for application, semester progress, final year, graduation, and alumni. Reset only the dedicated demo environment, never arbitrary volumes. Record readable 1920×1080 chapters, convert to MP4, and clearly label academic time jumps. Do not present `/demo/student-journey` or synthetic fixtures as real backend execution.

## Acceptance and Deliverables
- Run appropriate backend/frontend checks and real PostgreSQL browser tests, including unauthorized actions, unmet graduation requirements, and duplicate submissions. Fix failures; never weaken checks or fabricate success.
- A fresh installation completes the lifecycle without manual database repairs; changes survive refresh/restart; snapshot reset and backup restoration work.
- Deliver working scripts, connected seed/snapshots, Playwright tests/video, a concise Windows runbook, and `DEPLOY_READINESS_REPORT.md` listing commands, results, artifacts, unresolved blockers, and supported deployment scope.
- Clearly distinguish **verified**, **implemented but unverified**, and **blocked**. Do not declare the entire ERP production-ready based only on this lifecycle. Request only genuinely missing credentials or decisions, and continue independent work while blocked.
