using System.Security.Claims;
using AegisCore.API.Controllers;
using AegisCore.Application.Admissions;
using AegisCore.Application.Foundation;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AegisCore.Tests;

public sealed class AdmissionsOwnershipTests
{
    [Fact]
    public async Task Submit_DerivesApplicantOwnershipFromAuthenticatedTenantUser()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = tenantId;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"));
        var accessor = new FixedHttpContextAccessor { HttpContext = context };
        await using var db = new AegisDbContext(
            new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options,
            accessor);
        await db.Database.EnsureCreatedAsync();
        var department = new Department { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Admissions" };
        var program = new DegreeProgram
        {
            Id = Guid.NewGuid(), TenantId = tenantId, DepartmentId = department.Id, Name = "BS Secure Systems", Code = "BSSS"
        };
        db.AddRange(
            new Tenant { Id = tenantId, Name = "Ownership Test", ShortName = "OT" },
            new User { Id = userId, TenantId = tenantId, Username = "applicant", Email = "applicant@example.edu" },
            department,
            program);
        await db.SaveChangesAsync();
        var controller = new S2AdmissionsController(
            db,
            Mock.Of<IStorageProvider>(),
            Mock.Of<IMeritCalculationService>(),
            Mock.Of<ISeatAllocationService>(),
            Mock.Of<IIBCCEquivalenceService>(),
            new ConfigurationBuilder().Build())
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        var request = new AdmissionApplicationRequest(
            "Applicant Name", "applicant@example.edu", "+92-300-0000000", "42101-0000000-1",
            new DateOnly(2004, 1, 1), null, program.Id, "open_merit", 85m, 84m, 80, false);

        var result = await controller.Submit(request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        var application = await db.AdmissionApplications.SingleAsync();
        Assert.Equal(userId, application.ApplicantUserId);
        Assert.Equal("submitted", application.ApplicationStatus);
        Assert.NotEqual(Guid.Empty, application.Id);
    }

    private sealed class FixedHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
