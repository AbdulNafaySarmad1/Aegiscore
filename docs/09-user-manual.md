# Document 9 — User Manual
**Standard:** ISO/IEC/IEEE 26514

| Field | Value |
|-------|-------|
| Document ID | AEGIS-USER-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. Introduction

### 1.1 About This Manual

This User Manual provides comprehensive instructions for all user roles within AegisCore. Each section is tailored to a specific role, covering the core workflows, dashboard features, and common tasks that users will perform. This manual assumes that your institution has already been configured by a System Administrator and that you have received your login credentials.

### 1.2 Accessibility Notes

AegisCore is designed to meet WCAG 2.1 Level AA accessibility standards. Features include:
- Keyboard navigation for all functions
- Screen reader compatibility
- High contrast mode (toggle in user profile settings)
- Resizable text without loss of functionality
- Alternative text for all images and icons

### 1.3 Glossary of Terms

| Term | Definition |
|------|------------|
| ABAC | Attribute-Based Access Control — the security model that determines what you can access |
| CGPA | Cumulative Grade Point Average |
| Challan | Fee payment slip with unique identifier |
| CNIC | Computerised National Identity Card (Pakistan) |
| Dashboard | The main screen displayed after login, showing summary information |
| Enrolment | The record of a student registered in a specific course |
| GPA | Grade Point Average for a single semester |
| JWT | JSON Web Token — your digital access pass |
| Ledger | The complete record of all financial transactions |
| POS | Point of Sale — physical fee collection terminal |
| Tenant | Your institution's isolated data environment within AegisCore |

## 2. Getting Started (All Roles)

### 2.1 Logging In

1. Open your web browser and navigate to your institution's AegisCore URL (provided by your administrator).
2. Enter your institutional email address and password.
3. Click **Sign In**.
4. If Multi-Factor Authentication (MFA) is enabled for your account, enter the six-digit code from your authenticator application.
5. Upon first login, you will be prompted to change your temporary password.

### 2.2 First-Time Setup

1. After changing your password, complete your profile:
   - Verify your full name
   - Upload a profile photograph (optional)
   - Set your notification preferences (email, SMS, in-app)
2. Review and accept the terms of use.
3. You will be directed to your role-specific dashboard.

### 2.3 Profile Configuration

To update your profile at any time:
1. Click your name/avatar in the top-right corner.
2. Select **Profile Settings**.
3. Update your information as needed.
4. Click **Save Changes**.

### 2.4 Password Change

1. Navigate to **Profile Settings > Security**.
2. Enter your current password.
3. Enter and confirm your new password (minimum 12 characters, must include uppercase, lowercase, digit, and special character).
4. Click **Change Password**.
5. You will be logged out of all other sessions for security.

### 2.5 Logging Out

Click your name/avatar in the top-right corner and select **Sign Out**. Always sign out when using a shared or public computer.

## 3. SuperAdmin User Manual

### 3.1 Dashboard Overview

The SuperAdmin dashboard provides a system-wide view across all tenant institutions:

- **Active Institutions:** Count of currently active tenants
- **Total Users:** Aggregate user count across all tenants
- **System Health:** Real-time status of all infrastructure components
- **Recent Audit Events:** Latest security-relevant actions across the system
- **Licence Expiry Calendar:** Upcoming licence renewals

### 3.2 Institution Setup

1. Navigate to **Administration > Tenants**.
2. Click **Create New Tenant**.
3. Complete the institution details form (see Administrator Guide for detailed procedure).
4. The system provisions the tenant environment automatically.

### 3.3 User Provisioning

1. Navigate to **Administration > Users**.
2. Select the target institution from the tenant dropdown.
3. Click **Create User** or **Import CSV** for bulk provisioning.
4. Assign the appropriate role and department.

### 3.4 System Configuration

1. Navigate to **Administration > Configuration**.
2. Modify system-wide settings:
   - **Rate Limiting:** Adjust requests per minute per endpoint
   - **Session Timeout:** Set inactivity timeout duration
   - **Password Policy:** Configure complexity requirements
   - **MFA Policy:** Enable/disable MFA requirement per role
3. Click **Save Configuration**.

### 3.5 Audit Log Review

1. Navigate to **Administration > Audit Logs**.
2. Filter by:
   - **Tenant:** Specific institution or all
   - **User:** Specific user email
   - **Action:** Login, Create, Update, Delete, etc.
   - **Date Range:** Start and end dates
3. Review the filtered results.
4. Click **Export** to download audit data as CSV for external analysis.

## 4. ViceChancellor / Registrar User Manual

### 4.1 Dashboard Overview

The ViceChancellor/Registrar dashboard displays institutional-level metrics:

- **Enrolment Statistics:** Current semester enrolment by programme
- **Faculty Workload:** Teaching hours per department
- **Revenue Summary:** Fee collection vs. outstanding
- **Graduation Rate:** Percentage of students graduating on time
- **Attendance Overview:** Aggregate attendance by department
- **Recent Activities:** Latest actions across the institution

### 4.2 Admissions Management

