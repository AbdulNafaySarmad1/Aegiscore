using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class ProctoringControllerTests
{
    private readonly Mock<IProctoringService> _service;
    private readonly ProctoringController _controller;

    public ProctoringControllerTests()
    {
        _service = new Mock<IProctoringService>();
        _controller = new ProctoringController(_service.Object);
    }

    [Fact]
    public async Task CreateSession_ValidSession_ReturnsOkWithSession()
    {
        var session = new ProctoringSession { Id = Guid.NewGuid(), ExamId = Guid.NewGuid(), DurationMinutes = 120, CameraRequired = true };
        _service.Setup(s => s.CreateSessionAsync(It.IsAny<ProctoringSession>(), It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var result = await _controller.CreateSession(session, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(session, ok.Value);
    }

    [Fact]
    public async Task VerifyEnvironment_ValidIds_ReturnsRecord()
    {
        var sessionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var record = new StudentProctoringRecord { Id = Guid.NewGuid(), SessionId = sessionId, StudentId = studentId, VerificationStatus = EnvironmentVerificationStatus.Passed };
        _service.Setup(s => s.VerifyStudentEnvironmentAsync(sessionId, studentId, It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var result = await _controller.VerifyEnvironment(sessionId, studentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(record, ok.Value);
    }

    [Fact]
    public async Task LogEvent_ValidSeverity_ReturnsOk()
    {
        var recordId = Guid.NewGuid();
        _service.Setup(s => s.LogProctorEventAsync(recordId, ProctorEventSeverity.Warning, "SuspiciousMovement", "Student looked away repeatedly", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.LogEvent(recordId, "Warning", "SuspiciousMovement", "Student looked away repeatedly", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task LogEvent_CriticalSeverity_ReturnsOk()
    {
        var recordId = Guid.NewGuid();
        _service.Setup(s => s.LogProctorEventAsync(recordId, ProctorEventSeverity.Critical, "MultipleFaces", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.LogEvent(recordId, "Critical", "MultipleFaces", null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task LogEvent_InvalidSeverity_ReturnsBadRequest()
    {
        var recordId = Guid.NewGuid();

        var result = await _controller.LogEvent(recordId, "InvalidSeverity", "EventType", null, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task LogEvent_InfoSeverity_ReturnsOk()
    {
        var recordId = Guid.NewGuid();
        _service.Setup(s => s.LogProctorEventAsync(recordId, ProctorEventSeverity.Info, "SessionStart", null, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.LogEvent(recordId, "Info", "SessionStart", null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Complete_ValidRecord_ReturnsOkWithRecord()
    {
        var recordId = Guid.NewGuid();
        var record = new StudentProctoringRecord { Id = recordId, IntegrityStatus = StudentIntegrityStatus.Clean };
        _service.Setup(s => s.CompleteProctoringAsync(recordId, It.IsAny<CancellationToken>())).ReturnsAsync(record);

        var result = await _controller.Complete(recordId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(record, ok.Value);
    }

    [Fact]
    public async Task GetRecords_ExistingSession_ReturnsRecords()
    {
        var sessionId = Guid.NewGuid();
        var records = new List<StudentProctoringRecord> { new() { Id = Guid.NewGuid(), SessionId = sessionId }, new() { Id = Guid.NewGuid(), SessionId = sessionId } };
        _service.Setup(s => s.GetSessionRecordsAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(records);

        var result = await _controller.GetRecords(sessionId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetRecords_NoRecords_ReturnsEmpty()
    {
        var sessionId = Guid.NewGuid();
        _service.Setup(s => s.GetSessionRecordsAsync(sessionId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<StudentProctoringRecord>());

        var result = await _controller.GetRecords(sessionId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
