# Nocturne — Course Trees, Curriculum Management, Teacher Workload & Offline Attendance (U11)

## Role
You are a senior platform architect designing Nocturne's curriculum + workload + offline systems. This covers:

1. **Course Trees (Curriculum Builder)** — Visual graph editor for program curricula, not code
2. **Curriculum Management** — Add/remove/update courses, change credit hours, prerequisite chains
3. **CIS Mapping** — Link CLOs (learning outcomes) to courses for HEC accreditation
4. **Teacher Workload Management** — Cap hours per semester, prevent over-allocation
5. **Offline-First Attendance** — 30-minute offline storage with sync when online

---

## Part A — Course Trees & Visual Curriculum Builder

### Problem: Code-Based Curriculum Management

**Current state (bad):**
```
// Hard to update, error-prone
const curriculum = {
  CS101: { prereq: [], credits: 3 },
  CS201: { prereq: [CS101], credits: 3 },
  CS301: { prereq: [CS201, MATH201], credits: 4 }
}
```

**Solution: Visual graph-based editor**
- Drag-and-drop course nodes
- Click to connect prerequisites
- Double-click to edit credit hours
- See entire curriculum at once
- Change history tracked automatically

### Course Tree Data Model

```sql
CREATE TABLE programs (
    id UUID PRIMARY KEY,
    university_id UUID NOT NULL REFERENCES universities(id),
    
    -- Program metadata
    code VARCHAR(20) NOT NULL, -- "CS", "CE", "ME"
    name VARCHAR(200) NOT NULL, -- "Computer Science", "Civil Engineering"
    level ENUM('bachelors', 'masters', 'phd'),
    duration_years INT, -- 4, 2, 3
    total_credit_hours INT, -- 120, 60, 48
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(university_id, code, level)
);

CREATE TABLE curriculum_versions (
    id UUID PRIMARY KEY,
    program_id UUID NOT NULL REFERENCES programs(id),
    
    -- Versioning
    version_number INT NOT NULL, -- 1, 2, 3
    year_effective INT NOT NULL, -- 2024, 2025, 2026
    status ENUM('draft', 'approved', 'active', 'archived'), -- Default: draft
    
    -- Approval workflow
    created_by UUID NOT NULL REFERENCES users(id),
    approved_by UUID REFERENCES users(id),
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    approved_at TIMESTAMPTZ,
    
    -- Description
    changelog TEXT, -- "Increased Digital Logic from 2+1 to 3+1", etc.
    
    UNIQUE(program_id, version_number),
    UNIQUE(program_id, year_effective)
);

CREATE TABLE curriculum_courses (
    id UUID PRIMARY KEY,
    curriculum_version_id UUID NOT NULL REFERENCES curriculum_versions(id),
    course_id UUID NOT NULL REFERENCES courses(id),
    
    -- Placement in tree
    semester INT NOT NULL, -- 1, 2, 3, 4, 5, 6, 7, 8
    is_mandatory BOOLEAN DEFAULT true,
    is_elective BOOLEAN DEFAULT false,
    
    -- Credits (can change per curriculum version)
    theory_credit_hours INT NOT NULL, -- e.g., 3
    lab_credit_hours INT NOT NULL, -- e.g., 1
    total_credit_hours INT GENERATED ALWAYS AS (theory_credit_hours + lab_credit_hours),
    
    -- CLO mapping (for HEC accreditation)
    mapped_clos TEXT[], -- Array of CLO IDs this course covers
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(curriculum_version_id, course_id, semester)
);

CREATE TABLE curriculum_prerequisites (
    id UUID PRIMARY KEY,
    curriculum_version_id UUID NOT NULL REFERENCES curriculum_versions(id),
    
    -- Prerequisite relationship
    course_id UUID NOT NULL REFERENCES courses(id),
    prerequisite_course_id UUID NOT NULL REFERENCES courses(id),
    
    -- Relationship type
    relationship ENUM('prerequisite', 'corequisite', 'post-requisite'),
    
    -- Can be satisfied by alternative courses
    can_substitute_with UUID[] REFERENCES courses(id), -- e.g., ["MATH201", "MATH202"]
    
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    CONSTRAINT no_self_prerequisite CHECK (course_id != prerequisite_course_id),
    UNIQUE(curriculum_version_id, course_id, prerequisite_course_id)
);

CREATE TABLE curriculum_change_history (
    id UUID PRIMARY KEY,
    curriculum_version_id UUID NOT NULL REFERENCES curriculum_versions(id),
    
    -- What changed
    change_type ENUM('course_added', 'course_removed', 'course_moved', 'credits_changed', 'prerequisite_changed'),
    course_id UUID REFERENCES courses(id),
    
    -- Before/after
    before_value JSONB, -- e.g., {"semester": 3, "credits": 3}
    after_value JSONB,  -- e.g., {"semester": 4, "credits": 4}
    
    -- Who changed it
    changed_by UUID NOT NULL REFERENCES users(id),
    changed_at TIMESTAMPTZ DEFAULT NOW(),
    reason TEXT -- "Updated per HEC requirement", etc.
);
```