1. Navigate to **Students > Admissions**.
2. **Create Admission Session:**
   - Click **New Admission Session**
   - Set programme, quotas, start date, end date
   - Configure merit calculation formula
3. **Review Applications:**
   - View submitted applications with documents
   - Approve or reject individual applications
   - Add internal notes
4. **Generate Merit List:**
   - Click **Generate Merit List** after session closure
   - Review and adjust rankings if necessary
   - Publish merit list (notifies applicants via email/SMS)

### 4.3 Student Records Management

1. Navigate to **Students > Records**.
2. Search for a student by name, student ID, or CNIC.
3. View complete student profile:
   - Personal information
   - Enrolment history
   - Grade history with GPA/CGPA
   - Attendance records
   - Fee account status
4. **Update Records:**
   - Click **Edit** to modify student information
   - All changes are logged in the audit trail

### 4.4 Timetable Management

1. Navigate to **Academics > Timetable**.
2. Select department, semester, and academic year.
3. **Create Timetable Entry:**
   - Click **Add Entry**
   - Select course, faculty, day, time, room
   - System checks for conflicts (room double-booking, faculty overlap)
4. **View Timetable:**
   - By department, faculty, or room
   - Export as PDF for printing

### 4.5 Exam Scheduling

1. Navigate to **Academics > Examinations**.
2. **Create Exam Schedule:**
   - Click **New Exam Schedule**
   - Select courses, exam type (Midterm/Final), date range
   - System suggests time slots avoiding conflicts
3. **Assign Invigilators:**
   - Drag and drop faculty to exam slots
   - System validates availability
4. **Publish Schedule:**
   - Click **Publish** to notify students and faculty

### 4.6 Graduation Processing

1. Navigate to **Students > Graduation**.
2. **Identify Eligible Students:**
   - System automatically flags students meeting graduation requirements
   - Review list and manually add/remove if needed
3. **Process Graduation:**
   - Click **Process Graduation** for selected students
   - System updates student status to "Graduated"
   - Generates digital transcript
4. **Print Transcripts:**
   - Select students and click **Print Transcripts**
   - Transcripts include digital signature and verification QR code

## 5. Faculty User Manual

### 5.1 Dashboard Overview

The Faculty dashboard displays teaching-related information:

- **My Courses:** Current semester courses with enrolment counts
- **Today's Schedule:** Classes scheduled for the current day
- **Pending Tasks:** Attendance to mark, grades to enter
- **Student Alerts:** At-risk students in your courses
- **Announcements:** Department and institution-wide notices

### 5.2 Course Management

1. Navigate to **Courses > My Courses**.
2. Select a course to view:
   - Student roster with photographs
   - Attendance history
   - Grade distribution
   - Course materials (syllabus, lecture notes)
3. **Upload Course Materials:**
   - Click **Upload Materials**
   - Select file (PDF, DOCX, PPTX)
   - Add description and visibility settings

### 5.3 Attendance Marking

1. Navigate to **Attendance > Mark Attendance**.
2. Select course and date.
3. The student roster appears with attendance options:
   - **Present:** Default status
   - **Absent:** Student not present
   - **Late:** Student arrived after class start
   - **Excused:** Pre-approved absence
4. **Bulk Marking:**
   - Click **Mark All Present** to set all students as present
   - Then individually adjust absent/late students
5. Click **Submit Attendance**.
6. Attendance is recorded with timestamp and your identity.

### 5.4 Grade Entry

1. Navigate to **Grades > Enter Grades**.
2. Select course and exam type (Midterm, Final, Quiz, Assignment).
3. Enter marks for each student:
   - Type marks obtained
   - Total marks is pre-filled from exam configuration
   - Grade is auto-calculated based on grading scale
4. **Import from CSV:**
   - Download grade template
   - Fill in marks
   - Upload CSV
5. Click **Submit Grades**.
6. Grades are locked after submission; contact Registrar to unlock if changes needed.

### 5.5 Student Performance Analytics

1. Navigate to **Analytics > Student Performance**.
2. Select a course to view:
   - Grade distribution histogram
   - Attendance vs. performance correlation
   - Individual student trend lines
3. **Identify At-Risk Students:**
   - System highlights students with attendance < 75% or grades below threshold
   - Click on a student to see detailed risk factors

### 5.6 Departmental Workflows

1. Navigate to **Department > Overview**.
2. View departmental metrics:
   - Faculty workload distribution
   - Course enrolment trends
   - Research output summary
3. **Manage Department Courses:**
   - Add new courses to department catalogue
   - Assign faculty to courses
   - Set course prerequisites

## 6. Student User Manual

### 6.1 Dashboard Overview

The Student dashboard displays personal academic information:

- **Current Courses:** Enrolled courses this semester
- **Today's Timetable:** Classes scheduled for today
- **Attendance Summary:** Percentage attendance per course
- **Fee Status:** Outstanding balance and due dates
- **Notifications:** Recent announcements and alerts
- **CGPA:** Cumulative GPA with trend indicator

### 6.2 Viewing Records

