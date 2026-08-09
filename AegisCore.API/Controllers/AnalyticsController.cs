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
        var departmentId = DepartmentId();
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

    [Authorize(Roles = "Admin,SuperAdmin,Registrar,AdmissionsHead")]
    [HttpGet("admissions/{applicationId:guid}/decision-explanation")]
    public async Task<ActionResult<AnalyticsEnvelope<AdmissionDecisionExplanation>>> GetAdmissionDecisionExplanation(Guid applicationId, CancellationToken ct)
        => await analytics.GetAdmissionExplanationAsync(applicationId, ct) is { } explanation ? Ok(explanation) : NotFound();

    [Authorize(Roles = "Student,HOD,Dean,Admin,SuperAdmin,Registrar,ExamController,VC")]
    [HttpGet("predictions/{predictionId:guid}/explanation")]
    public async Task<ActionResult<AnalyticsEnvelope<PredictionExplanation>>> GetPredictionExplanation(Guid predictionId, CancellationToken ct)
    {
        var tenantWide = User.Claims.Any(claim => claim.Type == ClaimTypes.Role && TenantWideRoles.Contains(claim.Value, StringComparer.Ordinal));
        var studentId = User.IsInRole("Student") ? StudentId() : null;
        var departmentId = !tenantWide && (User.IsInRole("HOD") || User.IsInRole("Dean")) ? DepartmentId() : null;
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
    private Guid? DepartmentId() => ClaimGuid("department_id");

    private Guid? ClaimGuid(string type) => Guid.TryParse(User.FindFirst(type)?.Value, out var id) && id != Guid.Empty ? id : null;
}
