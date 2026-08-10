using System.Security.Claims;
using AegisCore.Application.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/analytics/v1")]
[Authorize]
public sealed class AnalyticsController(IAnalyticsIntelligenceService analytics) : ControllerBase
{
    private static readonly string[] TenantWideRoles = ["Admin", "SuperAdmin", "Registrar", "ExamController", "VC"];
    private static readonly string[] AdmissionsRoles = ["Admin", "SuperAdmin", "Registrar", "AdmissionsHead"];

    [Authorize(Roles = "Student")]
    [HttpGet("students/me/discovery")]
    public async Task<ActionResult<StudentAnalyticsDiscovery>> GetMyDiscovery([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (StudentId() is not { } studentId) return Forbid();
        return Ok(await analytics.GetStudentDiscoveryAsync(studentId, limit, ct));
    }

    [HttpGet("admissions/me/discovery")]
    public async Task<ActionResult<IReadOnlyList<AdmissionExplanationReference>>> GetMyAdmissionDiscovery(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        if (UserId() is not { } userId) return Forbid();
        return Ok(await analytics.GetApplicantDiscoveryAsync(userId, limit, ct));
    }

    [Authorize(Roles = "Admin,SuperAdmin,Registrar,AdmissionsHead")]
    [HttpGet("admissions/discovery")]
    public async Task<ActionResult<IReadOnlyList<AdmissionExplanationReference>>> GetAdmissionsDiscovery(
        [FromQuery] int limit = 50,
        CancellationToken ct = default) => Ok(await analytics.GetAdmissionsDiscoveryAsync(limit, ct));

    [Authorize(Roles = "Student,HOD,Dean,Admin,SuperAdmin,Registrar,ExamController,VC")]
    [HttpGet("predictions/discovery")]
    public async Task<ActionResult<IReadOnlyList<PredictionExplanationReference>>> GetPredictionDiscovery(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var tenantWide = HasAnyRole(TenantWideRoles);
        var studentId = User.IsInRole("Student") ? StudentId() : null;
        var departmentId = !tenantWide && (User.IsInRole("HOD") || User.IsInRole("Dean")) ? StaffDepartmentId() : null;
        if (!tenantWide && studentId is null && departmentId is null) return Forbid();
        return Ok(await analytics.GetPredictionDiscoveryAsync(studentId, departmentId, tenantWide, limit, ct));
    }

    [Authorize(Roles = "Student")]
    [HttpGet("students/me/telemetry")]
    public async Task<ActionResult<StudentTelemetry>> GetMyTelemetry(CancellationToken ct)
    {
        if (StudentId() is not { } studentId) return Forbid();
        return await analytics.GetLatestTelemetryAsync(studentId, null, ct) is { } telemetry ? Ok(telemetry) : NotFound();
    }

    [Authorize(Roles = "Student")]
    [HttpGet("students/me/courses/{courseId:guid}")]
    public async Task<ActionResult<StudentTelemetry>> GetMyCourseTelemetry(Guid courseId, CancellationToken ct)
    {
        if (StudentId() is not { } studentId) return Forbid();
        return await analytics.GetLatestTelemetryAsync(studentId, courseId, ct) is { } telemetry ? Ok(telemetry) : NotFound();
    }

    [Authorize(Roles = "Admin,SuperAdmin,Registrar,ExamController,VC,HOD,Dean")]
    [HttpGet("students/{studentId:guid}/telemetry")]
    public async Task<ActionResult<StudentTelemetry>> GetAuthorizedStudentTelemetry(Guid studentId, [FromQuery] Guid? courseId, CancellationToken ct)
    {
        var tenantWide = User.Claims.Any(claim => claim.Type == ClaimTypes.Role && TenantWideRoles.Contains(claim.Value, StringComparer.Ordinal));
        var departmentId = StaffDepartmentId();
        if (!tenantWide && departmentId is null) return Forbid();
        return await analytics.GetAuthorizedTelemetryAsync(studentId, courseId, departmentId, tenantWide, ct) is { } telemetry
            ? Ok(telemetry)
            : NotFound();
    }

    [Authorize(Roles = "Student")]
    [HttpGet("students/me/grades/{courseGradeId:guid}/explanation")]
    public async Task<ActionResult<AnalyticsEnvelope<GradeExplanation>>> GetMyGradeExplanation(Guid courseGradeId, CancellationToken ct)
    {
        if (StudentId() is not { } studentId) return Forbid();
        return await analytics.GetGradeExplanationAsync(courseGradeId, studentId, ct) is { } explanation
            ? Ok(explanation)
            : NotFound();
    }

    [Authorize]
    [HttpGet("admissions/{applicationId:guid}/decision-explanation")]
    public async Task<ActionResult<AnalyticsEnvelope<AdmissionDecisionExplanation>>> GetAdmissionDecisionExplanation(Guid applicationId, CancellationToken ct)
    {
        AnalyticsEnvelope<AdmissionDecisionExplanation>? explanation;
        if (HasAnyRole(AdmissionsRoles))
            explanation = await analytics.GetAdmissionExplanationAsync(applicationId, ct);
        else if (UserId() is { } userId)
            explanation = await analytics.GetApplicantAdmissionExplanationAsync(applicationId, userId, ct);
        else
            return Forbid();
        return explanation is not null ? Ok(explanation) : NotFound();
    }

    [Authorize(Roles = "Student,HOD,Dean,Admin,SuperAdmin,Registrar,ExamController,VC")]
    [HttpGet("predictions/{predictionId:guid}/explanation")]
    public async Task<ActionResult<AnalyticsEnvelope<PredictionExplanation>>> GetPredictionExplanation(Guid predictionId, CancellationToken ct)
    {
        var tenantWide = User.Claims.Any(claim => claim.Type == ClaimTypes.Role && TenantWideRoles.Contains(claim.Value, StringComparer.Ordinal));
        var studentId = User.IsInRole("Student") ? StudentId() : null;
        var departmentId = !tenantWide && (User.IsInRole("HOD") || User.IsInRole("Dean")) ? StaffDepartmentId() : null;
        if (!tenantWide && studentId is null && departmentId is null) return Forbid();
        return await analytics.GetPredictionExplanationAsync(predictionId, studentId, departmentId, tenantWide, ct) is { } explanation
            ? Ok(explanation)
            : NotFound();
    }

    [Authorize(Roles = "Admin,SuperAdmin,Registrar,ExamController,VC,HOD,Dean")]
    [HttpGet("models/{modelName}/quality")]
    public async Task<ActionResult<ModelQuality>> GetModelQuality(string modelName, [FromQuery] string? modelVersion, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(modelName) || modelName.Length > 160 || modelVersion?.Length > 80) return BadRequest();
        return await analytics.GetLatestModelQualityAsync(modelName, modelVersion, ct) is { } quality ? Ok(quality) : NotFound();
    }

    [Authorize(Roles = "Admin,SuperAdmin,Registrar,ExamController,VC,HOD,Dean")]
    [HttpGet("models/{modelName}/global-explanation")]
    public async Task<ActionResult<GlobalModelExplanation>> GetGlobalModelExplanation(string modelName, [FromQuery] string? modelVersion, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(modelName) || modelName.Length > 160 || modelVersion?.Length > 80) return BadRequest();
        return await analytics.GetLatestGlobalExplanationAsync(modelName, modelVersion, ct) is { } explanation ? Ok(explanation) : NotFound();
    }

    private Guid? StudentId() => ClaimGuid("student_id");
    private Guid? StaffDepartmentId() => ClaimGuid("staff_department_id");
    private Guid? UserId() => ClaimGuid(ClaimTypes.NameIdentifier) ?? ClaimGuid("sub");

    private bool HasAnyRole(IEnumerable<string> roles) =>
        User.Claims.Any(claim => claim.Type == ClaimTypes.Role && roles.Contains(claim.Value, StringComparer.Ordinal));

    private Guid? ClaimGuid(string type) => Guid.TryParse(User.FindFirst(type)?.Value, out var id) && id != Guid.Empty ? id : null;
}
