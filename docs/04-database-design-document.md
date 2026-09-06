# Document 4 — Database Design Document
**Standards:** ERD (Structured Notation) + Data Dictionary

| Field | Value |
|-------|-------|
| Document ID | AEGIS-DBDD-001 |
| Version | 1.1.0 |
| Date | 2026-09-06 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |
| 1.1.0 | 2026-09-06 | AegisCore Engineering | Added PostgreSQL, MySQL, SQL Server, and Oracle provider/migration strategy |

## 1. Introduction

### 1.1 Purpose

This Database Design Document defines the complete logical and physical data model for AegisCore. It includes the Entity Relationship Diagram described in structured notation, a comprehensive data dictionary for all entities, the indexing strategy, multi-tenancy isolation design, and data retention policies.

### 1.2 Scope

This document covers all database objects required for AegisCore, including tables, indexes, constraints, portable tenant filters, provider-specific migrations, PostgreSQL defense-in-depth policies, and optional PostgreSQL pgvector storage.

### 1.3 References

1. AegisCore Software Requirements Specification (AEGIS-SRS-001)
2. AegisCore Software Architecture Document (AEGIS-SAD-001)
3. EF Core providers for PostgreSQL, MySQL, SQL Server, and Oracle
4. P11 Multi-Dialect Database Support — `docs/deployment/P11_MULTI_DIALECT.md`

## 2. Entity Relationship Diagram (Structured Notation)

### 2.1 Core Entities

**Institution**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), name (VARCHAR 255, NOT NULL), motto (VARCHAR 500), logo_url (VARCHAR 1000), headcount (INTEGER), city (VARCHAR 100), state (VARCHAR 100), country (VARCHAR 100), postal_code (VARCHAR 20), latitude (DECIMAL 10,7), longitude (DECIMAL 10,7), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: tenant_id

**User**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL, FK → Institution.tenant_id), email (VARCHAR 255, NOT NULL), password_hash (VARCHAR 255, NOT NULL), full_name (VARCHAR 255, NOT NULL), role (VARCHAR 50, NOT NULL), department (VARCHAR 100), is_active (BOOLEAN, NOT NULL, DEFAULT TRUE), last_login_at (TIMESTAMPTZ), mfa_enabled (BOOLEAN, NOT NULL, DEFAULT FALSE), mfa_secret (VARCHAR 255), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (tenant_id, email)
- Foreign Key: tenant_id → Institution.tenant_id

**Student**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), user_id (UUID, FK → User.id), student_id (VARCHAR 50, NOT NULL), programme (VARCHAR 100, NOT NULL), enrolment_date (DATE, NOT NULL), expected_graduation_date (DATE), current_semester (INTEGER), cgpa (DECIMAL 4,2), status (VARCHAR 20, NOT NULL), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (tenant_id, student_id)
- Foreign Key: user_id → User.id, tenant_id → Institution.tenant_id

**Faculty**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), user_id (UUID, FK → User.id), employee_id (VARCHAR 50, NOT NULL), designation (VARCHAR 100), department (VARCHAR 100, NOT NULL), joining_date (DATE), specialisation (VARCHAR 255), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (tenant_id, employee_id)
- Foreign Key: user_id → User.id, tenant_id → Institution.tenant_id

**Department**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), name (VARCHAR 255, NOT NULL), code (VARCHAR 20, NOT NULL), head_id (UUID, FK → Faculty.id), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (tenant_id, code)
- Foreign Key: head_id → Faculty.id, tenant_id → Institution.tenant_id

**Course**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), code (VARCHAR 20, NOT NULL), title (VARCHAR 255, NOT NULL), credit_hours (INTEGER, NOT NULL), department_id (UUID, FK → Department.id), description (TEXT), is_active (BOOLEAN, NOT NULL, DEFAULT TRUE), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (tenant_id, code)
- Foreign Key: department_id → Department.id, tenant_id → Institution.tenant_id

