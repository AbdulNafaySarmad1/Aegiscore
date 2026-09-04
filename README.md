# AegisCore — Complete University Operating Suite

> **Architecture:** multi-tenant modular monolith in a monorepo. AegisCore is
> modular internally, unified commercially, and configurable operationally.
> Every valid license activates the complete core suite; modules are not sold
> or licensed separately. See
> [`docs/architecture/0001-modular-monolith.md`](docs/architecture/0001-modular-monolith.md).

## Overview
This is the AegisCore university operating platform: a Next.js 16 portal backed
by a .NET 10/C# 14 authoritative API, PostgreSQL, Redis, background processing,
and cloud/on-premises deployment paths.

## Operational local/demo stack

The supported local stack includes the React portal, ASP.NET Core API,
PostgreSQL 17, Redis 7, automatic migrations, persistent volumes, and an
explicitly non-production demo identity.

```bash
./scripts/bootstrap-env.sh
docker compose --env-file .env up -d --build --wait
```

Open <http://localhost:8080>. Verify the runtime contract with:

```bash
curl --fail http://localhost:8080/health/ready
docker compose --env-file .env ps
```

Stop without deleting data using `docker compose --env-file .env down`.
Demo mode is rejected when `ASPNETCORE_ENVIRONMENT=Production`; production
requires institutional SSO, a signed license, TLS/DNS, SMTP, backups, and
credentials for each selected external integration.

To run the verified prebuilt release-candidate images instead of compiling
locally:

```bash
./scripts/bootstrap-env.sh
printf '\nAEGISCORE_API_IMAGE=ghcr.io/abdulnafaysarmad1/aegiscore-api:recovery-latest\n' >> .env
printf 'AEGISCORE_PORTAL_IMAGE=ghcr.io/abdulnafaysarmad1/aegiscore-portal:recovery-latest\n' >> .env
docker compose --env-file .env pull api portal
docker compose --env-file .env up -d --no-build --wait
curl --fail http://localhost:8080/health/ready
```

The repository/package visibility must allow the host to pull from GHCR. Use
the immutable commit-SHA image tag for controlled releases; `recovery-latest`
is the moving integration tag for this recovery branch.

**22 prompts total:**
- 9 Utility Prompts (infrastructure, architecture, governance)
- 13 Sprint Prompts (feature development)

---

## Development Setup

Configure Git to use the repository's shared hooks once after cloning:

```bash
git config core.hooksPath .githooks
```

### Building

```bash
dotnet build AegisCore.slnx -c Release
```

The build must succeed with 0 errors before any pull request is merged.

### First-run setup

Generate the bootstrap token before starting an uninstalled instance:

```bash
openssl rand -hex 32
```

Set the result as `AEGISCORE_SETUP_TOKEN`, start AegisCore, and open
`${APP_URL}/setup`. Enter the token printed in the container startup log. Every
setup API request is bound to this token and limited to 10 attempts per minute
per client IP. Setup mode refuses to start when the token is missing or is not
a 32-byte hexadecimal/base64 value.

### Required GitHub status check

In **Settings → Branches → Branch protection rules**, create or edit the rule
for `main`. Enable **Require status checks to pass before merging**, enable
**Require branches to be up to date before merging**, and select
`Build & Test (.NET)` as a required check. Do not allow bypassing the rule.

## Quick Start

1. **Start here:** Read `INDEX.md` first
2. **Understand codebase:** Run `U1-codebase-understanding.md`
3. **Build Sprint 1:** Copy `S1-foundation.md` into Claude, paste your codebase summary, ask Claude to build it
4. **Repeat for each sprint:** S2 → S3 → ... → S13

---

## 📋 File Structure

