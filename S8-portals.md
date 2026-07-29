# Nocturne — Sprint 8: Student & Faculty Self-Service Portals

## Role
You are a senior .NET 10 / C# 14 engineer building the student and faculty self-service portals for Nocturne. These are critical touchpoints where end users interact daily with the system.

---

## Task 1 — Student Portal

### Key Pages

#### 1. Dashboard
- Current semester overview
- At-a-glance metrics:
  - GPA (current semester + cumulative)
  - Attendance percentage (with shortage alert if < 75%)
  - Fee status (paid / pending / overdue)
  - Current courses (with grades if available)

#### 2. Attendance Tracking
- Semester-wise attendance breakdown
- Course-wise attendance %
- Shortage alerts (red if below 75% policy threshold)
- Ability to request attendance appeals (if allowed)

#### 3. Fee Management
- Semester-wise fee statement
- Installment-wise payment status
- Outstanding amount calculation
- Online payment button (JazzCash / EasyPaisa)
- Payment history with receipt download
- Scholarship deductions (if any)

#### 4. Academic Records
- Results view (course-wise grades + GPA)
- Transcript request (character / bonafide / academic)
  - Dropdown: certificate type
  - Request reason (text field)
  - Submit → routed to Registrar for approval
  - Admin approves → PDF generated + emailed to student
- Semester-wise grade distribution (chart)
- Course rankings (rank in section)

#### 5. Timetable
- Current semester schedule
- Course-wise timings + venues
- Faculty names
- Calendar view + list view
- iCal export

#### 6. Course Registration / Add-Drop
- Available courses for current semester (filtered by program)
- Add course (drag-and-drop or checkbox + add button)
- Drop course (if within add-drop window per academic calendar)
- Pre-requisite validation (auto-check: student has completed prereq courses)
- Course clash detection (auto-flag if selected courses overlap)
- Submit registration → confirmation screen

#### 7. Hall Ticket Download
- Available only during exam period
- Display: exam date, time, hall, seat number
- QR code for entry verification
- Print-friendly layout
- Download as PDF

#### 8. Complaints / Grievances
- Submit grievance (category dropdown: academic / financial / conduct / other)
- Description (text area)
- Auto-generate ticket ID
- Track status (submitted → acknowledged → resolution → closed)
- View all past grievances

#### 9. Announcements
- University-wide announcements
- Program-specific announcements
- Department announcements
- Filterable by date range
- Mark as read

#### 10. Notifications
- In-app notification center
- Types: fee reminders, attendance alerts, result published, grade posted, hall ticket ready
- Mark as read / delete
- Notification preferences (enable/disable by type)

### Student Portal Authorization
- Student can only see their own data (RBAC enforced)
- Academic calendar determines which operations are available (e.g., add-drop only during window)

---

## Task 2 — Faculty Portal

#### 1. Dashboard
- Current semester courses
- Student count per course
- Attendance entry status (% completed)
- Marks entry status (% submitted)
- Pending grievances
- At-risk students (attendance < 75% OR grades trending low)

#### 2. Attendance Marking
- Course selection → list of enrolled students
- Checkbox grid: present / absent / leave per date
- Date range picker (select dates to mark attendance)
- Bulk mark (mark all present / all absent for selected dates)
- Save and submit → locked after submission
- Reopen request option (if admin allows)
- Export attendance as CSV

#### 3. Marks Entry
- Course selection → assessment component selection
- Student-wise marks entry (table view)
- Component-wise marks (e.g., quiz 1, quiz 2, midterm, final)
- Auto-calculate total marks per student
- Bulk upload: CSV with student_roll_number | comp1 | comp2 | ...
- Save as draft → submit when all marks entered
- Mark submission locks data
- Reopen request option

#### 4. Course Management
- CIS (Course Information Sheet) upload
  - File upload (PDF/image)
  - Auto-extraction of CLOs, teaching methods, assessment
  - Human review screen (edit extracted data if needed)
  - Confirm → populates course record + OBE module
- Syllabus upload (PDF)
- Course materials upload (lecture notes, slides, etc.)
- Materials auto-sync to Moodle

#### 5. Course Materials Library
- Organized folders (by week / by topic)
- Upload documents, videos, links
- Access control: students can download
- Analytics: track which materials are downloaded most

#### 6. Student At-Risk Alerts
- Auto-flagged students:
  - Attendance < 75%
  - Average grade < 2.0
  - Multiple failed assessments
- Actionable list with contact info
- Option to send message to student

#### 7. Messages / Communication
- Send message to individual student or all class
- In-app notification + email notification
- Message history per student

#### 8. Workload View
- Credit hours taught (current + cumulative)
- Number of students per course
- Administrative load (committee assignments, etc.)
- Research load (guided projects, theses)
- Total workload score (for appraisal purposes)

#### 9. Research / Publication Tracking
- List research projects (ongoing + completed)
- Publication records (co-authored, authored)
- Grant tracking
- Patent records
- Contribution to university research output

#### 10. Performance Appraisal
- View current appraisal cycle status
- Self-assessment form (if 360-degree feedback)
- View past appraisals (read-only)

#### 11. Leave Management
- Apply for leave (date range, reason, leave type)
- View leave balance
- See leave approval history

### Faculty Portal Authorization
- Faculty can only manage courses they teach (RBAC enforced)
- HOD can view faculty courses in their department

---

## Task 3 — Portal Navigation & UI

### Both Portals
- Top navigation bar:
  - Logo + university name
  - User profile menu (name, role, logout)
  - Notification bell (red dot if unread)
  - Settings (language, theme, preferences)
- Sidebar navigation (collapsible):
  - Icons + labels
  - Active page highlighted
  - Mobile: hamburger menu
- Responsive design (mobile-first)
- Dark mode toggle (user preference)

### Theme Customization
- Apply university primary/secondary colours (from onboarding wizard)
- Apply university logo
- Custom font (optional)
- Custom background (optional)

---

## Task 4 — Portal Integration Points

### Notifications
- All critical events trigger notifications:
  - Fee payment due
  - Attendance running low
  - Result published
  - Grievance status change
  - Leave approved/rejected
  - New announcement
  - Message from faculty/admin

### Real-time Updates
- Use SignalR for real-time notifications
- Server broadcasts to all connected clients:
  - Result published
  - Hall tickets ready
  - New announcements

### Accessibility
- WCAG 2.1 AA compliance
- Keyboard navigation
- Screen reader support
- High contrast mode

---

## Deliverables Checklist
- [ ] Student dashboard + 10 main pages
- [ ] Faculty dashboard + 11 main pages
- [ ] Role-based access control per page
- [ ] Responsive design (mobile + tablet + desktop)
- [ ] Dark mode toggle
- [ ] Real-time notifications (SignalR)
- [ ] PDF export (transcripts, attendance, marks)
- [ ] File upload/download with virus scanning
- [ ] Pagination for large lists
- [ ] Search + filter capabilities
- [ ] User preferences (theme, language, notifications)
- [ ] WCAG 2.1 AA compliance
- [ ] Performance optimization (lazy loading, caching)
- [ ] Unit tests + integration tests
