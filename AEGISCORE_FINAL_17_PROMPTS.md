# AegisCore — Final Deployment Fix Prompts
# 17 Prompts · Hallucination-Proof · Run In Order
# Generated: 2026-08-31
# Based on: Deployment Readiness Audit (commit 950a4043, branch main)

---

## EXECUTION ORDER AT A GLANCE

| When | Prompt | Item | Est. Hours |
|---|---|---|---|
| Hour 1–2 | P1 | Fix 95 compile errors | 16–28h |
| Hour 2–3 | P2 | Verify CI gate | 2h |
| Hour 3 | P10 | Delete duplicate backend | 2h |
| Day 1–3 | P3A | Extract full tenant entity lists | 2h |
| Day 1–3 | P3B | Apply tenant isolation fixes | 90–140h |
| Day 1 | P5 | Wire AddEnvironmentSubstitution() | 1h |
| Week 1 | P4 | Audit LINQ for tenant leaks | 8–12h |
| Week 1 | P6 | Full env var registry | 20–28h |
| Week 1 | P7 | Persist wizard state to DB | 48–70h |
| Week 1 | P8 | Consolidate + secure wizard | 32–48h |
| Week 2 | P9 | Build missing wizard steps | 80–120h |
| Week 2 | P12 | Input validators (top 30) | 60–90h |
| Week 2 | P13 | Dynamic RBAC | 56–80h |
| Week 2–3 | P14 | Frontend UX polish | 70–100h |
| Week 2–3 | P15 | Geolocation attendance | 60–85h |
| After demo | P16 | FERPA + GDPR compliance | 120–175h |
| After demo | P17 | WCAG 2.1 AA accessibility | 60–90h |
| Last of all | P11 | Multi-dialect database | 160–260h |

**Demo-ready subset (P1–P15):** ~530–810h · 7–10 weeks · 2 engineers
**Full Banner-competitive:** ~1,048–1,579h · 3–4 months · 3 engineers

---

---

# PHASE 1 — RESTORE THE BUILD
# Do these before anything else. Nothing is testable until P1 lands.

---

## Prompt 1 — Fix C0: The 95 Compile Errors

```
You are a C# senior engineer. I have an ERP codebase that fails
to build with 95 compile errors spread across three files.
Fix them all mechanically.

REPOSITORY ROOT: (provide path)

FILES TO FIX:
- AegisCore.Infrastructure/Deployment/DemoDataSeeder.cs        (55 errors)
- AegisCore.Infrastructure/Platform/ExtendedPlatformServices3.cs (23 errors)
- AegisCore.Infrastructure/Platform/ExtendedPlatformServices4.cs (17 errors)

No other project has its own errors — they fail only because
AegisCore.Infrastructure does.

ERROR CLASSES AND FIXES:

CS0117 (54×) — missing member in object initialiser
  Cause: properties referenced in seeders/services that do not
  exist on the current entity definition.
  Missing properties include (non-exhaustive — scan for all):
    ElectionResult.Status
    BoardAlert.IsRead
    MoraSubmission.Semester, StudentCount, IsSubmitted
    ComplianceBreach.RegistryId
    MentalHealthTicket.CrisisLevel, Description,
      ResponseDeadline, Resolution, ClosedAt
    WellnessCheckIn.TicketId
    DeansListEntry.Semester
    LeaderboardEntry.ScopeValue, Semester
    VisitingFacultyProfile.VettingStatus, HECEligible
    CashFlowForecast.Semester
  Fix strategy per case:
    Option A — add the missing property to the domain entity
               (correct if the field belongs in the schema)
    Option B — remove the initialiser line from the seeder
               (correct if it is demo data that shouldn't exist)
  Choose Option A for schema fields, Option B for pure demo noise.
  Document your choice per property.

CS0029 (18×) — enum assigned to string without conversion
  Fix: add .ToString() on the right-hand side.
  Example: Status = MyEnum.Active → Status = MyEnum.Active.ToString()

CS1061 (13×) — missing member or extension method
  Fix: same as CS0117 — add property or remove the call.

CS0664 (6×) — double literal assigned to decimal field
  Fix: add M suffix.
  Example: Amount = 100.0 → Amount = 100.0M

CS0266 (2×) — missing explicit cast
  Fix: add explicit cast.
  Example: (int)someDouble or (decimal)someDouble

CS0019 (2×) — bad operand types (string == enum)
  Fix: string == enum.ToString()

PROCESS:
1. Open each file. Find every compiler error by line number.
2. For each error: state the error code, the line, the fix,
   and your reasoning (Option A or B for CS0117).
3. Output the complete fixed file content for all three files.
4. After fixes: run `dotnet build AegisCore.Infrastructure.csproj
   -c Release` mentally and confirm zero errors remain.

CONSTRAINTS:
- Do NOT refactor anything beyond the mechanical fix.
- Do NOT rename properties, change access modifiers, or
  restructure classes.
- Do NOT touch any file outside the three listed above.
- The stale build.log in the repo root claims success — ignore it.
  It is from a Docker build, not a dotnet build.
```

---

## Prompt 2 — Verify Build & Enable CI Gate

```
You are a DevOps engineer. The 95 compile errors from P1 are now
fixed. I need to make the CI pipeline actually enforce the build.

REPOSITORY: (provide path)
CI FILE: .github/workflows/ci.yml

CURRENT STATE:
- ci.yml already runs: dotnet build AegisCore.slnx --configuration Release
- But main branch is currently RED and PRs are being merged anyway.
- build.log in repo root is a stale Docker build log showing false success.

TASK 1 — Make CI a hard gate:
Review ci.yml. Ensure:
  - The build step uses: dotnet build AegisCore.slnx -c Release
  - The step has: continue-on-error: false (or is absent, which defaults to false)
  - The job is in the required status checks for main branch
    (output instructions for setting this in GitHub branch protection rules)

TASK 2 — Delete the stale build.log:
  git rm build.log
  Explain why it must never be committed again.

TASK 3 — Add a pre-commit hook:
  Create .githooks/pre-commit with:
  #!/bin/bash
  echo "Running build check..."
  dotnet build AegisCore.slnx -c Release --no-restore -v quiet
  if [ $? -ne 0 ]; then
    echo "Build failed. Fix compile errors before committing."
    exit 1
  fi
  Add setup instructions to README.md:
  git config core.hooksPath .githooks

TASK 4 — Document the build command:
  Add to README.md under "Development Setup":
  ## Building
  dotnet build AegisCore.slnx -c Release
  # Must succeed with 0 errors before any PR is merged.

Output: updated ci.yml, pre-commit hook file, README additions,
GitHub branch protection instructions.
```

---

## Prompt 10 — Fix C6: Delete the Duplicate Backend
## ⚠️ Run this IMMEDIATELY after P2 — before anyone patches the wrong tree.

```
You are a senior engineer. There is a duplicate copy of the entire
backend codebase that must be deleted before any further work proceeds.

CANONICAL SOLUTION: ./AegisCore.slnx (root of repository)
DUPLICATE TO DELETE: aegiscore-backend/ (581 tracked files)

EVIDENCE OF DUPLICATION:
- .csproj files are byte-identical between the two trees
- Program.cs differs: root is 7040889e…, copy is 059178a0…
- aegiscore-backend/ is missing AegisCore.LedgerVerifier entirely
- CI (.github/workflows/ci.yml) and Dockerfile must reference only root

TASK 1 — Pre-delete verification checklist:
Confirm the following before deleting anything:
  [ ] .github/workflows/ci.yml references AegisCore.slnx (root), not
      aegiscore-backend/AegisCore.slnx
  [ ] Dockerfile references the root solution path
  [ ] docker-compose.yml and docker-compose.prod.yml reference root
  [ ] k8s/ manifests reference root
  [ ] No active branch has aegiscore-backend/ as its working tree
If any check fails: fix it BEFORE deleting.

TASK 2 — Delete the duplicate:
  git rm -r aegiscore-backend/
  git commit -m "Remove duplicate backend tree (aegiscore-backend/);
    canonical tree is ./AegisCore.slnx"

TASK 3 — Post-delete verification:
  [ ] dotnet build AegisCore.slnx -c Release still succeeds
  [ ] CI passes
  [ ] Docker build still works
  [ ] No import or reference in remaining code points to
      aegiscore-backend/

Output: pre-delete checklist results, git commands, post-delete
verification results.
```

