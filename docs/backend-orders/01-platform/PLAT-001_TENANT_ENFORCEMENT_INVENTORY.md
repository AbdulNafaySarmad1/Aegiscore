# PLAT-001: Tenant Enforcement Inventory And Regression Gate

## Order Metadata

- ORDER_ID: `PLAT-001`
- STATUS: `READY`
- PRIORITY: `P0`
- EXPECTED_BASE_COMMIT: `d53b4b564661f0b990c59674386287c50364848f`
- DEPENDENCIES: `NONE`
- PRODUCTION_CODE_CHANGE: `NO`
- PURPOSE: Establish a deterministic, independently runnable inventory of mapped entity tenant-enforcement shape before any tenant migration changes source truth.

## Verified Current State

The following symbols and files are `VERIFIED_EXISTING` at the expected base commit:

- `AegisCore.Infrastructure.AegisDbContext` in `AegisCore.Infrastructure/AegisDbContext.cs`.
- `AegisDbContext.OnModelCreating` applies a global query filter only when an entity CLR type implements `ITenantScoped`, lines 1945-1962.
- `AegisCore.Domain.Common.ITenantScoped` in `AegisCore.Domain/Common/ITenantScoped.cs`.
- `User.TenantId` exists, but `User` does not implement `ITenantScoped`, in `AegisCore.Domain/Entities/User.cs`.
- `Student` has no direct `TenantId` and does not implement `ITenantScoped`, in `AegisCore.Domain/Entities/Student.cs`.
- `AegisCore.Infrastructure/AegisCore.Infrastructure.csproj` targets `net10.0` and references `AegisCore.Domain`.
- `.github/workflows/ci.yml` owns the current all-branch CI and has a `build-backend` job.
- The root test project is currently unable to compile because deleted analytics symbols remain referenced. This known baseline failure must not be hidden, suppressed, or changed by this order.

The following are `TO_BE_CREATED`:

- `tools/AegisCore.TenancyAudit/AegisCore.TenancyAudit.csproj`
- `tools/AegisCore.TenancyAudit/Program.cs`
- `tools/AegisCore.TenancyAudit/tenant-enforcement-baseline.json`
- `tools/AegisCore.TenancyAudit/README.md`

The following is `TO_BE_MODIFIED`:

- `.github/workflows/ci.yml`

No symbol is `TO_BE_MIGRATED`, `TO_BE_DEPRECATED`, or `TO_BE_REMOVED` in this order.

## Target State

Create a standalone .NET 10 console audit tool that references `AegisCore.Domain` and `AegisCore.Infrastructure`, inspects public `DbSet<TEntity>` properties on `AegisDbContext` without constructing a context or connecting to a database, and produces one sorted record per distinct mapped CLR entity.

Each record must contain:

- fully qualified CLR type;
- table name from `[Table]` when present, otherwise CLR name;
- whether a public instance `TenantId` property exists;
- whether the type implements `ITenantScoped`;
- structural category exactly one of:
  - `FILTERED_DIRECT`: has `TenantId` and implements `ITenantScoped`;
  - `UNFILTERED_DIRECT`: has `TenantId` and does not implement `ITenantScoped`;
  - `NO_DIRECT_TENANT`: has no `TenantId` and does not implement `ITenantScoped`;
  - `INVALID_FILTER_MARKER`: implements `ITenantScoped` but no public `TenantId` property is visible.

The tool must support:

- `--write-baseline <path>`: write deterministic indented JSON with ordinal sorting and a schema version;
- `--check <path>`: compare current inventory with the checked-in baseline and return nonzero on any added, removed, renamed, or recategorized entity;
- default report mode: print category counts and all non-`FILTERED_DIRECT` entity names;
- `--json`: print the current deterministic inventory to standard output.

The baseline is a structural snapshot, not a declaration that current entities are safe. The README and console output must state this explicitly.

Add a separate `tenant-enforcement-inventory` CI job to `.github/workflows/ci.yml`. It must restore only the audit project and run `--check` against the checked-in baseline. It must not depend on or weaken the existing backend build/test job.

## Change Envelope

Allowed files:

- `.github/workflows/ci.yml`
- `tools/AegisCore.TenancyAudit/AegisCore.TenancyAudit.csproj`
- `tools/AegisCore.TenancyAudit/Program.cs`
- `tools/AegisCore.TenancyAudit/tenant-enforcement-baseline.json`
- `tools/AegisCore.TenancyAudit/README.md`

Allowed new files: only the four files under `tools/AegisCore.TenancyAudit/` listed above.

