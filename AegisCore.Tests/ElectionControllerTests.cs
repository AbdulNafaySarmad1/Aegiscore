using AegisCore.Domain;
using AegisCore.API.Controllers;
using AegisCore.Application.Platform;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.Tests.Controllers;

public sealed class ElectionControllerTests
{
    private readonly Mock<IElectionService> _service;
    private readonly ElectionsController _controller;

    public ElectionControllerTests()
    {
        _service = new Mock<IElectionService>();
        _controller = new ElectionsController(_service.Object);
    }

    [Fact]
    public async Task Create_ValidElection_ReturnsOkWithElection()
    {
        var election = new Election { Id = Guid.NewGuid(), Title = "Student Council Elections 2025" };
        _service.Setup(s => s.CreateElectionAsync(It.IsAny<Election>(), It.IsAny<CancellationToken>())).ReturnsAsync(election);
        var result = await _controller.Create(election, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(election, ok.Value);
    }

    [Fact]
    public async Task Nominate_ValidCandidate_ReturnsNomination()
    {
        var electionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var nomination = new CandidateNomination { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, PositionName = "President" };
        _service.Setup(s => s.NominateCandidateAsync(electionId, studentId, "President", It.IsAny<CancellationToken>())).ReturnsAsync(nomination);
        var result = await _controller.Nominate(electionId, studentId, "President", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(nomination, ok.Value);
    }

    [Fact]
    public async Task IssueToken_ValidStudent_ReturnsToken()
    {
        var electionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var token = new VoterToken { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, TokenHash = "tok-secure-hash" };
        _service.Setup(s => s.IssueVoterTokenAsync(electionId, studentId, It.IsAny<CancellationToken>())).ReturnsAsync(token);
        var result = await _controller.IssueToken(electionId, studentId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(token, ok.Value);
    }

    [Fact]
    public async Task CastVote_ValidToken_ReturnsBallot()
    {
        var electionId = Guid.NewGuid();
        var ballot = new EncryptedBallot { Id = Guid.NewGuid(), ElectionId = electionId, VoterTokenHash = "hash-token", EncryptedVoteData = "encrypted-data", Status = BallotStatus.Cast };
        _service.Setup(s => s.CastVoteAsync(electionId, "tok-secure", "encrypted-vote-data", It.IsAny<CancellationToken>())).ReturnsAsync(ballot);
        var result = await _controller.CastVote(electionId, "tok-secure", "encrypted-vote-data", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(ballot, ok.Value);
    }

    [Fact]
    public async Task Tally_CompletedElection_ReturnsResults()
    {
        var electionId = Guid.NewGuid();
        var results = new List<ElectionResult> { new() { Id = Guid.NewGuid(), ElectionId = electionId, VoteCount = 150 } };
        _service.Setup(s => s.TallyResultsAsync(electionId, It.IsAny<CancellationToken>())).ReturnsAsync(results);
        var result = await _controller.Tally(electionId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Tally_NoVotes_ReturnsEmpty()
    {
        var electionId = Guid.NewGuid();
        _service.Setup(s => s.TallyResultsAsync(electionId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ElectionResult>());
        var result = await _controller.Tally(electionId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Certify_ValidResult_ReturnsCertifiedResult()
    {
        var resultId = Guid.NewGuid();
        var certified = new ElectionResult { Id = resultId };
        _service.Setup(s => s.CertifyResultsAsync(resultId, It.IsAny<CancellationToken>())).ReturnsAsync(certified);
        var result = await _controller.Certify(resultId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(certified, ok.Value);
    }
}