---

---

# PHASE 2 — TENANT ISOLATION
# Run P3A first. Feed its output into P3B. Do not skip 3A.

---

## Prompt 3A — Extract Full Entity Lists (No Fixes — Discovery Only)

```
You are a C# senior engineer auditing a multi-tenant ERP codebase.

Your ONLY job in this step is to produce two lists.
Do NOT fix anything. Do NOT suggest anything. Just list.

FILE TO READ: AegisCore.Infrastructure/AegisDbContext.cs
ENTITY FOLDER: AegisCore.Domain/Entities/
INTERFACE: AegisCore.Domain/Interfaces/ITenantScoped.cs

TASK:

LIST 1 — "Has TenantId column BUT does NOT implement ITenantScoped"
For every DbSet<T> in AegisDbContext.cs:
  - Open the entity class file in AegisCore.Domain/Entities/
  - Does the class have: public Guid TenantId { get; set; } ?
  - Does the class declaration include : ITenantScoped ?
  - If TenantId EXISTS but ITenantScoped is ABSENT → List 1

LIST 2 — "Has NO TenantId column at all"
  - If TenantId property is completely absent → List 2

Audit-confirmed members of List 2 (verify these and find the rest):
  Attendance, AdmissionApplication, CourseGrade, SsoSession,
  ExamSeating, GradeOverrideRequest, SystemConfiguration,
  MeritListEntry, Semester, AssessmentScheme
  (audit says ~65 total — find all of them)

OUTPUT FORMAT — plain lists only, no prose:

LIST 1 — needs ITenantScoped interface:
EntityName | File path | Namespace

LIST 2 — needs TenantId column + interface:
EntityName | File path | Namespace

Total count for List 1: N
Total count for List 2: N
Grand total: N

Stop here. Do not proceed to fixes.
```

---

## Prompt 3B — Apply Tenant Isolation Fixes
## Feed Prompt 3A's output into this prompt before running.

```
You are a C# senior engineer fixing multi-tenant isolation in an ERP.

CONTEXT:
The global query filter in AegisDbContext.cs works as follows:
  modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantId)
This filter only activates if T implements ITenantScoped.
Adding the interface is sufficient to activate it — do not touch
AegisDbContext.cs.

INTERFACE LOCATION: AegisCore.Domain/Interfaces/ITenantScoped.cs
ENTITY FOLDER: AegisCore.Domain/Entities/
MIGRATIONS FOLDER: AegisCore.Infrastructure/Migrations/

INPUT — List 1 (has TenantId, missing ITenantScoped):
[PASTE FULL LIST 1 FROM PROMPT 3A OUTPUT HERE]

INPUT — List 2 (no TenantId at all):
[PASTE FULL LIST 2 FROM PROMPT 3A OUTPUT HERE]

---

TASK A — Fix List 1 entities:
For each entity in List 1:
  Add : ITenantScoped to the class declaration only.
  Output exact before/after diff for each file.
  Do not touch anything else in the file.

TASK B — Fix List 2 entities:
For each entity in List 2:
  Step 1: Add to entity class:
    public Guid TenantId { get; set; }
    And add : ITenantScoped to the declaration.
  Step 2: Generate EF Core migration named:
    AddTenantId_<EntityName>
    Migration must:
      - Add TenantId column (non-nullable Guid)
      - Add index: CREATE INDEX ON <table> (TenantId)
      - NOT set a default value
      - Include a comment: DATA MIGRATION REQUIRED —
        populate TenantId from known tenant before running
        this migration in production.
  Output: entity diff + full migration file per entity.

TASK C — Build-failing unit test:
File: AegisCore.Tests/Infrastructure/TenantIsolationTest.cs

[Fact]
public void AllDbSets_MustImplement_ITenantScoped()
{
    var dbSetTypes = typeof(AegisDbContext)
        .GetProperties()
        .Where(p => p.PropertyType.IsGenericType &&
               p.PropertyType.GetGenericTypeDefinition()
               == typeof(DbSet<>))
        .Select(p => p.PropertyType.GetGenericArguments()[0])
        .ToList();

    var failures = dbSetTypes
        .Where(t => !typeof(ITenantScoped).IsAssignableFrom(t))
        .Select(t => t.Name)
        .ToList();

    Assert.True(
        failures.Count == 0,
        $"The following entities are not ITenantScoped and " +
        $"will leak cross-tenant data: " +
        $"{string.Join(", ", failures)}. " +
        $"Add ITenantScoped before merging."
    );
}

Add this test to CI as a required check.

TASK D — Output a completion checklist:
| Entity | File | List | Fix Applied | Migration Name | Data Migration Needed |
One row per entity. Mark each ✅ or ❌.

CONSTRAINTS:
- Do not rename any properties.
- Do not change any other fields.
- Do not alter AegisDbContext.cs.
- If an entity has a nullable TenantId risk (existing rows),
  flag it in the checklist but still apply the fix.
```

---

## Prompt 4 — Audit LINQ Queries for Tenant Leaks

```
You are a C# senior engineer. Tenant isolation (ITenantScoped on all
entities) is now in place from P3B. I need to verify no query
bypasses the global EF filter.

CODEBASE: AegisCore.Infrastructure/ and AegisCore.Application/

TASK 1 — Find FromSqlRaw / ExecuteSqlRaw calls:
Scan for all .FromSqlRaw() and .ExecuteSqlRaw() calls.
These bypass EF global filters entirely.
For each:
  - Show the file, line, and SQL string
  - Determine if it accesses tenant-scoped data
  - If yes: either rewrite using LINQ (preferred) or add an
    explicit WHERE TenantId = @tenantId parameter

TASK 2 — Find Dapper queries:
Scan for IDbConnection, SqlConnection, NpgsqlConnection used
directly (not via EF). These bypass all filters.
For each: add explicit TenantId WHERE clause.

TASK 3 — Find AsNoTracking usage on sensitive entities:
AsNoTracking() does NOT bypass global filters, but flag any
usage on: Attendance, CourseGrade, SsoSession, AdmissionApplication
as high-risk for review.

TASK 4 — Find .IgnoreQueryFilters() calls:
Any call to .IgnoreQueryFilters() is an intentional filter bypass.
List every one. Each needs a comment explaining why it is safe
(e.g., super-admin cross-tenant reporting). If no comment exists,
add one or remove the bypass.

OUTPUT:
- Table of all raw SQL calls: File | Line | SQL | Tenant-safe?
- Table of all Dapper calls: File | Line | Fixed?
- Table of IgnoreQueryFilters: File | Line | Justification
- List of recommended rewrites with before/after code
```

---

---

# PHASE 3 — ENVIRONMENT & CONFIGURATION

---

## Prompt 5 — Fix C3: Wire AddEnvironmentSubstitution()
## One line. Highest fix-to-impact ratio in the entire audit.

