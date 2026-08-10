using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("Elections")]
public class Election : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(50)] public string ElectionType { get; set; } = "student_union";
    [MaxLength(20)] public string VotingMethod { get; set; } = "online";
    public DateTimeOffset NominationStart { get; set; }
    public DateTimeOffset NominationEnd { get; set; }
    public DateTimeOffset CampaigningStart { get; set; }
    public DateTimeOffset CampaigningEnd { get; set; }
    public DateTimeOffset VotingStart { get; set; }
    public DateTimeOffset VotingEnd { get; set; }
    public DateTimeOffset? ResultsPublishedAt { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "setup";
    [Column(TypeName = "decimal(5,2)")] public decimal? TurnoutRate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<ElectionPosition> Positions { get; set; } = [];
}

[Table("ElectionPositions")]
public class ElectionPosition : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    [Required, MaxLength(200)] public string PositionName { get; set; } = string.Empty;
    public int Seats { get; set; } = 1;
    [MaxLength(100)] public string? TermDuration { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(ElectionId))] public Election Election { get; set; } = null!;
}

[Table("ElectionCandidates")]
public class ElectionCandidate : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public Guid PositionId { get; set; }
    public Guid StudentId { get; set; }
    public int EndorsementCount { get; set; }
    public bool EligibilityVerified { get; set; }
    [MaxLength(20)] public string NominationStatus { get; set; } = "pending";
    [MaxLength(500)] public string? CandidateStatement { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("VoterTokens")]
public class VoterToken : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public Guid StudentId { get; set; }
    [Required, MaxLength(200)] public string TokenHash { get; set; } = string.Empty;
    public bool HasVoted { get; set; }
    public DateTimeOffset? VotedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("Ballots")]
public class Ballot : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    [Required, MaxLength(200)] public string EncryptedVoteData { get; set; } = string.Empty;
    [MaxLength(200)] public string? BallotHash { get; set; }
    public DateTimeOffset CastAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("ElectionResults")]
public class ElectionResult : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ElectionId { get; set; }
    public Guid PositionId { get; set; }
    public Guid CandidateId { get; set; }
    public int VoteCount { get; set; }
    public bool IsElected { get; set; }
    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
