# P4 — Tenant Query-Bypass Audit

Audit scope: `AegisCore.Infrastructure/` and `AegisCore.Application/`.

## Result

- Raw EF SQL calls: 7; all are tenant-safe after review.
- Dapper calls: 0. Dapper is not referenced by these projects.
- Direct ADO.NET/Npgsql call sites: 25 connection sites; tenant-data statements use explicit, parameterized tenant predicates. Infrastructure probes and external import-source queries do not access AegisCore tenant rows.
- `IgnoreQueryFilters()` calls: reduced from 245 to 28. The 218 request-path bypasses were removed. The remaining calls are limited to setup/bootstrap operations with an explicit tenant predicate, demo/bootstrap seeding, or background-worker tenant discovery.
- Sensitive `AsNoTracking()` calls involving `Attendance`, `CourseGrade`, `SsoSession`, or `AdmissionApplication`: 0.

## Raw EF SQL calls

| File | Line | SQL | Tenant-safe? | Finding |
|---|---:|---|---|---|
| `Finance/VendorSanctionsService.cs` | 55 | `SELECT set_config('app.tenant_id', {vendor.TenantId}, false)` | Yes | Parameterized interpolation binds the database session to the tenant already selected by the worker. |
| `Finance/PaymentPlanWorker.cs` | 33 | `SELECT set_config('app.tenant_id', {tenantId}, false)` | Yes | Parameterized interpolation; executed once per discovered tenant before the scoped service call. |
| `Finance/AuditSealService.cs` | 98 | `SELECT set_config('app.tenant_id', {tenantId}, false)` | Yes | Parameterized interpolation; tenant-bound seal operation follows. |
| `Finance/AuditSealService.cs` | 177 | `SELECT set_config('app.tenant_id', {entry.TenantId}, false)` | Yes | Parameterized interpolation; tenant-bound ledger signing follows. |
| `Onboarding/SetupImportService.cs` | 29 | `CREATE TABLE IF NOT EXISTS SetupImportRows/SetupAdministrators ...` | Yes | Schema DDL only; it reads no tenant rows. Both tables include a non-null `TenantId`. |
| `Onboarding/SetupImportService.cs` | 45 | `INSERT INTO SetupImportRows (... TenantId ...) VALUES (... {tenantId} ...)` | Yes | Parameterized tenant ID is written on every row. |
| `Onboarding/SetupImportService.cs` | 53 | `INSERT INTO SetupAdministrators (... TenantId ...) ... ON CONFLICT (TenantId)` | Yes | Parameterized tenant ID and a tenant-keyed upsert. |

There are no `FromSqlRaw` or `FromSqlInterpolated` calls.

## Dapper and direct-connection audit

| File | Lines | Statement(s) | Tenant-safe? | Fix/reason |
|---|---:|---|---|---|
| Dapper usage | — | No `Query`, `QueryAsync`, `Execute`, or `ExecuteAsync` Dapper calls | Yes | No Dapper dependency or call exists. |
| `Backup/DatabaseHealthService.cs` | 38, 58 | `SELECT 1`; PostgreSQL replay-lag metadata | Yes | Infrastructure health metadata; no application table access. |
| `Replication/ReplicaManagerService.cs` | 54, 111, 130, 168 | `SELECT 1`; replay lag; `pg_stat_activity`; `pg_promote()` | Yes | Database infrastructure metadata/control only; no tenant table access. |
| `Support/SupportTicketService.cs` | 92 | Insert support ticket | Yes | `tenant_id = @tenant` is inserted explicitly. |
| `Support/SupportTicketService.cs` | 134 | List support tickets | Yes | `WHERE tenant_id = @tenant`. |
| `Support/SupportTicketService.cs` | 163 | Get support ticket | Yes | `WHERE tenant_id = @tenant AND id = @id`. |
| `Support/SupportTicketService.cs` | 196 | Update support ticket feedback | Yes | `WHERE id = @id AND tenant_id = @tenant AND requester_id = @requester`. |
| `Support/SupportTicketService.cs` | 228 | List escalations joined to tickets | Yes | `e.tenant_id = @tenant`; join now also requires `t.tenant_id = e.tenant_id`. |
| `Support/SupportTicketService.cs` | 262 | Resolve escalation selection/update | Yes | Both statements require `tenant_id = @tenant`; join now also requires tenant equality. |
| `Support/SupportTicketService.cs` | 321, 333 | Insert knowledge article; tenant analytics | Yes | Inserts `tenant_id = @tenant`; analytics filters `tenant_id = @tenant`. |
| `Support/SupportTicketService.cs` | 388 | Search knowledge base | Yes | `WHERE tenant_id = @tenant`. |
| `Support/SupportTicketService.cs` | 431 | Create support schema | Yes | DDL only; all data tables have `tenant_id` and tenant-first indexes. |
| `Support/SupportTicketService.cs` | 499, 521 | Insert escalation/knowledge article helpers | Yes | Both insert the parameterized `@tenant`. |
| `DataImport/DataImportService.cs` | 156–159 | External source connection | N/A | Connects to a client-selected external source, not the AegisCore database. Its connection/job metadata is loaded through tenant-filtered EF sets. |
| `Onboarding/OnboardingService.cs` | 59 | Temporary table and `SELECT version()` | Yes | Wizard connectivity probe only; no application table access. |
| `AI/SupportBotQueryService.cs` | 106 | `SELECT university_name FROM tenant_configs WHERE tenant_id = @tenantId` | Yes | Explicit parameterized tenant predicate. |
| `Deployment/DeploymentReadinessChecker.cs` | 64 | `SELECT 1`; current PostgreSQL role flags | Yes | Startup infrastructure checks; no tenant table access. |
| `TenantSessionInterceptor.cs` | 10 | EF connection interceptor | Yes | Sets the current tenant session variable; it does not issue an unscoped tenant-data query. |