```
You are a C# senior engineer. I have a critical configuration bug.

PROBLEM:
AddEnvironmentSubstitution() is correctly implemented but never called.
Result: every ${VAR} in appsettings.Production.json is delivered
as a literal string to services at runtime.

Confirmed broken consumers (partial list):
  EmailNotificationService reads SmtpClient("${SMTP_HOST}", ...)
  → connects to the literal string "${SMTP_HOST}"
  PlatformServices.cs:124,152 — same issue
  Twilio, WhatsApp, Elasticsearch, Raast, AI endpoints — all broken

EXTENSION METHOD LOCATION:
  AegisCore.Infrastructure/Configuration/EnvironmentSubstitutionExtensions.cs:17

TASK 1 — Wire the call:
In AegisCore.API/Program.cs, BEFORE builder.Build(), add:
  builder.Configuration.AddEnvironmentSubstitution();
Output the exact diff showing where in Program.cs this line goes.
It must be before builder.Build() and after all other
configuration sources are added.

TASK 2 — Delete the defensive guards:
Scan the entire codebase for:
  .StartsWith("${")
  .Contains("${")
  used as a guard before using a config value.
These are now dead code. List every occurrence with file + line.
Delete each one. Output the diff per file.
Expected count: approximately 14 occurrences.

TASK 3 — Verify resolution:
For each of these config keys, confirm the resolved value
will be the environment variable, not the placeholder:
  SMTP_HOST, SMTP_PORT, SMTP_FROM
  TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN
  ELASTICSEARCH_URL
  OLLAMA_BASE_URL, OPENAI_API_KEY
  RAAST_ENDPOINT

TASK 4 — Write an integration test:
  Test: EnvironmentSubstitutionTest
  Set environment variable: TEST_VAR = "hello-resolved"
  Set in config: "TestKey": "${TEST_VAR}"
  Call AddEnvironmentSubstitution()
  Assert: configuration["TestKey"] == "hello-resolved"

Output: Program.cs diff, list of deleted guards, integration test.
```

---

## Prompt 6 — Implement Full Environment Variable Registry (C9)

```
You are a C# senior engineer. The startup readiness checker only
validates 3 environment variables. Production needs ~32.
Missing vars currently cause silent degraded mode, not a crash.

CURRENT STATE:
  DeploymentReadinessChecker.cs:194-206 — CheckRequiredEnvVars
  checks only 3 variables.
  DeploymentReadinessChecker.cs:152-167 — on failure, logs
  LogCritical then CONTINUES unless Deployment:FailOnReadinessError
  is true — but this key appears in NO settings file anywhere.

REQUIRED ENVIRONMENT VARIABLE REGISTRY:

Database (5):
  DB_HOST         string   required
  DB_PORT         int      required
  DB_NAME         string   required
  DB_USER         string   required   (currently DB_USERNAME — align naming)
  DB_PASS         string   required   (currently DB_PASSWORD — align naming)

Auth / SSO (5):
  SSO_PROVIDER      string  optional  (okta|entra|google|saml|local)
  SSO_CLIENT_ID     string  optional  (required if SSO_PROVIDER != local)
  SSO_CLIENT_SECRET string  optional  (required if SSO_PROVIDER != local)
  SSO_TENANT_ID     string  optional  (required if SSO_PROVIDER = entra)
  SSO_CALLBACK_URL  string  optional  (required if SSO_PROVIDER != local)

AI / Helpbot (4):
  AI_PROVIDER   string  required  (ollama|openai|granite|custom)
  AI_ENDPOINT   string  required
  AI_MODEL      string  required
  AI_API_KEY    string  optional  (required if AI_PROVIDER != ollama-local)

Security / PQC (4):
  PQC_MLKEM_PUBLIC_KEY   string  required
  PQC_MLKEM_PRIVATE_KEY  string  required
  PQC_MLDSA_PUBLIC_KEY   string  required
  PQC_MLDSA_PRIVATE_KEY  string  required

Cloudflare (2):
  CF_TURNSTILE_SITE_KEY    string  required
  CF_TURNSTILE_SECRET_KEY  string  required

App (6):
  APP_URL           string  required
  APP_ENV           string  required  (Production|Staging|Development)
  APP_SECRET        string  required
  SESSION_LIFETIME  int     required  (seconds — currently hardcoded 900)
  AEGISCORE_SETUP_TOKEN  string  required  (wizard bootstrap token)
  AEGISCORE_ENCRYPTION_KEY  string  required

TASK 1 — Create IEnvironmentValidator:
  Interface with:
    ValidationResult Validate();
    IReadOnlyList<string> GetMissingRequired();
    IReadOnlyList<string> GetMissingConditional(string condition);

TASK 2 — Implement the registry:
  Build a registry of all variables above with:
    Name, Category, Type, IsRequired, ConditionalOn (nullable)
  ConditionalOn example:
    SSO_CLIENT_ID is required when SSO_PROVIDER != "local"

TASK 3 — Hard startup failure:
  In DeploymentReadinessChecker.cs:
    Default Deployment:FailOnReadinessError to TRUE in Production.
    On any missing required variable:
      Log: "STARTUP BLOCKED: Required environment variable
            {NAME} is not set. Set it before starting AegisCore."
      Throw ApplicationException to halt startup.
      Do not continue in degraded mode.

TASK 4 — Rename divergent variables:
  Code currently uses DB_USERNAME — rename to DB_USER throughout.
  Code currently uses DB_PASSWORD — rename to DB_PASS throughout.
  AI_ENDPOINT split as OLLAMA_BASE_URL / AI:OpenAI:BaseUrl
    — consolidate to AI_ENDPOINT throughout.
  Output: search-and-replace list for all renamed vars.

TASK 5 — Generate .env.example:
  Complete .env.example file with every variable, its type,
  whether required or optional, and a safe placeholder value.
  No real secrets. Format:
    # Category
    VAR_NAME=example-value  # required | optional | conditional

TASK 6 — Generate Kubernetes secret manifest:
  k8s/secrets.yaml with all required vars as secret keys.
  Values as base64-encoded placeholders.
  Include instructions for sealing with Sealed Secrets or
  equivalent.

Output: IEnvironmentValidator, registry implementation,
updated DeploymentReadinessChecker, .env.example,
k8s/secrets.yaml, variable rename list.
```

---

---

# PHASE 4 — WIZARD CORRECTNESS

---

## Prompt 7 — Fix C4: Persist Wizard State to Database

```
You are a C# senior engineer. The wizard setup flow loses all
state on restart because it uses an in-memory singleton.

CURRENT BROKEN ARCHITECTURE:
  File: AegisCore.Application/Onboarding/WizardStateService.cs:21
  Registered: TryAddSingleton in OnboardingServiceExtensions.cs:12-13
  State held in memory: license key, DB credentials, Redis config,
    SMTP config, admin account details, domain name, TenantId
  TenantId: initialized as Guid.NewGuid() at construction
  Result: restart mid-wizard = new TenantId = corrupted install

EXISTING INFRASTRUCTURE TO USE:
  WizardSession entity — already exists and is migrated
  Migration: V1_3_WizardSessions — already applied
  WizardController — already uses WizardSession for some state
  Do not create new tables. Use what exists.

TASK 1 — Refactor WizardStateService:
  Convert from a stateful singleton into a stateless
  request/response DTO layer.
  All persistent state moves to WizardSession entity.
  WizardStateService becomes: LoadState(token) / SaveState(token, dto)

TASK 2 — Persist all state to WizardSession:
  Every wizard step that currently writes to WizardStateService
  must instead call SaveState(token, dto).
  State payload stored as encrypted JSON in WizardSession.Payload.
  Use the existing ISecretService for encryption — do not roll
  a new encryption scheme.

TASK 3 — Deterministic TenantId:
  TenantId generated ONCE at wizard Step 1.
  Persisted immediately to WizardSession.TenantId.
  Loaded on every subsequent step. Never regenerated.
  If TenantId already exists in session: use it. Never overwrite.

TASK 4 — ResumeWizardAsync:
  Implement: Task<WizardStateDto> ResumeWizardAsync(string token)
  Loads full state from WizardSession by bootstrap token.
  If session not found: return 404 with message
    "Setup session not found. Start a new installation."
  If session is complete: return 409 with message
    "This installation is already complete."

TASK 5 — Resumable wizard test:
  Integration test:
    1. Start wizard, complete steps 1–3, save state
    2. Simulate restart (new DI container, fresh singleton)
    3. Call ResumeWizardAsync(token)
    4. Assert: all step 1–3 state is restored exactly
    5. Assert: TenantId is identical before and after restart

Output: refactored WizardStateService, WizardSession schema
additions if any, updated step handlers, ResumeWizardAsync,
integration test.
```

