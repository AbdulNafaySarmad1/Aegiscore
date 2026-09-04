using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class MentalHealthControllerTests
{
    private readonly Mock<IMentalHealthService> _service;
    private readonly MentalHealthController _controller;

    public MentalHealthControllerTests()
    {
        _service = new Mock<IMentalHealthService>();
        _controller = new MentalHealthController(_service.Object);
    }

    [Fact]
    public async Task Submit_ValidTicket_ReturnsOkWithTicket()
    {
        var ticket = new MentalHealthTicket { Id = Guid.NewGuid(), AnonymousToken = "anon-001", Severity = TicketSeverity.Routine.ToString(), ConcernDescription = "Feeling stressed about exams" };
        _service.Setup(s => s.SubmitTicketAsync(It.IsAny<MentalHealthTicket>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var result = await _controller.Submit(ticket, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(ticket, ok.Value);
    }

    [Fact]
    public async Task Classify_ExistingTicket_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.ClassifyAndRouteAsync(id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Classify(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Assign_ValidIds_ReturnsOk()
    {
        var ticketId = Guid.NewGuid();
        var counselorId = Guid.NewGuid();
        _service.Setup(s => s.AssignCounselorAsync(ticketId, counselorId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Assign(ticketId, counselorId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Resolve_ValidResolution_ReturnsOk()
    {
        var ticketId = Guid.NewGuid();
        _service.Setup(s => s.ResolveTicketAsync(ticketId, TicketResolution.Resolved, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Resolve(ticketId, "Resolved", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Resolve_ReferredResolution_ReturnsOk()
    {
        var ticketId = Guid.NewGuid();
        _service.Setup(s => s.ResolveTicketAsync(ticketId, TicketResolution.Referred, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Resolve(ticketId, "Referred", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Resolve_InvalidResolution_ReturnsBadRequest()
    {
        var result = await _controller.Resolve(Guid.NewGuid(), "InvalidResolution", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task MandatoryReport_ValidTrigger_ReturnsReport()
    {
        var ticketId = Guid.NewGuid();
        var report = new MandatoryReport { Id = Guid.NewGuid(), TicketId = ticketId, Trigger = MandatoryReportTrigger.ImminentSelfHarm };
        _service.Setup(s => s.TriggerMandatoryReportAsync(ticketId, MandatoryReportTrigger.ImminentSelfHarm, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _controller.MandatoryReport(ticketId, "ImminentSelfHarm", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(report, ok.Value);
    }

    [Fact]
    public async Task MandatoryReport_ChildSafeguarding_ReturnsReport()
    {
        var ticketId = Guid.NewGuid();
        var report = new MandatoryReport { Id = Guid.NewGuid(), TicketId = ticketId, Trigger = MandatoryReportTrigger.ChildSafeguarding };
        _service.Setup(s => s.TriggerMandatoryReportAsync(ticketId, MandatoryReportTrigger.ChildSafeguarding, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var result = await _controller.MandatoryReport(ticketId, "ChildSafeguarding", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task MandatoryReport_InvalidTrigger_ReturnsBadRequest()
    {
        var result = await _controller.MandatoryReport(Guid.NewGuid(), "InvalidTrigger", CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }
}
