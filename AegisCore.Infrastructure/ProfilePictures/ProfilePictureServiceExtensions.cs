using AegisCore.Application.ProfilePictures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.ProfilePictures;

public static class ProfilePictureServiceExtensions
{
    public static IServiceCollection AddProfilePictures(this IServiceCollection services)
    {
        services.TryAddScoped<IProfilePictureService, ProfilePictureService>();
        return services;
    }
}