### Visual Curriculum Editor (Frontend)

```javascript
// React component using vis-network (graph visualization)

import { Network, DataSet } from 'vis-network/peer/esm/vis-network';

export function CurriculumBuilder({ curriculumVersionId }) {
    const [nodes, setNodes] = useState([]);
    const [edges, setEdges] = useState([]);
    const [selectedNode, setSelectedNode] = useState(null);
    
    // Load curriculum
    useEffect(() => {
        const loadCurriculum = async () => {
            const response = await fetch(
                `/api/curriculum/versions/${curriculumVersionId}/graph`
            );
            const { courses, prerequisites } = await response.json();
            
            // Convert courses to vis-network nodes
            const nodes = courses.map(course => ({
                id: course.id,
                label: `${course.code}\n${course.name}\n${course.theoryCredits}+${course.labCredits}`,
                title: course.description,
                color: {
                    background: getSemesterColor(course.semester),
                    highlight: '#FF0000'
                },
                font: { size: 14 },
                shape: 'box',
                data: course // Store full data in node
            }));
            
            // Convert prerequisites to vis-network edges
            const edges = prerequisites.map(prereq => ({
                from: prereq.prerequisiteCourseId,
                to: prereq.courseId,
                label: prereq.relationship,
                arrows: { to: true },
                color: getRelationshipColor(prereq.relationship)
            }));
            
            setNodes(nodes);
            setEdges(edges);
        };
        
        loadCurriculum();
    }, [curriculumVersionId]);
    
    // Render graph
    useEffect(() => {
        if (!nodes.length) return;
        
        const container = document.getElementById('network-container');
        const data = {
            nodes: new DataSet(nodes),
            edges: new DataSet(edges)
        };
        const options = {
            physics: {
                enabled: true,
                stabilization: { iterations: 200 }
            },
            interaction: {
                dragNodes: true,
                dragView: true,
                zoomView: true
            }
        };
        
        const network = new Network(container, data, options);
        
        // Click to select node
        network.on('click', event => {
            if (event.nodes.length > 0) {
                const nodeId = event.nodes[0];
                const node = nodes.find(n => n.id === nodeId);
                setSelectedNode(node);
            }
        });
        
    }, [nodes, edges]);
    
    // Edit selected node
    const handleUpdateCredits = async (courseId, newTheory, newLab) => {
        await fetch(`/api/curriculum/versions/${curriculumVersionId}/courses/${courseId}`, {
            method: 'PATCH',
            body: JSON.stringify({
                theoryCredits: newTheory,
                labCredits: newLab,
                reason: 'Updated via curriculum builder'
            })
        });
        
        // Refresh
        location.reload();
    };
    
    return (
        <div>
            <div id="network-container" style={{ width: '100%', height: '600px' }} />
            
            {selectedNode && (
                <div className="course-editor">
                    <h3>{selectedNode.data.name}</h3>
                    <p>Code: {selectedNode.data.code}</p>
                    <p>Semester: {selectedNode.data.semester}</p>
                    
                    <div>
                        <label>Theory Credit Hours:</label>
                        <input 
                            type="number" 
                            defaultValue={selectedNode.data.theoryCredits}
                            id="theory-credits"
                        />
                    </div>
                    
                    <div>
                        <label>Lab Credit Hours:</label>
                        <input 
                            type="number" 
                            defaultValue={selectedNode.data.labCredits}
                            id="lab-credits"
                        />
                    </div>
                    
                    <button onClick={() => {
                        const theory = document.getElementById('theory-credits').value;
                        const lab = document.getElementById('lab-credits').value;
                        handleUpdateCredits(selectedNode.id, theory, lab);
                    }}>
                        Update
                    </button>
                </div>
            )}
        </div>
    );
}

function getSemesterColor(semester) {
    const colors = [
        '#FF6B6B', // Red - Semester 1
        '#4ECDC4', // Teal - Semester 2
        '#45B7D1', // Blue - Semester 3
        '#FFA07A', // Light Coral - Semester 4
        '#98D8C8', // Mint - Semester 5
        '#F7DC6F', // Yellow - Semester 6
        '#BB8FCE', // Purple - Semester 7
        '#85C1E2'  // Light Blue - Semester 8
    ];
    return colors[(semester - 1) % colors.length];
}

function getRelationshipColor(relationship) {
    return {
        'prerequisite': '#FF0000',
        'corequisite': '#FFA500',
        'post-requisite': '#00AA00'
    }[relationship] || '#000000';
}
```