---

## Prompt 8 — Fix C5 + C7: Consolidate and Secure the Wizard

```
You are a C# senior engineer. There are three competing wizard
backends and four competing wizard UIs. None matches the 7-step
spec. The setup surface is also completely unsecured.

GROUNDING STEP — Confirm these paths before touching anything:
  [ ] AegisCore.API/Controllers/SetupController.cs
  [ ] AegisCore.API/Controllers/WizardController.cs
  [ ] AegisCore.API/Controllers/EnhancedSetupController.cs
  [ ] AegisCore.Portal/src/components/setup-wizard.tsx
  [ ] AegisCore.Portal/src/components/wizard/WizardShell.tsx
  [ ] AegisCore.Setup/ (Blazor project directory)
  [ ] wwwroot/setup/index.html
  [ ] wizard.html (root level of repository)
  [ ] WizardController uses WizardSession entity (DB-backed)
  [ ] WizardShell.tsx is approximately 1,059 lines

If any path does not exist: stop and report actual location.
Do not proceed until all paths are confirmed.

CURRENT STATE:
Backends:
  SetupController    — 11 steps, not DB-backed
  WizardController   — 8 steps, DB-backed, best validation ← KEEP
  EnhancedSetupController — permission + import steps only

UIs:
  setup-wizard.tsx   — 7 steps, delete
  WizardShell.tsx    — 1,059 lines, best microcopy ← KEEP
  AegisCore.Setup    — Blazor app, delete
  wwwroot/setup/index.html — delete
  wizard.html        — delete

PART A — Consolidation:

Step 1: Keep WizardController as the canonical backend.
  Fold these steps from EnhancedSetupController into WizardController:
    - Permission tree builder (dynamic roles)
    - Data import (bulk student/faculty/course upload)
  The merged WizardController must have exactly 7 steps:
    Step 1: University identity (name, logo, timezone, contact)
    Step 2: Database connection (connect, test, migrate)
    Step 3: SSO configuration (provider, credentials, test)
    Step 4: Permission tree (dynamic roles, not hardcoded)
    Step 5: AI model configuration (provider, endpoint, test)
    Step 6: Data import (students, faculty, courses)
    Step 7: Review and go-live

Step 2: Keep WizardShell.tsx as the canonical frontend.
  Extend it to render all 7 steps.
  Delete: setup-wizard.tsx, AegisCore.Setup/,
          wwwroot/setup/index.html, wizard.html

Step 3: Delete SetupController entirely.
  Verify: no route, test, or middleware references SetupController
  after deletion.

Step 4: Output consolidated WizardController (full 7-step
  implementation), updated WizardShell.tsx step list,
  and a deletion log confirming each removed file.

PART B — Security (C7):

Problem: WizardController is [AllowAnonymous]. Anyone who reaches
the host before installation completes can set admin credentials
and own the deployment.

Step 1: Bootstrap token:
  Read AEGISCORE_SETUP_TOKEN from environment on startup.
  If absent: refuse to start setup mode (use P6's validator).
  Every /api/wizard/* and /api/setup/* request must include:
    Header: X-Setup-Token: {token}
  Invalid token: 401 with message
    "Invalid setup token. Check AEGISCORE_SETUP_TOKEN."
  Implement as middleware, not per-controller attribute.

Step 2: Rate limiting on setup surface:
  Max 10 requests per minute per IP on all /api/wizard/* routes.
  On exceed: 429 with message
    "Too many setup attempts. Wait 60 seconds."

Step 3: Exception middleware in install mode:
  GlobalExceptionMiddleware is currently skipped in install mode.
  Add it to the install-mode middleware chain.
  CORS policy must also be active in install mode.

Step 4: Session binding:
  Wizard session is bound to the bootstrap token, not client IP.
  Token is a cryptographic random string (min 32 bytes, hex or base64).
  Printed to container logs on first startup:
    [AEGISCORE] Setup token: {token}
    [AEGISCORE] Navigate to {APP_URL}/setup to begin installation.

Output: bootstrap token middleware, updated Program.cs install-mode
branch, rate limit config, WizardController consolidated 7 steps,
deletion log for removed files, test for token validation.
```

---

## Prompt 9 — Build Missing Wizard Steps (3, 5, Document Template)

```
You are a full-stack engineer (C# backend + React/TypeScript frontend).
Three wizard steps are missing or near-zero implementation.
Build them on top of the consolidated WizardController from P8.

WIZARD STEP 3 — SSO Configuration (currently 10% complete)
Existing substrate:
  SamlConfiguration entity exists
  AzureAdIdentityService exists (partial)
  SsoSession entity exists
  No Okta, no generic SAML flow, no wizard step, no handshake test

Backend task:
  1. Implement ISsoProvider interface:
       ConfigureAsync(SsoConfigDto config)
       TestHandshakeAsync() → HandshakeResult(Success, Error)
  2. Implement providers:
       EntraProvider (AzureAD) — use existing AzureAdIdentityService
       GoogleWorkspaceProvider — OAuth2, discovery document
       GenericSamlProvider — SAML 2.0, SP-initiated flow
       LocalProvider — email/password fallback (already works)
  3. POST /api/wizard/step3/configure — save SSO config
  4. POST /api/wizard/step3/test — run handshake, return result
  5. Store credentials via ISecretService (not plaintext)

Frontend task:
  Wizard Step 3 UI in WizardShell.tsx:
    Provider selector: Entra | Google | SAML 2.0 | Local
    Conditional fields per provider:
      Entra: Tenant ID, Client ID, Client Secret
      Google: Client ID, Client Secret, Domain
      SAML: Metadata URL or XML upload, Entity ID, ACS URL
      Local: no fields (auto-enabled)
    "Test Connection" button → shows:
      ✅ SSO handshake successful — {provider} connected
      ❌ {error message with fix hint}
    Cannot proceed to Step 4 until test passes or Local selected.

---

WIZARD STEP 5 — AI Model Configuration (currently 15% complete)
Existing substrate:
  Ollama provider implemented: OllamaLlmProvider, OllamaEmbeddingProvider
  OpenAI-compatible provider implemented
  ai_provider_type enum: open_ai, anthropic, azure_open_ai, ollama, custom
  No wizard step, no IBM Granite adapter, no connection test UI

Backend task:
  1. Add GraniteProvider — IBM Granite is OpenAI-compatible:
       Endpoint: https://us-south.ml.cloud.ibm.com/ml/v1/text/generation
       Auth: Bearer token (IAM API key)
       Treat as a custom OpenAI-compatible endpoint
  2. POST /api/wizard/step5/configure — save AI config
  3. POST /api/wizard/step5/test — send test prompt "Hello", verify response
  4. Store API key via ISecretService

Frontend task:
  Wizard Step 5 UI in WizardShell.tsx:
    Provider selector: Ollama Local | Ollama Cloud | OpenAI | IBM Granite | Custom
    Conditional fields:
      Ollama Local: model name (default: mistral), no API key
      Ollama Cloud: endpoint URL, model name
      OpenAI: API key, model (default: gpt-4o-mini)
      IBM Granite: IAM API key, model (default: ibm/granite-13b-chat-v2)
      Custom: endpoint URL, model name, API key (optional)
    "Test Connection" button:
      ✅ Model responded — {provider} connected
      ❌ {error with fix hint}
    Cannot proceed to Step 6 until test passes.

---

WIZARD STEP 1 ADDITION — Document Template Designer (currently 0%)
No existing substrate. Build from scratch.

Backend task:
  1. DocumentTemplate entity:
       TenantId, Name, RawHtml (text), ExtractedPalette (json),
       ExtractedFonts (json), LogoUrl (string), CreatedAt
  2. POST /api/wizard/step1/template/upload
       Accepts: HTML file, PDF (extract text + colors), DOCX
       For HTML: store as-is, extract CSS color variables and font-family
       For PDF: use a PDF library to extract dominant colors
       For DOCX: convert to HTML, extract theme colors
       Returns: extracted palette + preview HTML
  3. POST /api/wizard/step1/template/write
       Accepts raw HTML body typed in browser
       Returns: preview HTML
  4. GET /api/wizard/step1/template/preview
       Renders a sample document (transcript, invoice) using the
       stored template — shows how it will look

Frontend task:
  Add to Wizard Step 1 UI:
    "Document Template" section (below logo upload):
    Three options (tabs):
      Upload file — accepts .html, .pdf, .docx
      Write HTML — Monaco editor or textarea
      Skip — use AegisCore default template
    On upload/write:
      Show extracted color swatches
      Show font names
      Show logo placement preview
    Preview button:
      Renders a fake transcript using the extracted design
      Side-by-side: "Your template" vs "Default AegisCore"
    Confirm button saves template to DB

Output: all three step implementations (backend + frontend),
migration for DocumentTemplate, test cases per step.
```

