using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AegisCore.Domain;
using AegisCore.Domain.Common;
using AegisCore.Domain.Entities;
using AccreditationBodyEntity = AegisCore.Domain.Entities.AccreditationBody;

namespace AegisCore.Infrastructure;

/// <summary>
/// Entity Framework Core database context for AegisCore.
/// </summary>
public class AegisDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    public Guid CurrentTenantId => _httpContextAccessor?.HttpContext?.Items["TenantId"] as Guid? ?? Guid.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="AegisDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the context.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for tenant resolution.</param>
    public AegisDbContext(DbContextOptions<AegisDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Departments.
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>
    /// Batches.
    /// </summary>
    public DbSet<Batch> Batches => Set<Batch>();

    /// <summary>
    /// Students.
    /// </summary>
    public DbSet<Student> Students => Set<Student>();

    /// <summary>
    /// Faculties.
    /// </summary>
    public DbSet<Faculty> Faculties => Set<Faculty>();

    /// <summary>
    /// Courses.
    /// </summary>
    public DbSet<Course> Courses => Set<Course>();

    /// <summary>
    /// Course assignments.
    /// </summary>
    public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();

    /// <summary>
    /// Enrollments.
    /// </summary>
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    /// <summary>
    /// Attendances.
    /// </summary>
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<CampusPolygon> CampusPolygons => Set<CampusPolygon>();
    public DbSet<FerpaAuditLog> FerpaAuditLogs => Set<FerpaAuditLog>();
    public DbSet<ConsentLedger> ConsentLedgers => Set<ConsentLedger>();
    public DbSet<ErasureRequest> ErasureRequests => Set<ErasureRequest>();

    /// <summary>
    /// Exam schedules.
    /// </summary>
    public DbSet<ExamSchedule> ExamSchedules => Set<ExamSchedule>();

    /// <summary>
    /// Exam seatings.
    /// </summary>
    public DbSet<ExamSeating> ExamSeatings => Set<ExamSeating>();

    /// <summary>
    /// Results.
    /// </summary>
    public DbSet<Result> Results => Set<Result>();

    /// <summary>
    /// Timetables.
    /// </summary>
    public DbSet<Timetable> Timetables => Set<Timetable>();

    /// <summary>
    /// CLO-PLO mappings.
    /// </summary>
    public DbSet<CloPlomapping> CloPlomappings => Set<CloPlomapping>();

    /// <summary>
    /// Scaling events.
    /// </summary>
    public DbSet<ScalingEvent> ScalingEvents => Set<ScalingEvent>();

    // General Ledger
    /// <summary>
    /// Chart of accounts.
    /// </summary>
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();

    /// <summary>
    /// Journal entries.
    /// </summary>
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    /// <summary>
    /// Journal entry lines.
    /// </summary>
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    /// <summary>
    /// Fiscal periods.
    /// </summary>
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();

    /// <summary>
    /// Fiscal years.
    /// </summary>
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();

    // HR ERP
    /// <summary>
    /// Employees.
    /// </summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>
    /// Employee documents.
    /// </summary>
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    /// <summary>
    /// Employee contracts.
    /// </summary>
    public DbSet<EmployeeContract> EmployeeContracts => Set<EmployeeContract>();

    /// <summary>
    /// Job postings.
    /// </summary>
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();

    /// <summary>
    /// Job applications.
    /// </summary>
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    /// <summary>
    /// Interview schedules.
    /// </summary>
    public DbSet<InterviewSchedule> InterviewSchedules => Set<InterviewSchedule>();

    /// <summary>
    /// Leave applications.
    /// </summary>
    public DbSet<LeaveApplication> LeaveApplications => Set<LeaveApplication>();

    /// <summary>
    /// Attendance records.
    /// </summary>
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    /// <summary>
    /// Performance reviews.
    /// </summary>
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();

    /// <summary>
    /// Training assignments.
    /// </summary>
    public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();

    // Research Management
    /// <summary>
    /// Research projects.
    /// </summary>
    public DbSet<ResearchProject> ResearchProjects => Set<ResearchProject>();

    /// <summary>
    /// Research project members.
    /// </summary>
    public DbSet<ResearchProjectMember> ResearchProjectMembers => Set<ResearchProjectMember>();

    /// <summary>
    /// Research milestones.
    /// </summary>
    public DbSet<ResearchMilestone> ResearchMilestones => Set<ResearchMilestone>();

    /// <summary>
    /// Research deliverables.
    /// </summary>
    public DbSet<ResearchDeliverable> ResearchDeliverables => Set<ResearchDeliverable>();

    /// <summary>
    /// Research grants.
    /// </summary>
    public DbSet<ResearchGrant> ResearchGrants => Set<ResearchGrant>();

    /// <summary>
    /// Grant expenditures.
    /// </summary>
    public DbSet<GrantExpenditure> GrantExpenditures => Set<GrantExpenditure>();

    /// <summary>
    /// Research publications.
    /// </summary>
    public DbSet<ResearchPublication> ResearchPublications => Set<ResearchPublication>();

    /// <summary>
    /// Publication authors.
    /// </summary>
    public DbSet<PublicationAuthor> PublicationAuthors => Set<PublicationAuthor>();

    /// <summary>
    /// Research patents.
    /// </summary>
    public DbSet<ResearchPatent> ResearchPatents => Set<ResearchPatent>();

    /// <summary>
    /// Patent inventors.
    /// </summary>
    public DbSet<PatentInventor> PatentInventors => Set<PatentInventor>();

    /// <summary>
    /// Ethics protocols.
    /// </summary>
    public DbSet<EthicsProtocol> EthicsProtocols => Set<EthicsProtocol>();

    /// <summary>
    /// Conference travels.
    /// </summary>
    public DbSet<ConferenceTravel> ConferenceTravels => Set<ConferenceTravel>();

    // Outcome Based Education (OBE)
    /// <summary>
    /// University missions.
    /// </summary>
    public DbSet<UniversityMission> UniversityMissions => Set<UniversityMission>();

    /// <summary>
    /// Program educational objectives.
    /// </summary>
    public DbSet<ProgramEducationalObjective> ProgramEducationalObjectives => Set<ProgramEducationalObjective>();

    /// <summary>
    /// Program outcomes.
    /// </summary>
    public DbSet<ProgramOutcome> ProgramOutcomes => Set<ProgramOutcome>();

    /// <summary>
    /// Course learning outcomes.
    /// </summary>
    public DbSet<CourseLearningOutcome> CourseLearningOutcomes => Set<CourseLearningOutcome>();

    /// <summary>
    /// CLO-PO mappings.
    /// </summary>
    public DbSet<CLOPOMapping> CLOPOMappings => Set<CLOPOMapping>();

    /// <summary>
    /// Rubrics.
    /// </summary>
    public DbSet<Rubric> Rubrics => Set<Rubric>();

    /// <summary>
    /// Rubric criteria.
    /// </summary>
    public DbSet<RubricCriterion> RubricCriteria => Set<RubricCriterion>();

    /// <summary>
    /// CLO assessment mappings.
    /// </summary>
    public DbSet<CLOAssessmentMapping> CLOAssessmentMappings => Set<CLOAssessmentMapping>();

    /// <summary>
    /// CLO attainments.
    /// </summary>
    public DbSet<CLOAttainment> CLOAttainments => Set<CLOAttainment>();

    /// <summary>
    /// PO attainments.
    /// </summary>
    public DbSet<POAttainment> POAttainments => Set<POAttainment>();

    /// <summary>
    /// Attainment thresholds.
    /// </summary>
    public DbSet<AttainmentThreshold> AttainmentThresholds => Set<AttainmentThreshold>();

    /// <summary>
    /// Gap analyses.
    /// </summary>
    public DbSet<GapAnalysis> GapAnalyses => Set<GapAnalysis>();

    /// <summary>
    /// Continuous improvement plans.
    /// </summary>
    public DbSet<ContinuousImprovementPlan> ContinuousImprovementPlans => Set<ContinuousImprovementPlan>();

    /// <summary>
    /// Accreditation reports.
    /// </summary>
    public DbSet<AccreditationReport> AccreditationReports => Set<AccreditationReport>();

    // Accreditation Suite
    /// <summary>
    /// Accreditation bodies.
    /// </summary>
    public DbSet<AccreditationBodyEntity> AccreditationBodies => Set<AccreditationBodyEntity>();

    /// <summary>
    /// Accreditation criteria.
    /// </summary>
    public DbSet<AccreditationCriterion> AccreditationCriteria => Set<AccreditationCriterion>();

    /// <summary>
    /// Accreditation cycles.
    /// </summary>
    public DbSet<AccreditationCycle> AccreditationCycles => Set<AccreditationCycle>();

    /// <summary>
    /// Accreditation evidences.
    /// </summary>
    public DbSet<AccreditationEvidence> AccreditationEvidences => Set<AccreditationEvidence>();

    /// <summary>
    /// Evidence versions.
    /// </summary>
    public DbSet<EvidenceVersion> EvidenceVersions => Set<EvidenceVersion>();

    /// <summary>
    /// Accreditation criterion mappings.
    /// </summary>
    public DbSet<AccreditationCriterionMapping> AccreditationCriterionMappings => Set<AccreditationCriterionMapping>();

    /// <summary>
    /// Faculty CVs.
    /// </summary>
    public DbSet<FacultyCV> FacultyCVs => Set<FacultyCV>();

    /// <summary>
    /// Lab inventories.
    /// </summary>
    public DbSet<LabInventory> LabInventories => Set<LabInventory>();

    /// <summary>
    /// Self-study reports.
    /// </summary>
    public DbSet<SelfStudyReport> SelfStudyReports => Set<SelfStudyReport>();

    /// <summary>
    /// SSR sections.
    /// </summary>
    public DbSet<SSRSection> SSRSections => Set<SSRSection>();

    /// <summary>
    /// SSR reviews.
    /// </summary>
    public DbSet<SSRReview> SSRReviews => Set<SSRReview>();

    /// <summary>
    /// Accreditation committees.
    /// </summary>
    public DbSet<AccreditationCommittee> AccreditationCommittees => Set<AccreditationCommittee>();

    /// <summary>
    /// Committee members.
    /// </summary>
    public DbSet<CommitteeMember> CommitteeMembers => Set<CommitteeMember>();

    /// <summary>
    /// Committee meetings.
    /// </summary>
    public DbSet<CommitteeMeeting> CommitteeMeetings => Set<CommitteeMeeting>();

    /// <summary>
    /// Action items.
    /// </summary>
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();

    /// <summary>
    /// Accreditation audit logs.
    /// </summary>
    public DbSet<AccreditationAuditLog> AccreditationAuditLogs => Set<AccreditationAuditLog>();

    /// <summary>
    /// Previous visit findings.
    /// </summary>
    public DbSet<PreviousVisitFinding> PreviousVisitFindings => Set<PreviousVisitFinding>();

    // Student Information System (SIS)
    /// <summary>
    /// Student profiles.
    /// </summary>
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    /// <summary>
    /// Student documents.
    /// </summary>
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();

    /// <summary>
    /// Student fees.
    /// </summary>
    public DbSet<StudentFee> StudentFees => Set<StudentFee>();

    /// <summary>
    /// Fee payments.
    /// </summary>
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();

    /// <summary>
    /// Student disciplines.
    /// </summary>
    public DbSet<StudentDiscipline> StudentDisciplines => Set<StudentDiscipline>();

    /// <summary>
    /// Student counselings.
    /// </summary>
    public DbSet<StudentCounseling> StudentCounselings => Set<StudentCounseling>();

    /// <summary>
    /// Student achievements.
    /// </summary>
    public DbSet<StudentAchievement> StudentAchievements => Set<StudentAchievement>();

    /// <summary>
    /// Student scholarships.
    /// </summary>
    public DbSet<StudentScholarship> StudentScholarships => Set<StudentScholarship>();

    /// <summary>
    /// Student hostels.
    /// </summary>
    public DbSet<StudentHostel> StudentHostels => Set<StudentHostel>();

    /// <summary>
    /// Student transports.
    /// </summary>
    public DbSet<StudentTransport> StudentTransports => Set<StudentTransport>();

    /// <summary>
    /// Student guardians.
    /// </summary>
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();

    /// <summary>
    /// Student transfers.
    /// </summary>
    public DbSet<StudentTransfer> StudentTransfers => Set<StudentTransfer>();

    /// <summary>
    /// Student clearances.
    /// </summary>
    public DbSet<StudentClearance> StudentClearances => Set<StudentClearance>();

    // Library Management System
    /// <summary>
    /// Library books.
    /// </summary>
    public DbSet<LibraryBook> LibraryBooks => Set<LibraryBook>();

    /// <summary>
    /// Book copies.
    /// </summary>
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    /// <summary>
    /// Library members.
    /// </summary>
    public DbSet<LibraryMember> LibraryMembers => Set<LibraryMember>();

    /// <summary>
    /// Book issues.
    /// </summary>
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();

    /// <summary>
    /// Book reservations.
    /// </summary>
    public DbSet<BookReservation> BookReservations => Set<BookReservation>();

    /// <summary>
    /// Fine payments.
    /// </summary>
    public DbSet<FinePayment> FinePayments => Set<FinePayment>();

    /// <summary>
    /// Library vendors.
    /// </summary>
    public DbSet<LibraryVendor> LibraryVendors => Set<LibraryVendor>();

    /// <summary>
    /// Book orders.
    /// </summary>
    public DbSet<BookOrder> BookOrders => Set<BookOrder>();

    /// <summary>
    /// Order items.
    /// </summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>
    /// Library budgets.
    /// </summary>
    public DbSet<LibraryBudget> LibraryBudgets => Set<LibraryBudget>();

    /// <summary>
    /// Library usages.
    /// </summary>
    public DbSet<LibraryUsage> LibraryUsages => Set<LibraryUsage>();

    // Research Labs Management
    /// <summary>
    /// Research labs.
    /// </summary>
    public DbSet<ResearchLab> ResearchLabs => Set<ResearchLab>();

    /// <summary>
    /// Lab equipments.
    /// </summary>
    public DbSet<LabEquipment> LabEquipments => Set<LabEquipment>();

    /// <summary>
    /// Lab bookings.
    /// </summary>
    public DbSet<LabBooking> LabBookings => Set<LabBooking>();

    /// <summary>
    /// Equipment bookings.
    /// </summary>
    public DbSet<EquipmentBooking> EquipmentBookings => Set<EquipmentBooking>();

    /// <summary>
    /// Equipment usage logs.
    /// </summary>
    public DbSet<EquipmentUsageLog> EquipmentUsageLogs => Set<EquipmentUsageLog>();

    /// <summary>
    /// Lab consumables.
    /// </summary>
    public DbSet<LabConsumable> LabConsumables => Set<LabConsumable>();

    /// <summary>
    /// Consumable requisitions.
    /// </summary>
    public DbSet<ConsumableRequisition> ConsumableRequisitions => Set<ConsumableRequisition>();

    /// <summary>
    /// Calibration records.
    /// </summary>
    public DbSet<CalibrationRecord> CalibrationRecords => Set<CalibrationRecord>();

    /// <summary>
    /// Lab safety inspections.
    /// </summary>
    public DbSet<LabSafetyInspection> LabSafetyInspections => Set<LabSafetyInspection>();

    /// <summary>
    /// Safety checklist items.
    /// </summary>
    public DbSet<SafetyChecklistItem> SafetyChecklistItems => Set<SafetyChecklistItem>();

    /// <summary>
    /// Equipment maintenances.
    /// </summary>
    public DbSet<EquipmentMaintenance> EquipmentMaintenances => Set<EquipmentMaintenance>();

    // Campus Asset ERP
    /// <summary>
    /// Campuses.
    /// </summary>
    public DbSet<CampusEntity> Campuses => Set<CampusEntity>();

    /// <summary>
    /// Buildings.
    /// </summary>
    public DbSet<Building> Buildings => Set<Building>();

    /// <summary>
    /// Floors.
    /// </summary>
    public DbSet<Floor> Floors => Set<Floor>();

    /// <summary>
    /// Rooms.
    /// </summary>
    public DbSet<Room> Rooms => Set<Room>();

    /// <summary>
    /// Asset categories.
    /// </summary>
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();

    /// <summary>
    /// Assets.
    /// </summary>
    public DbSet<Asset> Assets => Set<Asset>();

    /// <summary>
    /// Asset transfers.
    /// </summary>
    public DbSet<AssetTransfer> AssetTransfers => Set<AssetTransfer>();

    /// <summary>
    /// Asset maintenances.
    /// </summary>
    public DbSet<AssetMaintenance> AssetMaintenances => Set<AssetMaintenance>();

    /// <summary>
    /// Asset disposals.
    /// </summary>
    public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();

    /// <summary>
    /// Asset verifications.
    /// </summary>
    public DbSet<AssetVerification> AssetVerifications => Set<AssetVerification>();

    /// <summary>
    /// Verification cycles.
    /// </summary>
    public DbSet<VerificationCycle> VerificationCycles => Set<VerificationCycle>();

    // Maintenance ERP
    /// <summary>
    /// Maintenance tickets.
    /// </summary>
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();

    /// <summary>
    /// Ticket comments.
    /// </summary>
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    /// <summary>
    /// Ticket escalations.
    /// </summary>
    public DbSet<TicketEscalation> TicketEscalations => Set<TicketEscalation>();

    /// <summary>
    /// Work orders.
    /// </summary>
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    /// <summary>
    /// Work order materials.
    /// </summary>
    public DbSet<WorkOrderMaterial> WorkOrderMaterials => Set<WorkOrderMaterial>();

    /// <summary>
    /// Work order time entries.
    /// </summary>
    public DbSet<WorkOrderTimeEntry> WorkOrderTimeEntries => Set<WorkOrderTimeEntry>();

    /// <summary>
    /// Preventive maintenance schedules.
    /// </summary>
    public DbSet<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules => Set<PreventiveMaintenanceSchedule>();

    /// <summary>
    /// Maintenance contractors.
    /// </summary>
    public DbSet<MaintenanceContractor> MaintenanceContractors => Set<MaintenanceContractor>();

    /// <summary>
    /// SLA definitions.
    /// </summary>
    public DbSet<SLADefinition> SLADefinitions => Set<SLADefinition>();

    // Campus Security Management
    /// <summary>
    /// Visitors.
    /// </summary>
    public DbSet<Visitor> Visitors => Set<Visitor>();

    /// <summary>
    /// Vehicle passes.
    /// </summary>
    public DbSet<VehiclePass> VehiclePasses => Set<VehiclePass>();

    /// <summary>
    /// Gate entries.
    /// </summary>
    public DbSet<GateEntry> GateEntries => Set<GateEntry>();

    /// <summary>
    /// Security guards.
    /// </summary>
    public DbSet<SecurityGuard> SecurityGuards => Set<SecurityGuard>();

    /// <summary>
    /// Guard shifts.
    /// </summary>
    public DbSet<GuardShift> GuardShifts => Set<GuardShift>();

    /// <summary>
    /// Guard rounds.
    /// </summary>
    public DbSet<GuardRound> GuardRounds => Set<GuardRound>();

    /// <summary>
    /// Checkpoint scans.
    /// </summary>
    public DbSet<CheckpointScan> CheckpointScans => Set<CheckpointScan>();

    /// <summary>
    /// Security incidents.
    /// </summary>
    public DbSet<SecurityIncident> SecurityIncidents => Set<SecurityIncident>();

    /// <summary>
    /// Incident witnesses.
    /// </summary>
    public DbSet<IncidentWitness> IncidentWitnesses => Set<IncidentWitness>();

    /// <summary>
    /// Incident evidences.
    /// </summary>
    public DbSet<IncidentEvidence> IncidentEvidences => Set<IncidentEvidence>();

    /// <summary>
    /// Emergency contacts.
    /// </summary>
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();

    /// <summary>
    /// Emergency alerts.
    /// </summary>
    public DbSet<EmergencyAlert> EmergencyAlerts => Set<EmergencyAlert>();

    /// <summary>
    /// Fire equipments.
    /// </summary>
    public DbSet<FireEquipment> FireEquipments => Set<FireEquipment>();

    /// <summary>
    /// Fire drills.
    /// </summary>
    public DbSet<FireDrill> FireDrills => Set<FireDrill>();

    /// <summary>
    /// Lost and found items.
    /// </summary>
    public DbSet<LostFoundItem> LostFoundItems => Set<LostFoundItem>();

    // Transportation Management
    /// <summary>
    /// Transport routes.
    /// </summary>
    public DbSet<TransportRoute> TransportRoutes => Set<TransportRoute>();

    /// <summary>
    /// Route stops.
    /// </summary>
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();

    /// <summary>
    /// Transport vehicles.
    /// </summary>
    public DbSet<TransportVehicle> TransportVehicles => Set<TransportVehicle>();

    /// <summary>
    /// Transport drivers.
    /// </summary>
    public DbSet<TransportDriver> TransportDrivers => Set<TransportDriver>();

    /// <summary>
    /// Driver shifts.
    /// </summary>
    public DbSet<DriverShift> DriverShifts => Set<DriverShift>();

    /// <summary>
    /// Driver incidents.
    /// </summary>
    public DbSet<DriverIncident> DriverIncidents => Set<DriverIncident>();

    /// <summary>
    /// Bus attendances.
    /// </summary>
    public DbSet<BusAttendance> BusAttendances => Set<BusAttendance>();

    /// <summary>
    /// Fuel logs.
    /// </summary>
    public DbSet<FuelLog> FuelLogs => Set<FuelLog>();

    /// <summary>
    /// GPS locations.
    /// </summary>
    public DbSet<GPSLocation> GPSLocations => Set<GPSLocation>();

    /// <summary>
    /// Vehicle maintenances.
    /// </summary>
    public DbSet<VehicleMaintenance> VehicleMaintenances => Set<VehicleMaintenance>();

    // Hostel ERP Expansion
    /// <summary>
    /// Hostel blocks.
    /// </summary>
    public DbSet<HostelBlock> HostelBlocks => Set<HostelBlock>();

    /// <summary>
    /// Hostel floors.
    /// </summary>
    public DbSet<HostelFloor> HostelFloors => Set<HostelFloor>();

    /// <summary>
    /// Hostel rooms.
    /// </summary>
    public DbSet<HostelRoom> HostelRooms => Set<HostelRoom>();

    /// <summary>
    /// Hostel allocations.
    /// </summary>
    public DbSet<HostelAllocation> HostelAllocations => Set<HostelAllocation>();

    /// <summary>
    /// Room inspections.
    /// </summary>
    public DbSet<RoomInspection> RoomInspections => Set<RoomInspection>();

    /// <summary>
    /// Inspection checklist items.
    /// </summary>
    public DbSet<InspectionChecklistItem> InspectionChecklistItems => Set<InspectionChecklistItem>();

    /// <summary>
    /// Gate logs.
    /// </summary>
    public DbSet<GateLog> GateLogs => Set<GateLog>();

    /// <summary>
    /// Visitor logs.
    /// </summary>
    public DbSet<VisitorLog> VisitorLogs => Set<VisitorLog>();

    /// <summary>
    /// Hostel disciplines.
    /// </summary>
    public DbSet<HostelDiscipline> HostelDisciplines => Set<HostelDiscipline>();

    /// <summary>
    /// Utility readings.
    /// </summary>
    public DbSet<UtilityReading> UtilityReadings => Set<UtilityReading>();

    /// <summary>
    /// Mess menus.
    /// </summary>
    public DbSet<MessMenu> MessMenus => Set<MessMenu>();

    /// <summary>
    /// Mess attendances.
    /// </summary>
    public DbSet<MessAttendance> MessAttendances => Set<MessAttendance>();

    /// <summary>
    /// Mess feedbacks.
    /// </summary>
    public DbSet<MessFeedback> MessFeedbacks => Set<MessFeedback>();

    /// <summary>
    /// Roommate preferences.
    /// </summary>
    public DbSet<RoommatePreference> RoommatePreferences => Set<RoommatePreference>();

    // Student Success Platform
    /// <summary>
    /// Student risk scores.
    /// </summary>
    public DbSet<StudentRiskScore> StudentRiskScores => Set<StudentRiskScore>();

    /// <summary>
    /// Student risk trends.
    /// </summary>
    public DbSet<StudentRiskTrend> StudentRiskTrends => Set<StudentRiskTrend>();

    /// <summary>
    /// Advisor assignments.
    /// </summary>
    public DbSet<AdvisorAssignment> AdvisorAssignments => Set<AdvisorAssignment>();

    /// <summary>
    /// Advisor notes.
    /// </summary>
    public DbSet<AdvisorNote> AdvisorNotes => Set<AdvisorNote>();

    /// <summary>
    /// Advisor meetings.
    /// </summary>
    public DbSet<AdvisorMeeting> AdvisorMeetings => Set<AdvisorMeeting>();

    /// <summary>
    /// Interventions.
    /// </summary>
    public DbSet<Intervention> Interventions => Set<Intervention>();

    /// <summary>
    /// Career goals.
    /// </summary>
    public DbSet<CareerGoal> CareerGoals => Set<CareerGoal>();

    /// <summary>
    /// Skill gaps.
    /// </summary>
    public DbSet<SkillGap> SkillGaps => Set<SkillGap>();

    /// <summary>
    /// Mentor assignments.
    /// </summary>
    public DbSet<MentorAssignment> MentorAssignments => Set<MentorAssignment>();

    /// <summary>
    /// Mentoring sessions.
    /// </summary>
    public DbSet<MentoringSession> MentoringSessions => Set<MentoringSession>();

    /// <summary>
    /// Degree audits.
    /// </summary>
    public DbSet<DegreeAuditEntity> DegreeAudits => Set<DegreeAuditEntity>();

    /// <summary>
    /// Alert rules.
    /// </summary>
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    /// <summary>
    /// Student alerts.
    /// </summary>
    public DbSet<StudentAlert> StudentAlerts => Set<StudentAlert>();

    // Career Center
    /// <summary>
    /// Employers.
    /// </summary>
    public DbSet<Employer> Employers => Set<Employer>();

    /// <summary>
    /// Interview slots.
    /// </summary>
    public DbSet<InterviewSlot> InterviewSlots => Set<InterviewSlot>();

    /// <summary>
    /// Placement records.
    /// </summary>
    public DbSet<PlacementRecord> PlacementRecords => Set<PlacementRecord>();

    /// <summary>
    /// Campus drives.
    /// </summary>
    public DbSet<CampusDrive> CampusDrives => Set<CampusDrive>();

    /// <summary>
    /// Drive registrations.
    /// </summary>
    public DbSet<DriveRegistration> DriveRegistrations => Set<DriveRegistration>();

    /// <summary>
    /// Student resumes.
    /// </summary>
    public DbSet<StudentResume> StudentResumes => Set<StudentResume>();

    /// <summary>
    /// Alumni referrals.
    /// </summary>
    public DbSet<AlumniReferral> AlumniReferrals => Set<AlumniReferral>();

    /// <summary>
    /// Career events.
    /// </summary>
    public DbSet<CareerEvent> CareerEvents => Set<CareerEvent>();

    /// <summary>
    /// Career event registrations.
    /// </summary>
    public DbSet<CareerEventRegistration> CareerEventRegistrations => Set<CareerEventRegistration>();

    // Admissions CRM
    /// <summary>
    /// Admission leads.
    /// </summary>
    public DbSet<AdmissionLead> AdmissionLeads => Set<AdmissionLead>();

    /// <summary>
    /// Lead activities.
    /// </summary>
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();

    /// <summary>
    /// Lead tasks.
    /// </summary>
    public DbSet<LeadTask> LeadTasks => Set<LeadTask>();

    /// <summary>
    /// Marketing campaigns.
    /// </summary>
    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();

    /// <summary>
    /// School visits.
    /// </summary>
    public DbSet<SchoolVisit> SchoolVisits => Set<SchoolVisit>();

    /// <summary>
    /// Call logs.
    /// </summary>
    public DbSet<CallLog> CallLogs => Set<CallLog>();

    /// <summary>
    /// Chatbot conversations.
    /// </summary>
    public DbSet<ChatbotConversation> ChatbotConversations => Set<ChatbotConversation>();

    /// <summary>
    /// Chatbot messages.
    /// </summary>
    public DbSet<ChatbotMessage> ChatbotMessages => Set<ChatbotMessage>();

    // Business Intelligence Platform
    /// <summary>
    /// Data warehouse tables.
    /// </summary>
    public DbSet<DataWarehouseTable> DataWarehouseTables => Set<DataWarehouseTable>();

    /// <summary>
    /// DW columns.
    /// </summary>
    public DbSet<DWColumn> DWColumns => Set<DWColumn>();

    /// <summary>
    /// ETL jobs.
    /// </summary>
    public DbSet<ETLJob> ETLJobs => Set<ETLJob>();

    /// <summary>
    /// Dashboards.
    /// </summary>
    public DbSet<Dashboard> Dashboards => Set<Dashboard>();

    /// <summary>
    /// Dashboard widgets.
    /// </summary>
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();

    /// <summary>
    /// BI reports.
    /// </summary>
    public DbSet<BIReport> BIReports => Set<BIReport>();

    /// <summary>
    /// Report schedules.
    /// </summary>
    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();

    /// <summary>
    /// Dashboard subscriptions.
    /// </summary>
    public DbSet<DashboardSubscription> DashboardSubscriptions => Set<DashboardSubscription>();

    /// <summary>
    /// KPI alerts.
    /// </summary>
    public DbSet<KPIAlert> KPIAlerts => Set<KPIAlert>();

    /// <summary>
    /// Data export logs.
    /// </summary>
    public DbSet<DataExportLog> DataExportLogs => Set<DataExportLog>();

    // Workflow Studio
    /// <summary>
    /// Workflow definitions.
    /// </summary>
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    /// <summary>
    /// Workflow instances.
    /// </summary>
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();

    /// <summary>
    /// Workflow steps.
    /// </summary>
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();

    /// <summary>
    /// Approval records.
    /// </summary>
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();

    /// <summary>
    /// Notification records.
    /// </summary>
    public DbSet<NotificationRecord> NotificationRecords => Set<NotificationRecord>();

    // API Gateway
    /// <summary>
    /// API keys.
    /// </summary>
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    /// <summary>
    /// Webhook subscriptions.
    /// </summary>
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    // AegisCore Marketplace
    /// <summary>
    /// Extensions.
    /// </summary>
    public DbSet<Extension> Extensions => Set<Extension>();

    /// <summary>
    /// Extension installations.
    /// </summary>
    public DbSet<ExtensionInstallation> ExtensionInstallations => Set<ExtensionInstallation>();

    /// <summary>
    /// Extension reviews.
    /// </summary>
    public DbSet<ExtensionReview> ExtensionReviews => Set<ExtensionReview>();

    public DbSet<PluginRevenueShare> PluginRevenueShares => Set<PluginRevenueShare>();

    // Identity Governance
    /// <summary>
    /// Users.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Roles.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Permissions.
    /// </summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>
    /// Role permissions.
    /// </summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>
    /// User roles.
    /// </summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>
    /// SAML configurations.
    /// </summary>
    public DbSet<SamlConfiguration> SamlConfigurations => Set<SamlConfiguration>();

    // Disaster Recovery
    /// <summary>
    /// Backup configurations.
    /// </summary>
    public DbSet<BackupConfiguration> BackupConfigurations => Set<BackupConfiguration>();

    /// <summary>
    /// Backup records.
    /// </summary>
    public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();

    /// <summary>
    /// Read replica configs.
    /// </summary>
    public DbSet<ReadReplicaConfig> ReadReplicaConfigs => Set<ReadReplicaConfig>();

    // Multi-Campus Federation
    /// <summary>
    /// Campus configurations.
    /// </summary>
    public DbSet<CampusConfiguration> CampusConfigurations => Set<CampusConfiguration>();

    // Compliance Center
    /// <summary>
    /// Compliance frameworks.
    /// </summary>
    public DbSet<ComplianceFramework> ComplianceFrameworks => Set<ComplianceFramework>();

    /// <summary>
    /// Compliance controls.
    /// </summary>
    public DbSet<ComplianceControl> ComplianceControls => Set<ComplianceControl>();

    /// <summary>
    /// Control evidences.
    /// </summary>
    public DbSet<ControlEvidence> ControlEvidences => Set<ControlEvidence>();

    /// <summary>
    /// Audit findings.
    /// </summary>
    public DbSet<AuditFinding> AuditFindings => Set<AuditFinding>();

    /// <summary>
    /// Corrective action plans.
    /// </summary>
    public DbSet<CorrectiveActionPlan> CorrectiveActionPlans => Set<CorrectiveActionPlan>();

    // Internationalization (i18n)
    /// <summary>
    /// Translation keys.
    /// </summary>
    public DbSet<TranslationKey> TranslationKeys => Set<TranslationKey>();

    /// <summary>
    /// Translation values.
    /// </summary>
    public DbSet<TranslationValue> TranslationValues => Set<TranslationValue>();

    /// <summary>
    /// Country packs.
    /// </summary>
    public DbSet<CountryPack> CountryPacks => Set<CountryPack>();

    /// <summary>
    /// Tax slabs.
    /// </summary>
    public DbSet<TaxSlab> TaxSlabs => Set<TaxSlab>();

    /// <summary>
    /// Currencies.
    /// </summary>
    public DbSet<Currency> Currencies => Set<Currency>();

    // Degree Audit Engine
    /// <summary>
    /// Degree programs.
    /// </summary>
    public DbSet<DegreeProgram> DegreePrograms => Set<DegreeProgram>();

    /// <summary>
    /// Degree requirements.
    /// </summary>
    public DbSet<DegreeRequirement> DegreeRequirements => Set<DegreeRequirement>();

    /// <summary>
    /// Credit transfers.
    /// </summary>
    public DbSet<CreditTransfer> CreditTransfers => Set<CreditTransfer>();

    // Low-Code Forms Builder
    /// <summary>
    /// Form definitions.
    /// </summary>
    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();

    /// <summary>
    /// Form submissions.
    /// </summary>
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    // Digital Twin Campus
    /// <summary>
    /// IoT sensors.
    /// </summary>
    public DbSet<IoTSensor> IoTSensors => Set<IoTSensor>();

    /// <summary>
    /// Sensor readings.
    /// </summary>
    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    /// <summary>
    /// Room occupancies.
    /// </summary>
    public DbSet<RoomOccupancy> RoomOccupancies => Set<RoomOccupancy>();

    /// <summary>
    /// Floor plans.
    /// </summary>
    public DbSet<FloorPlan> FloorPlans => Set<FloorPlan>();

    /// <summary>
    /// Energy readings.
    /// </summary>
    public DbSet<EnergyReading> EnergyReadings => Set<EnergyReading>();

    // Enterprise Configuration Center
    /// <summary>
    /// Configuration entries.
    /// </summary>
    public DbSet<ConfigurationEntry> ConfigurationEntries => Set<ConfigurationEntry>();

    /// <summary>
    /// Config versions.
    /// </summary>
    public DbSet<ConfigVersion> ConfigVersions => Set<ConfigVersion>();

    /// <summary>
    /// Config audit logs.
    /// </summary>
    public DbSet<ConfigAuditLog> ConfigAuditLogs => Set<ConfigAuditLog>();

    // Enterprise Rule Engine
    /// <summary>
    /// Rule definitions.
    /// </summary>
    public DbSet<RuleDefinition> RuleDefinitions => Set<RuleDefinition>();

    /// <summary>
    /// Rule versions.
    /// </summary>
    public DbSet<RuleVersion> RuleVersions => Set<RuleVersion>();

    /// <summary>
    /// Decision tables.
    /// </summary>
    public DbSet<DecisionTable> DecisionTables => Set<DecisionTable>();

    // Platform Services (P79-P86)
    /// <summary>
    /// Notification templates.
    /// </summary>
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    /// <summary>
    /// User notification preferences.
    /// </summary>
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();

    /// <summary>
    /// Notification deliveries.
    /// </summary>
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    public DbSet<NoticeBoardItem> NoticeBoardItems => Set<NoticeBoardItem>();
    public DbSet<GrievanceTicket> GrievanceTickets => Set<GrievanceTicket>();
    public DbSet<GrievanceResponse> GrievanceResponses => Set<GrievanceResponse>();
    public DbSet<TeamsSyncLog> TeamsSyncLogs => Set<TeamsSyncLog>();
    public DbSet<LmsSyncLog> LmsSyncLogs => Set<LmsSyncLog>();
    public DbSet<PowerBiReport> PowerBiReports => Set<PowerBiReport>();
    public DbSet<AlumniProfile> AlumniProfiles => Set<AlumniProfile>();
    public DbSet<AlumniDonation> AlumniDonations => Set<AlumniDonation>();
    public DbSet<StudentSociety> StudentSocieties => Set<StudentSociety>();
    public DbSet<SocietyMembership> SocietyMemberships => Set<SocietyMembership>();
    public DbSet<StudentActivity> StudentActivities => Set<StudentActivity>();
    public DbSet<ActivityParticipation> ActivityParticipations => Set<ActivityParticipation>();
    public DbSet<StudentHealthRecord> StudentHealthRecords => Set<StudentHealthRecord>();
    public DbSet<MedicalVisit> MedicalVisits => Set<MedicalVisit>();
    public DbSet<SickLeaveApplication> SickLeaveApplications => Set<SickLeaveApplication>();

    /// <summary>
    /// Feature flags.
    /// </summary>
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    /// <summary>
    /// Scheduled jobs.
    /// </summary>
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();

    /// <summary>
    /// Job executions.
    /// </summary>
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();

    /// <summary>
    /// Import batches.
    /// </summary>
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    /// <summary>
    /// External database connections.
    /// </summary>
    public DbSet<ExternalDbConnection> ExternalDbConnections => Set<ExternalDbConnection>();

    /// <summary>
    /// Data import jobs.
    /// </summary>
    public DbSet<DataImportJob> DataImportJobs => Set<DataImportJob>();

    /// <summary>
    /// Field mapping templates.
    /// </summary>
    public DbSet<FieldMappingTemplate> FieldMappingTemplates => Set<FieldMappingTemplate>();

    // Replication Management
    public DbSet<ReplicationScalingConfig> ReplicationScalingConfigs => Set<ReplicationScalingConfig>();
    public DbSet<ReplicaHealthLog> ReplicaHealthLogs => Set<ReplicaHealthLog>();
    public DbSet<ReplicationScaleEvent> ReplicationScaleEvents => Set<ReplicationScaleEvent>();
    public DbSet<ReplicationLagAlert> ReplicationLagAlerts => Set<ReplicationLagAlert>();

    // Sync-Lag (pend write correction window)
    public DbSet<SyncLagConfig> SyncLagConfigs => Set<SyncLagConfig>();
    public DbSet<SyncLagEntry> SyncLagEntries => Set<SyncLagEntry>();

    // Tiered Document Vault (R2 PQC storage)
    public DbSet<DocumentTier> DocumentTiers => Set<DocumentTier>();
    public DbSet<DocumentVaultEntry> DocumentVaultEntries => Set<DocumentVaultEntry>();
    public DbSet<DocumentAccessLog> DocumentAccessLogs => Set<DocumentAccessLog>();
    public DbSet<DocumentRetentionPolicy> DocumentRetentionPolicies => Set<DocumentRetentionPolicy>();

    // Profile Pictures
    public DbSet<ProfilePictureRecord> ProfilePictureRecords => Set<ProfilePictureRecord>();

    /// <summary>
    /// Export logs.
    /// </summary>
    public DbSet<ExportLog> ExportLogs => Set<ExportLog>();

    /// <summary>
    /// Platform metrics.
    /// </summary>
    public DbSet<PlatformMetric> PlatformMetrics => Set<PlatformMetric>();

    // P87-P110 Extended Platform
    /// <summary>
    /// Queue definitions.
    /// </summary>
    public DbSet<QueueDefinition> QueueDefinitions => Set<QueueDefinition>();

    /// <summary>
    /// Queue jobs.
    /// </summary>
    public DbSet<QueueJob> QueueJobs => Set<QueueJob>();

    /// <summary>
    /// Domain events.
    /// </summary>
    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();

    /// <summary>
    /// Event schema versions.
    /// </summary>
    public DbSet<EventSchemaVersion> EventSchemaVersions => Set<EventSchemaVersion>();

    /// <summary>
    /// Diagnostic findings.
    /// </summary>
    public DbSet<DiagnosticFinding> DiagnosticFindings => Set<DiagnosticFinding>();

    /// <summary>
    /// Slow queries.
    /// </summary>
    public DbSet<SlowQuery> SlowQueries => Set<SlowQuery>();

    /// <summary>
    /// Service registrations.
    /// </summary>
    public DbSet<ServiceRegistration> ServiceRegistrations => Set<ServiceRegistration>();

    /// <summary>
    /// Service dependencies.
    /// </summary>
    public DbSet<ServiceDependency> ServiceDependencies => Set<ServiceDependency>();

    /// <summary>
    /// API endpoints.
    /// </summary>
    public DbSet<ApiEndpoint> ApiEndpoints => Set<ApiEndpoint>();

    /// <summary>
    /// Health scores.
    /// </summary>
    public DbSet<HealthScore> HealthScores => Set<HealthScore>();

    /// <summary>
    /// Integration registrations.
    /// </summary>
    public DbSet<IntegrationRegistration> IntegrationRegistrations => Set<IntegrationRegistration>();

    /// <summary>
    /// Integration incidents.
    /// </summary>
    public DbSet<IntegrationIncident> IntegrationIncidents => Set<IntegrationIncident>();

    /// <summary>
    /// Benchmark runs.
    /// </summary>
    public DbSet<BenchmarkRun> BenchmarkRuns => Set<BenchmarkRun>();

    /// <summary>
    /// AI providers.
    /// </summary>
    public DbSet<AIProvider> AIProviders => Set<AIProvider>();

    /// <summary>
    /// AI models.
    /// </summary>
    public DbSet<AIModel> AIModels => Set<AIModel>();

    /// <summary>
    /// AI token usages.
    /// </summary>
    public DbSet<AITokenUsage> AITokenUsages => Set<AITokenUsage>();

    /// <summary>
    /// Prompts.
    /// </summary>
    public DbSet<Prompt> Prompts => Set<Prompt>();

    /// <summary>
    /// Prompt versions.
    /// </summary>
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();

    /// <summary>
    /// Prompt AB tests.
    /// </summary>
    public DbSet<PromptABTest> PromptABTests => Set<PromptABTest>();

    /// <summary>
    /// Guardrail rules.
    /// </summary>
    public DbSet<GuardrailRule> GuardrailRules => Set<GuardrailRule>();

    /// <summary>
    /// Guardrail violations.
    /// </summary>
    public DbSet<GuardrailViolation> GuardrailViolations => Set<GuardrailViolation>();

    /// <summary>
    /// AI feedbacks.
    /// </summary>
    public DbSet<AIFeedback> AIFeedbacks => Set<AIFeedback>();

    /// <summary>
    /// Licenses.
    /// </summary>
    public DbSet<License> Licenses => Set<License>();

    /// <summary>
    /// License heartbeats.
    /// </summary>
    public DbSet<LicenseHeartbeat> LicenseHeartbeats => Set<LicenseHeartbeat>();

    /// <summary>
    /// Support tickets.
    /// </summary>
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    /// <summary>
    /// Release downloads.
    /// </summary>
    public DbSet<ReleaseDownload> ReleaseDownloads => Set<ReleaseDownload>();

    /// <summary>
    /// Update deployments.
    /// </summary>
    public DbSet<UpdateDeployment> UpdateDeployments => Set<UpdateDeployment>();

    /// <summary>
    /// Official modules.
    /// </summary>
    public DbSet<OfficialModule> OfficialModules => Set<OfficialModule>();

    /// <summary>
    /// Fee defaulter records.
    /// </summary>
    public DbSet<FeeDefaulterRecord> FeeDefaulterRecords => Set<FeeDefaulterRecord>();

    /// <summary>
    /// Misconduct cases.
    /// </summary>
    public DbSet<MisconductCase> MisconductCases => Set<MisconductCase>();

    /// <summary>
    /// Misconduct evidences.
    /// </summary>
    public DbSet<MisconductEvidence> MisconductEvidences => Set<MisconductEvidence>();

    /// <summary>
    /// Faculty workloads.
    /// </summary>
    public DbSet<FacultyWorkload> FacultyWorkloads => Set<FacultyWorkload>();

    /// <summary>
    /// Degree verifications.
    /// </summary>
    public DbSet<DegreeVerification> DegreeVerifications => Set<DegreeVerification>();

    /// <summary>
    /// Registration windows.
    /// </summary>
    public DbSet<RegistrationWindow> RegistrationWindows => Set<RegistrationWindow>();

    /// <summary>
    /// Registration enrollments.
    /// </summary>
    public DbSet<RegistrationEnrollment> RegistrationEnrollments => Set<RegistrationEnrollment>();

    /// <summary>
    /// Curriculum proposals.
    /// </summary>
    public DbSet<CurriculumProposal> CurriculumProposals => Set<CurriculumProposal>();

    /// <summary>
    /// Tunnel configurations.
    /// </summary>
    public DbSet<TunnelConfiguration> TunnelConfigurations => Set<TunnelConfiguration>();

    /// <summary>
    /// Access audit logs.
    /// </summary>
    public DbSet<AccessAuditLog> AccessAuditLogs => Set<AccessAuditLog>();

    /// <summary>
    /// Hardcoding findings.
    /// </summary>
    public DbSet<HardcodingFinding> HardcodingFindings => Set<HardcodingFinding>();

    // P111 Grade Appeals
    public DbSet<GradeAppeal> GradeAppeals => Set<GradeAppeal>();
    public DbSet<AppealEvidence> AppealEvidence => Set<AppealEvidence>();
    public DbSet<AppealCommitteeBrief> AppealCommitteeBriefs => Set<AppealCommitteeBrief>();

    // P112 Grade Distribution Analysis
    public DbSet<GradeDistribution> GradeDistributions => Set<GradeDistribution>();
    public DbSet<DistributionAnomalyAlert> DistributionAnomalyAlerts => Set<DistributionAnomalyAlert>();

    // P113 Plagiarism Detection
    public DbSet<PlagiarismScan> PlagiarismScans => Set<PlagiarismScan>();

    // P114 Plagiarism API Integration
    public DbSet<PlagiarismApiSubmission> PlagiarismApiSubmissions => Set<PlagiarismApiSubmission>();

    // P115 Convocation Management
    public DbSet<Convocation> Convocations => Set<Convocation>();
    public DbSet<ConvocationEligibility> ConvocationEligibilities => Set<ConvocationEligibility>();
    public DbSet<DegreeCertificate> DegreeCertificates => Set<DegreeCertificate>();
    public DbSet<ConvocationAttendance> ConvocationAttendances => Set<ConvocationAttendance>();

    // P116 Semester Freeze & Deferral
    public DbSet<SemesterFreeze> SemesterFreezes => Set<SemesterFreeze>();
    public DbSet<FreezeDocument> FreezeDocuments => Set<FreezeDocument>();

    // P117 Online Exam Proctoring
    public DbSet<ProctoringSession> ProctoringSessions => Set<ProctoringSession>();
    public DbSet<StudentProctoringRecord> StudentProctoringRecords => Set<StudentProctoringRecord>();
    public DbSet<ProctorEvent> ProctorEvents => Set<ProctorEvent>();

    // P118 Prerequisite Conflict Detection
    public DbSet<PrerequisiteConflict> PrerequisiteConflicts => Set<PrerequisiteConflict>();
    public DbSet<ChangeImpactSimulation> ChangeImpactSimulations => Set<ChangeImpactSimulation>();

    // P119 Fee Defaulter Prediction (extends P103)
    public DbSet<DefaulterRiskScore> DefaulterRiskScores => Set<DefaulterRiskScore>();
    public DbSet<DefaulterIntervention> DefaulterInterventions => Set<DefaulterIntervention>();
    public DbSet<CashFlowForecast> CashFlowForecasts => Set<CashFlowForecast>();

    // P120 Visiting Faculty Marketplace
    public DbSet<VisitingFacultyProfile> VisitingFacultyProfiles => Set<VisitingFacultyProfile>();
    public DbSet<FacultySlotMatch> FacultySlotMatches => Set<FacultySlotMatch>();
    public DbSet<VisitingFacultyContract> VisitingFacultyContracts => Set<VisitingFacultyContract>();

    // P121 Accreditation Readiness Score
    public DbSet<ReadinessScore> ReadinessScores => Set<ReadinessScore>();
    public DbSet<ReadinessGap> ReadinessGaps => Set<ReadinessGap>();
    public DbSet<ReadinessAlert> ReadinessAlerts => Set<ReadinessAlert>();
    public DbSet<EvidenceFreshnessLog> EvidenceFreshnessLogs => Set<EvidenceFreshnessLog>();

    // P122 Student Achievement Gamification
    public DbSet<AchievementBadge> AchievementBadges => Set<AchievementBadge>();
    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();
    public DbSet<DeansListEntry> DeansListEntries => Set<DeansListEntry>();
    public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
    public DbSet<AchievementRoadmap> AchievementRoadmaps => Set<AchievementRoadmap>();

    // P123 Mental Health Ticketing
    public DbSet<MentalHealthTicket> MentalHealthTickets => Set<MentalHealthTicket>();
    public DbSet<MandatoryReport> MandatoryReports => Set<MandatoryReport>();
    public DbSet<WellnessCheckIn> WellnessCheckIns => Set<WellnessCheckIn>();
    public DbSet<CounselorProfile> CounselorProfiles => Set<CounselorProfile>();

    // P124 Foreign Student Visa & MORA
    public DbSet<ForeignStudentRegistry> ForeignStudentRegistries => Set<ForeignStudentRegistry>();
    public DbSet<VisaDocument> VisaDocuments => Set<VisaDocument>();
    public DbSet<VisaExpiryAlert> VisaExpiryAlerts => Set<VisaExpiryAlert>();
    public DbSet<VisaRenewal> VisaRenewals => Set<VisaRenewal>();
    public DbSet<ComplianceBreach> ComplianceBreaches => Set<ComplianceBreach>();
    public DbSet<MoraSubmission> MoraSubmissions => Set<MoraSubmission>();

    // P125 Voice Assistant (NidaBot)
    public DbSet<VoiceSession> VoiceSessions => Set<VoiceSession>();

    // P126 Board Portal (BoardView)
    public DbSet<ExecutiveDashboard> ExecutiveDashboards => Set<ExecutiveDashboard>();
    public DbSet<BoardMeetingPack> BoardMeetingPacks => Set<BoardMeetingPack>();
    public DbSet<BoardAlert> BoardAlerts => Set<BoardAlert>();
    public DbSet<StrategicPlanGoal> StrategicPlanGoals => Set<StrategicPlanGoal>();
    public DbSet<BenchmarkComparison> BenchmarkComparisons => Set<BenchmarkComparison>();

    // P127 Student Elections
    public DbSet<Election> Elections => Set<Election>();
    public DbSet<CandidateNomination> CandidateNominations => Set<CandidateNomination>();
    public DbSet<VoterToken> VoterTokens => Set<VoterToken>();
    public DbSet<EncryptedBallot> EncryptedBallots => Set<EncryptedBallot>();
    public DbSet<ElectionResult> ElectionResults => Set<ElectionResult>();

    // Course Catalog Ingestion
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<CatalogCourseRecord> CatalogCourseRecords => Set<CatalogCourseRecord>();
    public DbSet<CatalogVersion> CatalogVersions => Set<CatalogVersion>();
    public DbSet<DegreeTree> DegreeTrees => Set<DegreeTree>();
    public DbSet<ElectiveSlot> ElectiveSlots => Set<ElectiveSlot>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<CourseGradePolicy> CourseGradePolicies => Set<CourseGradePolicy>();
    public DbSet<CourseEquivalency> CourseEquivalencies => Set<CourseEquivalency>();

    // LMS & Class Generation
    public DbSet<ClassGenerationJob> ClassGenerationJobs => Set<ClassGenerationJob>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<StudentSectionEnrollment> StudentSectionEnrollments => Set<StudentSectionEnrollment>();
    public DbSet<LmsProvision> LmsProvisions => Set<LmsProvision>();
    public DbSet<LmsRosterSyncLog> LmsRosterSyncLogs => Set<LmsRosterSyncLog>();
    public DbSet<LmsConfiguration> LmsConfigurations => Set<LmsConfiguration>();
    public DbSet<DirectorySyncLog> DirectorySyncLogs => Set<DirectorySyncLog>();

    // OPTIONAL-26 Cafeteria Management (FoodCore)
    public DbSet<CafeteriaUnit> CafeteriaUnits => Set<CafeteriaUnit>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<FoodOrder> FoodOrders => Set<FoodOrder>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryAlert> InventoryAlerts => Set<InventoryAlert>();
    public DbSet<VendorScorecard> VendorScorecards => Set<VendorScorecard>();
    public DbSet<DemandForecast> DemandForecasts => Set<DemandForecast>();
    public DbSet<WasteLog> WasteLogs => Set<WasteLog>();

    // Cross-cutting: Personality Mode
    public DbSet<PersonalityConfig> PersonalityConfigs => Set<PersonalityConfig>();

    // Cross-cutting: Chart System
    public DbSet<ChartConfiguration> ChartConfigurations => Set<ChartConfiguration>();
    public DbSet<InsightChip> InsightChips => Set<InsightChip>();

    // Cross-cutting: OCR
    public DbSet<OcrProviderConfig> OcrProviderConfigs => Set<OcrProviderConfig>();

    // P-J Login Audit
    /// <summary>
    /// Login audit records.
    /// </summary>
    public DbSet<LoginAuditRecord> LoginAuditRecords => Set<LoginAuditRecord>();

    /// <summary>
    /// Audit logs.
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<PolicyConfig> PolicyConfigs => Set<PolicyConfig>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<AcademicHoliday> AcademicHolidays => Set<AcademicHoliday>();
    public DbSet<CrossDeptPermissionRequest> CrossDeptPermissionRequests => Set<CrossDeptPermissionRequest>();
    public DbSet<LicenseState> LicenseStates => Set<LicenseState>();

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<WizardSession> WizardSessions => Set<WizardSession>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<WizardAdminCredential> WizardAdminCredentials => Set<WizardAdminCredential>();
    public DbSet<AdmissionApplication> AdmissionApplications => Set<AdmissionApplication>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<MeritList> MeritLists => Set<MeritList>();
    public DbSet<MeritListEntry> MeritListEntries => Set<MeritListEntry>();
    public DbSet<SeatAllocation> SeatAllocations => Set<SeatAllocation>();
    public DbSet<AssessmentScheme> AssessmentSchemes => Set<AssessmentScheme>();
    public DbSet<AssessmentComponent> AssessmentComponents => Set<AssessmentComponent>();
    public DbSet<CourseMark> CourseMarks => Set<CourseMark>();
    public DbSet<MarkModeration> MarkModerations => Set<MarkModeration>();
    public DbSet<GradeScale> GradeScales => Set<GradeScale>();
    public DbSet<GradeBand> GradeBands => Set<GradeBand>();
    public DbSet<CourseGrade> CourseGrades => Set<CourseGrade>();
    public DbSet<ExamRegistration> ExamRegistrations => Set<ExamRegistration>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<CisUpload> CisUploads => Set<CisUpload>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeComponent> FeeComponents => Set<FeeComponent>();
    public DbSet<StudentFeeDue> StudentFeeDues => Set<StudentFeeDue>();
    public DbSet<StudentFeePayment> StudentFeePayments => Set<StudentFeePayment>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<FinanceVoucher> FinanceVouchers => Set<FinanceVoucher>();
    public DbSet<FinanceVoucherLine> FinanceVoucherLines => Set<FinanceVoucherLine>();
    public DbSet<FinanceVoucherApproval> FinanceVoucherApprovals => Set<FinanceVoucherApproval>();
    public DbSet<FinanceVoucherApprovalRule> FinanceVoucherApprovalRules => Set<FinanceVoucherApprovalRule>();
    public DbSet<FinanceVoucherAttachment> FinanceVoucherAttachments => Set<FinanceVoucherAttachment>();
    public DbSet<FinanceVoucherReversal> FinanceVoucherReversals => Set<FinanceVoucherReversal>();
    public DbSet<FinanceVoucherSequence> FinanceVoucherSequences => Set<FinanceVoucherSequence>();
    public DbSet<FeeDemand> FeeDemands => Set<FeeDemand>();
    public DbSet<FeeInstallment> FeeInstallments => Set<FeeInstallment>();
    public DbSet<FinancePaymentPlanConfiguration> FinancePaymentPlanConfigurations => Set<FinancePaymentPlanConfiguration>();
    public DbSet<FinancePaymentPlan> FinancePaymentPlans => Set<FinancePaymentPlan>();
    public DbSet<FinancePayment> FinancePayments => Set<FinancePayment>();
    public DbSet<PaymentGatewayConfiguration> PaymentGatewayConfigurations => Set<PaymentGatewayConfiguration>();
    public DbSet<PaymentGatewayEvent> PaymentGatewayEvents => Set<PaymentGatewayEvent>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();
    public DbSet<FinanceForeignCurrencyTransaction> FinanceForeignCurrencyTransactions => Set<FinanceForeignCurrencyTransaction>();
    public DbSet<FinanceReportArtifact> FinanceReportArtifacts => Set<FinanceReportArtifact>();
    public DbSet<FinanceReportProfile> FinanceReportProfiles => Set<FinanceReportProfile>();
    public DbSet<TenantFinanceConfiguration> TenantFinanceConfigurations => Set<TenantFinanceConfiguration>();
    public DbSet<FinanceAccountingStandardPolicy> FinanceAccountingStandardPolicies => Set<FinanceAccountingStandardPolicy>();
    public DbSet<FinanceReportingEntity> FinanceReportingEntities => Set<FinanceReportingEntity>();
    public DbSet<FinanceRestatementRule> FinanceRestatementRules => Set<FinanceRestatementRule>();
    public DbSet<FinanceRestatementRun> FinanceRestatementRuns => Set<FinanceRestatementRun>();
    public DbSet<FinanceRestatementMemoLine> FinanceRestatementMemoLines => Set<FinanceRestatementMemoLine>();
    public DbSet<FinanceConsolidationGroup> FinanceConsolidationGroups => Set<FinanceConsolidationGroup>();
    public DbSet<FinanceConsolidationMember> FinanceConsolidationMembers => Set<FinanceConsolidationMember>();
    public DbSet<FinanceEliminationRule> FinanceEliminationRules => Set<FinanceEliminationRule>();
    public DbSet<FinanceConsolidationRun> FinanceConsolidationRuns => Set<FinanceConsolidationRun>();
    public DbSet<FinanceConsolidationMemoLine> FinanceConsolidationMemoLines => Set<FinanceConsolidationMemoLine>();
    public DbSet<FinancePqcKey> FinancePqcKeys => Set<FinancePqcKey>();
    public DbSet<FinancePqcKeyCeremony> FinancePqcKeyCeremonies => Set<FinancePqcKeyCeremony>();
    public DbSet<FinancePqcKeyCeremonyApproval> FinancePqcKeyCeremonyApprovals => Set<FinancePqcKeyCeremonyApproval>();
    public DbSet<FinanceLedgerSignature> FinanceLedgerSignatures => Set<FinanceLedgerSignature>();
    public DbSet<FinanceSecurityDeposit> FinanceSecurityDeposits => Set<FinanceSecurityDeposit>();
    public DbSet<FinanceSecurityDepositDeduction> FinanceSecurityDepositDeductions => Set<FinanceSecurityDepositDeduction>();
    public DbSet<FinanceAuditSeal> FinanceAuditSeals => Set<FinanceAuditSeal>();
    public DbSet<FinanceVendor> FinanceVendors => Set<FinanceVendor>();
    public DbSet<FinanceVendorContact> FinanceVendorContacts => Set<FinanceVendorContact>();
    public DbSet<FinanceStandingPurchaseOrder> FinanceStandingPurchaseOrders => Set<FinanceStandingPurchaseOrder>();
    public DbSet<FinanceStandingPurchaseOrderSpend> FinanceStandingPurchaseOrderSpend => Set<FinanceStandingPurchaseOrderSpend>();
    public DbSet<FinancePurchaseOrder> FinancePurchaseOrders => Set<FinancePurchaseOrder>();
    public DbSet<FinancePurchaseOrderLine> FinancePurchaseOrderLines => Set<FinancePurchaseOrderLine>();
    public DbSet<FinanceGoodsReceipt> FinanceGoodsReceipts => Set<FinanceGoodsReceipt>();
    public DbSet<FinanceGoodsReceiptLine> FinanceGoodsReceiptLines => Set<FinanceGoodsReceiptLine>();
    public DbSet<FinanceVendorInvoice> FinanceVendorInvoices => Set<FinanceVendorInvoice>();
    public DbSet<FinanceVendorInvoiceLine> FinanceVendorInvoiceLines => Set<FinanceVendorInvoiceLine>();
    public DbSet<FinanceThreeWayMatch> FinanceThreeWayMatches => Set<FinanceThreeWayMatch>();
    public DbSet<FinanceVendorContract> FinanceVendorContracts => Set<FinanceVendorContract>();
    public DbSet<FinanceVendorDocument> FinanceVendorDocuments => Set<FinanceVendorDocument>();
    public DbSet<FinancePayrollPosting> FinancePayrollPostings => Set<FinancePayrollPosting>();
    public DbSet<FinancePayrollPostingLine> FinancePayrollPostingLines => Set<FinancePayrollPostingLine>();
    public DbSet<FinancePayrollBankInstruction> FinancePayrollBankInstructions => Set<FinancePayrollBankInstruction>();
    public DbSet<FinancePayrollPaymentFile> FinancePayrollPaymentFiles => Set<FinancePayrollPaymentFile>();
    public DbSet<FinanceGratuityAssumption> FinanceGratuityAssumptions => Set<FinanceGratuityAssumption>();
    public DbSet<FinanceGratuityValuation> FinanceGratuityValuations => Set<FinanceGratuityValuation>();
    public DbSet<FinanceBankAccount> FinanceBankAccounts => Set<FinanceBankAccount>();
    public DbSet<FinanceBankFeedTransaction> FinanceBankFeedTransactions => Set<FinanceBankFeedTransaction>();
    public DbSet<FinanceGrant> FinanceGrants => Set<FinanceGrant>();
    public DbSet<FinanceScholarshipProgram> FinanceScholarshipPrograms => Set<FinanceScholarshipProgram>();
    public DbSet<FinanceScholarshipApplication> FinanceScholarshipApplications => Set<FinanceScholarshipApplication>();
    public DbSet<FinanceScholarshipApplicationEvent> FinanceScholarshipApplicationEvents => Set<FinanceScholarshipApplicationEvent>();
    public DbSet<FinanceScholarshipAward> FinanceScholarshipAwards => Set<FinanceScholarshipAward>();
    public DbSet<FinanceAssetAccountingEvent> FinanceAssetAccountingEvents => Set<FinanceAssetAccountingEvent>();
    public DbSet<FinanceUtilityBill> FinanceUtilityBills => Set<FinanceUtilityBill>();
    public DbSet<FinanceUtilityAllocation> FinanceUtilityAllocations => Set<FinanceUtilityAllocation>();
    public DbSet<FinanceInsurancePolicy> FinanceInsurancePolicies => Set<FinanceInsurancePolicy>();
    public DbSet<FinanceInsuranceClaim> FinanceInsuranceClaims => Set<FinanceInsuranceClaim>();
    public DbSet<FinanceInventoryItem> FinanceInventoryItems => Set<FinanceInventoryItem>();
    public DbSet<FinanceInventoryMovement> FinanceInventoryMovements => Set<FinanceInventoryMovement>();
    public DbSet<FinanceFleetCostPosting> FinanceFleetCostPostings => Set<FinanceFleetCostPosting>();
    public DbSet<FinanceFleetTrip> FinanceFleetTrips => Set<FinanceFleetTrip>();
    public DbSet<FinanceFleetProfile> FinanceFleetProfiles => Set<FinanceFleetProfile>();
    public DbSet<FinanceInvestmentFund> FinanceInvestmentFunds => Set<FinanceInvestmentFund>();
    public DbSet<FinanceInvestmentHolding> FinanceInvestmentHoldings => Set<FinanceInvestmentHolding>();
    public DbSet<FinanceInvestmentTransaction> FinanceInvestmentTransactions => Set<FinanceInvestmentTransaction>();
    public DbSet<FinanceInvestmentBenchmark> FinanceInvestmentBenchmarks => Set<FinanceInvestmentBenchmark>();
    public DbSet<FinanceRegulatorySchema> FinanceRegulatorySchemas => Set<FinanceRegulatorySchema>();
    public DbSet<FinanceRegulatorySubmission> FinanceRegulatorySubmissions => Set<FinanceRegulatorySubmission>();
    public DbSet<FinanceControlEvaluationBatch> FinanceControlEvaluationBatches => Set<FinanceControlEvaluationBatch>();
    public DbSet<FinanceSodRule> FinanceSodRules => Set<FinanceSodRule>();
    public DbSet<FinanceAuditorEngagement> FinanceAuditorEngagements => Set<FinanceAuditorEngagement>();
    public DbSet<FinanceAuditSample> FinanceAuditSamples => Set<FinanceAuditSample>();
    public DbSet<FinanceAuditEvidencePackage> FinanceAuditEvidencePackages => Set<FinanceAuditEvidencePackage>();
    public DbSet<FinanceTaxRule> FinanceTaxRules => Set<FinanceTaxRule>();
    public DbSet<FinanceWithholdingRecord> FinanceWithholdingRecords => Set<FinanceWithholdingRecord>();
    public DbSet<FinancePettyCashFund> FinancePettyCashFunds => Set<FinancePettyCashFund>();
    public DbSet<FinancePettyCashExpense> FinancePettyCashExpenses => Set<FinancePettyCashExpense>();
    public DbSet<FinancePettyCashCount> FinancePettyCashCounts => Set<FinancePettyCashCount>();
    public DbSet<FinanceLibraryAcquisition> FinanceLibraryAcquisitions => Set<FinanceLibraryAcquisition>();
    public DbSet<FinanceLibraryFine> FinanceLibraryFines => Set<FinanceLibraryFine>();
    public DbSet<ProcurementRequisition> ProcurementRequisitions => Set<ProcurementRequisition>();
    public DbSet<RequisitionLineItem> RequisitionLineItems => Set<RequisitionLineItem>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<GradeOverrideRequest> GradeOverrideRequests => Set<GradeOverrideRequest>();
    public DbSet<SectionOverrideRequest> SectionOverrideRequests => Set<SectionOverrideRequest>();
    public DbSet<AnonymousFeedbackSubmission> AnonymousFeedbackSubmissions => Set<AnonymousFeedbackSubmission>();
    public DbSet<CourseReEnrollment> CourseReEnrollments => Set<CourseReEnrollment>();
    public DbSet<CurriculumVersionRecord> CurriculumVersions => Set<CurriculumVersionRecord>();
    public DbSet<CurriculumCourse> CurriculumCourses => Set<CurriculumCourse>();
    public DbSet<CurriculumPrerequisite> CurriculumPrerequisites => Set<CurriculumPrerequisite>();
    public DbSet<CurriculumChangeHistory> CurriculumChangeHistory => Set<CurriculumChangeHistory>();
    public DbSet<OfflineAttendanceEvent> OfflineAttendanceEvents => Set<OfflineAttendanceEvent>();
    public DbSet<UniversitySsoProfile> UniversitySsoProfiles => Set<UniversitySsoProfile>();
    public DbSet<UserSecondFactor> UserSecondFactors => Set<UserSecondFactor>();
    public DbSet<SsoSession> SsoSessions => Set<SsoSession>();
    public DbSet<ExamPaperBatch> ExamPaperBatches => Set<ExamPaperBatch>();
    public DbSet<ExamPaperScan> ExamPaperScans => Set<ExamPaperScan>();
    public DbSet<AgenticGradeRun> AgenticGradeRuns => Set<AgenticGradeRun>();
    public DbSet<HumanGradeReview> HumanGradeReviews => Set<HumanGradeReview>();
    public DbSet<PaperDestructionManifest> PaperDestructionManifests => Set<PaperDestructionManifest>();

    public DbSet<DefaultRiskAssessment> DefaultRiskAssessments => Set<DefaultRiskAssessment>();
    public DbSet<PaymentPlanArrangement> PaymentPlanArrangements => Set<PaymentPlanArrangement>();
    public DbSet<PaymentPlanInstallment> PaymentPlanInstallments => Set<PaymentPlanInstallment>();
    public DbSet<InterventionRecord> InterventionRecords => Set<InterventionRecord>();
    public DbSet<EthicalAuditLog> EthicalAuditLogs => Set<EthicalAuditLog>();
    public DbSet<TeachingSlot> TeachingSlots => Set<TeachingSlot>();
    public DbSet<SlotCandidateMatch> SlotCandidateMatches => Set<SlotCandidateMatch>();
    public DbSet<FacultyEngagement> FacultyEngagements => Set<FacultyEngagement>();
    public DbSet<FacultyPerformanceRating> FacultyPerformanceRatings => Set<FacultyPerformanceRating>();
    public DbSet<FacultyCompensation> FacultyCompensations => Set<FacultyCompensation>();
    public DbSet<AccreditationFramework> AccreditationFrameworks => Set<AccreditationFramework>();
    public DbSet<CriterionEvidence> CriterionEvidences => Set<CriterionEvidence>();
    public DbSet<AccreditationScoreSnapshot> AccreditationScoreSnapshots => Set<AccreditationScoreSnapshot>();
    public DbSet<AccreditationAuditFinding> AccreditationAuditFindings => Set<AccreditationAuditFinding>();
    public DbSet<GapAnalysisItem> GapAnalysisItems => Set<GapAnalysisItem>();
    public DbSet<BoardMeeting> BoardMeetings => Set<BoardMeeting>();
    public DbSet<BoardDecision> BoardDecisions => Set<BoardDecision>();
    public DbSet<StrategicGoal> StrategicGoals => Set<StrategicGoal>();
    public DbSet<InstitutionBenchmark> InstitutionBenchmarks => Set<InstitutionBenchmark>();
    public DbSet<BoardResolution> BoardResolutions => Set<BoardResolution>();
    public DbSet<VoiceIntent> VoiceIntents => Set<VoiceIntent>();
    public DbSet<EscalationLog> EscalationLogs => Set<EscalationLog>();
    public DbSet<BadgeDefinition> BadgeDefinitions => Set<BadgeDefinition>();
    public DbSet<BadgeAward> BadgeAwards => Set<BadgeAward>();
    public DbSet<CounselingCaseNote> CounselingCaseNotes => Set<CounselingCaseNote>();
    public DbSet<MandatoryReportLog> MandatoryReportLogs => Set<MandatoryReportLog>();
    public DbSet<ForeignStudentRecord> ForeignStudentRecords => Set<ForeignStudentRecord>();
    public DbSet<DocumentExpiryTracker> DocumentExpiryTrackers => Set<DocumentExpiryTracker>();
    public DbSet<EnrollmentLetter> EnrollmentLetters => Set<EnrollmentLetter>();
    public DbSet<VisaRenewalWorkflow> VisaRenewalWorkflows => Set<VisaRenewalWorkflow>();
    public DbSet<ElectionPosition> ElectionPositions => Set<ElectionPosition>();
    public DbSet<ElectionCandidate> ElectionCandidates => Set<ElectionCandidate>();
    public DbSet<Ballot> Ballots => Set<Ballot>();
    public DbSet<PreOrder> PreOrders => Set<PreOrder>();
    public DbSet<CafeteriaTransaction> CafeteriaTransactions => Set<CafeteriaTransaction>();
    public DbSet<VendorPerformanceScore> VendorPerformanceScores => Set<VendorPerformanceScore>();
    public DbSet<CafeteriaInventory> CafeteriaInventory => Set<CafeteriaInventory>();
    public DbSet<FleetVehicle> FleetVehicles => Set<FleetVehicle>();
    public DbSet<TransportRegistration> TransportRegistrations => Set<TransportRegistration>();
    public DbSet<BoardingLog> BoardingLogs => Set<BoardingLog>();
    public DbSet<TransportIncident> TransportIncidents => Set<TransportIncident>();
    public DbSet<TransportInspection> TransportInspections => Set<TransportInspection>();

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types
    /// exposed in <see cref="DbSet{TEntity}"/> properties on your derived context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filter for multi-tenancy
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var tenantProperty = Expression.Property(parameter, "TenantId");
                var contextTenant = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
                var equal = Expression.Equal(tenantProperty, contextTenant);
                var lambda = Expression.Lambda(equal, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.Property(item => item.LatitudeEncrypted).HasColumnName("Latitude").HasColumnType("bytea");
            entity.Property(item => item.LongitudeEncrypted).HasColumnName("Longitude").HasColumnType("bytea");
            entity.Property(item => item.LocationStatus).HasConversion<string>().HasMaxLength(24).HasDefaultValue(LocationStatus.Unverifiable);
            entity.HasOne(item => item.Geofence).WithMany().HasForeignKey(item => item.GeofenceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CampusPolygon>(entity =>
        {
            entity.Property(item => item.VerticesJson).HasColumnName("Vertices").HasColumnType("jsonb");
            entity.HasIndex(item => new { item.TenantId, item.IsActive });
        });

        modelBuilder.Entity<FerpaAuditLog>(entity =>
        {
            entity.Property(item => item.IpAddressEncrypted).HasColumnName("IpAddress").HasColumnType("bytea");
            entity.HasIndex(item => new { item.TenantId, item.AccessedAt });
            entity.HasIndex(item => new { item.TenantId, item.StudentId, item.AccessedAt });
        });

        modelBuilder.Entity<ConsentLedger>(entity =>
        {
            entity.Property(item => item.IpAddressEncrypted).HasColumnName("IpAddress").HasColumnType("bytea");
            entity.HasIndex(item => new { item.TenantId, item.UserId, item.DataCategory });
            entity.HasIndex(item => new { item.TenantId, item.RevokedAt });
        });

        modelBuilder.Entity<ErasureRequest>(entity =>
        {
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).HasDefaultValue(ErasureRequestStatus.Pending);
            entity.HasIndex(item => new { item.TenantId, item.Status, item.DeadlineAt });
        });

        // Enum conversions (store as strings)
        modelBuilder.HasPostgresEnum<EnrollmentStatus>();
        modelBuilder.HasPostgresEnum<AttendanceStatus>();
        modelBuilder.HasPostgresEnum<ExamType>();
        modelBuilder.HasPostgresEnum<AegisCore.Domain.DayOfWeek>();
        modelBuilder.HasPostgresEnum<Designation>();
        modelBuilder.HasPostgresEnum<AlignmentLevel>();
        modelBuilder.HasPostgresEnum<AccountType>();
        modelBuilder.HasPostgresEnum<JournalEntryType>();
        modelBuilder.HasPostgresEnum<EmployeeType>();
        modelBuilder.HasPostgresEnum<EmployeeStatus>();
        modelBuilder.HasPostgresEnum<JobPostingStatus>();
        modelBuilder.HasPostgresEnum<ApplicationStatus>();
        modelBuilder.HasPostgresEnum<InterviewStatus>();
        modelBuilder.HasPostgresEnum<LeaveType>();
        modelBuilder.HasPostgresEnum<LeaveStatus>();
        modelBuilder.HasPostgresEnum<AttendanceMethod>();
        modelBuilder.HasPostgresEnum<ReviewType>();
        modelBuilder.HasPostgresEnum<ReviewStatus>();
        modelBuilder.HasPostgresEnum<TrainingStatus>();
        modelBuilder.HasPostgresEnum<ResearchProjectStatus>();
        modelBuilder.HasPostgresEnum<ResearchMemberRole>();
        modelBuilder.HasPostgresEnum<MilestoneStatus>();
        modelBuilder.HasPostgresEnum<DeliverableType>();
        modelBuilder.HasPostgresEnum<DeliverableStatus>();
        modelBuilder.HasPostgresEnum<GrantStatus>();
        modelBuilder.HasPostgresEnum<PublicationType>();
        modelBuilder.HasPostgresEnum<PatentStatus>();
        modelBuilder.HasPostgresEnum<EthicsReviewStatus>();
        modelBuilder.HasPostgresEnum<TravelStatus>();
        modelBuilder.HasPostgresEnum<BloomsLevel>();
        modelBuilder.HasPostgresEnum<MappingStrength>();
        modelBuilder.HasPostgresEnum<AssessmentType>();
        modelBuilder.HasPostgresEnum<CIPStatus>();
        modelBuilder.HasPostgresEnum<AccreditationCycleStatus>();
        modelBuilder.HasPostgresEnum<SSRStatus>();
        modelBuilder.HasPostgresEnum<SSRSectionStatus>();
        modelBuilder.HasPostgresEnum<SSRReviewStatus>();
        modelBuilder.HasPostgresEnum<CommitteeRole>();
        modelBuilder.HasPostgresEnum<ActionItemStatus>();
        modelBuilder.HasPostgresEnum<FindingType>();
        modelBuilder.HasPostgresEnum<FindingSeverity>();
        modelBuilder.HasPostgresEnum<FeeStatus>();
        modelBuilder.HasPostgresEnum<DisciplineSeverity>();
        modelBuilder.HasPostgresEnum<DisciplineStatus>();
        modelBuilder.HasPostgresEnum<AchievementType>();
        modelBuilder.HasPostgresEnum<ScholarshipStatus>();
        modelBuilder.HasPostgresEnum<HostelStatus>();
        modelBuilder.HasPostgresEnum<TransportStatus>();
        modelBuilder.HasPostgresEnum<TransferStatus>();
        modelBuilder.HasPostgresEnum<ClearanceStatus>();
        modelBuilder.HasPostgresEnum<BookStatus>();
        modelBuilder.HasPostgresEnum<CopyStatus>();
        modelBuilder.HasPostgresEnum<MemberType>();
        modelBuilder.HasPostgresEnum<MemberStatus>();
        modelBuilder.HasPostgresEnum<IssueStatus>();
        modelBuilder.HasPostgresEnum<ReservationStatus>();
        modelBuilder.HasPostgresEnum<OrderStatus>();
        modelBuilder.HasPostgresEnum<BudgetStatus>();
        modelBuilder.HasPostgresEnum<LabStatus>();
        modelBuilder.HasPostgresEnum<EquipmentStatus>();
        modelBuilder.HasPostgresEnum<BookingStatus>();
        modelBuilder.HasPostgresEnum<RequisitionStatus>();
        modelBuilder.HasPostgresEnum<CalibrationStatus>();
        modelBuilder.HasPostgresEnum<InspectionStatus>();
        modelBuilder.HasPostgresEnum<MaintenanceStatus>();
        modelBuilder.HasPostgresEnum<AssetStatus>();
        modelBuilder.HasPostgresEnum<DepreciationMethod>();
        modelBuilder.HasPostgresEnum<AssetMaintenanceStatus>();
        modelBuilder.HasPostgresEnum<DisposalMethod>();
        modelBuilder.HasPostgresEnum<DisposalStatus>();
        modelBuilder.HasPostgresEnum<VerificationStatus>();
        modelBuilder.HasPostgresEnum<VerificationCycleStatus>();
        modelBuilder.HasPostgresEnum<MaintenanceCategory>();
        modelBuilder.HasPostgresEnum<TicketPriority>();
        modelBuilder.HasPostgresEnum<TicketStatus>();
        modelBuilder.HasPostgresEnum<WorkOrderStatus>();
        modelBuilder.HasPostgresEnum<PMFrequency>();
        modelBuilder.HasPostgresEnum<EscalationLevel>();
        modelBuilder.HasPostgresEnum<VisitorType>();
        modelBuilder.HasPostgresEnum<VisitorStatus>();
        modelBuilder.HasPostgresEnum<VehiclePassType>();
        modelBuilder.HasPostgresEnum<VehiclePassStatus>();
        modelBuilder.HasPostgresEnum<EntryType>();
        modelBuilder.HasPostgresEnum<GuardStatus>();
        modelBuilder.HasPostgresEnum<ShiftStatus>();
        modelBuilder.HasPostgresEnum<IncidentType>();
        modelBuilder.HasPostgresEnum<IncidentSeverity>();
        modelBuilder.HasPostgresEnum<IncidentStatus>();
        modelBuilder.HasPostgresEnum<EmergencyContactType>();
        modelBuilder.HasPostgresEnum<EmergencyAlertType>();
        modelBuilder.HasPostgresEnum<EmergencyAlertSeverity>();
        modelBuilder.HasPostgresEnum<FireEquipmentStatus>();
        modelBuilder.HasPostgresEnum<LostFoundType>();
        modelBuilder.HasPostgresEnum<LostFoundStatus>();
        modelBuilder.HasPostgresEnum<RouteType>();
        modelBuilder.HasPostgresEnum<VehicleStatus>();
        modelBuilder.HasPostgresEnum<DriverStatus>();
        modelBuilder.HasPostgresEnum<VehicleMaintenanceStatus>();
        modelBuilder.HasPostgresEnum<HostelBlockType>();
        modelBuilder.HasPostgresEnum<RoomType>();
        modelBuilder.HasPostgresEnum<RoomStatus>();
        modelBuilder.HasPostgresEnum<AllocationStatus>();
        modelBuilder.HasPostgresEnum<InspectionType>();
        modelBuilder.HasPostgresEnum<GateLogType>();
        modelBuilder.HasPostgresEnum<HostelDisciplineStatus>();
        modelBuilder.HasPostgresEnum<UtilityType>();
        modelBuilder.HasPostgresEnum<MealType>();
        modelBuilder.HasPostgresEnum<RiskCategory>();
        modelBuilder.HasPostgresEnum<MeetingStatus>();
        modelBuilder.HasPostgresEnum<InterventionType>();
        modelBuilder.HasPostgresEnum<InterventionOutcome>();
        modelBuilder.HasPostgresEnum<CareerGoalStatus>();
        modelBuilder.HasPostgresEnum<AlertTriggerType>();
        modelBuilder.HasPostgresEnum<EmployerStatus>();
        modelBuilder.HasPostgresEnum<JobType>();
        modelBuilder.HasPostgresEnum<InterviewType>();
        modelBuilder.HasPostgresEnum<PlacementType>();
        modelBuilder.HasPostgresEnum<AegisCore.Domain.DriveType>();
        modelBuilder.HasPostgresEnum<DriveStatus>();
        modelBuilder.HasPostgresEnum<ReferralStatus>();
        modelBuilder.HasPostgresEnum<CareerEventType>();
        modelBuilder.HasPostgresEnum<CareerEventStatus>();
        modelBuilder.HasPostgresEnum<LeadSource>();
        modelBuilder.HasPostgresEnum<LeadStatus>();
        modelBuilder.HasPostgresEnum<ActivityType>();
        modelBuilder.HasPostgresEnum<LeadTaskStatus>();
        modelBuilder.HasPostgresEnum<CampaignType>();
        modelBuilder.HasPostgresEnum<CampaignStatus>();
        modelBuilder.HasPostgresEnum<VisitStatus>();
        modelBuilder.HasPostgresEnum<CallDirection>();
        modelBuilder.HasPostgresEnum<CallDisposition>();
        modelBuilder.HasPostgresEnum<MessageSender>();
        modelBuilder.HasPostgresEnum<RefreshFrequency>();

        modelBuilder.Entity<PolicyConfig>(entity =>
        {
            entity.HasIndex(e => new { e.Module, e.Key, e.EffectiveFrom }).IsUnique();
            entity.Property(e => e.Module).HasMaxLength(100);
            entity.Property(e => e.Key).HasMaxLength(200);
            entity.Property(e => e.ValueType).HasMaxLength(20);
        });

        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.HasIndex(e => e.IsCurrent);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasIndex(e => e.IsCurrent);
            entity.HasOne(e => e.AcademicYear).WithMany(e => e.Semesters).HasForeignKey(e => e.AcademicYearId);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<AcademicHoliday>(entity =>
        {
            entity.HasIndex(e => new { e.AcademicYearId, e.StartDate });
            entity.HasOne(e => e.AcademicYear).WithMany().HasForeignKey(e => e.AcademicYearId);
            entity.HasOne(e => e.Semester).WithMany(e => e.Holidays).HasForeignKey(e => e.SemesterId);
        });

        modelBuilder.Entity<CrossDeptPermissionRequest>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId);
        });

        modelBuilder.Entity<LicenseState>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.LockedAt);
            entity.Property(e => e.UniversityName).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.LastHeartbeatStatus).HasMaxLength(20);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortName).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(300).IsRequired();
            entity.Property(e => e.ShortName).HasMaxLength(20).IsRequired();
            entity.Property(e => e.LicenseMode).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<WizardSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.CompletedAt });
            entity.HasIndex(e => e.BootstrapTokenHash).IsUnique().HasFilter("\"BootstrapTokenHash\" <> ''");
            entity.Property(e => e.BootstrapTokenHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("text").IsRequired();
            entity.Property(e => e.LicenseMode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.LicenseKey).HasMaxLength(50);
            entity.Property(e => e.StepData).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedByIp).HasMaxLength(64);
        });

        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RawHtml).HasColumnType("text").IsRequired();
            entity.Property(e => e.ExtractedPalette).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ExtractedFonts).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.LogoUrl).HasMaxLength(2048);
        });

        modelBuilder.Entity<WizardAdminCredential>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.SessionId, e.Email }).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(300).IsRequired();
            entity.Property(e => e.RoleName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<AdmissionApplication>(entity =>
        {
            entity.HasIndex(e => e.Cnic).IsUnique();
            entity.HasIndex(e => new { e.ProgramId, e.QuotaType, e.ApplicationStatus });
            entity.HasMany(e => e.Documents).WithOne(e => e.Application).HasForeignKey(e => e.ApplicationId);
        });

        modelBuilder.Entity<ApplicationDocument>(entity =>
        {
            entity.HasIndex(e => new { e.ApplicationId, e.DocumentType });
        });

        modelBuilder.Entity<MeritList>(entity =>
        {
            entity.HasIndex(e => new { e.ProgramId, e.SemesterId, e.QuotaType, e.RoundNumber }).IsUnique();
            entity.HasMany(e => e.Entries).WithOne(e => e.MeritList).HasForeignKey(e => e.MeritListId);
        });

        modelBuilder.Entity<MeritListEntry>(entity =>
        {
            entity.HasIndex(e => new { e.MeritListId, e.MeritRank }).IsUnique();
            entity.HasOne(e => e.Application).WithMany().HasForeignKey(e => e.ApplicationId);
        });

        modelBuilder.Entity<SeatAllocation>(entity =>
        {
            entity.HasIndex(e => e.MeritListEntryId).IsUnique();
            entity.HasIndex(e => e.ApplicationId);
        });

        modelBuilder.Entity<AssessmentScheme>(entity =>
        {
            entity.HasIndex(e => new { e.CourseId, e.SemesterId }).IsUnique();
            entity.HasMany(e => e.Components).WithOne(e => e.Scheme).HasForeignKey(e => e.SchemeId);
        });

        modelBuilder.Entity<AssessmentComponent>(entity =>
        {
            entity.HasIndex(e => new { e.SchemeId, e.OrderIndex }).IsUnique();
        });

        modelBuilder.Entity<CourseMark>(entity =>
        {
            entity.HasIndex(e => new { e.CourseId, e.StudentId, e.AssessmentComponentId }).IsUnique();
            entity.HasOne(e => e.AssessmentComponent).WithMany().HasForeignKey(e => e.AssessmentComponentId);
        });

        modelBuilder.Entity<MarkModeration>(entity =>
        {
            entity.HasIndex(e => new { e.CourseId, e.SemesterId }).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<GradeScale>(entity =>
        {
            entity.HasMany(e => e.Grades).WithOne(e => e.GradeScale).HasForeignKey(e => e.GradeScaleId);
        });

        modelBuilder.Entity<GradeBand>(entity =>
        {
            entity.HasIndex(e => new { e.GradeScaleId, e.LetterGrade }).IsUnique();
        });

        modelBuilder.Entity<CourseGrade>(entity =>
        {
            entity.HasIndex(e => new { e.CourseId, e.StudentId }).IsUnique();
            entity.HasIndex(e => new { e.StudentId, e.SemesterId });
            entity.HasIndex(e => e.ResultStatus);
        });

        modelBuilder.Entity<ExamRegistration>(entity =>
        {
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });

        modelBuilder.Entity<Transcript>(entity =>
        {
            entity.HasIndex(e => e.VerificationHash).IsUnique();
        });

        modelBuilder.Entity<CisUpload>(entity =>
        {
            entity.HasIndex(e => new { e.CourseId, e.ProcessingStatus });
            entity.HasIndex(e => e.HumanReviewStatus);
        });

        modelBuilder.Entity<PayrollRun>(entity =>
        {
            entity.HasIndex(e => e.Month).IsUnique();
            entity.HasMany(e => e.Payslips).WithOne(e => e.PayrollRun).HasForeignKey(e => e.PayrollRunId);
        });

        modelBuilder.Entity<FinancePayrollPosting>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Period }).IsUnique();
            entity.HasMany(e => e.Lines).WithOne(e => e.Posting).HasForeignKey(e => e.FinancePayrollPostingId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinancePayrollPostingLine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FinancePayrollPostingId, e.EmployeeId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.CostCenterId });
        });
        modelBuilder.Entity<FinancePayrollBankInstruction>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EmployeeId }).IsUnique();
            entity.Property(e => e.EncryptedAccountReference).HasColumnType("bytea");
        });
        modelBuilder.Entity<FinancePayrollPaymentFile>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.PayrollPostingId, e.Format }).IsUnique();
            entity.Property(e => e.PqcSignature).HasColumnType("bytea");
        });
        modelBuilder.Entity<FinanceGratuityAssumption>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.EffectiveFrom }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_FinanceGratuityAssumptions_Rates", "\"DiscountRatePercent\" >= 0 AND \"SalaryGrowthRatePercent\" >= 0 AND \"TurnoverRatePercent\" >= 0 AND \"BenefitDivisor\" > 0 AND \"RetirementHorizonYears\" > 0"));
        });
        modelBuilder.Entity<FinanceGratuityValuation>(entity => entity.HasIndex(e => new { e.TenantId, e.ValuationDate }).IsUnique());
        modelBuilder.Entity<FinanceBankAccount>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.AccountReference }).IsUnique();
            entity.HasMany(e => e.FeedTransactions).WithOne(e => e.BankAccount).HasForeignKey(e => e.FinanceBankAccountId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceBankFeedTransaction>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FinanceBankAccountId, e.ExternalId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.BookingDate });
        });
        modelBuilder.Entity<FinanceGrant>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.DonorName, e.Purpose });
            entity.HasMany(e => e.Awards).WithOne(e => e.Grant).HasForeignKey(e => e.GrantId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceScholarshipProgram>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.ClosesAt });
            entity.HasOne(e => e.Grant).WithMany().HasForeignKey(e => e.GrantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Applications).WithOne(e => e.Program).HasForeignKey(e => e.ProgramId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_FinanceScholarshipPrograms_Values", "\"CoverageValue\" > 0 AND \"DurationMonths\" > 0 AND \"ClosesAt\" > \"OpensAt\""));
        });
        modelBuilder.Entity<FinanceScholarshipApplication>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ProgramId, e.StudentId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.AppliedAt });
            entity.HasMany(e => e.Events).WithOne(e => e.Application).HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceScholarshipApplicationEvent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ApplicationId, e.OccurredAt });
        });
        modelBuilder.Entity<FinanceScholarshipAward>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.StudentId, e.AwardedAt });
            entity.HasIndex(e => new { e.TenantId, e.GrantId });
            entity.HasIndex(e => new { e.TenantId, e.ApplicationId }).IsUnique().HasFilter("\"ApplicationId\" IS NOT NULL");
        });
        modelBuilder.Entity<FinanceAssetAccountingEvent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.AssetId, e.OccurredAt });
            entity.HasIndex(e => e.JournalEntryId).IsUnique().HasFilter("\"JournalEntryId\" IS NOT NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_FinanceAssetAccountingEvents_Values", "\"PreviousCarryingValue\" >= 0 AND \"NewCarryingValue\" >= 0 AND \"Proceeds\" >= 0"));
        });
        modelBuilder.Entity<FinanceUtilityBill>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.AccountNumber, e.BillingPeriodStart, e.BillingPeriodEnd }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.DueDate });
            entity.HasMany(e => e.Allocations).WithOne(e => e.Bill).HasForeignKey(e => e.FinanceUtilityBillId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceUtilityAllocation>(entity => entity.HasIndex(e => new { e.TenantId, e.FinanceUtilityBillId, e.CostCenterId }).IsUnique());
        modelBuilder.Entity<FinanceInsurancePolicy>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.PolicyNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.RenewalDate, e.Status });
            entity.HasMany(e => e.Claims).WithOne(e => e.Policy).HasForeignKey(e => e.FinanceInsurancePolicyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceInsuranceClaim>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ClaimNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.FiledAt });
        });
        modelBuilder.Entity<FinanceInventoryItem>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ItemCode }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.OnHandQuantity });
            entity.HasMany(e => e.Movements).WithOne(e => e.Item).HasForeignKey(e => e.FinanceInventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceInventoryMovement>(entity => entity.HasIndex(e => new { e.TenantId, e.FinanceInventoryItemId, e.MovementDate }));
        modelBuilder.Entity<FinanceFleetCostPosting>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.TransportVehicleId, e.CostDate });
            entity.HasIndex(e => e.JournalEntryId).IsUnique();
        });
        modelBuilder.Entity<FinanceFleetTrip>(entity => entity.HasIndex(e => new { e.TenantId, e.TransportVehicleId, e.TripDate }));
        modelBuilder.Entity<FinanceFleetProfile>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.TransportVehicleId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.NextServiceAt, e.Status });
            entity.ToTable(t => t.HasCheckConstraint("CK_FinanceFleetProfiles_Service", "\"ModelYear\" >= 1900 AND \"ServiceIntervalDays\" > 0 AND \"ServiceIntervalKm\" > 0 AND \"CurrentOdometer\" >= 0"));
        });
        modelBuilder.Entity<FinanceInvestmentFund>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasMany(e => e.Holdings).WithOne(e => e.Fund).HasForeignKey(e => e.FinanceInvestmentFundId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceInvestmentHolding>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Issuer, e.InstrumentType });
            entity.HasIndex(e => new { e.TenantId, e.MaturityDate, e.Status });
            entity.HasMany(e => e.Transactions).WithOne(e => e.Holding).HasForeignKey(e => e.FinanceInvestmentHoldingId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceInvestmentTransaction>(entity => entity.HasIndex(e => new { e.TenantId, e.FinanceInvestmentHoldingId, e.TransactionDate }));
        modelBuilder.Entity<FinanceInvestmentBenchmark>(entity => entity.HasIndex(e => new { e.TenantId, e.Name, e.ObservationDate }).IsUnique());
        modelBuilder.Entity<FinanceRegulatorySchema>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code, e.Version }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Code, e.IsActive });
            entity.Property(e => e.DefinitionJson).HasColumnType("jsonb");
        });
        modelBuilder.Entity<FinanceRegulatorySubmission>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.RegulatorySchemaId, e.GeneratedAt });
            entity.Property(e => e.PqcSignature).HasColumnType("bytea");
            entity.Property(e => e.PqcPublicKey).HasColumnType("bytea");
        });
        modelBuilder.Entity<FinanceControlEvaluationBatch>(entity =>
        {
            entity.Property(e => e.Trigger).HasMaxLength(40);
            entity.Property(e => e.ReferenceType).HasMaxLength(80);
            entity.Property(e => e.ResultsJson).HasColumnType("jsonb");
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.HasIndex(e => new { e.TenantId, e.EvaluatedAt });
            entity.HasIndex(e => new { e.TenantId, e.ReferenceType, e.ReferenceId });
        });
        modelBuilder.Entity<FinanceSodRule>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IncompatibleActionsJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        });
        modelBuilder.Entity<FinanceAuditorEngagement>(entity =>
        {
            entity.Property(e => e.AuditorName).HasMaxLength(200);
            entity.Property(e => e.AuditorEmail).HasMaxLength(320);
            entity.Property(e => e.TokenHash).HasMaxLength(64);
            entity.Property(e => e.ScopesJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.ExpiresAt });
            entity.HasIndex(e => e.TokenHash).IsUnique();
        });
        modelBuilder.Entity<FinanceAuditSample>(entity =>
        {
            entity.Property(e => e.PopulationType).HasMaxLength(40);
            entity.Property(e => e.SeedHash).HasMaxLength(64);
            entity.Property(e => e.SelectedReferencesJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.TenantId, e.AuditorEngagementId, e.CreatedAt });
        });
        modelBuilder.Entity<FinanceAuditEvidencePackage>(entity =>
        {
            entity.Property(e => e.Framework).HasMaxLength(40);
            entity.Property(e => e.StorageKey).HasMaxLength(500);
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.Property(e => e.PqcAlgorithm).HasMaxLength(80);
            entity.Property(e => e.PqcKeyId).HasMaxLength(200);
            entity.Property(e => e.PqcSignature).HasColumnType("bytea");
            entity.Property(e => e.PqcPublicKey).HasColumnType("bytea");
            entity.HasIndex(e => new { e.TenantId, e.GeneratedAt });
        });
        modelBuilder.Entity<FinanceTaxRule>(entity => entity.HasIndex(e => new { e.TenantId, e.TaxType, e.Jurisdiction, e.VendorCategory, e.EffectiveFrom }));
        modelBuilder.Entity<FinanceWithholdingRecord>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.PaymentReference }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.FilingPeriod, e.TaxType });
        });

        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.HasIndex(e => new { e.PayrollRunId, e.EmployeeId }).IsUnique();
            entity.HasIndex(e => e.EmployeeId);
        });

        modelBuilder.Entity<FeeStructure>(entity =>
        {
            entity.HasIndex(e => new { e.AcademicYearId, e.ProgramId, e.Semester, e.FeeType }).IsUnique();
            entity.HasMany(e => e.Components).WithOne(e => e.FeeStructure).HasForeignKey(e => e.FeeStructureId);
        });

        modelBuilder.Entity<FeeComponent>(entity =>
        {
            entity.HasIndex(e => new { e.FeeStructureId, e.OrderIndex }).IsUnique();
        });

        modelBuilder.Entity<StudentFeeDue>(entity =>
        {
            entity.HasIndex(e => new { e.StudentId, e.SemesterId }).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<StudentFeePayment>(entity =>
        {
            entity.HasIndex(e => e.FeeDueId);
            entity.HasIndex(e => e.ReferenceNumber);
        });

        modelBuilder.Entity<FinanceVoucher>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.VoucherNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt });
            entity.HasMany(e => e.Lines).WithOne(e => e.Voucher).HasForeignKey(e => e.FinanceVoucherId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Approvals).WithOne().HasForeignKey(e => e.FinanceVoucherId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PqcSignature).HasColumnType("bytea");
        });

        modelBuilder.Entity<FinanceVoucherLine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FinanceVoucherId });
            entity.HasIndex(e => new { e.TenantId, e.ChartOfAccountId });
            entity.ToTable(t => t.HasCheckConstraint("CK_FinanceVoucherLines_OneSided", "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)"));
        });

        modelBuilder.Entity<FinanceVoucherApproval>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FinanceVoucherId, e.ApproverUserId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.FinanceVoucherId, e.ApproverRole });
        });

        modelBuilder.Entity<FinanceVoucherApprovalRule>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Currency, e.MinimumAmount }).IsUnique();
            entity.Property(e => e.RequiredRolesJson).HasColumnType("jsonb");
            entity.ToTable(t => t.HasCheckConstraint("CK_FinanceVoucherApprovalRules_Range", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" > \"MinimumAmount\") AND \"MinimumApprovals\" > 0"));
        });
        modelBuilder.Entity<FinanceVoucherAttachment>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FinanceVoucherId });
            entity.HasIndex(e => new { e.TenantId, e.Sha256 });
        });
        modelBuilder.Entity<FinanceVoucherReversal>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.OriginalVoucherId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.ReversalVoucherId }).IsUnique();
        });

        modelBuilder.Entity<FinanceVoucherSequence>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.VoucherType, e.FiscalYear }).IsUnique());

        modelBuilder.Entity<FeeDemand>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.DemandNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.StudentId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.Status, e.DueDate });
            entity.HasMany(e => e.Installments).WithOne(e => e.Demand).HasForeignKey(e => e.FeeDemandId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeeInstallment>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.FeeDemandId, e.Sequence }).IsUnique());

        modelBuilder.Entity<FinancePayment>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.WebhookId }).IsUnique().HasFilter("\"WebhookId\" IS NOT NULL");
            entity.HasIndex(e => new { e.TenantId, e.FeeDemandId, e.Status });
        });

        modelBuilder.Entity<PaymentGatewayConfiguration>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.GatewaySlug }).IsUnique());

        modelBuilder.Entity<PaymentGatewayEvent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
            entity.HasIndex(e => new { e.TenantId, e.GatewaySlug, e.ReferenceId });
            entity.HasIndex(e => new { e.TenantId, e.GatewaySlug, e.CreatedAt });
        });

        modelBuilder.Entity<ExchangeRateSnapshot>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.BaseCurrency, e.QuoteCurrency, e.ValidForDate }).IsUnique());

        modelBuilder.Entity<FinanceForeignCurrencyTransaction>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ForeignCurrency, e.CreatedAt });
            entity.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceId });
            entity.ToTable(table => table.HasCheckConstraint("CK_FinanceForeignCurrencyTransactions_CurrenciesDiffer", "\"ForeignCurrency\" <> \"FunctionalCurrency\""));
        });

        modelBuilder.Entity<FinancePaymentPlanConfiguration>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.PlanType }).IsUnique());

        modelBuilder.Entity<FinancePaymentPlan>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.StudentId, e.Status });
            entity.HasIndex(e => new { e.TenantId, e.FeeDemandId }).IsUnique();
            entity.HasOne(e => e.Demand).WithOne(e => e.PaymentPlan).HasForeignKey<FinancePaymentPlan>(e => e.FeeDemandId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinanceSecurityDeposit>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.StudentId, e.Status });
            entity.HasMany(e => e.Deductions).WithOne(e => e.Deposit).HasForeignKey(e => e.SecurityDepositId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinanceSecurityDepositDeduction>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.SecurityDepositId, e.CreatedAt }));

        modelBuilder.Entity<FinanceAuditSeal>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.SealedAt });
            entity.Property(e => e.MerkleRoot).HasColumnType("bytea");
            entity.Property(e => e.Signature).HasColumnType("bytea");
            entity.Property(e => e.PublicKey).HasColumnType("bytea");
        });

        modelBuilder.Entity<FinancePqcKey>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Purpose, e.KeyId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Purpose, e.IsActive, e.ExpiresAt });
            entity.Property(e => e.PublicKey).HasColumnType("bytea");
        });

        modelBuilder.Entity<FinancePqcKeyCeremony>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Purpose, e.Status });
            entity.HasMany(e => e.Approvals).WithOne(e => e.Ceremony).HasForeignKey(e => e.CeremonyId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_FinancePqcKeyCeremonies_Threshold", "\"RequiredApprovals\" BETWEEN 2 AND 20"));
        });
        modelBuilder.Entity<FinancePqcKeyCeremonyApproval>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.CeremonyId, e.ApprovedByUserId }).IsUnique();
            entity.Property(e => e.ApprovalDigest).HasColumnType("bytea");
        });
        modelBuilder.Entity<FinanceLedgerSignature>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.JournalEntryId }).IsUnique();
            entity.Property(e => e.Signature).HasColumnType("bytea");
            entity.Property(e => e.PublicKey).HasColumnType("bytea");
        });

        modelBuilder.Entity<TenantFinanceConfiguration>(entity =>
        {
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.Property(e => e.AdditionalCurrenciesJson).HasColumnType("jsonb");
            entity.Property(e => e.TaxSettingsJson).HasColumnType("jsonb");
            entity.ToTable(t => t.HasCheckConstraint("CK_TenantFinanceConfigurations_LibraryThreshold", "\"LibraryCapitalizationThreshold\" >= 0"));
        });

        modelBuilder.Entity<FinanceAccountingStandardPolicy>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.StandardCode, e.PolicyCode, e.EffectiveFrom }).IsUnique();
            entity.Property(e => e.ValueJson).HasColumnType("jsonb");
        });
        modelBuilder.Entity<FinanceReportingEntity>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.ParentEntityId, e.IsActive });
        });
        modelBuilder.Entity<FinanceRestatementRule>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.SourceStandard, e.TargetStandard, e.IsActive });
            entity.ToTable(table => table.HasCheckConstraint("CK_FinanceRestatementRules_Factor", "\"Factor\" <> 0 AND \"SourceAccountId\" <> \"TargetAccountId\""));
        });
        modelBuilder.Entity<FinanceRestatementRun>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.SourceStandard, e.TargetStandard, e.CreatedAt });
            entity.HasMany(e => e.Lines).WithOne(e => e.Run).HasForeignKey(e => e.RestatementRunId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceRestatementMemoLine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.RestatementRunId, e.ChartOfAccountId });
            entity.ToTable(table => table.HasCheckConstraint("CK_FinanceRestatementMemoLines_OneSide", "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)"));
        });
        modelBuilder.Entity<FinanceConsolidationGroup>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasMany(e => e.Members).WithOne(e => e.Group).HasForeignKey(e => e.ConsolidationGroupId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceConsolidationMember>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.ConsolidationGroupId, e.ReportingEntityId, e.EffectiveFrom }).IsUnique());
        modelBuilder.Entity<FinanceEliminationRule>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.ConsolidationGroupId, e.IsActive }));
        modelBuilder.Entity<FinanceConsolidationRun>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ConsolidationGroupId, e.CreatedAt });
            entity.HasMany(e => e.Lines).WithOne(e => e.Run).HasForeignKey(e => e.ConsolidationRunId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceConsolidationMemoLine>(entity =>
            entity.HasIndex(e => new { e.TenantId, e.ConsolidationRunId, e.ChartOfAccountId }));

        modelBuilder.Entity<FinancePettyCashFund>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.DepartmentId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            entity.HasMany(e => e.Expenses).WithOne(e => e.Fund).HasForeignKey(e => e.FinancePettyCashFundId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Counts).WithOne(e => e.Fund).HasForeignKey(e => e.FinancePettyCashFundId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_FinancePettyCashFunds_PositiveFloat", "\"FloatAmount\" > 0 AND \"CurrentBalance\" >= 0 AND \"CurrentBalance\" <= \"FloatAmount\""));
        });
        modelBuilder.Entity<FinancePettyCashExpense>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FinancePettyCashFundId, e.SpentAt });
            entity.ToTable(table => table.HasCheckConstraint("CK_FinancePettyCashExpenses_PositiveAmount", "\"Amount\" > 0"));
        });
        modelBuilder.Entity<FinancePettyCashCount>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.FinancePettyCashFundId, e.CountedAt });
            entity.ToTable(table => table.HasCheckConstraint("CK_FinancePettyCashCounts_NonNegativeBalances", "\"SystemBalance\" >= 0 AND \"PhysicalBalance\" >= 0"));
        });
        modelBuilder.Entity<FinanceLibraryAcquisition>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Status, e.AcquiredAt });
            entity.ToTable(table => table.HasCheckConstraint("CK_FinanceLibraryAcquisitions_NonNegativeAmounts", "\"Cost\" >= 0 AND \"FairValue\" >= 0 AND \"AccumulatedAmortization\" >= 0"));
        });
        modelBuilder.Entity<FinanceLibraryFine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.StudentId, e.Status });
            entity.ToTable(table => table.HasCheckConstraint("CK_FinanceLibraryFines_PositiveAmount", "\"Amount\" > 0"));
        });

        modelBuilder.Entity<FinanceVendor>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.LegalName });
            entity.HasIndex(e => new { e.TenantId, e.Status, e.Category });
            entity.HasMany(e => e.Contacts).WithOne(e => e.Vendor).HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.StandingPurchaseOrders).WithOne(e => e.Vendor).HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceVendorContact>(entity => entity.HasIndex(e => new { e.TenantId, e.VendorId }));
        modelBuilder.Entity<FinanceStandingPurchaseOrder>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Number }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.EndsOn });
            entity.HasMany(e => e.Spend).WithOne(e => e.StandingPurchaseOrder).HasForeignKey(e => e.StandingPurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceStandingPurchaseOrderSpend>(entity => entity.HasIndex(e => new { e.TenantId, e.StandingPurchaseOrderId, e.SpentAt }));
        modelBuilder.Entity<FinancePurchaseOrder>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Number }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.VendorId, e.Status });
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(e => e.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<FinanceVendor>().WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_FinancePurchaseOrders_PositiveTotal", "\"TotalAmount\" > 0"));
        });
        modelBuilder.Entity<FinancePurchaseOrderLine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.PurchaseOrderId });
            entity.ToTable(t => t.HasCheckConstraint("CK_FinancePurchaseOrderLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"LineTotal\" = \"Quantity\" * \"UnitPrice\""));
        });
        modelBuilder.Entity<FinanceGoodsReceipt>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Number }).IsUnique();
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(e => e.GoodsReceiptId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<FinancePurchaseOrder>().WithMany().HasForeignKey(e => e.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceGoodsReceiptLine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.PurchaseOrderLineId });
            entity.HasOne<FinancePurchaseOrderLine>().WithMany().HasForeignKey(e => e.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_FinanceGoodsReceiptLines_Amounts", "\"QuantityAccepted\" >= 0 AND \"QuantityRejected\" >= 0 AND (\"QuantityAccepted\" + \"QuantityRejected\") > 0"));
        });
        modelBuilder.Entity<FinanceVendorInvoice>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.VendorId, e.InvoiceNumber }).IsUnique();
            entity.HasMany(e => e.Lines).WithOne().HasForeignKey(e => e.VendorInvoiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<FinancePurchaseOrder>().WithMany().HasForeignKey(e => e.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<FinanceVendor>().WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceVendorInvoiceLine>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.VendorInvoiceId });
            entity.HasOne<FinancePurchaseOrderLine>().WithMany().HasForeignKey(e => e.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceThreeWayMatch>(entity => entity.HasIndex(e => new { e.TenantId, e.VendorInvoiceId }).IsUnique());
        modelBuilder.Entity<FinanceVendorContract>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ContractNumber }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Status, e.EndsOn });
            entity.HasOne<FinanceVendor>().WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinanceVendorDocument>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.VendorId, e.UploadedAt });
            entity.HasOne<FinanceVendor>().WithMany().HasForeignKey(e => e.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PqcSignature).HasColumnType("bytea");
        });

        modelBuilder.Entity<CostCenter>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<ProcurementRequisition>(entity =>
        {
            entity.HasIndex(e => e.RequisitionNumber).IsUnique();
            entity.HasMany(e => e.LineItems).WithOne(e => e.Requisition).HasForeignKey(e => e.RequisitionId);
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.HasIndex(e => e.VendorName);
        });

        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasIndex(e => new { e.UniversityId, e.SubdomainName }).IsUnique();
            entity.HasIndex(e => e.ReplicationLagHours);
        });

        modelBuilder.Entity<GradeOverrideRequest>(entity =>
        {
            entity.HasIndex(e => new { e.StudentId, e.CourseId, e.SemesterId });
            entity.HasIndex(e => new { e.ApprovalStatus, e.ApprovalThreshold });
        });

        modelBuilder.Entity<SectionOverrideRequest>(entity =>
        {
            entity.HasIndex(e => new { e.StudentId, e.CourseId, e.Status });
        });

        modelBuilder.Entity<AnonymousFeedbackSubmission>(entity =>
        {
            entity.HasIndex(e => new { e.CourseId, e.FacultyId });
            entity.HasIndex(e => e.SubmittedAt);
        });

        modelBuilder.Entity<CourseReEnrollment>(entity =>
        {
            entity.HasIndex(e => new { e.StudentId, e.CourseId, e.TargetSemesterId }).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<CurriculumVersionRecord>(entity =>
        {
            entity.HasIndex(e => new { e.ProgramId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => new { e.ProgramId, e.YearEffective }).IsUnique();
            entity.HasMany(e => e.Courses).WithOne(e => e.CurriculumVersion).HasForeignKey(e => e.CurriculumVersionId);
            entity.HasMany(e => e.Prerequisites).WithOne(e => e.CurriculumVersion).HasForeignKey(e => e.CurriculumVersionId);
        });

        modelBuilder.Entity<CurriculumCourse>(entity =>
        {
            entity.HasIndex(e => new { e.CurriculumVersionId, e.CourseId, e.Semester }).IsUnique();
            entity.Property(e => e.TotalCreditHours).HasComputedColumnSql("\"TheoryCreditHours\" + \"LabCreditHours\"", stored: true);
        });

        modelBuilder.Entity<CurriculumPrerequisite>(entity =>
        {
            entity.HasIndex(e => new { e.CurriculumVersionId, e.CourseId, e.PrerequisiteCourseId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_CurriculumPrerequisites_NoSelf", "\"CourseId\" <> \"PrerequisiteCourseId\""));
        });

        modelBuilder.Entity<CurriculumChangeHistory>(entity =>
        {
            entity.HasIndex(e => new { e.CurriculumVersionId, e.ChangedAt });
        });

        modelBuilder.Entity<OfflineAttendanceEvent>(entity =>
        {
            entity.HasIndex(e => e.ClientEventId).IsUnique();
            entity.HasIndex(e => new { e.CourseId, e.StudentId });
            entity.HasIndex(e => e.WasOfflineAt);
        });

        modelBuilder.Entity<UniversitySsoProfile>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.HasIndex(e => e.SsoProvider);
        });

        modelBuilder.Entity<UserSecondFactor>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        modelBuilder.Entity<SsoSession>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<ExamPaperBatch>(entity =>
        {
            entity.HasIndex(e => new { e.ExamId, e.Status });
        });

        modelBuilder.Entity<ExamPaperScan>(entity =>
        {
            entity.HasIndex(e => new { e.BatchId, e.StudentId }).IsUnique();
            entity.HasIndex(e => e.QrPayload).IsUnique();
        });

        modelBuilder.Entity<AgenticGradeRun>(entity =>
        {
            entity.HasIndex(e => new { e.ScanId, e.Status });
            entity.HasIndex(e => e.Confidence);
        });

        modelBuilder.Entity<HumanGradeReview>(entity =>
        {
            entity.HasIndex(e => new { e.GradeRunId, e.Status });
        });

        modelBuilder.Entity<PaperDestructionManifest>(entity =>
        {
            entity.HasIndex(e => e.BatchId).IsUnique();
            entity.HasIndex(e => e.ManifestHash).IsUnique();
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasIndex(e => e.PoNumber).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => new { e.Module, e.Action, e.Resource }).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        modelBuilder.Entity<PolicyConfig>().HasData(
            SeedPolicy("11111111-1111-1111-1111-111111111101", "admissions", "merit_change_max_percent", "5", "int", "maximum % a dept head can change merit unilaterally"),
            SeedPolicy("11111111-1111-1111-1111-111111111102", "admissions", "merit_override_requires_vc", "true", "bool", "VC approval required for merit overrides"),
            SeedPolicy("11111111-1111-1111-1111-111111111103", "admissions", "merit_override_requires_chancellor", "true", "bool", "Chancellor approval required for major merit overrides"),
            SeedPolicy("11111111-1111-1111-1111-111111111104", "examination", "grace_marks_max", "2", "int", "maximum grace marks"),
            SeedPolicy("11111111-1111-1111-1111-111111111105", "examination", "attendance_hold_threshold_percent", "75", "int", "attendance threshold for exam eligibility"),
            SeedPolicy("11111111-1111-1111-1111-111111111106", "academic", "add_drop_window_days", "7", "int", "course add/drop window"),
            SeedPolicy("11111111-1111-1111-1111-111111111107", "finance", "late_fee_grace_days", "3", "int", "late fee grace period"),
            SeedPolicy("11111111-1111-1111-1111-111111111108", "license", "heartbeat_time", "08:00", "string", "daily license heartbeat time"),
            SeedPolicy("11111111-1111-1111-1111-111111111109", "license", "lockout_on_first_failure", "true", "bool", "lock system on first failed heartbeat"));
        modelBuilder.HasPostgresEnum<ETLJobStatus>();
        modelBuilder.HasPostgresEnum<DashboardType>();
        modelBuilder.HasPostgresEnum<VisualizationType>();
        modelBuilder.HasPostgresEnum<ReportCategory>();
        modelBuilder.HasPostgresEnum<ScheduleFrequency>();
        modelBuilder.HasPostgresEnum<ExportFormat>();
        modelBuilder.HasPostgresEnum<AlertCondition>();
        modelBuilder.HasPostgresEnum<WorkflowDefinitionStatus>();
        modelBuilder.HasPostgresEnum<WorkflowInstanceStatus>();
        modelBuilder.HasPostgresEnum<WorkflowStepStatus>();
        modelBuilder.HasPostgresEnum<WorkflowNodeType>();
        modelBuilder.HasPostgresEnum<WorkflowTriggerType>();
        modelBuilder.HasPostgresEnum<ApprovalType>();
        modelBuilder.HasPostgresEnum<ApprovalStatus>();
        modelBuilder.HasPostgresEnum<NotificationChannel>();
        modelBuilder.HasPostgresEnum<NotificationStatus>();
        modelBuilder.HasPostgresEnum<ConditionOperator>();
        modelBuilder.HasPostgresEnum<LogicalOperator>();
        modelBuilder.HasPostgresEnum<ApiKeyTier>();
        modelBuilder.HasPostgresEnum<ApiKeyStatus>();
        modelBuilder.HasPostgresEnum<ApiPermission>();
        modelBuilder.HasPostgresEnum<WebhookStatus>();
        modelBuilder.HasPostgresEnum<ExtensionStatus>();
        modelBuilder.HasPostgresEnum<ExtensionCategory>();
        modelBuilder.HasPostgresEnum<ExtensionPricingModel>();
        modelBuilder.HasPostgresEnum<InstallationStatus>();
        modelBuilder.HasPostgresEnum<UserStatus>();
        modelBuilder.HasPostgresEnum<AuthProvider>();
        modelBuilder.HasPostgresEnum<SessionStatus>();
        modelBuilder.HasPostgresEnum<BackupType>();
        modelBuilder.HasPostgresEnum<BackupStatus>();
        modelBuilder.HasPostgresEnum<ReplicaStatus>();
        modelBuilder.HasPostgresEnum<FailoverMode>();
        modelBuilder.HasPostgresEnum<BackupRetentionTier>();
        modelBuilder.HasPostgresEnum<CampusStatus>();
        modelBuilder.HasPostgresEnum<AegisCore.Domain.CalendarType>();
        modelBuilder.HasPostgresEnum<AegisCore.Domain.GradingScheme>();
        modelBuilder.HasPostgresEnum<TransferRequestStatus>();
        modelBuilder.HasPostgresEnum<ComplianceFrameworkStatus>();
        modelBuilder.HasPostgresEnum<ControlStatus>();
        modelBuilder.HasPostgresEnum<ControlReviewFrequency>();
        modelBuilder.HasPostgresEnum<AuditFindingSeverity>();
        modelBuilder.HasPostgresEnum<AuditFindingStatus>();
        modelBuilder.HasPostgresEnum<CAPStatus>();
        modelBuilder.HasPostgresEnum<LegalHoldStatus>();
        modelBuilder.HasPostgresEnum<DataCategory>();
        modelBuilder.HasPostgresEnum<CountryPackStatus>();
        modelBuilder.HasPostgresEnum<TextDirection>();
        modelBuilder.HasPostgresEnum<TaxType>();
        modelBuilder.HasPostgresEnum<DegreeRequirementCategory>();
        modelBuilder.HasPostgresEnum<AuditStatus>();
        modelBuilder.HasPostgresEnum<RequirementType>();
        modelBuilder.HasPostgresEnum<CurriculumVersion>();
        modelBuilder.HasPostgresEnum<FormStatus>();
        modelBuilder.HasPostgresEnum<FormFieldType>();
        modelBuilder.HasPostgresEnum<SubmissionStatus>();
        modelBuilder.HasPostgresEnum<SensorType>();
        modelBuilder.HasPostgresEnum<SensorStatus>();
        modelBuilder.HasPostgresEnum<OccupancyState>();
        modelBuilder.HasPostgresEnum<EnergyReadingType>();
        modelBuilder.HasPostgresEnum<ConfigurationDomain>();
        modelBuilder.HasPostgresEnum<ConfigEntryStatus>();
        modelBuilder.HasPostgresEnum<ConfigEnvironment>();
        modelBuilder.HasPostgresEnum<RuleStatus>();
        modelBuilder.HasPostgresEnum<RuleActionType>();
        modelBuilder.HasPostgresEnum<RuleDomain>();
        modelBuilder.HasPostgresEnum<RuleMatchStrategy>();
        modelBuilder.HasPostgresEnum<NotificationTemplateChannel>();
        modelBuilder.HasPostgresEnum<DeliveryStatus>();
        modelBuilder.HasPostgresEnum<FlagType>();
        modelBuilder.HasPostgresEnum<FlagScope>();
        modelBuilder.HasPostgresEnum<JobCategory>();
        modelBuilder.HasPostgresEnum<JobExecutionStatus>();
        modelBuilder.HasPostgresEnum<QueueStatus>();
        modelBuilder.HasPostgresEnum<EventStatus>();
        modelBuilder.HasPostgresEnum<DiagnosticSeverity>();
        modelBuilder.HasPostgresEnum<ServiceStatus>();
        modelBuilder.HasPostgresEnum<DependencyType>();
        modelBuilder.HasPostgresEnum<HealthScoreGrade>();
        modelBuilder.HasPostgresEnum<IntegrationStatus>();
        modelBuilder.HasPostgresEnum<IntegrationType>();
        modelBuilder.HasPostgresEnum<BenchmarkTier>();
        modelBuilder.HasPostgresEnum<BenchmarkStatus>();
        modelBuilder.HasPostgresEnum<AIProviderType>();
        modelBuilder.HasPostgresEnum<AIModelCapability>();
        modelBuilder.HasPostgresEnum<PromptApprovalStatus>();
        modelBuilder.HasPostgresEnum<GuardrailAction>();
        modelBuilder.HasPostgresEnum<GuardrailSeverity>();
        modelBuilder.HasPostgresEnum<LicenseEdition>();
        modelBuilder.HasPostgresEnum<LicenseStatus>();
        modelBuilder.HasPostgresEnum<UpdateChannel>();
        modelBuilder.HasPostgresEnum<UpdateStatus>();
        modelBuilder.HasPostgresEnum<DefaulterStatus>();
        modelBuilder.HasPostgresEnum<FeeDefaulterLevel>();
        modelBuilder.HasPostgresEnum<MisconductSeverity>();
        modelBuilder.HasPostgresEnum<MisconductStatus>();
        modelBuilder.HasPostgresEnum<SanctionType>();
        modelBuilder.HasPostgresEnum<WorkloadUnitType>();
        modelBuilder.HasPostgresEnum<VerificationStatus>();
        modelBuilder.HasPostgresEnum<RegistrationStatus>();
        modelBuilder.HasPostgresEnum<CurriculumApprovalStatus>();
        modelBuilder.HasPostgresEnum<TunnelStatus>();
        modelBuilder.HasPostgresEnum<HardcodingSeverity>();
        modelBuilder.HasPostgresEnum<HardcodingType>();
        modelBuilder.HasPostgresEnum<LoginEvent>();
        modelBuilder.HasPostgresEnum<LoginProvider>();
        modelBuilder.HasPostgresEnum<DeviceType>();

        // Department
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique().HasFilter(null);
        });

        // Student
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasIndex(e => e.UniversityId).IsUnique();
            entity.Property(e => e.DirectoryOptOut).HasDefaultValue(false);
        });

        // Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(c => c.CloMappingJson).HasColumnType("jsonb");
            entity.Property(c => c.PloMappingJson).HasColumnType("jsonb");
        });

        // Enrollment
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });

        // ExamSeating
        modelBuilder.Entity<ExamSeating>(entity =>
        {
            entity.HasIndex(e => new { e.ExamScheduleId, e.SeatNumber }).IsUnique();
        });

        // ScalingEvent
        modelBuilder.Entity<ScalingEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ReplicaName).HasMaxLength(100);
            entity.Property(e => e.TotalReplicas).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
        });

        // ChartOfAccount
        modelBuilder.Entity<ChartOfAccount>(entity =>
        {
            entity.HasIndex(e => e.AccountCode).IsUnique();
        });

        // JournalEntry
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasIndex(e => e.EntryNumber).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.IsPosted, e.IsSandbox, e.EntryDate });
        });

        // Employee
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<GrievanceTicket>(entity =>
        {
            entity.HasIndex(e => e.TicketNumber).IsUnique();
            entity.HasIndex(e => e.SubmitterId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<GrievanceResponse>(entity =>
        {
            entity.HasIndex(e => e.TicketId);
            entity.HasOne(e => e.Ticket).WithMany(e => e.Responses).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NoticeBoardItem>(entity =>
        {
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.ExpiryDate);
        });

        modelBuilder.Entity<TeamsSyncLog>(entity =>
        {
            entity.HasIndex(e => e.TimetableEntryId);
            entity.HasIndex(e => e.SyncStatus);
        });

        modelBuilder.Entity<LmsSyncLog>(entity =>
        {
            entity.HasIndex(e => e.CourseId);
            entity.HasIndex(e => new { e.CourseId, e.SyncType });
        });

        modelBuilder.Entity<PowerBiReport>(entity =>
        {
            entity.HasIndex(e => e.TargetRole);
            entity.HasIndex(e => e.PowerBiReportId).IsUnique();
        });

        modelBuilder.Entity<AlumniProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.AlumniId).IsUnique();
        });

        modelBuilder.Entity<AlumniDonation>(entity =>
        {
            entity.HasIndex(e => e.ReceiptNumber).IsUnique();
            entity.HasIndex(e => e.AlumniProfileId);
        });

        modelBuilder.Entity<StudentSociety>(entity => entity.HasIndex(e => e.SocietyName));
        modelBuilder.Entity<SocietyMembership>(entity => entity.HasIndex(e => new { e.SocietyId, e.StudentId }).IsUnique());
        modelBuilder.Entity<StudentActivity>(entity => entity.HasIndex(e => e.ScheduledDate));
        modelBuilder.Entity<ActivityParticipation>(entity => entity.HasIndex(e => new { e.ActivityId, e.StudentId }).IsUnique());
        modelBuilder.Entity<StudentHealthRecord>(entity => entity.HasIndex(e => e.StudentId).IsUnique());
        modelBuilder.Entity<MedicalVisit>(entity => entity.HasIndex(e => e.StudentId));
        modelBuilder.Entity<SickLeaveApplication>(entity => entity.HasIndex(e => new { e.StudentId, e.Status }));

        // ResearchGrant
        modelBuilder.Entity<ResearchGrant>(entity =>
        {
        });

        // ResearchPublication
        modelBuilder.Entity<ResearchPublication>(entity =>
        {
        });

        // ResearchPatent
        modelBuilder.Entity<ResearchPatent>(entity =>
        {
            entity.HasIndex(e => e.PatentNumber).IsUnique().HasFilter("\"PatentNumber\" IS NOT NULL");
        });

        // UniversityMission
        modelBuilder.Entity<UniversityMission>(entity =>
        {
        });

        // ProgramEducationalObjective
        modelBuilder.Entity<ProgramEducationalObjective>(entity =>
        {
        });

        // ProgramOutcome
        modelBuilder.Entity<ProgramOutcome>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // CourseLearningOutcome
        modelBuilder.Entity<CourseLearningOutcome>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // CLOPOMapping
        modelBuilder.Entity<CLOPOMapping>(entity =>
        {
        });

        // AccreditationReport
        modelBuilder.Entity<AccreditationReport>(entity =>
        {
        });

        // AccreditationBody
        modelBuilder.Entity<AccreditationBodyEntity>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // AccreditationCriterion
        modelBuilder.Entity<AccreditationCriterion>(entity =>
        {
        });

        // FacultyCV
        modelBuilder.Entity<FacultyCV>(entity =>
        {
        });

        // StudentProfile
        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(e => e.StudentId).IsUnique();
        });

        // LibraryBook
        modelBuilder.Entity<LibraryBook>(entity =>
        {
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.Author);
            entity.HasIndex(e => e.Category);
        });

        // BookCopy
        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.HasIndex(e => e.Barcode).IsUnique();
        });

        // LibraryMember
        modelBuilder.Entity<LibraryMember>(entity =>
        {
        });

        // LibraryVendor
        modelBuilder.Entity<LibraryVendor>(entity =>
        {
        });

        // BookOrder
        modelBuilder.Entity<BookOrder>(entity =>
        {
        });

        // ResearchLab
        modelBuilder.Entity<ResearchLab>(entity =>
        {
            entity.HasIndex(e => e.LabCode).IsUnique();
        });

        // LabEquipment
        modelBuilder.Entity<LabEquipment>(entity =>
        {
        });

        // Campus
        modelBuilder.Entity<CampusEntity>(entity =>
        {
            entity.HasIndex(e => e.CampusCode).IsUnique();
        });

        // AssetCategory
        modelBuilder.Entity<AssetCategory>(entity =>
        {
        });

        // Asset
        modelBuilder.Entity<Asset>(entity =>
        {
        });

        // VerificationCycle
        modelBuilder.Entity<VerificationCycle>(entity =>
        {
        });

        // MaintenanceTicket
        modelBuilder.Entity<MaintenanceTicket>(entity =>
        {
            entity.HasIndex(e => e.TicketNumber).IsUnique();
        });

        // WorkOrder
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasIndex(e => e.WorkOrderNumber).IsUnique();
        });

        // MaintenanceContractor
        modelBuilder.Entity<MaintenanceContractor>(entity =>
        {
        });

        // SLADefinition
        modelBuilder.Entity<SLADefinition>(entity =>
        {
            entity.HasIndex(e => new { e.Category, e.Priority }).IsUnique();
        });

        // Visitor
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasIndex(e => e.CNIC);
        });

        // VehiclePass
        modelBuilder.Entity<VehiclePass>(entity =>
        {
            entity.HasIndex(e => e.VehicleNumber);
        });

        // SecurityGuard
        modelBuilder.Entity<SecurityGuard>(entity =>
        {
            entity.HasIndex(e => e.BadgeNumber).IsUnique();
        });

        // SecurityIncident
        modelBuilder.Entity<SecurityIncident>(entity =>
        {
            entity.HasIndex(e => e.IncidentNumber).IsUnique();
        });

        // EmergencyContact
        modelBuilder.Entity<EmergencyContact>(entity =>
        {
            entity.HasIndex(e => e.Phone);
        });

        // EmergencyAlert
        modelBuilder.Entity<EmergencyAlert>(entity =>
        {
            entity.HasIndex(e => e.SentAt);
        });

        // FireEquipment
        modelBuilder.Entity<FireEquipment>(entity =>
        {
            entity.HasIndex(e => e.EquipmentCode).IsUnique();
        });

        // LostFoundItem
        modelBuilder.Entity<LostFoundItem>(entity =>
        {
            entity.HasIndex(e => e.Status);
        });

        // TransportRoute
        modelBuilder.Entity<TransportRoute>(entity =>
        {
        });

        // TransportVehicle
        modelBuilder.Entity<TransportVehicle>(entity =>
        {
        });

        // TransportDriver
        modelBuilder.Entity<TransportDriver>(entity =>
        {
            entity.HasIndex(e => e.LicenseNumber);
        });

        // HostelBlock
        modelBuilder.Entity<HostelBlock>(entity =>
        {
        });

        // AlertRule
        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.HasIndex(e => e.RuleName).IsUnique();
        });

        // Employer
        modelBuilder.Entity<Employer>(entity =>
        {
            entity.HasIndex(e => e.ContactEmail);
        });

        // StudentResume
        modelBuilder.Entity<StudentResume>(entity =>
        {
            entity.HasIndex(e => e.StudentId).IsUnique();
        });

        // CareerEvent
        modelBuilder.Entity<CareerEvent>(entity =>
        {
            entity.HasIndex(e => e.EventDate);
        });

        // AdmissionLead
        modelBuilder.Entity<AdmissionLead>(entity =>
        {
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Phone);
        });

        // MarketingCampaign
        modelBuilder.Entity<MarketingCampaign>(entity =>
        {
        });

        // DataWarehouseTable
        modelBuilder.Entity<DataWarehouseTable>(entity =>
        {
            entity.HasIndex(e => e.TableName).IsUnique();
        });

        // Dashboard
        modelBuilder.Entity<Dashboard>(entity =>
        {
        });

        // RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        });

        // UserRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
        });
    }

    private static PolicyConfig SeedPolicy(string id, string module, string policyName, string policyValue, string valueType, string description)
    {
        var policy = new PolicyConfig
        {
            Id = Guid.Parse(id), Module = module, Value = policyValue, ValueType = valueType,
            Description = description, CreatedBy = Guid.Empty,
            EffectiveFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
        typeof(PolicyConfig).GetProperty(nameof(PolicyConfig.Key))!.SetValue(policy, policyName);
        return policy;
    }
}

/// <summary>
/// Represents a scaling event in the system.
/// </summary>
public class ScalingEvent : ITenantScoped
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of the scaling event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Optional replica name.
    /// </summary>
    public string? ReplicaName { get; set; }

    /// <summary>
    /// Total number of replicas after the event.
    /// </summary>
    public int TotalReplicas { get; set; }

    /// <summary>
    /// Timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }
}
