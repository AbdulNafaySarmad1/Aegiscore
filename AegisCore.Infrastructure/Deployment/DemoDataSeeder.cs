using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Contracts.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment;

public class DemoDataSeeder : ISeedRunner
{
    public int Order => 6;
    private readonly IRepository<AchievementBadge> _badgeRepo;
    private readonly IRepository<MenuItem> _menuRepo;
    private readonly IRepository<CafeteriaUnit> _unitRepo;
    private readonly IRepository<DefaulterRiskScore> _riskRepo;
    private readonly IRepository<ReadinessScore> _readinessRepo;
    private readonly IRepository<BoardAlert> _alertRepo;
    private readonly IRepository<StrategicPlanGoal> _goalRepo;
    private readonly IRepository<VisitingFacultyProfile> _facultyRepo;
    private readonly IRepository<GradeAppeal> _appealRepo;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        IRepository<AchievementBadge> badgeRepo,
        IRepository<MenuItem> menuRepo,
        IRepository<CafeteriaUnit> unitRepo,
        IRepository<DefaulterRiskScore> riskRepo,
        IRepository<ReadinessScore> readinessRepo,
        IRepository<BoardAlert> alertRepo,
        IRepository<StrategicPlanGoal> goalRepo,
        IRepository<VisitingFacultyProfile> facultyRepo,
        IRepository<GradeAppeal> appealRepo,
        ILogger<DemoDataSeeder> logger)
    {
        _badgeRepo = badgeRepo; _menuRepo = menuRepo;
        _unitRepo = unitRepo; _riskRepo = riskRepo; _readinessRepo = readinessRepo;
        _alertRepo = alertRepo; _goalRepo = goalRepo; _facultyRepo = facultyRepo;
        _appealRepo = appealRepo; _logger = logger;
    }

    public async Task SeedAsync(Guid tenantId, TenantSeedData data, CancellationToken ct = default)
    {
        // Achievement badges
        var badges = new[] {
            new AchievementBadge { Id = Guid.NewGuid(), Name = "Dean's List", Description = "CGPA 3.5+ in a semester", Category = BadgeCategory.Academic, Tier = AwardTier.Gold, TenantId = tenantId },
            new AchievementBadge { Id = Guid.NewGuid(), Name = "Perfect Attendance", Description = "100% attendance in a semester", Category = BadgeCategory.Engagement, Tier = AwardTier.Silver, TenantId = tenantId },
            new AchievementBadge { Id = Guid.NewGuid(), Name = "First Submission", Description = "First to submit among class", Category = BadgeCategory.Engagement, Tier = AwardTier.Bronze, TenantId = tenantId },
            new AchievementBadge { Id = Guid.NewGuid(), Name = "Graduate", Description = "Graduation achieved", Category = BadgeCategory.Lifecycle, Tier = AwardTier.Platinum, TenantId = tenantId },
            new AchievementBadge { Id = Guid.NewGuid(), Name = "Student Council", Description = "Elected to student council", Category = BadgeCategory.Leadership, Tier = AwardTier.Gold, TenantId = tenantId },
            new AchievementBadge { Id = Guid.NewGuid(), Name = "Research Published", Description = "Co-authored research with faculty", Category = BadgeCategory.Academic, Tier = AwardTier.Diamond, TenantId = tenantId },
        };
        foreach (var b in badges) await _badgeRepo.AddAsync(b, ct);
        _logger.LogInformation("Seeded {Count} achievement badges", badges.Length);

        // Cafeteria units and menu items
        var unit = new CafeteriaUnit { Id = Guid.NewGuid(), Name = "Main Cafeteria", Location = "Building A, Ground Floor", Capacity = 200, OperatingHours = "07:00-18:00", TenantId = tenantId };
        await _unitRepo.AddAsync(unit, ct);
        var menuItems = new[] {
            new MenuItem { Id = Guid.NewGuid(), ItemName = "Chicken Biryani", Category = "meal", Price = 250m, Calories = 650, IsHalalCertified = true, CafeteriaUnitId = unit.Id, TenantId = tenantId },
            new MenuItem { Id = Guid.NewGuid(), ItemName = "Daal Chawal", Category = "meal", Price = 150m, Calories = 450, IsHalalCertified = true, CafeteriaUnitId = unit.Id, TenantId = tenantId },
            new MenuItem { Id = Guid.NewGuid(), ItemName = "Shawarma", Category = "meal", Price = 200m, Calories = 520, IsHalalCertified = true, CafeteriaUnitId = unit.Id, TenantId = tenantId },
            new MenuItem { Id = Guid.NewGuid(), ItemName = "Samosa (2 pcs)", Category = "snack", Price = 60m, Calories = 180, IsHalalCertified = true, CafeteriaUnitId = unit.Id, TenantId = tenantId },
            new MenuItem { Id = Guid.NewGuid(), ItemName = "Mango Lassi", Category = "beverage", Price = 100m, Calories = 220, IsHalalCertified = true, CafeteriaUnitId = unit.Id, TenantId = tenantId },
            new MenuItem { Id = Guid.NewGuid(), ItemName = "Gulab Jamun", Category = "dessert", Price = 80m, Calories = 300, IsHalalCertified = true, CafeteriaUnitId = unit.Id, TenantId = tenantId },
        };
        foreach (var m in menuItems) await _menuRepo.AddAsync(m, ct);
        _logger.LogInformation("Seeded cafeteria unit and {Count} menu items", menuItems.Length);

        // Fee defaulter risk scores
        var riskScores = new[] {
            new DefaulterRiskScore { Id = Guid.NewGuid(), StudentId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Semester = "Spring 2026", ProbabilityScore = 0.12, Tier = DefaulterRiskTier.Green, PredictionConfidence = 0.91, PredictedAt = DateTime.UtcNow, TenantId = tenantId },
            new DefaulterRiskScore { Id = Guid.NewGuid(), StudentId = Guid.Parse("00000000-0000-0000-0000-000000000002"), Semester = "Spring 2026", ProbabilityScore = 0.45, Tier = DefaulterRiskTier.Yellow, PredictionConfidence = 0.85, PredictedAt = DateTime.UtcNow, TenantId = tenantId },
            new DefaulterRiskScore { Id = Guid.NewGuid(), StudentId = Guid.Parse("00000000-0000-0000-0000-000000000003"), Semester = "Spring 2026", ProbabilityScore = 0.72, Tier = DefaulterRiskTier.Orange, PredictionConfidence = 0.82, PredictedAt = DateTime.UtcNow, TenantId = tenantId },
            new DefaulterRiskScore { Id = Guid.NewGuid(), StudentId = Guid.Parse("00000000-0000-0000-0000-000000000004"), Semester = "Spring 2026", ProbabilityScore = 0.91, Tier = DefaulterRiskTier.Red, PredictionConfidence = 0.94, PredictedAt = DateTime.UtcNow, TenantId = tenantId },
        };
        foreach (var r in riskScores) await _riskRepo.AddAsync(r, ct);
        _logger.LogInformation("Seeded {Count} defaulter risk scores", riskScores.Length);

        // Accreditation readiness
        var readiness = new ReadinessScore { Id = Guid.NewGuid(), FrameworkId = Guid.Parse("00000000-0000-0000-0000-000000000001"), FrameworkName = "HEC General", OverallScore = 78.5, Status = ReadinessStatus.Good, NextAuditDate = DateTime.UtcNow.AddMonths(4), DaysUntilAudit = 120, ComplianceVelocity = 2.3, TenantId = tenantId };
        await _readinessRepo.AddAsync(readiness, ct);
        _logger.LogInformation("Seeded accreditation readiness score: {Score}%", readiness.OverallScore);

        // Board alerts
        var alerts = new[] {
            new BoardAlert { Id = Guid.NewGuid(), Severity = "warning", Category = "financial", Message = "Fee collection below 80% target: Q2 collections at 72% of semester demand. Review payment plans.", RecommendedAction = "Activate defaulter intervention pipeline", ResponsibleOfficer = "CFO", TenantId = tenantId },
            new BoardAlert { Id = Guid.NewGuid(), Severity = "warning", Category = "academic", Message = "CGPA decline in Engineering: average CGPA dropped 0.3 points semester-over-semester.", RecommendedAction = "Investigate assessment difficulty and faculty changes", ResponsibleOfficer = "Dean of Engineering", TenantId = tenantId },
            new BoardAlert { Id = Guid.NewGuid(), Severity = "warning", Category = "compliance", Message = "HEC visit scheduled in 120 days: readiness score at 78.5%, below the 85% target.", RecommendedAction = "Close 3 priority gaps before audit", ResponsibleOfficer = "Registrar", TenantId = tenantId },
        };
        foreach (var a in alerts) await _alertRepo.AddAsync(a, ct);
        _logger.LogInformation("Seeded {Count} board alerts", alerts.Length);

        // Strategic plan goals
        var goals = new[] {
            new StrategicPlanGoal { Id = Guid.NewGuid(), Title = "Increase enrollment by 15%", Description = "Target 3,500 students by 2027", CurrentValue = 3100, TargetValue = 3500, PercentAchieved = 68, TargetDate = DateTime.UtcNow.AddMonths(18), StartDate = DateTime.UtcNow.AddMonths(-6), Status = StrategicGoalStatus.OnTrack, TenantId = tenantId },
            new StrategicPlanGoal { Id = Guid.NewGuid(), Title = "Achieve HEC W-category", Description = "Minimum 85% readiness score required", CurrentValue = 78.5, TargetValue = 85, PercentAchieved = 82, TargetDate = DateTime.UtcNow.AddMonths(12), StartDate = DateTime.UtcNow.AddMonths(-3), Status = StrategicGoalStatus.AtRisk, TenantId = tenantId },
            new StrategicPlanGoal { Id = Guid.NewGuid(), Title = "Launch 3 new MS programs", Description = "MS Data Science, MS AI, MS Cybersecurity", CurrentValue = 1, TargetValue = 3, PercentAchieved = 33, TargetDate = DateTime.UtcNow.AddMonths(6), StartDate = DateTime.UtcNow.AddMonths(-2), Status = StrategicGoalStatus.OffTrack, TenantId = tenantId },
            new StrategicPlanGoal { Id = Guid.NewGuid(), Title = "Faculty PhD ratio to 60%", Description = "Currently at 48%", CurrentValue = 48, TargetValue = 60, PercentAchieved = 42, TargetDate = DateTime.UtcNow.AddMonths(24), StartDate = DateTime.UtcNow.AddMonths(-12), Status = StrategicGoalStatus.OnTrack, TenantId = tenantId },
        };
        foreach (var g in goals) await _goalRepo.AddAsync(g, ct);
        _logger.LogInformation("Seeded {Count} strategic plan goals", goals.Length);

        // Visiting faculty
        var faculty = new[] {
            new VisitingFacultyProfile { Id = Guid.NewGuid(), FirstName = "Ahmed", LastName = "Khan", Cnic = "35202-1234567-1", Email = "ahmed.khan@example.com", HighestQualification = "PhD Computer Science", Institution = "LUMS", YearsOfTeachingExperience = 12, ProfileStatus = "ready_to_deploy", HecEligibilityStatus = "eligible", TenantId = tenantId },
            new VisitingFacultyProfile { Id = Guid.NewGuid(), FirstName = "Fatima", LastName = "Shah", Cnic = "35202-7654321-2", Email = "fatima.shah@example.com", HighestQualification = "MS Data Science", Institution = "NUST", YearsOfTeachingExperience = 6, ProfileStatus = "ready_to_deploy", HecEligibilityStatus = "eligible", TenantId = tenantId },
        };
        foreach (var f in faculty) await _facultyRepo.AddAsync(f, ct);
        _logger.LogInformation("Seeded {Count} visiting faculty profiles", faculty.Length);

        // Grade appeals
        var appeals = new[] {
            new GradeAppeal { Id = Guid.NewGuid(), StudentId = Guid.Parse("00000000-0000-0000-0000-000000000001"), CourseId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Semester = "Fall 2025", SubmittedGrade = "C", AppealGrounds = "Submitted assignment not included in grading. Assessment component missing.", GroundType = AppealGroundType.MissingSubmission, GroundStrength = EvidenceStrength.Strong, Status = AppealStatus.UnderReview, FacultyRespondentId = Guid.Parse("00000000-0000-0000-0000-000000000010"), IsEligible = true, TenantId = tenantId },
            new GradeAppeal { Id = Guid.NewGuid(), StudentId = Guid.Parse("00000000-0000-0000-0000-000000000002"), CourseId = Guid.Parse("00000000-0000-0000-0000-000000000002"), Semester = "Fall 2025", SubmittedGrade = "D", AppealGrounds = "Medical emergency during final exam. Certificate attached.", GroundType = AppealGroundType.ExtenuatingCircumstance, GroundStrength = EvidenceStrength.Conclusive, Status = AppealStatus.CommitteeReview, FacultyRespondentId = Guid.Parse("00000000-0000-0000-0000-000000000011"), IsEligible = true, TenantId = tenantId },
        };
        foreach (var a in appeals) await _appealRepo.AddAsync(a, ct);
        _logger.LogInformation("Seeded {Count} grade appeals", appeals.Length);

        _logger.LogInformation("Demo data seeding complete — dashboards will show live content on first boot");
    }
}
