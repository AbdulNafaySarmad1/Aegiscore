using System.Text.RegularExpressions;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// PostgreSQL is configured with a retrying execution strategy, and a retrying strategy refuses a
/// transaction the application starts itself: "The configured execution strategy
/// 'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions." Every such
/// transaction must therefore run inside <c>CreateExecutionStrategy().ExecuteAsync(...)</c>.
///
/// This was not a theory. Recording a fee payment answered 500 on PostgreSQL for exactly this
/// reason; twelve methods were fixed then, and nine more still had it — invoice matching, bank
/// reconciliation adjustments, asset disposal, asset revaluation and impairment, switching
/// accounting standard, processing an erasure request, approving a key ceremony, a petty-cash
/// expense and a standing-order spend. Each would throw on the first real request and on no test,
/// because a test database without retry configured accepts what PostgreSQL refuses. So the guard
/// reads the source: it fails the build rather than waiting for a deployment to find the next one.
/// </summary>
public class TransactionExecutionStrategyTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AegisCore.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return directory!.FullName;
    }

    [Fact]
    public void Every_Transaction_Runs_Inside_The_Execution_Strategy()
    {
        var root = RepositoryRoot();
        var offenders = new List<string>();

        foreach (var project in new[] { "AegisCore.Infrastructure", "AegisCore.API", "AegisCore.Application" })
        {
            var directory = Path.Combine(root, project);
            if (!Directory.Exists(directory)) continue;
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
                    continue;

                var lines = File.ReadAllLines(file);
                for (var index = 0; index < lines.Length; index++)
                {
                    // Only EF transactions are governed by the execution strategy. A transaction on a
                    // raw NpgsqlConnection — the support desk writes its tickets that way — is not.
                    if (!lines[index].Contains(".Database.BeginTransactionAsync(", StringComparison.Ordinal)) continue;
                    if (!IsCovered(lines, index))
                        offenders.Add($"{Path.GetRelativePath(root, file)}:{index + 1}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "These transactions are started outside an execution strategy and will throw on PostgreSQL. "
            + "Wrap the method body in db.Database.CreateExecutionStrategy().ExecuteAsync(async () => { ... }): "
            + string.Join(", ", offenders));
    }

    /// <summary>
    /// Walk back from the transaction to the start of its method. The transaction is covered when a
    /// CreateExecutionStrategy call appears on the way, and not covered when the method declaration
    /// arrives first. A method that first checks <c>CurrentTransaction is not null</c> and runs
    /// inline under a caller's transaction counts as covered too — that is the correct shape for a
    /// step that may be called either way.
    /// </summary>
    private static bool IsCovered(string[] lines, int index)
    {
        var declaration = new Regex(@"^\s{4}(public|private|protected|internal)\b");
        for (var cursor = index; cursor >= 0; cursor--)
        {
            var line = lines[cursor];
            if (line.Contains("CreateExecutionStrategy", StringComparison.Ordinal)) return true;
            if (line.Contains("CurrentTransaction is not null", StringComparison.Ordinal)) return true;
            if (cursor < index && declaration.IsMatch(line)) return false;
        }
        return false;
    }
}
