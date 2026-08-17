# AegisCore UNIFIED_SERVICE_DESK.md

Enterprise-wide ticketing / service-desk audit and architecture. Generated 2026-08-12 on branch `agent/prod-hardening`.
Audit + architecture ONLY. No implementation performed.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `DOCUMENTED_NOT_VERIFIED`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

Authoritative codebase = repo ROOT (`AegisCore.*`). `aegiscore-backend/` and `aegiscore-frontend/` are STALE and ignored.

---

## 1. Executive conclusion

**AegisCore does NOT have a general enterprise ticketing / service-desk system.**

It has several **specialized, department-siloed ticket-like systems** (maintenance/facilities, AI-first support, grievances, mental-health/counseling, security incidents, grade appeals) and a **configurable workflow engine** that could support a unified ticket lifecycle. But:

- The closest thing to a general ticket model (`MaintenanceTicket` + `TicketComment` + `TicketEscalation` + `SLADefinition` + `WorkOrder`) is **orphaned application-layer code**: its services/repositories are NOT registered in DI and NO controller exposes it (`CONFIRMED_BY_CODE` — zero references from API/DI).
- No department-agnostic "ticket core" exists.
- No HR or IT service-desk queue exists for employees/faculty.
- No general request→category→queue→assign→comment→escalate→resolve→close→audit flow exists.
- The AI support ticketing system is AI-first (LLM auto-answer + escalation), not an agent-driven service desk.
- Mental-health ticketing is the most complete ticket system but is intentionally SPECIALIZED (anonymity, crisis, mandatory reporting) and must remain protected.

**Core-question answer: NO.** An employee cannot today open an HR service-desk ticket and have it flow through an HR queue with assignment/comments/escalation/resolution/audit. The maintenance-ticket domain model is close to what's needed, but it is not wired and is facilities-specific.

**Recommended path:** Build a **Unified Ticket Core** in .NET that provides common primitives (ticket, comments, attachments, status history, assignment, priorities, SLA targets, watchers, categories from tenant config), and **integrate specialized domains as guarded consumers** — never weaken mental-health/counseling security.

---

## 2. Existing ticket-like systems

| # | NAME | DOMAIN | ENTITY | TABLE | CONTROLLER | SERVICE | FRONTEND ROUTE | STATUS | CONFIDENCE |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Maintenance/Facilities ticketing | Campus / Facilities | `MaintenanceTicket`, `TicketComment`, `TicketEscalation`, `SLADefinition`, `WorkOrder` (+PM/equipment/contractor) | `MaintenanceTickets`, `TicketComments`, `TicketEscalations`, `SLADefinitions`, `WorkOrders` | **NONE (orphaned)** | `Application/Maintenance/TicketService.cs`, `SLAEngineService.cs`, `WorkOrderService.cs`, `PreventiveMaintenanceService.cs` | NONE | SPECIALIZED_ONLY (unwired) | 90 |
| 2 | AI support ticketing | Platform / Support | `support_tickets` (raw SQL, not an EF entity) | `support_tickets`, `support_escalations`, `support_knowledge_base` (runtime-created via `EnsureSchemaAsync`) | `SupportController` (`/api/support/*`) | `Infrastructure/Support/SupportTicketService.cs` | NONE | PARTIAL (AI-first, not agent queue) | 90 |
| 3 | Customer-portal support ticket | Platform / Marketplace | `SupportTicket` (P100) | `SupportTickets` | `ExtendedPlatformControllers.cs` (`/api/customer/tickets`) | `CustomerPortalService.CreateTicketAsync` | NONE | STUB | 85 |
| 4 | Grievances | Communications | `GrievanceTicket`, `GrievanceResponse` | `GrievanceTickets`, `GrievanceResponses` | `CommunicationsController` (`/api/communications/grievances*`) | `Platform/PlatformServices.cs` `GrievanceService` | NONE | SPECIALIZED_ONLY | 85 |
| 5 | Mental-health / counseling | Student Welfare | `MentalHealthTicket`, `CounselorProfile`, `CounselingCaseNote`, `MandatoryReportLog` | `MentalHealthTickets`, `CounselorProfiles`, `CounselingCaseNotes`, `MandatoryReportLogs` | `ExtendedPlatformControllers3` (`/api/mental-health`), `StudentLifecycleModuleEndpoints` (`/api/student/v2/mental-health`) | `StudentLifecycle/MentalHealthService.cs` | `/mental-health` | SPECIALIZED_ONLY (complete, protected) | 95 |
| 6 | Security incidents | Campus security | `SecurityIncident`, `IncidentEvidence`, `IncidentWitness` | `SecurityIncidents`, `IncidentEvidences`, `IncidentWitnesses` | NONE found (only a webhook/event-type string in `DeveloperControllers.cs:161`) | `Application/Security/IncidentReportingService.cs` | NONE | SPECIALIZED_ONLY (unwired) | 80 |
| 7 | Grade appeals | Academics | `GradeAppeal` | `GradeAppeals` | `GradeAppealsController` (`/api/grade-appeals`) | `IGradeAppealService` | `/grade-appeals` | SPECIALIZED_ONLY | 85 |
| 8 | Grade-change approvals | Academics | `MarkModeration` (GRADE_CHANGE:) | `MarkModerations` | `GradeApprovalDiscoveryController`, `S3ExaminationController` | grading/approval services | `/grade-approvals` | SPECIALIZED_ONLY (correct dept-scoped pattern) | 90 |
| 9 | HR leave requests | HR | `LeaveRequest` (via `HrController`) | leaves | `HrController` (`/api/hr/leaves*`) | HR services | NONE | SPECIALIZED_ONLY | 80 |

