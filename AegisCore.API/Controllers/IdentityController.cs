using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/identity")]
[Authorize]
public class IdentityController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IPermissionRepository _permissionRepo;
    private readonly IIdentityService _identityService;
    private readonly AegisDbContext _db;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IPermissionRepository permissionRepo,
        IIdentityService identityService,
        AegisDbContext db,
        ILogger<IdentityController> logger)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _permissionRepo = permissionRepo;
        _identityService = identityService;
        _db = db;
        _logger = logger;
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _identityService.CreateUserAsync(
            request.Username, request.Email, request.FirstName, request.LastName, Guid.Empty, cancellationToken);

        return Ok(new UserResponse
        {
            Id = user.Id, Username = user.Username, Email = user.Email,
            FirstName = user.FirstName, LastName = user.LastName,
            Status = user.Status.ToString(), AuthProvider = user.AuthProvider.ToString(), CreatedAt = user.CreatedAt
        });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Read")]
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers([FromQuery] string? status, CancellationToken cancellationToken)
    {
        IReadOnlyList<User> users;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, true, out var s))
            users = await _userRepo.GetByStatusAsync(s, cancellationToken);
        else
            users = await _userRepo.GetAllAsync(cancellationToken);

        var response = users.Select(u => new UserResponse
        {
            Id = u.Id, Username = u.Username, Email = u.Email,
            FirstName = u.FirstName, LastName = u.LastName,
            Status = u.Status.ToString(), AuthProvider = u.AuthProvider.ToString(),
            CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt
        });

        return Ok(new { total = response.Count(), users = response });
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetWithRolesAsync(id, cancellationToken);
        if (user == null) return NotFound();

        var roles = user.UserRoles.Select(ur => new RoleDto
        {
            Id = ur.Role.Id, Name = ur.Role.Name, Description = ur.Role.Description, IsSystem = ur.Role.IsSystem,
            Permissions = ur.Role.RolePermissions?.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id, Name = rp.Permission.Name,
                Resource = rp.Permission.Resource, Action = rp.Permission.Action,
                Module = rp.Permission.Module, Description = rp.Permission.Description
            }).ToList() ?? new()
        }).ToList();

        return Ok(new UserResponse
        {
            Id = user.Id, Username = user.Username, Email = user.Email,
            FirstName = user.FirstName, LastName = user.LastName,
            Status = user.Status.ToString(), AuthProvider = user.AuthProvider.ToString(),
            CreatedAt = user.CreatedAt, LastLoginAt = user.LastLoginAt,
            Roles = roles
        });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Approve")]
    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        await _identityService.DeactivateUserAsync(id, cancellationToken);
        return Ok(new { message = "User deactivated." });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Read")]
    [HttpPost("users/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken cancellationToken)
    {
        await _identityService.SuspendUserAsync(id, cancellationToken);
        return Ok(new { message = "User suspended." });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleRepo.GetAllAsync(cancellationToken);
        var response = roles.Select(r => new RoleDto
        {
            Id = r.Id, Name = r.Name, Description = r.Description, IsSystem = r.IsSystem
        });

        return Ok(new { total = response.Count(), roles = response });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(), Name = request.Name, Description = request.Description, TenantId = Guid.Empty
        };

        await _roleRepo.AddAsync(role, cancellationToken);

        foreach (var permId in request.PermissionIds)
        {
            var rp = new RolePermission { Id = Guid.NewGuid(), RoleId = role.Id, PermissionId = permId };
            _db.RolePermissions.Add(rp);
        }
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("users/assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await _identityService.AssignRoleAsync(request.UserId, request.RoleId, Guid.Empty, cancellationToken);
        return Ok(new { message = "Role assigned." });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("users/remove-role")]
    public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        await _identityService.RemoveRoleAsync(request.UserId, request.RoleId, cancellationToken);
        return Ok(new { message = "Role removed." });
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> ListPermissions([FromQuery] string? module, CancellationToken cancellationToken)
    {
        IReadOnlyList<Permission> permissions;
        if (!string.IsNullOrWhiteSpace(module))
            permissions = await _permissionRepo.GetByModuleAsync(module, cancellationToken);
        else
            permissions = await _permissionRepo.GetAllAsync(cancellationToken);

        var response = permissions.Select(p => new PermissionDto
        {
            Id = p.Id, Name = p.Name, Resource = p.Resource, Action = p.Action, Module = p.Module, Description = p.Description
        });

        return Ok(new { total = response.Count(), permissions = response });
    }

    [HttpGet("matrix")]
    public async Task<IActionResult> GetPermissionMatrix(CancellationToken cancellationToken)
    {
        var roles = await _roleRepo.GetAllAsync(cancellationToken);
        var permissions = await _permissionRepo.GetAllAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToList();
        var allRolePermissions = await _permissionRepo.GetByRolesAsync(roleIds, cancellationToken);

        var matrix = new PermissionMatrixDto
        {
            Roles = roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description, IsSystem = r.IsSystem }).ToList(),
            Permissions = permissions.Select(p => new PermissionDto { Id = p.Id, Name = p.Name, Resource = p.Resource, Action = p.Action, Module = p.Module }).ToList(),
            RolePermissions = allRolePermissions
        };

        return Ok(matrix);
    }

    [HttpGet("users/{id:guid}/permissions")]
    public async Task<IActionResult> GetUserPermissions(Guid id, CancellationToken cancellationToken)
    {
        var permissions = await _identityService.GetUserPermissionsAsync(id, cancellationToken);
        var response = permissions.Select(p => new PermissionDto
        {
            Id = p.Id, Name = p.Name, Resource = p.Resource, Action = p.Action, Module = p.Module
        });

        return Ok(new { total = response.Count(), permissions = response });
    }

    [HttpGet("users/{id:guid}/check-permission")]
    public async Task<IActionResult> CheckPermission(Guid id, [FromQuery] string resource, [FromQuery] string action, CancellationToken cancellationToken)
    {
        var has = await _identityService.HasPermissionAsync(id, resource, action, cancellationToken);
        return Ok(new { userId = id, resource, action, hasPermission = has });
    }
}
