# ADR 0001: Multi-tenant modular monolith

## Status

Accepted.

## Decision

AegisCore is one complete university operating suite, implemented as a
multi-tenant modular monolith in a monorepo.

- Next.js 16 owns the browser experience.
- .NET 10 owns APIs, business rules, authorization, licensing, workflows,
  persistence, integrations, and background processing.
- PostgreSQL is shared infrastructure, but every business module owns its
  schema and migrations.
- Redis keys, object-storage paths, jobs, events, and audit records are
  tenant-scoped.
- Vertical slices organize use cases inside each module.
- Module-to-module collaboration uses contracts and events, never direct
  access to another module's tables or infrastructure types.

## Commercial invariant

Every valid AegisCore license activates the complete core suite. Core modules
are not editions, add-ons, feature entitlements, or separately priced items.
Pricing may vary by institution size, deployment model, migration effort,
integrations, infrastructure, and support, but never by core feature access.

Operational configuration may hide an unused workspace or disable an
institution-specific workflow. That configuration does not alter ownership,
licensing, or availability of the complete suite.

Extensions may integrate through versioned contracts, but cannot replace core
authorization, licensing, tenancy, audit, or data-ownership rules.

## Target repository shape

```text
apps/
  api/                 ASP.NET Core composition root
  portal/              Next.js 16
  worker/              background-job host
  setup/               installation tooling

modules/
  Identity/
  Admissions/
  Students/
  Academics/
  Attendance/
  Examinations/
  OBE/
  Finance/
  HR/
  Library/
  Transport/
  Hostel/
  Procurement/
  Communications/
  Support/
  Documents/
  Licensing/
  Audit/

building-blocks/
  SharedKernel/
  EventBus/
  Persistence/
  Observability/
  Security/

tests/
  Unit/
  Integration/
  Architecture/
  Contract/
  EndToEnd/

deploy/
  compose/
  azure/
  aws/
  gcp/
  bare-metal/
```

The physical migration to this layout is incremental and CI-gated. A folder
move is complete only when project references, Docker builds, Compose,
Kubernetes manifests, migrations, tests, and deployment runbooks use the new
path in the same commit.

## Boundary rules

1. No tenant-name conditionals in business logic.
2. Every tenant-owned row carries `TenantId`.
3. A module cannot query another module's tables.
4. A module cannot reference another module's infrastructure assembly.
5. SharedKernel contains only universal primitives, never `Student`, `Course`,
   `Employee`, `Invoice`, or another business entity.
6. The API and portal never decide commercial module availability.
7. Permissions determine what a user may do; tenant configuration determines
   how the institution operates.
8. Core module completeness is protected by architecture tests.

## Migration order

1. Establish this contract and remove module-level licensing.
2. Move executable hosts under `apps/`.
3. Extract building blocks from the current shared layers.
4. Move one business capability at a time into `modules/<Name>`.
5. Give each module its PostgreSQL schema and migration ownership.
6. Add dependency, schema-ownership, tenancy, and cross-tenant isolation tests.
7. Remove the transitional layer-first projects after the last module moves.
