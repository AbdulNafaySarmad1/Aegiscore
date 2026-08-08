# Backend Agent Report

## Baseline

- Branch: `agent/backend-sol`; initial worktree clean at `950a4043`.
- Restore: PASS with four `NU1510` package-pruning warnings.
- Release build: FAIL with 98 errors. Most failures came from platform services and demo data binding to pre-merge properties on canonical v2 entities.
- Tests: FAIL/BLOCKED because the failed build did not produce `AegisCore.Tests.dll`.

## Work Completed

- Reconciled compile-time usage of overlapping platform and v2 entity generations without deleting canonical entities or generating a destructive migration.
- Restored successful root-solution Release build and test execution.
- Hardened current-tenant enforcement and election ballot integrity.
- Updated stale controller fixtures to current entity shapes.

## Build Fixes

- Updated platform default prediction, visiting faculty, gamification, mental health, foreign student, voice, board, election, and demo seed usage to canonical property names and types.
- Converted money/rate/score literals to the correct decimal representation.
- Stopped demo Dean's List seeding because no authoritative semester IDs are available.

## Domain Fixes

- Added non-persisted legacy election response fields needed by the existing legacy controller contract.
- Added a non-persisted one-time plaintext voter-token response field while retaining `TokenHash` as the only persisted token value.

## EF / Database Fixes

- Added fail-closed current-tenant helpers to `AegisDbContext`.
- Removed inappropriate `IgnoreQueryFilters()` use from sensitive object-ID and tenant-bound paths in student lifecycle, default prediction, visiting faculty, board, cafeteria, and elections.
- EF pending-model check still reports drift. No migration was generated because collided old/v2 table generations require an explicit data-transition decision.

## Dependency Injection Fixes

- Updated legacy election service activation to include `AegisDbContext` so writes are committed transactionally.
- Full solution build verifies constructor resolution at compile time; no separate runtime service-provider activation sweep was added.

## Authentication Fixes

- Added HttpOnly access and refresh cookies for the Portal's existing same-origin proxy while preserving Bearer headers and JSON token responses for existing consumers.
- Refresh and logout now accept the refresh cookie; refresh-token consumption uses atomic Redis get-and-delete semantics to reject concurrent replay.
- Logout is idempotent and clears browser cookies.
- Added SignalR query-token support scoped to `/hubs/*`.
- Excluded expired role assignments from newly issued JWTs and permission checks.

## Authorization / Tenant Isolation Fixes

- Tenant-explicit methods now reject IDs that do not match the authenticated tenant.
- Tenant-sensitive lookups now rely on global filters rather than bypassing them.
- Removed arbitrary "first database tenant" and random tenant fallbacks from the repaired write paths.
- Global exception handling now maps authorization failures to generic HTTP 403 and argument validation failures to generic HTTP 400 without exposing exception details.
- Reconciled Casbin with the upstream ASP.NET authorization pipeline so ordinary authenticated roles are not rejected after satisfying endpoint role, permission, or fallback policies.
- `GET /api/session` now exposes active permissions as `Module:Resource:Action` capability keys.
- State-machine hub groups are tenant-prefixed and clients can no longer publish transition events.

## Finance / High-Risk Fixes

- Default-risk assessment, forecast, intervention, fee due, scholarship, payment, and hostel lookups no longer bypass tenant filters.
- No payment-success, ledger, webhook, signing, or reconciliation behavior was weakened or fabricated.

## API Contract Changes

- V2 ballot requests now require a single-use `voterToken`.
- Legacy semester strings must contain GUID semester IDs where canonical entities require `SemesterId`.
- Legacy election tally no longer fabricates results.
- Details are recorded in `docs/BACKEND_CONTRACT_NOTES.md`.

## Reliability / Concurrency Fixes

- V2 ballot insertion and voter-token consumption occur in one `SaveChangesAsync` unit.
- Legacy election mutations now call `SaveChangesAsync`; previously repository operations only changed EF tracking state.
- Ballot route/body election ID mismatches and duplicate position choices are rejected.

## Tests Added or Updated

- Added `TenantAndElectionIntegrityTests` for cross-tenant leaderboard isolation, tenant mismatch rejection, token-at-rest hashing, token replay rejection, and secure exception status mapping.
- Updated stale election, prediction, mental health, board, achievement, and foreign-student controller fixtures to canonical entity shapes.
- Extended `SecurityHardeningTests` for HttpOnly cookie issuance, cookie-based refresh, atomic refresh-token consumption, ordinary-role Casbin passage, and expired-role JWT exclusion.

