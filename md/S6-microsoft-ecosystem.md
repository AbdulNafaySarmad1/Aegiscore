# AegisCore — Sprint 6: Microsoft Ecosystem (Teams + Moodle LMS + Power BI)

## Role
You are a senior .NET 10 / C# 14 engineer integrating AegisCore with the Microsoft ecosystem: automatic Teams class creation, Moodle LMS roster sync, and embedded Power BI dashboards for role-specific analytics.

---

## Task 1 — Microsoft Teams Auto-Provisioning

### Setup
- Azure AD app registration (delegated permissions):
  - `EduAdmin.ReadWrite` — create educationClass
  - `Directory.ReadWrite.All` — manage groups
  - `Calendars.ReadWrite` — create Teams meetings

### Database Table
```sql
CREATE TABLE teams_sync_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    timetable_entry_id UUID NOT NULL REFERENCES timetable_entries(id),
    teams_team_id VARCHAR(200),
    teams_channel_id VARCHAR(200),
    sync_status VARCHAR(20) DEFAULT 'pending', -- pending | success | failed
    error_message TEXT,
    synced_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Workflow

1. **Trigger: Timetable Entry Created**
   ```csharp
   [HttpPost("/api/timetable")]
   public async Task<IActionResult> CreateTimetableEntryAsync(TimetableEntryDto dto)
   {
       var entry = await _timetableService.CreateAsync(dto);
       
       // Fire background job
       await _backgroundJobClient.EnqueueAsync<TeamsProvisioningService>(
           x => x.ProvisionTeamsClassAsync(entry.Id, CancellationToken.None)
       );
       
       return Ok(entry);
   }
   ```

2. **Teams Provisioning Service**
   ```csharp
   public class TeamsProvisioningService
   {
       public async Task ProvisionTeamsClassAsync(Guid timetableEntryId, CancellationToken ct = default)
       {
           var entry = await _db.TimetableEntries
               .Include(x => x.Course)
               .Include(x => x.Faculty)
               .Include(x => x.Enrollments)
               .FirstAsync(x => x.Id == timetableEntryId, ct);
           
           var graphClient = new GraphServiceClient(_tokenProvider);
           
           try
           {
               // 1. Create educationClass
               var educationClass = new EducationClass
               {
                   DisplayName = $"{entry.Course.Code} - {entry.Faculty.FirstName} {entry.Faculty.LastName}",
                   CourseNumber = entry.Course.Code,
                   ExternalId = timetableEntryId.ToString(),
                   ExternalSource = EducationExternalSourceType.Sis
               };
               
               var createdClass = await graphClient.Education.Classes
                   .PostAsync(educationClass, ct);
               
               // 2. Add faculty as owner
               await graphClient.Education.Classes[createdClass.Id].Members
                   .PostAsync(new EducationUser
                   {
                       UserId = entry.Faculty.AzureAdUserId,
                       Role = EducationUserRole.Teacher
                   }, ct);
               
               // 3. Add enrolled students
               foreach (var enrollment in entry.Enrollments)
               {
                   await graphClient.Education.Classes[createdClass.Id].Members
                       .PostAsync(new EducationUser
                       {
                           UserId = enrollment.Student.AzureAdUserId,
                           Role = EducationUserRole.Student
                       }, ct);
               }
               
               // 4. Create recurring Teams meetings (one per timetable slot)
               var startDate = DateTime.UtcNow.AddDays(7);
               var meeting = new Event
               {
                   Subject = $"{entry.Course.Code} Class",
                   Start = new DateTimeTimeZone
                   {
                       DateTime = startDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                       TimeZone = "Asia/Karachi"
                   },
                   End = new DateTimeTimeZone
                   {
                       DateTime = startDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                       TimeZone = "Asia/Karachi"
                   },
                   IsReminderOn = true,
                   Recurrence = new PatternedRecurrence
                   {
                       Pattern = new RecurrencePattern
                       {
                           Type = RecurrencePatternType.Weekly,
                           DaysOfWeek = ParseWeekDays(entry.DayOfWeek),
                           Interval = 1
                       }
                   },
                   IsOnlineMeeting = true,
                   OnlineMeetingProvider = OnlineMeetingProviderType.TeamsForBusiness
               };
               
               await graphClient.Me.Events.PostAsync(meeting, ct);
               
               // 5. Log success
               var syncLog = new TeamsSyncLog
               {
                   TimetableEntryId = timetableEntryId,
                   TeamsTeamId = createdClass.Id,
                   SyncStatus = "success",
                   SyncedAt = DateTime.UtcNow
               };
               _db.TeamsSyncLogs.Add(syncLog);
               await _db.SaveChangesAsync(ct);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Teams provisioning failed for timetable entry {Id}", timetableEntryId);
               var syncLog = new TeamsSyncLog
               {
                   TimetableEntryId = timetableEntryId,
                   SyncStatus = "failed",
                   ErrorMessage = ex.Message
               };
               _db.TeamsSyncLogs.Add(syncLog);
               await _db.SaveChangesAsync(ct);
           }
       }
   }
   ```

3. **Enrollment Changes**
   - When student adds/drops course: sync to Teams (add/remove member)
   - Deletion of timetable entry: archive Teams class

---

## Task 2 — Moodle LMS Integration

### Setup
- Moodle instance (self-hosted on Hetzner)
- LTI 1.3 connection configured between AegisCore and Moodle
- AegisCore = Tool Consumer, Moodle = Tool Provider

### Database Table
```sql
CREATE TABLE lms_sync_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    course_id UUID NOT NULL REFERENCES courses(id),
    sync_type VARCHAR(20) NOT NULL, -- enrollment | grades | materials
    sync_status VARCHAR(20) DEFAULT 'pending',
    synced_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Roster Sync (Course Enrollments → Moodle)
