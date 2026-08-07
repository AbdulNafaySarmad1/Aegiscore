using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class PlagiarismControllerTests
{
    private readonly Mock<IPlagiarismDetectionService> _service;
    private readonly PlagiarismController _controller;

    public PlagiarismControllerTests()
    {
        _service = new Mock<IPlagiarismDetectionService>();
        _controller = new PlagiarismController(_service.Object);
    }

    [Fact]
    public async Task Scan_ValidInput_ReturnsOkWithScan()
    {
        var submissionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var scan = new PlagiarismScan { Id = Guid.NewGuid(), SubmissionId = submissionId, StudentId = studentId, AssignmentId = assignmentId, CourseId = courseId, Semester = "2025S" };
        _service.Setup(s => s.ScanSubmissionAsync(submissionId, studentId, assignmentId, courseId, "2025S", "test submission", It.IsAny<CancellationToken>())).ReturnsAsync(scan);

        var result = await _controller.Scan(submissionId, studentId, assignmentId, courseId, "2025S", "test submission", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(scan, ok.Value);
    }

    [Fact]
    public async Task Scan_EmptyText_StillReturnsOk()
    {
        var submissionId = Guid.NewGuid();
        var scan = new PlagiarismScan { Id = Guid.NewGuid() };
        _service.Setup(s => s.ScanSubmissionAsync(submissionId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2025S", "", It.IsAny<CancellationToken>())).ReturnsAsync(scan);

        var result = await _controller.Scan(submissionId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "2025S", "", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetByAssignment_ExistingAssignment_ReturnsScans()
    {
        var assignmentId = Guid.NewGuid();
        var scans = new List<PlagiarismScan> { new() { Id = Guid.NewGuid(), AssignmentId = assignmentId }, new() { Id = Guid.NewGuid(), AssignmentId = assignmentId } };
        _service.Setup(s => s.GetByAssignmentAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync(scans);

        var result = await _controller.GetByAssignment(assignmentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetByAssignment_NoScans_ReturnsEmpty()
    {
        var assignmentId = Guid.NewGuid();
        _service.Setup(s => s.GetByAssignmentAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PlagiarismScan>());

        var result = await _controller.GetByAssignment(assignmentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithScan()
    {
        var id = Guid.NewGuid();
        var scan = new PlagiarismScan { Id = id, OverallSimilarity = 0.85 };
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(scan);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(scan, ok.Value);
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsException()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((PlagiarismScan?)null);

        var result = await _controller.GetById(id, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
    }
}
