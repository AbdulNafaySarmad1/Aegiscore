using AegisCore.Application.Finance;
using AegisCore.Application.Foundation;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Finance;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AegisCore.Tests;

/// <summary>
/// One fee ledger. The lifecycle operations (raise the semester's demand, record a payment, apply a
/// scholarship) write FeeDemands / FinancePayments — the same rows the finance module, the
/// executive dashboard, graduation clearance and the student's own finance line read.
/// </summary>
public sealed class FeeLedgerTests
{
    private sealed record Seed(Guid Tenant, Guid Student, Guid Semester, Guid Structure);

    private static async Task<Seed> SeedAsync(AegisDbContext db, Guid tenant, decimal fee = 120_000m)
    {
        var year = new AcademicYear { Id = Guid.NewGuid(), TenantId = tenant, Name = "2026-27", StartDate = new(2026, 9, 1), EndDate = new(2027, 6, 30) };
        var semester = new Semester { Id = Guid.NewGuid(), TenantId = tenant, AcademicYearId = year.Id, Name = "Fall 2026", Type = "Fall", StartDate = new(2026, 9, 1), EndDate = new(2027, 1, 15) };
        var department = new Department { Id = Guid.NewGuid(), TenantId = tenant, Name = "Computing", Code = "CMP-" + Guid.NewGuid().ToString("N")[..6] };
        var batch = new Batch { Id = Guid.NewGuid(), TenantId = tenant, Name = "2026", StartYear = 2026, EndYear = 2030, DepartmentId = department.Id };
        var program = new DegreeProgram { Id = Guid.NewGuid(), TenantId = tenant, Name = "BS CS", Code = "BSCS-" + Guid.NewGuid().ToString("N")[..4], DepartmentId = department.Id, IsActive = true };
        var structure = new FeeStructure { Id = Guid.NewGuid(), TenantId = tenant, AcademicYearId = year.Id, ProgramId = program.Id, Semester = 1, FeeType = "open_merit", TotalFee = fee };
        var student = new Student { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "Ayesha", LastName = "Khan", BatchId = batch.Id, EnrollmentStatus = EnrollmentStatus.Active };
        db.AddRange(year, semester, department, batch, program, structure, student);
        await db.SaveChangesAsync();
        return new Seed(tenant, student.Id, semester.Id, structure.Id);
    }

    private static FeeService Service(AegisDbContext db)
    {
        var policies = new Mock<IPolicyService>();
        policies.Setup(p => p.GetAsync("finance", "fee_due_days", 30, It.IsAny<CancellationToken>())).ReturnsAsync(30);
        policies.Setup(p => p.GetAsync("finance", "late_fee_grace_days", 3, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        return new FeeService(db, policies.Object, new AccountingService(db));
    }

    [Theory, DatabaseProviders]
    public async Task Demand_Payment_And_Scholarship_Live_On_FeeDemands_And_Post_To_The_Journal(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var seed = await SeedAsync(db, tenant);
        var service = Service(db);

        var demand = await service.CalculateFeeAsync(seed.Student, seed.Semester, seed.Structure);
        Assert.Equal(120_000m, demand.TotalAmount);
        Assert.Equal("OPEN", demand.Status);
        Assert.Equal(seed.Semester, demand.SemesterId);
        Assert.Equal(seed.Structure, demand.FeeStructureId);
        Assert.Single(demand.Installments);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CalculateFeeAsync(seed.Student, seed.Semester, seed.Structure)); // once per semester

        await service.RecordPaymentAsync(demand.Id, new PaymentRequest("jazzcash", 50_000m, "JC-001"));
        await service.ApplyScholarshipAsync(demand.Id, 20_000m, Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordPaymentAsync(demand.Id, new PaymentRequest("jazzcash", 50_000m, "JC-001"))); // same reference twice
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordPaymentAsync(demand.Id, new PaymentRequest("cheque", 60_000m, "CHQ-9"))); // over the balance

        // Read back through a fresh context: the finance module's rows, not a parallel ledger.
        await using var fresh = database.CreateContext(tenant);
        var stored = await fresh.FeeDemands.Include(d => d.Installments).SingleAsync(d => d.Id == demand.Id);
        Assert.Equal(50_000m, stored.PaidAmount);
        Assert.Equal(20_000m, stored.ScholarshipDeduction);
        Assert.Equal(50_000m, stored.OutstandingAmount);
        Assert.Equal("PARTIAL", stored.Status);
        var payment = await fresh.FinancePayments.SingleAsync(p => p.FeeDemandId == demand.Id);
        Assert.Equal("SETTLED", payment.Status);
        Assert.Equal("JAZZCASH", payment.Method);
        Assert.Equal(stored.Installments.Single().Id, payment.FeeInstallmentId);
        Assert.True(await fresh.JournalEntries.AnyAsync(j => j.Description == $"Fee payment {payment.Id:N}" && j.IsPosted && j.TotalDebit == 50_000m));

        await service.RecordPaymentAsync(demand.Id, new PaymentRequest("bank_transfer", 50_000m, "BT-002"));
        Assert.Equal("PAID", (await fresh.FeeDemands.AsNoTracking().SingleAsync(d => d.Id == demand.Id)).Status);
        Assert.Equal(0m, await service.CalculateLateFeeAsync(demand.Id));
    }

