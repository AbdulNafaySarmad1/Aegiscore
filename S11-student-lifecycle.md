# AegisCore — Sprint 11: Extended Student Lifecycle (Placement + Alumni + Activities)

## Role
You are a senior .NET 10 / C# 14 engineer building the extended student lifecycle modules for AegisCore. These manage student development beyond academics: placement, alumni engagement, and extracurricular activities.

---

## Task 1 — Placement Cell

### Database Tables
```sql
CREATE TABLE recruiters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_name VARCHAR(300) NOT NULL,
    company_url VARCHAR(500),
    company_address TEXT,
    contact_person VARCHAR(100),
    contact_email VARCHAR(200),
    contact_phone VARCHAR(20),
    industry VARCHAR(100),
    hiring_for_roles VARCHAR(500), -- comma-separated
    approval_status VARCHAR(20) DEFAULT 'pending', -- pending | approved | rejected
    approved_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE placement_drives (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    recruiter_id UUID NOT NULL REFERENCES recruiters(id),
    drive_name VARCHAR(200) NOT NULL,
    scheduled_date DATE NOT NULL,
    scheduled_time TIME NOT NULL,
    location VARCHAR(200),
    positions_available INT NOT NULL,
    job_description TEXT,
    eligibility_cgpa DECIMAL(3, 2),
    status VARCHAR(20) DEFAULT 'scheduled', -- scheduled | completed | cancelled
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE placement_registrations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    placement_drive_id UUID NOT NULL REFERENCES placement_drives(id),
    student_id UUID NOT NULL REFERENCES users(id),
    applied_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status VARCHAR(20) DEFAULT 'registered', -- registered | shortlisted | selected | rejected
    selected_date TIMESTAMPTZ,
    salary_offered DECIMAL(12, 2),
    joining_date DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE placement_interviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    registration_id UUID NOT NULL REFERENCES placement_registrations(id),
    interview_round INT NOT NULL, -- 1, 2, 3 (tech, HR, etc)
    interview_date TIMESTAMPTZ,
    interview_result VARCHAR(20), -- pending | passed | failed
    interview_feedback TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Student Eligibility Engine
```csharp
public class PlacementEligibilityService
{
    public bool IsEligibleForDrive(StudentEntity student, PlacementDrive drive)
    {
        // Eligibility checks
        var cgpa = _gradingService.CalculateCGPA(student.Id);
        if (cgpa < drive.EligibilityCgpa) return false;
        
        // Degree completion %
        var completedCredits = student.CompletedCredits;
        var requiredCredits = student.Program.TotalCredits;
        var completionPercent = (completedCredits / requiredCredits) * 100;
        if (completionPercent < 80) return false; // Min 80% degree completion
        
        // No active arrears (failed courses)
        var failedCourses = student.CourseGrades.Count(x => !x.IsPassing);
        if (failedCourses > 0) return false;
        
        return true;
    }
}
```

### Placement Statistics
- Total students registered
- Placement rate (% placed / % of batch)
- Average package (salary)
- Highest package
- Companies visited
- Timeline (days from graduation to placement)

### Placement Portal (Student View)
- Browse upcoming drives
- Register for drives (if eligible)
- View interview results
- Accept/reject offer
- Download joining letter

### Placement Portal (Admin View)
- Recruiter onboarding
- Drive scheduling
- Student eligibility verification
- Interview coordination
- Offer letter generation
- Placement statistics dashboard

---

## Task 2 — Alumni Management

### Database Tables
```sql
CREATE TABLE alumni (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL UNIQUE REFERENCES users(id),
    graduation_year INT NOT NULL,
    batch_number VARCHAR(50),
    alumni_id VARCHAR(50) NOT NULL UNIQUE,
    current_company VARCHAR(200),
    current_designation VARCHAR(100),
    current_location VARCHAR(200),
    bio TEXT,
    profile_photo_url TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE alumni_event_registrations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    alumni_id UUID NOT NULL REFERENCES alumni(id),
    event_id UUID NOT NULL REFERENCES events(id),
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE alumni_feedback_surveys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    alumni_id UUID NOT NULL REFERENCES alumni(id),
    survey_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    program_relevance_score INT, -- 1-5 scale
    career_readiness_score INT,
    institution_satisfaction_score INT,
    feedback_text TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE donations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    alumni_id UUID NOT NULL REFERENCES alumni(id),
    donation_amount DECIMAL(15, 2) NOT NULL,
    donation_date DATE NOT NULL,
    donation_type VARCHAR(50), -- one_time | recurring | endowment
    purpose VARCHAR(200), -- scholarship | infrastructure | research
    receipt_number VARCHAR(50),
    payment_method VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Alumni Portal
- Update profile (company, designation, location, bio)
- View job board (internships, career opportunities from recruiters)
- Browse alumni network (search by batch, company, location)
- RSVP to alumni events
- Provide feedback surveys (feeds OBE framework)
- Make donations (one-time or recurring)

### Alumni Admin Portal
- Alumni database (batch/department/year filtering)
- Event management + invitations
- Survey management
- Donation tracking
- Alumni engagement metrics

---

## Task 3 — Extracurricular Activities

### Database Tables
```sql
CREATE TABLE student_societies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    society_name VARCHAR(200) NOT NULL,
    description TEXT,
    faculty_advisor_id UUID NOT NULL REFERENCES users(id),
    president_id UUID REFERENCES users(id),
    founded_year INT,
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE society_memberships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    society_id UUID NOT NULL REFERENCES student_societies(id),
    student_id UUID NOT NULL REFERENCES users(id),
    role VARCHAR(50), -- member | secretary | treasurer | president
    joined_date DATE NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE student_activities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    activity_name VARCHAR(200) NOT NULL,
    activity_type VARCHAR(50), -- sports | cultural | competition | volunteer
    organizing_society_id UUID REFERENCES student_societies(id),
    scheduled_date DATE NOT NULL,
    scheduled_time TIME,
    location VARCHAR(200),
    description TEXT,
    organizer_id UUID NOT NULL REFERENCES users(id),
    approval_status VARCHAR(20) DEFAULT 'pending', -- pending | approved | completed | cancelled
    approved_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE activity_participation (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    activity_id UUID NOT NULL REFERENCES student_activities(id),
    student_id UUID NOT NULL REFERENCES users(id),
    participation_role VARCHAR(50), -- participant | organizer | sponsor
    participation_status VARCHAR(20) DEFAULT 'registered', -- registered | participated | completed
    points_earned INT, -- co-curricular points for student record
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Society & Activity Management
- Students can register societies
- Faculty advisor approval required
- Create events/activities
- Track attendance + co-curricular points
- Annual society reports

### Student Activity Dashboard
- View ongoing activities
- Register for activities
- View co-curricular transcript
- Contribution to overall student development

---

## Task 4 — Health & Medical Center

### Database Tables
```sql
CREATE TABLE student_health_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL UNIQUE REFERENCES users(id),
    blood_type VARCHAR(5),
    allergies TEXT,
    chronic_conditions TEXT,
    emergency_contact_name VARCHAR(100),
    emergency_contact_phone VARCHAR(20),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE medical_visits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES users(id),
    visit_date TIMESTAMPTZ NOT NULL,
    visit_reason TEXT,
    medical_officer_id UUID NOT NULL REFERENCES users(id),
    diagnosis TEXT,
    treatment TEXT,
    prescribed_medicines TEXT,
    rest_days INT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE sick_leave_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES users(id),
    from_date DATE NOT NULL,
    to_date DATE NOT NULL,
    medical_visit_id UUID REFERENCES medical_visits(id),
    status VARCHAR(20) DEFAULT 'pending', -- pending | approved | rejected
    approved_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Medical Center Portal
- Students book appointments
- View medical history (private)
- Request sick leave (auto-linked to attendance)
- Medical fitness certificate issuance

---

## Deliverables Checklist
- [ ] Recruiter onboarding workflow
- [ ] Placement drive scheduling
- [ ] Student eligibility engine
- [ ] Interview coordination + results
- [ ] Placement statistics dashboard
- [ ] Alumni profile management
- [ ] Alumni job board + networking
- [ ] Alumni survey collection (feeds OBE)
- [ ] Donation tracking + receipts
- [ ] Student society management
- [ ] Co-curricular activity tracking
- [ ] Activity approval workflows
- [ ] Student health records (HIPAA-grade privacy)
- [ ] Medical visit logging
- [ ] Sick leave requests (auto-sync to attendance)
- [ ] All notifications
- [ ] Tests