### Curriculum Management API

```csharp
[ApiController]
[Route("api/curriculum")]
public class CurriculumController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    
    // Get curriculum as graph (for visual editor)
    [HttpGet("versions/{versionId}/graph")]
    public async Task<IActionResult> GetCurriculumGraph(Guid versionId, CancellationToken ct = default)
    {
        var version = await _db.CurriculumVersions
            .Include(v => v.Courses).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);
        
        var prerequisites = await _db.CurriculumPrerequisites
            .Where(p => p.CurriculumVersionId == versionId)
            .ToListAsync(ct);
        
        return Ok(new
        {
            courses = version.Courses.Select(cc => new
            {
                id = cc.Course.Id,
                code = cc.Course.Code,
                name = cc.Course.Name,
                description = cc.Course.Description,
                semester = cc.Semester,
                theoryCredits = cc.TheoryCreditHours,
                labCredits = cc.LabCreditHours,
                isMandatory = cc.IsMandatory,
                isElective = cc.IsElective,
                mappedClos = cc.MappedClos
            }),
            prerequisites = prerequisites.Select(p => new
            {
                courseId = p.CourseId,
                prerequisiteCourseId = p.PrerequisiteCourseId,
                relationship = p.Relationship.ToString(),
                canSubstituteWith = p.CanSubstituteWith
            })
        });
    }
    
    // Add course to curriculum
    [HttpPost("versions/{versionId}/courses")]
    [Authorize(Roles = "HOD, VC")]
    public async Task<IActionResult> AddCourseToCurriculum(
        Guid versionId,
        [FromBody] AddCourseRequest request,
        CancellationToken ct = default)
    {
        var version = await _db.CurriculumVersions.FindAsync(versionId, ct);
        
        if (version.Status != "draft")
            return BadRequest("Can only edit draft curricula");
        
        var curricCourse = new CurriculumCourse
        {
            CurriculumVersionId = versionId,
            CourseId = request.CourseId,
            Semester = request.Semester,
            TheoryCreditHours = request.TheoryCredits,
            LabCreditHours = request.LabCredits,
            IsMandatory = request.IsMandatory,
            MappedClos = request.MappedClos
        };
        
        _db.CurriculumCourses.Add(curricCourse);
        
        // Log change
        _db.CurriculumChangeHistories.Add(new CurriculumChangeHistory
        {
            CurriculumVersionId = versionId,
            ChangeType = "course_added",
            CourseId = request.CourseId,
            AfterValue = new { semester = request.Semester, credits = request.TheoryCredits + request.LabCredits },
            ChangedBy = User.GetUserId(),
            Reason = request.Reason
        });
        
        await _db.SaveChangesAsync(ct);
        
        return Ok(new { message = "Course added" });
    }
    
    // Remove course from curriculum
    [HttpDelete("versions/{versionId}/courses/{courseId}")]
    [Authorize(Roles = "HOD, VC")]
    public async Task<IActionResult> RemoveCourseFromCurriculum(
        Guid versionId,
        Guid courseId,
        [FromQuery] string reason,
        CancellationToken ct = default)
    {
        var version = await _db.CurriculumVersions.FindAsync(versionId, ct);
        
        if (version.Status != "draft")
            return BadRequest("Can only edit draft curricula");
        
        var curricCourse = await _db.CurriculumCourses
            .FirstOrDefaultAsync(cc => cc.CurriculumVersionId == versionId
                && cc.CourseId == courseId, ct);
        
        if (curricCourse == null)
            return NotFound();
        
        _db.CurriculumCourses.Remove(curricCourse);
        
        // Log change
        _db.CurriculumChangeHistories.Add(new CurriculumChangeHistory
        {
            CurriculumVersionId = versionId,
            ChangeType = "course_removed",
            CourseId = courseId,
            BeforeValue = new { semester = curricCourse.Semester },
            ChangedBy = User.GetUserId(),
            Reason = reason
        });
        
        await _db.SaveChangesAsync(ct);
        
        return Ok(new { message = "Course removed" });
    }
    
    // Update course credits (e.g., 2+1 → 3+1)
    [HttpPatch("versions/{versionId}/courses/{courseId}")]
    [Authorize(Roles = "HOD, VC")]
    public async Task<IActionResult> UpdateCourseCredits(
        Guid versionId,
        Guid courseId,
        [FromBody] UpdateCourseRequest request,
        CancellationToken ct = default)
    {
        var version = await _db.CurriculumVersions.FindAsync(versionId, ct);
        
        if (version.Status != "draft")
            return BadRequest("Can only edit draft curricula");
        
        var curricCourse = await _db.CurriculumCourses
            .FirstOrDefaultAsync(cc => cc.CurriculumVersionId == versionId
                && cc.CourseId == courseId, ct);
        
        var oldCredits = curricCourse.TotalCreditHours;
        
        curricCourse.TheoryCreditHours = request.TheoryCredits;
        curricCourse.LabCreditHours = request.LabCredits;
        
        // Log change
        _db.CurriculumChangeHistories.Add(new CurriculumChangeHistory
        {
            CurriculumVersionId = versionId,
            ChangeType = "credits_changed",
            CourseId = courseId,
            BeforeValue = new { credits = oldCredits },
            AfterValue = new { credits = curricCourse.TotalCreditHours },
            ChangedBy = User.GetUserId(),
            Reason = request.Reason ?? $"Changed from {oldCredits} to {curricCourse.TotalCreditHours}"
        });
        
        await _db.SaveChangesAsync(ct);
        
        return Ok(new { message = $"Credits updated: {request.TheoryCredits}+{request.LabCredits}" });
    }
    
    // Approve curriculum (make it active)
    [HttpPost("versions/{versionId}/approve")]
    [Authorize(Roles = "VC, Chancellor")]
    public async Task<IActionResult> ApproveCurriculum(
        Guid versionId,
        CancellationToken ct = default)
    {
        var version = await _db.CurriculumVersions.FindAsync(versionId, ct);
        
        version.Status = "approved";
        version.ApprovedBy = User.GetUserId();
        version.ApprovedAt = DateTime.UtcNow;
        
        // Notify HOD
        var program = await _db.Programs.FindAsync(version.ProgramId, ct);
        await _notifications.SendAsync(
            $"Curriculum {program.Name} v{version.VersionNumber} approved",
            recipients: await GetDepartmentHeadsAsync(program.Id, ct),
            priority: "high"
        );
        
        await _db.SaveChangesAsync(ct);
        
        return Ok(new { message = "Curriculum approved" });
    }
}
```

