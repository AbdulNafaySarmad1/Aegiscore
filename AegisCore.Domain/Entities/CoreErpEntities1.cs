using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// GAP 03 — Research Management

[Table("ResearchProjects")] public class ResearchProject : ITenantScoped { [Key] public Guid Id { get; set; }
    [Required][MaxLength(300)] public string Title { get; set; } = ""; [MaxLength(5000)] public string? Description { get; set; }
    public Guid PrincipalInvestigatorId { get; set; } public ResearchProjectStatus Status { get; set; }
    public DateTime StartDate { get; set; } public DateTime? EndDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Budget { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<ResearchProjectMember> Members { get; set; } = []; public ICollection<ResearchMilestone> Milestones { get; set; } = []; public ICollection<ResearchDeliverable> Deliverables { get; set; } = []; }

[Table("ResearchProjectMembers")] public class ResearchProjectMember : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ResearchProjectId { get; set; } public Guid FacultyId { get; set; } public ResearchMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(ResearchProjectId))] public ResearchProject Project { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("ResearchMilestones")] public class ResearchMilestone : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ResearchProjectId { get; set; } [MaxLength(300)] public string Title { get; set; } = "";
    public MilestoneStatus Status { get; set; } public DateTime DueDate { get; set; } public DateTime? CompletedAt { get; set; }
    [ForeignKey(nameof(ResearchProjectId))] public ResearchProject Project { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("ResearchDeliverables")] public class ResearchDeliverable : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ResearchProjectId { get; set; } [MaxLength(300)] public string Title { get; set; } = "";
    public DeliverableType Type { get; set; } public DeliverableStatus Status { get; set; } public DateTime? SubmittedAt { get; set; }
    [ForeignKey(nameof(ResearchProjectId))] public ResearchProject Project { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("ResearchGrants")] public class ResearchGrant : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; [MaxLength(200)] public string? FundingAgency { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; } public GrantStatus Status { get; set; }
    public DateTime ApplicationDate { get; set; } public DateTime? AwardDate { get; set; } public DateTime? ExpiryDate { get; set; }
    public ICollection<GrantExpenditure> Expenditures { get; set; } = []; public Guid TenantId { get; set; } }

[Table("GrantExpenditures")] public class GrantExpenditure : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ResearchGrantId { get; set; } [MaxLength(500)] public string Description { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; } public DateTime ExpenditureDate { get; set; }
    [ForeignKey(nameof(ResearchGrantId))] public ResearchGrant Grant { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("ResearchPublications")] public class ResearchPublication : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(500)] public string Title { get; set; } = ""; public PublicationType Type { get; set; }
    [MaxLength(200)] public string? JournalName { get; set; } public DateTime? PublishedDate { get; set; }
    [MaxLength(200)] public string? Doi { get; set; } public ICollection<PublicationAuthor> Authors { get; set; } = []; public Guid TenantId { get; set; } }

[Table("PublicationAuthors")] public class PublicationAuthor : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ResearchPublicationId { get; set; } public Guid FacultyId { get; set; } public int AuthorOrder { get; set; }
    [ForeignKey(nameof(ResearchPublicationId))] public ResearchPublication Publication { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("ResearchPatents")] public class ResearchPatent : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; [MaxLength(100)] public string? PatentNumber { get; set; }
    public PatentStatus Status { get; set; } public DateTime? FiledDate { get; set; } public DateTime? GrantedDate { get; set; }
    public ICollection<PatentInventor> Inventors { get; set; } = []; public Guid TenantId { get; set; } }

[Table("PatentInventors")] public class PatentInventor : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ResearchPatentId { get; set; } public Guid FacultyId { get; set; }
    [ForeignKey(nameof(ResearchPatentId))] public ResearchPatent Patent { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("EthicsProtocols")] public class EthicsProtocol : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; public EthicsReviewStatus Status { get; set; }
    public Guid SubmittedByUserId { get; set; } public DateTime SubmittedAt { get; set; } public DateTime? ReviewedAt { get; set; }
    [MaxLength(200)] public string? ReviewComments { get; set; } public Guid TenantId { get; set; } }

[Table("ConferenceTravels")] public class ConferenceTravel : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string ConferenceName { get; set; } = ""; [MaxLength(200)] public string? Location { get; set; }
    public TravelStatus Status { get; set; } public DateTime TravelDateFrom { get; set; } public DateTime TravelDateTo { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ApprovedBudget { get; set; } public Guid FacultyId { get; set; } public Guid TenantId { get; set; } }

