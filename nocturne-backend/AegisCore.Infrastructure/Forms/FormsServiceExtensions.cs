using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Forms;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Forms;

public static class FormsServiceExtensions
{
    public static IServiceCollection AddFormsBuilder(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IFormDefinitionRepository, FormDefinitionRepository>();
        services.TryAddScoped<IFormSubmissionRepository, FormSubmissionRepository>();

        services.TryAddScoped<IFormService, FormService>();

        return services;
    }
}
