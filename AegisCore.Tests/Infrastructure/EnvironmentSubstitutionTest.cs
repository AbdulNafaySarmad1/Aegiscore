using AegisCore.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Tests.Infrastructure;

public class EnvironmentSubstitutionTest
{
    [Fact]
    public void AddEnvironmentSubstitution_ResolvesEnvironmentVariablePlaceholder()
    {
        const string variableName = "TEST_VAR";
        const string expected = "hello-resolved";
        var original = Environment.GetEnvironmentVariable(variableName);

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
            Environment.SetEnvironmentVariable(variableName, original);
        }
    }
}
