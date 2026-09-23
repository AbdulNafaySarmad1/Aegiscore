using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AegisCore.API.Authorization;
using AegisCore.Application.Integrations;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Integrations.Forms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

/// <summary>
/// Incoming form submissions from external form tools (JotForm, or anything that speaks the
/// generic contract in docs/integrations/external-forms.md), and the screens that say what each
/// field means. The webhook is anonymous at the HTTP layer and authenticated by a per-form token.
/// </summary>
[ApiController]
[Route("api/integrations/forms")]
public sealed class FormIntegrationsController(
    AegisDbContext db,
    IFormIngestionService ingestion,
    IEnumerable<IFormPayloadAdapter> adapters,
    IConfiguration configuration,
    ILogger<FormIntegrationsController> logger) : ControllerBase
{
    public const string TokenHeader = "X-AegisCore-Form-Token";
    private static readonly string[] Providers = ["generic", "jotform", FormIngestionService.NativeProvider];

    // ── The webhook ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Receives one submission. The token may be sent as <c>Authorization: Bearer</c>, as
    /// <c>X-AegisCore-Form-Token</c>, or — for tools whose webhook settings take only a URL, which
    /// includes JotForm — as the <c>token</c> query parameter over HTTPS.
    /// </summary>
    [HttpPost("{tenantId:guid}/webhook")]
    [AllowAnonymous]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> Receive(Guid tenantId, CancellationToken ct)
    {
        var token = PresentedToken();
        if (tenantId == Guid.Empty || token is null) return Unauthorized(new { error = "A form token is required." });
        var hash = HashToken(token);
        // Anonymous: the tenant filter has nothing to bind to, so the tenant is an explicit predicate.
        var source = await db.FormIntegrationSources.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(s => s.TenantId == tenantId && s.TokenHash == hash && s.IsActive, ct);
        var tenantActive = source is not null && await db.Tenants.IgnoreQueryFilters().AsNoTracking().AnyAsync(t => t.Id == tenantId && t.IsActive, ct);
        if (source is null || !tenantActive) return Unauthorized(new { error = "The form token is not valid for this institution." });
        if (source.Provider == FormIngestionService.NativeProvider) return BadRequest(new { error = "This source is an AegisCore form; it does not take webhooks." });
        HttpContext.Items["TenantId"] = tenantId;

        var adapter = adapters.FirstOrDefault(a => a.Provider == source.Provider);
        if (adapter is null) return BadRequest(new { error = $"No adapter is installed for {source.Provider}." });

        FormSubmissionEnvelope envelope;
        try
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            var body = "";
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(ct);
                foreach (var (key, value) in form) fields[key] = value.ToString();
            }
            else
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                body = await reader.ReadToEndAsync(ct);
            }
            envelope = adapter.Parse(Request.ContentType ?? "", body, fields);
        }
        catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException or InvalidDataException)
        {
            logger.LogWarning("Form webhook for source {SourceId} rejected an unreadable payload: {Reason}", source.Id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        if (!string.IsNullOrWhiteSpace(source.ExternalFormId) && envelope.ExternalFormId is not null
            && !string.Equals(source.ExternalFormId, envelope.ExternalFormId, StringComparison.Ordinal))
            return BadRequest(new { error = "The submission is for a different form than this token was issued for." });

        try
        {
            var result = await ingestion.IngestAsync(source.Id, envelope, ct);
            logger.LogInformation("Form webhook {SourceId}: submission {Submission} {Status} ({Recorded} answers recorded, {Unmapped} unmapped, duplicate {Duplicate}).",
                source.Id, result.SubmissionId, result.Status, result.RecordedCount, result.UnmappedCount, result.Duplicate);
            return Ok(new
            {
                accepted = true, submissionId = result.SubmissionId, status = result.Status, duplicate = result.Duplicate,
                fields = result.FieldCount, recorded = result.RecordedCount, unmapped = result.UnmappedCount,
            });
        }
        catch (FormatException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Sources ───────────────────────────────────────────────────────────────────────────────

    [HttpGet("sources")]
    [RequirePermission("Integrations", "Read")]
    public async Task<IActionResult> Sources(CancellationToken ct)
    {
        var sources = await db.FormIntegrationSources.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
        var ids = sources.Select(s => s.Id).ToList();
        var mappingCounts = await db.FormFieldMappings.AsNoTracking().Where(m => ids.Contains(m.SourceId))
            .GroupBy(m => new { m.SourceId, Unmapped = m.Target == FormFieldTargets.Unmapped })
            .Select(g => new { g.Key.SourceId, g.Key.Unmapped, Count = g.Count() }).ToListAsync(ct);
        var submissionCounts = await db.FormIntegrationSubmissions.AsNoTracking().Where(s => ids.Contains(s.SourceId))
            .GroupBy(s => s.SourceId).Select(g => new { SourceId = g.Key, Count = g.Count() }).ToListAsync(ct);
        return Ok(sources.Select(s => new
        {
            s.Id, s.Name, s.Provider, s.ExternalFormId, s.ScaleMax, s.DefaultCourseId, s.DefaultSemester, s.IsActive,
            s.CreatedAt, s.LastReceivedAt, tokenHint = s.TokenHint,
            webhookUrl = s.Provider == FormIngestionService.NativeProvider ? null : WebhookUrl(s.TenantId),
            fields = mappingCounts.Where(c => c.SourceId == s.Id).Sum(c => c.Count),
            unmappedFields = mappingCounts.Where(c => c.SourceId == s.Id && c.Unmapped).Sum(c => c.Count),
            submissions = submissionCounts.FirstOrDefault(c => c.SourceId == s.Id)?.Count ?? 0,
        }));
    }

    [HttpPost("sources")]
    [RequirePermission("Integrations", "Write")]
    public async Task<IActionResult> CreateSource([FromBody] FormSourceRequest request, CancellationToken ct)
    {
        var problem = await ValidateAsync(request, ct);
        if (problem is not null) return BadRequest(new { error = problem });
        var token = NewToken();
        var source = new FormIntegrationSource
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Provider = request.Provider.Trim().ToLowerInvariant(),
            ExternalFormId = Blank(request.ExternalFormId), ScaleMax = request.ScaleMax ?? 5,
            DefaultCourseId = request.DefaultCourseId, DefaultSemester = Blank(request.DefaultSemester),
            TokenHash = HashToken(token), TokenHint = token[^4..], CreatedByUserId = CurrentUserId,
        };
        db.FormIntegrationSources.Add(source);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Form integration source {SourceId} ({Provider}) created by {UserId}.", source.Id, source.Provider, CurrentUserId);
        // The token is shown once. Only its hash is stored.
        return Ok(new { source.Id, source.Name, source.Provider, token, webhookUrl = WebhookUrl(source.TenantId), tokenHint = source.TokenHint });
    }

    [HttpPut("sources/{id:guid}")]
    [RequirePermission("Integrations", "Write")]
    public async Task<IActionResult> UpdateSource(Guid id, [FromBody] FormSourceRequest request, CancellationToken ct)
    {
        var source = await db.FormIntegrationSources.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (source is null) return NotFound();
        var problem = await ValidateAsync(request, ct);
        if (problem is not null) return BadRequest(new { error = problem });
        if (!string.Equals(source.Provider, request.Provider.Trim(), StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "A source's form tool cannot change; create a new source instead." });
        source.Name = request.Name.Trim();
        source.ExternalFormId = Blank(request.ExternalFormId);
        source.ScaleMax = request.ScaleMax ?? source.ScaleMax;
        source.DefaultCourseId = request.DefaultCourseId;
        source.DefaultSemester = Blank(request.DefaultSemester);
        source.IsActive = request.IsActive ?? source.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("sources/{id:guid}/token")]
    [RequirePermission("Integrations", "Write")]
    public async Task<IActionResult> RotateToken(Guid id, CancellationToken ct)
    {
        var source = await db.FormIntegrationSources.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (source is null) return NotFound();
        var token = NewToken();
        source.TokenHash = HashToken(token);
        source.TokenHint = token[^4..];
        source.TokenRotatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Form integration source {SourceId} token rotated by {UserId}.", source.Id, CurrentUserId);
        return Ok(new { token, webhookUrl = WebhookUrl(source.TenantId), tokenHint = source.TokenHint });
    }

    /// <summary>
    /// Disconnects a form: its token stops working and its mapping and stored deliveries are removed.
    /// The answers already recorded from it stay in the report unless <c>discardResponses</c> is set.
    /// </summary>
    [HttpDelete("sources/{id:guid}")]
    [RequirePermission("Integrations", "Write")]
    public async Task<IActionResult> DeleteSource(Guid id, [FromQuery] bool discardResponses = false, CancellationToken ct = default)
    {
        var source = await db.FormIntegrationSources.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (source is null) return NotFound();
        var submissionIds = await db.FormIntegrationSubmissions.Where(s => s.SourceId == id).Select(s => s.Id).ToListAsync(ct);
        var discarded = 0;
        if (discardResponses)
            discarded = await db.OutcomeSurveyResponses.Where(r => r.SubmissionId != null && submissionIds.Contains(r.SubmissionId.Value)).ExecuteDeleteAsync(ct);
        else
            await db.OutcomeSurveyResponses.Where(r => r.SubmissionId != null && submissionIds.Contains(r.SubmissionId.Value))
                .ExecuteUpdateAsync(u => u.SetProperty(r => r.SubmissionId, (Guid?)null), ct);
        await db.FormIntegrationSubmissions.Where(s => s.SourceId == id).ExecuteDeleteAsync(ct);
        await db.FormFieldMappings.Where(m => m.SourceId == id).ExecuteDeleteAsync(ct);
        db.FormIntegrationSources.Remove(source);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Form integration source {SourceId} removed by {UserId}; {Discarded} recorded answer(s) discarded.", id, CurrentUserId, discarded);
        return Ok(new { removed = true, submissions = submissionIds.Count, discardedAnswers = discarded });
    }

    // ── Mapping ───────────────────────────────────────────────────────────────────────────────

    [HttpGet("sources/{id:guid}/mappings")]
    [RequirePermission("Integrations", "Read")]
    public async Task<IActionResult> Mappings(Guid id, CancellationToken ct)
    {
        if (!await db.FormIntegrationSources.AnyAsync(s => s.Id == id, ct)) return NotFound();
        var rows = await db.FormFieldMappings.AsNoTracking().Where(m => m.SourceId == id)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.ExternalFieldId).ToListAsync(ct);
        return Ok(rows.Select(m => new
        {
            m.Id, m.ExternalFieldId, m.QuestionText, m.Target, m.CloCode, m.ProgramOutcomeId, m.Category, m.SortOrder, m.FirstSeenAt, m.UpdatedAt,
        }));
    }

    /// <summary>
    /// Sets the meaning of many fields at once, creating rows for fields not yet seen, so a 77-question
    /// form can be loaded from its question list before the first response arrives.
    /// </summary>
    [HttpPut("sources/{id:guid}/mappings")]
    [RequirePermission("Integrations", "Write")]
    public async Task<IActionResult> SaveMappings(Guid id, [FromBody] List<FieldMappingRequest> request, CancellationToken ct)
    {
        if (!await db.FormIntegrationSources.AnyAsync(s => s.Id == id, ct)) return NotFound();
        if (request.Count is 0 or > 1000) return BadRequest(new { error = "Send between 1 and 1000 field mappings." });
        var poIds = request.Where(r => r.ProgramOutcomeId != null).Select(r => r.ProgramOutcomeId!.Value).Distinct().ToList();
        var knownPos = await db.ProgramOutcomes.AsNoTracking().Where(p => poIds.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct);
        var errors = new List<string>();
        foreach (var row in request)
        {
            var field = row.ExternalFieldId?.Trim() ?? "";
            if (field.Length is 0 or > 200) { errors.Add("Every row needs a field id of at most 200 characters."); continue; }
            if (!FormFieldTargets.All.Contains(row.Target)) errors.Add($"{field}: \"{row.Target}\" is not a mapping target.");
            else if (row.Target == FormFieldTargets.Clo && string.IsNullOrWhiteSpace(row.CloCode)) errors.Add($"{field}: a CLO mapping needs a CLO code.");
            else if (row.Target == FormFieldTargets.Plo && (row.ProgramOutcomeId is not Guid po || !knownPos.Contains(po))) errors.Add($"{field}: choose an existing programme outcome.");
        }
        if (request.Select(r => r.ExternalFieldId?.Trim()).Distinct(StringComparer.Ordinal).Count() != request.Count) errors.Add("A field appears more than once.");
        if (errors.Count > 0) return BadRequest(new { error = "The mapping was not saved.", errors = errors.Take(50) });

        var existing = await db.FormFieldMappings.Where(m => m.SourceId == id).ToDictionaryAsync(m => m.ExternalFieldId, StringComparer.Ordinal, ct);
        var nextOrder = existing.Count == 0 ? 0 : existing.Values.Max(m => m.SortOrder) + 1;
        var created = 0;
        var changed = 0;
        foreach (var row in request)
        {
            var field = row.ExternalFieldId!.Trim();
            if (!existing.TryGetValue(field, out var mapping))
            {
                mapping = new FormFieldMapping { Id = Guid.NewGuid(), SourceId = id, ExternalFieldId = field, QuestionText = field, SortOrder = row.SortOrder ?? nextOrder++ };
                db.FormFieldMappings.Add(mapping);
                existing[field] = mapping;
                created++;
            }
            if (!string.IsNullOrWhiteSpace(row.QuestionText)) mapping.QuestionText = row.QuestionText.Trim()[..Math.Min(row.QuestionText.Trim().Length, 1000)];
            if (row.SortOrder is int order) mapping.SortOrder = order;
            mapping.Target = row.Target;
            mapping.CloCode = row.Target == FormFieldTargets.Clo ? row.CloCode!.Trim().ToUpperInvariant() : null;
            mapping.ProgramOutcomeId = row.Target == FormFieldTargets.Plo ? row.ProgramOutcomeId : null;
            mapping.Category = row.Target == FormFieldTargets.Satisfaction ? Blank(row.Category) ?? "General" : null;
            mapping.UpdatedAt = DateTime.UtcNow;
            mapping.UpdatedByUserId = CurrentUserId;
            changed++;
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Form integration source {SourceId}: {Changed} field mapping(s) saved ({Created} new) by {UserId}.", id, changed, created, CurrentUserId);
        return Ok(new { saved = changed, created, unmapped = existing.Values.Count(m => m.Target == FormFieldTargets.Unmapped) });
    }

    [HttpPost("sources/{id:guid}/reprocess")]
    [RequirePermission("Integrations", "Write")]
    public async Task<IActionResult> Reprocess(Guid id, CancellationToken ct)
    {
        if (!await db.FormIntegrationSources.AnyAsync(s => s.Id == id, ct)) return NotFound();
        return Ok(await ingestion.ReprocessAsync(id, ct));
    }

    [HttpGet("sources/{id:guid}/submissions")]
    [RequirePermission("Integrations", "Read")]
    public async Task<IActionResult> Submissions(Guid id, [FromQuery] int take = 25, CancellationToken ct = default)
    {
        if (!await db.FormIntegrationSources.AnyAsync(s => s.Id == id, ct)) return NotFound();
        var rows = await db.FormIntegrationSubmissions.AsNoTracking().Where(s => s.SourceId == id)
            .OrderByDescending(s => s.ReceivedAt).Take(Math.Clamp(take, 1, 200))
            .Select(s => new { s.Id, s.ExternalSubmissionId, s.ReceivedAt, s.Status, s.FieldCount, s.RecordedCount, s.UnmappedCount, s.Problems, s.CourseId, s.InstructorName, s.Semester })
            .ToListAsync(ct);
        var courseIds = rows.Where(r => r.CourseId != null).Select(r => r.CourseId!.Value).Distinct().ToList();
        var codes = await db.Courses.AsNoTracking().Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Code ?? c.Title, ct);
        return Ok(rows.Select(r => new
        {
            r.Id, r.ExternalSubmissionId, r.ReceivedAt, r.Status, r.FieldCount, r.RecordedCount, r.UnmappedCount, r.InstructorName, r.Semester,
            course = r.CourseId is Guid c && codes.TryGetValue(c, out var code) ? code : null,
            problems = string.IsNullOrEmpty(r.Problems) ? Array.Empty<string>() : r.Problems.Split('\n'),
        }));
    }

    /// <summary>What a field can be mapped to: the courses, the CLO codes in use, and the programme outcomes.</summary>
    [HttpGet("options")]
    [RequirePermission("Integrations", "Read")]
    public async Task<IActionResult> Options(CancellationToken ct)
    {
        var courses = await db.Courses.AsNoTracking().OrderBy(c => c.Code).Select(c => new { c.Id, c.Code, c.Title }).ToListAsync(ct);
        var clos = await db.CourseLearningOutcomes.AsNoTracking().Select(c => new { c.Code, c.CourseId }).ToListAsync(ct);
        var programOutcomes = await db.ProgramOutcomes.AsNoTracking().OrderBy(p => p.Code).Select(p => new { p.Id, p.Code, p.Description, p.ProgramId }).ToListAsync(ct);
        return Ok(new
        {
            targets = FormFieldTargets.All,
            courses,
            cloCodes = clos.GroupBy(c => c.Code.ToUpperInvariant()).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { code = g.Key, courses = g.Select(c => c.CourseId).Distinct().Count() }),
            programOutcomes,
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────────────────

    private string? PresentedToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Blank(authorization[7..]);
        return Blank(Request.Headers[TokenHeader].ToString()) ?? Blank(Request.Query["token"].ToString());
    }

    private async Task<string?> ValidateAsync(FormSourceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) return "Give the form a name of at most 200 characters.";
        if (!Providers.Contains(request.Provider?.Trim().ToLowerInvariant())) return $"The form tool must be one of: {string.Join(", ", Providers)}.";
        if (request.ScaleMax is < 2 or > 10) return "The rating scale must top out between 2 and 10.";
        if (request.ExternalFormId?.Length > 200 || request.DefaultSemester?.Length > 50) return "The form id or semester is too long.";
        if (request.DefaultCourseId is Guid course && !await db.Courses.AnyAsync(c => c.Id == course, ct)) return "The fixed course does not exist.";
        return null;
    }

    private string WebhookUrl(Guid tenantId)
    {
        var origin = configuration["APP_URL"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
        return $"{origin}/api/integrations/forms/{tenantId:D}/webhook";
    }

    private Guid? CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static string NewToken() => "acf_" + Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record FormSourceRequest(string Name, string Provider, string? ExternalFormId, int? ScaleMax, Guid? DefaultCourseId, string? DefaultSemester, bool? IsActive);

public sealed record FieldMappingRequest(string? ExternalFieldId, string? QuestionText, string Target, string? CloCode, Guid? ProgramOutcomeId, string? Category, int? SortOrder);
