using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class ConvocationControllerTests
{
    private readonly Mock<IConvocationService> _service;
    private readonly ConvocationsController _controller;

    public ConvocationControllerTests()
    {
        _service = new Mock<IConvocationService>();
        _controller = new ConvocationsController(_service.Object);
    }

    [Fact]
    public async Task Create_ValidConvocation_ReturnsOk()
    {
        var convo = new Convocation { Id = Guid.NewGuid(), Title = "Spring 2025 Convocation", AcademicYear = "2025", CeremonyDate = DateTime.UtcNow.AddMonths(3) };
        _service.Setup(s => s.CreateAsync(It.IsAny<Convocation>(), It.IsAny<CancellationToken>())).ReturnsAsync(convo);

        var result = await _controller.Create(convo, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(convo, ok.Value);
    }

    [Fact]
    public async Task CheckEligibility_ExistingConvocation_ReturnsEligibilityList()
    {
        var id = Guid.NewGuid();
        var eligibility = new List<ConvocationEligibility> { new() { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), ConvocationId = id } };
        _service.Setup(s => s.CheckEligibilityAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(eligibility);

        var result = await _controller.CheckEligibility(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task CheckEligibility_NoEligibleStudents_ReturnsEmpty()
    {
        var id = Guid.NewGuid();
        _service.Setup(s => s.CheckEligibilityAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ConvocationEligibility>());

        var result = await _controller.CheckEligibility(id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GenerateCertificates_ExistingConvocation_ReturnsCertificates()
    {
        var id = Guid.NewGuid();
        var certs = new List<DegreeCertificate> { new() { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), ConvocationId = id, SerialNumber = "CERT-001" } };
        _service.Setup(s => s.GenerateCertificatesAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(certs);

        var result = await _controller.GenerateCertificates(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task CheckIn_ValidIds_ReturnsAttendance()
    {
        var convoId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var attendance = new ConvocationAttendance { Id = Guid.NewGuid(), StudentId = studentId, ConvocationId = convoId, CheckInTime = DateTime.UtcNow };
        _service.Setup(s => s.CheckInAsync(convoId, studentId, It.IsAny<CancellationToken>())).ReturnsAsync(attendance);

        var result = await _controller.CheckIn(convoId, studentId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(attendance, ok.Value);
    }

    [Fact]
    public async Task Upcoming_HasUpcomingConvocations_ReturnsList()
    {
        var convos = new List<Convocation> { new() { Id = Guid.NewGuid(), Title = "Fall 2025 Convocation" }, new() { Id = Guid.NewGuid(), Title = "Spring 2026 Convocation" } };
        _service.Setup(s => s.GetUpcomingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(convos);

        var result = await _controller.Upcoming(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Upcoming_NoUpcoming_ReturnsEmpty()
    {
        _service.Setup(s => s.GetUpcomingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Convocation>());

        var result = await _controller.Upcoming(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