```csharp
public class MoodleRosterSyncService
{
    public async Task SyncEnrollmentAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _db.Courses
            .Include(x => x.Enrollments)
            .FirstAsync(x => x.Id == courseId, ct);
        
        var moodleClient = new MoodleClient(_moodleApiKey, _moodleBaseUrl);
        
        foreach (var enrollment in course.Enrollments)
        {
            var user = enrollment.Student;
            
            // Enroll student in Moodle course
            await moodleClient.EnrollUserAsync(new EnrollUserRequest
            {
                UserId = user.MoodleUserId, // synced during user creation
                CourseId = course.MoodleCourseId,
                RoleId = 5 // Student role in Moodle
            }, ct);
        }
        
        var syncLog = new LmsSyncLog
        {
            CourseId = courseId,
            SyncType = "enrollment",
            SyncStatus = "success",
            SyncedAt = DateTime.UtcNow
        };
        _db.LmsSyncLogs.Add(syncLog);
        await _db.SaveChangesAsync(ct);
    }
}
```

### Grade Passback (Moodle Grades → AegisCore)
```csharp
public class MoodleGradePassbackService
{
    public async Task SyncGradesAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _db.Courses
            .Include(x => x.Enrollments)
            .FirstAsync(x => x.Id == courseId, ct);
        
        var moodleClient = new MoodleClient(_moodleApiKey, _moodleBaseUrl);
        
        // Get grades from Moodle
        var grades = await moodleClient.GetCourseGradesAsync(course.MoodleCourseId, ct);
        
        foreach (var grade in grades)
        {
            var enrollment = course.Enrollments.FirstOrDefault(
                x => x.Student.MoodleUserId == grade.UserId
            );
            
            if (enrollment != null)
            {
                // Create or update marks in AegisCore from Moodle
                var marks = new CourseMarks
                {
                    CourseId = courseId,
                    StudentId = enrollment.StudentId,
                    AssessmentComponentId = grade.AssignmentId, // map Moodle assignment to component
                    ObtainedMarks = (int)grade.Grade
                };
                
                _db.CourseMarks.Add(marks);
            }
        }
        
        await _db.SaveChangesAsync(ct);
    }
}
```

### SSO (Microsoft 365 Login → Moodle)
- Moodle OAuth2 provider: Azure AD
- Same login credentials across AegisCore, Teams, Moodle
- No separate password management

---

## Task 3 — Power BI Embedded Dashboards

### Setup
- Azure Power BI Premium capacity (or Pro licenses)
- AegisCore service principal with Power BI API permissions
- Data gateway: PostgreSQL → Power BI Service

