using AegisCore.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Tests.Infrastructure;

public sealed class EnvironmentSubstitutionTest
{
    [Fact]
    public void AddEnvironmentSubstitution_resolves_environment_variable_placeholder()
    {
        const string variableName = "TEST_VAR";
        const string expected = "hello-resolved";
        var previous = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(variableName, expected);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestKey"] = "${TEST_VAR}"
                })
                .AddEnvironmentSubstitution()
                .Build();
            Assert.Equal(expected, configuration["TestKey"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, previous);
        }
    }
}
