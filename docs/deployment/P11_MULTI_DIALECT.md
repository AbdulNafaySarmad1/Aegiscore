# P11 Multi-Dialect Database Support

Updated: 2026-09-06

## Supported primary databases

AegisCore selects one primary relational provider at process startup with
`DB_DIALECT`. The setting is optional and defaults to `postgres` for backward
compatibility.

| `DB_DIALECT` | EF Core provider | Migration assembly | Wizard version query |
|---|---|---|---|
| `postgres` | Npgsql | `AegisCore.Infrastructure` | `SELECT version()` |
| `mysql` | MySql.EntityFrameworkCore | `AegisCore.Migrations.MySql` | `SELECT VERSION()` |
| `mssql` | Microsoft.EntityFrameworkCore.SqlServer | `AegisCore.Migrations.MsSql` | `SELECT @@VERSION` |
| `oracle` | Oracle.EntityFrameworkCore | `AegisCore.Migrations.Oracle` | `SELECT * FROM v$version` |

The repository remains on EF Core 10. Pomelo's released package targets EF Core
9, so the MySQL adapter deliberately uses Oracle's EF Core 10-compatible
`MySql.EntityFrameworkCore` provider and its `UseMySQL` extension. Revisit
Pomelo only after an EF Core 10-compatible stable release is available.

## Adapter contract

`IDbDialectAdapter` owns provider registration, options, enum storage, JSON
storage, tenant-session compatibility SQL, version checks, and connection
creation. The application registers one adapter as a singleton. The context
factory, runtime context, deployment readiness check, primary health check, and
wizard all resolve the same dialect contract.

Legacy entity annotations are normalized after model configuration:

| Logical value | PostgreSQL | MySQL | SQL Server | Oracle |
|---|---|---|---|---|
| JSON document | `jsonb` | `json` | `nvarchar(max)` | `CLOB` |
| Binary payload | `bytea` | `longblob` | `varbinary(max)` | `BLOB` |
| Unbounded text | `text` | `longtext` | `nvarchar(max)` | `CLOB` |
| Enum | native PostgreSQL enum | string | string | string |

Filtered indexes and quoted SQL expressions are normalized where a provider
requires different syntax. Generated non-PostgreSQL snapshots contain no
`jsonb` or `bytea` store types.

## Migration strategy

P11 implements Option B: a migration assembly per dialect.

SQL Server database-side cascades are normalized to `NO ACTION` to avoid its
multiple-cascade-path restriction while application/EF relationship handling
remains portable. Oracle decimals without an explicit store type use Oracle's
existing `18,2` default, recorded as canonical `decimal(18,2)` metadata so the
generated snapshot remains stable across design-time reloads.

- PostgreSQL retains its existing, already-deployed migration history in
  `AegisCore.Infrastructure/Migrations`.
- MySQL starts with
  `AegisCore.Migrations.MySql/Migrations/20260906110629_MultiDialectBaseline.cs`.
- SQL Server starts with
  `AegisCore.Migrations.MsSql/Migrations/20260906160208_MultiDialectBaseline.cs`.
- Oracle starts with
  `AegisCore.Migrations.Oracle/Migrations/20260906163548_MultiDialectBaseline.cs`.

Create future migrations against the selected assembly. For example:

```bash
DB_DIALECT=mysql AEGISCORE_DB_CONNECTION='...' \
  dotnet tool run dotnet-ef migrations add Example \
  --project AegisCore.Migrations.MySql/AegisCore.Migrations.MySql.csproj \
  --startup-project AegisCore.API/AegisCore.API.csproj \
  --context AegisDbContext --output-dir Migrations
```

Equivalent changes to the logical model must receive a migration in all four
assemblies. Never copy provider-generated SQL between assemblies.

## Tenant isolation

Every entity implementing `ITenantScoped` receives the
`AegisDbContext.CurrentTenantId` global query filter. Background cross-tenant
discovery uses `IgnoreQueryFilters` only to read tenant identifiers and then
binds each operation to one tenant.

Historical PostgreSQL Finance migrations contain enforced RLS policies.
They are preserved because changing an applied migration is unsafe and because
removing the policies is a security-sensitive database mutation. A
PostgreSQL-only connection interceptor therefore supplies the existing
`app.tenant_id` session value as defense in depth. MySQL, SQL Server, and
Oracle do not register this interceptor; their isolation is the global filter.
All historical Finance and RLS migrations carry a P11 portability note.

## Setup wizard

Wizard Step 2 presents all four dialects, selects the conventional port, stores
the dialect in persisted wizard state, creates the provider connection through
the adapter, and executes its version query. Migration activation also resolves
the provider-specific migration assembly. Generated environment output includes
`DB_DIALECT`.

Connection failures remain categorized as authentication, network/listener, or
general database failures without logging the password or connection string.

## Verification

`.github/workflows/ci.yml` has a fail-independent matrix that starts
PostgreSQL 17, MySQL 8.4, SQL Server 2022, and Oracle Free 23 containers and runs
`dotnet ef database update` with the matching migration project.

Local verification on 2026-09-06 applied PostgreSQL's complete historical
migration chain and each non-PostgreSQL baseline successfully against disposable
PostgreSQL 17, MySQL 8.4, SQL Server 2022, and Oracle Free 23 containers. All
four migration assemblies also return `No changes have been made to the model`
from `dotnet ef migrations has-pending-model-changes`.

Focused dialect/environment tests pass 21/21.

Focused unit coverage in `DatabaseDialectTests` verifies:

- adapter resolution, aliases, connection types, and exact version queries;
- provider-native JSON, binary, and text model metadata;
- all four wizard connection-string formats;
- the environment registry and PostgreSQL default.

PostgreSQL-native physical backup and read-replica promotion commands remain
provider-specific operational integrations. On MySQL, SQL Server, and Oracle,
operators must configure the equivalent vendor backup and replication tooling;
this does not affect primary EF data access, migrations, readiness, or health.
