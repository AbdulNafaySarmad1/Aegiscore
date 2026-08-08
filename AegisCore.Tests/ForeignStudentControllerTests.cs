using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class ForeignStudentControllerTests
{
    private readonly Mock<IForeignStudentService> _service;
    private readonly ForeignStudentsController _controller;

    public ForeignStudentControllerTests()
    {
        _service = new Mock<IForeignStudentService>();
        _controller = new ForeignStudentsController(_service.Object);
    }

    [Fact]
    public async Task Register_ValidRegistry_ReturnsOk()
    {
        var registry = new ForeignStudentRegistry { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), Nationality = "Nigeria", PassportNumber = "A12345678", VisaType = VisaType.StudentVisa };
        _service.Setup(s => s.RegisterStudentAsync(It.IsAny<ForeignStudentRegistry>(), It.IsAny<CancellationToken>())).ReturnsAsync(registry);

        var result = await _controller.Register(registry, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(registry, ok.Value);
    }

    [Fact]
    public async Task ExpiryAlerts_HasAlerts_ReturnsAlerts()
    {
        var alerts = new List<VisaExpiryAlert> { new() { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), DaysRemaining = 30, Urgency = ExpiryUrgency.ThirtyDays } };
        _service.Setup(s => s.CheckExpiriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(alerts);

        var result = await _controller.ExpiryAlerts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task ExpiryAlerts_NoAlerts_ReturnsEmpty()
    {
        _service.Setup(s => s.CheckExpiriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<VisaExpiryAlert>());

        var result = await _controller.ExpiryAlerts(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task MoraSubmit_ValidSemester_ReturnsSubmission()
    {
        var semesterId = Guid.NewGuid();
        var submission = new MoraSubmission { Id = Guid.NewGuid(), SemesterId = semesterId, SubmissionType = "enrollment", Status = "submitted" };
        _service.Setup(s => s.SubmitToMoraAsync(semesterId.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var result = await _controller.MoraSubmit(semesterId.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(submission, ok.Value);
    }

    [Fact]
    public async Task InitiateRenewal_ExistingRegistry_ReturnsRenewal()
    {
        var registryId = Guid.NewGuid();
        var renewal = new VisaRenewal { Id = Guid.NewGuid(), RegistryId = registryId, Status = RenewalStatus.Submitted };
        _service.Setup(s => s.InitiateRenewalAsync(registryId, It.IsAny<CancellationToken>())).ReturnsAsync(renewal);

        var result = await _controller.InitiateRenewal(registryId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(renewal, ok.Value);
    }

    [Fact]
    public async Task ReportBreach_ValidInput_ReturnsBreach()
    {
        var registryId = Guid.NewGuid();
        var breach = new ComplianceBreach { Id = Guid.NewGuid(), ForeignStudentRecordId = registryId, BreachType = "visa_expired", Description = "Visa expired without renewal" };
        _service.Setup(s => s.ReportBreachAsync(registryId, "Visa expired without renewal", It.IsAny<CancellationToken>())).ReturnsAsync(breach);

        var result = await _controller.ReportBreach(registryId, "Visa expired without renewal", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(breach, ok.Value);
    }

    [Fact]
    public async Task Compliance_HasCompliance_ReturnsReport()
    {
        var students = new List<ForeignStudentRegistry> { new() { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), Nationality = "India", Compliance = ComplianceLevel.FullyCompliant } };
        _service.Setup(s => s.GetComplianceReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(students);

        var result = await _controller.Compliance(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
