using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AegisCore.Domain.Common;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests.Infrastructure;

public sealed class TenantIsolationTest
{
    [Fact]
    public void All_DbSet_entities_must_be_tenant_scoped()
    {
        var entityTypes = typeof(AegisDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Distinct()
            .OrderBy(t => t.FullName)
            .ToArray();

        var compliant = entityTypes
            .Where(t => typeof(ITenantScoped).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .ToArray();

        var missingInterface = entityTypes
            .Where(t => !typeof(ITenantScoped).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .ToArray();

        var list1Types = missingInterface
            .Where(t => t.GetProperty(nameof(ITenantScoped.TenantId))?.PropertyType == typeof(Guid))
            .OrderBy(t => t.FullName)
            .ToArray();

        var list2Types = missingInterface
            .Where(t => t.GetProperty(nameof(ITenantScoped.TenantId))?.PropertyType != typeof(Guid))
            .OrderBy(t => t.FullName)
            .ToArray();

        var repositoryRoot = FindRepositoryRoot();
        var entitySourceDirectory = Path.Combine(repositoryRoot, "AegisCore.Domain", "Entities");
        var sourceFiles = Directory
            .EnumerateFiles(entitySourceDirectory, "*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var compliantEntries = compliant.Select(t => DescribeEntity(t, repositoryRoot, sourceFiles)).ToArray();
        var list1 = list1Types.Select(t => DescribeEntity(t, repositoryRoot, sourceFiles)).ToArray();
        var list2 = list2Types.Select(t => DescribeEntity(t, repositoryRoot, sourceFiles)).ToArray();

        var reportDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
        Directory.CreateDirectory(reportDirectory);

        File.WriteAllText(
            Path.Combine(reportDirectory, "tenant-isolation-report.json"),
            JsonSerializer.Serialize(new
            {
                TotalDbSetEntities = entityTypes.Length,
                AlreadyCompliantCount = compliantEntries.Length,
                List1Count = list1.Length,
                List2Count = list2.Length,
                GrandTotalGaps = list1.Length + list2.Length,
                AlreadyCompliant = compliantEntries,
                List1HasTenantIdNoInterface = list1,
                List2NoTenantId = list2
            }, new JsonSerializerOptions { WriteIndented = true }));

        var text = new StringBuilder();
        text.AppendLine("LIST 1 — needs ITenantScoped interface:");
        foreach (var entry in list1)
        {
            text.AppendLine($"{entry.EntityName} | {entry.FilePath} | {entry.Namespace}");
        }

        text.AppendLine();
        text.AppendLine("LIST 2 — needs TenantId column + interface:");
        foreach (var entry in list2)
        {
            text.AppendLine($"{entry.EntityName} | {entry.FilePath} | {entry.Namespace}");
        }

        text.AppendLine();
        text.AppendLine($"Total count for List 1: {list1.Length}");
        text.AppendLine($"Total count for List 2: {list2.Length}");
        text.AppendLine($"Grand total: {list1.Length + list2.Length}");

        File.WriteAllText(
            Path.Combine(reportDirectory, "tenant-isolation-report.txt"),
            text.ToString());

        Assert.True(
            missingInterface.Length == 0,
            $"Tenant isolation gaps detected. LIST1_HAS_TENANTID_NO_INTERFACE ({list1.Length}): {string.Join(", ", list1.Select(x => x.EntityName))}{Environment.NewLine}" +
            $"LIST2_NO_TENANTID ({list2.Length}): {string.Join(", ", list2.Select(x => x.EntityName))}");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AegisCore.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing AegisCore.slnx.");
    }

    private static EntitySourceEntry DescribeEntity(Type type, string repositoryRoot, IReadOnlyList<string> sourceFiles)
    {
        var classPattern = new Regex($@"\b(?:class|record)\s+{Regex.Escape(type.Name)}\b", RegexOptions.CultureInvariant);

        foreach (var sourceFile in sourceFiles)
        {
            var source = File.ReadAllText(sourceFile);
            if (!classPattern.IsMatch(source))
            {
                continue;
            }

            var namespaceMatch = Regex.Match(
                source,
                @"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*(?:;|\{)",
                RegexOptions.CultureInvariant);

            return new EntitySourceEntry(
                type.Name,
                Path.GetRelativePath(repositoryRoot, sourceFile).Replace('\\', '/'),
                namespaceMatch.Success ? namespaceMatch.Groups[1].Value : type.Namespace ?? string.Empty);
        }

        return new EntitySourceEntry(type.Name, "SOURCE_FILE_NOT_FOUND", type.Namespace ?? string.Empty);
    }

    private sealed record EntitySourceEntry(string EntityName, string FilePath, string Namespace);
}
