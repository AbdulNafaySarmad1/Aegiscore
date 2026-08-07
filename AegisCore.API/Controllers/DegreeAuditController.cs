using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.DegreeAudit;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/degree-audit")]
[Authorize(Roles = "Admin,Registrar,SuperAdmin")]
public class DegreeAuditController : ControllerBase
{
    private readonly IDegreeAuditService _auditService;
    private readonly IDegreeProgramRepository _programRepo;
    private readonly IDegreeRequirementRepository _requirementRepo;
    private readonly IDegreeAuditRepository _auditRepo;
    private readonly ICreditTransferRepository _creditRepo;
    private readonly ILogger<DegreeAuditController> _logger;

    public DegreeAuditController(
        IDegreeAuditService auditService,
        IDegreeProgramRepository programRepo,
        IDegreeRequirementRepository requirementRepo,
        IDegreeAuditRepository auditRepo,
        ICreditTransferRepository creditRepo,
        ILogger<DegreeAuditController> logger)
    {
        _auditService = auditService;
        _programRepo = programRepo;
        _requirementRepo = requirementRepo;
        _auditRepo = auditRepo;
        _creditRepo = creditRepo;
        _logger = logger;
    }

    [HttpPost("programs")]
    public async Task<IActionResult> CreateProgram([FromBody] CreateProgramRequest request, CancellationToken cancellationToken)
    {
        var program = new DegreeProgram
        {
            Id = Guid.NewGuid(), Name = request.Name, Code = request.Code,
            DegreeType = request.DegreeType, DurationYears = request.DurationYears,
            MinCreditHours = request.MinCreditHours, MinCGPA = request.MinCGPA,
            CurriculumVersion = Enum.TryParse<CurriculumVersion>(request.CurriculumVersion, true, out var cv) ? cv : CurriculumVersion.V2025,
            DepartmentId = request.DepartmentId
        };

        await _programRepo.AddAsync(program, cancellationToken);
        return Ok(new DegreeProgramDto { Id = program.Id, Name = program.Name, Code = program.Code });
    }

    [HttpGet("programs")]
    public async Task<IActionResult> ListPrograms([FromQuery] Guid? departmentId, CancellationToken cancellationToken)
    {
        IReadOnlyList<DegreeProgram> programs;
        if (departmentId.HasValue)
            programs = await _programRepo.GetByDepartmentAsync(departmentId.Value, cancellationToken);
        else
            programs = await _programRepo.GetAllAsync(cancellationToken);

        var response = programs.Select(p => new DegreeProgramDto
        {
            Id = p.Id, Name = p.Name, Code = p.Code, DegreeType = p.DegreeType,
            DurationYears = p.DurationYears, MinCreditHours = p.MinCreditHours,
            MinCGPA = p.MinCGPA, CurriculumVersion = p.CurriculumVersion.ToString(),
            RequirementCount = p.Requirements?.Count ?? 0
        });

        return Ok(new { total = response.Count(), programs = response });
    }

    [HttpPost("requirements")]
    public async Task<IActionResult> AddRequirement([FromBody] AddRequirementRequest request, CancellationToken cancellationToken)
    {
        var requirement = new DegreeRequirement
        {
            Id = Guid.NewGuid(), ProgramId = request.ProgramId, Name = request.Name,
            Category = Enum.TryParse<DegreeRequirementCategory>(request.Category, true, out var cat) ? cat : DegreeRequirementCategory.Core,
            RequirementType = Enum.TryParse<RequirementType>(request.RequirementType, true, out var rt) ? rt : RequirementType.CourseCompletion,
            CourseId = request.CourseId, MinCredits = request.MinCredits,
            CourseCount = request.CourseCount, SortOrder = request.SortOrder,
            IsRequired = request.IsRequired,
            AllowedCourseIdsJson = System.Text.Json.JsonSerializer.Serialize(request.AllowedCourseIds ?? new List<Guid>())
        };

        await _requirementRepo.AddAsync(requirement, cancellationToken);
        return Ok(new RequirementDto { Id = requirement.Id, Name = requirement.Name, Category = requirement.Category.ToString() });
    }

    [HttpPost("run/{studentId:guid}")]
    public async Task<IActionResult> RunAudit(Guid studentId, [FromQuery] Guid programId, CancellationToken cancellationToken)
    {
        var audits = await _auditService.RunAuditAsync(studentId, programId, cancellationToken);
        var progress = await _auditService.GetStudentProgressAsync(studentId, cancellationToken);
        return Ok(progress);
    }

    [HttpGet("progress/{studentId:guid}")]
    public async Task<IActionResult> GetProgress(Guid studentId, CancellationToken cancellationToken)
    {
        var progress = await _auditService.GetStudentProgressAsync(studentId, cancellationToken);
        return Ok(progress);
    }

    [HttpPost("what-if/{studentId:guid}")]
    public async Task<IActionResult> RunWhatIf(Guid studentId, [FromBody] WhatIfRequest request, CancellationToken cancellationToken)
    {
        var result = await _auditService.RunWhatIfAsync(studentId, request.ProposedCourseIds, cancellationToken);
        return Ok(result);
    }

    [HttpPost("waive/{auditId:guid}")]
    public async Task<IActionResult> WaiveRequirement(Guid auditId, [FromQuery] string reason, CancellationToken cancellationToken)
    {
        await _auditService.WaiveRequirementAsync(auditId, reason, cancellationToken);
        return Ok(new { message = "Requirement waived." });
    }

    [HttpPost("credit-transfers")]
    public async Task<IActionResult> SubmitCreditTransfer([FromBody] CreditTransferDto request, CancellationToken cancellationToken)
    {
        var transfer = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.Empty,
            ExternalCourseName = request.ExternalCourseName,
            ExternalInstitution = request.ExternalInstitution,
            CreditHours = request.CreditHours,
            InternalCourseCode = request.InternalCourseCode
        };

        await _auditService.SubmitCreditTransferAsync(transfer, cancellationToken);
        return Ok(new { message = "Credit transfer submitted." });
    }

    [HttpPost("credit-transfers/{id:guid}/approve")]
    public async Task<IActionResult> ApproveCreditTransfer(Guid id, [FromQuery] string approvedBy, CancellationToken cancellationToken)
    {
        await _auditService.ApproveCreditTransferAsync(id, approvedBy, cancellationToken);
        return Ok(new { message = "Credit transfer approved." });
    }

    [HttpGet("credit-transfers")]
    public async Task<IActionResult> ListCreditTransfers([FromQuery] Guid? studentId, CancellationToken cancellationToken)
    {
        IReadOnlyList<CreditTransfer> transfers;
        if (studentId.HasValue)
            transfers = await _creditRepo.GetByStudentAsync(studentId.Value, cancellationToken);
        else
            transfers = await _creditRepo.GetPendingAsync(cancellationToken);

        var response = transfers.Select(t => new CreditTransferDto
        {
            Id = t.Id, ExternalCourseName = t.ExternalCourseName,
            ExternalInstitution = t.ExternalInstitution, CreditHours = t.CreditHours,
            InternalCourseCode = t.InternalCourseCode, IsApproved = t.IsApproved, CreatedAt = t.CreatedAt
        });

        return Ok(new { total = response.Count(), transfers = response });
    }
}