```
aegiscore-prompts/
├── INDEX.md                          # Start here - roadmap & navigation
├── README.md                         # This file
├── utility/
│   ├── U1-codebase-understanding.md    # Audit existing code
│   ├── U2-patch.md                     # Bug fix workflow
│   ├── U3-cleanup.md                   # Code quality improvement
│   ├── U4-docker-build-validator.md    # Docker validation
│   ├── U5-onboarding-wizard.md         # First-run setup
│   ├── U6-license-engine.md            # License heartbeat + governance
│   ├── U7-cloud-agnosticism.md         # Data sovereignty guarantee
│   ├── U8-competitive-analysis.md      # Beat every competitor
│   └── U9-community-plugins.md         # Plugin ecosystem
└── sprints/
    ├── S1-foundation.md                # Cloud-agnostic + RBAC + audit + calendar
    ├── S2-admissions.md                # Merit (SHAP) + allocation + payment
    ├── S3-examination.md               # Grading + transcripts + results
    ├── S4-hr.md                        # Recruitment + payroll (Pakistan-compliant) + leave
    ├── S5-obe-cis.md                   # OBE + CIS scanner (HEC accreditation)
    ├── S6-microsoft-ecosystem.md       # Teams + Moodle + Power BI
    ├── S7-finance.md                   # Fees + GL + procurement + PIFRA
    ├── S8-portals.md                   # Student + faculty self-service
    ├── S9-communications.md            # WhatsApp + email + SMS + tickets
    ├── S10-campus-ops.md               # Library + transport + visitor mgmt
    ├── S11-student-lifecycle.md        # Placement + alumni + activities + health
    ├── S12-asset-inventory-hec.md      # Assets + inventory + HEMIS export
    └── S13-mobile-apps.md              # React Native offline-first apps
```

---

## 🚀 How to Use These Prompts

### Option 1: Claude (Recommended)
```
1. Open Claude chat
2. Copy entire contents of a prompt file (e.g., S1-foundation.md)
3. Paste into chat
4. Add: "I'm Abdul, building AegisCore in C# 14/.NET 10. Here's my codebase structure [paste U1 output]. Build this sprint."
5. Claude generates all code for that sprint
6. Repeat for each sprint
```

### Option 2: Manual Development
- Use each prompt as a specification document
- Follow the deliverables checklist
- Reference database schemas, service interfaces, API endpoints
- Cross-check against dependent sprints

---

## 🎯 Key Features (What AegisCore Includes)

### Academic
- ✅ SHAP-explainable merit lists (statistical rigor)
- ✅ OBE framework with CLO-PO mapping (HEC accreditation)
- ✅ CIS scanner (auto-extract CLOs from PDFs)
- ✅ Examination system (grading, transcripts, hall tickets)
- ✅ Online admissions (payments via JazzCash/EasyPaisa)

### Operations
- ✅ Federated RBAC (dept autonomy, no bottlenecks)
- ✅ HR system (Pakistan-compliant payroll with EOBI/income tax/PF)
- ✅ Finance system (GL accounting, procurement, PIFRA export)
- ✅ Campus operations (library, transport, visitor management)

### Integration
- ✅ Microsoft Teams (auto-provision classes, sync enrollments)
- ✅ Moodle LMS (grade passback, roster sync, SSO)
- ✅ Power BI (embedded role-specific dashboards)

### Communications
- ✅ WhatsApp Business API (95% open rates)
- ✅ Email + SMS (fallback)
- ✅ In-app notifications (real-time SignalR)
- ✅ Grievance ticketing system

### Ecosystem
- ✅ Community plugin system (developers earn ₱35K-140K per plugin)
- ✅ Plugin marketplace (discovery, installation, ratings)
- ✅ Revenue sharing (30% AegisCore Systems, 70% developer)

### Mobile
- ✅ React Native iOS + Android apps
- ✅ Offline-first (SQLite sync when connected)
- ✅ Biometric auth (Face ID / Fingerprint)

