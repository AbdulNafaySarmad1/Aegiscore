# Nocturne — Governance, Permission Overrides, Feedback & Replication (U10)

## Role
You are a senior platform architect designing Nocturne's governance layer. This covers:
1. **Intentional Replication Lag** — Prevent automatic replica syncs; force deliberate, configured delays
2. **Permission Overrides** — HOD can override grades, section assignments, fees for exceptional cases
3. **Anonymous Feedback** — Students evaluate teachers/courses; zero traceability
4. **Dynamic Fee Calculation** — Credit hours change = fees recalculate in real-time
5. **Course Re-enrollment Logic** — Student can't complete course → auto re-enroll next sem, fees waived

---

## Part A — Replication Lag & Data Consistency Strategy

### Problem: Automatic Replica Sync During Load

**Scenario:**
- Teacher is entering marks for 500 students
- System detects high load
- Automatically spins up read replicas + write replicas
- Immediately syncs all data
- **Problem:** Creates race conditions, inconsistent grading, marks entered twice

**Solution: Intentional Lag**

Instead of automatic sync, enforce a **configured delay** (default: 12 hours) before replicas catch up to primary database.

### Architecture

```
Primary Database (PostgreSQL)
    ├─ Write API calls go HERE (marks, grades, fees)
    ├─ Cached in Redis (15-minute TTL)
    └─ Replication Stream (12-hour lag)
         ├─ Read Replica 1 (database1.read.nocturne.edu.pk)
         ├─ Read Replica 2 (database2.read.nocturne.edu.pk)
         └─ Write Replica (database.write.nocturne.edu.pk) — for backup writes only
```

### Replication Lag Configuration

During onboarding wizard, ask:

```
Q: "What is your desired replication lag (hours)?"
   Default: 12
   Min: 1
   Max: 72
   Rationale: "Higher lag = more stable reads. Lower lag = fresher reads but more conflicts."

A: University selects 12 hours
   → Stored in system_configuration.replication_lag_hours = 12
   → PostgreSQL replication begins with 12-hour delay
```

### Database Configuration Table

```sql
CREATE TABLE system_configuration (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL REFERENCES universities(id),
    
    -- Replication
    replication_lag_hours INT DEFAULT 12, -- Configured lag
    primary_db_connection_string TEXT NOT NULL,
    read_replica_1_connection_string TEXT,
    read_replica_2_connection_string TEXT,
    write_replica_connection_string TEXT,
    
    -- Redis
    redis_connection_string TEXT NOT NULL,
    redis_cache_ttl_minutes INT DEFAULT 15,
    
    -- DNS
    subdomain_name TEXT, -- e.g., "nocturne.ssuet.edu.pk"
    custom_dns_enabled BOOLEAN DEFAULT false,
    dns_nameserver_1 TEXT,
    dns_nameserver_2 TEXT,
    
    -- Capacity
    regular_class_capacity INT DEFAULT 60,
    exam_class_capacity INT DEFAULT 80,
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(university_id, subdomain_name)
);
```

### Read/Write Router

```csharp
public interface IDatabaseRouter
{
    // All writes go to PRIMARY only
    Task<T> WriteAsync<T>(
        Func<ApplicationDbContext, Task<T>> operation,
        CancellationToken ct = default
    );
    
    // Reads can go to replica (with lag accepted)
    Task<T> ReadAsync<T>(
        Func<ApplicationDbContext, Task<T>> operation,
        bool allowStaleData = true, // Accept replication lag
        CancellationToken ct = default
    );
    
    // Immediate consistent read (go to PRIMARY)
    Task<T> ReadConsistentAsync<T>(
        Func<ApplicationDbContext, Task<T>> operation,
        CancellationToken ct = default
    );
}

public class DatabaseRouter : IDatabaseRouter
{
    private readonly ApplicationDbContext _primaryDb;
    private readonly ApplicationDbContext _replicaDb; // 12-hour lag
    
    public async Task<T> WriteAsync<T>(
        Func<ApplicationDbContext, Task<T>> operation,
        CancellationToken ct = default)
    {
        // ALWAYS write to primary
        return await operation(_primaryDb);
    }
    
    public async Task<T> ReadAsync<T>(
        Func<ApplicationDbContext, Task<T>> operation,
        bool allowStaleData = true,
        CancellationToken ct = default)
    {
        // If stale data OK, read from replica (cheaper, less load)
        if (allowStaleData && _replicaDb != null)
        {
            try
            {
                return await operation(_replicaDb);
            }
            catch
            {
                // Replica failed, fall back to primary
                return await operation(_primaryDb);
            }
        }
        
        // Force fresh read from primary
        return await operation(_primaryDb);
    }
    
    public async Task<T> ReadConsistentAsync<T>(
        Func<ApplicationDbContext, Task<T>> operation,
        CancellationToken ct = default)
    {
        // Always go to primary for consistent reads
        return await operation(_primaryDb);
    }
}
```

### Example: Teacher Entering Marks

