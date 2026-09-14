using System.Security.Cryptography;
using System.Text.Json.Nodes;
using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardProvisioningService
{
    private readonly AegisDbContext _db;
    private readonly IPqcSignatureService _pqc;

    public WizardProvisioningService(AegisDbContext db, IPqcSignatureService pqc, IAccountingStandardsEngine accounting)
    {
        _db = db;
        _pqc = pqc;
        _ = accounting;
    }

    public async Task ProvisionAsync(Guid tenantId, string stepData, CancellationToken ct)
    {
        var root = JsonNode.Parse(stepData)?.AsObject()
            ?? throw new InvalidOperationException("Wizard data is invalid.");
        var university = root["step1"]?.AsObject()
            ?? throw new InvalidOperationException("University identity is missing.");
        var permissionStep = root["step4"]?.AsObject();
        var licenseStep = root["step0"]?.AsObject();
        var licenseMode = licenseStep?["mode"]?.GetValue<string>()?.Trim().ToLowerInvariant() ?? "demo";
        if (licenseMode is not ("demo" or "paid"))
            throw new InvalidOperationException("License mode is invalid.");

        var name = Required(university, "name");
        var shortName = Required(university, "shortName").ToUpperInvariant();
        var tenant = await _db.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Id == tenantId, ct);
        if (tenant is null)
        {
            tenant = new Tenant { Id = tenantId, TenantId = tenantId, CreatedAt = DateTimeOffset.UtcNow };
            _db.Tenants.Add(tenant);
        }
        tenant.Name = name;
        tenant.ShortName = shortName;
        tenant.LicenseMode = licenseMode;
        tenant.IsActive = true;
        tenant.ActivatedAt ??= DateTimeOffset.UtcNow;

        var campus = await _db.Campuses.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.IsMain, ct);
        if (campus is null)
        {
            campus = new AegisCore.Domain.Entities.Campus { Id = Guid.NewGuid(), TenantId = tenantId, IsMain = true };
            _db.Campuses.Add(campus);
        }
        campus.Name = name;
        campus.CampusCode = shortName;
        campus.Address = Text(university, "street");
        campus.City = Text(university, "city");
        campus.Country = Text(university, "country");
        campus.Timezone = Required(university, "timezone");
        campus.Language = Text(university, "language", "English");

        await PersistPermissionTreeAsync(tenantId, permissionStep, ct);

        var license = await _db.Licenses.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.TenantId == tenantId, ct);
        if (license is null)
        {
            var demo = licenseMode == "demo";
            license = new License
            {
                Id = Guid.NewGuid(), TenantId = tenantId, CustomerName = name,
                CustomerEmail = Text(university, "contactEmail"),
                LicenseKey = demo ? Convert.ToHexString(RandomNumberGenerator.GetBytes(24)) : Required(licenseStep!, "licenseKey"),
                Edition = demo ? LicenseEdition.Community : LicenseEdition.Professional,
                Status = demo ? LicenseStatus.Trial : LicenseStatus.Active,
                MaxUsers = demo ? 500 : int.MaxValue,
                MaxCampuses = demo ? 1 : int.MaxValue,
                FeatureEntitlementsJson = new JsonObject { ["mode"] = licenseMode }.ToJsonString(),
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = demo ? DateTime.UtcNow.AddDays(30) : null
            };
            _db.Licenses.Add(license);
        }

        // A local/demo wizard issues a scoped trial license as part of go-live.
        // Without the runtime state, the license middleware correctly treats the
        // freshly installed tenant as pending activation and blocks real writes.
        if (licenseMode == "demo")
        {
            var licenseState = await _db.LicenseStates.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId, ct);
            if (licenseState is null)
            {
                _db.LicenseStates.Add(new LicenseState
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, LicenseKey = license.LicenseKey,
                    UniversityId = tenantId, UniversityName = name, Status = "active",
                    LicenseJson = new JsonObject { ["mode"] = "demo", ["edition"] = "community" }.ToJsonString(),
                    ActivatedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                licenseState.Status = "active";
                licenseState.LicenseKey = license.LicenseKey;
                licenseState.UniversityName = name;
                licenseState.ActivatedAt ??= DateTimeOffset.UtcNow;
                licenseState.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        await _pqc.SignAsync(tenantId, "voucher-approval", ReadOnlyMemory<byte>.Empty, archival: false, ct);
    }

    private async Task PersistPermissionTreeAsync(Guid tenantId, JsonObject? step, CancellationToken ct)
    {
        foreach (var roleNode in step?["roles"]?.AsArray() ?? [])
        {
            var roleObject = roleNode?.AsObject();
            var roleName = roleObject?["name"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(roleName) || roleName.Length > 64) continue;
            var role = await _db.Roles.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Name == roleName, ct);
            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, Name = roleName,
                    Description = "Created by the setup wizard.", IsSystem = false
                };
                _db.Roles.Add(role);
            }

            foreach (var permissionNode in roleObject?["permissions"]?.AsArray() ?? [])
            {
                var value = permissionNode?.GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(value)) continue;
                var parts = value.Split('.', 2, StringSplitOptions.TrimEntries);
                var resource = parts[0];
                var action = parts.Length == 2 ? parts[1] : "Read";
                var permission = await _db.Permissions.IgnoreQueryFilters().SingleOrDefaultAsync(
                    item => item.TenantId == tenantId && item.Resource == resource && item.Action == action, ct);
                if (permission is null)
                {
                    permission = new Permission
                    {
                        Id = Guid.NewGuid(), TenantId = tenantId, Name = $"{resource}.{action}",
                        Resource = resource, Action = action, Module = resource,
                        Description = "Created by the setup wizard."
                    };
                    _db.Permissions.Add(permission);
                }
                if (!await _db.RolePermissions.IgnoreQueryFilters().AnyAsync(
                        item => item.TenantId == tenantId && item.RoleId == role.Id && item.PermissionId == permission.Id, ct))
                    _db.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(), TenantId = tenantId, RoleId = role.Id, PermissionId = permission.Id
                    });
            }
        }
    }

    private static string Required(JsonObject value, string fieldName)
        => value[fieldName]?.GetValue<string>()?.Trim() is { Length: > 0 } result
            ? result
            : throw new InvalidOperationException($"Required wizard field '{fieldName}' is missing.");

    private static string Text(JsonObject value, string fieldName, string fallback = "")
        => value[fieldName]?.GetValue<string>()?.Trim() is { Length: > 0 } result ? result : fallback;
}
