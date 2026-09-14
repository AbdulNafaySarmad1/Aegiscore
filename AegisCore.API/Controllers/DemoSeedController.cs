using AegisCore.API.Authorization;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Deployment.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController, Authorize, Route("api/admin/demo-seed")]
public sealed class DemoSeedController(AegisDbContext db, IConfiguration configuration, IServiceProvider services) : ControllerBase
{
    [HttpPost, RequirePermission("Settings", "Write")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("DemoSeed:Enabled")) return NotFound();
        var seeder = ActivatorUtilities.CreateInstance<DemoSeeder>(services);
        return Ok(await seeder.SeedAsync(db.CurrentTenantId, ct));
    }
}