```csharp
[HttpPost("api/examination/marks/batch")]
public async Task<IActionResult> BatchEnterMarks(
    [FromBody] List<StudentMarkEntry> marks,
    CancellationToken ct = default)
{
    using (var transaction = _db.Database.BeginTransaction())
    {
        try
        {
            // Step 1: All writes go to PRIMARY only
            foreach (var entry in marks)
            {
                var studentResult = await _db.StudentExaminationResults
                    .FirstOrDefaultAsync(r => r.StudentId == entry.StudentId 
                        && r.ExamId == entry.ExamId, ct);
                
                if (studentResult == null)
                {
                    studentResult = new StudentExaminationResult
                    {
                        StudentId = entry.StudentId,
                        ExamId = entry.ExamId,
                        MarksObtained = entry.Marks,
                        EnteredAt = DateTime.UtcNow,
                        EnteredBy = User.GetUserId()
                    };
                    _db.StudentExaminationResults.Add(studentResult);
                }
                else
                {
                    studentResult.MarksObtained = entry.Marks;
                    studentResult.UpdatedAt = DateTime.UtcNow;
                }
            }
            
            // Step 2: All committed to PRIMARY
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            
            // Step 3: Replicas will sync after 12 hours (configured lag)
            // No manual sync. No race conditions.
            
            return Ok(new { message = "Marks entered. Replicas sync in 12 hours." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return BadRequest(ex.Message);
        }
    }
}
```

---

## Part B — Permission Overrides (Governance)

### Override Types

1. **Grade Override** — HOD overrides student's final grade
2. **Section Override** — Move student to different section
3. **Fee Override** — Waive or adjust student fees
4. **Attendance Override** — Mark student present despite absence
5. **Course Re-enrollment Override** — Auto re-enroll in incomplete course

### Grade Override Table

```sql
CREATE TABLE grade_overrides (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL REFERENCES universities(id),
    student_id UUID NOT NULL REFERENCES students(id),
    course_id UUID NOT NULL REFERENCES courses(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    
    original_grade CHAR(1), -- A, B, C, D, F
    original_marks INT,
    original_gpa DECIMAL(3,2),
    
    override_grade CHAR(1),
    override_marks INT,
    override_gpa DECIMAL(3,2),
    
    reason TEXT NOT NULL, -- "Broken bones, couldn't attend final exam"
    justification TEXT, -- Detailed explanation
    
    -- Approval workflow
    requested_by UUID NOT NULL REFERENCES users(id), -- HOD or Professor
    approved_by UUID REFERENCES users(id), -- VC or Chancellor (if needed)
    approval_status ENUM('pending', 'approved', 'rejected', 'escalated'), -- Default: pending
    approval_threshold ENUM('hod', 'vc', 'chancellor'), -- Who needs to approve?
    
    approved_at TIMESTAMPTZ,
    requested_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Audit
    affected_gpa_change DECIMAL(3,2), -- Before vs after impact
    transcript_regenerated BOOLEAN DEFAULT false,
    
    CONSTRAINT grade_override_audit
    CHECK (original_grade != override_grade OR original_marks != override_marks)
);
```

### Override Approval Rules

```csharp
public class OverrideApprovalMatrix
{
    // Grade change ±5% or less → HOD auto-approves
    // Grade change ±5% to ±15% → HOD + VC approval needed
    // Grade change >±15% → VC + Chancellor approval needed
    
    public static OverrideApprovalLevel GetApprovalLevel(
        int originalMarks,
        int overrideMarks,
        int totalMarks = 100)
    {
        var percentChange = Math.Abs(originalMarks - overrideMarks) / (decimal)totalMarks * 100;
        
        return percentChange switch
        {
            <= 5 => OverrideApprovalLevel.HOD,           // Auto-approve
            <= 15 => OverrideApprovalLevel.HOD_AND_VC,  // Need 2 sigs
            > 15 => OverrideApprovalLevel.VC_AND_CHANCELLOR, // Need 3 sigs
        };
    }
}

public enum OverrideApprovalLevel
{
    HOD,
    HOD_AND_VC,
    VC_AND_CHANCELLOR
}
```

### Grade Override API

