using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/communications")]
[Authorize]
public class CommunicationsController : ControllerBase
{
    private readonly AegisDbContext _db;
    private readonly IGrievanceService _grievances;

    public CommunicationsController(AegisDbContext db, IGrievanceService grievances)
    {
        _db = db;
        _grievances = grievances;
    }

    [HttpPost("whatsapp/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> WhatsAppWebhook([FromBody] WhatsAppWebhookRequest request, CancellationToken ct)
    {
        foreach (var status in request.Statuses)
        {
            var delivery = await _db.NotificationDeliveries.FirstOrDefaultAsync(d => d.Id.ToString() == status.ClientReference, ct);
            if (delivery == null) continue;
            delivery.Status = status.Status.ToLowerInvariant() switch
            {
                "delivered" => DeliveryStatus.Delivered,
                "read" => DeliveryStatus.Opened,
                "failed" => DeliveryStatus.Failed,
                _ => delivery.Status
            };
            delivery.DeliveredAt ??= delivery.Status is DeliveryStatus.Delivered or DeliveryStatus.Opened ? DateTime.UtcNow : null;
            delivery.OpenedAt ??= delivery.Status == DeliveryStatus.Opened ? DateTime.UtcNow : null;
            delivery.ErrorMessage = status.Error;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new { updated = request.Statuses.Count });
    }

    [HttpGet("notices")]
    public async Task<IActionResult> Notices([FromQuery] string visibility = "all", CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var notices = await _db.NoticeBoardItems.AsNoTracking()
            .Where(n => !n.IsArchived && (n.Visibility == "all" || n.Visibility == visibility) && (!n.ExpiryDate.HasValue || n.ExpiryDate > now))
            .OrderByDescending(n => n.PostedDate)
            .ToListAsync(ct);
        return Ok(new { total = notices.Count, notices });
    }

    [HttpPost("notices")]
    public async Task<IActionResult> CreateNotice([FromBody] NoticeRequest request, CancellationToken ct)
    {
        var notice = new Domain.Entities.NoticeBoardItem { Id = Guid.NewGuid(), Title = request.Title, Content = request.Content, PostedBy = request.PostedBy, Visibility = request.Visibility, ExpiryDate = request.ExpiryDate };
        _db.NoticeBoardItems.Add(notice);
        await _db.SaveChangesAsync(ct);
        return Ok(notice);
    }

    [HttpPost("grievances")]
    public async Task<IActionResult> CreateGrievance([FromBody] CreateGrievanceTicketRequest request, [FromQuery] Guid submitterId, CancellationToken ct)
        => Ok(await _grievances.CreateAsync(request, submitterId, ct));

    [HttpGet("grievances")]
    public async Task<IActionResult> GetGrievances([FromQuery] Guid? submitterId, CancellationToken ct)
        => Ok(new { tickets = await _grievances.ListAsync(submitterId, ct) });

    [HttpPost("grievances/{ticketId:guid}/responses")]
    public async Task<IActionResult> Respond(Guid ticketId, [FromBody] GrievanceResponseRequest request, [FromQuery] Guid responderId, CancellationToken ct)
        => Ok(await _grievances.RespondAsync(ticketId, request, responderId, ct));
}

public record WhatsAppWebhookRequest(List<WhatsAppStatusDto> Statuses);
public record WhatsAppStatusDto(string ClientReference, string Status, string? Error);
public record NoticeRequest(string Title, string Content, Guid PostedBy, string Visibility, DateTime? ExpiryDate);
