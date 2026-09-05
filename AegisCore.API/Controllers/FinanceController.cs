using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/finance")]
[AegisCore.API.Authorization.RequirePermission("Finance", "Read")]
public sealed class FinanceController(AegisDbContext db, IFeeService fees, IAccountingService accounting, IBudgetService budget) : ControllerBase
{
    [HttpPost("fee-structures")]
    [AegisCore.API.Authorization.RequirePermission("Finance", "Write")]
    public async Task<IActionResult> CreateFeeStructure([FromBody] FeeStructure structure, CancellationToken ct)
    {
        if (structure.TotalFee < 0 || structure.Semester < 1 || string.IsNullOrWhiteSpace(structure.FeeType)) return BadRequest("Fee structure values are invalid.");
        if (structure.Components.Any(c => c.Amount < 0)) return BadRequest("Fee component amounts cannot be negative.");
        structure.Id = structure.Id == Guid.Empty ? Guid.NewGuid() : structure.Id;
        foreach (var component in structure.Components) { component.Id = component.Id == Guid.Empty ? Guid.NewGuid() : component.Id; component.FeeStructureId = structure.Id; }
        db.FeeStructures.Add(structure);
        await db.SaveChangesAsync(ct);
        return Ok(structure);
    }

    [HttpPost("dues")]
    public async Task<IActionResult> CreateDue([FromBody] FeeDueRequest request, CancellationToken ct)
        => Ok(await fees.CalculateFeeAsync(request.StudentId, request.SemesterId, request.FeeStructureId, ct));

    [HttpPost("dues/{id:guid}/payments")]
    public async Task<IActionResult> Payment(Guid id, [FromBody] PaymentRequest payment, CancellationToken ct)
    {
        await fees.RecordPaymentAsync(id, payment, ct);
        return Ok(new { success = true });
    }

    [HttpPost("dues/{id:guid}/scholarship")]
    [AegisCore.API.Authorization.RequirePermission("Finance", "Approve")]
    public async Task<IActionResult> Scholarship(Guid id, [FromBody] ScholarshipRequest request, CancellationToken ct)
    {
        await fees.ApplyScholarshipAsync(id, request.Amount, request.ScholarshipId, ct);
        return Ok(new { success = true });
    }

    [HttpGet("pifra")]
    [AegisCore.API.Authorization.RequirePermission("Finance", "Export")]
    public async Task<IActionResult> Pifra([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (to < from) return BadRequest("Date range is invalid.");
        return Content(await accounting.ExportPifraAsync(from, to, ct), "application/xml");
    }

    [HttpGet("cost-centers/{id:guid}/variance")]
    [AegisCore.API.Authorization.RequirePermission("Finance", "Read")]
    public async Task<IActionResult> Variance(Guid id, CancellationToken ct) => Ok(await budget.GetVarianceAsync(id, ct));

    [HttpPost("procurement/requisitions")]
    [AegisCore.API.Authorization.RequirePermission("Finance", "Write")]
    public async Task<IActionResult> Requisition([FromBody] ProcurementRequisition requisition, CancellationToken ct)
    {
        if (requisition.RequestingDepartmentId == Guid.Empty || requisition.LineItems.Count == 0) return BadRequest("Department and at least one line item are required.");
        if (requisition.LineItems.Any(i => i.Quantity <= 0 || i.UnitPrice < 0 || string.IsNullOrWhiteSpace(i.Description))) return BadRequest("Requisition line items are invalid.");
        requisition.Id = requisition.Id == Guid.Empty ? Guid.NewGuid() : requisition.Id;
        requisition.RequestedBy = UserId();
        requisition.RequisitionNumber = string.IsNullOrWhiteSpace(requisition.RequisitionNumber) ? $"REQ-{DateTime.UtcNow:yyyyMMdd}-{requisition.Id:N}"[..21] : requisition.RequisitionNumber;
        foreach (var line in requisition.LineItems) { line.Id = line.Id == Guid.Empty ? Guid.NewGuid() : line.Id; line.RequisitionId = requisition.Id; line.TotalPrice = line.Quantity * line.UnitPrice; }
        requisition.TotalAmount = requisition.LineItems.Sum(i => i.TotalPrice);
        requisition.Status = "submitted";
        db.ProcurementRequisitions.Add(requisition);
        await db.SaveChangesAsync(ct);
        return Ok(requisition);
    }

    [HttpGet("vendors")]
    [AegisCore.API.Authorization.RequirePermission("Finance", "Read")]
    public async Task<IActionResult> Vendors(CancellationToken ct) => Ok(await db.Vendors.AsNoTracking().Where(v => v.Status == "active").ToListAsync(ct));

    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}

public sealed record FeeDueRequest(Guid StudentId, Guid SemesterId, Guid FeeStructureId);
public sealed record ScholarshipRequest(decimal Amount, Guid ScholarshipId);
