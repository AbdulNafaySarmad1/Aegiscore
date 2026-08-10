namespace AegisCore.Application.CampusOperations;

public interface IElectionService
{
    Task<ElectionSetupDto> CreateElectionAsync(ElectionSetupRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CandidateDto>> GetVerifiedCandidatesAsync(Guid electionId, CancellationToken ct = default);
    Task<VoterTokenDto> IssueVoterTokenAsync(Guid electionId, Guid studentId, CancellationToken ct = default);
    Task<BallotReceiptDto> CastBallotAsync(CastBallotRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ElectionResultDto>> ComputeResultsAsync(Guid electionId, CancellationToken ct = default);
    Task<ElectionParticipationDto> GetParticipationStatsAsync(Guid electionId, CancellationToken ct = default);
}

public sealed record ElectionSetupRequest(
    string Title,
    string ElectionType,
    string VotingMethod,
    DateTimeOffset NominationStart,
    DateTimeOffset NominationEnd,
    DateTimeOffset CampaigningStart,
    DateTimeOffset CampaigningEnd,
    DateTimeOffset VotingStart,
    DateTimeOffset VotingEnd,
    List<PositionSetupRequest> Positions);

public sealed record PositionSetupRequest(
    string PositionName,
    int Seats);

public sealed record ElectionSetupDto(
    Guid ElectionId,
    string Title,
    string Status,
    List<PositionSetupDto> Positions);

public sealed record PositionSetupDto(
    Guid PositionId,
    string PositionName,
    int Seats);

public sealed record CandidateDto(
    Guid Id,
    Guid StudentId,
    string? StudentName,
    Guid PositionId,
    string PositionName,
    int EndorsementCount,
    bool EligibilityVerified,
    string NominationStatus);

public sealed record VoterTokenDto(
    Guid TokenId,
    string TokenHash,
    bool HasVoted);

public sealed record CastBallotRequest(
    Guid ElectionId,
    List<BallotChoice> Choices);

public sealed record BallotChoice(
    Guid PositionId,
    Guid CandidateId);

public sealed record BallotReceiptDto(
    string ReceiptHash,
    DateTimeOffset CastAt);

public sealed record ElectionResultDto(
    Guid PositionId,
    string PositionName,
    Guid CandidateId,
    string CandidateName,
    int VoteCount,
    bool IsElected);

public sealed record ElectionParticipationDto(
    Guid ElectionId,
    int EligibleVoters,
    int VotesCast,
    decimal TurnoutRate);