1. Navigate to **My Records > Academic History**.
2. View:
   - Complete enrolment history by semester
   - Grades for each course
   - Semester GPA and cumulative CGPA
   - Transcript (printable with verification QR code)

### 6.3 Viewing Timetable

1. Navigate to **My Timetable**.
2. View weekly schedule with:
   - Course name and code
   - Time and room
   - Faculty name
3. **Export:** Download as PDF or subscribe to calendar feed (iCal).

### 6.4 Viewing Results

1. Navigate to **My Results**.
2. Select semester to view:
   - Course-wise marks and grades
   - GPA for the semester
   - Comparative class statistics (your rank, class average)
3. **Result Notification:** You will receive an in-app and email notification when new results are published.

### 6.5 Fee Status

1. Navigate to **My Fees**.
2. View:
   - Total fees due for current semester
   - Payment history with dates and amounts
   - Outstanding balance
   - Next payment due date
3. **Pay Online:**
   - Click **Pay Now**
   - Select payment method (bank transfer, credit/debit card)
   - Complete payment on banking gateway
   - Payment is reflected in your account within minutes
4. **Download Challan:**
   - Click **Download Challan** for bank counter payment
   - Print and present at designated bank

### 6.6 Notifications

1. Click the bell icon in the top navigation bar.
2. View recent notifications:
   - Grade published
   - Fee due reminder
   - Class cancellation
   - Examination schedule
   - Institution announcements
3. Click a notification to view full details.

## 7. Finance Officer User Manual

### 7.1 Dashboard Overview

The Finance Officer dashboard displays financial metrics:

- **Revenue Summary:** Total collected, outstanding, projected
- **Recent Transactions:** Latest fee payments and refunds
- **POS Activity:** Today's counter collections
- **Reconciliation Status:** Bank reconciliation completion
- **Alerts:** Large transactions, reconciliation discrepancies

### 7.2 Fee Processing

1. Navigate to **Finance > Fee Collection**.
2. **Search Student:**
   - Enter student ID or name
   - View fee account details
3. **Record Payment:**
   - Click **Record Payment**
   - Enter amount, payment method (Online, POS, Cash, Bank Transfer)
   - Enter reference number (cheque number, transaction ID)
   - Click **Submit**
4. **Print Receipt:**
   - After recording payment, click **Print Receipt**
   - Receipt includes transaction details and verification QR code

### 7.3 POS Operations

1. Navigate to **Finance > POS Terminal**.
2. **Process Counter Payment:**
   - Enter student ID
   - System displays outstanding fees
   - Enter amount tendered
   - System calculates change if cash payment
   - Click **Process Payment**
3. **End of Day Reconciliation:**
   - Click **Close Shift**
   - System displays shift summary: total collected, payment method breakdown
   - Verify against physical cash/terminal receipts
   - Click **Confirm Close**

### 7.4 Ledger Management

1. Navigate to **Finance > Ledger**.
2. View complete transaction history:
   - Filter by date range, transaction type, student
   - View running balance
3. **Manual Entry:**
   - Click **Add Entry** for adjustments, corrections
   - Select transaction type (Adjustment, Waiver, Penalty)
   - Enter amount and description
   - Requires supervisor approval for amounts above threshold

### 7.5 Reconciliation

1. Navigate to **Finance > Reconciliation**.
2. **Upload Bank Statement:**
   - Click **Upload Statement**
   - Select bank and upload CSV/PDF statement
3. **Auto-Reconciliation:**
   - System matches transactions by reference number and amount
   - Displays matched, unmatched, and potential matches
4. **Manual Matching:**
   - For unmatched transactions, manually link to system records
5. **Reconciliation Report:**
   - Click **Generate Report** for audit-ready reconciliation summary

### 7.6 Financial Reports

1. Navigate to **Finance > Reports**.
2. Select report type:
   - **Fee Collection Summary:** By programme, semester, payment method
   - **Outstanding Dues:** Students with overdue payments
   - **Revenue Projection:** Based on enrolment and historical collection rates
   - **Audit Report:** Complete transaction log for external audit
3. Set date range and filters.
4. Click **Generate Report**.
5. Export as PDF or Excel.

## 8. Common Error Messages

| Error Message | Meaning | What to Do |
|---------------|---------|------------|
| "Invalid credentials" | Email or password incorrect | Verify email and password; use "Forgot Password" if needed |
| "Account locked" | Too many failed login attempts | Wait 15 minutes or contact administrator |
| "Session expired" | Your login session timed out | Log in again |
| "Access denied" | You do not have permission for this action | Contact your administrator if you believe this is an error |
| "Validation error" | Required fields missing or invalid format | Check all fields and correct as indicated |
| "Duplicate entry" | Record already exists | Check if the record was already created |
| "Rate limit exceeded" | Too many requests in short time | Wait and try again |
| "Service unavailable" | System is undergoing maintenance | Try again later; check institution announcements |
| "Payment failed" | Banking gateway rejected payment | Verify payment details; try again or use alternative method |
| "File too large" | Uploaded file exceeds size limit | Reduce file size or contact administrator |
