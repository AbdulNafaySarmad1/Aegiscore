# Document 3 — Software Architecture Document
**Standards:** ISO/IEC/IEEE 42010 + C4 Model + UML

| Field | Value |
|-------|-------|
| Document ID | AEGIS-SAD-001 |
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

This Software Architecture Document (SAD) describes the architectural design of AegisCore, a multi-tenant, cloud-native university ERP platform. It follows the ISO/IEC/IEEE 42010 architecture description framework and employs the C4 Model for structural decomposition at four levels of abstraction. This document serves as the authoritative reference for the development team, infrastructure engineers, and technical stakeholders to understand the system's structure, behaviour, and design rationale.

### 1.2 Scope

This document covers the complete architecture of AegisCore Version 1.0.0, including system context, container decomposition, component-level design, code-level patterns, architectural decisions, and quality attribute scenarios.

### 1.3 References

1. ISO/IEC/IEEE 42010:2022 — Systems and software engineering — Architecture description
2. C4 Model — https://c4model.com
3. AegisCore Vision Document (AEGIS-VISION-001)
4. AegisCore Software Requirements Specification (AEGIS-SRS-001)
5. UML 2.5.1 Specification

## 2. Architecture Description (ISO/IEC/IEEE 42010 Viewpoints)

### 2.1 Stakeholders and Concerns

| Stakeholder | Concerns |
|-------------|----------|
| Development Team | Modularity, testability, code organisation, technology choices |
| Infrastructure Engineers | Deployability, scalability, monitoring, disaster recovery |
| Security Team | Authentication, authorisation, data protection, audit |
| Institutional IT Directors | Integration, performance, reliability, data sovereignty |
| HEC Auditors | Compliance, data standards, security certification |

### 2.2 Architecture Viewpoints

| Viewpoint | Purpose | Model Used |
|-----------|---------|------------|
| Context | System scope and external actors | C4 Level 1 |
| Container | High-level technology choices and responsibilities | C4 Level 2 |
| Component | Internal structure of the core API | C4 Level 3 |
| Code | Implementation patterns and conventions | C4 Level 4 + UML |
| Deployment | Physical infrastructure topology | UML Deployment Diagram |
| Behavioural | Runtime interaction sequences | UML Sequence Diagram |
| Structural | Domain entity relationships | UML Class Diagram |

## 3. C4 Model — Level 1: System Context

AegisCore is a unified university ERP platform that serves as the central system of record for Pakistani public universities. It interacts with the following external actors and systems:

**External Actors (People):**
- **Students:** Access personal academic records, fee status, timetable, and results via Blazor web interface.
- **Faculty:** Mark attendance, enter grades, manage courses via Blazor web interface.
- **Registrars:** Manage student lifecycle, admissions, examinations, graduation via Blazor web interface.
- **Vice Chancellors:** View institutional dashboards and analytics via Blazor web interface.
- **Finance Officers:** Process fees, manage ledger, generate reports via Blazor web interface.
- **SuperAdmins:** Provision tenants, manage system configuration, review audit logs via Blazor web interface.
- **System Administrators:** Monitor infrastructure, manage backups, troubleshoot issues via administrative interfaces.

**External Systems:**
- **HEC Central Systems:** AegisCore exports standardised reports and data to HEC for compliance and benchmarking.
- **Banking Gateways (1Link):** AegisCore integrates with Pakistani banking APIs for online fee collection.
- **Email Gateway (SMTP):** AegisCore sends transactional emails (admission confirmations, fee reminders, result notifications).
- **SMS Gateway:** AegisCore sends SMS notifications for critical alerts.
- **LMS Platforms:** AegisCore exchanges course and enrolment data with existing Learning Management Systems.
- **Infisical:** AegisCore retrieves secrets (database credentials, API keys) at runtime from Infisical.
- **GitHub Actions:** AegisCore's CI/CD pipeline is triggered by GitHub Actions for automated build, test, and deployment.

## 4. C4 Model — Level 2: Container Diagram

AegisCore is composed of the following containers:

### 4.1 Blazor WebAssembly Frontend
- **Technology:** Blazor WebAssembly, MudBlazor, C#/Razor
- **Responsibility:** Delivers the user interface for all user roles. Runs entirely in the browser. Communicates with the API container via REST and SignalR.
- **Deployment:** Served as static files from the API container or a CDN.

### 4.2 .NET 10 API Server
- **Technology:** ASP.NET Core 10 Minimal API / Controllers, C# 14
- **Responsibility:** Implements all business logic, enforces ABAC authorisation via Casbin, manages authentication (Argon2id + JWT), orchestrates database operations, and serves the Blazor frontend static files.
- **Deployment:** Docker container on Hetzner EPYC servers, horizontally scaled behind nginx reverse proxy.

### 4.3 PostgreSQL Database
- **Technology:** PostgreSQL 16+ with pgvector extension
- **Responsibility:** Primary data store for all tenant data. Implements multi-tenant isolation via Row-Level Security. Stores ML feature vectors using pgvector.
- **Deployment:** Primary on Hetzner Helsinki, standby replica on Hetzner Nuremberg.

### 4.4 Redis Cache
- **Technology:** Redis 7+
- **Responsibility:** Session storage, response caching, distributed locking, SignalR backplane for horizontal scaling.
- **Deployment:** Primary on Hetzner Helsinki, replica on Hetzner Nuremberg.

### 4.5 Infisical Secrets Manager
- **Technology:** Infisical (self-hosted or cloud)
- **Responsibility:** Secure storage and injection of all secrets (database credentials, API keys, JWT signing keys) at container startup.
- **Deployment:** Accessed via HTTPS API at runtime.

### 4.6 GitHub Actions CI/CD
- **Technology:** GitHub Actions workflows
- **Responsibility:** Automated build, test, Docker image creation, and deployment to Hetzner and Azure environments.
- **Deployment:** GitHub-hosted runners.

### 4.7 Azure App Service (Auxiliary)
- **Technology:** Azure App Service
- **Responsibility:** Hosts staging environment, runs certain auxiliary services, provides additional geographic redundancy.
- **Deployment:** Azure West Europe region.

## 5. C4 Model — Level 3: Component Diagram (Core API)

The .NET 10 API Server is decomposed into the following components:

### 5.1 Authentication Component
- **Responsibility:** User registration, login, password hashing (Argon2id), JWT issuance and validation, refresh token management, MFA (TOTP).
- **Interfaces:** `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `POST /api/auth/mfa/verify`
- **Dependencies:** User Repository, Argon2id Library, JWT Library, Redis (for refresh token storage)

### 5.2 Authorisation Component (Casbin ABAC)
- **Responsibility:** Policy enforcement point for every API request. Evaluates Casbin policies based on user attributes (role, department, tenant), resource attributes (endpoint, entity type, tenant), and environment attributes (time, IP).
- **Interfaces:** ASP.NET Core Middleware (intercepts all requests), Casbin Enforcer
- **Dependencies:** Casbin.NET, Policy Store (PostgreSQL), User Repository

### 5.3 Student Lifecycle Component
- **Responsibility:** Admissions, enrolment, registration, attendance, examinations, grading, transcripts, graduation.
- **Interfaces:** REST endpoints under `/api/students`, `/api/admissions`, `/api/examinations`, `/api/grades`
- **Dependencies:** Student Repository, Course Repository, Enrolment Repository, Attendance Repository, Grade Repository

### 5.4 Finance Component
- **Responsibility:** Fee structure management, fee collection (online and POS), ledger management, reconciliation, financial reporting.
- **Interfaces:** REST endpoints under `/api/finance`, `/api/fees`, `/api/ledger`
- **Dependencies:** Fee Repository, Transaction Repository, Ledger Repository, Banking Gateway Client

### 5.5 Faculty Component
- **Responsibility:** Course management, attendance marking, grade entry, faculty workload, research tracking.
- **Interfaces:** REST endpoints under `/api/faculty`, `/api/courses`
- **Dependencies:** Faculty Repository, Course Repository, Attendance Repository, Grade Repository

### 5.6 ML Analytics Component
- **Responsibility:** Student retention prediction, performance analytics, institutional benchmarking, resource forecasting.
- **Interfaces:** REST endpoints under `/api/analytics`, internal service for model inference
- **Dependencies:** ML Model Service, pgvector Repository, Analytics Repository

### 5.7 Notification Component (SignalR)
- **Responsibility:** Real-time notifications to connected clients via SignalR hubs.
- **Interfaces:** SignalR Hub (`/hubs/notifications`), internal event bus
- **Dependencies:** SignalR, Redis (backplane), Email Client, SMS Client

### 5.8 System Administration Component
- **Responsibility:** Tenant provisioning, user management, policy configuration, system monitoring, backup management, licence tracking.
- **Interfaces:** REST endpoints under `/api/admin`, `/api/tenants`
- **Dependencies:** Tenant Repository, User Repository, Audit Repository, Monitoring Service

## 6. C4 Model — Level 4: Code-Level Patterns

### 6.1 Repository Pattern

All data access is abstracted behind repository interfaces following the Repository pattern:

