using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class GradeAppealsControllerTests
{
    private readonly Mock<IGradeAppealService> _service;
    private readonly GradeAppealsController _controller;

    public GradeAppealsControllerTests()
    {
        _service = new Mock<IGradeAppealService>();
        _controller = new GradeAppealsController(_service.Object);
    }

    [Fact]
    public async Task Submit_ValidAppeal_ReturnsOkWithAppeal()
    {
        var appeal = new GradeAppeal { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), CourseId = Guid.NewGuid(), Semester = "2025S", SubmittedGrade = "B+" };
        _service.Setup(s => s.SubmitAsync(It.IsAny<GradeAppeal>(), It.IsAny<CancellationToken>())).ReturnsAsync(appeal);

        var result = await _controller.Submit(appeal, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(appeal, ok.Value);
    }

    [Fact]
    public async Task GetByStudent_ExistingStudent_ReturnsOkWithAppeals()
    {
        var appeals = new List<GradeAppeal> { new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() } };
        var studentId = Guid.NewGuid();
        _service.Setup(s => s.GetByStudentAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(appeals);

        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetByStudent_NoAppeals_ReturnsEmptyList()
    {
        var studentId = Guid.NewGuid();
        _service.Setup(s => s.GetByStudentAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<GradeAppeal>());

        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithAppeal()
    {
        var id = Guid.NewGuid();
        var appeal = new GradeAppeal { Id = id, StudentId = Guid.NewGuid() };
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(appeal);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(appeal, ok.Value);
    }

    [Fact]
    public async Task Classify_ExistingAppeal_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.ClassifyGroundsAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Classify(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GenerateBrief_ExistingAppeal_ReturnsOkWithBrief()
    {
        var id = Guid.NewGuid();
        var brief = new AppealCommitteeBrief { Id = Guid.NewGuid(), AppealId = id };
        _service.Setup(s => s.GenerateCommitteeBriefAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(brief);

        var result = await _controller.GenerateBrief(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(brief, ok.Value);
    }

    [Fact]
    public async Task Decision_UpheldWithoutGrade_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.ProcessDecisionAsync(id, "upheld", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Decision(id, "upheld", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Decision_Dismissed_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.ProcessDecisionAsync(id, "dismissed", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Decision(id, "dismissed", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Decision_PartiallyUpheldWithAdjustedGrade_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.ProcessDecisionAsync(id, "partially_upheld", "A-", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Decision(id, "partially_upheld", "A-", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Pending_HasPendingAppeals_ReturnsOk()
    {
        var appeals = new List<GradeAppeal> { new() { Id = Guid.NewGuid(), Status = AppealStatus.Submitted } };
        _service.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(appeals);

        var result = await _controller.Pending(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Pending_NoPending_ReturnsEmpty()
    {
        _service.Setup(s => s.GetPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<GradeAppeal>());

        var result = await _controller.Pending(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
