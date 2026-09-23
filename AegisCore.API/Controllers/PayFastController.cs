using System.Security.Claims;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Finance.Gateways.PayFast;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

/// <summary>
/// PayFast (Pakistan) hosted checkout for fee demands. The student starts an attempt here, is sent
/// to PayFast's own payment page, and comes back to the portal; PayFast separately calls the IPN.
/// No endpoint here accepts card data — there is nothing to accept it into.
/// </summary>
[ApiController]
[Route("api/payments/payfast")]
public sealed class PayFastController(AegisDbContext db, PayFastCheckoutService checkout, ILogger<PayFastController> logger) : ControllerBase
{
    private const int MaxFields = 60;

    /// <summary>
    /// PayFast's server-to-server notification (CHECKOUT_URL). Anonymous at the HTTP layer; every
    /// notice is authenticated by PayFast's validation hash and, for a success, re-read from
    /// PayFast's transaction API before anything is recorded. PayFast sends the detail in the query
    /// string; a form body is read too.
    /// </summary>
    [HttpGet("{tenantId:guid}/ipn")]
    [HttpPost("{tenantId:guid}/ipn")]
    [AllowAnonymous]
    [RequestSizeLimit(65_536)]
    public async Task<IActionResult> Ipn(Guid tenantId, CancellationToken ct)
    {
        var fields = await CollectAsync(ct);
        if (tenantId == Guid.Empty || fields is null) return BadRequest();
        // Anonymous: the tenant filter has nothing to bind to, so the tenant is an explicit predicate.
        var active = await db.Tenants.IgnoreQueryFilters().AsNoTracking().AnyAsync(t => t.Id == tenantId && t.IsActive, ct);
        if (!active) return BadRequest();
        HttpContext.Items["TenantId"] = tenantId;

        var outcome = await checkout.ProcessNotificationAsync(tenantId, fields, "ipn", null, ct);
        return outcome switch
        {
            PayFastNotificationOutcome.Rejected => BadRequest(),
            // Ask PayFast to try again later; the expiry sweep also re-checks with PayFast.
            PayFastNotificationOutcome.VerificationUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable),
            _ => Ok(new { received = true })
        };
    }

    /// <summary>Starts an attempt to pay one fee demand in full, and returns the form the browser posts to PayFast.</summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> Begin([FromBody] BeginPayFastCheckoutRequest request, CancellationToken ct)
    {
        if (Tenant is not Guid tenantId) return Forbid();
        if (Student is not Guid studentId) return Problem("Only a student can pay their own fee challan online.", statusCode: StatusCodes.Status403Forbidden);
        if (request.FeeDemandId == Guid.Empty) return UnprocessableEntity(new { success = false, error = "A fee demand is required." });
        return await Run(async () =>
        {
            var start = await checkout.BeginAsync(tenantId, request.FeeDemandId, studentId, ct);
            return Ok(new { success = true, start.SessionId, start.BasketId, start.ActionUrl, start.Fields, start.Amount, start.Currency, start.ExpiresAt, start.IsSandbox });
        });
    }

    [HttpGet("checkout/{basketId}")]
    [Authorize]
    public async Task<IActionResult> Get(string basketId, CancellationToken ct)
    {
        if (Tenant is not Guid tenantId || Student is not Guid studentId) return Forbid();
        return await Run(async () => Ok(await checkout.GetAsync(tenantId, basketId, studentId, ct)));
    }

    /// <summary>
    /// The student's browser hands over what PayFast put on the return URL. It gets exactly the
    /// same scrutiny as the IPN, so a return can settle an attempt when the IPN is late or cannot
    /// reach AegisCore — and a forged return settles nothing.
    /// </summary>
    [HttpPost("checkout/{basketId}/confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm(string basketId, [FromBody] Dictionary<string, string?> returned, CancellationToken ct)
    {
        if (Tenant is not Guid tenantId || Student is not Guid studentId) return Forbid();
        if (returned is null || returned.Count > MaxFields) return UnprocessableEntity(new { success = false, error = "The return parameters are invalid." });
        var fields = returned.Where(x => x.Value is not null && x.Key.Length <= 64 && x.Value.Length <= 1024)
            .ToDictionary(x => x.Key, x => x.Value!, StringComparer.OrdinalIgnoreCase);
        // The basket in the path must be the one PayFast returned; the hash is what authenticates it.
        if (fields.TryGetValue("basket_id", out var returnedBasket) && !string.Equals(returnedBasket.Trim(), basketId, StringComparison.Ordinal))
            return UnprocessableEntity(new { success = false, error = "The return parameters name another payment attempt." });
        return await Run(async () =>
        {
            var outcome = await checkout.ProcessNotificationAsync(tenantId, fields, "return", studentId, ct);
            var view = await checkout.GetAsync(tenantId, basketId, studentId, ct);
            return Ok(new { verification = outcome.ToString(), checkout = view });
        });
    }

    [HttpPost("checkout/{basketId}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(string basketId, CancellationToken ct)
    {
        if (Tenant is not Guid tenantId || Student is not Guid studentId) return Forbid();
        return await Run(async () => Ok(await checkout.CancelAsync(tenantId, basketId, studentId, ct)));
    }

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (PayFastCheckoutException ex)
        {
            var status = ex.Code switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "forbidden" => StatusCodes.Status403Forbidden,
                "gateway_error" => StatusCodes.Status502BadGateway,
                "unavailable" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status422UnprocessableEntity
            };
            return StatusCode(status, new { success = false, error = ex.Message });
        }
    }

    private async Task<IReadOnlyDictionary<string, string>?> CollectAsync(CancellationToken ct)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in Request.Query) fields[key] = value.ToString();
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            foreach (var (key, value) in form) fields[key] = value.ToString();
        }
        if (fields.Count is 0 or > MaxFields || fields.Any(x => x.Key.Length > 64 || x.Value.Length > 1024))
        {
            logger.LogWarning("PayFast IPN request had no usable fields or too many; ignored.");
            return null;
        }
        return fields;
    }

    private Guid? Tenant => Guid.TryParse(User.FindFirstValue("tenant_id") ?? User.FindFirstValue("tid"), out var id) && id != Guid.Empty ? id : null;
    private Guid? Student => Guid.TryParse(User.FindFirstValue("student_id"), out var id) && id != Guid.Empty ? id : null;
}

public sealed record BeginPayFastCheckoutRequest(Guid FeeDemandId);
