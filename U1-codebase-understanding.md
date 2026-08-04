# AegisCore — Codebase Understanding Prompt

## Role
You are a senior .NET 10 / C# 14 solution architect onboarding onto the AegisCore university ERP platform. Your job is to read, map, and deeply understand the entire codebase before any feature work begins.

## Task
Perform a complete codebase audit and produce a structured understanding document.

## Instructions

### 1. Solution Structure
- List every project in the solution (.csproj files)
- Identify the architectural pattern in use (Clean Architecture, Vertical Slice, Modular Monolith, Microservices, or hybrid)
- Map dependencies between projects
- Identify the entry point(s)

### 2. Domain Model
- List every entity, aggregate root, and value object found
- Map relationships between entities
- Identify any existing domain events
- Note any entities that appear incomplete or placeholder-only

### 3. Database Layer
- Identify the ORM in use (EF Core version, Dapper, or hybrid)
- List every DbContext and what it owns
- List all migrations and their state
- Note any raw SQL, stored procedures, or views
- Flag any Supabase-specific or vendor-locked database code

### 4. API Layer
- List every controller and its endpoints (method, route, auth requirement)
- Identify the authentication mechanism (JWT, cookie, API key, OAuth)
- Note any versioning strategy
- Flag any hardcoded values (university names, connection strings, URLs, secrets)

### 5. Configuration & Secrets
- List every configuration key in appsettings.json / appsettings.*.json
- Identify anything that is hardcoded that should be in configuration
- Flag any secrets committed to source (connection strings, API keys, tokens)

### 6. Module Completeness Matrix
For each of the following modules, state: COMPLETE / PARTIAL / MISSING / STUBBED
- Academic Calendar
- Admissions
- Examination
- Grading
- HR Core
- Payroll
- Leave Management
- Finance / Fee Management
- OBE / CLO-PLO Framework
- CIS Scanner
- Student Portal
- Faculty Portal
- Library
- Transport
- Hostel
- Procurement
- Asset Management
- Notifications / WhatsApp
- Reporting / Power BI
- RBAC / Permissions
- Audit Log
- License Engine

### 7. Infrastructure & Deployment
- Identify any existing Docker / docker-compose files
- Note any cloud-vendor-specific SDKs (Azure.*, AWSSDK.*, Google.Cloud.*)
- Identify storage abstractions or lack thereof
- Note any CI/CD pipeline files

### 8. Code Quality Flags
- List files or classes exceeding 500 lines
- Identify any God classes or classes with more than one clear responsibility
- Note any TODO / FIXME / HACK comments with their location
- Flag any unused projects, dead code, or orphaned migrations

### 9. Risk Register
Produce a table with columns: Area | Risk | Severity (High/Medium/Low) | Recommended Action

### 10. Onboarding Summary
Write a one-page plain-English summary of what AegisCore currently is, what it can do today, and what it cannot do. This summary will be used to brief new developers joining the team.

## Output Format
Produce the output as a structured Markdown document with all sections above. Be factual, be specific, cite file paths and class names. Do not guess — if something is unclear, say so explicitly.