---

---

# PHASE 5 — DATABASE & INFRASTRUCTURE

---

## Prompt 11 — Fix C2: Multi-Dialect Database Support
## ⚠️ DO THIS LAST. Do not start until P3B, P5, P7, P8 are all complete.

```
You are a C# senior engineer. The codebase is hardcoded to
PostgreSQL at every layer. The RFP requires MySQL/MariaDB,
MSSQL, and Oracle support.

THIS IS THE LARGEST CHANGE IN THE AUDIT (160–260h).
Do not start until the following are confirmed complete:
  ✅ C1 — all entities are ITenantScoped (P3B done)
  ✅ C3 — AddEnvironmentSubstitution wired (P5 done)
  ✅ C4 — wizard state persisted to DB (P7 done)
  ✅ C5/C7 — wizard consolidated and secured (P8 done)

CURRENT HARDCODED POSTGRES LOCATIONS:
  UseNpgsql() — DbContextServiceExtensions.cs:18
  UseNpgsql() — AegisDbContextFactory.cs:23
  HasPostgresEnum() — multiple entity configs (list all)
  jsonb columns — (list all)
  set_config('app.tenant_id',…) RLS — Infrastructure/Migrations/Finance*.cs
  NpgsqlConnection direct instantiation — OnboardingService.cs:53-72

TASK 1 — Introduce DB_DIALECT:
  Add to env var registry (P6):
    DB_DIALECT = postgres | mysql | mssql | oracle
  Default: postgres (backward compatible)

TASK 2 — IDbDialectAdapter interface:
  Methods:
    IServiceCollection AddDbProvider(IServiceCollection, string connStr)
    void ConfigureEnumStrategy(ModelBuilder, Type enumType)
    void ConfigureJsonColumn(PropertyBuilder, string columnName)
    string GetTenantFilterSql(string tenantId)
    string GetVersionQuery()

TASK 3 — Implement adapters:
  PostgresDialectAdapter  — wraps UseNpgsql, HasPostgresEnum, jsonb, set_config
  MySqlDialectAdapter     — UseMySQL (Pomelo), HasConversion<string>, JSON column
  MsSqlDialectAdapter     — UseSqlServer, HasConversion<string>, NVARCHAR(MAX) JSON
  OracleDialectAdapter    — UseOracle, HasConversion<string>, CLOB JSON

TASK 4 — Replace hardcoded enum strategy:
  Find every HasPostgresEnum() call.
  Replace with: dialectAdapter.ConfigureEnumStrategy(builder, typeof(MyEnum))
  Each adapter implements this as HasConversion<string> or native enum.

TASK 5 — Replace jsonb columns:
  Find every .HasColumnType("jsonb") call.
  Replace with: dialectAdapter.ConfigureJsonColumn(builder, "column_name")
  Postgres uses jsonb. Others use TEXT / NVARCHAR(MAX) / CLOB.

TASK 6 — Replace RLS with app-level filter:
  Delete set_config('app.tenant_id',…) from Finance migrations.
  The ITenantScoped global query filter (now on all entities from P3B)
  replaces RLS entirely. Document this explicitly.
  Add a comment to each Finance migration explaining the change.

TASK 7 — Replace NpgsqlConnection in wizard:
  OnboardingService.cs:53-72 instantiates NpgsqlConnection directly.
  Replace with dialectAdapter.CreateConnection(connStr)
  which returns the correct IDbConnection for the selected dialect.

TASK 8 — Migration strategy:
  Current migrations are Postgres-only.
  Two options — recommend one and implement it:
    Option A: Single migration set, guard dialect-specific SQL
              behind if (dialect == "postgres") blocks
    Option B: Per-dialect migration folders
              Migrations/Postgres/, Migrations/MySql/, etc.
  Either way: add a 4-dialect CI test matrix that runs
  dotnet ef database update against each dialect in Docker.

TASK 9 — Connection test in wizard:
  Step 2 of the wizard must test the connection for all 4 dialects.
  Use dialectAdapter.GetVersionQuery() to run a dialect-appropriate
  connectivity check (SELECT version() for PG/MySQL,
  SELECT @@VERSION for MSSQL, SELECT * FROM v$version for Oracle).

Output: IDbDialectAdapter + 4 adapters, enum/JSON replacements,
migration strategy implementation, 4-dialect Docker test matrix,
updated wizard Step 2, updated OnboardingService connection test.
```

---

---

# PHASE 6 — SECURITY & INPUT VALIDATION

---

## Prompt 12 — Fix C8: Input Validators for Top 30 Endpoints

```
You are a C# senior engineer. FluentValidation is installed and
AddValidatorsFromAssembly is wired, but the codebase has exactly
zero AbstractValidator implementations. Nothing is validated.

FRAMEWORK:
  FluentValidation.AspNetCore is referenced
  AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()) is called
  All 50 controllers have [ApiController]
  Just adding AbstractValidator classes will activate validation.

TASK 1 — Identify the top 30 write endpoints by risk:
  Priority order:
    Auth:       POST /api/auth/login, /register, /reset-password,
                /refresh-token, /change-password
    Wizard:     POST /api/wizard/step1 through step7
    Enrolment:  POST /api/enrolment, PUT /api/enrolment/{id}
    Marks:      POST /api/grades, PUT /api/grades/{id},
                POST /api/grades/override
    Attendance: POST /api/attendance/checkin, POST /api/attendance/bulk
    Finance:    POST /api/finance/invoice, POST /api/finance/payment,
                POST /api/finance/posting
    Import:     POST /api/import/students, /faculty, /courses
    Users:      POST /api/users, PUT /api/users/{id}, DELETE /api/users/{id}
  List all 30 with their request DTO class names.

TASK 2 — Write AbstractValidator for each:
  Include per endpoint:
    Email: EmailAddress(), MaximumLength(256), NotEmpty()
    Phone: Matches(@"^\+?[1-9]\d{7,14}$"), NotEmpty()
    Numeric ranges: InclusiveBetween() for GPA (0.0–4.0),
                    age (16–100), fee amounts (0–10,000,000)
    Enum fields: Must(x => Enum.IsDefined(typeof(MyEnum), x))
    Dates: Must(d => d > DateTime.UtcNow.AddYears(-1))
           LessThan on EndDate vs StartDate
    Foreign keys: MustAsync(async (id, ct) =>
                    await _repo.ExistsAsync(id, ct))
                  with message: "{Entity} {id} does not exist."
    Passwords: MinimumLength(12), Matches for complexity
    Strings: NotEmpty(), MaximumLength(), no leading/trailing spaces

TASK 3 — Registration:
  Confirm AddValidatorsFromAssembly picks up all 30 new validators.
  Add a build-time test:
    All request DTOs used as [FromBody] params in the top 30
    controllers must have a corresponding AbstractValidator.
    If not: test fails with the missing class name.

TASK 4 — Test:
  For each validator, one test proving it rejects bad input:
    AuthLoginValidator: empty email → ValidationException
    GradeValidator: GPA = 5.0 → ValidationException
    AttendanceValidator: future date → ValidationException
    (30 tests total, one per validator)

Output: list of 30 endpoints + DTOs, 30 validator classes,
registration verification, 30 test cases.
```

