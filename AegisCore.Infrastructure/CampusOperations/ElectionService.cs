using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.CampusOperations;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.CampusOperations;

public sealed class ElectionService(AegisDbContext db, ILogger<ElectionService> logger) : IElectionService
{
    public async Task<ElectionSetupDto> CreateElectionAsync(ElectionSetupRequest request, CancellationToken ct = default)
    {
        var tenantId = db.GetRequiredTenantId();
        var election = new Election
        {
            Id = Guid.NewGuid(), Title = request.Title, ElectionType = request.ElectionType,
            VotingMethod = request.VotingMethod, NominationStart = request.NominationStart,
            NominationEnd = request.NominationEnd, CampaigningStart = request.CampaigningStart,
            CampaigningEnd = request.CampaigningEnd, VotingStart = request.VotingStart,
            VotingEnd = request.VotingEnd, Status = "setup", TenantId = tenantId
        };
        db.Set<Election>().Add(election);
        var positions = request.Positions.Select(p => new ElectionPosition
        {
            Id = Guid.NewGuid(), ElectionId = election.Id, PositionName = p.PositionName,
            Seats = p.Seats, TenantId = election.TenantId
        }).ToList();
        foreach (var pos in positions) db.Set<ElectionPosition>().Add(pos);
        await db.SaveChangesAsync(ct);
        return new ElectionSetupDto(election.Id, election.Title, election.Status, positions.Select(p => new PositionSetupDto(p.Id, p.PositionName, p.Seats)).ToList());
    }

    public async Task<IReadOnlyList<CandidateDto>> GetVerifiedCandidatesAsync(Guid electionId, CancellationToken ct = default)
    {
        var electionExists = await db.Set<Election>().AnyAsync(e => e.Id == electionId, ct);
        if (!electionExists) throw new InvalidOperationException("Election not found.");
        var candidates = await db.Set<ElectionCandidate>().Where(c => c.ElectionId == electionId).ToListAsync(ct);
        return candidates.Select(c => new CandidateDto(c.Id, c.StudentId, $"Student-{c.StudentId.ToString()[..8]}", c.PositionId, "Position", c.EndorsementCount, c.EligibilityVerified, c.NominationStatus)).ToList();
    }

    public async Task<VoterTokenDto> IssueVoterTokenAsync(Guid electionId, Guid studentId, CancellationToken ct = default)
    {
        var election = await db.Set<Election>().SingleOrDefaultAsync(e => e.Id == electionId, ct)
            ?? throw new InvalidOperationException("Election not found.");
        var existing = await db.Set<VoterToken>().FirstOrDefaultAsync(t => t.ElectionId == electionId && t.StudentId == studentId, ct);
        if (existing is not null && existing.HasVoted) throw new InvalidOperationException("Student has already voted.");
        var plaintextToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(plaintextToken);
        var token = existing ?? new VoterToken { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, TenantId = election.TenantId };
        token.TokenHash = tokenHash;
        if (existing is null) db.Set<VoterToken>().Add(token);
        await db.SaveChangesAsync(ct);
        return new VoterTokenDto(token.Id, plaintextToken, token.HasVoted);
    }

    public async Task<BallotReceiptDto> CastBallotAsync(CastBallotRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.VoterToken)) throw new UnauthorizedAccessException("A voter token is required.");
        if (request.Choices.Count == 0 || request.Choices.Select(c => c.PositionId).Distinct().Count() != request.Choices.Count)
            throw new ArgumentException("Exactly one candidate must be selected per position.", nameof(request));

        var election = await db.Set<Election>().SingleOrDefaultAsync(e => e.Id == request.ElectionId, ct)
            ?? throw new InvalidOperationException("Election not found.");
        var suppliedHash = HashToken(request.VoterToken);
        var token = await db.Set<VoterToken>().SingleOrDefaultAsync(t => t.ElectionId == request.ElectionId && t.TokenHash == suppliedHash, ct)
            ?? throw new UnauthorizedAccessException("The voter token is invalid.");
        if (token.HasVoted) throw new InvalidOperationException("The voter token has already been used.");

        var candidateIds = request.Choices.Select(c => c.CandidateId).ToList();
        var candidates = await db.Set<ElectionCandidate>()
            .Where(c => c.ElectionId == request.ElectionId && candidateIds.Contains(c.Id) && c.EligibilityVerified)
            .ToListAsync(ct);
        if (candidates.Count != request.Choices.Count || request.Choices.Any(choice => candidates.All(c => c.Id != choice.CandidateId || c.PositionId != choice.PositionId)))
            throw new ArgumentException("One or more ballot choices are invalid.", nameof(request));

        foreach (var choice in request.Choices)
        {
            var ballot = new Ballot
            {
                Id = Guid.NewGuid(), ElectionId = request.ElectionId,
                EncryptedVoteData = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{choice.PositionId}:{choice.CandidateId}")),
                BallotHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{choice.PositionId}{choice.CandidateId}{Guid.NewGuid()}"))),
                CastAt = DateTimeOffset.UtcNow, TenantId = election.TenantId
            };
            db.Set<Ballot>().Add(ballot);
        }
        token.HasVoted = true;
        token.VotedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return new BallotReceiptDto(Guid.NewGuid().ToString("N")[..16], DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<ElectionResultDto>> ComputeResultsAsync(Guid electionId, CancellationToken ct = default)
    {
        var electionExists = await db.Set<Election>().AnyAsync(e => e.Id == electionId, ct);
        if (!electionExists) throw new InvalidOperationException("Election not found.");
        var ballots = await db.Set<Ballot>().Where(b => b.ElectionId == electionId).ToListAsync(ct);
        var positions = await db.Set<ElectionPosition>().Where(p => p.ElectionId == electionId).ToListAsync(ct);
        var candidates = await db.Set<ElectionCandidate>().Where(c => c.ElectionId == electionId).ToListAsync(ct);
        var results = new List<ElectionResultDto>();
        foreach (var position in positions)
        {
            var posCandidates = candidates.Where(c => c.PositionId == position.Id).ToList();
            foreach (var candidate in posCandidates)
            {
                var voteCount = ballots.Count(b => b.EncryptedVoteData.Contains(candidate.Id.ToString()));
                results.Add(new ElectionResultDto(position.Id, position.PositionName, candidate.Id, $"Candidate-{candidate.Id.ToString()[..6]}", voteCount, voteCount > 0 && posCandidates.OrderByDescending(c => ballots.Count(b => b.EncryptedVoteData.Contains(c.Id.ToString()))).Take(position.Seats).Contains(candidate)));
            }
        }
        results.Sort((a, b) => b.VoteCount.CompareTo(a.VoteCount));
        return results;
    }

    public async Task<ElectionParticipationDto> GetParticipationStatsAsync(Guid electionId, CancellationToken ct = default)
    {
        var electionExists = await db.Set<Election>().AnyAsync(e => e.Id == electionId, ct);
        if (!electionExists) throw new InvalidOperationException("Election not found.");
        var eligible = await db.Set<VoterToken>().Where(t => t.ElectionId == electionId).CountAsync(ct);
        var voted = await db.Set<VoterToken>().Where(t => t.ElectionId == electionId && t.HasVoted).CountAsync(ct);
        return new ElectionParticipationDto(electionId, eligible, voted, eligible > 0 ? Math.Round(voted * 100m / eligible, 2) : 0m);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