---

## Part B — CIS Mapping (Learning Outcomes)

### CLO to Course Mapping

```csharp
[HttpPost("api/curriculum/versions/{versionId}/courses/{courseId}/map-clos")]
[Authorize(Roles = "HOD, Faculty")]
public async Task<IActionResult> MapCourseToLearningOutcomes(
    Guid versionId,
    Guid courseId,
    [FromBody] MapClosRequest request,
    CancellationToken ct = default)
{
    var curricCourse = await _db.CurriculumCourses
        .FirstOrDefaultAsync(cc => cc.CurriculumVersionId == versionId
            && cc.CourseId == courseId, ct);
    
    // Validate that CLOs exist
    var clos = await _db.LearningOutcomes
        .Where(lo => request.ClosIds.Contains(lo.Id))
        .ToListAsync(ct);
    
    if (clos.Count != request.ClosIds.Count)
        return BadRequest("Some CLOs not found");
    
    // Update mapping
    curricCourse.MappedClos = request.ClosIds;
    
    // For HEC accreditation: verify all CLOs are covered by some course
    var allMappedClos = await _db.CurriculumCourses
        .Where(cc => cc.CurriculumVersionId == versionId)
        .SelectMany(cc => cc.MappedClos)
        .Distinct()
        .ToListAsync(ct);
    
    var programClos = await _db.LearningOutcomes
        .Where(lo => lo.DepartmentId == _db.CurriculumVersions
            .First(v => v.Id == versionId).Program.DepartmentId)
        .ToListAsync(ct);
    
    var uncoveredClos = programClos
        .Where(lo => !allMappedClos.Contains(lo.Id))
        .ToList();
    
    if (uncoveredClos.Any())
    {
        // Warn but don't block (can still save)
        return Ok(new
        {
            message = "Course mapped",
            warning = $"Warning: {uncoveredClos.Count} CLOs are not covered by any course"
        });
    }
    
    await _db.SaveChangesAsync(ct);
    
    return Ok(new { message = "CLOs mapped successfully" });
}
```

