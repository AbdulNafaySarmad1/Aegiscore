# P5 Environment Substitution

`builder.Configuration.AddEnvironmentSubstitution()` is registered immediately after `WebApplication.CreateBuilder(args)`. At that point the default JSON, environment, command-line, and user-secret sources have been added, and configuration has not yet been consumed by service registration.

## Deleted Placeholder Guards

| File | Original line | Guard removed |
|---|---:|---|
| `AegisCore.API/Extensions/ProductionServicesExtensions.cs` | 102 | `origin.TrimStart().StartsWith("${", ...)` |
| `AegisCore.API/Extensions/ProductionServicesExtensions.cs` | 157 | `audience.StartsWith("${", ...)` |
| `AegisCore.API/Extensions/ProductionServicesExtensions.cs` | 161 | `authority.StartsWith("${", ...)` |
| `AegisCore.API/Extensions/ProductionServicesExtensions.cs` | 175 | `publicKeyPem.StartsWith("${", ...)` |
| `AegisCore.API/Extensions/ProductionServicesExtensions.cs` | 184 | `publicKeyPem.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/DbContextServiceExtensions.cs` | 53 | `value.TrimStart().StartsWith("${", ...)` |
| `AegisCore.Infrastructure/AI/AzureDocumentIntelligenceService.cs` | 27 | `endpoint.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/AI/AzureDocumentIntelligenceService.cs` | 28 | `apiKey.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/AI/AzureDocumentIntelligenceService.cs` | 75 | `endpoint.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/AI/AzureDocumentIntelligenceService.cs` | 76 | `apiKey.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/Deployment/DeploymentReadinessChecker.cs` | 115 | `value.TrimStart().StartsWith("${", ...)` |
| `AegisCore.Infrastructure/Security/JwtTokenService.cs` | 35 | `privateKeyPem.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/Security/JwtTokenService.cs` | 36 | `publicKeyPem.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/MicrosoftEcosystem/MicrosoftEcosystemServices.cs` | 149 | `staticToken.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/MicrosoftEcosystem/MicrosoftEcosystemServices.cs` | 194 | `fallbackToken.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/Search/SearchServiceExtensions.cs` | 15 | `esUrl.StartsWith("${", ...)` |
| `AegisCore.Infrastructure/InfrastructureServiceExtensions.cs` | 153 | `value.TrimStart().StartsWith("${", ...)` |

The remaining `Assert.Contains("...${...}")` in `SetupControllerRouteTests` validates Docker Compose interpolation syntax and is not a runtime guard.

## Production Resolution Map

| Environment variable | Configuration path |
|---|---|
| `SMTP_HOST` | `Email:SmtpHost` |
| `SMTP_PORT` | `Email:SmtpPort` |
| `SMTP_FROM` | `Email:FromAddress` |
| `TWILIO_ACCOUNT_SID` | `Twilio:AccountSid` |
| `TWILIO_AUTH_TOKEN` | `Twilio:AuthToken` |
| `ELASTICSEARCH_URL` | `Search:ElasticsearchUrl` |
| `OLLAMA_BASE_URL` | `AI:Ollama:BaseUrl` |
| `OPENAI_API_KEY` | `AI:OpenAI:ApiKey` |
| `RAAST_ENDPOINT` | `Raast:BaseUrl` |

Every configuration path above is backed by a `${VAR}` placeholder in `appsettings.Production.json` and resolves through the substitution provider before service registration.
