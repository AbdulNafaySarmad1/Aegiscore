using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class SemesterFreezeControllerTests
{
    private readonly Mock<ISemesterFreezeService> _service;
    private readonly SemesterFreezesController _controller;

    public SemesterFreezeControllerTests()
    {
        _service = new Mock<ISemesterFreezeService>();
        _controller = new SemesterFreezesController(_service.Object);
    }

    [Fact]
    public async Task Apply_ValidFreeze_ReturnsOkWithFreeze()
    {
        var freeze = new SemesterFreeze { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), CurrentSemester = "2025S", Type = FreezeType.Deferral };
        _service.Setup(s => s.ApplyAsync(It.IsAny<SemesterFreeze>(), It.IsAny<CancellationToken>())).ReturnsAsync(freeze);

        var result = await _controller.Apply(freeze, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(freeze, ok.Value);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsFreeze()
    {
        var id = Guid.NewGuid();
        var freeze = new SemesterFreeze { Id = id, StudentId = Guid.NewGuid() };
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(freeze);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(freeze, ok.Value);
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsException()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((SemesterFreeze?)null);

        var result = await _controller.GetById(id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
    }

    [Fact]
    public async Task GetByStudent_ExistingStudent_ReturnsFreezes()
    {
        var studentId = Guid.NewGuid();
        var freezes = new List<SemesterFreeze> { new() { Id = Guid.NewGuid(), StudentId = studentId } };
        _service.Setup(s => s.GetByStudentAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(freezes);

        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetByStudent_NoFreezes_ReturnsEmpty()
    {
        var studentId = Guid.NewGuid();
        _service.Setup(s => s.GetByStudentAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<SemesterFreeze>());

        var result = await _controller.GetByStudent(studentId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Approve_ApprovedDecision_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        _service.Setup(s => s.ProcessApprovalAsync(id, true, null, approverId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Approve(id, true, null, approverId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Approve_RejectedDecision_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        _service.Setup(s => s.ProcessApprovalAsync(id, false, "Insufficient documentation", approverId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Approve(id, false, "Insufficient documentation", approverId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task InitiateReturn_ValidFreeze_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.InitiateReturnAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.InitiateReturn(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