---

## Prompt 13 — Fix H1: Replace Four-Role Allow-List with Dynamic RBAC

```
You are a C# senior engineer. The wizard hardcodes four roles and
97 controllers use [Authorize(Roles="...")] string literals.
The dynamic RBAC substrate already exists but is unused.

CURRENT BROKEN STATE:
  EnhancedSetupService.cs:134-138:
    var allowedRoles = new[] {"VC","HOD","Faculty","Student"};
    if (!allowedRoles.Contains(role)) throw ...
  97 × [Authorize(Roles="VC")] style literals across controllers.
  Casbin enforcer: exists, is registered, is NOT used for authz.
  Tables: Role, Permission, RolePermission — all exist, all ITenantScoped.

TASK 1 — Delete the four-role allow-list:
  In EnhancedSetupService.cs:134-138:
  Remove the allowedRoles validation entirely.
  Any role name the admin enters is valid.
  The only constraint: role name must be non-empty and ≤ 64 chars.

TASK 2 — Wizard Step 4 — Dynamic permission tree UI:
  WizardShell.tsx Step 4:
    "What roles exist at your institution?" — free text add
    Admin can add N roles (no limit, no preset list)
    For each role: a permission matrix (table):
      Rows: modules (Students, Grades, Attendance, Finance,
            Timetable, Admissions, Reports, Settings, System)
      Columns: Read | Write | Delete | Approve | Export
      Toggle per cell
    "Add Role" button for adding more
    Default seeded roles (suggested, not forced):
      Admin, Faculty, Student — admin can rename or delete these

TASK 3 — Seed roles from wizard to DB:
  On wizard Step 4 save:
    POST /api/wizard/step4/roles → persist to Role table
    POST /api/wizard/step4/permissions → persist to Permission table
    POST /api/wizard/step4/matrix → persist to RolePermission table
  All rows keyed by TenantId.

TASK 4 — [RequirePermission] attribute:
  Create: RequirePermissionAttribute(string resource, string action)
  Middleware reads the current user's roles from JWT claims.
  Checks RolePermission table (cached per tenant per request):
    user.roles → RolePermission → Permission.Resource + Action
  If no matching permission: 403 with message:
    "You do not have {action} permission on {resource}."

TASK 5 — Replace 97 [Authorize(Roles=...)] literals:
  Map each literal to the nearest [RequirePermission] equivalent:
    [Authorize(Roles="VC")] → [RequirePermission("System","Admin")]
    [Authorize(Roles="Faculty")] → [RequirePermission("Grades","Write")]
    (map all 97 — output the full list)
  Output: complete mapping table + updated controller attributes.

TASK 6 — Casbin integration:
  Wire the existing Casbin enforcer to evaluate
  RequirePermission checks using the RolePermission data.
  Casbin policy syncs from DB on tenant login + 5-minute cache TTL.

Output: deleted allow-list, wizard Step 4 UI, role/permission
persistence endpoints, RequirePermissionAttribute, 97-attribute
replacement table, Casbin policy sync.
```

---

---

# PHASE 7 — FRONTEND UX

---

## Prompt 14 — Fix H3, H5, H6: Frontend UX (Demo-Ready)

```
You are a React/TypeScript senior engineer working on a Next.js
Portal. Three high-visibility UX issues must be fixed before
any demo.

PORTAL LOCATION: AegisCore.Portal/src/
PAGES: 43 pages under src/app/
COMPONENTS: 26 components under src/components/
CURRENT CSS: src/app/globals.css has exactly 2 media queries:
  max-width: 1023px  (mobile — treats tablet as mobile, WRONG)
  max-width: 639px

---

FIX H3 — Error Boundaries (prevents white-screen crashes):

For Next.js App Router:

Create: src/app/error.tsx
  'use client'
  Shows: "Something went wrong" + description + Reload button
  Logs error to console (or error service if wired)

Create: src/app/global-error.tsx
  Same pattern — catches root layout errors

Create: src/app/not-found.tsx
  Shows: "Page not found" + Back to Dashboard button

Create: src/components/ErrorBoundary.tsx
  Class component wrapping React.ErrorBoundary
  Prop: fallback (ReactNode)
  Wrap each of the 43 page root components with this

High-risk pages needing their own error.tsx (create one per):
  src/app/finance/error.tsx
  src/app/examinations/error.tsx
  src/app/admissions/error.tsx
  src/app/grades/error.tsx

---

FIX H5 — Tablet Breakpoint (768–1023px tier):

Add to globals.css:
  @media (min-width: 768px) and (max-width: 1023px) {
    /* Sidebar: collapsed by default, toggle on tap */
    /* Layout: two-column grid (sidebar + content) */
    /* Tables: card-style on tablet (not compressed) */
    /* Buttons: min touch target 44×44px */
    /* Navigation: sidebar not bottom nav, not top nav */
  }

Rules per component type:
  Sidebar: visible but collapsed (icon only) on tablet
           Expand on tap or hover
  Data tables: 768px → card layout (each row becomes a card)
               1024px+ → full table layout
  Dashboards: 768px → 2-column grid, not 3-4 column
  Forms: 768px → single column, not two-column
  Modals: 768px → 90% width, not fixed 600px

Test on these viewports:
  iPad (768×1024), iPad Pro (1024×1366),
  Samsung Tab S8 (800×1280)

---

FIX H6 — Microcopy Through WizardError Shape:

The backend already returns WizardError with:
  { code, userMessage, howToFix, fixCommand, technicalDetail }

Build: src/components/ErrorDisplay.tsx
  Props: error (WizardError shape), onRetry?: () => void
  Renders:
    Icon (warning/error based on severity)
    userMessage in large text
    howToFix in smaller text
    Retry button (if onRetry provided)
    "Show technical details" toggle (shows technicalDetail)

Replace these strings across all 43 pages:
  "Failed to load {module} data" (40+ occurrences)
    → use ErrorDisplay with appropriate userMessage
  "Failed" (17 occurrences)
    → "Something went wrong — try again"
  "Success" (4 occurrences)
    → specific message per context
       e.g. "Grades saved" not "Success"

Empty states (add to every list/table page that currently shows nothing):
  No data message + context-appropriate action button
  e.g. "No students enrolled yet — Import students to get started"

Loading states:
  Replace bare spinners with descriptive text:
  "Loading grades..." not a spinner alone

Output: ErrorBoundary + error.tsx files (all pages), tablet CSS
tier, ErrorDisplay component, microcopy replacements across 43
pages, viewport test results.
```

---

## Prompt 15 — Geolocation Attendance

