using System.Collections;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace AegisCore.API.Logging;

public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "token", "key", "jwt", "apikey", "authorization"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        if (value is IDictionary dictionary)
        {
            var properties = new List<LogEventProperty>();
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                if (IsSensitive(key))
                    properties.Add(new LogEventProperty(key, new ScalarValue("***REDACTED***")));
                else
                    properties.Add(new LogEventProperty(key, propertyValueFactory.CreatePropertyValue(entry.Value, true)));
            }

            result = new StructureValue(properties);
            return true;
        }

        var type = value.GetType();
        if (IsExcludedType(type))
        {
            result = new StructureValue(Array.Empty<LogEventProperty>());
            return false;
        }

        var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToList();

        if (!allProps.Any(p => IsSensitive(p.Name)))
        {
            result = new StructureValue(Array.Empty<LogEventProperty>());
            return false;
        }

        var structureProps = allProps.Select(p =>
        {
            var propValue = p.GetValue(value);
            if (IsSensitive(p.Name))
                return new LogEventProperty(p.Name, new ScalarValue("***REDACTED***"));

            return new LogEventProperty(p.Name, propertyValueFactory.CreatePropertyValue(propValue, true));
        });

        result = new StructureValue(structureProps);
        return true;
    }

    private static bool IsSensitive(string name)
        => SensitiveKeys.Any(sk => name.Contains(sk, StringComparison.OrdinalIgnoreCase));

    private static bool IsExcludedType(Type type)
        => type.IsPrimitive
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(DateTime)
        || type == typeof(DateTimeOffset)
        || type == typeof(Guid)
        || type == typeof(TimeSpan)
        || type == typeof(Uri)
        || type == typeof(Version)
        || type.IsEnum;
}
