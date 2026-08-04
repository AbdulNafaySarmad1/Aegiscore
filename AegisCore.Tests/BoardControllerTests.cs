using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class BoardControllerTests
{
    private readonly Mock<IBoardPortalService> _service;
    private readonly BoardController _controller;

    public BoardControllerTests()
    {
        _service = new Mock<IBoardPortalService>();
        _controller = new BoardController(_service.Object);
    }

    [Fact]
    public async Task Dashboard_ValidRole_ReturnsDashboard()
    {
        var dashboard = new ExecutiveDashboard { Id = Guid.NewGuid(), UserRole = "Dean", FiscalYear = "2025", CompositeRankingScore = 87.5 };
        _service.Setup(s => s.GenerateDashboardAsync("Dean", "2025", It.IsAny<CancellationToken>())).ReturnsAsync(dashboard);

        var result = await _controller.Dashboard("Dean", "2025", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dashboard, ok.Value);
    }

    [Fact]
    public async Task BoardPack_ValidInput_ReturnsPack()
    {
        var date = DateTime.UtcNow.AddDays(14);
        var pack = new BoardMeetingPack { Id = Guid.NewGuid(), Title = "Q3 Board Meeting", MeetingDate = date };
        _service.Setup(s => s.GenerateBoardPackAsync("Q3 Board Meeting", date, It.IsAny<CancellationToken>())).ReturnsAsync(pack);

        var result = await _controller.BoardPack("Q3 Board Meeting", date, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(pack, ok.Value);
    }

    [Fact]
    public async Task Alerts_HasAlerts_ReturnsAlerts()
    {
        var alerts = new List<BoardAlert> { new() { Id = Guid.NewGuid(), Title = "Accreditation deadline approaching", Category = AlertCategory.Financial }, new() { Id = Guid.NewGuid(), Title = "Budget variance detected", Category = AlertCategory.Compliance } };
        _service.Setup(s => s.GetAlertsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(alerts);

        var result = await _controller.Alerts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Alerts_NoAlerts_ReturnsEmpty()
    {
        _service.Setup(s => s.GetAlertsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<BoardAlert>());

        var result = await _controller.Alerts(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task StrategicGoals_HasGoals_ReturnsGoals()
    {
        var goals = new List<StrategicPlanGoal> { new() { Id = Guid.NewGuid(), Title = "Increase research output", CurrentValue = 65, TargetValue = 100, PercentAchieved = 65 } };
        _service.Setup(s => s.GetStrategicGoalsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(goals);

        var result = await _controller.StrategicGoals(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task StrategicGoals_NoGoals_ReturnsEmpty()
    {
        _service.Setup(s => s.GetStrategicGoalsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<StrategicPlanGoal>());

        var result = await _controller.StrategicGoals(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Benchmarks_ValidYear_ReturnsComparisons()
    {
        var benchmarks = new List<BenchmarkComparison> { new() { Id = Guid.NewGuid(), FactorName = "Graduation Rate", InstitutionValue = 92, NationalAverage = 85, PeerAverage = 88, AcademicYear = "2025", IsAboveAverage = true } };
        _service.Setup(s => s.GetBenchmarksAsync("2025", It.IsAny<CancellationToken>())).ReturnsAsync(benchmarks);

        var result = await _controller.Benchmarks("2025", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Benchmarks_NoData_ReturnsEmpty()
    {
        _service.Setup(s => s.GetBenchmarksAsync("2025", It.IsAny<CancellationToken>())).ReturnsAsync(new List<BenchmarkComparison>());

        var result = await _controller.Benchmarks("2025", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
