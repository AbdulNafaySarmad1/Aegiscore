using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class DefaulterPredictionControllerTests
{
    private readonly Mock<IDefaulterPredictionService> _service;
    private readonly DefaulterPredictionController _controller;

    public DefaulterPredictionControllerTests()
    {
        _service = new Mock<IDefaulterPredictionService>();
        _controller = new DefaulterPredictionController(_service.Object);
    }

    [Fact]
    public async Task Predict_ValidStudent_ReturnsRiskScore()
    {
        var studentId = Guid.NewGuid();
        var score = new DefaulterRiskScore { Id = Guid.NewGuid(), StudentId = studentId, Semester = "2025S", ProbabilityScore = 0.72, Tier = DefaulterRiskTier.Yellow };
        _service.Setup(s => s.PredictRiskAsync(studentId, "2025S", It.IsAny<CancellationToken>())).ReturnsAsync(score);

        var result = await _controller.Predict(studentId, "2025S", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(score, ok.Value);
    }

    [Fact]
    public async Task GetScores_ExistingSemester_ReturnsScores()
    {
        var scores = new List<DefaulterRiskScore> { new() { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), Semester = "2025S", ProbabilityScore = 0.45 } };
        _service.Setup(s => s.GetRiskScoresBySemesterAsync("2025S", It.IsAny<CancellationToken>())).ReturnsAsync(scores);

        var result = await _controller.GetScores("2025S", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetScores_NoScores_ReturnsEmpty()
    {
        _service.Setup(s => s.GetRiskScoresBySemesterAsync("2025S", It.IsAny<CancellationToken>())).ReturnsAsync(new List<DefaulterRiskScore>());

        var result = await _controller.GetScores("2025S", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Intervene_ValidTier_ReturnsIntervention()
    {
        var riskScoreId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var intervention = new DefaulterIntervention { Id = Guid.NewGuid(), RiskScoreId = riskScoreId, StudentId = studentId, TriggeredByTier = DefaulterRiskTier.Red };
        _service.Setup(s => s.GenerateInterventionAsync(riskScoreId, studentId, DefaulterRiskTier.Red, It.IsAny<CancellationToken>())).ReturnsAsync(intervention);

        var result = await _controller.Intervene(riskScoreId, studentId, "Red", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(intervention, ok.Value);
    }

    [Fact]
    public async Task Intervene_InvalidTier_ReturnsBadRequest()
    {
        var result = await _controller.Intervene(Guid.NewGuid(), Guid.NewGuid(), "InvalidTier", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Intervene_GreenTier_ReturnsIntervention()
    {
        var riskScoreId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var intervention = new DefaulterIntervention { Id = Guid.NewGuid(), RiskScoreId = riskScoreId, StudentId = studentId, TriggeredByTier = DefaulterRiskTier.Green };
        _service.Setup(s => s.GenerateInterventionAsync(riskScoreId, studentId, DefaulterRiskTier.Green, It.IsAny<CancellationToken>())).ReturnsAsync(intervention);

        var result = await _controller.Intervene(riskScoreId, studentId, "Green", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Forecast_ValidSemester_ReturnsForecast()
    {
        var semesterId = Guid.NewGuid();
        var forecast = new CashFlowForecast { Id = Guid.NewGuid(), SemesterId = semesterId, ExpectedCollection = 500000m, ProjectedShortfall = 25000m };
        _service.Setup(s => s.GenerateForecastAsync(semesterId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

        var result = await _controller.Forecast(semesterId.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(forecast, ok.Value);
    }

    [Fact]
    public async Task Outcomes_HasOutcomes_ReturnsList()
    {
        var outcomes = new List<DefaulterIntervention> { new() { Id = Guid.NewGuid(), IsSuccessful = true }, new() { Id = Guid.NewGuid(), IsSuccessful = false } };
        _service.Setup(s => s.GetInterventionOutcomesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(outcomes);

        var result = await _controller.Outcomes(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Outcomes_NoOutcomes_ReturnsEmpty()
    {
        _service.Setup(s => s.GetInterventionOutcomesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<DefaulterIntervention>());

        var result = await _controller.Outcomes(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
