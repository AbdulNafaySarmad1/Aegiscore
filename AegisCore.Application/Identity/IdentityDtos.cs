namespace AegisCore.Application.Identity;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Guid> PermissionIds { get; set; } = new();
}

public class AssignRoleRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class PermissionMatrixDto
{
    public List<RoleDto> Roles { get; set; } = new();
    public List<PermissionDto> Permissions { get; set; } = new();
    public Dictionary<Guid, List<Guid>> RolePermissions { get; set; } = new();
}

public class SamlConfigurationRequest
{
    public string EntityId { get; set; } = string.Empty;
    public string IdpSsoUrl { get; set; } = string.Empty;
    public string IdpCertificate { get; set; } = string.Empty;
    public Dictionary<string, string>? AttributeMapping { get; set; }
}

public class SamlConfigurationResponse
{
    public Guid Id { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string SpEntityId { get; set; } = string.Empty;
    public string SpAcsUrl { get; set; } = string.Empty;
    public string MetadataUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