```csharp
[HttpPost("api/academic/grade-override/request")]
[Authorize(Roles = "HOD, VC")]
public async Task<IActionResult> RequestGradeOverride(
    [FromBody] GradeOverrideRequest request,
    CancellationToken ct = default)
{
    var audit = new AuditLog
    {
        Action = "GRADE_OVERRIDE_REQUESTED",
        StudentId = request.StudentId,
        CourseId = request.CourseId,
        Details = request.Reason,
        PerformedBy = User.GetUserId(),
        Timestamp = DateTime.UtcNow
    };
    _db.AuditLogs.Add(audit);
    
    var @override = new GradeOverride
    {
        StudentId = request.StudentId,
        CourseId = request.CourseId,
        OriginalGrade = request.OriginalGrade,
        OverrideGrade = request.OverrideGrade,
        Reason = request.Reason,
        RequestedBy = User.GetUserId(),
        ApprovalStatus = "pending",
        ApprovalThreshold = OverrideApprovalMatrix.GetApprovalLevel(
            request.OriginalMarks,
            request.OverrideMarks
        )
    };
    
    _db.GradeOverrides.Add(@override);
    await _db.SaveChangesAsync(ct);
    
    // Notify approvers
    await _notificationService.SendAsync(
        $"Grade override requested for {student.Name}",
        recipients: GetApprovers(@override.ApprovalThreshold),
        priority: "high"
    );
    
    return Ok(new { overrideId = @override.Id, status = "pending_approval" });
}

[HttpPost("api/academic/grade-override/{overrideId}/approve")]
[Authorize(Roles = "VC, Chancellor")]
public async Task<IActionResult> ApproveGradeOverride(
    Guid overrideId,
    [FromBody] ApprovalRequest request,
    CancellationToken ct = default)
{
    var @override = await _db.GradeOverrides.FindAsync(overrideId, ct);
    
    @override.ApprovedBy = User.GetUserId();
    @override.ApprovedAt = DateTime.UtcNow;
    @override.ApprovalStatus = "approved";
    
    // Update student's actual grade
    var result = await _db.StudentExaminationResults
        .FirstOrDefaultAsync(r => r.StudentId == @override.StudentId
            && r.CourseId == @override.CourseId, ct);
    
    if (result != null)
    {
        result.MarksObtained = @override.OverrideMarks;
        result.Grade = @override.OverrideGrade;
        result.OverriddenGrade = true;
        result.OverrideReason = @override.Reason;
    }
    
    // Regenerate transcript
    await _transcriptService.RegenerateAsync(@override.StudentId, ct);
    
    // Audit
    _db.AuditLogs.Add(new AuditLog
    {
        Action = "GRADE_OVERRIDE_APPROVED",
        StudentId = @override.StudentId,
        PerformedBy = User.GetUserId(),
        Details = request.ApprovalComments
    });
    
    await _db.SaveChangesAsync(ct);
    
    return Ok(new { message = "Grade override approved" });
}
```

---

## Part C — Anonymous Student Feedback System

### Problem: Traceability

Standard feedback systems let HODs/admins see WHO wrote what → chills honest feedback → 30% response rate.

**Solution: Zero-traceability anonymous feedback**

No way to trace feedback back to student. Zero. Period.

### Architecture

```
Student Submits Feedback
    ↓
Encryption Layer (student ID removed IMMEDIATELY)
    ↓
Anonymous Submission
    ↓
Aggregated Statistics ONLY
    ├─ Likert scores (1-5)
    ├─ Percentages (45% like, 55% dislike)
    ├─ Text summary (AI-deduped similar comments)
    └─ NO individual responses visible
```

### Anonymous Feedback Table

```sql
CREATE TABLE anonymous_feedback (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL REFERENCES universities(id),
    
    -- Anonymous: no student_id stored
    -- Only semantic hash of feedback
    feedback_hash VARCHAR(64) NOT NULL, -- SHA-256 of feedback content
    
    teacher_id UUID NOT NULL REFERENCES users(id),
    course_id UUID NOT NULL REFERENCES courses(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    
    -- Likert scale (teacher popularity)
    teaching_quality INT CHECK (teaching_quality BETWEEN 1 AND 5),
    communication INT CHECK (communication BETWEEN 1 AND 5),
    course_clarity INT CHECK (course_clarity BETWEEN 1 AND 5),
    fairness INT CHECK (fairness BETWEEN 1 AND 5),
    engagement INT CHECK (engagement BETWEEN 1 AND 5),
    
    -- Open-ended (anonymized)
    feedback_text_encrypted TEXT, -- AES-256 encrypted
    sentiment_score DECIMAL(3,2), -- -1.0 to 1.0 (negative to positive)
    
    -- No link back to student
    submitted_at TIMESTAMPTZ DEFAULT NOW(),
    
    CONSTRAINT no_student_traceability CHECK (true) -- Enforced in code
);
```

### Anonymous Submission API

```csharp
[HttpPost("api/feedback/submit-anonymous")]
[Authorize(Roles = "Student")]
public async Task<IActionResult> SubmitAnonymousFeedback(
    [FromBody] AnonymousFeedbackRequest request,
    CancellationToken ct = default)
{
    var userId = User.GetUserId();
    var student = await _db.Users.FindAsync(userId, ct);
    
    // CRITICAL: Student ID is NOT stored or linked
    // Only the feedback content is stored
    
    var feedback = new AnonymousFeedback
    {
        UniversityId = student.UniversityId,
        // ⬅️ NO student_id field
        TeacherId = request.TeacherId,
        CourseId = request.CourseId,
        SemesterId = request.SemesterId,
        
        TeachingQuality = request.TeachingQuality,
        Communication = request.Communication,
        CourseClarity = request.CourseClarity,
        Fairness = request.Fairness,
        Engagement = request.Engagement,
        
        // Encrypt feedback text
        FeedbackTextEncrypted = _encryptionService.Encrypt(request.FeedbackText),
        
        // AI sentiment analysis
        SentimentScore = await _nlpService.AnalyzeSentimentAsync(request.FeedbackText),
        
        // Hash the feedback content (for deduplication only)
        FeedbackHash = _cryptoService.SHA256(request.FeedbackText),
        
        SubmittedAt = DateTime.UtcNow
    };
    
    _db.AnonymousFeedback.Add(feedback);
    await _db.SaveChangesAsync(ct);
    
    // DO NOT return feedback ID or any linkable info
    return Ok(new { message = "Feedback submitted anonymously" });
}
```

