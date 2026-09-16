using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AegisCore.API.Extensions;

/// <summary>
/// Marks a controller that only exists while the installation wizard runs. Its services are
/// registered by <see cref="SetupServicesExtensions.AddSetupServices"/> alone, so mapping it in the
/// installed runtime would answer every request with a 500 from the DI container. The installed
/// runtime removes these controllers and answers their routes with a sealed-installation response.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InstallationOnlyAttribute : Attribute;

public sealed class InstallationOnlyControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo)
        => base.IsController(typeInfo) && typeInfo.GetCustomAttribute<InstallationOnlyAttribute>() is null;
}