**Nothing here is a department-agnostic ticket core.** Each system hard-codes its domain.

---

## 3. Source evidence

### 3.1 Orphaned maintenance/facilities ticket core (the big one)
- `AegisCore.Domain/Entities/MaintenanceTicket.cs` — full ticket entity: `TicketNumber, Title, Description, Category(MaintenanceCategory), Priority(TicketPriority), Status(TicketStatus), AssetId, RoomId, BuildingId, CreatedByUserId, CreatedAt, ClosedAt, SLADeadline, IsSLABreached, PhotoUrl, TenantId` + collections `WorkOrders, Comments, Escalations`.
- `AegisCore.Domain/Entities/TicketComment.cs` — `MaintenanceTicketId, Comment, CommentedByUserId, CommentedAt, TenantId`.
- `AegisCore.Domain/Entities/TicketEscalation.cs` — `EscalationLevel, EscalatedToUserId, Reason, IsResolved`.
- `AegisCore.Domain/Entities/SLADefinition.cs` — `Category, Priority, ResponseTimeHours, ResolutionTimeHours, EscalationTimeHours, EscalationLevel, IsActive, TenantId`.
- `AegisCore.Domain/Entities/WorkOrder.cs` — `WorkOrderNumber, AssignedToEmployeeId, AssignedToContractorId, Status, StartedAt/CompletedAt/SignedOffAt, ResolutionNotes, LaborCost/MaterialCost/TotalCost, HoursWorked, IsPreventiveMaintenance, PMScheduleId`.
- `AegisCore.Application/Maintenance/TicketService.cs` — `CreateTicketAsync` (TKT-YYYYMMDD-number, SLA lookup), `AssignTicketAsync`, `ResolveTicketAsync` (marks SLABreached), `CloseTicketAsync`, `AddCommentAsync`, `GetTicketDashboardAsync`.
- `AegisCore.Application/Maintenance/SLAEngineService.cs` — `CreateSLADefinitionAsync`, `CheckSLAComplianceAsync`, `GetSLAReportAsync`.
- `AegisCore.Application/Maintenance/WorkOrderService.cs` — create/start/complete/sign-off work orders.
- Repos exist: `Infrastructure/Repositories/MaintenanceTicketRepository.cs`, `SLADefinitionRepository.cs`, `WorkOrderRepository.cs`, `EquipmentMaintenanceRepository.cs`, `MaintenanceContractorRepository.cs`, `PreventiveMaintenanceScheduleRepository.cs`.
- **NOT REGISTERED / NO CONTROLLER:** `rg "AddScoped<TicketService>|AddScoped<SLAEngine|AddScoped<WorkOrderService|AddScoped<Preventive|MaintenanceTicket" AegisCore.API AegisCore.Infrastructure` → only hits are the Application-layer source files and `Program.cs` line 13 (`using AegisCore.Application.Maintenance;`). `IMaintenanceTicketRepository`/`ISLADefinitionRepository`/`IWorkOrderRepository` are not registered (only the generic `IRepository<>` at `InfrastructureServiceExtensions.cs:57`). **No controller references these services.** The entities are in `AegisDbContext` (lines 632-672) with indexes, so tables exist in migrations — but the feature is dead code.
- Tenant: `MaintenanceTicket.TenantId` present but the class does NOT implement `ITenantScoped` → **no EF global filter** (`rg "ITenantScoped" MaintenanceTicket.cs` → empty). Same for `TicketComment`, `TicketEscalation`, `SLADefinition`, `WorkOrder`, `SecurityIncident`, `GrievanceTicket`, `SupportTicket`. Only `MentalHealthTicket`/`CounselorProfile`/`CounselingCaseNote`/`MandatoryReportLog` implement `ITenantScoped`.

### 3.2 AI support ticketing
- `AegisCore.Infrastructure/Support/SupportTicketService.cs` — raw Npgsql, **creates its own tables at runtime** (`EnsureSchemaAsync` → `CREATE TABLE IF NOT EXISTS support_tickets ...`), LLM first-line answer, critical-term detection, knowledge-base search, escalation on low confidence, CSAT feedback, analytics. Statuses: `open / ai_resolved / escalated / closed`. NOT in EF migrations.
- `AegisCore.API/Controllers/SupportController.cs` — `/api/support/tickets` (create/list/get), `/api/support/tickets/{id}/feedback`, `/api/support/escalations`, `/api/support/escalations/{id}/resolve`, `/api/support/knowledge`, `/api/support/analytics`. Staff check: roles `"Administrator"`, `"VC"`, `"Support"` (note: canonical role set uses `Admin` not `Administrator`, and there is no seeded `Support` role in `ROLE_MATRIX.md` — likely a role-string mismatch, `INFERRED`).
- **Frontend: NONE** (`rg "support" AegisCore.Portal/src` → no ticket page; `/api/support` not consumed).
- Tenant-aware via SQL `WHERE tenant_id = @tenant` (good) but schema is out-of-band from EF (migration drift risk).