---

## Part C — Teacher Workload Management

### Teacher Capacity Table

```sql
CREATE TABLE teacher_workloads (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL REFERENCES users(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    
    -- Allocation
    allocated_hours INT NOT NULL, -- Max 16 hours/week
    allocated_sections INT NOT NULL, -- Max 4 sections
    
    -- Tracking
    actual_hours INT DEFAULT 0,
    actual_sections INT DEFAULT 0,
    
    -- Constraints
    prefer_morning BOOLEAN DEFAULT false,
    prefer_afternoon BOOLEAN DEFAULT false,
    
    -- Audit
    allocated_by UUID NOT NULL REFERENCES users(id),
    allocated_at TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(teacher_id, semester_id),
    CONSTRAINT hours_not_exceeded CHECK (actual_hours <= allocated_hours)
);

CREATE TABLE teacher_course_assignments (
    id UUID PRIMARY KEY,
    teacher_id UUID NOT NULL REFERENCES users(id),
    course_section_id UUID NOT NULL REFERENCES course_sections(id),
    semester_id UUID NOT NULL REFERENCES semesters(id),
    
    -- Assignment details
    teaching_hours INT NOT NULL, -- Hours per week for this section
    is_lab BOOLEAN DEFAULT false,
    
    assigned_at TIMESTAMPTZ DEFAULT NOW(),
    assigned_by UUID NOT NULL REFERENCES users(id),
    
    UNIQUE(teacher_id, course_section_id, semester_id)
);
```

### Workload Check Before Assignment

```csharp
[HttpPost("api/academic/teacher-assignments")]
[Authorize(Roles = "HOD")]
public async Task<IActionResult> AssignTeacherToSection(
    [FromBody] AssignTeacherRequest request,
    CancellationToken ct = default)
{
    var workload = await _db.TeacherWorkloads
        .FirstOrDefaultAsync(w => w.TeacherId == request.TeacherId
            && w.SemesterId == request.SemesterId, ct);
    
    if (workload == null)
        return BadRequest("Teacher workload not configured for this semester");
    
    var section = await _db.CourseSections.FindAsync(request.CourseSectionId, ct);
    
    // Check if assignment would exceed hours
    var proposedHours = workload.ActualHours + section.HoursPerWeek;
    
    if (proposedHours > workload.AllocatedHours)
    {
        return BadRequest(new
        {
            error = "Assignment exceeds teacher workload",
            currentHours = workload.ActualHours,
            proposedHours = proposedHours,
            maxHours = workload.AllocatedHours,
            hoursAvailable = workload.AllocatedHours - workload.ActualHours
        });
    }
    
    // Check section count
    var assignedSections = await _db.TeacherCourseAssignments
        .Where(a => a.TeacherId == request.TeacherId
            && a.SemesterId == request.SemesterId)
        .CountAsync(ct);
    
    if (assignedSections + 1 > workload.AllocatedSections)
    {
        return BadRequest(new
        {
            error = "Assignment exceeds max sections",
            currentSections = assignedSections,
            maxSections = workload.AllocatedSections
        });
    }
    
    // Assign
    var assignment = new TeacherCourseAssignment
    {
        TeacherId = request.TeacherId,
        CourseSectionId = request.CourseSectionId,
        SemesterId = request.SemesterId,
        TeachingHours = section.HoursPerWeek,
        IsLab = section.IsLab,
        AssignedBy = User.GetUserId()
    };
    
    _db.TeacherCourseAssignments.Add(assignment);
    
    // Update workload
    workload.ActualHours += section.HoursPerWeek;
    workload.ActualSections += 1;
    
    await _db.SaveChangesAsync(ct);
    
    return Ok(new
    {
        message = "Teacher assigned",
        workload = new
        {
            allocatedHours = workload.AllocatedHours,
            actualHours = workload.ActualHours,
            hoursRemaining = workload.AllocatedHours - workload.ActualHours
        }
    });
}

[HttpGet("api/academic/teachers/{teacherId}/semester/{semesterId}/workload")]
public async Task<IActionResult> GetTeacherWorkload(
    Guid teacherId,
    Guid semesterId,
    CancellationToken ct = default)
{
    var workload = await _db.TeacherWorkloads
        .FirstOrDefaultAsync(w => w.TeacherId == teacherId
            && w.SemesterId == semesterId, ct);
    
    var assignments = await _db.TeacherCourseAssignments
        .Include(a => a.CourseSection).ThenInclude(cs => cs.Course)
        .Where(a => a.TeacherId == teacherId
            && a.SemesterId == semesterId)
        .ToListAsync(ct);
    
    return Ok(new
    {
        workload = new
        {
            allocatedHours = workload.AllocatedHours,
            allocatedSections = workload.AllocatedSections,
            actualHours = workload.ActualHours,
            actualSections = workload.ActualSections,
            hoursRemaining = workload.AllocatedHours - workload.ActualHours,
            sectionsRemaining = workload.AllocatedSections - workload.ActualSections
        },
        assignments = assignments.Select(a => new
        {
            id = a.Id,
            courseCode = a.CourseSection.Course.Code,
            courseName = a.CourseSection.Course.Name,
            section = a.CourseSection.SectionNumber,
            hoursPerWeek = a.TeachingHours,
            isLab = a.IsLab
        })
    });
}
```