**Enrolment**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), student_id (UUID, FK → Student.id), course_id (UUID, FK → Course.id), semester (VARCHAR 20, NOT NULL), academic_year (VARCHAR 10, NOT NULL), enrolment_date (DATE, NOT NULL), status (VARCHAR 20, NOT NULL), grade (VARCHAR 5), grade_points (DECIMAL 3,1), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (student_id, course_id, semester, academic_year)
- Foreign Key: student_id → Student.id, course_id → Course.id, tenant_id → Institution.tenant_id

**Attendance**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), enrolment_id (UUID, FK → Enrolment.id), lecture_date (DATE, NOT NULL), status (VARCHAR 20, NOT NULL), marked_by (UUID, FK → User.id), marked_at (TIMESTAMPTZ, NOT NULL), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (enrolment_id, lecture_date)
- Foreign Key: enrolment_id → Enrolment.id, marked_by → User.id, tenant_id → Institution.tenant_id

**ExamSchedule**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), course_id (UUID, FK → Course.id), exam_date (DATE, NOT NULL), start_time (TIME, NOT NULL), end_time (TIME, NOT NULL), room (VARCHAR 50), exam_type (VARCHAR 20, NOT NULL), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Foreign Key: course_id → Course.id, tenant_id → Institution.tenant_id

**ExamResult**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), enrolment_id (UUID, FK → Enrolment.id), exam_schedule_id (UUID, FK → ExamSchedule.id), marks_obtained (DECIMAL 5,2), total_marks (DECIMAL 5,2), grade (VARCHAR 5), entered_by (UUID, FK → User.id), entered_at (TIMESTAMPTZ, NOT NULL), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (enrolment_id, exam_schedule_id)
- Foreign Key: enrolment_id → Enrolment.id, exam_schedule_id → ExamSchedule.id, entered_by → User.id, tenant_id → Institution.tenant_id

**FeeAccount**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), student_id (UUID, FK → Student.id), total_due (DECIMAL 12,2), total_paid (DECIMAL 12,2), outstanding (DECIMAL 12,2), last_payment_date (DATE), status (VARCHAR 20, NOT NULL), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Unique Constraint: (student_id)
- Foreign Key: student_id → Student.id, tenant_id → Institution.tenant_id

**FeeTransaction**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), fee_account_id (UUID, FK → FeeAccount.id), amount (DECIMAL 12,2, NOT NULL), transaction_type (VARCHAR 20, NOT NULL), payment_method (VARCHAR 20), reference_number (VARCHAR 100), transaction_date (TIMESTAMPTZ, NOT NULL), processed_by (UUID, FK → User.id), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Foreign Key: fee_account_id → FeeAccount.id, processed_by → User.id, tenant_id → Institution.tenant_id

**Timetable**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), course_id (UUID, FK → Course.id), faculty_id (UUID, FK → Faculty.id), day_of_week (INTEGER, NOT NULL), start_time (TIME, NOT NULL), end_time (TIME, NOT NULL), room (VARCHAR 50), semester (VARCHAR 20, NOT NULL), academic_year (VARCHAR 10, NOT NULL), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL), is_deleted (BOOLEAN, NOT NULL, DEFAULT FALSE)
- Primary Key: id
- Foreign Key: course_id → Course.id, faculty_id → Faculty.id, tenant_id → Institution.tenant_id

**AuditLog**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), user_id (UUID), action (VARCHAR 50, NOT NULL), entity_type (VARCHAR 50, NOT NULL), entity_id (UUID), old_values (JSONB), new_values (JSONB), ip_address (INET), user_agent (VARCHAR 500), created_at (TIMESTAMPTZ, NOT NULL)
- Primary Key: id
- Note: AuditLog is append-only; no UPDATE or DELETE operations permitted.

