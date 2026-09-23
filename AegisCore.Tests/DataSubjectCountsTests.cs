using AegisCore.API.Controllers;
using AegisCore.Application.Compliance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Tabletop finding F-2: a breach assessment has 24 hours to answer "whose data, how many", and
/// that needed a hand-written query. The report answers it with counts only — never a name.
/// </summary>
public sealed class DataSubjectCountsTests
{
    [Theory, DatabaseProviders]
    public async Task The_report_counts_people_by_category_and_names_nobody(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);

        var department = new Department { Id = Guid.NewGuid(), TenantId = tenant, Name = "Computing", Code = "CMP-" + Guid.NewGuid().ToString("N")[..6] };
        var batch = new Batch { Id = Guid.NewGuid(), TenantId = tenant, Name = "2026", StartYear = 2026, EndYear = 2030, DepartmentId = department.Id };
        db.AddRange(department, batch);
        var live = new Student { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "Ayesha", LastName = "Khan", BatchId = batch.Id, EnrollmentStatus = EnrollmentStatus.Active };
        var erased = new Student { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "ERASED_USER_abc", LastName = "", BatchId = batch.Id, EnrollmentStatus = EnrollmentStatus.Active };
        db.AddRange(live, erased);
        db.StudentGuardians.Add(new StudentGuardian { Id = Guid.NewGuid(), TenantId = tenant, StudentId = live.Id, Name = "Parent", Phone = "0300" });
        db.Employees.Add(new Employee { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "Bilal", LastName = "Ahmed", DepartmentId = department.Id, Salary = 100_000m });
        db.StudentCounselings.Add(new StudentCounseling { Id = Guid.NewGuid(), TenantId = tenant, StudentId = live.Id, Notes = "private", SessionDate = DateTime.UtcNow, CounselorUserId = Guid.NewGuid() });
        db.StudentCounselings.Add(new StudentCounseling { Id = Guid.NewGuid(), TenantId = tenant, StudentId = live.Id, Notes = "second session", SessionDate = DateTime.UtcNow, CounselorUserId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var controller = new PrivacyComplianceController(db, Mock.Of<IPrivacyComplianceService>());
        var result = Assert.IsType<OkObjectResult>(await controller.DataSubjectCounts());
        var payload = System.Text.Json.JsonSerializer.Serialize(result.Value);

        using var document = System.Text.Json.JsonDocument.Parse(payload);
        var categories = document.RootElement.GetProperty("categories").EnumerateArray()
            .ToDictionary(c => c.GetProperty("category").GetString()!, c => c.GetProperty("count").GetInt32());

        Assert.Equal(2, categories["Students on the register"]);
        Assert.Equal(1, categories["Students already anonymised by erasure"]);
        Assert.Equal(1, categories["Guardians"]);
        Assert.Equal(1, categories["Employees"]);
        // Two sessions, one student: the report counts people, not rows.
        Assert.Equal(1, categories["Students with counselling records"]);

        // Nothing identifying anybody may appear in a report that gets pasted into an incident record.
        Assert.DoesNotContain("Ayesha", payload);
        Assert.DoesNotContain("Bilal", payload);
        Assert.DoesNotContain("0300", payload);
        Assert.DoesNotContain("private", payload);
    }
}