---

## Part D — Offline-First Attendance Marking

### Problem: No Internet, Can't Mark Attendance

Teacher on campus, system down, can't mark 60 students present. Loses data.

**Solution: 30-minute offline storage + sync**

```
Teacher marks attendance (offline)
    ↓
Device stores: { timestamp, action, studentId } locally
    ↓
Device goes online
    ↓
Service Worker syncs: "These were marked offline at 2:30 PM"
    ↓
Server accepts with metadata: { offline: true, markedAt: "2:30 PM" }
```

### Offline Storage Schema

```sql
CREATE TABLE attendance_offline_queue (
    id UUID PRIMARY KEY,
    device_id VARCHAR(100) NOT NULL,
    teacher_id UUID NOT NULL REFERENCES users(id),
    
    -- Attendance action
    course_section_id UUID NOT NULL REFERENCES course_sections(id),
    student_id UUID NOT NULL REFERENCES students(id),
    attendance_status ENUM('present', 'absent', 'late'), 
    
    -- Offline metadata
    marked_offline BOOLEAN DEFAULT true,
    offline_marked_at TIMESTAMPTZ NOT NULL, -- When teacher clicked button
    synced_at TIMESTAMPTZ, -- When device went online
    
    -- Sync status
    sync_status ENUM('pending', 'synced', 'failed', 'conflict'),
    sync_error TEXT,
    
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Offline Service Worker (JavaScript)

```javascript
// Register service worker
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/sw.js');
}

// Service Worker: sw.js
const CACHE_NAME = 'attendance-v1';
const OFFLINE_QUEUE_DB = 'nocturne-attendance-offline';

self.addEventListener('install', event => {
    event.waitUntil(caches.open(CACHE_NAME));
});

// Intercept attendance marking requests
self.addEventListener('fetch', event => {
    if (event.request.method === 'POST' && 
        event.request.url.includes('/api/attendance/mark')) {
        
        event.respondWith(
            fetch(event.request)
                .catch(async () => {
                    // Network failed - store offline
                    const data = await event.request.json();
                    await storeOfflineAsync(data);
                    
                    return new Response(
                        JSON.stringify({
                            message: 'Attendance marked offline',
                            offline: true,
                            markedAt: new Date().toISOString()
                        }),
                        {
                            status: 202,
                            headers: { 'Content-Type': 'application/json' }
                        }
                    );
                })
        );
    }
});

// Store offline in IndexedDB
async function storeOfflineAsync(data) {
    const db = await openDatabase(OFFLINE_QUEUE_DB);
    const tx = db.transaction('offlineQueue', 'readwrite');
    const store = tx.objectStore('offlineQueue');
    
    await store.add({
        deviceId: getDeviceId(),
        teacherId: data.teacherId,
        courseSectionId: data.courseSectionId,
        studentId: data.studentId,
        attendanceStatus: data.status,
        markedOfflineAt: new Date(),
        syncStatus: 'pending'
    });
}