### 3.3 Grievances
- `AegisCore.Domain/Entities/CommunicationEntities.cs:20-50` — `GrievanceTicket` (`TicketNumber, SubmitterId, Category, Subject, Description, Status, AssignedTo, Priority, SubmittedAt, ResolvedAt, ResolutionNotes, TenantId`) + `GrievanceResponse` (`ResponseText, StatusUpdate`).
- `AegisCore.Infrastructure/Platform/PlatformServices.cs:183-228` `GrievanceService` — create/respond/list (submitter-filtered). `RespondAsync` updates status + resolves.
- `CommunicationsController` `/api/communications/grievances`, `/grievances/{ticketId}/responses`.
- **Not `ITenantScoped`** (TenantId present but no filter). No assignment queue, no SLA, no attachments, no frontend.

### 3.4 Mental-health (the protected model)
- `AegisCore.Domain/Entities/MentalHealthEntities.cs` — `MentalHealthTicket : ITenantScoped` (`AnonymousToken, Severity, ConcernType, Status, AssignedCounselorId, CrisisProtocolsTriggered, MandatoryReportTriggered`), `CounselorProfile : ITenantScoped` (caseload caps), `CounselingCaseNote : ITenantScoped` (private-to-counselor flag), `MandatoryReportLog : ITenantScoped`.
- `AegisCore.Infrastructure/StudentLifecycle/MentalHealthService.cs` — anonymous submit, crisis keyword detection, routing to counselor by caseload, queue, resolve, crisis re-check, aggregate stats. Note `IgnoreQueryFilters()` used in queue/caseload lookups (`:32, :44, :73`) — cross-tenant counselor reads must be verified (SEC pattern).
- Endpoints: `/api/mental-health/*` + `/api/student/v2/mental-health/*`; frontend `/mental-health` exists (counselor queue + anonymous submit).
- **This is the reference for how to do a specialized ticket system correctly** (tenant-scoped, guarded, role-aware, protected anonymity).

### 3.5 Security incidents
- `AegisCore.Domain/Entities/SecurityIncident.cs`, `IncidentEvidence.cs`, `IncidentWitness.cs`; `Application/Security/IncidentReportingService.cs` (report/assign/resolve/add-witness). **No controller found** (only `security.incident_reported` string at `DeveloperControllers.cs:161`). Orphaned at API layer.

### 3.6 Reusable infrastructure (positive evidence)
- **Workflow engine:** `AegisCore.Infrastructure/Workflow/WorkflowEngine.cs` (`WorkflowDefinition` w/ `NodesJson/TransitionsJson`, `WorkflowInstance`, `WorkflowStep`, `ApprovalService`, `InitializeApprovalsForStep`, SignalR `StateMachineHub`), registered in `WorkflowServiceExtensions.cs`. This is the mechanism to run ticket lifecycle + approval workflows rather than building another engine.
- **Notifications:** `INotificationService` (`Application/Workflow/IWorkflowServices.cs:24-31` — Email/SMS/InApp/`SendAsync(NotificationRecord)`), `EmailNotificationService`, SignalR `NotificationHub` + `SignalRNotificationRealtimePublisher`. WhatsApp via channel switch. (Provider abstraction still monolithic — see `PROVIDER_ADAPTERS.md`.)
- **Storage:** `IStorageService` (`Contracts/Abstractions/Cloud/CloudAbstractions.cs`) + S3/R2/Azure/GCP/on-prem providers; `UploadValidationService` (magic bytes, traversal).
- **Audit:** `IAuditLogService` (`Application/Common/Interfaces/IAuditLogService.cs`, `Infrastructure/Security/AuditLogService.cs`).
- **Config:** `ConfigurationCenterService` (`/api/config`), `FeatureFlagService` w/ tenant override, `TenantConfigService`.
- **Identity/auth:** JWT + Casbin + ABAC (`X-Tenant-Id` header equality) + role attributes.
- **Tenant model:** `ITenantScoped` global filter + `app.tenant_id` GUC — the pattern all ticket entities must follow.

---

## 4. General ticketing readiness

| Capability needed | Existing? | Evidence | Note |
|---|---|---|---|
| Ticket entity with number/status/priority | YES (maintenance model) | `MaintenanceTicket.cs` | unwired; maintenance-specific fields |
| Comments thread | YES | `TicketComment.cs` | unwired |
| Assignment | YES (work order/assignment) | `WorkOrder.cs`, `AssignTicketAsync` | unwired |
| Status lifecycle (Open/Assigned/InProgress/Resolved/Closed/Reopened) | YES | `Enums.cs:667-675` `TicketStatus` | matches desired lifecycle |
| SLA definition + deadline + breach | YES | `SLADefinition.cs`, `SLAEngineService.cs` | unwired; category is `MaintenanceCategory` (facilities-only) |
| Escalation | YES | `TicketEscalation.cs` | unwired |
| Attachments | PARTIAL | `PhotoUrl` on MaintenanceTicket; `IncidentEvidence.FileUrl`; storage abstraction exists | no generic ticket attachment table |
| Categories from tenant config | NO | `MaintenanceCategory` is a hard enum | must become configurable |
| Department/queue config | NO | none | must be configurable |
| Watchers / tags / status history | NO | none | |
| Notifications on ticket events | PARTIAL | `INotificationService` exists | no ticket-event wiring |
| Audit trail | PARTIAL | `IAuditLogService` exists | not wired to tickets |
| Frontend | NO | no ticket/service-desk pages | |
| Agent/queue UI | NO | — | |

**General ticketing readiness: PARTIAL.** The domain primitives exist mostly in the orphaned maintenance layer; the wiring, configurability, and frontend do not.

---

## 5. Reusable components

