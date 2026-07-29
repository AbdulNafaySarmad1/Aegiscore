using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;

namespace AegisCore.Tests;

public sealed class EnhancedSetupAndSupportTests
{
    [Fact]
    public async Task EveryGeneratedWorkbookPassesItsOwnValidator()
    {
        var root = Path.Combine(Path.GetTempPath(), $"aegis-setup-{Guid.NewGuid():N}");
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(root);
        var service = new EnhancedSetupService(environment.Object);

        foreach (var template in EnhancedSetupService.Templates.Values)
        {
            var bytes = service.GenerateTemplate(template.Key);
            await using var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "file", $"{template.Key}.xlsx");

            var result = await service.ValidateAsync(template.Key, formFile, CancellationToken.None);

            Assert.True(result.IsValid, $"{template.Key}: {string.Join("; ", result.Errors)}");
            Assert.Equal(1, result.RowCount);
        }
    }

    [Fact]
    public void HierarchyRejectsDuplicateDepartmentCodes()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath)
            .Returns(Path.Combine(Path.GetTempPath(), $"aegis-setup-{Guid.NewGuid():N}"));
        var service = new EnhancedSetupService(environment.Object);
        var request = new SaveHierarchyRequest("Vice Chancellor",
        [
            new(Guid.NewGuid(), "Computer Science", "CS", "Dr. One"),
            new(Guid.NewGuid(), "Computing", "CS", "Dr. Two")
        ]);

        var error = Assert.Throws<ArgumentException>(() => service.SaveHierarchy(request));

        Assert.Contains("codes must be unique", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("password_reset", "Use the verified reset page and follow the emailed link.", true, 0.98)]
    [InlineData("grades", "I am not sure. I need to escalate this to the support team.", true, 0.43)]
    [InlineData("other", "", false, 0)]
    public void ConfidenceScoringEscalatesUncertainAnswers(
        string category,
        string response,
        bool hasKnowledge,
        decimal expectedMaximum)
    {
        var score = SupportTicketService.EvaluateConfidence(category, response, hasKnowledge);

        Assert.InRange(score, 0, expectedMaximum);
        if (response.Contains("escalate", StringComparison.OrdinalIgnoreCase))
            Assert.True(score < 0.70m);
    }
}