Forbidden files:

- all `AegisCore.API/**` production source;
- all `AegisCore.Application/**` production source;
- all `AegisCore.Contracts/**` production source;
- all `AegisCore.Domain/**` production source;
- all `AegisCore.Infrastructure/**` production source and migrations;
- `AegisCore.Tests/**`;
- `AegisCore.Portal/**`;
- prompt files;
- stale lowercase source trees.

Do not add the audit project to `AegisCore.slnx` in this order because the root solution has a separately blocked lineage issue.

## Domain And Tenant Invariants

- The tool is read-only and must never open a database connection.
- The tool must not infer tenant safety through navigation properties.
- `UNFILTERED_DIRECT` and `NO_DIRECT_TENANT` are risk inventory categories, not exemptions.
- A newly mapped entity or changed category must fail `--check` until an architect refreshes the baseline in a tenant-remediation order.
- `Guid.Empty`, first-row tenant inference, environment fallback, and RLS assumptions are outside this tool and must not be represented as safe.

## Authorization And Ownership

The tool processes source metadata only and has no runtime authorization surface. It must not read configuration values, environment secrets, connection strings, or user data.

## Data Model, Migration, And Backfill

- No schema change.
- No EF migration.
- No model snapshot edit.
- No data backfill.

## API, Service, Event, And Job Contracts

- No application API, service, event, or background-job contract changes.
- CI invokes the tool as a short-lived process only.

## Provider Abstraction

No provider dependency is permitted. The tool must run without PostgreSQL, Redis, Valkey, object storage, cloud credentials, or Docker.

## Failure Behavior

- Invalid arguments: print usage to standard error and return nonzero.
- Missing/unreadable baseline: return nonzero.
- Duplicate CLR types exposed through multiple `DbSet` properties: de-duplicate by fully qualified CLR type.
- Invalid filter marker: include the record and return nonzero in both `--check` and baseline-write modes.
- Inventory drift: print a concise added/removed/changed diff and return nonzero.
- Reflection/load failure: return nonzero without swallowing the exception context.

## Transaction, Concurrency, And Idempotency

- No transactions are used.
- Identical source assemblies must produce byte-identical baseline JSON except for a single final newline.
- Concurrent invocations must not use shared temporary files.
- Baseline writes use a temporary sibling file and atomic replace to avoid partial output.

## Cache Rules

No cache is permitted. Every invocation reflects the current built assemblies.

## Security, Privacy, And Audit

- Never print configuration, environment values, connection strings, or data rows.
- Do not execute static constructors beyond normal assembly/type loading where avoidable.
- Do not claim this inventory proves RLS, write stamping, ownership, or cross-tenant isolation.

## Observability

Console output must include:

- schema version;
- total distinct mapped CLR entity count;
- count per structural category;
- baseline check result;
- drift details when present.

No network telemetry is added.

## Performance Requirements

- Complete within 15 seconds on the repository CI runner after restore/build.
- Use one reflection pass and ordinal collections; no source-tree scan and no database model construction.
- Database indexes: not applicable; this order does not connect to or query a database.
- N+1 risk: not applicable; this order performs one in-memory reflection pass over distinct `DbSet<TEntity>` property types.

## Tests And Validation Commands

Run exactly:

```bash
pwd
git branch --show-current
git status --short
git rev-parse HEAD
git log --oneline -5
dotnet restore tools/AegisCore.TenancyAudit/AegisCore.TenancyAudit.csproj
dotnet build tools/AegisCore.TenancyAudit/AegisCore.TenancyAudit.csproj --no-restore --configuration Release
dotnet run --project tools/AegisCore.TenancyAudit/AegisCore.TenancyAudit.csproj --configuration Release --no-build -- --check tools/AegisCore.TenancyAudit/tenant-enforcement-baseline.json
dotnet run --project tools/AegisCore.TenancyAudit/AegisCore.TenancyAudit.csproj --configuration Release --no-build -- --json
git diff --check
git status --short
```

Also perform these objective spot checks against the JSON:

- `AegisCore.Domain.Entities.User` is `UNFILTERED_DIRECT`.
- `AegisCore.Domain.Entities.Student` is `NO_DIRECT_TENANT`.
- At least one current `ITenantScoped` entity is `FILTERED_DIRECT`.
- No record is `INVALID_FILTER_MARKER`.

Do not run or claim a green root solution build. Record the known analytics compile blocker as pre-existing if observed.

## Acceptance Criteria