| Component | Source | Reuse value |
|---|---|---|
| Workflow engine + approval service | `Infrastructure/Workflow/WorkflowEngine.cs`, `ApprovalService.cs` | RUN ticket lifecycle + approval workflows (avoid new engine) |
| Notification service + SignalR | `Application/Workflow/IWorkflowServices.cs`, `NotificationHub`, `SignalRNotificationRealtimePublisher` | ticket-event notifications (refactor to provider abstraction per PROVIDER_ADAPTERS.md) |
| Storage abstraction | `IStorageService` + providers | ticket attachments |
| Upload validation | `UploadValidationService` | attachment security |
| Audit service | `IAuditLogService` | ticket audit trail |
| Tenant config / feature flags | `ConfigurationCenterService`, `FeatureFlagService` | department/category/SLA config |
| AuthZ stack | Casbin + ABAC + roles | ticket permissions + dept boundaries |
| `ITenantScoped` + GUC | `Common/ITenantScoped.cs`, `TenantSessionInterceptor` | ticket tenant isolation (REQUIRED for new entities) |
| Mental-health ticket pattern | `MentalHealthEntities.cs` + `MentalHealthService.cs` | reference for guarded specialized domain |
| Maintenance ticket domain model | `MaintenanceTicket`/`TicketComment`/`TicketEscalation`/`SLADefinition` | direct seed for the unified core (rename/generalize) |

---

## 6. Components that must remain specialized

| System | Why it must stay protected | Constraint |
|---|---|---|
| Mental-health / counseling | anonymity (AnonymousToken), crisis classification, mandatory reporting, restricted visibility, caseload rules | MUST NOT be folded into a generic ticket entity; a generic engine may provide primitives (status/assignment/comments) but crisis/mandatory-report/anonymity stay domain-owned |
| Security incidents | evidence/witness handling, immutable resolution, sensitive | incident lifecycle stays domain-owned |
| Grade appeals + grade-change approvals | academic integrity, HOD dept-scoping, moderation lock semantics | keep bespoke; they are academic workflows, not service-desk |
| AI support ticketing | LLM triage, knowledge base, CSAT | keep as the AI-first channel; may POST into the ticket core later |
| HR leave requests | payroll-sensitive approval | leave remains HR domain |

Rule: a generic ticketing engine may provide **common primitives** while specialized domains keep **additional security/workflow rules**. Never weaken specialized domain rules to reuse the generic entity.

---

## 7. Proposed domain model (AegisCore Service Desk)

Minimum set AegisCore actually needs (NOT a full ITIL model):

```
ServiceTicket                      → "TicketCore" (the unified ticket)
  Id (Guid)                         PK
  TenantId (Guid)                   ITenantScoped REQUIRED
  TicketNumber (string)             SD-YYYYMMDD-XXXXXX
  RequesterId (Guid)                FK → User
  RequesterType (enum)              Student | Faculty | Employee | System
  DepartmentId (Guid?)              owning department/queue (HR, IT, Facilities, Finance, Student Services)
  CategoryId (Guid)                 from configurable TicketCategory
  SubcategoryId (Guid?)             optional
  Subject (string)
  Description (string)
  Priority (enum)                   Critical/High/Normal/Low  (tenant-configurable labels ok)
  Status (enum)                     New/Triaged/Assigned/InProgress/WaitingForRequester/Resolved/Closed/Reopened/Rejected/Cancelled/Escalated
  AssignedTeamId (Guid?)            from configurable TicketTeam
  AssignedUserId (Guid?)            handler
  CreatedAt / UpdatedAt
  DueAt (from SLA policy)
  ResolvedAt / ClosedAt
  Resolution (string?)              resolution notes
  Source (enum)                     Portal | Email | WhatsApp | API | AI | Phone
  Visibility (enum)                 Public | Department | Private
  Confidentiality (enum)            Normal | Confidential | Restricted
  SlaPolicyId (Guid?)
  ParentTicketId (Guid?)            for sub-tickets / linked work
  CorrelationId (Guid?)             dedupe / linked record
```

