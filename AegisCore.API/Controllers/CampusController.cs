using AegisCore.Application.Campus;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/campus")]
[Authorize]
public class CampusController : ControllerBase
{
    private readonly ICampusService _campusService;
    private readonly ICampusRepository _campusRepo;
    private readonly IConsolidatedReportingService _reportingService;
    private readonly ILogger<CampusController> _logger;

    public CampusController(
        ICampusService campusService,
        ICampusRepository campusRepo,
        IConsolidatedReportingService reportingService,
        ILogger<CampusController> logger)
    {
        _campusService = campusService;
        _campusRepo = campusRepo;
        _reportingService = reportingService;
        _logger = logger;
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    [HttpPost]
    public async Task<IActionResult> RegisterCampus([FromBody] CreateCampusRequest request, CancellationToken cancellationToken)
    {
        var campus = await _campusService.RegisterCampusAsync(request, cancellationToken);
        return Ok(campus);
    }

    [HttpGet]
    public async Task<IActionResult> ListCampuses(CancellationToken cancellationToken)
    {
        var campuses = await _campusService.GetCampusesAsync(cancellationToken);
        return Ok(new { total = campuses.Count, campuses });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCampus(Guid id, CancellationToken cancellationToken)
    {
        var campus = await _campusRepo.GetByIdAsync(id, cancellationToken);
        if (campus == null) return NotFound();

        return Ok(new CampusDto
        {
            Id = campus.Id, Name = campus.Name, CampusCode = campus.CampusCode,
            Address = campus.Address, City = campus.City, Country = campus.Country,
            Phone = campus.Phone, Email = campus.Email,
            Status = campus.Status.ToString(), CalendarType = campus.CalendarType.ToString(),
            GradingScheme = campus.GradingScheme.ToString(), IsMain = campus.IsMain,
            BuildingCount = campus.Buildings?.Count ?? 0, CreatedAt = campus.CreatedAt
        });
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCampus(Guid id, [FromBody] UpdateCampusRequest request, CancellationToken cancellationToken)
    {
        var campus = await _campusService.UpdateCampusAsync(id, request, cancellationToken);
        return Ok(campus);
    }

    [HttpPost("{id:guid}/config")]
    public async Task<IActionResult> GetCampusConfig(Guid id, CancellationToken cancellationToken)
    {
        var config = await _campusService.GetCampusConfigAsync(id, cancellationToken);
        return Ok(new
        {
            config.CampusId,
            AcademicCalendar = System.Text.Json.JsonSerializer.Deserialize<object>(config.AcademicCalendarJson),
            GradingPolicy = System.Text.Json.JsonSerializer.Deserialize<object>(config.GradingPolicyJson),
            AdmissionRules = System.Text.Json.JsonSerializer.Deserialize<object>(config.AdmissionRulesJson),
            FeeStructure = System.Text.Json.JsonSerializer.Deserialize<object>(config.FeeStructureJson),
            Policies = System.Text.Json.JsonSerializer.Deserialize<object>(config.PoliciesJson)
        });
    }

    [HttpGet("report/university")]
    public async Task<IActionResult> GetUniversityReport(CancellationToken cancellationToken)
    {
        var report = await _reportingService.GetUniversityReportAsync(cancellationToken);
        return Ok(report);
    }

    [HttpGet("report/comparison")]
    public async Task<IActionResult> GetCampusComparison(CancellationToken cancellationToken)
    {
        var comparison = await _reportingService.GetCampusComparisonAsync(cancellationToken);
        return Ok(comparison);
    }
}

[ApiController]
[Route("api/campus/transfer")]
[Authorize]
public class TransferController : ControllerBase
{
    private readonly ICampusService _campusService;
    private readonly IRepository<StudentTransfer> _transferRepo;
    private readonly ILogger<TransferController> _logger;

    public TransferController(
        ICampusService campusService,
        IRepository<StudentTransfer> transferRepo,
        ILogger<TransferController> logger)
    {
        _campusService = campusService;
        _transferRepo = transferRepo;
        _logger = logger;
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    [HttpPost]
    public async Task<IActionResult> InitiateTransfer([FromBody] InitiateTransferRequest request, CancellationToken cancellationToken)
    {
        var sourceCampus = request.SourceCampusCode;
        var destCampus = request.DestCampusCode;

        await _campusService.InitiateStudentTransferAsync(request.StudentId, Guid.Empty, Guid.Empty, request.Reason, cancellationToken);
        return Ok(new { message = "Transfer initiated." });
    }

    [HttpGet]
    public async Task<IActionResult> ListTransfers(CancellationToken cancellationToken)
    {
        var transfers = await _transferRepo.GetAllAsync(cancellationToken);
        var response = transfers.Select(t => new TransferRequestDto
        {
            Id = t.Id, StudentId = t.StudentId,
            Status = t.Status.ToString(), Reason = t.Reason, CreatedAt = t.CreatedAt
        });

        return Ok(new { total = response.Count(), transfers = response });
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Approve")]
    [HttpPost("{id:guid}/approve-source")]
    public async Task<IActionResult> ApproveSource(Guid id, [FromBody] ApproveTransferRequest request, CancellationToken cancellationToken)
    {
        await _campusService.ApproveTransferAsync(id, request.ApproverName, true, cancellationToken);
        return Ok(new { message = "Source campus approved." });
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Approve")]
    [HttpPost("{id:guid}/approve-dest")]
    public async Task<IActionResult> ApproveDest(Guid id, [FromBody] ApproveTransferRequest request, CancellationToken cancellationToken)
    {
        await _campusService.ApproveTransferAsync(id, request.ApproverName, false, cancellationToken);
        return Ok(new { message = "Destination campus approved. Transfer completed." });
    }
}