**MLFeatureVector**
- Attributes: id (UUID, PK), tenant_id (UUID, NOT NULL), entity_type (VARCHAR 50, NOT NULL), entity_id (UUID, NOT NULL), vector (vector(1536)), metadata (JSONB), model_version (VARCHAR 20), created_at (TIMESTAMPTZ, NOT NULL), updated_at (TIMESTAMPTZ, NOT NULL)
- Primary Key: id
- Index: HNSW index on vector column for similarity search

### 2.2 Relationships and Cardinality

| Entity A | Entity B | Relationship | Cardinality |
|----------|----------|--------------|-------------|
| Institution | User | has | 1:N |
| Institution | Student | has | 1:N |
| Institution | Faculty | has | 1:N |
| Institution | Department | has | 1:N |
| Institution | Course | has | 1:N |
| User | Student | is a | 1:1 |
| User | Faculty | is a | 1:1 |
| Department | Faculty (head) | headed by | 1:1 |
| Department | Course | offers | 1:N |
| Student | Enrolment | enrols in | 1:N |
| Course | Enrolment | has | 1:N |
| Enrolment | Attendance | records | 1:N |
| Course | ExamSchedule | schedules | 1:N |
| Enrolment | ExamResult | receives | 1:N |
| ExamSchedule | ExamResult | produces | 1:N |
| Student | FeeAccount | has | 1:1 |
| FeeAccount | FeeTransaction | records | 1:N |
| Course | Timetable | scheduled in | 1:N |
| Faculty | Timetable | assigned to | 1:N |

## 3. Data Dictionary

