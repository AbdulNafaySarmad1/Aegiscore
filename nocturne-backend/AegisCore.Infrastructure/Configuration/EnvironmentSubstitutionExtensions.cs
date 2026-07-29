using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Text.RegularExpressions;

namespace AegisCore.Infrastructure.Configuration;

/// <summary>
/// Resolves ${VAR} placeholders in configuration values from environment variables.
/// The project's appsettings.Production.json ships values like "${ELASTICSEARCH_URL";
/// .NET does not substitute these by default, so this provider does it at load time.
/// Unset variables resolve to an empty string.
/// </summary>
public static class EnvironmentSubstitutionExtensions
{
    private static readonly Regex Placeholder = new(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

    public static IConfigurationBuilder AddEnvironmentSubstitution(this IConfigurationBuilder builder)
    {
        var sources = builder.Sources.ToList();
        builder.Sources.Clear();
        foreach (var source in sources)
            builder.Sources.Add(new SubstitutionConfigurationSource(source));
        return builder;
    }

    public static string Substitute(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return Placeholder.Replace(value, m =>
            Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? "");
    }
}

public class SubstitutionConfigurationSource : IConfigurationSource
{
    private readonly IConfigurationSource _inner;

    public SubstitutionConfigurationSource(IConfigurationSource inner) => _inner = inner;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new SubstitutionConfigurationProvider(_inner.Build(builder));
}

public class SubstitutionConfigurationProvider : IConfigurationProvider
{
    private readonly IConfigurationProvider _inner;

    public SubstitutionConfigurationProvider(IConfigurationProvider inner) => _inner = inner;

    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
        => _inner.GetChildKeys(earlierKeys, parentPath);

    public IChangeToken GetReloadToken() => _inner.GetReloadToken();

    public void Load() => _inner.Load();

    public void Set(string key, string? value) => _inner.Set(key, value);

    public bool TryGet(string key, out string? value)
    {
        if (_inner.TryGet(key, out value) && value is not null)
        {
            value = EnvironmentSubstitutionExtensions.Substitute(value);
            return true;
        }
        return false;
    }
}
