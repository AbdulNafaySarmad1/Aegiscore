# Document 2 — Software Requirements Specification
**Standard:** ISO/IEC/IEEE 29148

| Field | Value |
|-------|-------|
| Document ID | AEGIS-SRS-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) defines the complete functional and non-functional requirements for AegisCore, a multi-tenant, cloud-native university ERP platform developed by Nocturne Systems (Pvt. Ltd.) for Pakistani public universities. This document serves as the authoritative reference for all stakeholders — developers, testers, system administrators, and institutional users — to understand what the system must do and under what constraints it must operate.

### 1.2 Scope

AegisCore encompasses six integrated modules: Student Lifecycle, Finance & Fees, Faculty Portal, ML Analytics, Enterprise Security, and System Administration. The system is designed to serve 150+ Pakistani public universities, supporting 9,000+ concurrent users with a design target of 50,000 concurrent users. This SRS covers the complete functional behaviour, performance characteristics, security requirements, and operational constraints of the Version 1.0.0 release.

### 1.3 Definitions, Acronyms, and Abbreviations

| Term | Definition |
|------|------------|
| ABAC | Attribute-Based Access Control — authorisation model using attributes of users, resources, and environment |
| Argon2id | Memory-hard password hashing algorithm, winner of the Password Hashing Competition |
| Casbin | Open-source access control library supporting multiple models including ABAC |
| CI/CD | Continuous Integration / Continuous Deployment |
| EPYC | AMD EPYC processor series used in Hetzner dedicated servers |
| HEC | Higher Education Commission of Pakistan |
| HEI | Higher Education Institution |
| JWT | JSON Web Token — compact, URL-safe means of representing claims between parties |
| ML | Machine Learning |
| pgvector | PostgreSQL extension for vector similarity search |
| POS | Point of Sale — physical fee collection terminal |
| RLS | Row-Level Security — PostgreSQL feature for fine-grained access control |
| RTO | Recovery Time Objective |
| RPO | Recovery Point Objective |
| SAM | Serviceable Addressable Market |
| SLA | Service Level Agreement |
| TLS | Transport Layer Security |

### 1.4 References

1. ISO/IEC/IEEE 29148:2018 — Systems and software engineering — Life cycle processes — Requirements engineering
2. AegisCore Vision Document (AEGIS-VISION-001)
3. AegisCore P1.txt — Project Context Document
4. OWASP ASVS Version 4.0.3
5. PostgreSQL 16 Documentation
6. Casbin Documentation — https://casbin.org

### 1.5 Overview

Section 2 provides the overall description of the product. Section 3 enumerates all functional requirements organised by module. Section 4 specifies non-functional requirements covering performance, security, scalability, and operational characteristics. Section 5 defines external interface requirements. Section 6 documents system constraints.

## 2. Overall Description

### 2.1 Product Perspective

AegisCore is a new, self-contained software system. It does not replace an existing system at any specific institution but rather provides a unified platform where previously fragmented or non-existent systems operated. The system interfaces with:

- **External banking systems** via API for online fee collection
- **HEC central reporting systems** via standardised data export formats
- **Existing LMS platforms** via REST API for course and enrolment data synchronisation
- **Email and SMS gateways** for notifications
- **Infisical** for secrets management
- **GitHub Actions** for CI/CD pipeline automation

The system is deployed across three infrastructure nodes:
- **Hetzner Helsinki (Primary):** Production workload, PostgreSQL primary, Redis primary
- **Hetzner Nuremberg (Secondary/DR):** PostgreSQL standby, Redis replica, warm standby for failover
- **Azure App Service:** CI/CD pipeline, staging environment, auxiliary services

### 2.2 Product Functions

AegisCore provides the following high-level functions:

1. **Student Lifecycle Management:** End-to-end management from admission application through graduation, including enrolment, registration, attendance, examination, grading, and transcript generation.
2. **Financial Management:** Fee structure configuration, online and POS fee collection, ledger management, reconciliation, and financial reporting.
3. **Faculty Workflow Management:** Course assignment, attendance marking, grade entry, student performance monitoring, and departmental administration.
4. **Machine Learning Analytics:** Predictive modelling for student retention, performance trend analysis, resource utilisation forecasting, and institutional benchmarking.
5. **Enterprise Security:** Authentication, authorisation (ABAC), audit logging, encryption, tenant isolation, and security monitoring.
6. **System Administration:** Tenant provisioning, user management, role configuration, system monitoring, backup management, and licence tracking.

### 2.3 User Classes and Characteristics

| User Class | Description | Technical Proficiency | Primary Interactions |
|------------|-------------|----------------------|---------------------|
| SuperAdmin | System-wide administrator with full access across all tenants | High | Tenant provisioning, system configuration, audit review |
| ViceChancellor | Institutional executive with read access to all institutional data and analytics | Medium | Dashboard, analytics, reports |
| Registrar | Operational manager responsible for student records and academic administration | Medium-High | Student lifecycle, admissions, examinations, graduation |
| Faculty | Teaching staff managing courses and student assessment | Low-Medium | Attendance, grade entry, course materials |
| Student | Enrolled student accessing personal academic information | Low | Records, timetable, results, fee status |
| FinanceOfficer | Staff responsible for fee collection and financial management | Medium | Fee processing, POS, ledger, reconciliation |
| SystemAdministrator | IT staff responsible for infrastructure and maintenance | High | Deployment, monitoring, backup, troubleshooting |

### 2.4 Operating Environment

- **Server OS:** Ubuntu 22.04 LTS (Hetzner EPYC), Azure App Service (Windows/Linux)
- **Runtime:** .NET 10, C# 14
- **Database:** PostgreSQL 16+ with pgvector extension
- **Cache:** Redis 7+
- **Web Server:** Kestrel (behind nginx reverse proxy on Hetzner)
- **Client Browser:** Modern browsers — Chrome 120+, Firefox 120+, Edge 120+, Safari 17+
- **Network:** HTTPS (TLS 1.3), WebSocket (SignalR)
- **Container Runtime:** Docker 24+

### 2.5 Design and Implementation Constraints

1. The authoritative backend must use C# 14 on .NET 10; the browser frontend must use Next.js 16 and React 19.
2. The system must use PostgreSQL as the sole relational database with multi-tenant isolation via Row-Level Security.
3. The system must use Redis for caching and session management.
4. The system must implement ABAC via Casbin.NET — role-based access control alone is insufficient.
5. The system must use Argon2id for password hashing with configurable parameters.
6. The system must support horizontal scaling to 50,000 concurrent users without architectural changes.
7. The system must deploy via Docker containers orchestrated through GitHub Actions.
8. All secrets must be managed through Infisical and never stored in configuration files or environment variables directly.
9. The system must maintain tenant data isolation such that no tenant can access another tenant's data under any circumstances.
10. The system must comply with HEC data reporting standards and formats.

### 2.6 Assumptions and Dependencies

As documented in the Vision Document (AEGIS-VISION-001), Section 6.

## 3. Functional Requirements

### 3.1 Student Lifecycle Module

**FR-001:** The system shall allow Registrars to create and manage admission sessions with configurable start dates, end dates, and programme quotas.

**FR-002:** The system shall allow prospective students to submit online admission applications with document uploads (CNIC, transcripts, photographs).

**FR-003:** The system shall support configurable merit calculation formulas based on academic history, entry test scores, and institutional preferences.

**FR-004:** The system shall generate merit lists automatically upon admission session closure and allow manual adjustments by authorised Registrar staff.

**FR-005:** The system shall allow Registrars to enrol accepted students into programmes, assigning student IDs and generating enrolment records.

**FR-006:** The system shall support course registration each semester with prerequisite validation and credit hour limit enforcement.

**FR-007:** The system shall allow Faculty to mark student attendance per lecture with configurable attendance policies (minimum percentage requirements).

**FR-008:** The system shall allow Registrars to create examination schedules with room assignments, invigilator assignments, and conflict detection.

**FR-009:** The system shall allow Faculty to enter student grades with configurable grading scales and automatic GPA/CGPA calculation.

**FR-010:** The system shall generate official transcripts in HEC-compliant format with digital signatures.

**FR-011:** The system shall track graduation eligibility based on credit hour completion, CGPA thresholds, and any additional institutional requirements.

**FR-012:** The system shall maintain a complete audit trail of all changes to student records with timestamp, user ID, and previous/new values.

### 3.2 Finance & Fees Module

**FR-013:** The system shall allow Finance Officers to define fee structures per programme, semester, and student category with configurable line items.

**FR-014:** The system shall generate fee challans for students with unique payment identifiers and due dates.

**FR-015:** The system shall support online fee payment via integration with Pakistani banking gateways (1Link, etc.).

**FR-016:** The system shall support POS-based fee collection with receipt generation and real-time ledger updates.

**FR-017:** The system shall maintain a double-entry ledger with automated reconciliation against bank statements.

**FR-018:** The system shall generate financial reports including fee collection summaries, outstanding dues, revenue projections, and audit-ready transaction logs.

**FR-019:** The system shall support configurable late fee penalties and discount policies.

**FR-020:** The system shall allow Finance Officers to process refunds with approval workflow and audit trail.

**FR-021:** The system shall integrate fee payment status with student enrolment — students with outstanding fees beyond configurable thresholds shall be flagged for enrolment restrictions.

### 3.3 Faculty Portal Module

**FR-022:** The system shall allow Faculty to view their assigned courses, class schedules, and student rosters.

**FR-023:** The system shall allow Faculty to mark student attendance with support for bulk marking and individual student notes.

**FR-024:** The system shall allow Faculty to enter and submit student grades with validation against configured grading scales.

**FR-025:** The system shall provide Faculty with student performance analytics including attendance history, grade trends, and comparative class statistics.

**FR-026:** The system shall allow Department Heads to manage course assignments, faculty workloads, and departmental timetables.

**FR-027:** The system shall support faculty leave management with approval workflow.

**FR-028:** The system shall allow Faculty to upload course materials (syllabus, lecture notes) for student access.

**FR-029:** The system shall track faculty research output including publications, projects, and supervision assignments.

### 3.4 ML Analytics Module

**FR-030:** The system shall generate student retention risk scores using ML models trained on historical attendance, grade, and demographic data.

**FR-031:** The system shall provide institutional benchmarking comparing key metrics (graduation rate, retention rate, average GPA) against anonymised peer institution data.

**FR-032:** The system shall forecast resource utilisation (classroom occupancy, faculty workload) for upcoming semesters based on enrolment projections.

**FR-033:** The system shall generate automated alerts for students flagged as at-risk based on configurable thresholds.

**FR-034:** The system shall store ML feature vectors using pgvector for efficient similarity search and clustering.

**FR-035:** The system shall provide Vice Chancellors with an executive dashboard displaying key institutional KPIs with drill-down capability.

**FR-036:** The system shall support configurable ML model retraining schedules with version tracking.

### 3.5 Enterprise Security Module

**FR-037:** The system shall authenticate users via Argon2id password hashing with configurable memory, iterations, and parallelism parameters.

**FR-038:** The system shall issue JWTs upon successful authentication with configurable expiry and refresh token support.

**FR-039:** The system shall enforce ABAC policies via Casbin for every API request, evaluating user attributes, resource attributes, and environment attributes.

**FR-040:** The system shall support role hierarchy: SuperAdmin > ViceChancellor > Registrar > DepartmentHead > Faculty > Student, with FinanceOfficer as a parallel branch.

**FR-041:** The system shall log all authentication attempts (successful and failed), authorisation decisions, and sensitive data access in an immutable audit log.

**FR-042:** The system shall enforce tenant isolation via PostgreSQL Row-Level Security policies such that queries are automatically filtered by tenant_id.

**FR-043:** The system shall encrypt sensitive data at rest using PostgreSQL column-level encryption for PII fields.

**FR-044:** The system shall enforce TLS 1.3 for all external communications and TLS 1.2 minimum for internal service-to-service communication.

**FR-045:** The system shall support multi-factor authentication (MFA) via TOTP for administrative accounts.

**FR-046:** The system shall implement rate limiting on authentication endpoints (5 attempts per minute per IP) and API endpoints (configurable per endpoint).

### 3.6 System Administration Module

**FR-047:** The system shall allow SuperAdmins to provision new tenants with institution-specific configuration (name, logo, domain, campuses, academic policies, workflows, integrations, and branding). Every tenant receives the complete core suite.

**FR-048:** The system shall allow SuperAdmins to create the initial tenant administrator account during provisioning.

**FR-049:** The system shall support bulk user import via CSV with validation and error reporting.

**FR-050:** The system shall allow administrators to configure Casbin policies through a web interface without requiring code changes.

**FR-051:** The system shall provide system health monitoring dashboards showing CPU, memory, disk, database connections, Redis memory, and request latency.

**FR-052:** The system shall support automated database backups with configurable schedules and retention policies.

**FR-053:** The system shall allow administrators to view and query audit logs filtered by tenant, user, action type, and date range.

**FR-054:** The system shall track complete-suite licence information per tenant including institution identity, licensed headcount, deployment identity, activation state, heartbeat status, and expiry date.

**FR-055:** The system shall send automated alerts for system events including backup failures, disk space warnings, and security anomalies.

## 4. Non-Functional Requirements

### 4.1 Performance Requirements

**NFR-001:** The system shall support a minimum of 9,000 concurrent users with a design target of 50,000 concurrent users.

**NFR-002:** The system shall maintain a p95 response time of less than 200 milliseconds for all API endpoints under full concurrent load.

**NFR-003:** The system shall maintain a p99 response time of less than 500 milliseconds for all API endpoints under full concurrent load.

**NFR-004:** Database query execution time shall not exceed 100 milliseconds for 95% of queries under production load.

**NFR-005:** The system shall serve Next.js static assets with a Time to First Byte (TTFB) of less than 50 milliseconds.

**NFR-006:** SignalR real-time updates shall be delivered within 500 milliseconds of the triggering event.

### 4.2 Scalability Requirements

**NFR-007:** The system shall scale horizontally by adding additional application server instances without code changes.

**NFR-008:** The system shall support linear throughput scaling up to 50,000 concurrent users with proportional infrastructure increase.

**NFR-009:** PostgreSQL shall support read scaling via read replicas with automatic failover.

**NFR-010:** Redis shall support clustering for horizontal cache scaling.

### 4.3 Availability Requirements

**NFR-011:** The system shall maintain 99.9% uptime (excluding planned maintenance windows) measured monthly.

**NFR-012:** Planned maintenance windows shall not exceed 4 hours per month and shall be scheduled during off-peak hours (02:00–06:00 Pakistan Standard Time).

**NFR-013:** The system shall achieve a Recovery Time Objective (RTO) of less than 1 hour for disaster recovery failover from Helsinki to Nuremberg.

**NFR-014:** The system shall achieve a Recovery Point Objective (RPO) of less than 5 minutes for data loss in disaster recovery scenarios.

### 4.4 Security Requirements

**NFR-015:** The system shall comply with OWASP ASVS Level 2 requirements as verified by external security audit.

**NFR-016:** All authentication credentials shall be hashed using Argon2id with minimum parameters: memory=65536 KiB, iterations=3, parallelism=4.

**NFR-017:** All data in transit shall be encrypted using TLS 1.3 (external) or TLS 1.2 minimum (internal).

**NFR-018:** All PII data at rest shall be encrypted using AES-256-GCM.

**NFR-019:** The system shall maintain an immutable audit log of all security-relevant events with tamper detection.

**NFR-020:** Session tokens shall expire after 60 minutes of inactivity and shall be invalidated upon password change.

**NFR-021:** The system shall enforce password complexity: minimum 12 characters, at least one uppercase, one lowercase, one digit, and one special character.

### 4.5 Maintainability Requirements

**NFR-022:** The system shall achieve a minimum of 80% code coverage for unit tests across all business logic modules.

**NFR-023:** All public API endpoints shall be documented with OpenAPI 3.1 specifications.

**NFR-024:** The system shall support zero-downtime deployments for patch and minor version updates.

**NFR-025:** Database migrations shall be reversible for the most recent migration.

### 4.6 Portability Requirements

**NFR-026:** The system shall be deployable on any x86-64 Linux server running Ubuntu 22.04 LTS or later.

**NFR-027:** The system shall be deployable via Docker containers with all dependencies encapsulated.

**NFR-028:** The system shall support migration between cloud providers with minimal configuration changes.

## 5. External Interface Requirements

### 5.1 User Interfaces

- **Next.js 16 and React 19:** Primary user interface for all user roles, rendered in modern web browsers.
- **AegisCore design system:** Reusable accessible components providing a consistent institutional interface.
- **Responsive Design:** All interfaces shall be usable on screens from 320px (mobile) to 2560px (desktop).
- **Accessibility:** Interfaces shall meet WCAG 2.1 Level AA compliance.

### 5.2 Hardware Interfaces

- **Hetzner EPYC Servers:** AMD EPYC processors, minimum 32 cores, 128 GB RAM, NVMe SSD storage.
- **Network:** 1 Gbps minimum bandwidth between application servers and database servers.

### 5.3 Software Interfaces

| Interface | Protocol | Purpose |
|-----------|----------|---------|
| PostgreSQL 16+ | TCP/IP, port 5432 | Primary data store |
| Redis 7+ | TCP/IP, port 6379 | Cache and session store |
| Infisical | HTTPS REST API | Secrets management |
| GitHub Actions | HTTPS REST API | CI/CD pipeline triggers |
| Banking Gateways (1Link) | HTTPS REST API | Online fee payment processing |
| Email Gateway (SMTP) | SMTP/TLS, port 587 | Email notifications |
| SMS Gateway | HTTPS REST API | SMS notifications |
| LMS Platforms | HTTPS REST API | Course and enrolment data synchronisation |

### 5.4 Communication Interfaces

- **REST API:** Primary client-server communication, JSON payloads, OpenAPI 3.1 documented.
- **GraphQL:** Available for complex data queries requiring client-specified field selection.
- **SignalR:** Real-time bidirectional communication for notifications, live dashboards, and collaborative features.
- **WebSocket:** Underlying transport for SignalR connections.

## 6. System Constraints

1. **Multi-Tenant Architecture:** Every database query must be scoped to a single tenant. Cross-tenant queries are prohibited except for system-level analytics using anonymised data.
2. **HEC Compliance:** All student record formats, transcript formats, and reporting data structures must conform to HEC specifications.
3. **Data Sovereignty:** All student PII must remain within Pakistani jurisdiction or approved international jurisdictions as defined by HEC and Ministry of IT policy.
4. **Technology Stack:** The system must use the specified technology stack (C#/.NET 10, PostgreSQL, Redis, Casbin) — deviations require architectural review and approval.
5. **Licensing Model:** The system must enforce a complete-suite licence per tenant based on institution identity, contracted headcount, deployment identity, activation state, heartbeat status, and expiry. Core modules cannot be licensed separately.
6. **Audit Trail:** All modifications to student records, financial transactions, and user permissions must be logged immutably.
7. **Backward Compatibility:** API changes must maintain backward compatibility for at least one major version.

## 7. Appendices

### Appendix A: Requirements Traceability Matrix

A full requirements traceability matrix mapping each functional requirement to its originating stakeholder need, architectural component, and verification method is maintained in the project management system and updated with each release.

### Appendix B: Glossary

See Section 1.3 for the complete glossary of terms.