    [Fact]
    public async Task Graduation_Clearance_Reads_The_Same_Ledger()
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres: false);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var seed = await SeedAsync(db, tenant, fee: 10_000m);
        var service = Service(db);
        var graduation = new AegisCore.Infrastructure.StudentLifecycle.GraduationService(db);

        var demand = await service.CalculateFeeAsync(seed.Student, seed.Semester, seed.Structure);
        Assert.True(await graduation.HasOutstandingFeesAsync(seed.Student));
        await service.ApplyScholarshipAsync(demand.Id, 4_000m, Guid.NewGuid());
        await service.RecordPaymentAsync(demand.Id, new PaymentRequest("cash", 6_000m, "CASH-1"));
        Assert.False(await graduation.HasOutstandingFeesAsync(seed.Student));
    }

    /// <summary>The migration carries every v1 row into the unified ledger and leaves no v1 table behind.</summary>
    [PilotPostgresFact]
    public async Task Migration_Folds_The_V1_Ledger_Into_FeeDemands()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: true);
        await using var db = database.CreateContext(Guid.NewGuid());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260918065834_AlignColumnTypesWithModel"); // the last migration with the v1 tables
        var tenant = Guid.NewGuid(); var student = Guid.NewGuid(); var semester = Guid.NewGuid(); var structure = Guid.NewGuid();
        var paidDue = Guid.NewGuid(); var openDue = Guid.NewGuid(); var payment = Guid.NewGuid(); var verifier = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(db.Database.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"""
                INSERT INTO "StudentFeeDues" ("Id","TenantId","StudentId","SemesterId","FeeStructureId","TotalDue","PaidAmount","ScholarshipDeduction","OutstandingAmount","Status","CreatedAt")
                VALUES ('{paidDue}','{tenant}','{student}','{semester}','{structure}',115000,115000,0,0,'paid','2026-08-15T00:00:00Z'),
                       ('{openDue}','{tenant}','{Guid.NewGuid()}','{semester}','{structure}',115000,0,15000,100000,'pending','2026-08-15T00:00:00Z');
                INSERT INTO "StudentFeePayments" ("Id","TenantId","FeeDueId","PaymentMethod","Amount","ReferenceNumber","PaymentDate","VerifiedBy","VerificationDate","CreatedAt")
                VALUES ('{payment}','{tenant}','{paidDue}','manual',115000,'FNU-DEMO-PAID-0001','2026-08-20T05:00:00Z','{verifier}','2026-08-20T05:05:00Z','2026-08-20T05:00:00Z');
                """, connection);
            await command.ExecuteNonQueryAsync();
        }

        await database.MigrateAsync(db);

        await using var after = database.CreateContext(tenant);
        var demands = await after.FeeDemands.Include(d => d.Installments).OrderBy(d => d.DemandNumber).ToListAsync();
        Assert.Equal(2, demands.Count);
        var paid = demands.Single(d => d.Id == paidDue);
        Assert.Equal("PAID", paid.Status); Assert.Equal(115_000m, paid.PaidAmount); Assert.Equal(0m, paid.OutstandingAmount);
        Assert.Equal(semester, paid.SemesterId); Assert.Equal(structure, paid.FeeStructureId);
        Assert.StartsWith("FD-V1-", paid.DemandNumber);
        var open = demands.Single(d => d.Id == openDue);
        Assert.Equal("OPEN", open.Status); Assert.Equal(15_000m, open.ScholarshipDeduction); Assert.Equal(100_000m, open.OutstandingAmount);
        Assert.All(demands, d => Assert.Single(d.Installments));
        var carried = await after.FinancePayments.SingleAsync(p => p.Id == payment);
        Assert.Equal(paidDue, carried.FeeDemandId); Assert.Equal("SETTLED", carried.Status); Assert.Equal("MANUAL", carried.Method); Assert.Equal("FNU-DEMO-PAID-0001", carried.ExternalReference);
        Assert.Equal(paid.Installments.Single().Id, carried.FeeInstallmentId);

        var verification = await AegisCore.Infrastructure.Database.SchemaVerifier.VerifyAsync(after);
        Assert.True(verification.IsCurrent, verification.Describe());
        await using (var connection = new NpgsqlConnection(db.Database.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("select count(*) from information_schema.tables where table_name in ('StudentFeeDues','StudentFeePayments')", connection);
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
        }
    }
}