### Database for Embed Config
```sql
CREATE TABLE power_bi_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    report_name VARCHAR(200) NOT NULL,
    power_bi_report_id VARCHAR(200) NOT NULL,
    power_bi_workspace_id VARCHAR(200) NOT NULL,
    target_role VARCHAR(50) NOT NULL, -- vc | registrar | dean | hod | faculty
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Reports to Build
1. **VC Dashboard** (strategic overview)
   - Student enrollment trend (year-over-year)
   - Revenue collected vs budget
   - Faculty productivity (courses taught, research output)
   - At-risk students (failing courses)
   - OBE attainment by program

2. **Registrar Dashboard** (operations)
   - Semester calendar progress
   - Admission pipeline (applicants → enrolled)
   - Course utilization (capacity vs enrolled)
   - Grade distribution by course
   - Late fee collections

3. **Dean Dashboard** (faculty-level)
   - Faculty workload (credit hours taught)
   - Student feedback scores
   - Research metrics (publications, grants)
   - Department-wise OBE attainment
   - Faculty attendance (leave tracking)

4. **HOD Dashboard** (department)
   - Departmental OBE goals vs attainment
   - Course-wise CLO achievement
   - Student retention rates
   - Faculty performance appraisals
   - Budget utilization

5. **Faculty Dashboard** (personal)
   - Courses taught (current semester)
   - Student grades distribution
   - Course completion status
   - Student feedback summary
   - Research output

### Embedded Reports Service
```csharp
public class PowerBIEmbeddedService
{
    public async Task<EmbedConfig> GetEmbedConfigAsync(Guid userId, string reportRole, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(userId);
        var report = await _db.PowerBIReports.FirstAsync(x => x.TargetRole == reportRole, ct);
        
        var client = new PowerBIClient(new TokenCredentials(_accessToken));
        
        // Generate embed token
        var generateTokenRequestParameters = new GenerateTokenRequest(
            accessLevel: "View",
            datasetId: report.DatasetId
        );
        
        if (user.Role == "HOD")
        {
            // Apply RLS (Row-Level Security): HOD sees only their department
            var rls = new EffectiveIdentity(
                username: user.Email,
                roles: new List<string> { "HOD" },
                customData: new Dictionary<string, string> { { "department_id", user.DepartmentId.ToString() } }
            );
            generateTokenRequestParameters.EffectiveIdentities = new List<EffectiveIdentity> { rls };
        }
        
        var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(
            report.PowerBIWorkspaceId,
            report.PowerBIReportId,
            generateTokenRequestParameters,
            ct
        );
        
        return new EmbedConfig
        {
            ReportId = report.PowerBIReportId,
            EmbedUrl = report.EmbedUrl,
            EmbedToken = tokenResponse.Token,
            ExpirationTime = tokenResponse.Expiration
        };
    }
}
```

### Frontend (Blazor / Razor Pages)
```html
<div id="reportContainer" style="height: 600px;">
    <iframe id="report" 
            src="" 
            frameborder="0" 
            allowfullscreen
            style="width: 100%; height: 100%;"></iframe>
</div>

<script>
    const embedConfig = @Html.Raw(Json.Serialize(Model.EmbedConfig));
    
    const config = {
        type: "report",
        id: embedConfig.ReportId,
        embedUrl: embedConfig.EmbedUrl,
        accessToken: embedConfig.EmbedToken,
        tokenExpiration: embedConfig.ExpirationTime
    };
    
    const report = powerbi.embed(
        document.getElementById("reportContainer"),
        config
    );
</script>
```

---

## Deliverables Checklist
- [ ] Teams auto-provisioning service (Graph API integration)
- [ ] Teams enrollment sync (add/remove students)
- [ ] Moodle roster sync (course enrollments)
- [ ] Moodle grade passback (assignment grades → AegisCore marks)
- [ ] SSO setup (Azure AD → Moodle)
- [ ] 5 Power BI dashboards (VC, Registrar, Dean, HOD, Faculty)
- [ ] Row-level security for HOD/Faculty dashboards
- [ ] Embedded report service + frontend
- [ ] All error logging and retry logic
- [ ] Integration tests
