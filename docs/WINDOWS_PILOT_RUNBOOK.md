# Windows pilot rehearsal

This is a local engineering rehearsal, not a production deployment. See
`DEPLOY_READINESS_REPORT.md` for executed results and missing lifecycle work.
Use Docker Desktop with Linux containers/WSL2. No local LLM is required.

From the repository root, in PowerShell:

```powershell
docker build -t aegiscore:pilot-local .
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/pilot.ps1 Start -EnableDemoSeed -Scenario pakistan-bscs-v1 -DatabaseName aegiscore_scenario_v1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/pilot.ps1 Check
```

The execution policy override applies only to this process. The script binds
the application to `http://localhost:8080`, PostgreSQL to `127.0.0.1:55432`,
and the local email inbox to `http://localhost:8025`. It uses only containers
named `aegiscore-pilot-*` with the `aegiscore.scope=dedicated-demo` label.
It does not delete arbitrary volumes. PostgreSQL has a 1 GiB memory limit;
the application has 4 GiB, Redis and Mailpit 256 MiB each. These are limits,
not measured whole-stack usage.

Visit `/setup`. The generated setup token and keys are in
`.local/pilot/runtime.env`; do not share that file or capture it on video.
Complete the actual wizard and provision the administrator with a real password.
Configure SMTP for `aegiscore-pilot-mail:1025` without TLS/authentication on this
isolated network. Select manual institutional email provisioning. Use the
application's supported manual payments; external gateways are not verified.
Do not enable `Runtime:DemoMode`: that flag changes authentication/licensing.
The wizard has a trial option, but its post-activation license behavior must be
verified; no authority credentials were available for this rehearsal.

`-EnableDemoSeed` is the explicit opt-in. It binds the selected versioned scenario
to the configured tenant and generates a demo-only password in the ignored local
runtime environment. Institution, curriculum, accounts, applicant, fees and
academic history come from `demo-scenarios/<name>/scenario.json`; application code
and capture tests contain no scenario record IDs. Start recreates only its labeled
application container when its image or opt-in environment changes.
After normal administrator login with `Settings.Write`, submit
`POST /api/admin/demo-seed` through the same-origin portal. This endpoint is
absent in behavior (404) when opt-in is off. Account identifiers are read from the
selected scenario; the generated password applies to newly created accounts only.
Repeated seeds preserve existing passwords and data.
This seed is a small foundation, not five completed lifecycle snapshots.

The default recorded applicant is `aisha.khan@applicant.example.invalid` / Aisha Khan.
They must still verify OTP and submit using `/apply`. After admission, `/activate`
sends a single-use password setup link to the personal recovery email. No default
password is assigned to admitted students. `/alumni` reads only the signed-in
user's approved profile. The complete graduation approval workflow remains
unfinished; do not repair it through SQL for a purported real demo.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/pilot.ps1 Snapshot -Chapter application-v1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/pilot.ps1 Reset -Chapter application-v1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/pilot.ps1 Start
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/pilot.ps1 Check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/pilot.ps1 Stop
```

Snapshot stops the application while copying the database, storage, configuration
and key material. It records image identity, migration history and file hashes.
Reset rejects another image, validates hashes, and restores only dedicated demo
paths/database. It leaves the application stopped and removes its stopped container
so Start reloads restored environment settings. Take a new snapshot name for each
chapter; existing snapshots are not overwritten. Protect snapshots as credentials.
These operations are not a substitute for a tested client disaster-recovery policy.

Capture on Windows (from `AegisCore.Portal`):

```powershell
npm.cmd ci
npx.cmd playwright install chromium
$env:APP_URL='http://localhost:8080'
$env:PILOT_CAPTURE_ACK='dedicated-demo'
# Supply ADMIN_EMAIL/PASSWORD, STUDENT_EMAIL/PASSWORD, PILOT_STUDENT_ID.
npm.cmd run capture:lifecycle
```

The added lifecycle suite currently checks alumni boundaries; it is not a full
journey recording. It requires real login and backend responses, and records at
1920x1080. Existing `capture:demo` remains a separate shorter suite. Never substitute
`capture:visual-demo`, fixtures, or `/demo/student-journey` for backend evidence.
Once successful chapters exist, convert their WebM with
`ffmpeg -i chapter.webm -c:v libx264 -pix_fmt yuv420p -movflags +faststart chapter.mp4`.
Label academic time jumps in the chapter title and metadata; never imply years
of academic records were created during one continuous browser session.

Client updates require an approved immutable image/digest via
`AEGISCORE_RELEASE_IMAGE` and `AEGISCORE_CLIP_IMAGE`. Automatic application updates
are disabled. Review migration compatibility, back up all state, apply the reviewed
migration chain in a maintenance window, start the new image, then verify readiness
and real login. Roll back image and matching complete state together. The local
script does not yet automate a successful/failed client upgrade rehearsal.