```
You are a full-stack engineer (C# + React/TypeScript).
Geolocation attendance is 0% implemented. Build it end-to-end.

CURRENT ATTENDANCE SCHEMA:
  Attendance entity has:
    EnrollmentId, CourseId, FacultyId, Date, Status
  Missing: TenantId (P3B adds this), coordinates, geofence reference
  File: AegisCore.Domain/Entities/Attendance.cs

---

BACKEND TASKS:

TASK 1 — Schema additions (migration required):
  Add to Attendance entity:
    Latitude         decimal(9,6)?   nullable (null = no GPS)
    Longitude        decimal(9,6)?   nullable
    GeofenceId       Guid?           nullable FK to CampusPolygon
    LocationStatus   enum: ON_CAMPUS | OFF_CAMPUS | UNVERIFIABLE
    IsManualOverride bool            default false
    OverrideReason   string?         nullable

  Create CampusPolygon entity:
    Id         Guid
    TenantId   Guid (ITenantScoped)
    Name       string (e.g. "Main Campus", "North Campus")
    Vertices   json (array of {lat, lon} pairs — minimum 3 points)
    CreatedAt  DateTime
    IsActive   bool

  Migration name: AddGeolocationToAttendance

TASK 2 — Coordinate encryption:
  Latitude and Longitude stored encrypted at rest.
  Use existing AegisCore.Infrastructure/Security/ encryption service.
  Decrypt only in authorized endpoints (not teacher-facing).

TASK 3 — Point-in-polygon algorithm (backend):
  Implement Ray Casting algorithm in C#:
    bool IsPointInPolygon(decimal lat, decimal lon,
                          List<(decimal lat, decimal lon)> polygon)
  This must match the frontend turf.js result for the same inputs.
  Add unit tests with known coordinates:
    Point inside polygon → true
    Point outside polygon → false
    Point on boundary → true (treat as inside)

TASK 4 — Check-in endpoint:
  POST /api/attendance/checkin
  Body: { studentId, courseId, latitude, longitude }
  Auth: [RequirePermission("Attendance","Write")]
  Logic:
    Load active CampusPolygon for tenant
    Run IsPointInPolygon
    If inside:  LocationStatus = ON_CAMPUS
    If outside: LocationStatus = OFF_CAMPUS
    If no GPS:  LocationStatus = UNVERIFIABLE
    Save to Attendance (coordinates encrypted)
    Return: { status, recordedAt, message }
  Response messages:
    ON_CAMPUS:     "Attendance recorded — you're on campus."
    OFF_CAMPUS:    "Attendance recorded — location shows off-campus.
                    Contact your teacher if this is incorrect."
    UNVERIFIABLE:  "Attendance recorded — location unavailable.
                    Marked for teacher review."

TASK 5 — Teacher override endpoint:
  POST /api/attendance/{id}/override
  Body: { reason }
  Auth: [RequirePermission("Attendance","Approve")]
  Sets: IsManualOverride = true, OverrideReason = reason
  Does NOT expose student coordinates to teacher.

TASK 6 — Campus polygon management:
  POST /api/campuses/polygon — create geofence (admin only)
  PUT  /api/campuses/polygon/{id} — update polygon
  GET  /api/campuses/polygon — list polygons for tenant

---

FRONTEND TASKS:

TASK 7 — Mobile check-in component:
  src/components/attendance/CheckInButton.tsx
  Behavior:
    Button: "Mark Attendance"
    On click: request browser geolocation
    If granted: POST to /api/attendance/checkin with coords
    If denied:  POST with null coords (UNVERIFIABLE)
    Show result: green/amber/red badge with the LocationStatus message
    Disable button after successful check-in (prevent double-mark)

TASK 8 — Teacher dashboard:
  src/app/attendance/page.tsx additions:
    Per student row:
      Show badge: ON CAMPUS | OFF CAMPUS | UNVERIFIABLE | MANUAL OVERRIDE
      Do NOT show coordinates (not even to teacher)
      Show: Override button (opens reason modal) for OFF CAMPUS rows
    Filter: show only OFF CAMPUS rows for quick review

TASK 9 — Admin polygon editor:
  src/app/admin/campus/page.tsx
  Simple map interface (use Leaflet or Google Maps embed):
    Draw polygon on map to define campus boundary
    Name the polygon
    Save → POST to /api/campuses/polygon
    Show existing polygons for the tenant

Output: Attendance schema migration, CampusPolygon migration,
coordinate encryption, point-in-polygon with tests, check-in
endpoint, override endpoint, polygon management endpoints,
CheckInButton component, teacher dashboard additions, admin
polygon editor.
```

---

---

# PHASE 8 — COMPLIANCE

---

## Prompt 16 — FERPA + GDPR Compliance

```
You are a C# senior engineer implementing regulatory compliance.
Both FERPA and GDPR appear ZERO times in the codebase.

---

PART A — FERPA (Family Educational Rights and Privacy Act)

FERPA requires:
  - Log every access to student educational records
  - Prevent teachers accessing other teachers' class records
  - Prevent students accessing other students' records
  - Admin can run audit report: who accessed what when

TASK A1 — FerpaAuditLog entity:
  Id          Guid
  TenantId    Guid (ITenantScoped)
  UserId      Guid (who accessed)
  UserRole    string
  ResourceType  string (Grade | Attendance | Transcript | Admission)
  ResourceId    Guid (the specific record accessed)
  StudentId     Guid (whose record was accessed)
  Action        string (Read | Export | Print)
  AccessedAt    DateTime
  IpAddress     string (encrypted)
  Justified     bool (was access within normal role scope?)

TASK A2 — [RequireFerpaAudit] middleware:
  Attribute: [RequireFerpaAudit(ResourceType, StudentIdParam)]
  Applied to every endpoint that reads:
    Grades, Attendance, Transcripts, AdmissionApplications
  On each request:
    Extract ResourceType and StudentId from route/query
    Verify: accessing user has a legitimate relationship
            to this student (enrolled in their course,
            or is the student themselves, or is admin)
    Write FerpaAuditLog entry regardless of outcome
    If no legitimate relationship: 403 +
      "You do not have authorisation to access this student's records."

TASK A3 — FERPA audit report endpoint:
  GET /api/admin/ferpa-audit
  Auth: [RequirePermission("System","Admin")]
  Filters: dateFrom, dateTo, userId, studentId, resourceType
  Returns: paginated FerpaAuditLog entries
  Response includes: human-readable summary per row

TASK A4 — Directory information opt-out:
  Add to Student entity:
    DirectoryOptOut bool default false
  If true: student's name, photo, enrollment status
           are excluded from any public-facing directory
  Enforce in all directory/search endpoints.

---

PART B — GDPR (General Data Protection Regulation)

GDPR requires:
  - Right to erasure: all PII deleted within 30 days of request
  - Consent tracking: prove what was consented to and when
  - DSAR: export all personal data on request
  - Data retention: auto-delete stale records

TASK B1 — ConsentLedger entity:
  Id            Guid
  TenantId      Guid (ITenantScoped)
  UserId        Guid
  DataCategory  string (e.g. "academic_records", "biometric",
                         "location_data", "communications")
  ConsentedAt   DateTime
  ExpiresAt     DateTime? nullable
  RevokedAt     DateTime? nullable
  ConsentText   string (exact text shown to user at consent time)
  IpAddress     string (encrypted)

TASK B2 — ErasureRequest entity:
  Id           Guid
  TenantId     Guid
  UserId       Guid
  RequestedAt  DateTime
  DeadlineAt   DateTime (RequestedAt + 30 days)
  ErasedAt     DateTime? nullable
  Status       enum: Pending | InProgress | Completed | Failed
  FailureReason  string? nullable

TASK B3 — Cascade erasure implementation:
  POST /api/users/{id}/erasure-request
  Auth: user requesting their own erasure, or admin
  On request: create ErasureRequest, set Status=Pending

  Background job (runs daily):
    Find ErasureRequest where Status=Pending or InProgress
    For each: execute erasure cascade:
      Student: anonymize Name → "ERASED_USER_{id}",
               Email → "{id}@erased.invalid",
               Phone → null, DateOfBirth → null
      Attendance: set StudentId → null (keep academic record)
      Grades: keep grade values, anonymize StudentId reference
      Transcripts: anonymize student name on transcript
      FerpaAuditLog: replace UserId/StudentId with "ERASED"
                     (keep the log for compliance)
      ConsentLedger: mark all entries Revoked
      Sessions: delete all active sessions
      Profile photos: delete from storage
    Set Status=Completed, ErasedAt=now
    If any step fails: Status=Failed, FailureReason=message

TASK B4 — DSAR export:
  GET /api/users/{id}/dsar-export
  Auth: user themselves, or admin
  Returns: ZIP file containing:
    student.json   — all profile fields
    grades.csv     — all grade records
    attendance.csv — all attendance records
    consent.json   — all ConsentLedger entries
    access_log.json — all FerpaAuditLog entries for this user
  ZIP generated on-demand. Download link expires in 24h.

TASK B5 — Data retention background job:
  Runs daily at 02:00 tenant local time.
  Deletes:
    FerpaAuditLog entries older than 6 years
    ConsentLedger entries where RevokedAt > 6 years ago
    ErasureRequest entries where Completed > 7 years ago
  Retention period configurable per tenant:
    TenantConfiguration.AuditRetentionYears (default: 6)

Output: FerpaAuditLog + ConsentLedger + ErasureRequest schemas,
[RequireFerpaAudit] middleware, FERPA audit report, directory
opt-out, cascade erasure service, DSAR export endpoint,
retention background job, test: full erasure cycle end-to-end.
```

