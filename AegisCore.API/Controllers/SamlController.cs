using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/identity/saml")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class SamlController : ControllerBase
{
    private readonly ISamlService _samlService;
    private readonly ISamlConfigurationRepository _samlRepo;
    private readonly IIdentityService _identityService;
    private readonly ILogger<SamlController> _logger;

    public SamlController(
        ISamlService samlService,
        ISamlConfigurationRepository samlRepo,
        IIdentityService identityService,
        ILogger<SamlController> logger)
    {
        _samlService = samlService;
        _samlRepo = samlRepo;
        _identityService = identityService;
        _logger = logger;
    }

    [HttpGet("metadata")]
    [Produces("application/xml")]
    public IActionResult GetMetadata()
    {
        var xml = _samlService.GenerateMetadataXml(Guid.Empty);
        return Content(xml, "application/xml");
    }

    [HttpGet("login")]
    public IActionResult InitiateLogin([FromQuery] string? relayState)
    {
        var redirectUrl = _samlService.GenerateAuthnRequest(Guid.Empty, relayState);
        return Redirect(redirectUrl);
    }

    [HttpPost("acs")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> AssertionConsumerService(
        [FromForm] string SAMLResponse,
        [FromForm] string? RelayState,
        CancellationToken cancellationToken)
    {
        var result = await _samlService.ValidateSamlResponseAsync(SAMLResponse, RelayState, cancellationToken);

        if (!result.IsValid)
        {
            _logger.LogWarning("SAML authentication failed: {Error}", result.Error);
            return BadRequest(new { error = "saml_validation_failed", message = result.Error });
        }

        var user = await _identityService.UpsertUserFromSamlAsync(
            result.NameId ?? "",
            result.Email ?? result.NameId ?? "",
            result.FirstName ?? "",
            result.LastName ?? "",
            result.Claims,
            Guid.Empty,
            cancellationToken);

        return Ok(new
        {
            message = "SAML authentication successful.",
            user = new { id = user.Id, email = user.Email, name = $"{user.FirstName} {user.LastName}" }
        });
    }

    [HttpPost("config")]
    public async Task<IActionResult> ConfigureSaml([FromBody] SamlConfigurationRequest request, CancellationToken cancellationToken)
    {
        var existing = await _samlRepo.GetByTenantAsync(Guid.Empty, cancellationToken);

        var config = existing ?? new SamlConfiguration { Id = Guid.NewGuid(), TenantId = Guid.Empty };

        config.EntityId = request.EntityId;
        config.IdpSsoUrl = request.IdpSsoUrl;
        config.IdpCertificate = request.IdpCertificate;
        config.AttributeMappingJson = System.Text.Json.JsonSerializer.Serialize(
            request.AttributeMapping ?? new Dictionary<string, string>());
        config.IsEnabled = true;
        config.UpdatedAt = DateTime.UtcNow;

        if (existing == null)
        {
            config.SpEntityId = $"{Request.Scheme}://{Request.Host}/api/identity/saml/metadata";
            config.SpAcsUrl = $"{Request.Scheme}://{Request.Host}/api/identity/saml/acs";
            await _samlRepo.AddAsync(config, cancellationToken);
        }
        else
        {
            await _samlRepo.UpdateAsync(config, cancellationToken);
        }

        return Ok(new SamlConfigurationResponse
        {
            Id = config.Id,
            EntityId = config.EntityId,
            SpEntityId = config.SpEntityId,
            SpAcsUrl = config.SpAcsUrl,
            MetadataUrl = $"{Request.Scheme}://{Request.Host}/api/identity/saml/metadata",
            IsEnabled = config.IsEnabled
        });
    }
}
