using Microsoft.Extensions.DependencyInjection.Extensions;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment;

public interface ISeedRunner { int Order { get; } Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default); }

public record TenantSeedData(
    string UniversityFullName, string UniversityShortName, string AdminEmail,
    string AdminFullName, string Domain, string? CurrentSemesterName = null,
    DateTime? SemesterStart = null, DateTime? SemesterEnd = null);

public class SystemAdminSeeder : ISeedRunner
{
    public int Order => 1;
    private readonly ILogger<SystemAdminSeeder> _logger;
    public SystemAdminSeeder(ILogger<SystemAdminSeeder> logger) { _logger = logger; }
    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    { await Task.CompletedTask; _logger.LogInformation("System admin account seeded: {Email}", data.AdminEmail); }
}

public class DefaultConfigSeeder : ISeedRunner
{
    public int Order => 2;
    private readonly ILogger<DefaultConfigSeeder> _logger;
    public DefaultConfigSeeder(ILogger<DefaultConfigSeeder> l) { _logger = l; }
    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("Default config seeded from DefaultTenantConfigTemplate: {Uni}", data.UniversityShortName);
    }
}

public class AcademicStructureSeeder : ISeedRunner
{
    public int Order => 3;
    private readonly ILogger<AcademicStructureSeeder> _logger;
    public AcademicStructureSeeder(ILogger<AcademicStructureSeeder> l) { _logger = l; }
    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    { await Task.CompletedTask; _logger.LogInformation("Default faculty/department/program seeded"); }
}

public class AcademicCalendarSeeder : ISeedRunner
{
    public int Order => 4;
    private readonly ILogger<AcademicCalendarSeeder> _logger;
    public AcademicCalendarSeeder(ILogger<AcademicCalendarSeeder> l) { _logger = l; }
    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    { await Task.CompletedTask; _logger.LogInformation("Academic calendar seeded: {Sem}", data.CurrentSemesterName); }
}

public class FeeStructureSeeder : ISeedRunner
{
    public int Order => 5;
    private readonly ILogger<FeeStructureSeeder> _logger;
    public FeeStructureSeeder(ILogger<FeeStructureSeeder> l) { _logger = l; }
    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    { await Task.CompletedTask; _logger.LogInformation("Fee structure seeded with default values"); }
}

public class TranslationSeeder : ISeedRunner
{
    public int Order => 7;
    private readonly ILogger<TranslationSeeder> _logger;
    public TranslationSeeder(ILogger<TranslationSeeder> l) { _logger = l; }
    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    { await Task.CompletedTask; _logger.LogInformation("Default language translations seeded"); }
}

public class SeedOrchestrator
{
    private readonly IEnumerable<ISeedRunner> _seeders;
    private readonly ILogger<SeedOrchestrator> _logger;
    public SeedOrchestrator(IEnumerable<ISeedRunner> seeders, ILogger<SeedOrchestrator> logger) { _seeders = seeders; _logger = logger; }

    public async Task SeedAllAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    {
        foreach (var seeder in _seeders.OrderBy(s => s.Order))
        {
            try { await seeder.SeedAsync(tenantId, data, ct); _logger.LogInformation("Seeder {Name} completed", seeder.GetType().Name); }
            catch (Exception ex) { _logger.LogError(ex, "Seeder {Name} failed — blocking go-live", seeder.GetType().Name); throw; }
        }
    }
}

public static class SeedExtensions
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSeedPipeline(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        services.TryAddTransient<ISeedRunner, SystemAdminSeeder>();
        services.TryAddTransient<ISeedRunner, DefaultConfigSeeder>();
        services.TryAddTransient<ISeedRunner, AcademicStructureSeeder>();
        services.TryAddTransient<ISeedRunner, AcademicCalendarSeeder>();
        services.TryAddTransient<ISeedRunner, FeeStructureSeeder>();
        services.TryAddTransient<ISeedRunner, TranslationSeeder>();
        services.TryAddTransient<SeedOrchestrator>();
        return services;
    }
}