### Teacher Profile: Aggregated Feedback Stats

```csharp
[HttpGet("api/teachers/{teacherId}/feedback-profile")]
[Authorize]
public async Task<IActionResult> GetTeacherFeedbackProfile(
    Guid teacherId,
    CancellationToken ct = default)
{
    // Only aggregated stats, ZERO individual feedback
    
    var allFeedback = await _db.AnonymousFeedback
        .Where(f => f.TeacherId == teacherId)
        .ToListAsync(ct);
    
    if (!allFeedback.Any())
        return Ok(new { message = "No feedback yet" });
    
    var profile = new TeacherFeedbackProfile
    {
        TeacherId = teacherId,
        TotalResponseCount = allFeedback.Count,
        
        AverageScores = new
        {
            TeachingQuality = allFeedback.Average(f => f.TeachingQuality),
            Communication = allFeedback.Average(f => f.Communication),
            CourseClarity = allFeedback.Average(f => f.CourseClarity),
            Fairness = allFeedback.Average(f => f.Fairness),
            Engagement = allFeedback.Average(f => f.Engagement)
        },
        
        // Positive vs Negative split
        Sentiment = new
        {
            PositivePercentage = (allFeedback.Count(f => f.SentimentScore > 0.3m) / (decimal)allFeedback.Count * 100),
            NeutralPercentage = (allFeedback.Count(f => f.SentimentScore >= -0.3m && f.SentimentScore <= 0.3m) / (decimal)allFeedback.Count * 100),
            NegativePercentage = (allFeedback.Count(f => f.SentimentScore < -0.3m) / (decimal)allFeedback.Count * 100)
        },
        
        // Likability ranking (within department)
        LikabilityRank = await GetTeacherLikabilityRank(teacherId, ct),
        
        // Summary of common themes (AI deduplicated)
        CommonThemes = await ExtractCommonThemesAsync(allFeedback, ct),
        
        // NO individual feedback visible
        // NO way to know who said what
    };
    
    return Ok(profile);
}

private async Task<List<string>> ExtractCommonThemesAsync(
    List<AnonymousFeedback> feedback,
    CancellationToken ct)
{
    // Decrypt all feedback
    var texts = feedback
        .Select(f => _encryptionService.Decrypt(f.FeedbackTextEncrypted))
        .ToList();
    
    // Use AI to group similar feedback into themes
    var themes = await _nlpService.ExtractThemesAsync(texts);
    
    // Return only themes, not individual responses
    return themes
        .OrderByDescending(t => t.Frequency)
        .Take(5) // Top 5 themes
        .Select(t => t.Summary)
        .ToList();
}
```

### Visibility Rules (Who Can See What)

```
Student:
  ✅ Can submit anonymous feedback
  ✅ Can see AGGREGATED stats of all teachers
  ✅ CANNOT see individual feedback
  ❌ CANNOT see who wrote what

Teacher:
  ✅ Can see their aggregated stats (average scores, themes)
  ✅ Can see positive/negative percentage
  ✅ Can see common themes ("students say: lecture speed too fast")
  ❌ CANNOT see individual responses
  ❌ CANNOT see who wrote what

HOD/Admin:
  ✅ Can see department-wide aggregated stats
  ✅ Can see teacher rankings within department
  ✅ Can see trends over semesters
  ❌ CANNOT see individual feedback
  ❌ CANNOT see who wrote what
  ❌ CANNOT identify students

VC/Chancellor:
  ✅ Same as HOD (aggregated only)
```

### Encryption Guarantee

```csharp
public class AnonymousFeedbackGuarantee
{
    // Even if database is compromised:
    
    public static bool CanTraceStudent(List<AnonymousFeedback> compromisedDb)
    {
        // 1. No student_id stored
        foreach (var feedback in compromisedDb)
        {
            if (feedback.StudentId != null)
                return true; // VIOLATED
        }
        
        // 2. No session ID or IP linked
        // (handled at API layer — never stored)
        
        // 3. Feedback text is encrypted
        // (attacker can't read even if they have DB)
        
        // 4. Hashes are one-way
        // (can't reverse-engineer to find duplicates)
        
        return false; // SAFE: CANNOT trace
    }
}
```

---

## Part D — Section Override Capability

