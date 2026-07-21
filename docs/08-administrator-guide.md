# Document 8 — Administrator Guide
**Standard:** ISO/IEC/IEEE 26511

| Field | Value |
|-------|-------|
| Document ID | AEGIS-ADM-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. System Administration Overview

AegisCore system administration encompasses the management of tenants, users, security policies, database operations, monitoring, and licensing. This guide is intended for System Administrators and SuperAdmins responsible for the operational health of the AegisCore platform.

### 1.1 Administrator Roles

| Role | Responsibilities |
|------|-----------------|
| SuperAdmin | Full system access across all tenants; tenant provisioning; system configuration; audit review |
| System Administrator | Infrastructure management; deployment; monitoring; backup and recovery |
| Tenant Administrator | Institution-level user management; role assignment; institution configuration |

## 2. Institution Onboarding Procedure

### 2.1 Creating a New Tenant

1. Log in to the AegisCore administration dashboard as a SuperAdmin.
2. Navigate to **Administration > Tenants > Create New Tenant**.
3. Complete the tenant creation form:
   - **Institution Name:** Full legal name of the university
   - **Tenant ID:** Auto-generated UUID (or custom subdomain)
   - **Modules Enabled:** Select which modules to activate (Student Lifecycle, Finance, Faculty, Analytics)
   - **Student Headcount:** Total enrolled students (determines licence tier)
   - **Admin Email:** Email address for the initial tenant administrator
   - **Admin Password:** Temporary password (must be changed on first login)
4. Click **Create Tenant**.
5. The system will:
   - Create the tenant record in the database
   - Provision the initial SuperAdmin user for the tenant
   - Apply default Casbin policies for the tenant
   - Generate the tenant-specific configuration
6. Provide the tenant administrator with their login credentials via secure channel.

### 2.2 Post-Provisioning Tasks

1. **Verify tenant isolation:** Log in as the new tenant administrator and confirm that only the new tenant's data is visible.
2. **Configure institution settings:** Navigate to **Settings > Institution** and set:
   - University logo
   - Academic calendar (semester start/end dates)
   - Grading scale configuration
   - Fee structure defaults
3. **Import initial data:** Use the bulk import tools to load:
   - Department structure
   - Course catalogue
   - Faculty roster
   - Student records (if migrating from existing system)

## 3. User Management

### 3.1 Creating Users

1. Navigate to **Administration > Users > Create User**.
2. Complete the user creation form:
   - **Email:** User's institutional email address
   - **Full Name:** User's complete name
   - **Role:** Select from SuperAdmin, ViceChancellor, Registrar, DepartmentHead, Faculty, Student, FinanceOfficer
   - **Department:** (if applicable)
   - **Temporary Password:** Auto-generated or manually set
3. Click **Create User**.
4. The user will receive an email with login instructions and must change their password on first login.

### 3.2 Assigning Roles

Roles can be assigned at user creation or modified later:

1. Navigate to **Administration > Users**.
2. Search for the user by email or name.
3. Click **Edit** on the user record.
4. Change the **Role** dropdown.
5. Click **Save**.
6. The user's permissions will update immediately upon their next API request (Casbin policies are evaluated per request).

### 3.3 Bulk Import via CSV

For large-scale user provisioning:

1. Navigate to **Administration > Users > Import CSV**.
2. Download the CSV template.
3. Populate the CSV with user data:
   ```
   email,full_name,role,department,student_id,employee_id
   ahmed.khan@university.edu.pk,Ahmed Khan,Student,Computer Science,CS-2025-001,
   fatima.ali@university.edu.pk,Fatima Ali,Faculty,Computer Science,,FAC-2019-042
   ```
4. Upload the CSV file.
5. Review the validation results:
   - **Green rows:** Successfully imported
   - **Yellow rows:** Warnings (e.g., duplicate email — skipped)
   - **Red rows:** Errors (e.g., invalid role — not imported)
6. Download the import report for audit purposes.

### 3.4 Deactivating Users

1. Navigate to **Administration > Users**.
2. Search for the user.
3. Click **Deactivate**.
4. Confirm the deactivation.
5. The user will be unable to log in; their data remains in the system for audit purposes.
6. To reactivate, click **Activate** on the same user record.

## 4. Casbin Policy Management

### 4.1 Understanding Casbin Policies

AegisCore uses Casbin ABAC (Attribute-Based Access Control) for authorisation. Policies define who can do what to which resources under what conditions.

A policy consists of:
- **Subject (who):** User role or specific user
- **Object (what):** Resource or endpoint pattern
- **Action (can do):** Read, Write, Delete, etc.
- **Effect:** Allow or Deny
- **Conditions:** Additional attribute constraints (department, time, IP)

### 4.2 Viewing Current Policies

1. Navigate to **Administration > Security > Policies**.
2. The policy list displays all active policies with:
   - Policy ID
   - Subject pattern
   - Object pattern
   - Action pattern
   - Effect (Allow/Deny)
   - Conditions (if any)
   - Last modified date

### 4.3 Adding a New Policy

1. Navigate to **Administration > Security > Policies > Add Policy**.
2. Complete the policy form:
   - **Subject:** Select role or enter specific user email
   - **Object:** Enter resource pattern (e.g., `/api/students/*` for all student endpoints, `/api/students/{id}` for specific student)
   - **Action:** Select Read, Write, Delete, or enter custom action
   - **Effect:** Allow or Deny
   - **Conditions (optional):** Enter JSON conditions, e.g.:
     ```json
     {"department": "Computer Science", "time_range": "08:00-18:00"}
     ```
3. Click **Add Policy**.
4. The policy takes effect immediately.

### 4.4 Modifying an Existing Policy

1. Navigate to **Administration > Security > Policies**.
2. Click **Edit** on the policy to modify.
3. Update the fields as needed.
4. Click **Save**.
5. The updated policy takes effect immediately.

### 4.5 Removing a Policy

1. Navigate to **Administration > Security > Policies**.
2. Click **Delete** on the policy to remove.
3. Confirm the deletion.
4. The policy is removed immediately.

**Warning:** Removing policies can inadvertently grant or revoke access. Always test policy changes in the staging environment before applying to production.

### 4.6 Policy Testing

1. Navigate to **Administration > Security > Policy Tester**.
2. Enter test parameters:
   - **User:** Select a test user
   - **Resource:** Enter the resource path
   - **Action:** Select the action
3. Click **Test**.
4. The system evaluates all applicable policies and displays:
   - **Result:** Allow or Deny
   - **Matching Policies:** List of policies that matched
   - **Reasoning:** Explanation of the decision

## 5. Database Administration

### 5.1 Backup Procedures

**Automated Backups:** The system performs automated backups according to the configured schedule:
- Full backup: Daily at 02:00 UTC
- WAL archiving: Continuous
- Transaction log backup: Every 15 minutes

**Manual Backup:**
