namespace AegisCore.Infrastructure.Deployment;

// One implementation owns fictional data; never seed independent dangling IDs.
public sealed class DemoDataSeeder(Seeders.DemoSeeder seeder) : ISeedRunner
{
    public int Order => 6;
    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
        => await seeder.SeedAsync(tenantId, ct);
}
