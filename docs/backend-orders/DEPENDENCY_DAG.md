# Backend Dependency DAG

## Rules

1. `ORDER_INDEX.md` owns status and priority; this file owns dependency shape.
2. Only `READY` nodes with a materialized order file may execute.
3. A completed order changes source truth. Every dependent `PLANNED_REVERIFY_AFTER_DEPENDENCIES` node must receive a new symbol audit and base commit before becoming `READY`.
4. Blocked nodes do not authorize a worker to choose policy, migration lineage, credentials, mappings, or institutional defaults.
5. Domain waves may run in parallel only when they do not modify the same entities, `AegisDbContext`, migration snapshot, DI registration, or middleware pipeline.

## Critical Spine

```text
MST-001 + MST-002 -> MST-003
MST-001 -> PLAT-002
PLAT-001 + MST-002 -> PLAT-003
PLAT-001 -> PLAT-004
PLAT-003 + PLAT-004 -> PLAT-005
PLAT-005 -> PLAT-006
PLAT-006 -> PLAT-007
PLAT-005 -> PLAT-008
PLAT-004 + PLAT-008 -> PLAT-009
PLAT-006 + PLAT-007 + PLAT-009 -> PLAT-010
PLAT-003 -> SEC-003
MST-001 + PLAT-003 -> SEC-004
SEC-003 + SEC-004 + MST-003 -> SEC-007
SEC-003 + PLAT-003 -> SEC-008
MST-001 -> OPS-001
MST-001 + MST-002 + OPS-005 -> OPS-008
```

## Domain Chains

```text
Academics:
SEC-004 -> ACAD-001 -> ACAD-002
ACAD-003 + HR-001 -> ACAD-004 -> ACAD-005
ACAD-004 + FAC-003 -> ACAD-006
ACAD-001 + ACAD-004 -> ACAD-007
ACAD-008 -> ACAD-009 -> ACAD-013
ACAD-008 + ACAD-009 + DOC-004 + LIB-004 -> ACAD-014
ACAD-003 + ACAD-008 -> ACAD-010 -> ACAD-011
ACAD-004 + FAC-003 -> ACAD-012 -> ACAD-013

Finance:
MST-002 -> FIN-002 -> FIN-003 -> FIN-004 -> FIN-005
FIN-001 + FIN-003 -> FIN-006 -> FIN-007
FIN-003 -> FIN-008 -> FIN-009
HR-005 + FIN-003 -> FIN-010
FIN-003 + DOC-004 + OPS-003 -> FIN-011

Health:
MST-002 + PLAT-003 + MST-003 -> HLTH-001 -> HLTH-002
HLTH-002 -> HLTH-003 -> HLTH-004 -> HLTH-005
HLTH-001 + HLTH-002 + PLAT-009 -> HLTH-006
HLTH-002 + DOC-004 -> HLTH-007

Library and documents:
PLAT-003 + MST-003 -> LIB-001 -> LIB-002 -> LIB-003 -> LIB-004
LIB-001 + LIB-002 -> LIB-005
PLAT-006 + PLAT-007 -> DOC-001 -> DOC-002
DOC-001 -> DOC-003 -> DOC-004

HR and service desk:
SEC-004 + PLAT-003 -> HR-001 -> HR-002
HR-001 + ACAD-004 -> HR-003 -> HR-004 -> HR-005
HR-002 + HR-005 + DOC-004 + SD-002 -> HR-006
MST-002 + MST-003 + PLAT-003 -> SD-001 -> SD-002 -> SD-003

Facilities:
PLAT-003 + MST-002 -> FAC-001
FAC-001 + PLAT-004 -> FAC-003
FAC-001 + ACAD-004 -> FAC-002
FAC-003 -> FAC-004
FAC-003 + SD-002 -> FAC-005
DOC-001 + DOC-002 + OPS-003 + FAC-001 -> FAC-006 -> FAC-007

Operability:
MST-001 -> OPS-001
PLAT-002 + DOC-001 + OPS-001 -> OPS-002
PLAT-008 + PLAT-009 + OPS-001 -> OPS-003
DOC-001 + OPS-001 -> OPS-004
MST-002 + PLAT-009 -> OPS-005
OPS-002 + OPS-005 -> OPS-006
PLAT-006 + PLAT-007 + OPS-001 -> OPS-007
MST-001 + MST-002 + OPS-005 -> OPS-008
OPS-001 + ACAD-011 + FIN-011 + DOC-004 -> OPS-009
PLAT-010 + OPS-004 + OPS-006 + OPS-008 + OPS-009 -> OPS-010
```

## Safe Execution Waves

| Wave | Orders | Gate |
|---|---|---|
| 0 | PLAT-001 | Immediately executable; no production files or broken main test project touched. |
| Decision gate | MST-001, MST-002, PLAT-004 | Human decisions required; workers must not improvise. |
| 1 | MST-003, PLAT-002, PLAT-003, OPS-001 | Materialize separately after decision commits; avoid concurrent `AegisDbContext` edits. |
| 2A | PLAT-005, SEC-003, SEC-004 | Reverify middleware, DI, migrations, and test baseline first. |
| 2B | PLAT-008 | Execute only after PLAT-005 completes and refresh distributed-state symbols. |
| 2C | PLAT-009 | Execute only after PLAT-008 and PLAT-004 complete. |
| 2D | OPS-005 | Execute only after PLAT-009 and MST-002 complete. |
| 2E | OPS-008 | Execute only after OPS-005 and both master decisions complete. |
| 3 | Tenant/security corrections in each domain | Execute bounded entity batches, not one repository-wide migration. |
| 4 | Canonical domain workflows | Registration, finance, health, library, HR/service desk, facilities. |
| 5 | Provider substitution, async generation, performance, release certification | Requires stable contracts and representative data. |

## Serialization Hotspots

Orders touching any of these must not execute concurrently unless the order explicitly defines a merge boundary:

- `AegisCore.Infrastructure/AegisDbContext.cs`
- `AegisCore.Infrastructure/Migrations/AegisDbContextModelSnapshot.cs`
- new EF migrations
- `AegisCore.Infrastructure/InfrastructureServiceExtensions.cs`
- `AegisCore.API/Program.cs`
- authentication and tenant middleware
- `User`, `Student`, `Employee`, `Faculty`, `Course`, `CourseGrade`, `JournalEntry`, and `Space` source-of-truth entities
