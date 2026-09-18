using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Finance;

/// <summary>
/// The read routes the module screens open with. Twenty-two portal screens asked for lists their
/// modules never served (52 of their 65 calls answered 404/405), swallowed the failure and drew an
/// empty state, so every one of them looked installed and idle. Each route here reads the module's
/// own rows through the tenant filter and answers in the shape the screen renders; the write
/// actions stay on the module controllers.
/// </summary>
public static class ModuleReadEndpoints
{
    public static IEndpointRouteBuilder MapModuleReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api").RequireAuthorization();

        // ── Achievements ──
        api.MapGet("/achievements/badges", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Set<AchievementBadge>().AsNoTracking().OrderBy(b => b.Category).ThenBy(b => b.Name)
            .Select(b => new { id = b.Id, name = b.Name, category = b.Category.ToString(), tier = b.Tier.ToString(), criteria = b.Description ?? "", imageUrl = b.ImageUrl ?? "", isActive = b.IsActive }).ToListAsync(ct))).WithTags("Achievements");
        api.MapGet("/achievements/recent", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from award in db.Set<StudentBadge>().AsNoTracking()
            join badge in db.Set<AchievementBadge>().AsNoTracking() on award.BadgeId equals badge.Id
            join student in db.Students.AsNoTracking() on award.StudentId equals student.Id
            orderby award.IssuedAt descending
            select new { studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, badgeId = badge.Id, badgeName = badge.Name, earnedAt = award.IssuedAt, triggerEvent = award.AchievementDetail ?? "" }).Take(100).ToListAsync(ct))).WithTags("Achievements");
        api.MapGet("/achievements/deans-list", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from entry in db.DeansListEntries.AsNoTracking()
            join student in db.Students.AsNoTracking() on entry.StudentId equals student.Id
            join semester in db.Semesters.AsNoTracking() on entry.SemesterId equals semester.Id into semesters from semester in semesters.DefaultIfEmpty()
            orderby entry.Cgpa descending
            select new { studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, gpa = entry.Cgpa, semester = semester != null ? semester.Name : "", entryType = entry.Status, processedAt = entry.CreatedAt }).Take(200).ToListAsync(ct))).WithTags("Achievements");
        api.MapGet("/achievements/leaderboard", async (AegisDbContext db, string? scope, CancellationToken ct) =>
        {
            var scopeName = string.IsNullOrWhiteSpace(scope) || scope == "all" ? null : scope;
            var query = db.LeaderboardEntries.AsNoTracking().Where(l => !l.IsOptedOut);
            if (scopeName is not null) query = query.Where(l => l.Scope.ToLower() == scopeName.ToLower());
            return Results.Ok(await (
                from entry in query
                join student in db.Students.AsNoTracking() on entry.StudentId equals student.Id
                orderby entry.Rank
                select new { rank = entry.Rank, studentId = student.RollNumber ?? student.Id.ToString(), studentName = entry.IsAnonymized ? "Student " + entry.Rank : student.FirstName + " " + student.LastName, totalScore = entry.Score, badgeCount = db.Set<StudentBadge>().Count(b => b.StudentId == entry.StudentId), topBadge = entry.Metric }).Take(100).ToListAsync(ct));
        }).WithTags("Achievements");

        // ── Transport ──
        api.MapGet("/transport/fleet", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from vehicle in db.TransportVehicles.AsNoTracking()
            join driver in db.TransportDrivers.AsNoTracking() on vehicle.Id equals driver.AssignedVehicleId into drivers from driver in drivers.DefaultIfEmpty()
            orderby vehicle.RegistrationNumber
            select new { id = vehicle.Id, registration = vehicle.RegistrationNumber, type = vehicle.Model ?? "", capacity = vehicle.Capacity, driver = driver != null ? driver.Name : "", status = vehicle.Status.ToString(), assignedRouteId = vehicle.AssignedRouteId,
                nextServiceDue = db.VehicleMaintenances.Where(m => m.TransportVehicleId == vehicle.Id && m.CompletedAt == null).OrderBy(m => m.ScheduledDate).Select(m => (DateTime?)m.ScheduledDate).FirstOrDefault(),
                lastServiceDate = db.VehicleMaintenances.Where(m => m.TransportVehicleId == vehicle.Id && m.CompletedAt != null).OrderByDescending(m => m.CompletedAt).Select(m => m.CompletedAt).FirstOrDefault() }).ToListAsync(ct))).WithTags("Transport");
        api.MapGet("/transport/compliance/alerts", async (AegisDbContext db, CancellationToken ct) =>
        {
            var soon = DateTime.UtcNow.AddDays(30);
            var licences = await db.TransportDrivers.AsNoTracking().Where(d => d.LicenseExpiry != null && d.LicenseExpiry <= soon)
                .Select(d => new { id = d.Id, subject = d.Name, type = "Driving licence", expiryDate = d.LicenseExpiry!.Value, description = "Driver " + d.Name + "'s licence " + (d.LicenseExpiry < DateTime.UtcNow ? "has expired." : "expires within 30 days.") }).ToListAsync(ct);
            var maintenance = await db.VehicleMaintenances.AsNoTracking().Where(m => m.CompletedAt == null && m.ScheduledDate <= soon)
                .Join(db.TransportVehicles.AsNoTracking(), m => m.TransportVehicleId, v => v.Id, (m, v) => new { id = m.Id, subject = v.RegistrationNumber, type = "Maintenance", expiryDate = m.ScheduledDate, description = (m.Description ?? "Scheduled maintenance") + (m.ScheduledDate < DateTime.UtcNow ? " is overdue." : " is due within 30 days.") }).ToListAsync(ct);
            return Results.Ok(licences.Concat(maintenance).Select(a => new { a.id, a.subject, a.type, a.expiryDate, a.description, severity = a.expiryDate < DateTime.UtcNow ? "Critical" : "Warning" }).OrderBy(a => a.expiryDate).ToList());
        }).WithTags("Transport");
        api.MapGet("/transport/fee-summary", async (AegisDbContext db, CancellationToken ct) =>
        {
            var registrations = await db.Set<TransportRegistration>().AsNoTracking().CountAsync(ct);
            var attendance = await db.Set<BusAttendance>().AsNoTracking().CountAsync(ct);
            return Results.Ok(new { studentsRegistered = registrations, boardingsRecorded = attendance, note = registrations == 0 ? "No students are registered for transport yet." : null });
        }).WithTags("Transport");

        // ── Convocations ──
        api.MapGet("/convocations", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Set<Convocation>().AsNoTracking().OrderByDescending(c => c.CeremonyDate)
            .Select(c => new { id = c.Id, title = c.Title, academicYear = c.AcademicYear, ceremonyDate = c.CeremonyDate, venue = c.Venue ?? "", guestTicketsPerStudent = c.GuestTicketsPerStudent, status = c.Status.ToString(),
                eligible = db.Set<ConvocationEligibility>().Count(e => e.ConvocationId == c.Id && e.Status.ToString() == "Eligible"), attendees = db.Set<ConvocationAttendance>().Count(a => a.ConvocationId == c.Id), certificates = db.Set<DegreeCertificate>().Count(d => d.ConvocationId == c.Id) }).ToListAsync(ct))).WithTags("Convocations");
        api.MapGet("/convocations/eligibility", async (AegisDbContext db, Guid? convocationId, CancellationToken ct) => Results.Ok(await (
            from e in db.Set<ConvocationEligibility>().AsNoTracking().Where(e => convocationId == null || e.ConvocationId == convocationId)
            join student in db.Students.AsNoTracking() on e.StudentId equals student.Id
            select new { id = e.Id, studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, convocationId = e.ConvocationId, status = e.Status.ToString(), blockingReason = e.BlockingReason, creditsCompleted = e.CreditsCompleted, cgpaMet = e.CgpaMet, duesCleared = e.DuesCleared, thesisApproved = e.ThesisApproved, libraryCleared = e.LibraryCleared, disciplinaryCleared = e.DisciplinaryCleared }).Take(500).ToListAsync(ct))).WithTags("Convocations");
        api.MapGet("/convocations/certificates", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from d in db.Set<DegreeCertificate>().AsNoTracking()
            join student in db.Students.AsNoTracking() on d.StudentId equals student.Id
            select new { id = d.Id, serialNumber = d.SerialNumber, studentName = student.FirstName + " " + student.LastName, studentId = student.RollNumber ?? student.Id.ToString(), degreeTitle = d.DegreeTitle ?? "", status = d.Status.ToString(), attestationStatus = d.AttestationStatus.ToString(), hecReference = d.HECReference }).Take(500).ToListAsync(ct))).WithTags("Convocations");
        api.MapGet("/convocations/attendees", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from a in db.Set<ConvocationAttendance>().AsNoTracking()
            join student in db.Students.AsNoTracking() on a.StudentId equals student.Id
            select new { id = a.Id, studentName = student.FirstName + " " + student.LastName, studentId = student.RollNumber ?? student.Id.ToString(), status = a.Status.ToString(), checkInTime = a.CheckInTime, guestCount = a.GuestCount, certificateCollected = a.CertificateCollected }).Take(500).ToListAsync(ct))).WithTags("Convocations");

        // ── Placement ──
        api.MapGet("/placement/partners", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Employers.AsNoTracking().OrderBy(e => e.Name)
            .Select(e => new { id = e.Id, name = e.Name, description = e.Description, contactEmail = e.ContactEmail, status = e.Status.ToString(), openings = db.JobPostings.Count(j => j.EmployerId == e.Id && j.Status.ToString() == "Open"), placed = db.PlacementRecords.Count(p => p.EmployerId == e.Id) }).ToListAsync(ct))).WithTags("Placement");
        api.MapGet("/placement/active", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from j in db.JobPostings.AsNoTracking()
            join e in db.Employers.AsNoTracking() on j.EmployerId equals e.Id
            orderby j.PostedAt descending
            select new { id = j.Id, title = j.Title, employer = e.Name, jobType = j.JobType.ToString(), location = j.Location, status = j.Status.ToString(), postedAt = j.PostedAt, expiresAt = j.ExpiresAt, applications = j.Applications.Count }).Take(200).ToListAsync(ct))).WithTags("Placement");
        api.MapGet("/placement/analytics", async (AegisDbContext db, CancellationToken ct) =>
        {
            var placements = await db.PlacementRecords.AsNoTracking().Select(p => new { p.Type, p.Salary, p.StartDate }).ToListAsync(ct);
            var graduates = await db.Students.AsNoTracking().CountAsync(s => s.EnrollmentStatus == AegisCore.Domain.EnrollmentStatus.Graduated, ct);
            var withSalary = placements.Where(p => p.Salary is > 0).Select(p => p.Salary!.Value).ToList();
            return Results.Ok(new
            {
                placed = placements.Count, graduates, openings = await db.JobPostings.CountAsync(j => j.Status.ToString() == "Open", ct), employers = await db.Employers.CountAsync(ct),
                placementRate = graduates == 0 ? (double?)null : Math.Round(100.0 * placements.Count / graduates, 1),
                medianSalary = withSalary.Count == 0 ? (decimal?)null : withSalary.OrderBy(s => s).ElementAt(withSalary.Count / 2),
                byType = placements.GroupBy(p => p.Type.ToString()).Select(g => new { type = g.Key, count = g.Count() }).OrderByDescending(g => g.count).ToList()
            });
        }).WithTags("Placement");

        // ── Grade distributions ──
        api.MapGet("/grade-distributions/courses", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from d in db.GradeDistributions.AsNoTracking()
            join course in db.Courses.AsNoTracking() on d.CourseId equals course.Id
            orderby d.GeneratedAt descending
            select new { id = d.Id, courseId = d.CourseId, courseCode = course.Code, courseTitle = course.Title, semester = d.Semester, mean = d.Mean, median = d.Median, stdDev = d.StdDev, passRate = d.PassRate, distinctionRate = d.DistinctionRate, failureRate = d.FailureRate, enrollmentCount = d.EnrollmentCount, curveShape = d.CurveShape.ToString(), letterDistributionJson = d.LetterDistributionJson, generatedAt = d.GeneratedAt }).Take(200).ToListAsync(ct))).WithTags("GradeDistributions");
        api.MapGet("/grade-distributions/anomalies", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from a in db.Set<DistributionAnomalyAlert>().AsNoTracking()
            join d in db.GradeDistributions.AsNoTracking() on a.DistributionId equals d.Id
            join course in db.Courses.AsNoTracking() on d.CourseId equals course.Id
            orderby a.CreatedAt descending
            select new { id = a.Id, courseCode = course.Code, courseTitle = course.Title, semester = d.Semester, anomalyType = a.AnomalyType.ToString(), description = a.Description, recommendedAction = a.RecommendedAction, isResolved = a.IsResolved, createdAt = a.CreatedAt }).Take(200).ToListAsync(ct))).WithTags("GradeDistributions");

        // ── Proctoring ──
        api.MapGet("/proctoring/sessions", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Set<ProctoringSession>().AsNoTracking().OrderByDescending(s => s.ScheduledStart)
            .Select(s => new { id = s.Id, examId = s.ExamId, mode = s.Mode.ToString(), durationMinutes = s.DurationMinutes, lockdownEnabled = s.LockdownEnabled, cameraRequired = s.CameraRequired, micRequired = s.MicRequired, sensitivity = s.Sensitivity, scheduledStart = s.ScheduledStart, actualStart = s.ActualStart, actualEnd = s.ActualEnd,
                students = db.Set<StudentProctoringRecord>().Count(r => r.SessionId == s.Id), flagged = db.Set<StudentProctoringRecord>().Count(r => r.SessionId == s.Id && r.CriticalCount > 0) }).Take(200).ToListAsync(ct))).WithTags("Proctoring");
        api.MapGet("/proctoring/integrity", async (AegisDbContext db, Guid? sessionId, CancellationToken ct) => Results.Ok(await (
            from r in db.Set<StudentProctoringRecord>().AsNoTracking().Where(r => sessionId == null || r.SessionId == sessionId)
            join student in db.Students.AsNoTracking() on r.StudentId equals student.Id
            orderby r.CriticalCount descending, r.WarningCount descending
            select new { id = r.Id, sessionId = r.SessionId, studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, integrityStatus = r.IntegrityStatus.ToString(), warningCount = r.WarningCount, criticalCount = r.CriticalCount, verificationStatus = r.VerificationStatus.ToString(), isEscalated = r.IsEscalated, escalatedAt = r.EscalatedAt }).Take(500).ToListAsync(ct))).WithTags("Proctoring");
        api.MapGet("/proctoring/events", async (AegisDbContext db, Guid? recordId, CancellationToken ct) => Results.Ok(await db.Set<ProctorEvent>().AsNoTracking().Where(e => recordId == null || e.RecordId == recordId).OrderByDescending(e => e.RecordedAt)
            .Select(e => new { id = e.Id, recordId = e.RecordId, severity = e.Severity.ToString(), eventType = e.EventType, description = e.Description, recordedAt = e.RecordedAt }).Take(200).ToListAsync(ct))).WithTags("Proctoring");

        // ── Visiting faculty ──
        api.MapGet("/visiting-faculty/list", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.VisitingFacultyProfiles.AsNoTracking().OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new { id = p.Id, name = p.FirstName + " " + p.LastName, institution = p.Institution, degreeTitle = p.DegreeTitle, specializations = p.SpecializationsCsv, yearsTeaching = p.YearsOfTeachingExperience, yearsIndustry = p.YearsOfIndustryExperience, hecEligibility = p.HecEligibilityStatus, status = p.ProfileStatus, documentsVerified = p.DocumentsVerified, referencesChecked = p.ReferencesChecked, perCreditHourRate = p.PerCreditHourRate }).ToListAsync(ct))).WithTags("VisitingFaculty");
        api.MapGet("/visiting-faculty/matches", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from m in db.Set<FacultySlotMatch>().AsNoTracking()
            join p in db.VisitingFacultyProfiles.AsNoTracking() on m.FacultyProfileId equals p.Id
            orderby m.OpenSlotId, m.Rank
            select new { id = m.Id, slotId = m.OpenSlotId, facultyName = p.FirstName + " " + p.LastName, compositeScore = m.CompositeScore, rank = m.Rank, isSelected = m.IsSelected, matchedAt = m.MatchedAt }).Take(300).ToListAsync(ct))).WithTags("VisitingFaculty");
        api.MapGet("/visiting-faculty/contracts", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from c in db.Set<VisitingFacultyContract>().AsNoTracking()
            join p in db.VisitingFacultyProfiles.AsNoTracking() on c.FacultyProfileId equals p.Id
            join course in db.Courses.AsNoTracking() on c.CourseId equals course.Id into courses from course in courses.DefaultIfEmpty()
            orderby c.StartDate descending
            select new { id = c.Id, facultyName = p.FirstName + " " + p.LastName, course = course != null ? course.Code + " " + course.Title : "", status = c.Status.ToString(), creditHours = c.CreditHours, compensationAmount = c.CompensationAmount, startDate = c.StartDate, endDate = c.EndDate, signedAt = c.SignedAt, rehireStatus = c.RehireStatus != null ? c.RehireStatus.ToString() : null, performanceRating = c.PerformanceRating }).Take(300).ToListAsync(ct))).WithTags("VisitingFaculty");

        // ── Cafeteria ──
        api.MapGet("/cafeteria/menu-items", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Set<MenuItem>().AsNoTracking().OrderBy(m => m.Category).ThenBy(m => m.ItemName)
            .Select(m => new { id = m.Id, name = m.ItemName, category = m.Category, price = m.Price, isHalalCertified = m.IsHalalCertified, allergens = m.AllergensCsv, isAvailable = m.IsAvailable }).ToListAsync(ct))).WithTags("Cafeteria");
        api.MapGet("/cafeteria/orders", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Set<PreOrder>().AsNoTracking().OrderByDescending(o => o.CreatedAt)
            .Select(o => new { id = o.Id, studentId = o.StudentId, status = o.Status, quantity = o.Quantity, totalAmount = o.TotalAmount, readyAt = o.ReadyAt, createdAt = o.CreatedAt }).Take(200).ToListAsync(ct))).WithTags("Cafeteria");
        api.MapGet("/cafeteria/inventory-alerts", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Set<CafeteriaInventory>().AsNoTracking().Where(i => i.QuantityOnHand <= i.ReorderPoint).OrderBy(i => i.IngredientName)
            .Select(i => new { id = i.Id, itemName = i.IngredientName, quantityOnHand = i.QuantityOnHand, reorderLevel = i.ReorderPoint, expiryDate = i.ExpiryDate }).ToListAsync(ct))).WithTags("Cafeteria");

        // ── Semester freezes ──
        api.MapGet("/freezes", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from f in db.Set<SemesterFreeze>().AsNoTracking()
            join student in db.Students.AsNoTracking() on f.StudentId equals student.Id
            orderby f.RequestDate descending
            select new { id = f.Id, studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, type = f.Type.ToString(), reasonCategory = f.ReasonCategory.ToString(), reasonDescription = f.ReasonDescription, status = f.Status.ToString(), currentSemester = f.CurrentSemester, totalSemesters = f.TotalSemesters, refundAmount = f.RefundAmount, outstandingBalance = f.OutstandingBalance, freezesUsed = f.FreezesUsed, maxFreezeSemesters = f.MaxFreezeSemesters, requestDate = f.RequestDate }).Take(300).ToListAsync(ct))).WithTags("Freezes");

        // ── Foreign students ──
        api.MapGet("/foreign-students", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from r in db.Set<ForeignStudentRegistry>().AsNoTracking()
            join student in db.Students.AsNoTracking() on r.StudentId equals student.Id
            orderby student.LastName
            select new { id = r.Id, studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, nationality = r.Nationality, program = r.Program, yearOfStudy = r.YearOfStudy, visaType = r.VisaType.ToString(), visaExpiry = r.VisaExpiry, passportExpiry = r.PassportExpiry, moraStatus = r.MoraStatus.ToString(), compliance = r.Compliance.ToString(), expectedGraduation = r.ExpectedGraduation }).Take(500).ToListAsync(ct))).WithTags("ForeignStudents");
        api.MapGet("/foreign-students/visa-renewals", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from v in db.Set<VisaRenewal>().AsNoTracking()
            join r in db.Set<ForeignStudentRegistry>().AsNoTracking() on v.RegistryId equals r.Id
            join student in db.Students.AsNoTracking() on r.StudentId equals student.Id
            orderby v.ApplicationDeadline
            select new { id = v.Id, studentName = student.FirstName + " " + student.LastName, nationality = r.Nationality, status = v.Status.ToString(), applicationDeadline = v.ApplicationDeadline, submittedAt = v.SubmittedAt, approvedAt = v.ApprovedAt, rejectionReason = v.RejectionReason }).Take(300).ToListAsync(ct))).WithTags("ForeignStudents");
        api.MapGet("/foreign-students/compliance-breaches", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from b in db.Set<ComplianceBreach>().AsNoTracking()
            join r in db.Set<ForeignStudentRecord>().AsNoTracking() on b.ForeignStudentRecordId equals r.Id into records from r in records.DefaultIfEmpty()
            orderby b.OccurredAt descending
            select new { id = b.Id, breachType = b.BreachType, status = b.Status, occurredAt = b.OccurredAt, resolutionType = b.ResolutionType, studentId = r != null ? r.StudentId : (Guid?)null }).Take(300).ToListAsync(ct))).WithTags("ForeignStudents");

        // ── Defaulter prediction ──
        api.MapGet("/defaulter-prediction/tiers", async (AegisDbContext db, CancellationToken ct) =>
        {
            var scores = await (from s in db.Set<DefaulterRiskScore>().AsNoTracking()
                                join student in db.Students.AsNoTracking() on s.StudentId equals student.Id
                                orderby s.ProbabilityScore descending
                                select new { id = s.Id, studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, semester = s.Semester, probability = s.ProbabilityScore, tier = s.Tier.ToString(), confidence = s.PredictionConfidence, predictedAt = s.PredictedAt }).Take(500).ToListAsync(ct);
            return Results.Ok(new { scores, tiers = scores.GroupBy(s => s.tier).Select(g => new { tier = g.Key, count = g.Count() }).ToList() });
        }).WithTags("DefaulterPrediction");
        api.MapGet("/defaulter-prediction/interventions", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from i in db.Set<DefaulterIntervention>().AsNoTracking()
            join student in db.Students.AsNoTracking() on i.StudentId equals student.Id
            orderby i.InitiatedAt descending
            select new { id = i.Id, studentName = student.FirstName + " " + student.LastName, type = i.Type.ToString(), triggeredByTier = i.TriggeredByTier.ToString(), initiatedAt = i.InitiatedAt, completedAt = i.CompletedAt, outcome = i.Outcome != null ? i.Outcome.ToString() : null, isSuccessful = i.IsSuccessful }).Take(300).ToListAsync(ct))).WithTags("DefaulterPrediction");

        // ── Mental health ──
        api.MapGet("/mental-health/counselor/queue", async (AegisDbContext db, CancellationToken ct) =>
        {
            var tickets = await db.MentalHealthTickets.AsNoTracking().Where(t => t.Status != "resolved").OrderByDescending(t => t.Severity == "crisis" ? 3 : t.Severity == "urgent" ? 2 : 1).ThenBy(t => t.CreatedAt)
                .Select(t => new { id = t.Id, reference = t.AnonymousToken, severity = t.Severity, concernType = t.ConcernType, preferredLanguage = t.PreferredLanguage, status = t.Status, assignedCounselorId = t.AssignedCounselorId, firstResponseAt = t.FirstResponseAt, crisisProtocolsTriggered = t.CrisisProtocolsTriggered, createdAt = t.CreatedAt }).Take(300).ToListAsync(ct);
            var counselors = await db.CounselorProfiles.AsNoTracking().Where(c => c.IsActive).Select(c => new { id = c.Id, fullName = c.FullName, isOnCall = c.IsOnCall, currentCaseload = c.CurrentCaseload, maxCaseload = c.MaxCaseload, languages = c.LanguagesCsv, specializations = c.SpecializationsCsv }).ToListAsync(ct);
            return Results.Ok(new { tickets, counselors });
        }).WithTags("MentalHealth");

        // ── Board ──
        api.MapGet("/board/metrics", async (AegisDbContext db, CancellationToken ct) =>
        {
            var latest = await db.Set<ExecutiveDashboard>().AsNoTracking().OrderByDescending(d => d.GeneratedAt).FirstOrDefaultAsync(ct);
            return Results.Ok(latest is null ? null : new { latest.Id, latest.FiscalYear, latest.AcademicExcellenceScore, latest.FinancialStabilityScore, latest.AccreditationScore, latest.StudentSuccessScore, latest.FacultyQualityScore, latest.CompositeRankingScore, kpis = System.Text.Json.JsonDocument.Parse(latest.KPIsJson).RootElement, latest.GeneratedAt });
        }).WithTags("Board");

        // ── Classes ──
        api.MapGet("/classes/offerings", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from o in db.CourseOfferings.AsNoTracking()
            join course in db.Courses.AsNoTracking() on o.CourseId equals course.Id
            orderby o.AcademicYear descending, o.Semester, course.Code
            select new { id = o.Id, courseCode = course.Code, courseTitle = course.Title, academicYear = o.AcademicYear, semester = o.Semester, sectionNumber = o.SectionNumber, capacity = o.Capacity, enrolledCount = o.EnrolledCount, status = o.Status.ToString() }).Take(500).ToListAsync(ct))).WithTags("Classes");
        api.MapGet("/classes/sections", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from c in db.Classes.AsNoTracking()
            join course in db.Courses.AsNoTracking() on c.CourseId equals course.Id
            join faculty in db.Faculties.AsNoTracking() on c.PrimaryFacultyId equals faculty.Id into faculties from faculty in faculties.DefaultIfEmpty()
            orderby c.AcademicYear descending, c.Semester, course.Code, c.SectionNumber
            select new { id = c.Id, courseCode = course.Code, courseTitle = course.Title, section = c.SectionLabel ?? c.SectionNumber.ToString(), academicYear = c.AcademicYear, semester = c.Semester, faculty = faculty != null ? faculty.FirstName + " " + faculty.LastName : "", capacity = c.Capacity, enrolledCount = c.EnrolledCount, status = c.Status.ToString() }).Take(500).ToListAsync(ct))).WithTags("Classes");
        api.MapGet("/classes/sync-logs", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from p in db.LmsProvisions.AsNoTracking()
            join c in db.Classes.AsNoTracking() on p.ClassId equals c.Id
            join course in db.Courses.AsNoTracking() on c.CourseId equals course.Id
            orderby p.LastSyncAt descending
            select new { id = p.Id, courseCode = course.Code, section = c.SectionLabel ?? c.SectionNumber.ToString(), provider = p.Provider.ToString(), status = p.Status.ToString(), externalUrl = p.ExternalUrl, lastSyncAt = p.LastSyncAt, retryCount = p.RetryCount }).Take(300).ToListAsync(ct))).WithTags("Classes");

        // ── Prerequisite conflicts ──
        api.MapGet("/prerequisite-conflicts", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from c in db.Set<PrerequisiteConflict>().AsNoTracking()
            join source in db.Courses.AsNoTracking() on c.SourceCourseId equals source.Id into sources from source in sources.DefaultIfEmpty()
            join target in db.Courses.AsNoTracking() on c.TargetCourseId equals target.Id into targets from target in targets.DefaultIfEmpty()
            orderby c.IsResolved, c.Severity descending, c.DetectedAt descending
            select new { id = c.Id, type = c.Type.ToString(), severity = c.Severity.ToString(), sourceCourse = source != null ? source.Code : null, targetCourse = target != null ? target.Code : null, description = c.Description, resolutionSuggestion = c.ResolutionSuggestion, isResolved = c.IsResolved, detectedAt = c.DetectedAt, resolvedAt = c.ResolvedAt }).Take(300).ToListAsync(ct))).WithTags("PrerequisiteConflicts");

        // ── Grade appeals ──
        api.MapGet("/grade-appeals", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await (
            from a in db.Set<GradeAppeal>().AsNoTracking()
            join student in db.Students.AsNoTracking() on a.StudentId equals student.Id
            join course in db.Courses.AsNoTracking() on a.CourseId equals course.Id
            orderby a.SubmissionDate descending
            select new { id = a.Id, studentId = student.RollNumber ?? student.Id.ToString(), studentName = student.FirstName + " " + student.LastName, courseCode = course.Code, courseTitle = course.Title, semester = a.Semester, submittedGrade = a.SubmittedGrade, groundType = a.GroundType.ToString(), groundStrength = a.GroundStrength.ToString(), status = a.Status.ToString(), committeeDecision = a.CommitteeDecision, adjustedGrade = a.AdjustedGrade, submissionDate = a.SubmissionDate, deadline = a.Deadline, resolvedAt = a.ResolvedAt, isEligible = a.IsEligible }).Take(300).ToListAsync(ct))).WithTags("GradeAppeals");

        // ── Catalogue ingestion ──
        api.MapGet("/catalog/ingest/jobs", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.IngestionJobs.AsNoTracking().OrderByDescending(j => j.Id)
            .Select(j => new { id = j.Id, documentName = j.DocumentName, documentType = j.DocumentType, status = j.Status.ToString(), extractionConfidence = j.ExtractionConfidence, coursesExtracted = j.CoursesExtracted, coursesInserted = j.CoursesInserted, coursesUpdated = j.CoursesUpdated }).Take(100).ToListAsync(ct))).WithTags("CatalogIngestion");
        api.MapGet("/catalog/courses", async (AegisDbContext db, Guid? jobId, CancellationToken ct) => Results.Ok(await db.CatalogCourseRecords.AsNoTracking().Where(c => jobId == null || c.JobId == jobId).OrderBy(c => c.DegreeProgram).ThenBy(c => c.SemesterSequence).ThenBy(c => c.CourseCode)
            .Select(c => new { id = c.Id, jobId = c.JobId, degreeProgram = c.DegreeProgram, semesterSequence = c.SemesterSequence, semesterLabel = c.SemesterLabel, courseCode = c.CourseCode, courseTitle = c.CourseTitle, creditHoursTotal = c.CreditHoursTotal, courseType = c.CourseType, prerequisitesJson = c.PrerequisitesJson }).Take(1000).ToListAsync(ct))).WithTags("CatalogIngestion");

        // ── Elections ──
        api.MapGet("/elections", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.Elections.AsNoTracking().OrderByDescending(e => e.VotingStart)
            .Select(e => new { id = e.Id, title = e.Title, electionType = e.ElectionType, votingMethod = e.VotingMethod, nominationStart = e.NominationStart, nominationEnd = e.NominationEnd, votingStart = e.VotingStart, votingEnd = e.VotingEnd, status = e.Status, turnoutRate = e.TurnoutRate, resultsPublishedAt = e.ResultsPublishedAt,
                positions = e.Positions.Count, candidates = db.ElectionCandidates.Count(c => c.ElectionId == e.Id), ballots = db.Set<EncryptedBallot>().Count(b => b.ElectionId == e.Id) }).ToListAsync(ct))).WithTags("Elections");
        api.MapGet("/elections/candidates", async (AegisDbContext db, Guid? electionId, CancellationToken ct) => Results.Ok(await (
            from c in db.ElectionCandidates.AsNoTracking().Where(c => electionId == null || c.ElectionId == electionId)
            join student in db.Students.AsNoTracking() on c.StudentId equals student.Id
            join position in db.Set<ElectionPosition>().AsNoTracking() on c.PositionId equals position.Id into positions from position in positions.DefaultIfEmpty()
            select new { id = c.Id, electionId = c.ElectionId, position = position != null ? position.PositionName : "", studentName = student.FirstName + " " + student.LastName, studentId = student.RollNumber ?? student.Id.ToString(), endorsementCount = c.EndorsementCount, status = c.NominationStatus, eligibilityVerified = c.EligibilityVerified }).Take(500).ToListAsync(ct))).WithTags("Elections");
        api.MapGet("/elections/results", async (AegisDbContext db, Guid? electionId, CancellationToken ct) => Results.Ok(await (
            from r in db.ElectionResults.AsNoTracking().Where(r => electionId == null || r.ElectionId == electionId)
            join position in db.Set<ElectionPosition>().AsNoTracking() on r.PositionId equals position.Id into positions from position in positions.DefaultIfEmpty()
            join c in db.ElectionCandidates.AsNoTracking() on r.CandidateId equals c.Id into candidates from c in candidates.DefaultIfEmpty()
            join student in db.Students.AsNoTracking() on (c != null ? c.StudentId : Guid.Empty) equals student.Id into students from student in students.DefaultIfEmpty()
            orderby r.VoteCount descending
            select new { id = r.Id, electionId = r.ElectionId, position = position != null ? position.PositionName : "", candidate = student != null ? student.FirstName + " " + student.LastName : "", voteCount = r.VoteCount, isElected = r.IsElected }).Take(500).ToListAsync(ct))).WithTags("Elections");

        // ── Voice ──
        api.MapGet("/voice/sessions", async (AegisDbContext db, CancellationToken ct) => Results.Ok(await db.VoiceSessions.AsNoTracking().OrderByDescending(s => s.CreatedAt)
            .Select(s => new { id = s.Id, userRole = s.UserRole, detectedLanguage = s.DetectedLanguage, transcript = s.Transcript, intent = s.Intent, responseText = s.ResponseText, asrConfidence = s.AsrConfidence, wasEscalated = s.WasEscalated, escalationReason = s.EscalationReason, createdAt = s.CreatedAt }).Take(100).ToListAsync(ct))).WithTags("Voice");

        return endpoints;
    }
}