## Remaining `IgnoreQueryFilters()` calls

Every retained bypass has an adjacent justification comment. Setup paths run before an authenticated HTTP tenant context exists and include an explicit `TenantId` predicate. Workers use a bypass only to discover minimal tenant-keyed work, then set both the application tenant context and PostgreSQL session tenant before invoking ordinary services.

| File | Line | Justification |
|---|---:|---|
| `Finance/VendorSanctionsService.cs` | 49 | Background discovery selects only due vendor IDs and tenant IDs; each item is tenant-bound before screening. |
| `Finance/PaymentPlanWorker.cs` | 26 | Background discovery enumerates tenant IDs only; each sweep is tenant-bound. |
| `Finance/AuditSealService.cs` | 91 | Background discovery enumerates tenant IDs only; each seal is tenant-bound. |
| `Finance/AuditSealService.cs` | 170 | Cross-tenant worker selects only posted entry identifiers, tenant IDs, and hashes before binding. |
| `Finance/AuditSealService.cs` | 171 | Nested existence check is part of the same bounded ledger-signature discovery statement. |
| `Onboarding/WizardGatewayService.cs` | 83 | Install-mode gateway configuration lookup has explicit `TenantId` and gateway predicates. |
| `Onboarding/WizardGatewayService.cs` | 108 | Install-mode gateway health lookup has explicit `TenantId` and gateway predicates. |
| `Onboarding/WizardGatewayService.cs` | 139 | Install-mode gateway disable query has an explicit `TenantId` predicate. |
| `Onboarding/WizardSessionService.cs` | 19 | Bootstrap session lookup has an explicit `TenantId` predicate. |
| `Onboarding/WizardSessionService.cs` | 31 | Bootstrap session lookup has session ID plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 39 | Bootstrap session save has session ID plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 84 | Bootstrap mode change has session ID plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 119 | Bootstrap gateway state update has session ID plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 135 | Bootstrap completion has session ID plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 146 | Wizard credential lookup has session ID plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 153 | Provisioned-user lookup has email plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 162 | Provisioned-role lookup has name plus explicit `TenantId`. |
| `Onboarding/WizardSessionService.cs` | 169 | Provisioned user-role check has all IDs plus explicit `TenantId`. |
| `Onboarding/WizardProvisioningService.cs` | 37 | Finance configuration lookup has explicit `TenantId`. |
| `Onboarding/WizardProvisioningService.cs` | 53 | Main-campus lookup has explicit `TenantId`. |
| `Onboarding/WizardProvisioningService.cs` | 79 | Department lookup has explicit `TenantId`. |
| `Onboarding/WizardProvisioningService.cs` | 101 | Fiscal-year check has explicit `TenantId`. |
| `Onboarding/WizardProvisioningService.cs` | 125 | Chart-of-account check has explicit `TenantId`. |
| `Onboarding/WizardProvisioningService.cs` | 135 | Degree-program check has explicit `TenantId`. |
| `Onboarding/WizardProvisioningService.cs` | 152 | License lookup has explicit `TenantId`. |
| `Deployment/Seeders/DemoSeeder.cs` | 53 | Demo bootstrap user check runs without HTTP context and has explicit `TenantId`. |
| `Deployment/Seeders/DemoSeeder.cs` | 78 | Demo bootstrap role check runs without HTTP context and has explicit `TenantId`. |
| `Security/Pqc/PqcKeyCeremonyService.cs` | 102 | Background discovery selects expiring key purpose/algorithm and tenant ID; rotation is tenant-bound. |

## Applied rewrites

### Ordinary EF queries

Before (representative of 218 removed bypasses):

```csharp
await db.Entities.IgnoreQueryFilters()
    .Where(x => x.TenantId == tenantId)
    .ToListAsync(ct);
```

After:

```csharp
await db.Entities
    .Where(x => x.TenantId == tenantId)
    .ToListAsync(ct);
```

The global filter is now authoritative, while the explicit predicate remains defense in depth where already present.

### Tenant-safe support joins

Before:

```sql
JOIN support_tickets t ON t.id = e.ticket_id
```

After:

```sql
JOIN support_tickets t ON t.id = e.ticket_id AND t.tenant_id = e.tenant_id
```

### Background worker binding

Before:

```csharp
await service.ProcessAsync(item.TenantId, item.Id, ct);
```

After:

```csharp
tenantContext.HttpContext = new DefaultHttpContext();
tenantContext.HttpContext.Items["TenantId"] = item.TenantId;
await db.Database.ExecuteSqlInterpolatedAsync(
    $"SELECT set_config('app.tenant_id', {item.TenantId:D}, false)", ct);
await service.ProcessAsync(item.TenantId, item.Id, ct);
```

This supplies the same tenant context used by request-path global filters and keeps PostgreSQL RLS/session enforcement aligned.

## Sensitive no-tracking review

No `AsNoTracking()` query involving `Attendance`, `CourseGrade`, `SsoSession`, or `AdmissionApplication` was found in the audited projects.