1. The standalone project builds at the expected base commit.
2. Baseline generation is deterministic.
3. `--check` passes against the committed baseline.
4. A temporary test that changes one baseline category causes `--check` to fail with a clear diff; revert only that temporary test change before completion.
5. The CI job runs independently of the currently failing root test project.
6. No production, migration, portal, prompt, or existing test file changes.
7. The report explicitly states that structural inventory is not proof of tenant safety.

## Regression Gates

- Existing `.github/workflows/ci.yml` backend, frontend, and Docker jobs remain semantically unchanged.
- No package version outside the new audit project changes.
- No secret or connection string enters the baseline.
- Baseline ordering is stable across two consecutive writes.

## Rollback Plan

Remove the `tenant-enforcement-inventory` CI job and the four new audit-tool files. There is no schema, data, runtime, or deployment rollback.

## Forbidden Changes

- Do not add `ITenantScoped`, `TenantId`, filters, RLS, or migrations in this order.
- Do not suppress, skip, delete, or rewrite analytics tests.
- Do not alter the root solution to make CI green.
- Do not classify an entity as safe through a parent/navigation relationship.
- Do not modify stale duplicate trees.
- Do not commit or push unless the operator separately instructs it.

## Stop Conditions

Stop and report `ORDER_STALE` if:

- HEAD differs materially from the expected base commit;
- any verified symbol or allowed file is missing;
- another change already introduces an equivalent authoritative tenant inventory;
- generating the inventory requires a database connection or production code edit;
- the worktree is unexpectedly dirty.

Stop and report `BLOCKED` if deterministic reflection cannot enumerate mapped `DbSet<TEntity>` types without application startup.

## Mandatory Worker Contract

- Execute one approved order only. The architecture and change envelope in this order are authoritative.
- Before editing, run all repository checks, verify `EXPECTED_BASE_COMMIT`, inspect every `VERIFIED_EXISTING` symbol and relevant project/CI registrations, and run the narrowest baseline build.
- Stop with `ORDER_STALE` when HEAD drift is material. Stop when a verified symbol is missing, source contradicts the order, unexpected data loss is possible, invariants cannot be preserved, or the worktree is unexpectedly dirty.
- If client-specific information becomes necessary, stop with `CLIENT_INFORMATION_REQUIRED`; never fabricate it.
- Do not redesign, invent requirements/symbols, expand scope, fix unrelated defects, hard-code a tenant, bypass authorization, delete/disable tests, return placeholders, perform unrelated upgrades, expose secrets, or modify stale duplicate trees.
- Make the smallest coherent patch, preserve project conventions, reuse verified abstractions, and do not create a parallel truth store.
- Validate after each logical unit rather than accumulating an untested patch.
- Every write path must obey the order's tenant, authorization, audit, transaction, concurrency, idempotency, cache, security, and failure rules. Provider-specific behavior belongs only in an authorized adapter.
- For any schema work, follow the exact migration/backfill sequence, preserve data, add ordered constraints/indexes, validate upgrade/rollback, and never replace database guarantees with frontend checks. This order permits no schema work.
- Retries require idempotency. Cache/dependency failure must follow this order's specified behavior rather than an improvised fallback.
- Run every validation command and report actual output. Separate pre-existing failures from regressions and never claim an unrun check.
- Do not commit for this execution because the operator explicitly prohibited commits. If a future operator authorizes a commit, stage only this order's files and use `<type>(<area>): <summary> [PLAT-001]` without amending or rewriting history.
- Never push unless an operator explicitly instructs it; this execution prohibits pushing.

## Required Completion Report

Return exactly the fields required by `prompts/10_DEEPSEEK_V4_PRO_EXECUTION_CONTRACT.md`:

```text
ORDER_ID:
BASE_COMMIT:
FINAL_COMMIT:
STATUS: IMPLEMENTED | BLOCKED | ORDER_STALE
FILES_CHANGED:
FILES_CREATED:
FILES_REMOVED:
MIGRATIONS_CREATED:
DATA_BACKFILL:
TESTS_ADDED:
COMMANDS_RUN:
BUILD_RESULT:
TEST_RESULT:
ACCEPTANCE_CRITERIA_RESULTS:
TENANT_RESULT:
AUTHORIZATION_RESULT:
SECURITY_RESULT:
CONCURRENCY_RESULT:
PERFORMANCE_RESULT:
KNOWN_LIMITATIONS:
DEVIATIONS_FROM_ORDER:
BLOCKERS:
GIT_DIFF_SUMMARY:
```