---

## Prompt 17 — WCAG 2.1 AA Accessibility

```
You are a frontend accessibility engineer. The Portal has 43 pages
and no accessibility testing whatsoever. WCAG 2.1 AA is required.

CURRENT STATE:
  No axe-core, no jest-axe, no pa11y, no Lighthouse CI
  52 aria-label, 4 aria-hidden, 2 aria-live, 1 aria-sort — very thin
  ACCESSIBILITY.md claims "layouts collapse below 900px" (no 900px
  breakpoint exists) and "0 known issues" (never tested)
  2 media queries total — no tablet tier (being fixed in P14)

TASK 1 — Add axe-core to CI:
  Install: npm install --save-dev @axe-core/playwright
  Add to .github/workflows/ci.yml a new job: accessibility-check
    - Starts the Portal dev server
    - Runs axe against these 10 highest-risk pages:
        /login, /dashboard, /students, /grades,
        /attendance, /finance, /timetable,
        /setup (wizard), /admin, /profile
    - Fails CI if any WCAG 2.1 AA violation is introduced
    - Uploads violation report as CI artifact
  This job runs on every PR.

TASK 2 — Audit all 43 pages:
  Run axe-core against every page in src/app/.
  Output a violation report per page:
    | Page | Rule | Element | Impact | Fix |
  Categorise by impact: Critical | Serious | Moderate | Minor
  Focus first on Critical and Serious.

TASK 3 — Fix the top 20 violations:
  Common WCAG 2.1 AA failures to look for and fix:
    Images missing alt text:
      <img> without alt → add descriptive alt or alt="" if decorative
    Form inputs missing labels:
      <input> without <label for=...> → add label
    Buttons without accessible name:
      <button><svg></button> → add aria-label
    Low color contrast:
      Text on background below 4.5:1 ratio → fix colors
    Links with non-descriptive text:
      "Click here", "Read more" → make descriptive
    Missing skip navigation:
      Add <a href="#main-content">Skip to main content</a> at top
    Tables missing headers:
      <table> without <th> and scope → add them
    Focus not visible:
      CSS that removes :focus outline → restore it
    Modals not trapping focus:
      Focus must stay inside modal while open → fix with focus-trap
    Form error messages not associated:
      Error text not linked to input via aria-describedby → fix

TASK 4 — Fix ACCESSIBILITY.md:
  Replace current inaccurate content with:
    ## Accessibility Statement
    Standard: WCAG 2.1 Level AA
    Last tested: {date}
    Testing method: axe-core automated + manual keyboard nav
    Known limitations: {list any genuine ones}
    How to test locally:
      npx axe http://localhost:3000 --include="main"
      Or: axe DevTools browser extension (free tier)
    How to test keyboard navigation:
      Tab through every interactive element
      Ensure focus is always visible
      Ensure modal focus trapping works
      Ensure skip-to-content link appears on first Tab

TASK 5 — Keyboard navigation audit:
  Tab through these flows and document any traps or gaps:
    Login flow, Dashboard, Grade entry form,
    Attendance mark, Wizard step 1–7, Data import
  Fix any focus traps or missing keyboard interactions.

Output: CI axe configuration, 43-page violation report, 20 fixes
with before/after code, corrected ACCESSIBILITY.md, keyboard
audit report.
```

---

---

# HARDCODED VALUES — FIX ALONGSIDE RELEVANT PROMPTS

These must be converted to tenant configuration. Fix them during
the prompt that touches the related module.

Fix during P8 (wizard) or P9 (wizard steps):
  Province lists hardcoded in WizardShell.tsx:116
  https://aegiscore.university.edu.pk in AllowedOrigins

Fix during P13 (RBAC):
  expiresIn = 900 (token lifetime) → SESSION_LIFETIME env var
  MaxRows = 50_000 in EnhancedSetupService:14 → tenant config
  _dangerousKeywords SQL denylist → replaced by allow-list (P12)

Fix during P14 (frontend):
  currency: "PKR" in finance-dashboard.tsx:28 → tenant config
  currency: "PKR" in department-finance-dashboard.tsx:19 → tenant config
  currency: "PKR" in finance/operations/page.tsx:22 → tenant config
  currency: "PKR" in student/finance/[studentId]/page.tsx:38 → tenant config
  Intl.NumberFormat("en-PK") in 6 files → use tenant locale
  currency === "PKR" ? "raast" : "stripe" → tenant payment config
  "HEC Approved" / "Pending HEC" → tenant-configurable accreditor
  CNIC column label → tenant-configurable ID document name
  "PKR 48,500 Outstanding..." literal placeholder → remove entirely
  "Pakistan Studies" in transcript sample → remove
  Sample data: @university.edu.pk, Dr. Ayesha Ahmed → remove
  SslMode: Disable in design-time factory → Require always

Fix during P16 (compliance):
  FERPA / GDPR — document these are opt-in compliance modules
  LICENSE_SERVER_URL heartbeat → disclose payload, add offline mode
  Cloudflare tunnel hardcoded in GoLiveAsync → make optional

---

# NOTES FOR ALL AGENTS

1. Read the Graphify knowledge graph (graphify-out/) first.
   Use it for navigation. Do not re-read files the graph covers.

2. Every prompt states exact file paths from the audit.
   If a file is not at the stated path: STOP and report it.
   Do not assume or guess an alternate path.

3. Do not fix anything outside the stated scope of each prompt.
   Scope creep between prompts causes merge conflicts.

4. After completing any prompt: run dotnet build AegisCore.slnx
   -c Release and confirm 0 errors before declaring done.

5. The duplicate backend aegiscore-backend/ is deleted in P10.
   If you see it: you are in the wrong tree. Use the root.

6. All infrastructure is CLIENT-OWNED. AegisCore stores no data
   on Nocturne infrastructure. We configure only, never host.
   Do not introduce any Nocturne-hosted dependencies.

7. The licence heartbeat (LICENSE_SERVER_URL) calls home.
   Until offline licence mode is implemented, disclose this
   to the client before deployment.
