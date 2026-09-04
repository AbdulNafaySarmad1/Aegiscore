using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// GAP 14 — Student Success Platform

[Table("StudentRiskScores")] public class StudentRiskScore : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public double Score { get; set; } public RiskCategory Category { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("StudentRiskTrends")] public class StudentRiskTrend : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [Column(TypeName = "jsonb")] public string ScoresJson { get; set; } = "[]";
    public DateTime FromDate { get; set; } public DateTime ToDate { get; set; } public Guid TenantId { get; set; } }

[Table("AdvisorAssignments")] public class AdvisorAssignment : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid FacultyId { get; set; } public Guid StudentId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow; public bool IsActive { get; set; } = true; public Guid TenantId { get; set; } }

[Table("AdvisorNotes")] public class AdvisorNote : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AdvisorAssignmentId { get; set; } [MaxLength(5000)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AdvisorAssignmentId))] public AdvisorAssignment Assignment { get; set; } = null!; }

[Table("AdvisorMeetings")] public class AdvisorMeeting : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AdvisorAssignmentId { get; set; } public DateTime ScheduledAt { get; set; }
    public MeetingStatus Status { get; set; } [MaxLength(2000)] public string? Notes { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(AdvisorAssignmentId))] public AdvisorAssignment Assignment { get; set; } = null!; }

[Table("Interventions")] public class Intervention : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public InterventionType Type { get; set; }
    public InterventionOutcome Outcome { get; set; } [MaxLength(2000)] public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("CareerGoals")] public class CareerGoal : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(500)] public string Goal { get; set; } = "";
    public CareerGoalStatus Status { get; set; } public DateTime? TargetDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("SkillGaps")] public class SkillGap : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(200)] public string SkillName { get; set; } = "";
    [MaxLength(50)] public string? CurrentLevel { get; set; } [MaxLength(50)] public string? TargetLevel { get; set; }
    public Guid TenantId { get; set; } }

[Table("MentorAssignments")] public class MentorAssignment : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid MentorId { get; set; } public Guid StudentId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow; public bool IsActive { get; set; } = true; public Guid TenantId { get; set; } }

[Table("MentoringSessions")] public class MentoringSession : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid MentorAssignmentId { get; set; } public DateTime ScheduledAt { get; set; }
    public MeetingStatus Status { get; set; } [MaxLength(2000)] public string? Notes { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(MentorAssignmentId))] public MentorAssignment Assignment { get; set; } = null!; }

[Table("AlertRules")] public class AlertRule : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string RuleName { get; set; } = ""; public AlertTriggerType TriggerType { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal Threshold { get; set; } public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; } }

[Table("StudentAlerts")] public class StudentAlert : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public AlertTriggerType TriggerType { get; set; }
    [MaxLength(500)] public string? Message { get; set; } public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// GAP 15 — Career Center

[Table("Employers")] public class Employer : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(300)] public string? ContactEmail { get; set; } public EmployerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("JobPostings")] public class JobPosting : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; public Guid EmployerId { get; set; }
    public JobType JobType { get; set; } [MaxLength(200)] public string? Location { get; set; }
    [MaxLength(5000)] public string? Description { get; set; } public JobPostingStatus Status { get; set; }
    public DateTime PostedAt { get; set; } = DateTime.UtcNow; public DateTime? ExpiresAt { get; set; }
    public ICollection<JobApplication> Applications { get; set; } = []; public Guid TenantId { get; set; } }

[Table("JobApplications")] public class JobApplication : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid JobPostingId { get; set; } public Guid StudentId { get; set; }
    public ApplicationStatus Status { get; set; } [MaxLength(2000)] public string? CoverLetter { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(JobPostingId))] public JobPosting JobPosting { get; set; } = null!; }

[Table("InterviewSlots")] public class InterviewSlot : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid JobPostingId { get; set; } public Guid StudentId { get; set; }
    public DateTime ScheduledAt { get; set; } public InterviewStatus Status { get; set; }
    public InterviewType Type { get; set; } public Guid TenantId { get; set; } }

[Table("PlacementRecords")] public class PlacementRecord : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid EmployerId { get; set; }
    [MaxLength(200)] public string? Position { get; set; } public PlacementType Type { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? Salary { get; set; } public DateTime StartDate { get; set; }
    public Guid TenantId { get; set; } }

[Table("CampusDrives")] public class CampusDrive : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid EmployerId { get; set; } [MaxLength(200)] public string Name { get; set; } = "";
    public DriveType DriveType { get; set; } public DriveStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; } public Guid TenantId { get; set; }
    public ICollection<DriveRegistration> Registrations { get; set; } = []; }

[Table("DriveRegistrations")] public class DriveRegistration : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CampusDriveId { get; set; } public Guid StudentId { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(CampusDriveId))] public CampusDrive Drive { get; set; } = null!; }

[Table("StudentResumes")] public class StudentResume : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [Column(TypeName = "jsonb")] public string ResumeJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("AlumniReferrals")] public class AlumniReferral : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AlumniUserId { get; set; } public Guid StudentId { get; set; }
    public ReferralStatus Status { get; set; } [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("CareerEvents")] public class CareerEvent : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = ""; public CareerEventType EventType { get; set; }
    public CareerEventStatus Status { get; set; } public DateTime EventDate { get; set; }
    [MaxLength(200)] public string? Location { get; set; } public Guid TenantId { get; set; }
    public ICollection<CareerEventRegistration> Registrations { get; set; } = []; }

[Table("CareerEventRegistrations")] public class CareerEventRegistration : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CareerEventId { get; set; } public Guid StudentId { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(CareerEventId))] public CareerEvent Event { get; set; } = null!; }

// GAP 16 — Admissions CRM

