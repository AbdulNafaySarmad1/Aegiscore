using System.Text.Json;
using AegisCore.Application.Admissions;
using AegisCore.Domain;
using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Microsoft.Graph;
using GoogleUser = Google.Apis.Admin.Directory.directory_v1.Data.User;
using GoogleUserName = Google.Apis.Admin.Directory.directory_v1.Data.UserName;

namespace AegisCore.Infrastructure.Admissions;

internal static class EmailLocalPart
{
    public static string Build(string firstName, string lastName, int sequence)
    {
        static string Clean(string s) => new(s.Trim().ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
        var baseName = $"{Clean(firstName)}.{Clean(lastName)}";
        if (string.IsNullOrWhiteSpace(baseName.Trim('.'))) baseName = "student";
        return sequence <= 0 ? baseName : $"{baseName}{sequence}";
    }
}

/// <summary>Mode C — the admin types the institutional address; no external API call is made.</summary>
public sealed class ManualEmailProvisioner : IInstitutionalEmailProvisioner
{
    public EmailProvisioningMode Mode => EmailProvisioningMode.Manual;

    public Task<ProvisionedEmail> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default)
    {
        // The caller supplies the address; nothing to create.
        var address = string.IsNullOrWhiteSpace(request.DomainName)
            ? throw new InvalidOperationException("Manual provisioning requires the institutional email to be supplied.")
            : request.DomainName;
        return Task.FromResult(new ProvisionedEmail(address, Created: false));
    }
}

/// <summary>Mode A — Google Workspace via the Admin SDK Directory API with a service account.</summary>
public sealed class GoogleWorkspaceEmailProvisioner(ILogger<GoogleWorkspaceEmailProvisioner> logger) : IInstitutionalEmailProvisioner
{
    public EmailProvisioningMode Mode => EmailProvisioningMode.GoogleWorkspace;

    public async Task<ProvisionedEmail> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DomainName) || string.IsNullOrWhiteSpace(request.CredentialsJson))
        {
            throw new InvalidOperationException("Google Workspace provisioning needs a domain and service-account credentials.");
        }

        var creds = JsonSerializer.Deserialize<GoogleCreds>(request.CredentialsJson!)
            ?? throw new InvalidOperationException("Google Workspace credentials are not valid JSON.");

        var credential = GoogleCredential.FromJson(request.CredentialsJson)
            .CreateScoped(DirectoryService.Scope.AdminDirectoryUser)
            .CreateWithUser(creds.AdminEmail);

        using var service = new DirectoryService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AegisCore"
        });

        var sequence = request.Sequence;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var address = $"{EmailLocalPart.Build(request.FirstName, request.LastName, sequence)}@{request.DomainName}";
            try
            {
                await service.Users.Insert(new GoogleUser
                {
                    PrimaryEmail = address,
                    Name = new GoogleUserName { GivenName = request.FirstName, FamilyName = request.LastName },
                    Password = Guid.NewGuid().ToString("N") + "Aa1!",
                    ChangePasswordAtNextLogin = true
                }).ExecuteAsync(ct);
                logger.LogInformation("Provisioned Google Workspace account {Address}", address);
                return new ProvisionedEmail(address, Created: true);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Conflict)
            {
                sequence = sequence <= 0 ? 1 : sequence + 1; // name taken — append / bump the sequence
            }
        }

        throw new InvalidOperationException("Could not find a free Google Workspace address after several attempts.");
    }

    private sealed record GoogleCreds(string AdminEmail);
}

/// <summary>Mode B — Microsoft 365 via MS Graph with an app registration (client credentials).</summary>
public sealed class Microsoft365EmailProvisioner(ILogger<Microsoft365EmailProvisioner> logger) : IInstitutionalEmailProvisioner
{
    public EmailProvisioningMode Mode => EmailProvisioningMode.Microsoft365;

    public async Task<ProvisionedEmail> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DomainName) || string.IsNullOrWhiteSpace(request.CredentialsJson))
        {
            throw new InvalidOperationException("Microsoft 365 provisioning needs a domain and app-registration credentials.");
        }

        var creds = JsonSerializer.Deserialize<M365Creds>(request.CredentialsJson!)
            ?? throw new InvalidOperationException("Microsoft 365 credentials are not valid JSON.");

        var credential = new ClientSecretCredential(creds.TenantId, creds.ClientId, creds.ClientSecret);
        var graph = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);

        var sequence = request.Sequence;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var local = EmailLocalPart.Build(request.FirstName, request.LastName, sequence);
            var address = $"{local}@{request.DomainName}";
            try
            {
                await graph.Users.PostAsync(new Microsoft.Graph.Models.User
                {
                    AccountEnabled = true,
                    DisplayName = $"{request.FirstName} {request.LastName}",
                    MailNickname = local,
                    UserPrincipalName = address,
                    PasswordProfile = new Microsoft.Graph.Models.PasswordProfile
                    {
                        ForceChangePasswordNextSignIn = true,
                        Password = Guid.NewGuid().ToString("N") + "Aa1!"
                    }
                }, cancellationToken: ct);
                logger.LogInformation("Provisioned Microsoft 365 account {Address}", address);
                return new ProvisionedEmail(address, Created: true);
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex) when (
                ex.ResponseStatusCode == 400 || ex.ResponseStatusCode == 409)
            {
                sequence = sequence <= 0 ? 1 : sequence + 1;
            }
        }

        throw new InvalidOperationException("Could not find a free Microsoft 365 address after several attempts.");
    }

    private sealed record M365Creds(string TenantId, string ClientId, string ClientSecret);
}

public sealed class InstitutionalEmailProvisionerFactory(IEnumerable<IInstitutionalEmailProvisioner> provisioners)
    : IInstitutionalEmailProvisionerFactory
{
    public IInstitutionalEmailProvisioner Resolve(EmailProvisioningMode mode)
        => provisioners.FirstOrDefault(p => p.Mode == mode)
           ?? throw new InvalidOperationException($"No email provisioner is registered for mode {mode}.");
}