## Commands Executed

- PASS: `git status`
- PASS: `git branch --show-current`
- PASS: `git log -1 --oneline --decorate`
- PASS: `dotnet restore AegisCore.slnx`
- FAIL (baseline): `dotnet build AegisCore.slnx --no-restore -m:1 -p:BuildInParallel=false --configuration Release`
- FAIL/BLOCKED (baseline): `dotnet test AegisCore.Tests/AegisCore.Tests.csproj --no-build --configuration Release -m:1 -p:BuildInParallel=false`
- PASS: focused Infrastructure and full-solution Release builds during the repair loop.
- PASS: focused `TenantAndElectionIntegrityTests` runs.
- PASS: `dotnet tool restore`
- FAIL: `dotnet ef migrations has-pending-model-changes --project AegisCore.Infrastructure/AegisCore.Infrastructure.csproj --startup-project AegisCore.API/AegisCore.API.csproj --no-build --configuration Release` (model drift detected).
- PASS (final): `dotnet restore AegisCore.slnx` (four `NU1510` warnings).
- PASS (final): `dotnet build AegisCore.slnx --no-restore -m:1 -p:BuildInParallel=false --configuration Release` (zero errors, 20 warnings).
- PASS (final): `dotnet test AegisCore.Tests/AegisCore.Tests.csproj --no-build --configuration Release -m:1 -p:BuildInParallel=false` (539 passed, one skipped).
- PASS (final): focused `SecurityHardeningTests` run (25 passed, one skipped).
- EXPECTED FAIL (final): `dotnet ef migrations has-pending-model-changes --project AegisCore.Infrastructure/AegisCore.Infrastructure.csproj --startup-project AegisCore.API/AegisCore.API.csproj --no-build --configuration Release` (known model drift remains; no migration generated).
- PASS (final): `git status`, `git diff --stat`, `git diff --check`, and `git branch --show-current`; branch remains `agent/backend-sol`, no whitespace errors were found, and no Portal file was modified.

## Remaining Failures

- EF reports pending model changes against the checked-in snapshot.
- One pre-existing oversized-upload boundary test is skipped.
- Build warnings remain, primarily package-pruning, unused logger, nullable initialization, and existing analyzer warnings.

## Deferred High-Risk Decisions

- Decide canonical ownership and data migration for collided legacy/v2 tables such as Dean's List, leaderboard, mental health, MORA, voice, board alerts, elections, cafeteria, visiting faculty, and cash-flow forecasts.
- Generate an additive, data-preserving migration only after mapping old columns and records to canonical fields.
- Replace the remaining placeholder prediction/tally integrations with evidence-based implementations where their source data or cryptographic workflow is currently unavailable.

## Frontend-Relevant Contract Notes

- Portal files were not modified.
- V2 election clients must retain the one-time issue-token response and submit it as `voterToken` when casting a ballot.
- Existing Portal election and several legacy module paths remain inconsistent with the current backend routes and need a frontend contract reconciliation pass.
- Added `docs/FRONTEND_API_HANDOFF.md` with auth/session examples, cookie and SignalR behavior, canonical roles, KPI response guidance, and known route mismatches.

## Security-Relevant Changes

- Tenant mismatches fail closed.
- Sensitive v2 queries no longer bypass tenant filters in repaired modules.
- Voter tokens use 256 bits of randomness, are SHA-256 hashed at rest, rotate before use, and reject replay.
- Voter-token consumption is an EF concurrency token, causing concurrent duplicate ballot attempts to roll back.
- Ballot choices are validated against eligible candidates and positions in the tenant-scoped election.
- Client error responses do not reveal exception types or stack details.

## Recommended Next Actions

1. Design and review the collided-table reconciliation migration with representative production data before applying it.
2. Add API integration tests that authenticate two tenants and exercise the hardened routes through middleware.
3. Reconcile Portal routes and request DTOs with v2 election, achievement, mental-health, foreign-student, visiting-faculty, and finance endpoints.
4. Audit remaining `IgnoreQueryFilters()` calls, retaining them only for explicitly tenant-predicated workers and administrative operations.
