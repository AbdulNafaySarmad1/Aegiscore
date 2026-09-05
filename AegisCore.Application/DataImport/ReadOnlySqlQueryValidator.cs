using System.Text.RegularExpressions;

namespace AegisCore.Application.DataImport;

public static class ReadOnlySqlQueryValidator
{
    private static readonly HashSet<string> AllowedStatementKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "WITH"
    };

    private static readonly HashSet<string> StatementKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "WITH", "INSERT", "UPDATE", "DELETE", "MERGE", "UPSERT", "DROP", "ALTER", "CREATE", "TRUNCATE", "COPY",
        "CALL", "EXEC", "EXECUTE", "GRANT", "REVOKE", "COMMENT", "VACUUM", "ANALYZE", "REINDEX", "CLUSTER", "REFRESH", "DO",
        "SET", "RESET", "SHOW", "LOCK", "INTO", "ATTACH", "DETACH", "PRAGMA", "REPLACE", "LOAD", "UNLOAD", "UNION",
        "INTERSECT", "EXCEPT"
    };

    private static readonly HashSet<string> MutatingFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "nextval", "setval", "pg_advisory_lock", "pg_advisory_xact_lock", "dblink_exec", "lo_import", "lo_export"
    };

    public static bool IsValid(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 10_000 ||
            query.Contains(';') || query.Contains("--", StringComparison.Ordinal) || query.Contains("/*", StringComparison.Ordinal))
            return false;

        var withoutLiterals = Regex.Replace(query, "'(?:''|[^'])*'|\"(?:\"\"|[^\"])*\"", " ", RegexOptions.CultureInvariant);
        var tokens = Regex.Matches(withoutLiterals, "[A-Za-z_][A-Za-z0-9_$]*", RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .ToArray();
        if (tokens.Length == 0 || !AllowedStatementKeywords.Contains(tokens[0])) return false;
        if (withoutLiterals.Count(character => character == '(') != withoutLiterals.Count(character => character == ')')) return false;
        if (tokens.Any(MutatingFunctions.Contains)) return false;
        return tokens.Where(StatementKeywords.Contains).All(AllowedStatementKeywords.Contains);
    }
}
