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
        var election = new Election
        {
            Id = Guid.NewGuid(), Title = request.Title, ElectionType = request.ElectionType,
            VotingMethod = request.VotingMethod, NominationStart = request.NominationStart,
            NominationEnd = request.NominationEnd, CampaigningStart = request.CampaigningStart,
            CampaigningEnd = request.CampaigningEnd, VotingStart = request.VotingStart,
            VotingEnd = request.VotingEnd, Status = "setup", TenantId = await GetTenantIdAsync(ct)
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
        var candidates = await db.Set<ElectionCandidate>().Where(c => c.ElectionId == electionId).ToListAsync(ct);
        return candidates.Select(c => new CandidateDto(c.Id, c.StudentId, $"Student-{c.StudentId.ToString()[..8]}", c.PositionId, "Position", c.EndorsementCount, c.EligibilityVerified, c.NominationStatus)).ToList();
    }

    public async Task<VoterTokenDto> IssueVoterTokenAsync(Guid electionId, Guid studentId, CancellationToken ct = default)
    {
        var existing = await db.Set<VoterToken>().FirstOrDefaultAsync(t => t.ElectionId == electionId && t.StudentId == studentId, ct);
        if (existing is not null && existing.HasVoted) throw new InvalidOperationException("Student has already voted.");
        if (existing is not null) return new VoterTokenDto(existing.Id, existing.TokenHash, existing.HasVoted);
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{electionId}{studentId}{Guid.NewGuid()}")));
        var token = new VoterToken { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, TokenHash = tokenHash, TenantId = await GetTenantIdAsync(ct) };
        db.Set<VoterToken>().Add(token);
        await db.SaveChangesAsync(ct);
        return new VoterTokenDto(token.Id, token.TokenHash, token.HasVoted);
    }

    public async Task<BallotReceiptDto> CastBallotAsync(CastBallotRequest request, CancellationToken ct = default)
    {
        foreach (var choice in request.Choices)
        {
            var ballot = new Ballot
            {
                Id = Guid.NewGuid(), ElectionId = request.ElectionId,
                EncryptedVoteData = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{choice.PositionId}:{choice.CandidateId}")),
                BallotHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{choice.PositionId}{choice.CandidateId}{Guid.NewGuid()}"))),
                CastAt = DateTimeOffset.UtcNow, TenantId = await GetTenantIdAsync(ct)
            };
            db.Set<Ballot>().Add(ballot);
        }
        await db.SaveChangesAsync(ct);
        return new BallotReceiptDto(Guid.NewGuid().ToString("N")[..16], DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<ElectionResultDto>> ComputeResultsAsync(Guid electionId, CancellationToken ct = default)
    {
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
        var eligible = await db.Set<VoterToken>().Where(t => t.ElectionId == electionId).CountAsync(ct);
        var voted = await db.Set<VoterToken>().Where(t => t.ElectionId == electionId && t.HasVoted).CountAsync(ct);
        return new ElectionParticipationDto(electionId, eligible, voted, eligible > 0 ? Math.Round(voted * 100m / eligible, 2) : 0m);
    }

    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var first = await db.Set<Election>().Select(e => e.TenantId).FirstOrDefaultAsync(ct);
        return first != Guid.Empty ? first : Guid.NewGuid();
    }
}