Related (only what's needed):

| Model | Purpose | Required? |
|---|---|---|
| `TicketComment` | thread (requester ↔ handler), visibility-aware | YES |
| `TicketAttachment` | files via IStorageService; signed URL, audit | YES |
| `TicketStatusHistory` | immutable lifecycle audit | YES (cheap, high value) |
| `TicketAssignmentHistory` | who assigned to whom/when | YES (audit) |
| `TicketWatcher` | subscribe to notifications | OPTIONAL (P3) |
| `TicketTag` | free tagging | OPTIONAL (P3) |
| `TicketCategory` | configurable categories/subcategories per tenant | YES |
| `TicketTeam` | configurable queue teams + members + roles | YES |
| `TicketSlaPolicy` | priority → ack/resolve targets per tenant | YES |
| `TicketEscalation` | escalation records | YES (reuse existing entity, generalize) |
| `TicketApproval` | approval step for workflow-bound tickets | via WorkflowEngine instead |

**Explicitly NOT modeled:** CMDB, asset discovery, full ITIL, change management, problem management, ServiceNow-grade features. Scope = university service desk.

---

## 8. Workflow model

Desired lifecycle:

```
NEW → TRIAGED → ASSIGNED → IN_PROGRESS → WAITING_FOR_REQUESTER → RESOLVED → CLOSED
                ↓              ↓              ↓                     ↑
             REJECTED      ESCALATED      REOPENED              REOPENED
             CANCELLED
```

**Decision: reuse the existing WorkflowEngine.** `WorkflowDefinition` (JSON nodes/transitions, versioned, `Category` field) + `WorkflowInstance`/`WorkflowStep` + `ApprovalService.InitializeApprovalsForStep` + SignalR StateMachineHub already provide configurable state machines with approval steps. The ticket lifecycle becomes a **default WorkflowDefinition per department/category** created from tenant config; the `ServiceTicket.Status` is a denormalized mirror of the workflow state for fast queue queries. Specialized domains (mental-health, incidents, grade appeals) register their own guarded definitions that enforce their rules — the engine is shared, the rules are not.

Approval integration: some tickets need approval (HR salary correction, finance refund, IT privileged access, registrar record correction). Route those through `ApprovalService`/`InitializeApprovalsForStep` with approver lists from the ticket's department/team config. **Do NOT put approval logic in a TicketController** — it belongs in the workflow engine.

---

## 9. Authorization model

Respect existing Casbin + ABAC + role attributes. Required permissions (map to `[RequirePermission]`-style or Casbin policy strings — the unused `[RequirePermission]` infrastructure could finally be wired, or use Casbin rules):

- `ticket.create` — all authenticated users (students/faculty/employees)
- `ticket.read.own` — own tickets (requester)
- `ticket.comment.own` — own tickets
- `ticket.close.own` — requester may close resolved/reopen (policy-configurable)
- `ticket.queue.read` — queue staff (by department)
- `ticket.assign` — queue manager (department-scoped)
- `ticket.update` — assigned handler
- `ticket.resolve` — assigned handler
- `ticket.escalate` — handler/manager
- `ticket.category.manage`, `ticket.sla.manage`, `ticket.admin` — admin

Department boundaries are the security crux:
- **Department-scoped queues**: a handler sees only their department's queue (reuse the proven HOD department-scoping pattern from `GradeApprovalDiscoveryController.cs:72-80`).
- **Confidentiality gating**: `Confidentiality = Confidential` tickets (HR grievances, counseling) are visible ONLY to the assigned team + requester + named approvers. A normal IT technician must NOT see confidential HR grievances.
- **Mental-health protection**: its own service enforces anonymity + crisis + mandatory reporting; the ticket core must never bypass it.
- ABAC `X-Tenant-Id` header equality already enforces tenant; new ticket queries must be `ITenantScoped`-filtered (no `IgnoreQueryFilters`).

---

## 10. Tenant configuration model

Categories, departments/queues, teams, workflows, SLA policies, labels ("Service Desk" vs "Help Center" vs "Campus Support"), and priorities come from tenant configuration / database configuration — NEVER hard-coded per university. This fits the zero-fork manifest (`TENANT_CONFIGURATION.md`): a `service_desk:` section in the tenant manifest defines department queues, categories, teams, SLA policies, and workflow definitions. No university-specific source forks. Ticket entities MUST be `ITenantScoped` (unlike today's maintenance/grievance/support/incident entities which carry TenantId without the filter — this is a known architectural risk, `DATA_MODEL.md` §2).

---

## 11. SLA model

No general reusable SLA engine exists beyond the orphaned `SLAEngineService` (facilities-scoped, `MaintenanceCategory`-keyed). Design conceptually:

- `TicketSlaPolicy`: priority → acknowledgement target (hours) + resolution target (hours) + escalation level/target, per tenant.
- `ServiceTicket.DueAt` derived at create/triage from policy.
- Overdue detection: background sweep (like `SLAEngineService.CheckSLAComplianceAsync`) or DB query on queue read.
- Escalation: on breach, create `TicketEscalation` + notify handler/manager.
- Queue metrics: `avg ack time`, `avg resolution time`, `SLA compliance %`, by department.
- Actual values MUST be tenant-configurable (manifest/SLA table), not constants.

The existing `TicketEscalation`/`SLADefinition` entities can be generalized (add `DepartmentId`, keep category flexible) and the `SLAEngineService` reuse patterns for a periodic sweep.

---

## 12. Notification integration

The ticket domain should emit **notification intent** (events), not contain provider API logic. Recommended event names: `TicketCreated`, `TicketAssigned`, `TicketCommentAdded`, `TicketInformationRequested`, `TicketEscalated`, `TicketResolved`, `TicketClosed`, `TicketReopened`, `TicketSlaWarning`, `TicketSlaBreached`.

Design: a `TicketNotificationService` maps ticket domain events → `NotificationRecord` (via `INotificationService.SendAsync` / channel switch) + SignalR hub push. Keep provider abstraction per `PROVIDER_ADAPTERS.md` (email/WhatsApp/SMS/Teams as adapters behind `INotificationProvider`). Today `INotificationService` is a monolithic channel switch — reuse it now, refactor to providers when the notification work package lands.

---

## 13. Attachment architecture

Reuse `IStorageService` (S3/R2/Azure/GCP/on-prem) + `UploadValidationService`. `TicketAttachment` stores object-key/URL + metadata:
- **Authorization**: only ticket participants (requester, assigned handler/team, department queue, named approvers) can download.
- **Tenant isolation**: bucket/prefix keyed by tenant; never cross-tenant.
- **Size limits**: per attachment (e.g., 10-25MB) + total-per-ticket cap, mirroring existing `[RequestSizeLimit]` practice.
- **Allowed types**: magic-byte validation (reuse `UploadValidationService`); content-type allow-list per ticket category (e.g., HR documents).
- **Malware scanning boundary**: no scanner exists today — recommend an async AV scan hook (vendor service) on a background worker before the attachment becomes available to handlers (design only).
- **Signed URLs**: `IStorageService.GetPresignedUrlAsync` for time-limited download.
- **Retention**: per tenant policy (ticket retention window; purge with audit record).
- **Audit trail**: every upload/download logged via `IAuditLogService`.

---

## 14. HR workflow example

Faculty member creates ticket:
- Department: **HR** · Category: **Payroll / Compensation** · Priority: **Normal** · Subject: "Missing salary increment" · Description: ... · Attachment: salary-slip.pdf
- `TicketCreated` notification → HR team queue.
- HR opens ticket → assigns to payroll officer → `TicketAssigned`.
- Officer comments "request information: latest payslip" → `TicketInformationRequested` → status `WAITING_FOR_REQUESTER`.
- Faculty replies + uploads evidence → `TicketCommentAdded` → status `IN_PROGRESS`.
- Approval path: salary correction needs manager approval → workflow engine `ApprovalService` step (HR manager approves) → `TicketEscalated` only if needed.
- Officer resolves → `TicketResolved` → faculty notified; faculty can reopen within policy → `TicketReopened`.
- Close → `TicketClosed`.
- Every step → `TicketStatusHistory` + `IAuditLogService` (who did what, when, from which department/tenant).

---

## 15. IT workflow example

Faculty: "University email account is locked."
- Department: **IT** · Category: **Identity / Email**.
- Routing is **configuration-driven**: `TicketCategory` → `TicketTeam` mapping in tenant config (category "Identity / Email" → IT queue team), NOT source-code if/else.
- IT queue sees it via `ticket.queue.read` (dept-scoped); technician assigns, un-locks account via Graph (integration boundary, not ticket-core concern), resolves, closes.
- Optional privileged-access requests route to an approval workflow step (workflow engine), e.g., "grant elevated permissions" requires IT manager approval.

---

## 16. Reporting

Built-in analytics (per department + tenant) is sufficient for the pilot; Power BI can consume later via the same metrics surface:
- open/closed counts, backlog, aging buckets
- average response time, average resolution time
- SLA compliance %, SLA breaches
- tickets by department / category / priority / status
- reopened tickets, assignee workload
- CSAT (if feedback added)

Implement as SQL aggregates in a `TicketAnalyticsService` (mirror the pattern in `SupportTicketService.GetAnalyticsAsync` and finance dashboards) — no separate analytics engine needed. Expose via `/api/service-desk/reports/*`; later a BI dataset can wrap the same queries.

---

## 17. Frontend architecture

No reusable ticket UI exists today (the only ticket frontend is `/mental-health`). Recommended routes (names advisory):

- `/service-desk` — requester landing (create + my tickets)
- `/service-desk/new` — create ticket (category/subcategory/priority/description/attachments)
- `/service-desk/my` — my tickets (status, reopen, comment)
- `/service-desk/tickets/[id]` — detail (thread, attachments, history, SLA)
- staff:
  - `/service-desk/queue` — department queue
  - `/service-desk/assigned` — my assigned
  - `/service-desk/reports` — management metrics
  - `/service-desk/configuration` — categories/teams/SLA (admin)

Reuse existing portal patterns (AppShell, `api-client.ts`, existing detail/queue pages like `/grade-approvals` as a queue UI reference, `/mental-health` as a ticket-detail reference). Client-side role gating is cosmetic; backend Casbin/ABAC enforces.

---

## 18. Migration / consolidation strategy

Decision: **C = share common infrastructure now; migrate specialized systems onto a common ticket core later where it preserves their rules.**

- **Now (P1/P2):** build the Unified Ticket Core as a NEW module (`ServiceDesk`). Do not rewrite mental-health, incidents, grade appeals, AI support. They remain specialized. The core coexists.
- **Consolidation targets (P3+):** maintenance/facilities ticketing is a candidate to migrate onto the core (its model is a near-superset; the orphaned services would be replaced by the core + tenant-config categories). AI support can POST escalations into the core as `Source = AI`. Grievances can be expressed as a service-desk category (HR/Student Affairs grievance) with confidentiality gating.
- **Never-consolidate:** mental-health (crisis/anonymity/mandatory-reporting), security incidents (evidence/immutability), grade appeals (academic integrity), HR leave/payroll approvals.
- Migration is config/data work, NOT a fork: each consolidated system maps its category/status onto ticket-core configuration.

---

## 19. Pilot MVP

**PILOT PRIORITY: P2** (during pilot) — unified ticketing is NOT a 120-day-pilot blocker; the pilot can run on existing workflows (grade approvals, mental-health, support) plus admin-provisioned fixes. But it IS a fast-follow with high value (faculty/student facing).

**Minimum pilot service desk (P2 scope):**
- `ServiceTicket` + `TicketComment` + `TicketAttachment` + `TicketStatusHistory` + `TicketAssignmentHistory` + `TicketCategory` + `TicketTeam` (all `ITenantScoped`).
- 3-4 configured departments/queues: HR, IT, Facilities, Student Services (categories from tenant config).
- Lifecycle NEW→TRIAGED→ASSIGNED→IN_PROGRESS→WAITING_FOR_REQUESTER→RESOLVED→CLOSED (+REOPENED/REJECTED/CANCELLED).
- Create/comment/attach/assign/update/resolve/close/reopen; requester self-service view.
- SLA policy (ack/resolve/due) + overdue flag; simple queue metrics.
- Notifications on the key events (create/assign/comment/resolve/close) via existing `INotificationService`.
- Frontend: `/service-desk`, `/service-desk/new`, `/service-desk/my`, `/service-desk/tickets/[id]`, `/service-desk/queue`.
- Audit trail via `IAuditLogService` + status history.

**Explicitly out of pilot MVP:** full ITSM, CMDB, change management, problem management, watchers/tags, Power BI dataset, Teams deep integration, malware scanner (add scanner as a separate security work item, not pilot-blocking).

---

## 20. Full future capability

- All departments/categories/queues via tenant config (HR, IT, Finance, Accounts, Examinations, Registrar, Admissions, Student Affairs, Transport, Library, Hostel, Security, Medical, Facilities, Maintenance, Procurement, Cafeteria, Academic Dept, OBE — configurable, NOT hard-coded).
- Approval workflows bound via WorkflowEngine (salary correction, fee correction/refund, privileged access, record corrections).
- SLA + escalation engine generalized; queue metrics + Power BI dataset.
- Consolidation of maintenance tickets + grievances + AI-support escalations onto the core (source = channel).
- Email-to-ticket (ingress), WhatsApp/SMS request intake, tenant-configurable labels ("Service Desk" / "Help Center" / "Campus Support").
- Watch/tags, CSAT, knowledge-base tie-in, SLA warning/breach notifications.

---

## 21. Implementation work packages

| ID | PRIORITY | OBJECTIVE | SOURCE FILES LIKELY AFFECTED | NEW FILES LIKELY REQUIRED | BACKEND WORK | FRONTEND WORK | DATABASE WORK | AUTHORIZATION WORK | SECURITY WORK | TESTS | DEPENDENCIES | ACCEPTANCE CRITERIA | COMPLEXITY |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| SD-01 | P2 | Core ticket entities + DbContext + migration | `AegisDbContext.cs`, `Domain/Entities/*`, `Common/ITenantScoped.cs` | `Domain/Entities/ServiceTicket.cs`, `TicketCategory.cs`, `TicketTeam.cs`, `TicketAttachment.cs`, `TicketStatusHistory.cs`, `TicketAssignmentHistory.cs` | entity + mapping + indexes | — | EF migration; all `ITenantScoped`; tenant index | — | tenant isolation (no `IgnoreQueryFilters`) | EF tests (tenant filter applied) | none | Tables created; tenant filter enforced; indexes on status/department/due | L |
| SD-02 | P2 | Ticket service + lifecycle + SLA target | `Infrastructure` module | `Application/ServiceDesk/*`, `Infrastructure/ServiceDesk/TicketService.cs`, `SlaService.cs` | create/triage/assign/update/resolve/close/reopen; due-at; status+assignment history writes | — | — | — | validation | unit + integration | SD-01 | Lifecycle transitions enforced; history recorded; SLA due computed | L |
| SD-03 | P2 | Comments + attachments | `IStorageService`, `UploadValidationService` | `Infrastructure/ServiceDesk/TicketAttachmentService.cs` | comment thread; attachment upload/download/signed-URL | — | TicketAttachment | participant-only download | magic bytes; size; tenant bucket | tests | SD-01, SD-02, storage | Attach/list/download works; non-participant blocked | M |
| SD-04 | P2 | Category/team/SLA configuration API | `ConfigCenter`, `FeatureFlag` | `Application/ServiceDesk/ServiceDeskConfigurationService.cs` | CRUD categories/subcats/teams/SLA policies per tenant | config screens | tables | `ticket.category.manage`/`sla.manage` | — | tests | SD-01 | Admin configures departments/categories/SLA; config persists per tenant | M |
| SD-05 | P2 | Workflow-engine integration for lifecycle + approvals | `Workflow/WorkflowEngine.cs`, `ApprovalService.cs` | `Infrastructure/ServiceDesk/ServiceDeskWorkflowAdapter.cs` | default lifecycle definition per category; approval steps for approval-bound tickets | — | — | approver lists from team config | no bypass | workflow tests | SD-02, existing workflow | Ticket lifecycle runs on WorkflowEngine; approval tickets require approval to resolve | L |
| SD-06 | P2 | Notification events | `INotificationService`, `NotificationHub` | `Infrastructure/ServiceDesk/TicketNotificationService.cs` | emit events → NotificationRecord + hub push | in-app read UI | — | — | — | tests | SD-02 | Create/assign/comment/resolve/close notify participants; SignalR delivered | M |
| SD-07 | P2 | Queue + analytics endpoints | — | `Application/ServiceDesk/*`, controllers | dept-scoped queue; SLA metrics; backlog/aging | — | — | `ticket.queue.read` dept-scope | dept isolation | tests | SD-02 | Queue shows only own dept; metrics correct | M |
| SD-08 | P2 | Portal routes | existing pages as reference | `/service-desk*` pages, components | — | requester + queue UI | — | client gating (backend enforces) | — | Playwright smoke | SD-02..07 | Requesters create/view/comment; staff queue/assign/resolve | L |
| SD-09 | P2 | Audit trail wiring | `IAuditLogService` | in SD-02 service | log all mutations | — | — | — | audit completeness | tests | SD-02 | every mutation audited with actor/tenant/action | S |
| SD-10 | P3 | Generalize orphaned maintenance/SLA onto core | `Maintenance/*` orphaned services, `SLADefinition` | migration mapping config | retire orphan services; map facilities categories | facilities queue | data migration | — | tenant filter fixes | regression | SD-04 | Facilities ticketing runs on core; old orphan code removed | XL |
| SD-11 | P3 | Escalation engine (generalize `TicketEscalation`) | `TicketEscalation.cs`, `SLAEngineService.cs` | `Infrastructure/ServiceDesk/TicketEscalationService.cs` | periodic sweep → breach/escalation + notifications | escalation view | generalize entity | handler/manager notify | — | tests | SD-05, SD-06 | overdue → escalation created + notified | M |
| SD-12 | P3 | AI-support → ticket-core bridge | `SupportTicketService` | bridge adapter | escalation POST into core (Source=AI) | — | link tables | staff scope | — | tests | SD-02 | AI escalations appear as core tickets | M |
| SD-13 | P3 | Watch/tags + CSAT + BI dataset | — | — | watchers, tags, feedback; analytics dataset | watcher/feedback UI | — | — | — | tests | SD-07 | optional polish complete | M |

---

## 22. Risks

| Risk | Detail | Mitigation |
|---|---|---|
| Tenant-isolation gap | New ticket entities MUST be `ITenantScoped`; existing ticket-ish entities (maintenance/grievance/support/incident) are NOT filtered (`DATA_MODEL.md` §2) | All new entities implement `ITenantScoped`; audit existing `IgnoreQueryFilters()` in mental-health/counselor paths (`MentalHealthService.cs:32,44,73`) |
| Role-string mismatch | `SupportController` checks `"Administrator"`/`"Support"`; canonical roles use `Admin`/no `Support` (`ROLE_MATRIX.md`) | Align staff roles with canonical role set; document |
| Workflow-engine complexity | Reusing WorkflowEngine for ticket lifecycle adds indirection vs simple state enum | Use engine for lifecycle+approvals as designed; denormalize status for fast queues |
| Mental-health rule erosion | Generic ticket must never bypass crisis/anonymity/mandatory-report | Specialized domain stays authoritative; core is advisory |
| Department boundary leakage | An IT tech seeing confidential HR grievances | Confidentiality gating + dept-scoped queue policies + tests |
| Schema drift | AI-support uses runtime-created raw tables (not EF) | Keep separate; migration path = bridge adapter (SD-12), not merge |
| Attachments abuse | Parser/malware surface | UploadValidationService + size caps + optional async AV scan (security work item) |
| Over-building | Risk of building ServiceNow | Scope locked to pilot MVP (SD-01..09); P3 items gated |

---

## 23. Acceptance criteria (pilot MVP)

1. A faculty member can create an HR ticket with category/priority/description/attachment; it lands in the HR queue.
2. HR staff can open, assign, comment, request information, attach, escalate, resolve, close; requester is notified at each step.
3. Requester sees own tickets + status + permitted history; can reply, upload evidence, and reopen within policy.
4. Department queues are isolated; a normal IT technician cannot see HR-confidential or mental-health tickets.
5. SLA due dates are computed from tenant-configured policies; overdue is flagged; escalation works.
6. Every mutation is recorded in `TicketStatusHistory` + `IAuditLogService` with tenant + actor.
7. All ticket entities are `ITenantScoped`; no cross-tenant read reproducible in a staging test.
8. Categories/teams/SLA policies are configured per tenant (no source fork); labels configurable ("Service Desk"/"Help Center"/"Campus Support").
9. Mental-health, security incidents, grade appeals, AI support remain operational and un-weakened.
10. Reports (queue, SLA compliance, backlog, aging) return correct numbers via SQL aggregates.

---

## 24. Evidence index

- `AegisCore.Domain/Entities/MaintenanceTicket.cs`, `TicketComment.cs`, `TicketEscalation.cs`, `SLADefinition.cs`, `WorkOrder.cs`
- `AegisCore.Application/Maintenance/TicketService.cs`, `SLAEngineService.cs`, `WorkOrderService.cs`
- `AegisCore.Infrastructure/Repositories/{MaintenanceTicket,SLADefinition,WorkOrder}Repository.cs`
- `AegisCore.Infrastructure/Support/SupportTicketService.cs` (+ runtime schema)
- `AegisCore.API/Controllers/SupportController.cs`
- `AegisCore.Domain/Entities/CommunicationEntities.cs` (GrievanceTicket/GrievanceResponse)
- `AegisCore.Infrastructure/Platform/PlatformServices.cs:183-228` (GrievanceService)
- `AegisCore.Domain/Entities/MentalHealthEntities.cs`; `AegisCore.Infrastructure/StudentLifecycle/MentalHealthService.cs`
- `AegisCore.Domain/Entities/SecurityIncident.cs`, `IncidentEvidence.cs`; `Application/Security/IncidentReportingService.cs`
- `AegisCore.Domain/Enums.cs:646-702` (MaintenanceCategory/TicketPriority/TicketStatus/WorkOrderStatus/EscalationLevel)
- `AegisCore.Infrastructure/AegisDbContext.cs` (DbSets 632-672, 713, 1338, 1541, 1671; GUC/filter 1949-1962)
- `AegisCore.Infrastructure/Workflow/WorkflowEngine.cs`, `ApprovalService.cs`
- `AegisCore.Application/Workflow/IWorkflowServices.cs:24-31` (INotificationService)
- `AegisCore.Contracts/Abstractions/Cloud/CloudAbstractions.cs` (IStorageService)
- `AegisCore.Application/Common/Interfaces/IAuditLogService.cs`
- `AegisCore.API/Controllers/CommunicationsController.cs` (grievances), `GradeAppealsController.cs`, `GradeApprovalDiscoveryController.cs` (dept-scope pattern)
- `AegisCore.Portal/src/app/mental-health/page.tsx`, `/grade-appeals`, `/grade-approvals` (frontend references)