// GAP 04 — Outcome Based Education (OBE)

[Table("UniversityMissions")] public class UniversityMission : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(2000)] public string Statement { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("ProgramEducationalObjectives")] public class ProgramEducationalObjective : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(1000)] public string Description { get; set; } = ""; public Guid ProgramId { get; set; }
    public int SortOrder { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("ProgramOutcomes")] public class ProgramOutcome : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string Code { get; set; } = ""; [MaxLength(1000)] public string Description { get; set; } = "";
    public Guid ProgramId { get; set; } public int SortOrder { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("CourseLearningOutcomes")] public class CourseLearningOutcome : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string Code { get; set; } = ""; [MaxLength(1000)] public string Description { get; set; } = "";
    public Guid CourseId { get; set; } public BloomsLevel BloomsLevel { get; set; } public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("CLOPOMappings")] public class CLOPOMapping : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CourseLearningOutcomeId { get; set; } public Guid ProgramOutcomeId { get; set; }
    public MappingStrength Strength { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(CourseLearningOutcomeId))] public CourseLearningOutcome CLO { get; set; } = null!;
    [ForeignKey(nameof(ProgramOutcomeId))] public ProgramOutcome PO { get; set; } = null!; }

[Table("Rubrics")] public class Rubric : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; public Guid CourseLearningOutcomeId { get; set; }
    public ICollection<RubricCriterion> Criteria { get; set; } = []; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("RubricCriteria")] public class RubricCriterion : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid RubricId { get; set; } [MaxLength(500)] public string Description { get; set; } = "";
    public int MaxScore { get; set; } public int SortOrder { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(RubricId))] public Rubric Rubric { get; set; } = null!; }

[Table("CLOAssessmentMappings")] public class CLOAssessmentMapping : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CourseLearningOutcomeId { get; set; } public Guid AssessmentId { get; set; }
    public AssessmentType AssessmentType { get; set; } public decimal Weightage { get; set; } public Guid TenantId { get; set; } }

[Table("CLOAttainments")] public class CLOAttainment : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CourseLearningOutcomeId { get; set; } public Guid StudentId { get; set; } public Guid CourseId { get; set; }
    public decimal AttainmentLevel { get; set; } [MaxLength(50)] public string? Semester { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("POAttainments")] public class POAttainment : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ProgramOutcomeId { get; set; } public Guid StudentId { get; set; }
    public decimal AttainmentLevel { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("AttainmentThresholds")] public class AttainmentThreshold : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ProgramOutcomeId { get; set; } public decimal Threshold { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("GapAnalyses")] public class GapAnalysis : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ProgramOutcomeId { get; set; } [MaxLength(2000)] public string? Gap { get; set; }
    [MaxLength(2000)] public string? Action { get; set; } public CIPStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("ContinuousImprovementPlans")] public class ContinuousImprovementPlan : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(500)] public string Title { get; set; } = ""; public CIPStatus Status { get; set; }
    public DateTime TargetDate { get; set; } public DateTime? CompletedAt { get; set; }
    public Guid TenantId { get; set; } }

[Table("AccreditationReports")] public class AccreditationReport : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(500)] public string Title { get; set; } = ""; [MaxLength(100)] public string? AccreditationBody { get; set; }
    public DateTime SubmittedAt { get; set; } public Guid TenantId { get; set; } }

// GAP 06 — Examination Office

[Table("QuestionBanks")] public class QuestionBank { [Key] public Guid Id { get; set; }
    [MaxLength(2000)] public string QuestionText { get; set; } = ""; [MaxLength(50)] public string? QuestionType { get; set; }
    public Guid CourseId { get; set; } public BloomsLevel BloomsLevel { get; set; }
    [MaxLength(100)] public string? Difficulty { get; set; } public bool IsApproved { get; set; }
    public Guid CreatedByUserId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("ExamPapers")] public class ExamPaper { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; public Guid CourseId { get; set; }
    public ExamType ExamType { get; set; } public int TotalMarks { get; set; } public int DurationMinutes { get; set; }
    [Column(TypeName = "jsonb")] public string QuestionsJson { get; set; } = "[]";
    public bool IsApproved { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }
