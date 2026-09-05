using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("scim/v2")]
[AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
public class ScimController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly ILogger<ScimController> _logger;

    public ScimController(
        IIdentityService identityService,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        ILogger<ScimController> logger)
    {
        _identityService = identityService;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _logger = logger;
    }

    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? filter, [FromQuery] int startIndex = 1, [FromQuery] int count = 100, CancellationToken cancellationToken = default)
    {
        var users = await _userRepo.GetAllAsync(cancellationToken);
        var resources = users.Skip(startIndex - 1).Take(count).Select(u => new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            id = u.Id.ToString(),
            externalId = u.ExternalId,
            userName = u.Username,
            name = new { givenName = u.FirstName, familyName = u.LastName },
            emails = new[] { new { value = u.Email, primary = true } },
            active = u.Status == UserStatus.Active,
            meta = new { resourceType = "User", created = u.CreatedAt, lastModified = u.CreatedAt }
        });

        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            totalResults = users.Count,
            startIndex,
            itemsPerPage = count,
            Resources = resources.ToList()
        });
    }

    [HttpGet("Users/{id}")]
    public async Task<IActionResult> GetUser(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound(new { detail = "User not found." });

        var user = await _userRepo.GetByIdAsync(guid, cancellationToken);
        if (user == null)
            return NotFound(new { detail = "User not found." });

        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            id = user.Id.ToString(),
            externalId = user.ExternalId,
            userName = user.Username,
            name = new { givenName = user.FirstName, familyName = user.LastName },
            emails = new[] { new { value = user.Email, primary = true } },
            active = user.Status == UserStatus.Active,
            meta = new { resourceType = "User", created = user.CreatedAt, lastModified = user.CreatedAt }
        });
    }

    [HttpPost("Users")]
    public async Task<IActionResult> CreateUser([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var userName = body.TryGetProperty("userName", out var un) ? un.GetString() ?? "" : "";
        var email = "";
        if (body.TryGetProperty("emails", out var emails) && emails.ValueKind == JsonValueKind.Array)
        {
            var firstEmail = emails[0];
            email = firstEmail.TryGetProperty("value", out var ev) ? ev.GetString() ?? "" : "";
        }
        var givenName = body.TryGetProperty("name", out var name) && name.TryGetProperty("givenName", out var gn) ? gn.GetString() ?? "" : "";
        var familyName = body.TryGetProperty("name", out var n2) && n2.TryGetProperty("familyName", out var fn) ? fn.GetString() ?? "" : "";

        var user = await _identityService.CreateUserAsync(userName, email, givenName, familyName, Guid.Empty, cancellationToken);

        return Created($"/scim/v2/Users/{user.Id}", new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            id = user.Id.ToString(),
            userName = user.Username,
            name = new { givenName = user.FirstName, familyName = user.LastName },
            emails = new[] { new { value = user.Email, primary = true } },
            active = true,
            meta = new { resourceType = "User", created = user.CreatedAt }
        });
    }

    [HttpPut("Users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound(new { detail = "User not found." });

        var user = await _userRepo.GetByIdAsync(guid, cancellationToken);
        if (user == null)
            return NotFound(new { detail = "User not found." });

        user.Status = body.TryGetProperty("active", out var active) && active.GetBoolean() ? UserStatus.Active : UserStatus.Inactive;

        if (body.TryGetProperty("name", out var name))
        {
            if (name.TryGetProperty("givenName", out var gn))
                user.FirstName = gn.GetString() ?? user.FirstName;
            if (name.TryGetProperty("familyName", out var fn))
                user.LastName = fn.GetString() ?? user.LastName;
        }

        await _userRepo.UpdateAsync(user, cancellationToken);

        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            id = user.Id.ToString(),
            userName = user.Username,
            name = new { givenName = user.FirstName, familyName = user.LastName },
            emails = new[] { new { value = user.Email, primary = true } },
            active = user.Status == UserStatus.Active,
            meta = new { resourceType = "User", lastModified = DateTime.UtcNow }
        });
    }

    [HttpGet("Groups")]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken)
    {
        var roles = await _roleRepo.GetAllAsync(cancellationToken);

        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            totalResults = roles.Count,
            Resources = roles.Select(r => new
            {
                schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
                id = r.Id.ToString(),
                displayName = r.Name,
                meta = new { resourceType = "Group", created = r.CreatedAt }
            }).ToList()
        });
    }

    [HttpGet("ServiceProviderConfig")]
    public IActionResult GetServiceProviderConfig()
    {
        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig" },
            patch = new { supported = true },
            bulk = new { supported = false, maxOperations = 0, maxPayloadSize = 0 },
            filter = new { supported = true, maxResults = 200 },
            changePassword = new { supported = false },
            sort = new { supported = false },
            etag = new { supported = false },
            authenticationSchemes = new[]
            {
                new { type = "oauthbearertoken", name = "OAuth Bearer Token", description = "Authentication via Bearer token" }
            }
        });
    }

    [HttpGet("ResourceTypes")]
    public IActionResult GetResourceTypes()
    {
        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            Resources = new[]
            {
                new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" }, id = "User", name = "User", endpoint = "/scim/v2/Users", schema = "urn:ietf:params:scim:schemas:core:2.0:User" },
                new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ResourceType" }, id = "Group", name = "Group", endpoint = "/scim/v2/Groups", schema = "urn:ietf:params:scim:schemas:core:2.0:Group" }
            }
        });
    }

    [HttpGet("Schemas")]
    public IActionResult GetSchemas()
    {
        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            Resources = new[]
            {
                new { id = "urn:ietf:params:scim:schemas:core:2.0:User", name = "User" },
                new { id = "urn:ietf:params:scim:schemas:core:2.0:Group", name = "Group" }
            }
        });
    }
}
