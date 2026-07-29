# Test and Edge Case Report

Date: 2026-07-29

## Current Executed Tests

- Backend solution: `dotnet test Nocturne.slnx -m:1 -p:BuildInParallel=false`
- Result: 351 passed, 0 failed.
- Split backend: `dotnet test Nocturne.slnx -m:1 -p:BuildInParallel=false` in `nocturne-backend`
- Result: 351 passed, 0 failed.
- Portal source and split: `npm run build` and full `npm audit`
- Result: builds passed, full audits found 0 vulnerabilities.
- Mobile source: `npm run typecheck`, `npm test`, and full `npm audit` in `Nocturne.Mobile`
- Result: typecheck passed, 1 test passed, audit found 0 vulnerabilities.
- Mobile split: `npm run typecheck`, `npm test`, and full `npm audit` in `nocturne-mobile/react-native`
- Result: typecheck passed, 1 test passed, audit found 0 vulnerabilities.
- NuGet API graph: `dotnet list AegisCore.API/AegisCore.API.csproj package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`
- Result: no vulnerable packages.
- Docker Compose: `docker compose --env-file .env.example up -d --build`
- Result: API/PostgreSQL/Redis started; `/health` returned `Healthy`; Swagger JSON returned OpenAPI `3.0.1`; stack was stopped with `docker compose --env-file .env.example down`.

## Edge Cases Added In This Hardening Pass

- Setup wizard browser routes match backend routes.
- Redis and SMTP setup cannot report success while skipping real checks.
- Login stats no longer return hardcoded zero values.
- JWT auth no longer crashes when external authority is not configured for local/on-prem Compose.
- License status/management routes are reachable while the system is locked.
- Global exception middleware wraps auth and API middleware so failures return JSON and security headers.
- Expo mobile tests use Jest 29, matching `jest-expo`, instead of mixing Jest 30 runtime internals with Expo's Jest 29 preset.
- Policy service no longer embeds provider-specific `DateTimeOffset` comparisons in EF queries.
- Audit log immutability is now guarded in the PostgreSQL migration SQL with row-level security policies.
- S1 has SQLite-backed tests for policy service, academic calendar, permission authorization, and audit migration evidence.
- Portal dev dependency advisories were remediated by moving to patched Vite/Storybook/Cypress versions.

## Coverage Measurement

- `dotnet test AegisCore.Tests/AegisCore.Tests.csproj --collect:"XPlat Code Coverage"`
- Result: 351 passed; Cobertura line coverage is 1.89% (1,473/77,729) and branch coverage is 5.59% (222/3,970).
- The low aggregate rate is dominated by EF migration snapshots, entity models, controllers, and provider integrations that are not exercised by this unit suite. The 70% aggregate coverage target remains open; PostgreSQL/Redis/API, portal, and mobile offline-sync integration coverage is still required before claiming it.
