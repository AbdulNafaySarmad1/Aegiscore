using System.Text;
using System.Text.Json;
using AegisCore.Application.Geolocation;
using AegisCore.API.Controllers;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AegisCore.Tests;

public sealed class GeolocationAttendanceTests
{
    private static readonly CampusVertex[] Square =
    [
        new(24.800000m, 67.000000m),
        new(24.800000m, 67.100000m),
        new(24.900000m, 67.100000m),
        new(24.900000m, 67.000000m)
    ];

    [Fact]
    public void PointInsidePolygon_ReturnsTrue() =>
        Assert.True(GeofenceGeometry.IsPointInPolygon(24.850000m, 67.050000m, Square));

    [Fact]
    public void PointOutsidePolygon_ReturnsFalse() =>
        Assert.False(GeofenceGeometry.IsPointInPolygon(25.000000m, 67.050000m, Square));

    [Fact]
    public void PointOnBoundary_IsTreatedAsInside() =>
        Assert.True(GeofenceGeometry.IsPointInPolygon(24.800000m, 67.050000m, Square));

    [Fact]
    public void CampusPolygon_RequiresAtLeastThreeValidVertices()
    {
        var polygon = new CampusPolygon();
        Assert.Throws<ArgumentException>(() => polygon.SetVertices(Square[..2]));
        Assert.Throws<ArgumentOutOfRangeException>(() => polygon.SetVertices(
            [new(91m, 0m), new(0m, 0m), new(1m, 1m)]));
    }

    [Fact]
    public void AttendanceCoordinates_ArePersistedOnlyAsAuthenticatedCiphertext()
    {
        var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());
        using var encryption = new PqcCryptographyService(
            Options.Create(new PqcCryptographyOptions { MasterKeyBase64 = key }),
            NullLogger<PqcCryptographyService>.Instance);
        var attendance = new Attendance
        {
            TenantId = Guid.NewGuid(),
            EnrollmentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            FacultyId = Guid.NewGuid(),
            Date = DateTime.UtcNow.Date,
            Status = AttendanceStatus.Present
        };
        var aad = Encoding.UTF8.GetBytes($"{attendance.TenantId:N}:{attendance.Id:N}:latitude");
        var plaintext = Encoding.UTF8.GetBytes("24.850000");
        var ciphertext = encryption.Encrypt(plaintext, aad);

        attendance.SetEncryptedLocation(24.850000m, 67.050000m, ciphertext, new byte[32]);

        Assert.Equal(24.850000m, attendance.Latitude);
        Assert.NotEqual(plaintext, attendance.LatitudeEncrypted);
        Assert.Equal("24.850000", Encoding.UTF8.GetString(encryption.Decrypt(attendance.LatitudeEncrypted!, aad)));
    }

    [Fact]
    public async Task CheckIn_EndToEnd_EncryptsCoordinates_RedactsRoster_AndAllowsOverride()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Items["TenantId"] = tenantId;
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options;
        await using var db = new AegisDbContext(options, accessor);
        await db.Database.EnsureCreatedAsync();

        var department = new Department { TenantId = tenantId, Name = "Engineering", Code = "ENG" };
        var batch = new Batch
        {
            TenantId = tenantId,
            Name = "2026",
            StartYear = 2026,
            EndYear = 2030,
            DepartmentId = department.Id
        };
        var student = new Student
        {
            TenantId = tenantId,
            FirstName = "Amina",
            LastName = "Khan",
            UniversityId = "STU-GEO-001",
            BatchId = batch.Id
        };
        var faculty = new Faculty
        {
            TenantId = tenantId,
            FirstName = "Sana",
            LastName = "Ali",
            EmployeeCode = "FAC-GEO-001",
            DepartmentId = department.Id
        };
        var course = new Course
        {
            TenantId = tenantId,
            Title = "Geospatial Systems",
            Code = "GEO-101",
            CreditHours = 3,
            DepartmentId = department.Id
        };
        var enrollment = new Enrollment
        {
            TenantId = tenantId,
            StudentId = student.Id,
            CourseId = course.Id
        };
        var assignment = new CourseAssignment
        {
            TenantId = tenantId,
            CourseId = course.Id,
            FacultyId = faculty.Id
        };
        var polygon = new CampusPolygon
        {
            TenantId = tenantId,
            Name = "Main Campus"
        };
        polygon.SetVertices(Square);
        db.AddRange(department, batch, student, faculty, course, enrollment, assignment, polygon);
        await db.SaveChangesAsync();

        var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());
        using var encryption = new PqcCryptographyService(
            Options.Create(new PqcCryptographyOptions { MasterKeyBase64 = key }),
            NullLogger<PqcCryptographyService>.Instance);
        var controller = new AttendanceGeolocationController(db, encryption);

        var response = Assert.IsType<OkObjectResult>(await controller.CheckIn(
            new AttendanceCheckInRequest(student.UniversityId!, course.Id, 24.850000m, 67.050000m),
            CancellationToken.None));
        var responseJson = JsonSerializer.Serialize(response.Value);
        using var responseDocument = JsonDocument.Parse(responseJson);
        Assert.Equal("ON_CAMPUS", responseDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal("Attendance recorded — you're on campus.", responseDocument.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain("latitude", responseJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("longitude", responseJson, StringComparison.OrdinalIgnoreCase);

        db.ChangeTracker.Clear();
        var persisted = await db.Attendances.SingleAsync();
        Assert.Equal(LocationStatus.OnCampus, persisted.LocationStatus);
        Assert.Equal(polygon.Id, persisted.GeofenceId);
        Assert.Null(persisted.Latitude);
        Assert.Null(persisted.Longitude);
        Assert.NotNull(persisted.LatitudeEncrypted);
        Assert.NotNull(persisted.LongitudeEncrypted);
        Assert.False(persisted.LatitudeEncrypted.SequenceEqual(Encoding.UTF8.GetBytes("24.850000")));

        var roster = Assert.IsType<OkObjectResult>(await controller.Roster(course.Id, CancellationToken.None));
        var rosterJson = JsonSerializer.Serialize(roster.Value);
        Assert.Contains("ON_CAMPUS", rosterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("latitude", rosterJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("longitude", rosterJson, StringComparison.OrdinalIgnoreCase);

        var overridden = Assert.IsType<OkObjectResult>(await controller.Override(
            persisted.Id, new AttendanceOverrideRequest("Verified classroom presence"), CancellationToken.None));
        var overrideJson = JsonSerializer.Serialize(overridden.Value);
        Assert.Contains("MANUAL_OVERRIDE", overrideJson, StringComparison.Ordinal);
        Assert.DoesNotContain("latitude", overrideJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("longitude", overrideJson, StringComparison.OrdinalIgnoreCase);
        Assert.True((await db.Attendances.SingleAsync()).IsManualOverride);
    }
}
