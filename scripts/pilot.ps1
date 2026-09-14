[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Start','Check','Snapshot','Reset','Stop')][string]$Action,
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,48}$')][string]$Chapter = 'foundation',
    [string]$Image = 'aegiscore:pilot-local',
    [switch]$EnableDemoSeed,
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,48}$')][string]$Scenario = 'pakistan-bscs-v1',
    [ValidatePattern('^aegiscore_[a-z0-9_]{1,40}$')][string]$DatabaseName = 'aegiscore_pilot'
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$state = Join-Path $root '.local/pilot'
$snapshot = Join-Path $state "snapshots/$Chapter"
$pg = 'aegiscore-pilot-postgres'
$app = 'aegiscore-pilot-app'
$redis = 'aegiscore-pilot-redis'
$mail = 'aegiscore-pilot-mail'
$network = 'aegiscore-pilot'
function Docker {
    & docker.exe @args
    if ($LASTEXITCODE -ne 0) { throw "Docker command failed: $($args[0])" }
}
function Exists([string]$name) {
    $names = @(Docker ps -a --format '{{.Names}}')
    return $names -contains $name
}
function Assert-Owned([string]$name) {
    $inspection = (Docker inspect $name | ConvertFrom-Json)[0]
    if ($inspection.Config.Labels.'aegiscore.scope' -ne 'dedicated-demo') {
        throw "Refusing to operate on unowned container $name"
    }
}
function Assert-StatePath([string]$path) {
    $absolute = [IO.Path]::GetFullPath($path)
    if (!$absolute.StartsWith($state + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Path is outside the dedicated demo state directory.'
    }
    if ((Test-Path -LiteralPath $absolute) -and ((Get-Item -LiteralPath $absolute).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Refusing a linked state directory.'
    }
}
function Secret {
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($bytes)
}
function Set-EnvSetting([string]$path, [string]$name, [string]$value) {
    $lines = @([IO.File]::ReadAllLines($path))
    $found = $false
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index].StartsWith("$name=", [StringComparison]::Ordinal)) {
            $lines[$index] = "$name=$value"
            $found = $true
        }
    }
    if (!$found) { $lines += "$name=$value" }
    [IO.File]::WriteAllLines($path, $lines)
}
function Stop-App {
    if (Exists $app) { Assert-Owned $app; Docker stop $app | Out-Null }
}
New-Item -ItemType Directory -Force -Path $state | Out-Null
Docker info --format '{{.ServerVersion}}' | Out-Null
switch ($Action) {
    'Start' {
        if ($Image -match ':latest$') { throw 'Use a versioned local image or digest.' }
        Docker image inspect $Image --format '{{.Id}}' | Out-Null
        $networks = @(Docker network ls --format '{{.Name}}')
        if ($networks -notcontains $network) { Docker network create --label aegiscore.scope=dedicated-demo $network | Out-Null }
        $envPath = Join-Path $state 'runtime.env'
        foreach ($directory in @('storage','config')) { New-Item -ItemType Directory -Force -Path (Join-Path $state $directory) | Out-Null }
        if (!(Test-Path -LiteralPath $envPath)) {
            $settings = @(
                'AEGISCORE_COMPONENT=application', 'ASPNETCORE_ENVIRONMENT=Production',
                'Runtime__DemoMode=false', 'Database__AutoMigrate=true',
                'TENANT_ID=eba35470-ad75-4cac-bb60-c8d4886ebfd7', 'DB_DIALECT=postgres',
                "AEGISCORE_DB_CONNECTION=Host=$pg;Port=5432;Database=$DatabaseName;Username=aegiscore_pilot;Password=local-fictional-pilot-only",
                "AEGISCORE_REDIS_CONNECTION=$redis`:6379", 'APP_URL=http://localhost:8080',
                'AEGISCORE_STORAGE_PROVIDER=local', 'AEGISCORE_STORAGE_LOCAL_PATH=/app/storage',
                'AEGISCORE_SECRET_PROVIDER=FILE', 'AEGISCORE_LOCAL_SECRET_PATH=/app/config/secrets/vault.bin',
                'DataProtection__KeyPath=/app/config/data-protection',
                "Email__SmtpHost=$mail", 'Email__SmtpPort=1025', 'Email__EnableSsl=false',
                'Email__FromAddress=admissions@pilot.example.invalid', 'Email__FromName=Fictional Pilot University',
                "APP_SECRET=$(Secret)", "AEGISCORE_SETUP_TOKEN=$(Secret)",
                "AEGISCORE_ENCRYPTION_KEY=$(Secret)", "AEGISCORE_PQC_KEY_BASE64=$(Secret)",
                "AEGISCORE_JWT_SECRET=$(Secret)", "Governance__FeedbackEncryptionKeyBase64=$(Secret)",
                "LICENSE_HMAC_SECRET=$(Secret)", 'AI_PROVIDER=disabled'
            )
            [IO.File]::WriteAllLines($envPath, $settings)
        }
        Set-EnvSetting $envPath 'AEGISCORE_DB_CONNECTION' "Host=$pg;Port=5432;Database=$DatabaseName;Username=aegiscore_pilot;Password=local-fictional-pilot-only"
        # Keep the local pilot's readiness contract explicit while retaining all
        # secrets in the dedicated state file. These values are local-only defaults.
        Set-EnvSetting $envPath 'DB_HOST' $pg
        Set-EnvSetting $envPath 'DB_PORT' '5432'
        Set-EnvSetting $envPath 'DB_NAME' $DatabaseName
        Set-EnvSetting $envPath 'DB_USER' 'aegiscore_pilot'
        Set-EnvSetting $envPath 'DB_PASS' 'local-fictional-pilot-only'
        Set-EnvSetting $envPath 'APP_ENV' 'Production'
        Set-EnvSetting $envPath 'SESSION_LIFETIME' '3600'
        Set-EnvSetting $envPath 'CF_TURNSTILE_SITE_KEY' 'local-pilot-disabled'
        Set-EnvSetting $envPath 'CF_TURNSTILE_SECRET_KEY' 'local-pilot-disabled'
        foreach ($key in @('PQC_MLKEM_PRIVATE_KEY','PQC_MLKEM_PUBLIC_KEY','PQC_MLDSA_PRIVATE_KEY','PQC_MLDSA_PUBLIC_KEY')) {
            $existing = [IO.File]::ReadAllLines($envPath) | Where-Object { $_.StartsWith("$key=", [StringComparison]::Ordinal) } | Select-Object -First 1
            if (!$existing) { Set-EnvSetting $envPath $key (Secret) }
        }
        $runtimeSettings = [IO.File]::ReadAllText($envPath)
        if (!$runtimeSettings.Contains('DataProtection__CertificatePath=')) {
            $certificatePath = Join-Path $state 'config/key-ring.pfx'
            if (Test-Path -LiteralPath $certificatePath) { throw 'Existing certificate has no configured password; restore its matching runtime.env.' }
            $certificatePassword = Secret
            $rsa = [Security.Cryptography.RSACng]::new(2048)
            try {
                $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=AegisCore Local Pilot Key Ring', $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
                $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5), [DateTimeOffset]::UtcNow.AddYears(2))
                try { [IO.File]::WriteAllBytes($certificatePath, $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $certificatePassword)) }
                finally { $certificate.Dispose() }
            } finally { $rsa.Dispose() }
            [IO.File]::AppendAllText($envPath, "`nDataProtection__CertificatePath=/app/config/key-ring.pfx`nDataProtection__CertificatePassword=$certificatePassword`n")
        }
        if (!$runtimeSettings.Contains('Email__SmtpHost=')) {
            [IO.File]::AppendAllText($envPath, "`nEmail__SmtpHost=$mail`nEmail__SmtpPort=1025`nEmail__EnableSsl=false`nEmail__FromAddress=admissions@pilot.example.invalid`nEmail__FromName=Fictional Pilot University`n")
        }
        if ($EnableDemoSeed) {
            $tenantLine = [IO.File]::ReadAllLines($envPath) | Where-Object { $_.StartsWith('TENANT_ID=', [StringComparison]::Ordinal) } | Select-Object -First 1
            if (!$tenantLine) { throw 'The runtime environment has no TENANT_ID.' }
            Set-EnvSetting $envPath 'DemoSeed__Enabled' 'true'
            Set-EnvSetting $envPath 'DemoSeed__TenantId' $tenantLine.Substring('TENANT_ID='.Length)
            Set-EnvSetting $envPath 'DemoSeed__ScenarioRoot' '/app/demo-scenarios'
            Set-EnvSetting $envPath 'DemoSeed__Scenario' $Scenario
            if (!([IO.File]::ReadAllText($envPath).Contains('DemoSeed__Password='))) { Set-EnvSetting $envPath 'DemoSeed__Password' (Secret) }
        }
        if (!(Exists $pg)) {
            Docker run -d --name $pg --label aegiscore.scope=dedicated-demo --network $network --memory 1g -p 127.0.0.1:55432:5432 -e POSTGRES_DB=$DatabaseName -e POSTGRES_USER=aegiscore_pilot -e POSTGRES_PASSWORD=local-fictional-pilot-only postgres:17-alpine | Out-Null
        } else {
            Assert-Owned $pg; Docker start $pg | Out-Null
            $attached = Docker inspect --format '{{json .NetworkSettings.Networks}}' $pg
            if ($attached -notmatch '"aegiscore-pilot"') { Docker network connect $network $pg }
        }
        $postgresReady = $false
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            & docker.exe exec $pg pg_isready -U aegiscore_pilot -d postgres *> $null
            if ($LASTEXITCODE -eq 0) { $postgresReady = $true; break }
            Start-Sleep -Seconds 1
        }
        if (!$postgresReady) { throw 'Dedicated PostgreSQL did not become ready.' }
        $databases = @(Docker exec $pg psql -U aegiscore_pilot -d postgres -At -c 'SELECT datname FROM pg_database')
        if ($databases -notcontains $DatabaseName) { Docker exec $pg createdb -U aegiscore_pilot $DatabaseName }
        # A completed wizard owns the tenant identity. Rebind the local runtime
        # to that persisted tenant before starting the API so login and seeding
        # cannot accidentally use the bootstrap placeholder.
        $tenantQuery = Join-Path $state 'tenant-query.sql'
        [IO.File]::WriteAllText($tenantQuery, 'SELECT "Id"::text FROM "Tenants" ORDER BY "CreatedAt" LIMIT 2;')
        Docker cp $tenantQuery "${pg}:/tmp/pilot-tenant-query.sql"
        $hasTenantTable = @(Docker exec $pg psql -U aegiscore_pilot -d $DatabaseName -At -c "SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Tenants' LIMIT 1" 2>$null)
        $tenantIds = if ($hasTenantTable -contains '1') { @(Docker exec $pg psql -U aegiscore_pilot -d $DatabaseName -At -f /tmp/pilot-tenant-query.sql 2>$null | Where-Object { $_ -match '^[0-9a-fA-F-]{36}$' }) } else { @() }
        if ($tenantIds.Count -eq 0) {
            $installationState = Join-Path $state 'config/installation.json'
            if (Test-Path -LiteralPath $installationState) { Remove-Item -LiteralPath $installationState -Force }
        }
        if ($tenantIds.Count -eq 1) {
            $tenantId = $tenantIds.ToString().Trim()
            Set-EnvSetting $envPath 'TENANT_ID' $tenantId
            if ($EnableDemoSeed) { Set-EnvSetting $envPath 'DemoSeed__TenantId' $tenantId }
        }
        foreach ($service in @(@($redis,'redis:7-alpine'), @($mail,'axllent/mailpit:v1.29.1'))) {
            if (!(Exists $service[0])) {
                $ports = if ($service[0] -eq $mail) { @('-p','127.0.0.1:8025:8025') } else { @() }
                Docker run -d --name $service[0] --label aegiscore.scope=dedicated-demo --network $network --memory 256m @ports $service[1] | Out-Null
            } else { Assert-Owned $service[0]; Docker start $service[0] | Out-Null }
        }
        if (Exists $app) {
            Assert-Owned $app
            $requestedImage = Docker image inspect $Image --format '{{.Id}}'
            $runningImage = Docker inspect $app --format '{{.Image}}'
            if ($EnableDemoSeed -or $requestedImage -ne $runningImage) { Docker rm -f $app | Out-Null }
        }
        if (!(Exists $app)) {
            Docker run -d --name $app --label aegiscore.scope=dedicated-demo --network $network --memory 4g --restart unless-stopped --env-file $envPath -p 127.0.0.1:8080:8080 --mount "type=bind,source=$state/storage,target=/app/storage" --mount "type=bind,source=$state/config,target=/app/config" $Image | Out-Null
        } else { Assert-Owned $app; Docker start $app | Out-Null }
        Write-Output 'Started. Run Check; configure local SMTP host aegiscore-pilot-mail:1025 in setup. Inbox: http://localhost:8025'
    }
    'Check' {
        Assert-Owned $pg
        Docker exec $pg pg_isready -U aegiscore_pilot -d $DatabaseName
        if (!(Exists $app)) { throw 'PostgreSQL is ready, but the application has not been created.' }
        Assert-Owned $app
        $response = Invoke-RestMethod 'http://localhost:8080/health/ready' -TimeoutSec 10
        $response | ConvertTo-Json
        if ($response.status -eq 'setup_required') { throw 'Installation wizard has not completed; business readiness is not verified.' }
        Invoke-RestMethod 'http://localhost:8080/api/runtime/ready' -TimeoutSec 10 | ConvertTo-Json
        Docker stats --no-stream --format '{{.Name}} {{.MemUsage}} {{.CPUPerc}}' $app $pg $redis $mail
    }
    'Snapshot' {
        Assert-Owned $pg
        if (Test-Path -LiteralPath $snapshot) { throw 'Snapshot already exists; choose a new chapter name.' }
        Assert-StatePath $snapshot
        if (!(Test-Path (Join-Path $state 'runtime.env'))) { throw 'Required runtime configuration is missing.' }
        $running = (Exists $app) -and ((Docker inspect --format '{{.State.Running}}' $app) -eq 'true')
        Stop-App
        try {
            New-Item -ItemType Directory -Path $snapshot | Out-Null
            Docker exec $pg pg_dump -U aegiscore_pilot -d $DatabaseName -Fc -f /tmp/pilot-snapshot.dump
            Docker cp "${pg}:/tmp/pilot-snapshot.dump" (Join-Path $snapshot 'database.dump')
            foreach ($entry in @('storage','config','runtime.env')) { Copy-Item -LiteralPath (Join-Path $state $entry) -Destination $snapshot -Recurse }
            $schemaQuery = Join-Path $state 'schema-query.sql'
            [IO.File]::WriteAllText($schemaQuery, 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";')
            Docker cp $schemaQuery "${pg}:/tmp/pilot-schema-query.sql"
            $schema = @(Docker exec $pg psql -U aegiscore_pilot -d $DatabaseName -At -v ON_ERROR_STOP=1 -f /tmp/pilot-schema-query.sql)
            $files = @(Get-ChildItem -LiteralPath $snapshot -File -Recurse | ForEach-Object {
                @{ path = $_.FullName.Substring($snapshot.Length + 1); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
            })
            @{ format = 1; chapter = $Chapter; database = $DatabaseName; commit = (git -C $root rev-parse HEAD); image = (Docker image inspect $Image --format '{{.Id}}'); schema = $schema; files = $files; recordedLifecycle = $false } |
                ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $snapshot 'manifest.json')
        } finally { if ($running) { Docker start $app | Out-Null } }
    }
    'Reset' {
        Assert-Owned $pg
        Assert-StatePath $snapshot
        $manifest = Get-Content -LiteralPath (Join-Path $snapshot 'manifest.json') -Raw | ConvertFrom-Json
        if ($manifest.format -ne 1 -or $manifest.database -ne $DatabaseName -or $manifest.image -ne (Docker image inspect $Image --format '{{.Id}}')) { throw 'Snapshot, database and application image versions do not match.' }
        foreach ($file in $manifest.files) {
            $source = Join-Path $snapshot $file.path
            Assert-StatePath $source
            if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $file.sha256) { throw 'Snapshot integrity check failed.' }
        }
        Stop-App
        Docker cp (Join-Path $snapshot 'database.dump') "${pg}:/tmp/pilot-restore.dump"
        Docker exec $pg pg_restore --list /tmp/pilot-restore.dump | Out-Null
        # Recreate only this fixed, ownership-checked demo DB so objects introduced
        # by a failed upgrade cannot survive a restore to the previous schema.
        Docker exec $pg dropdb -U aegiscore_pilot --force --if-exists $DatabaseName
        Docker exec $pg createdb -U aegiscore_pilot $DatabaseName
        Docker exec $pg pg_restore -U aegiscore_pilot -d $DatabaseName --exit-on-error --single-transaction /tmp/pilot-restore.dump
        foreach ($entry in @('storage','config','runtime.env')) {
            $target = Join-Path $state $entry
            Assert-StatePath $target
            if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
            Copy-Item -LiteralPath (Join-Path $snapshot $entry) -Destination $state -Recurse
        }
        if (Exists $app) { Assert-Owned $app; Docker rm $app | Out-Null }
        Write-Output 'Restored database, uploads, configuration and keys. Application remains stopped; Start and Check before recording.'
    }
    'Stop' {
        foreach ($name in @($app,$pg,$redis,$mail)) { if (Exists $name) { Assert-Owned $name; Docker stop $name | Out-Null } }
    }
}