### Scenario
Student in Section A is misbehaving. Professor requests HOD to move them to Section B.

**Constraints:**
- Section B must have available seats
- Can't move to full sections
- Fees must adjust automatically
- Must respect both regular and exam capacity

### Section Override Table

```sql
CREATE TABLE section_overrides (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL REFERENCES universities(id),
    student_id UUID NOT NULL REFERENCES students(id),
    course_id UUID NOT NULL REFERENCES courses(id),
    
    from_section_id UUID NOT NULL REFERENCES course_sections(id),
    to_section_id UUID NOT NULL REFERENCES course_sections(id),
    
    reason TEXT NOT NULL, -- "Misbehavior", "Accommodation", etc.
    requested_by UUID NOT NULL REFERENCES users(id), -- Professor
    approved_by UUID REFERENCES users(id), -- HOD
    
    approval_status ENUM('pending', 'approved', 'rejected'),
    approved_at TIMESTAMPTZ,
    
    -- Fee adjustments
    original_fee DECIMAL(12,2),
    new_fee DECIMAL(12,2),
    fee_adjustment DECIMAL(12,2),
    
    requested_at TIMESTAMPTZ DEFAULT NOW(),
    effective_at TIMESTAMPTZ
);
```

### Capacity Check & Override API

```csharp
[HttpPost("api/academic/section-override/request")]
[Authorize(Roles = "Professor")]
public async Task<IActionResult> RequestSectionOverride(
    [FromBody] SectionOverrideRequest request,
    CancellationToken ct = default)
{
    var fromSection = await _db.CourseSections
        .Include(s => s.EnrolledStudents)
        .FirstOrDefaultAsync(s => s.Id == request.FromSectionId, ct);
    
    var toSection = await _db.CourseSections
        .Include(s => s.EnrolledStudents)
        .FirstOrDefaultAsync(s => s.Id == request.ToSectionId, ct);
    
    // Check capacity
    var config = await _db.SystemConfiguration
        .FirstOrDefaultAsync(c => c.UniversityId == User.GetUniversityId(), ct);
    
    var toSectionCurrentEnrollment = toSection.EnrolledStudents.Count;
    var toSectionCapacity = config.RegularClassCapacity;
    
    if (toSectionCurrentEnrollment >= toSectionCapacity)
    {
        return BadRequest(new
        {
            error = "Target section is full",
            currentEnrollment = toSectionCurrentEnrollment,
            capacity = toSectionCapacity,
            availableSeats = toSectionCapacity - toSectionCurrentEnrollment
        });
    }
    
    // Create override request
    var @override = new SectionOverride
    {
        StudentId = request.StudentId,
        CourseId = request.CourseId,
        FromSectionId = request.FromSectionId,
        ToSectionId = request.ToSectionId,
        Reason = request.Reason,
        RequestedBy = User.GetUserId(),
        ApprovalStatus = "pending",
        RequestedAt = DateTime.UtcNow
    };
    
    _db.SectionOverrides.Add(@override);
    await _db.SaveChangesAsync(ct);
    
    // Notify HOD
    var hod = await GetDepartmentHeadAsync(request.CourseId, ct);
    await _notificationService.SendAsync(
        $"Section override request for {student.Name}",
        recipients: new[] { hod.UserId },
        priority: "high"
    );
    
    return Ok(new { overrideId = @override.Id, status = "pending_approval" });
}

[HttpPost("api/academic/section-override/{overrideId}/approve")]
[Authorize(Roles = "HOD")]
public async Task<IActionResult> ApproveSectionOverride(
    Guid overrideId,
    CancellationToken ct = default)
{
    var @override = await _db.SectionOverrides
        .Include(o => o.FromSection)
        .Include(o => o.ToSection)
        .FirstOrDefaultAsync(o => o.Id == overrideId, ct);
    
    // Double-check capacity
    var toSectionEnrollment = @override.ToSection.EnrolledStudents.Count;
    var capacity = (await _db.SystemConfiguration
        .FirstOrDefaultAsync(ct: ct)).RegularClassCapacity;
    
    if (toSectionEnrollment >= capacity)
    {
        return BadRequest("Target section filled up since request");
    }
    
    // Move student
    var enrollment = await _db.CourseEnrollments
        .FirstOrDefaultAsync(e => e.StudentId == @override.StudentId
            && e.CourseId == @override.CourseId, ct);
    
    if (enrollment != null)
    {
        var oldSectionId = enrollment.SectionId;
        enrollment.SectionId = @override.ToSectionId;
        
        // Calculate fee adjustment
        var oldSectionFee = await GetSectionFeeAsync(oldSectionId, ct);
        var newSectionFee = await GetSectionFeeAsync(@override.ToSectionId, ct);
        var feeAdjustment = newSectionFee - oldSectionFee;
        
        // Auto-adjust student fees
        var studentFees = await _db.StudentFees
            .FirstOrDefaultAsync(f => f.StudentId == @override.StudentId
                && f.SemesterId == enrollment.SemesterId, ct);
        
        if (studentFees != null)
        {
            studentFees.TotalFees += feeAdjustment;
            studentFees.UpdatedAt = DateTime.UtcNow;
            
            // Log adjustment
            _db.FeeAdjustments.Add(new FeeAdjustment
            {
                StudentId = @override.StudentId,
                Reason = $"Section change: {oldSectionId} → {@override.ToSectionId}",
                Amount = feeAdjustment,
                ApprovedBy = User.GetUserId()
            });
        }
    }
    
    @override.ApprovedBy = User.GetUserId();
    @override.ApprovedAt = DateTime.UtcNow;
    @override.ApprovalStatus = "approved";
    @override.EffectiveAt = DateTime.UtcNow;
    
    // Audit
    _db.AuditLogs.Add(new AuditLog
    {
        Action = "SECTION_OVERRIDE_APPROVED",
        StudentId = @override.StudentId,
        PerformedBy = User.GetUserId()
    });
    
    await _db.SaveChangesAsync(ct);
    
    return Ok(new { message = "Section override approved" });
}
```