### 3.1 Institution

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, UNIQUE | - | Tenant identifier for multi-tenant isolation | N |
| name | VARCHAR(255) | NOT NULL | - | Full name of the institution | N |
| motto | VARCHAR(500) | - | NULL | Institution motto or tagline | N |
| logo_url | VARCHAR(1000) | - | NULL | URL to institution logo image | N |
| headcount | INTEGER | - | NULL | Total students + faculty + staff | N |
| city | VARCHAR(100) | - | NULL | City of main campus | N |
| state | VARCHAR(100) | - | NULL | Province or state | N |
| country | VARCHAR(100) | - | NULL | Country | N |
| postal_code | VARCHAR(20) | - | NULL | Postal code | N |
| latitude | DECIMAL(10,7) | - | NULL | Geographic latitude | N |
| longitude | DECIMAL(10,7) | - | NULL | Geographic longitude | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.2 User

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| email | VARCHAR(255) | NOT NULL | - | User email address (used for login) | Y |
| password_hash | VARCHAR(255) | NOT NULL | - | Argon2id password hash | Y |
| full_name | VARCHAR(255) | NOT NULL | - | User's full name | Y |
| role | VARCHAR(50) | NOT NULL | - | User role (SuperAdmin, ViceChancellor, Registrar, Faculty, Student, FinanceOfficer) | N |
| department | VARCHAR(100) | - | NULL | Department affiliation | N |
| is_active | BOOLEAN | NOT NULL | TRUE | Whether the user account is active | N |
| last_login_at | TIMESTAMPTZ | - | NULL | Timestamp of last successful login | N |
| mfa_enabled | BOOLEAN | NOT NULL | FALSE | Whether MFA is enabled | N |
| mfa_secret | VARCHAR(255) | - | NULL | TOTP secret for MFA | Y |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.3 Student

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| user_id | UUID | FK → User.id | - | Associated user account | N |
| student_id | VARCHAR(50) | NOT NULL | - | Institution-assigned student ID | Y |
| programme | VARCHAR(100) | NOT NULL | - | Degree programme name | N |
| enrolment_date | DATE | NOT NULL | - | Date of first enrolment | N |
| expected_graduation_date | DATE | - | NULL | Expected graduation date | N |
| current_semester | INTEGER | - | NULL | Current semester number | N |
| cgpa | DECIMAL(4,2) | - | NULL | Cumulative Grade Point Average | N |
| status | VARCHAR(20) | NOT NULL | 'Active' | Enrolment status (Active, Graduated, Suspended, Withdrawn) | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.4 Faculty

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| user_id | UUID | FK → User.id | - | Associated user account | N |
| employee_id | VARCHAR(50) | NOT NULL | - | Institution-assigned employee ID | Y |
| designation | VARCHAR(100) | - | NULL | Job title (Professor, Associate Professor, etc.) | N |
| department | VARCHAR(100) | NOT NULL | - | Primary department | N |
| joining_date | DATE | - | NULL | Date of joining the institution | N |
| specialisation | VARCHAR(255) | - | NULL | Area of academic specialisation | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.5 Course

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| code | VARCHAR(20) | NOT NULL | - | Course code (e.g., CS-301) | N |
| title | VARCHAR(255) | NOT NULL | - | Course title | N |
| credit_hours | INTEGER | NOT NULL | - | Number of credit hours | N |
| department_id | UUID | FK → Department.id | - | Offering department | N |
| description | TEXT | - | NULL | Course description | N |
| is_active | BOOLEAN | NOT NULL | TRUE | Whether the course is currently offered | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.6 Enrolment

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| student_id | UUID | FK → Student.id | - | Enrolled student | N |
| course_id | UUID | FK → Course.id | - | Enrolled course | N |
| semester | VARCHAR(20) | NOT NULL | - | Semester identifier (e.g., Fall-2025) | N |
| academic_year | VARCHAR(10) | NOT NULL | - | Academic year (e.g., 2025-26) | N |
| enrolment_date | DATE | NOT NULL | - | Date of enrolment | N |
| status | VARCHAR(20) | NOT NULL | 'Enrolled' | Enrolment status | N |
| grade | VARCHAR(5) | - | NULL | Final grade awarded | N |
| grade_points | DECIMAL(3,1) | - | NULL | Grade points for GPA calculation | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.7 Attendance

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| enrolment_id | UUID | FK → Enrolment.id | - | Enrolment record | N |
| lecture_date | DATE | NOT NULL | - | Date of the lecture | N |
| status | VARCHAR(20) | NOT NULL | - | Attendance status (Present, Absent, Late, Excused) | N |
| marked_by | UUID | FK → User.id | - | Faculty who marked attendance | N |
| marked_at | TIMESTAMPTZ | NOT NULL | NOW() | When attendance was marked | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.8 ExamSchedule

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| course_id | UUID | FK → Course.id | - | Course being examined | N |
| exam_date | DATE | NOT NULL | - | Date of examination | N |
| start_time | TIME | NOT NULL | - | Examination start time | N |
| end_time | TIME | NOT NULL | - | Examination end time | N |
| room | VARCHAR(50) | - | NULL | Examination room/hall | N |
| exam_type | VARCHAR(20) | NOT NULL | - | Type (Midterm, Final, Quiz) | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.9 ExamResult

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| enrolment_id | UUID | FK → Enrolment.id | - | Enrolment record | N |
| exam_schedule_id | UUID | FK → ExamSchedule.id | - | Examination schedule | N |
| marks_obtained | DECIMAL(5,2) | - | NULL | Marks obtained by student | N |
| total_marks | DECIMAL(5,2) | - | NULL | Total possible marks | N |
| grade | VARCHAR(5) | - | NULL | Grade awarded | N |
| entered_by | UUID | FK → User.id | - | Faculty who entered the result | N |
| entered_at | TIMESTAMPTZ | NOT NULL | NOW() | When result was entered | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.10 FeeAccount

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| student_id | UUID | FK → Student.id | - | Student account | N |
| total_due | DECIMAL(12,2) | - | 0.00 | Total fees due | N |
| total_paid | DECIMAL(12,2) | - | 0.00 | Total fees paid | N |
| outstanding | DECIMAL(12,2) | - | 0.00 | Outstanding balance | N |
| last_payment_date | DATE | - | NULL | Date of last payment | N |
| status | VARCHAR(20) | NOT NULL | 'Active' | Account status | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.11 FeeTransaction

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| fee_account_id | UUID | FK → FeeAccount.id | - | Associated fee account | N |
| amount | DECIMAL(12,2) | NOT NULL | - | Transaction amount | N |
| transaction_type | VARCHAR(20) | NOT NULL | - | Type (Payment, Refund, Adjustment, Penalty) | N |
| payment_method | VARCHAR(20) | - | NULL | Method (Online, POS, Cash, Bank Transfer) | N |
| reference_number | VARCHAR(100) | - | NULL | External reference number | N |
| transaction_date | TIMESTAMPTZ | NOT NULL | NOW() | Date and time of transaction | N |
| processed_by | UUID | FK → User.id | - | Staff who processed the transaction | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.12 Timetable

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL, FK | - | Tenant identifier | N |
| course_id | UUID | FK → Course.id | - | Course | N |
| faculty_id | UUID | FK → Faculty.id | - | Assigned faculty | N |
| day_of_week | INTEGER | NOT NULL | - | Day of week (1=Monday, 7=Sunday) | N |
| start_time | TIME | NOT NULL | - | Class start time | N |
| end_time | TIME | NOT NULL | - | Class end time | N |
| room | VARCHAR(50) | - | NULL | Classroom/venue | N |
| semester | VARCHAR(20) | NOT NULL | - | Semester | N |
| academic_year | VARCHAR(10) | NOT NULL | - | Academic year | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |
| is_deleted | BOOLEAN | NOT NULL | FALSE | Soft delete flag | N |