// Listen for online event
self.addEventListener('online', async () => {
    const db = await openDatabase(OFFLINE_QUEUE_DB);
    const tx = db.transaction('offlineQueue', 'readonly');
    const store = tx.objectStore('offlineQueue');
    const items = await store.getAll();
    
    for (const item of items) {
        if (item.syncStatus === 'pending') {
            try {
                const response = await fetch('/api/attendance/sync-offline', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        ...item,
                        wasOfflineAt: item.markedOfflineAt
                    })
                });
                
                if (response.ok) {
                    // Mark as synced in IndexedDB
                    const tx = db.transaction('offlineQueue', 'readwrite');
                    tx.objectStore('offlineQueue').delete(item.id);
                }
            } catch (error) {
                console.error('Sync failed:', error);
                // Will retry next time
            }
        }
    }
});
```

### Attendance Marking API

```csharp
// Normal online marking
[HttpPost("api/attendance/mark")]
[Authorize(Roles = "Faculty")]
public async Task<IActionResult> MarkAttendance(
    [FromBody] MarkAttendanceRequest request,
    CancellationToken ct = default)
{
    var attendance = new StudentAttendance
    {
        CourseSectionId = request.CourseSectionId,
        StudentId = request.StudentId,
        AttendanceStatus = request.Status,
        MarkedAt = DateTime.UtcNow,
        MarkedBy = User.GetUserId(),
        IsOfflineSync = false // Marked online
    };
    
    _db.StudentAttendances.Add(attendance);
    await _db.SaveChangesAsync(ct);
    
    return Ok(new { message = "Attendance marked" });
}

// Sync offline queue
[HttpPost("api/attendance/sync-offline")]
[Authorize(Roles = "Faculty")]
public async Task<IActionResult> SyncOfflineAttendance(
    [FromBody] List<SyncOfflineRequest> requests,
    CancellationToken ct = default)
{
    foreach (var request in requests)
    {
        // Check for duplicates (prevent double-marking)
        var existing = await _db.StudentAttendances
            .FirstOrDefaultAsync(a => a.StudentId == request.StudentId
                && a.CourseSectionId == request.CourseSectionId
                && a.MarkedAt >= request.WasOfflineAt.AddMinutes(-1)
                && a.MarkedAt <= request.WasOfflineAt.AddMinutes(1), ct);
        
        if (existing != null)
        {
            // Already marked, skip (prevent duplicate)
            continue;
        }
        
        var attendance = new StudentAttendance
        {
            CourseSectionId = request.CourseSectionId,
            StudentId = request.StudentId,
            AttendanceStatus = request.AttendanceStatus,
            MarkedAt = request.WasOfflineAt, // Use offline timestamp
            MarkedBy = User.GetUserId(),
            IsOfflineSync = true, // Marked offline, synced later
            SyncedAt = DateTime.UtcNow // When it synced
        };
        
        _db.StudentAttendances.Add(attendance);
        
        // Log for audit
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "ATTENDANCE_SYNCED_OFFLINE",
            StudentId = request.StudentId,
            PerformedBy = User.GetUserId(),
            Details = $"Marked offline at {request.WasOfflineAt}, synced at {DateTime.UtcNow}"
        });
    }
    
    await _db.SaveChangesAsync(ct);
    
    return Ok(new
    {
        message = "Offline attendance synced",
        synced = requests.Count,
        duplicatesSkipped = 0 // In real implementation, track this
    });
}

[HttpGet("api/attendance/report")]
public async Task<IActionResult> GetAttendanceReport(
    Guid courseSectionId,
    Guid semesterId,
    CancellationToken ct = default)
{
    var attendances = await _db.StudentAttendances
        .Where(a => a.CourseSectionId == courseSectionId)
        .GroupBy(a => a.StudentId)
        .Select(g => new
        {
            studentId = g.Key,
            totalClasses = g.Count(),
            presentCount = g.Count(a => a.AttendanceStatus == "present"),
            absentCount = g.Count(a => a.AttendanceStatus == "absent"),
            lateCount = g.Count(a => a.AttendanceStatus == "late"),
            attendancePercentage = (decimal)g.Count(a => a.AttendanceStatus == "present") / g.Count() * 100,
            offlineSyncedCount = g.Count(a => a.IsOfflineSync),
            offlineIndicator = g.Any(a => a.IsOfflineSync) ? "⚠️ Contains offline-synced records" : ""
        })
        .ToListAsync(ct);
    
    return Ok(attendances);
}
```

### Mobile App Offline Support (React Native)

```javascript
// Offline attendance marking (React Native)
import AsyncStorage from '@react-native-async-storage/async-storage';
import NetInfo from '@react-native-community/netinfo';

