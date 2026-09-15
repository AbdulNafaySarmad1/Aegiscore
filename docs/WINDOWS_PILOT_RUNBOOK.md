# Windows PostgreSQL pilot rehearsal

Use Docker Desktop with Linux containers/WSL2. The local stack binds the portal to `http://localhost:8080`, PostgreSQL to `127.0.0.1:55432`, and Mailpit to `http://localhost:8025`. It uses only containers labeled `aegiscore.scope=dedicated-demo`; generated configuration and secrets stay under ignored `.local/pilot/` paths.

## Build and start

Run from the repository root in PowerShell:

```powershell
$image = 'aegiscore:cohort-v12'
$database = 'aegiscore_cohort_v2'

docker.exe build --tag $image --progress plain .
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Start `
  -Image $image -DatabaseName $database -EnableDemoSeed -Scenario pakistan-bscs-v1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Check `
  -Image $image -DatabaseName $database
```

`-EnableDemoSeed` is the explicit opt-in for the 50-student fictional scenario. Fresh client installations contain no demo records. `Runtime__DemoMode` stays false, authentication uses real password hashes and sessions, manual payments and manual institutional-email provisioning are selected, Mailpit provides the local inbox, and AI is disabled.

## Complete a fresh installation

The setup token and generated keys are in `.local/pilot/runtime.env`; keep that file private. The automated browser installation uses the real seven-step wizard:

```powershell
Set-Location AegisCore.Portal
$env:APP_URL = 'http://localhost:8080'
$env:PILOT_CAPTURE_ACK = 'dedicated-demo'
$env:DEMO_SCENARIO_PATH = (Resolve-Path ..\demo-scenarios\pakistan-bscs-v1\scenario.json).Path
npm.cmd run capture:install
Set-Location ..
```

The generated administrator login is stored in `.local/pilot/capture-admin.json`. The demo-only account password is stored as `DemoSeed__Password` in `.local/pilot/runtime.env`.

## Record the paced lifecycle

From `AegisCore.Portal`, load the private runtime values into the process without printing them:

```powershell
$settings = @{}
Get-Content -LiteralPath ..\.local\pilot\runtime.env | ForEach-Object {
  $index = $_.IndexOf('=')
  if ($index -gt 0) { $settings[$_.Substring(0,$index)] = $_.Substring($index+1) }
}
$admin = Get-Content -LiteralPath ..\.local\pilot\capture-admin.json -Raw | ConvertFrom-Json
$env:APP_URL = 'http://localhost:8080'
$env:PILOT_CAPTURE_ACK = 'dedicated-demo'
$env:ADMIN_EMAIL = $admin.email
$env:ADMIN_PASSWORD = $admin.password
$env:STUDENT_EMAIL = 'aisha.khan@fnu-pilot.example.invalid'
$env:STUDENT_PASSWORD = $settings['DemoSeed__Password']
npm.cmd run capture:lifecycle
```

The suite records at 1920×1080 and takes about two minutes. It pauses on each business page, scrolls long records, labels the 2026–2030 academic time jump, and performs graduation/alumni writes through the real backend. The scenario supplies the historical four-year data; the recording does not imply that those years elapsed during the browser session.

Convert the successful WebM to a widely playable MP4:

```powershell
$recordings = (Resolve-Path ..\recordings).Path
docker.exe run --rm --mount "type=bind,source=$recordings,target=/recordings" `
  jrottenberg/ffmpeg:7.1-alpine320 -y `
  -i /recordings/aegiscore-50-student-lifecycle.webm `
  -c:v libx264 -preset medium -crf 20 -pix_fmt yuv420p -movflags +faststart -an `
  /recordings/aegiscore-50-student-lifecycle.mp4
```

## Snapshot, restore, readiness and stop

```powershell
$image = 'aegiscore:cohort-v12'
$database = 'aegiscore_cohort_v2'
$chapter = 'alumni-50-v12'

powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Snapshot -Chapter $chapter -Image $image -DatabaseName $database
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Reset -Chapter $chapter -Image $image -DatabaseName $database
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Start -Image $image -DatabaseName $database -EnableDemoSeed -Scenario pakistan-bscs-v1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Check -Image $image -DatabaseName $database
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\pilot.ps1 Stop -Image $image -DatabaseName $database
```

Snapshot and reset keep the database, uploaded-file directory, runtime environment, data-protection certificate and required key material together. Reset refuses a mismatched image or database and never targets arbitrary Docker volumes.

Use immutable versioned images for updates. Back up the matching state first, apply the reviewed image, wait for API startup, and run `Check`. Roll back the image and its matching complete snapshot together. Private registry, real license authority, production SMTP/DNS/TLS, external payment gateways and Google/Microsoft provisioning require credentials that were unavailable for this rehearsal.
