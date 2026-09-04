using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class AchievementControllerTests
{
    private readonly Mock<IAchievementService> _service;
    private readonly AchievementsController _controller;

    public AchievementControllerTests()
    {
        _service = new Mock<IAchievementService>();
        _controller = new AchievementsController(_service.Object);
    }

    [Fact]
    public async Task Detect_ExistingStudent_ReturnsAchievements()
    {
        var studentId = Guid.NewGuid();
        var achievements = new List<StudentBadge> { new() { Id = Guid.NewGuid(), StudentId = studentId }, new() { Id = Guid.NewGuid(), StudentId = studentId } };
        _service.Setup(s => s.DetectAchievementsAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(achievements);

        var result = await _controller.Detect(studentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Detect_NoAchievements_ReturnsEmpty()
    {
        var studentId = Guid.NewGuid();
        _service.Setup(s => s.DetectAchievementsAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<StudentBadge>());

        var result = await _controller.Detect(studentId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task IssueBadge_ValidInput_ReturnsBadge()
    {
        var studentId = Guid.NewGuid();
        var badgeId = Guid.NewGuid();
        var badge = new StudentBadge { Id = Guid.NewGuid(), StudentId = studentId, BadgeId = badgeId, AchievementDetail = "Completed all assignments early" };
        _service.Setup(s => s.IssueBadgeAsync(studentId, badgeId, "Completed all assignments early", It.IsAny<CancellationToken>())).ReturnsAsync(badge);

        var result = await _controller.IssueBadge(studentId, badgeId, "Completed all assignments early", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(badge, ok.Value);
    }

    [Fact]
    public async Task ProcessDeansList_ValidSemester_ReturnsEntries()
    {
        var entries = new List<DeansListEntry> { new() { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), Cgpa = 3.9M } };
        _service.Setup(s => s.ProcessDeansListAsync("2025S", It.IsAny<CancellationToken>())).ReturnsAsync(entries);

        var result = await _controller.ProcessDeansList("2025S", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Leaderboard_ValidScope_ReturnsEntries()
    {
        var entries = new List<LeaderboardEntry> { new() { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), Rank = 1, Score = 4.0M } };
        _service.Setup(s => s.GenerateLeaderboardAsync(LeaderboardScope.Department, "CS", "2025S", It.IsAny<CancellationToken>())).ReturnsAsync(entries);

        var result = await _controller.Leaderboard("Department", "CS", "2025S", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Leaderboard_InvalidScope_ReturnsBadRequest()
    {
        var result = await _controller.Leaderboard("InvalidScope", "CS", "2025S", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Roadmap_ExistingStudent_ReturnsRoadmap()
    {
        var studentId = Guid.NewGuid();
        var roadmap = new AchievementRoadmap { Id = Guid.NewGuid(), StudentId = studentId };
        _service.Setup(s => s.GenerateRoadmapAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(roadmap);

        var result = await _controller.Roadmap(studentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(roadmap, ok.Value);
    }
}