### 3.13 AuditLog

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL | - | Tenant identifier | N |
| user_id | UUID | - | NULL | User who performed the action | N |
| action | VARCHAR(50) | NOT NULL | - | Action performed (CREATE, UPDATE, DELETE, LOGIN, etc.) | N |
| entity_type | VARCHAR(50) | NOT NULL | - | Type of entity affected | N |
| entity_id | UUID | - | NULL | ID of entity affected | N |
| old_values | JSONB | - | NULL | Previous values (for UPDATE/DELETE) | Y |
| new_values | JSONB | - | NULL | New values (for CREATE/UPDATE) | Y |
| ip_address | INET | - | NULL | IP address of the request | Y |
| user_agent | VARCHAR(500) | - | NULL | User agent string | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | When the audit entry was created | N |

### 3.14 MLFeatureVector

| Column | Data Type | Constraints | Default | Description | PII |
|--------|-----------|-------------|---------|-------------|-----|
| id | UUID | PRIMARY KEY | gen_random_uuid() | Unique identifier | N |
| tenant_id | UUID | NOT NULL | - | Tenant identifier | N |
| entity_type | VARCHAR(50) | NOT NULL | - | Type of entity (Student, Course, etc.) | N |
| entity_id | UUID | NOT NULL | - | ID of the entity | N |
| vector | vector(1536) | - | NULL | Embedding vector for ML similarity search | N |
| metadata | JSONB | - | NULL | Additional metadata for the vector | N |
| model_version | VARCHAR(20) | - | NULL | Version of the ML model that generated the vector | N |
| created_at | TIMESTAMPTZ | NOT NULL | NOW() | Record creation timestamp | N |
| updated_at | TIMESTAMPTZ | NOT NULL | NOW() | Record last update timestamp | N |

## 4. Index Strategy

