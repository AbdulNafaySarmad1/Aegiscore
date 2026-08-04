using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.DegreeAudit;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.DegreeAudit;

public class DegreeAuditService : IDegreeAuditService
{
    private readonly IDegreeProgramRepository _programRepo;
    private readonly IDegreeRequirementRepository _requirementRepo;
    private readonly IDegreeAuditRepository _auditRepo;
    private readonly ICreditTransferRepository _creditRepo;
    private readonly IRepository<Student> _studentRepo;
    private readonly IRepository<Enrollment> _enrollmentRepo;
    private readonly IRepository<Course> _courseRepo;
    private readonly ILogger<DegreeAuditService> _logger;

    public DegreeAuditService(
        IDegreeProgramRepository programRepo,
        IDegreeRequirementRepository requirementRepo,
        IDegreeAuditRepository auditRepo,
        ICreditTransferRepository creditRepo,
        IRepository<Student> studentRepo,
        IRepository<Enrollment> enrollmentRepo,
        IRepository<Course> courseRepo,
        ILogger<DegreeAuditService> logger)
    {
        _programRepo = programRepo;
        _requirementRepo = requirementRepo;
        _auditRepo = auditRepo;
        _creditRepo = creditRepo;
        _studentRepo = studentRepo;
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DegreeAuditEntity>> RunAuditAsync(Guid studentId, Guid programId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepo.GetByIdAsync(studentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var program = await _programRepo.GetWithRequirementsAsync(programId, cancellationToken)
            ?? throw new InvalidOperationException("Program not found.");

        var requirements = await _requirementRepo.GetByProgramAsync(programId, cancellationToken);
        var enrollments = await _enrollmentRepo.FindAsync(e => e.StudentId == studentId, cancellationToken);
        var transfers = await _creditRepo.GetByStudentAsync(studentId, cancellationToken);

        var allCourses = await _courseRepo.GetAllAsync(cancellationToken);
        var completedCourses = enrollments
            .Where(e => e.Grade != null && e.Grade != "F")
            .Select(e => new { Enrollment = e, Course = allCourses.FirstOrDefault(c => c.Id == e.CourseId) })
            .Where(x => x.Course != null)
            .ToList<dynamic>();

        var totalCreditsEarned = completedCourses.Sum(x => x.Course!.CreditHours)
            + transfers.Where(t => t.IsApproved).Sum(t => t.CreditHours);

        var cgpa = enrollments
            .Where(e => e.GpaPoints.HasValue && e.Grade != "F")
            .Average(e => (double?)e.GpaPoints) ?? 0;

        var audits = new List<DegreeAuditEntity>();

        foreach (var requirement in requirements)
        {
            var existingAudit = await _auditRepo.GetByStudentAndRequirementAsync(studentId, requirement.Id, cancellationToken);

            var audit = existingAudit ?? new DegreeAuditEntity
            {
                Id = Guid.NewGuid(), StudentId = studentId, ProgramId = programId,
                RequirementId = requirement.Id, TenantId = Guid.NewGuid() /* student.TenantId */
            };

            EvaluateRequirement(audit, requirement, completedCourses, transfers, totalCreditsEarned, (decimal)cgpa);
            audit.LastEvaluatedAt = DateTime.UtcNow;

            if (existingAudit == null)
                await _auditRepo.AddAsync(audit, cancellationToken);
            else
                await _auditRepo.UpdateAsync(audit, cancellationToken);

            audits.Add(audit);
        }

        _logger.LogInformation("Degree audit completed for student {StudentId}: {Satisfied}/{Total} requirements met",
            studentId, audits.Count(a => a.Status == AuditStatus.Satisfied), audits.Count);

        return audits;
    }

    public async Task<StudentProgressDto> GetStudentProgressAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepo.GetByIdAsync(studentId, cancellationToken)
            ?? throw new InvalidOperationException("Student not found.");

        var audits = await _auditRepo.GetByStudentAsync(studentId, cancellationToken);
        var program = audits.FirstOrDefault()?.Program;

        var enrollments = await _enrollmentRepo.FindAsync(e => e.StudentId == studentId, cancellationToken);
        var courses = await _courseRepo.GetAllAsync(cancellationToken);
        var transfers = await _creditRepo.GetByStudentAsync(studentId, cancellationToken);

        var cgpa = enrollments.Where(e => e.GpaPoints.HasValue && e.Grade != "F").Average(e => (double?)e.GpaPoints) ?? 0;
        var creditsEarned = enrollments
            .Where(e => e.Grade != null && e.Grade != "F")
            .Sum(e => courses.FirstOrDefault(c => c.Id == e.CourseId)?.CreditHours ?? 0)
            + transfers.Where(t => t.IsApproved).Sum(t => t.CreditHours);

        var creditsEnrolled = enrollments
            .Where(e => e.Grade == null)
            .Sum(e => courses.FirstOrDefault(c => c.Id == e.CourseId)?.CreditHours ?? 0);

        var satisfied = audits.Count(a => a.Status == AuditStatus.Satisfied || a.Status == AuditStatus.Waived);
        var inProgress = audits.Count(a => a.Status == AuditStatus.InProgress);
        var requiredCount = audits.Count(a => a.Requirement?.IsRequired == true);

        return new StudentProgressDto
        {
            StudentId = studentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            ProgramName = program?.Name ?? "Unknown",
            CurrentCGPA = Math.Round((decimal)cgpa, 2),
            RequiredCGPA = program?.MinCGPA ?? DefaultTenantConfigTemplate.MinCGPA,
            CreditsEarned = creditsEarned,
            CreditsRequired = program?.MinCreditHours ?? 120,
            CreditsEnrolled = creditsEnrolled,
            TotalRequirements = audits.Count,
            SatisfiedRequirements = satisfied,
            InProgressRequirements = inProgress,
            RemainingRequirements = requiredCount - satisfied,
            CompletionPercentage = audits.Count > 0 ? Math.Round((double)satisfied / audits.Count * 100, 1) : 0,
            IsEligibleForGraduation = audits.All(a => a.Status == AuditStatus.Satisfied || a.Status == AuditStatus.Waived)
                && creditsEarned >= (program?.MinCreditHours ?? 0)
                && (decimal)cgpa >= (program?.MinCGPA ?? 0),
            Requirements = audits.Select(a => new RequirementProgressDto
            {
                RequirementId = a.RequirementId,
                Name = a.Requirement?.Name ?? "",
                Category = a.Requirement?.Category.ToString() ?? "",
                Status = a.Status.ToString(),
                EarnedValue = a.EarnedValue,
                RequiredValue = a.RequiredValue,
                ProgressText = a.Status == AuditStatus.Satisfied ? "Completed" : a.Status == AuditStatus.InProgress ? "In Progress" : "Not Started"
            }).ToList()
        };
    }

    public async Task<WhatIfResultDto> RunWhatIfAsync(Guid studentId, List<Guid> proposedCourses, CancellationToken cancellationToken = default)
    {
        var allCourses = await _courseRepo.GetAllAsync(cancellationToken);
        var additionalCredits = proposedCourses.Sum(cId => allCourses.FirstOrDefault(c => c.Id == cId)?.CreditHours ?? 0);

        return new WhatIfResultDto
        {
            ProposedCourses = proposedCourses,
            AdditionalCreditsEarned = additionalCredits,
            RequirementsCleared = 0,
            ClearedRequirements = new List<string>(),
            RemainingRequirementsAfterAdd = 0,
            RemainingCreditsAfterAdd = 0,
            NewCompletionPercentage = 0
        };
    }

    public async Task<DegreeAuditEntity> WaiveRequirementAsync(Guid auditId, string reason, CancellationToken cancellationToken = default)
    {
        var audit = await _auditRepo.GetByIdAsync(auditId, cancellationToken)
            ?? throw new InvalidOperationException("Audit record not found.");

        audit.Status = AuditStatus.Waived;
        audit.Notes = reason;
        await _auditRepo.UpdateAsync(audit, cancellationToken);

        return audit;
    }

    public async Task<CreditTransfer> SubmitCreditTransferAsync(CreditTransfer transfer, CancellationToken cancellationToken)
    {
        await _creditRepo.AddAsync(transfer, cancellationToken);
        return transfer;
    }

    public async Task<CreditTransfer> ApproveCreditTransferAsync(Guid transferId, string approvedBy, CancellationToken cancellationToken)
    {
        var transfer = await _creditRepo.GetByIdAsync(transferId, cancellationToken)
            ?? throw new InvalidOperationException("Credit transfer not found.");

        transfer.IsApproved = true;
        transfer.ApprovedAt = DateTime.UtcNow;
        transfer.ApprovedBy = approvedBy;
        await _creditRepo.UpdateAsync(transfer, cancellationToken);

        return transfer;
    }

    private static void EvaluateRequirement(DegreeAuditEntity audit, DegreeRequirement requirement,
        List<dynamic> completedCourses, IReadOnlyList<CreditTransfer> transfers,
        int totalCreditsEarned, decimal cgpa)
    {
        switch (requirement.RequirementType)
        {
            case RequirementType.CourseCompletion:
                if (requirement.CourseId.HasValue)
                {
                    var completed = completedCourses.Any(x => x.Course.Id == requirement.CourseId.Value);
                    audit.Status = completed ? AuditStatus.Satisfied : AuditStatus.NotStarted;
                    audit.EarnedValue = completed ? 1 : 0;
                    audit.RequiredValue = 1;
                }
                break;

            case RequirementType.MinimumCredits:
            case RequirementType.ElectiveCredits:
                var credits = totalCreditsEarned;
                var minCredits = requirement.MinCredits ?? 0;
                audit.Status = credits >= minCredits ? AuditStatus.Satisfied
                    : credits > 0 ? AuditStatus.InProgress : AuditStatus.NotStarted;
                audit.EarnedValue = credits;
                audit.RequiredValue = minCredits;
                break;

            case RequirementType.MinimumCGPA:
                var minCGPA = requirement.MinGradePoints ?? DefaultTenantConfigTemplate.MinCGPA;
                audit.Status = cgpa >= minCGPA ? AuditStatus.Satisfied
                    : cgpa > 0 ? AuditStatus.InProgress : AuditStatus.NotStarted;
                audit.EarnedValue = cgpa;
                audit.RequiredValue = minCGPA;
                break;

            case RequirementType.CourseGroup:
                var allowedIds = JsonSerializer.Deserialize<List<Guid>>(requirement.AllowedCourseIdsJson) ?? new();
                var count = completedCourses.Count(x => allowedIds.Contains(x.Course.Id));
                var required = requirement.CourseCount ?? 0;
                audit.Status = count >= required ? AuditStatus.Satisfied
                    : count > 0 ? AuditStatus.InProgress : AuditStatus.NotStarted;
                audit.CompletedCount = count;
                audit.RequiredCount = required;
                break;

            default:
                audit.Status = AuditStatus.NotStarted;
                break;
        }
    }
}