### Data Sovereignty
- ✅ 100% cloud-agnostic (local disk, S3, Azure, SFTP, on-prem)
- ✅ Zero vendor lock-in
- ✅ Data never leaves university's infrastructure
- ✅ License heartbeat (daily validation, no piracy workarounds)

---

## 🏆 Competitive Advantages

### vs EduSuite
- ✅ SHAP-explainable merit (they just score)
- ✅ OBE framework (HEC accreditation)
- ✅ Cloud-agnostic (they're cloud-only)
- ✅ Offline mobile

### vs SyberEdge
- ✅ Software sophistication (academic + finance + lifecycle)
- ✅ Data sovereignty
- ✅ Research tracking

### vs Futuresol
- ✅ SHAP merit + OBE
- ✅ Modern stack (C# 14/.NET 10)
- ✅ Extended lifecycle (placement, alumni)

### vs OpenEduCat
- ✅ Modern architecture
- ✅ OBE framework
- ✅ CIS scanner (unique)
- ✅ Governance + license model
- ✅ Offline mobile

### vs Global ERPs (Oracle/SAP/Dynamics)
- ✅ Pakistan-native (not 50-language bloat)
- ✅ Data sovereignty (data in Pakistan)
- ✅ Cost (₱8M vs ₱50M+)
- ✅ Deployment speed (1 month vs 12)
- ✅ No US/Europe servers

---

## 📊 Build Timeline

**Recommended:** 8 months solo, 2 months with 4 engineers working in parallel

### Phase 1: Foundation (Weeks 1-3)
- S1 (Cloud-agnostic + RBAC + audit)

### Phase 2: Core Academic (Weeks 4-12)
- S2 (Admissions + SHAP merit)
- S3 (Examination + grading)
- S5 (OBE + CIS scanner)

### Phase 3: Operations (Weeks 13-18)
- S4 (HR + payroll)
- S7 (Finance + GL)
- S9 (Communications)

### Phase 4: Interfaces (Weeks 19-23)
- S8 (Portals)
- S6 (Microsoft ecosystem)

### Phase 5: Extended Modules (Weeks 24-28)
- S10 (Campus ops)
- S11 (Student lifecycle)
- S12 (Assets/inventory)

### Phase 6: Mobile (Weeks 29-34)
- S13 (React Native apps)

---

## 🔒 Security & Compliance

- ✅ AES-256 encryption at rest
- ✅ Immutable audit logs (all actions tracked)
- ✅ RBAC with permission matrix
- ✅ Row-level security (HOD sees only their dept)
- ✅ GDPR-compatible (data deletion, export)
- ✅ Pakistan data residency (no US/EU servers)
- ✅ PIFRA compliance (financial audit trail)
- ✅ HEC accreditation ready (OBE + HEMIS export)

---

## 💻 Tech Stack

- **Language:** C# 14
- **Framework:** .NET 10
- **Database:** PostgreSQL (cloud-agnostic)
- **Cache:** Redis
- **Frontend:** Blazor Server / Razor Pages (or Next.js if preferred)
- **Mobile:** React Native (iOS + Android)
- **Deployment:** Docker + Kubernetes (any cloud or on-prem)
- **Storage:** Abstracted (local/S3/Azure/SFTP)

---

## 🚢 Launch Roadmap

### Month 1-8: Build
- Complete all 13 sprints
- Internal testing
- Security audit
- Documentation

### Month 9: Soft Launch
- Deploy to SSUET (pilot customer)
- Gather feedback
- Fix critical bugs

### Month 10: Public Launch
- Market to 500 Pakistani universities
- Launch plugin marketplace
- Begin developer onboarding

### Year 2+: Scale
- 50+ universities deployed
- 100+ plugins in marketplace
- ₱4B annual revenue (licensing + plugins)

---

## 📞 Questions?

Refer to `INDEX.md` for detailed navigation, sprint dependencies, and competitive positioning.

---

**Built for Pakistan. By Pakistanis. With no apologies. 🇵🇰🚀**