| Table | Index Name | Columns | Type | Purpose |
|-------|-----------|---------|------|---------|
| Institution | idx_institution_tenant | tenant_id | UNIQUE B-tree | Tenant lookup |
| User | idx_user_tenant_email | (tenant_id, email) | UNIQUE B-tree | Login lookup |
| User | idx_user_tenant_role | (tenant_id, role) | B-tree | Role-based queries |
| Student | idx_student_tenant_studentid | (tenant_id, student_id) | UNIQUE B-tree | Student ID lookup |
| Student | idx_student_tenant_programme | (tenant_id, programme) | B-tree | Programme filtering |
| Student | idx_student_tenant_status | (tenant_id, status) | B-tree | Status filtering |
| Faculty | idx_faculty_tenant_employeeid | (tenant_id, employee_id) | UNIQUE B-tree | Employee ID lookup |
| Faculty | idx_faculty_tenant_department | (tenant_id, department) | B-tree | Department filtering |
| Department | idx_department_tenant_code | (tenant_id, code) | UNIQUE B-tree | Department code lookup |
| Course | idx_course_tenant_code | (tenant_id, code) | UNIQUE B-tree | Course code lookup |
| Course | idx_course_tenant_department | (tenant_id, department_id) | B-tree | Department course listing |
| Enrolment | idx_enrolment_tenant_student | (tenant_id, student_id) | B-tree | Student enrolment history |
| Enrolment | idx_enrolment_tenant_course | (tenant_id, course_id) | B-tree | Course roster |
| Enrolment | idx_enrolment_unique | (student_id, course_id, semester, academic_year) | UNIQUE B-tree | Prevent duplicate enrolment |
| Attendance | idx_attendance_tenant_enrolment | (tenant_id, enrolment_id) | B-tree | Attendance by enrolment |
| Attendance | idx_attendance_unique | (enrolment_id, lecture_date) | UNIQUE B-tree | Prevent duplicate attendance |
| ExamSchedule | idx_examschedule_tenant_course | (tenant_id, course_id) | B-tree | Exam schedule by course |
| ExamResult | idx_examresult_tenant_enrolment | (tenant_id, enrolment_id) | B-tree | Results by enrolment |
| ExamResult | idx_examresult_unique | (enrolment_id, exam_schedule_id) | UNIQUE B-tree | Prevent duplicate results |
| FeeAccount | idx_feeaccount_tenant_student | (tenant_id, student_id) | UNIQUE B-tree | Fee account by student |
| FeeTransaction | idx_feetransaction_tenant_account | (tenant_id, fee_account_id) | B-tree | Transactions by account |
| FeeTransaction | idx_feetransaction_date | (tenant_id, transaction_date) | B-tree | Date-range queries |
| Timetable | idx_timetable_tenant_course | (tenant_id, course_id) | B-tree | Timetable by course |
| Timetable | idx_timetable_tenant_faculty | (tenant_id, faculty_id) | B-tree | Timetable by faculty |
| AuditLog | idx_auditlog_tenant_created | (tenant_id, created_at DESC) | B-tree | Recent audit entries |
| AuditLog | idx_auditlog_tenant_user | (tenant_id, user_id) | B-tree | Audit by user |
| AuditLog | idx_auditlog_tenant_entity | (tenant_id, entity_type, entity_id) | B-tree | Audit by entity |
| MLFeatureVector | idx_mlvector_tenant_entity | (tenant_id, entity_type, entity_id) | B-tree | Vector lookup by entity |
| MLFeatureVector | idx_mlvector_hnsw | vector | HNSW | Similarity search (pgvector) |

## 5. Multi-Tenancy Isolation Strategy

### 5.1 Portable application filter

Every mapped entity that implements `ITenantScoped` receives an EF Core global
query filter comparing its `TenantId` with
`AegisDbContext.CurrentTenantId`. This is the authoritative portable isolation
mechanism for PostgreSQL, MySQL, SQL Server, and Oracle. Cross-tenant background
jobs may bypass the filter only for tenant-ID discovery and must bind the
subsequent operation to an explicit tenant.

### 5.2 PostgreSQL defense in depth

Applied PostgreSQL Finance migrations retain their historical forced RLS
policies. A PostgreSQL-only connection interceptor supplies `app.tenant_id` so
those policies continue to protect existing installations. Other providers do
not register the interceptor.

### 5.3 Provider-specific physical model

`DB_DIALECT` selects one adapter and migration assembly. PostgreSQL stores JSON
as `jsonb`, MySQL as `json`, SQL Server as `nvarchar(max)`, and Oracle as

