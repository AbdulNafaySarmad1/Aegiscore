using AegisCore.Application.Foundation;
using AegisCore.Application.Hr;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/hr")]
[AegisCore.API.Authorization.RequirePermission("Staff", "Read")]
public sealed class HrController(AegisDbContext db, IPayrollService payroll, IAcademicCalendarService calendar) : ControllerBase
{
    [HttpGet("employees")]
    [AegisCore.API.Authorization.RequirePermission("Staff", "Read")]
    [AegisCore.API.Authorization.RequireAccessAudit("Employee")]
    public async Task<IActionResult> Employees(CancellationToken ct)
        => Ok(await db.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).ToListAsync(ct));

    [HttpPost("employees")]
    [AegisCore.API.Authorization.RequirePermission("Staff", "Write")]
    public async Task<IActionResult> CreateEmployee([FromBody] Employee employee, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(employee.FirstName) || string.IsNullOrWhiteSpace(employee.LastName)) return BadRequest("Employee name is required.");
        if (employee.Salary is < 0) return BadRequest("Salary cannot be negative.");
        employee.Id = employee.Id == Guid.Empty ? Guid.NewGuid() : employee.Id;
        employee.Status = employee.Status == 0 ? EmployeeStatus.Active : employee.Status;
        if (employee.DateOfJoining is null) employee.DateOfJoining = DateTime.UtcNow;
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    [HttpGet("employees/{id:guid}")]
    [AegisCore.API.Authorization.RequireAccessAudit("Employee", "id")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpGet("jobs")]
    [AllowAnonymous]
    public async Task<IActionResult> Jobs(CancellationToken ct)
        => Ok(await db.JobPostings.AsNoTracking().Where(j => j.Status == JobPostingStatus.Active).OrderByDescending(j => j.PostedAt).ToListAsync(ct));

    [HttpPost("jobs")]
    [AegisCore.API.Authorization.RequirePermission("Staff", "Write")]
    public async Task<IActionResult> CreateJob([FromBody] JobPosting posting, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(posting.Title)) return BadRequest("Job title is required.");
        posting.Id = posting.Id == Guid.Empty ? Guid.NewGuid() : posting.Id;
        posting.Status = posting.Status == 0 ? JobPostingStatus.Draft : posting.Status;
        db.JobPostings.Add(posting);
        await db.SaveChangesAsync(ct);
        return Ok(posting);
    }

    [HttpPost("leaves")]
    public async Task<IActionResult> ApplyLeave([FromBody] LeaveRequest request, CancellationToken ct)
    {
        if (request.EmployeeId == Guid.Empty || request.To < request.From) return BadRequest("A valid employee and date range are required.");
        if (!await db.Employees.AnyAsync(e => e.Id == request.EmployeeId && e.Status == EmployeeStatus.Active, ct)) return NotFound("Employee was not found.");
        var overlap = await db.LeaveApplications.AnyAsync(l => l.EmployeeId == request.EmployeeId && l.Status != LeaveStatus.Rejected && l.Status != LeaveStatus.Cancelled && l.StartDate <= request.To.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) && l.EndDate >= request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), ct);
        if (overlap) return Conflict("Leave dates overlap an existing application.");
        var days = await calendar.GetWorkingDaysAsync(request.From, request.To, ct);
        if (days == 0) return BadRequest("Leave range contains no working days.");
        var leave = new LeaveApplication { Id = Guid.NewGuid(), EmployeeId = request.EmployeeId, LeaveType = request.LeaveType, StartDate = request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), EndDate = request.To.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Reason = request.Reason?.Trim(), Status = LeaveStatus.Pending };
        db.LeaveApplications.Add(leave);
        await db.SaveChangesAsync(ct);
        return Ok(new { leave, workingDays = days });
    }

    [HttpPost("leaves/{id:guid}/approve")]
    [AegisCore.API.Authorization.RequirePermission("Staff", "Approve")]
    public async Task<IActionResult> ApproveLeave(Guid id, CancellationToken ct)
    {
        var leave = await db.LeaveApplications.FindAsync([id], ct);
        if (leave is null) return NotFound();
        if (leave.Status != LeaveStatus.Pending) return Conflict("Only pending leave can be approved.");
        leave.Status = LeaveStatus.ApprovedByHod;
        leave.ApprovedByUserId = UserId();
        var from = DateOnly.FromDateTime(leave.StartDate);
        var to = DateOnly.FromDateTime(leave.EndDate);
        for (var day = from; day <= to; day = day.AddDays(1))
            if (day.DayOfWeek is not (System.DayOfWeek.Saturday or System.DayOfWeek.Sunday))
                db.AttendanceRecords.Add(new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = leave.EmployeeId, CheckIn = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Method = AttendanceMethod.Manual, Status = AttendanceStatus.Excused });
        await db.SaveChangesAsync(ct);
        return Ok(leave);
    }

    [HttpPost("leaves/{id:guid}/reject")]
    [AegisCore.API.Authorization.RequirePermission("Staff", "Approve")]
    public async Task<IActionResult> RejectLeave(Guid id, CancellationToken ct)
    {
        var leave = await db.LeaveApplications.FindAsync([id], ct);
        if (leave is null) return NotFound();
        if (leave.Status != LeaveStatus.Pending) return Conflict("Only pending leave can be rejected.");
        leave.Status = LeaveStatus.Rejected;
        leave.ApprovedByUserId = UserId();
        await db.SaveChangesAsync(ct);
        return Ok(leave);
    }

    [HttpPost("payroll/{year:int}/{month:int}/run")]
    [AegisCore.API.Authorization.RequirePermission("Payroll", "Write")]
    public async Task<IActionResult> RunPayroll(int year, int month, CancellationToken ct)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100) return BadRequest("Payroll month is invalid.");
        return Ok(await payroll.RunPayrollAsync(new DateOnly(year, month, 1), UserId(), ct));
    }

    [HttpGet("payroll/{id:guid}/bank-export")]
    [AegisCore.API.Authorization.RequirePermission("Payroll", "Export")]
    [AegisCore.API.Authorization.RequireAccessAudit("Payroll", "id")]
    public async Task<IActionResult> BankExport(Guid id, CancellationToken ct)
        => File(System.Text.Encoding.UTF8.GetBytes(await payroll.ExportBankTransferCsvAsync(id, ct)), "text/csv", $"payroll-{id:N}.csv");

    [HttpGet("employees/{id:guid}/gratuity")]
    [AegisCore.API.Authorization.RequirePermission("Payroll", "Read")]
    [AegisCore.API.Authorization.RequireAccessAudit("Employee", "id")]
    public async Task<IActionResult> Gratuity(Guid id, [FromQuery] DateOnly resignationDate, CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync([id], ct);
        return employee is null ? NotFound() : Ok(new { amount = payroll.CalculateGratuity(employee, resignationDate == default ? DateOnly.FromDateTime(DateTime.UtcNow) : resignationDate) });
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}

public sealed record LeaveRequest(Guid EmployeeId, LeaveType LeaveType, DateOnly From, DateOnly To, string? Reason);