---

## Part E — Course Re-enrollment & Automatic Fee Removal

### Scenario
Student broke their leg mid-semester, couldn't complete finals in Mathematics. 

**Solution:**
1. System detects: Student didn't sit final exam → mark course as "incomplete"
2. Automatic re-enrollment: Next semester → Mathematics re-enrolled
3. Automatic fee waiver: This semester's Mathematics fees → REMOVED
4. Student can retake with clean slate

### Course Status Table

```sql
CREATE TABLE course_enrollments (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL,
    student_id UUID NOT NULL REFERENCES students(id),
    course_id UUID NOT NULL REFERENCES courses(id),
    section_id UUID NOT NULL REFERENCES course_sections(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    
    enrollment_status ENUM(
        'enrolled',
        'active',
        'completed',
        'dropped',
        'incomplete', -- ⬅️ NEW: Couldn't finish (medical, emergency, etc.)
        're-enrolled'
    ),
    
    -- Incomplete details
    incomplete_reason TEXT, -- "Broken leg", "Medical emergency", etc.
    incomplete_approved_by UUID REFERENCES users(id), -- HOD approval
    incomplete_requested_at TIMESTAMPTZ,
    
    -- Marks/Grade
    final_marks INT,
    grade CHAR(1),
    gpa DECIMAL(3,2),
    
    credited_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Automatic Re-enrollment Job

```csharp
// Runs at semester start
[BackgroundJob]
public async Task ProcessIncompleteCoursesAsync(Guid semesterId, CancellationToken ct = default)
{
    var incompleteEnrollments = await _db.CourseEnrollments
        .Where(e => e.EnrollmentStatus == "incomplete"
            && e.SemesterId < semesterId) // Previous semester
        .Include(e => e.Student)
        .Include(e => e.Course)
        .ToListAsync(ct);
    
    foreach (var incomplete in incompleteEnrollments)
    {
        // Check if student is re-taking this course
        var reEnrollment = await _db.CourseEnrollments
            .FirstOrDefaultAsync(e => e.StudentId == incomplete.StudentId
                && e.CourseId == incomplete.CourseId
                && e.SemesterId == semesterId, ct);
        
        if (reEnrollment == null)
        {
            // Auto re-enroll in SAME course, this semester
            var enrollment = new CourseEnrollment
            {
                StudentId = incomplete.StudentId,
                CourseId = incomplete.CourseId,
                SectionId = incomplete.SectionId,
                SemesterId = semesterId,
                EnrollmentStatus = "re-enrolled",
                CreatedAt = DateTime.UtcNow
            };
            
            _db.CourseEnrollments.Add(enrollment);
            
            // AUTO-REMOVE FEES for this course from PREVIOUS semester
            var previousSemesterFees = await _db.StudentFees
                .FirstOrDefaultAsync(f => f.StudentId == incomplete.StudentId
                    && f.SemesterId == incomplete.SemesterId, ct);
            
            if (previousSemesterFees != null)
            {
                var courseCredits = incomplete.Course.CreditHours;
                var feePerCredit = previousSemesterFees.TotalFees / incomplete.Student.TotalEnrolledCredits;
                var feeToRemove = feePerCredit * courseCredits;
                
                previousSemesterFees.TotalFees -= feeToRemove;
                
                // Log removal
                _db.FeeAdjustments.Add(new FeeAdjustment
                {
                    StudentId = incomplete.StudentId,
                    Reason = $"Incomplete course fee removal: {incomplete.Course.Code}",
                    Amount = -feeToRemove, // Negative = credit
                    ApprovedBy = Guid.Empty, // System
                    AutomaticAdjustment = true
                });
            }
        }
    }
    
    await _db.SaveChangesAsync(ct);
}