[Table("AdmissionLeads")] public class AdmissionLead : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(300)] public string? Email { get; set; }
    [MaxLength(50)] public string? Phone { get; set; } public LeadSource Source { get; set; } public LeadStatus Status { get; set; }
    [MaxLength(200)] public string? ProgramInterest { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<LeadActivity> Activities { get; set; } = []; }

[Table("LeadActivities")] public class LeadActivity : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AdmissionLeadId { get; set; } public ActivityType ActivityType { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; } public DateTime ActivityDate { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AdmissionLeadId))] public AdmissionLead Lead { get; set; } = null!; }

[Table("LeadTasks")] public class LeadTask : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AdmissionLeadId { get; set; } [MaxLength(500)] public string Title { get; set; } = "";
    public LeadTaskStatus Status { get; set; } public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDate { get; set; } public Guid TenantId { get; set; } }

[Table("MarketingCampaigns")] public class MarketingCampaign : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public CampaignType CampaignType { get; set; }
    public CampaignStatus Status { get; set; } public DateTime StartDate { get; set; } public DateTime? EndDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Budget { get; set; } public Guid TenantId { get; set; } }

[Table("SchoolVisits")] public class SchoolVisit : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string SchoolName { get; set; } = ""; public VisitStatus Status { get; set; }
    public DateTime VisitDate { get; set; } public Guid AssignedToUserId { get; set; } public Guid TenantId { get; set; } }

[Table("CallLogs")] public class CallLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AdmissionLeadId { get; set; } public CallDirection Direction { get; set; }
    public CallDisposition Disposition { get; set; } public int DurationSeconds { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; } public DateTime CallDate { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("ChatbotConversations")] public class ChatbotConversation : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid? LeadId { get; set; } public DateTime StartedAt { get; set; } = DateTime.UtcNow; public DateTime? EndedAt { get; set; }
    public Guid TenantId { get; set; } public ICollection<ChatbotMessage> Messages { get; set; } = []; }

[Table("ChatbotMessages")] public class ChatbotMessage : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ChatbotConversationId { get; set; } public MessageSender Sender { get; set; }
    [MaxLength(5000)] public string Content { get; set; } = ""; public DateTime SentAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(ChatbotConversationId))] public ChatbotConversation ChatbotConversation { get; set; } = null!; }

// GAP 17 — Business Intelligence Platform

[Table("DataWarehouseTables")] public class DataWarehouseTable : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string TableName { get; set; } = ""; [MaxLength(100)] public string? SourceModule { get; set; }
    public ICollection<DWColumn> Columns { get; set; } = []; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("DWColumns")] public class DWColumn : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DataWarehouseTableId { get; set; } [MaxLength(200)] public string ColumnName { get; set; } = "";
    [MaxLength(100)] public string? DataType { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(DataWarehouseTableId))] public DataWarehouseTable Table { get; set; } = null!; }

[Table("ETLJobs")] public class ETLJob : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public ETLJobStatus Status { get; set; }
    public DateTime? LastRunAt { get; set; } public int RecordsProcessed { get; set; }
    [MaxLength(100)] public string? CronExpression { get; set; } public Guid TenantId { get; set; } }

[Table("Dashboards")] public class Dashboard : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public DashboardType DashboardType { get; set; }
    public Guid? CampusId { get; set; } public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<DashboardWidget> Widgets { get; set; } = []; public ICollection<DashboardSubscription> Subscriptions { get; set; } = []; }

[Table("DashboardWidgets")] public class DashboardWidget : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DashboardId { get; set; } public VisualizationType VisualizationType { get; set; }
    [MaxLength(300)] public string? Title { get; set; } [Column(TypeName = "jsonb")] public string ConfigJson { get; set; } = "{}";
    public int SortOrder { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(DashboardId))] public Dashboard Dashboard { get; set; } = null!; }

[Table("BIReports")] public class BIReport : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; public ReportCategory ReportCategory { get; set; }
    public Guid DataWarehouseTableId { get; set; } [Column(TypeName = "jsonb")] public string QueryJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(DataWarehouseTableId))] public DataWarehouseTable DataWarehouseTable { get; set; } = null!; }

[Table("ReportSchedules")] public class ReportSchedule : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid BIReportId { get; set; } public ScheduleFrequency Frequency { get; set; }
    public ExportFormat Format { get; set; } [MaxLength(500)] public string? Recipients { get; set; }
    public bool IsActive { get; set; } = true; public Guid TenantId { get; set; } }

[Table("DashboardSubscriptions")] public class DashboardSubscription : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DashboardId { get; set; } public Guid UserId { get; set; }
    public RefreshFrequency RefreshFrequency { get; set; } public bool IsActive { get; set; } = true; public Guid TenantId { get; set; }
    [ForeignKey(nameof(DashboardId))] public Dashboard Dashboard { get; set; } = null!; }

[Table("KPIAlerts")] public class KPIAlert : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DashboardWidgetId { get; set; } [MaxLength(200)] public string AlertName { get; set; } = "";
    public AlertCondition AlertCondition { get; set; } [Column(TypeName = "decimal(18,4)")] public decimal Threshold { get; set; }
    public bool IsActive { get; set; } = true; public Guid TenantId { get; set; }
    [ForeignKey(nameof(DashboardWidgetId))] public DashboardWidget DashboardWidget { get; set; } = null!; }

[Table("DataExportLogs")] public class DataExportLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid? BIReportId { get; set; } public Guid? DashboardId { get; set; }
    public ExportFormat Format { get; set; } public int RowCount { get; set; }
    public Guid ExportedByUserId { get; set; } public DateTime ExportedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(BIReportId))] public BIReport? BIReport { get; set; }
    [ForeignKey(nameof(DashboardId))] public Dashboard? Dashboard { get; set; } }