export function AttendanceMarker({ courseSectionId }) {
    const [isOnline, setIsOnline] = useState(true);
    const [offlineQueue, setOfflineQueue] = useState([]);
    
    useEffect(() => {
        const unsubscribe = NetInfo.addEventListener(state => {
            setIsOnline(state.isConnected);
            
            if (state.isConnected && offlineQueue.length > 0) {
                syncOfflineAttendanceAsync();
            }
        });
        
        return () => unsubscribe();
    }, [offlineQueue]);
    
    const markAttendanceAsync = async (studentId, status) => {
        const timestamp = new Date();
        
        if (isOnline) {
            // Online: send immediately
            try {
                await fetch('/api/attendance/mark', {
                    method: 'POST',
                    body: JSON.stringify({
                        courseSectionId,
                        studentId,
                        status
                    })
                });
            } catch (error) {
                // Fall back to offline storage
                await storeOfflineAsync(studentId, status, timestamp);
            }
        } else {
            // Offline: store locally
            await storeOfflineAsync(studentId, status, timestamp);
        }
    };
    
    const storeOfflineAsync = async (studentId, status, timestamp) => {
        const queue = await AsyncStorage.getItem('offlineQueue');
        const items = queue ? JSON.parse(queue) : [];
        
        items.push({
            studentId,
            status,
            markedAt: timestamp.toISOString(),
            courseSectionId,
            synced: false
        });
        
        await AsyncStorage.setItem('offlineQueue', JSON.stringify(items));
        setOfflineQueue(items.filter(i => !i.synced));
        
        // Show toast: "Marked offline"
        Toast.show({
            text: `${studentId} marked ${status} (offline)`,
            duration: 2000
        });
    };
    
    const syncOfflineAttendanceAsync = async () => {
        const queue = await AsyncStorage.getItem('offlineQueue');
        const items = queue ? JSON.parse(queue) : [];
        
        const unsynced = items.filter(i => !i.synced);
        
        if (!unsynced.length) return;
        
        try {
            await fetch('/api/attendance/sync-offline', {
                method: 'POST',
                body: JSON.stringify(unsynced)
            });
            
            // Mark as synced
            const synced = items.map(i => ({
                ...i,
                synced: unsynced.some(u => u.studentId === i.studentId) ? true : i.synced
            }));
            
            await AsyncStorage.setItem('offlineQueue', JSON.stringify(synced));
            setOfflineQueue(synced.filter(i => !i.synced));
        } catch (error) {
            console.error('Sync failed:', error);
            // Will retry when online
        }
    };
    
    return (
        <View>
            <Text>{isOnline ? '🟢 Online' : '🔴 Offline'}</Text>
            {offlineQueue.length > 0 && (
                <Text>⚠️ {offlineQueue.length} records waiting to sync</Text>
            )}
            
            {/* Attendance marking UI */}
        </View>
    );
}
```

---

## Checklist

- [ ] Course tree (curriculum version) database schema
- [ ] Visual curriculum builder (React component with vis-network)
- [ ] Curriculum API (add/remove/update courses)
- [ ] CLO mapping to courses
- [ ] Curriculum approval workflow
- [ ] Change history tracking
- [ ] Teacher workload management (allocation + actual)
- [ ] Teacher assignment constraints (hours, sections)
- [ ] Workload check before assignment
- [ ] Offline attendance storage (IndexedDB)
- [ ] Service worker for offline sync
- [ ] Attendance sync API
- [ ] React Native offline attendance
- [ ] Duplicate detection (prevent double-marking)
- [ ] Audit logging (offline vs online)
- [ ] Attendance report with offline indicator
- [ ] Integration with S1 (foundation), S3 (examination), S8 (portals)

---

**This is curriculum management + workload planning + offline-first operations.**

University designs curriculum once with a beautiful graph editor. Nocturne enforces it. Teachers can mark attendance anywhere, anytime. System tracks everything.

This is what enterprise ERPs should do. 🚀