// API: Mark course as incomplete (HOD approval required)
[HttpPost("api/academic/course-enrollment/{enrollmentId}/mark-incomplete")]
[Authorize(Roles = "HOD")]
public async Task<IActionResult> MarkCourseIncomplete(
    Guid enrollmentId,
    [FromBody] IncompleteRequest request,
    CancellationToken ct = default)
{
    var enrollment = await _db.CourseEnrollments.FindAsync(enrollmentId, ct);
    
    enrollment.EnrollmentStatus = "incomplete";
    enrollment.IncompleteReason = request.Reason;
    enrollment.IncompleteApprovedBy = User.GetUserId();
    enrollment.IncompleteRequestedAt = DateTime.UtcNow;
    
    // Fee removal happens automatically next semester
    _db.CourseEnrollments.Update(enrollment);
    await _db.SaveChangesAsync(ct);
    
    return Ok(new { message = "Course marked incomplete. Fees will be removed next semester." });
}
```

---

## Part F — Dynamic Fee Calculation (Real-Time Credit Hour Changes)

### Problem
Student enrolls in 3 courses (9 credit hours) → Fee calculated as ₱90K.
Then adds 1 course (12 credit hours total) → Fee should update to ₱120K immediately.

### Fee Calculation Engine

```sql
CREATE TABLE student_fees (
    id UUID PRIMARY KEY,
    student_id UUID NOT NULL REFERENCES students(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    university_id UUID NOT NULL,
    
    -- Dynamic calculation based on credit hours
    total_enrolled_credits INT NOT NULL, -- Recalculate whenever this changes
    fee_per_credit_hour DECIMAL(12,2) NOT NULL,
    
    tuition_fee DECIMAL(12,2) GENERATED ALWAYS AS (
        total_enrolled_credits * fee_per_credit_hour
    ),
    
    lab_fee DECIMAL(12,2), -- Fixed or per-credit
    technology_fee DECIMAL(12,2), -- Fixed
    library_fee DECIMAL(12,2), -- Fixed
    activity_fee DECIMAL(12,2), -- Fixed
    
    total_fees DECIMAL(12,2) GENERATED ALWAYS AS (
        (total_enrolled_credits * fee_per_credit_hour)
        + COALESCE(lab_fee, 0)
        + COALESCE(technology_fee, 0)
        + COALESCE(library_fee, 0)
        + COALESCE(activity_fee, 0)
    ),
    
    -- Adjustments
    scholarships DECIMAL(12,2) DEFAULT 0,
    fee_waivers DECIMAL(12,2) DEFAULT 0,
    financial_aid DECIMAL(12,2) DEFAULT 0,
    
    amount_due DECIMAL(12,2) GENERATED ALWAYS AS (
        total_fees - scholarships - fee_waivers - financial_aid
    ),
    
    last_recalculated_at TIMESTAMPTZ DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(student_id, semester_id)
);
```

### Fee Recalculation Trigger

```csharp
[HttpPost("api/academic/course-enrollment")]
[Authorize]
public async Task<IActionResult> EnrollInCourse(
    [FromBody] CourseEnrollmentRequest request,
    CancellationToken ct = default)
{
    var enrollment = new CourseEnrollment
    {
        StudentId = request.StudentId,
        CourseId = request.CourseId,
        SectionId = request.SectionId,
        SemesterId = request.SemesterId,
        EnrollmentStatus = "enrolled"
    };
    
    _db.CourseEnrollments.Add(enrollment);
    await _db.SaveChangesAsync(ct);
    
    // TRIGGER: Recalculate fees
    await RecalculateStudentFeesAsync(request.StudentId, request.SemesterId, ct);
    
    return Ok(new { message = "Enrolled. Fees updated." });
}

[HttpDelete("api/academic/course-enrollment/{enrollmentId}")]
[Authorize]
public async Task<IActionResult> DropCourse(
    Guid enrollmentId,
    CancellationToken ct = default)
{
    var enrollment = await _db.CourseEnrollments.FindAsync(enrollmentId, ct);
    
    _db.CourseEnrollments.Remove(enrollment);
    await _db.SaveChangesAsync(ct);
    
    // TRIGGER: Recalculate fees
    await RecalculateStudentFeesAsync(enrollment.StudentId, enrollment.SemesterId, ct);
    
    return Ok(new { message = "Course dropped. Fees updated." });
}

private async Task RecalculateStudentFeesAsync(
    Guid studentId,
    Guid semesterId,
    CancellationToken ct = default)
{
    // Get all enrolled courses
    var enrolledCourses = await _db.CourseEnrollments
        .Where(e => e.StudentId == studentId
            && e.SemesterId == semesterId
            && e.EnrollmentStatus == "enrolled")
        .Include(e => e.Course)
        .ToListAsync(ct);
    
    var totalCredits = enrolledCourses.Sum(e => e.Course.CreditHours);
    var config = await _db.SystemConfiguration
        .FirstOrDefaultAsync(c => c.UniversityId == enrolledCourses.First().Course.UniversityId, ct);
    
    var studentFees = await _db.StudentFees
        .FirstOrDefaultAsync(f => f.StudentId == studentId
            && f.SemesterId == semesterId, ct);
    
    if (studentFees == null)
    {
        studentFees = new StudentFee
        {
            StudentId = studentId,
            SemesterId = semesterId,
            FeePerCreditHour = config.FeePerCreditHour,
        };
        _db.StudentFees.Add(studentFees);
    }
    
    // Update enrolled credits
    studentFees.TotalEnrolledCredits = totalCredits;
    // Total_fees auto-calculates via GENERATED AS column
    studentFees.LastRecalculatedAt = DateTime.UtcNow;
    
    _db.StudentFees.Update(studentFees);
    await _db.SaveChangesAsync(ct);
    
    // Notify student of fee change
    await _notificationService.SendAsync(
        $"Your fees have been updated: ₱{studentFees.AmountDue}",
        recipients: new[] { studentId },
        priority: "info"
    );
}
```

### Real-Time Fee Endpoint

```csharp
[HttpGet("api/billing/student/{studentId}/semester/{semesterId}/fees")]
[Authorize]
public async Task<IActionResult> GetStudentFees(
    Guid studentId,
    Guid semesterId,
    CancellationToken ct = default)
{
    var fees = await _db.StudentFees
        .FirstOrDefaultAsync(f => f.StudentId == studentId
            && f.SemesterId == semesterId, ct);
    
    if (fees == null)
        return NotFound();
    
    return Ok(new
    {
        totalEnrolledCredits = fees.TotalEnrolledCredits,
        feePerCreditHour = fees.FeePerCreditHour,
        tuitionFee = fees.TotalEnrolledCredits * fees.FeePerCreditHour,
        labFee = fees.LabFee,
        technologyFee = fees.TechnologyFee,
        libraryFee = fees.LibraryFee,
        activityFee = fees.ActivityFee,
        
        totalFees = fees.TotalFees,
        scholarships = fees.Scholarships,
        feeWaivers = fees.FeeWaivers,
        financialAid = fees.FinancialAid,
        
        amountDue = fees.AmountDue,
        lastRecalculatedAt = fees.LastRecalculatedAt
    });
}
```

---

## Part G — Onboarding Wizard Enhancements

### New Wizard Questions

```csharp
public class GovernanceWizardStep : WizardStep
{
    public async Task<WizardResponse> ExecuteAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<SystemConfigurationService>();
        
        // Question 1: Replication Lag
        var lag = await AskUserAsync(new AskOptions
        {
            Question = "What replication lag (hours) do you want between primary and replica databases?",
            Subtitle = "Higher = more stable reads. Lower = fresher reads.",
            DefaultValue = "12",
            Min = 1,
            Max = 72,
            Help = "This delays replica sync intentionally to prevent race conditions during high load."
        });
        
        await config.SetAsync("replication_lag_hours", int.Parse(lag));
        
        // Question 2: Class Capacity
        var regularCapacity = await AskUserAsync(new AskOptions
        {
            Question = "What is your regular class capacity (seats)?",
            DefaultValue = "60",
            Help = "Maximum students per section during normal classes."
        });
        
        var examCapacity = await AskUserAsync(new AskOptions
        {
            Question = "What is your exam hall capacity (seats)?",
            DefaultValue = "80",
            Help = "Maximum students per exam session (usually higher due to spread-out seating)."
        });
        
        await config.SetAsync("regular_class_capacity", int.Parse(regularCapacity));
        await config.SetAsync("exam_class_capacity", int.Parse(examCapacity));
        
        // Question 3: Fee Structure
        var feePerCredit = await AskUserAsync(new AskOptions
        {
            Question = "What is your base fee per credit hour (PKR)?",
            DefaultValue = "10000",
            Help = "Example: 4-credit course = 4 × 10000 = 40000 PKR"
        });
        
        await config.SetAsync("fee_per_credit_hour", decimal.Parse(feePerCredit));
        
        return WizardResponse.Success("Governance configured");
    }
}
```

---

## Checklist

- [ ] Replication lag configuration in wizard
- [ ] Database router (write to primary, read from replica with lag)
- [ ] Grade override workflow + approval matrix
- [ ] Anonymous feedback system (zero traceability)
- [ ] Teacher feedback profile (aggregated stats only)
- [ ] Section override with capacity checks
- [ ] Automatic fee adjustment on section change
- [ ] Incomplete course re-enrollment job
- [ ] Automatic fee removal for incomplete courses
- [ ] Dynamic fee recalculation on credit hour changes
- [ ] Audit logging for all overrides
- [ ] Integration with S2 (admissions), S3 (examination), S7 (finance)
- [ ] API endpoints for all overrides
- [ ] Real-time fee calculation endpoints
- [ ] Encryption service for feedback text
- [ ] NLP service for sentiment analysis + theme extraction

---

**This is governance + smart defaults + automatic corrections.**

University tells Nocturne once (at setup) what their policies are. Then Nocturne enforces them automatically. Overrides are tracked. Fees recalculate. Data is safe.

This is enterprise-grade ERP thinking. 🔥
