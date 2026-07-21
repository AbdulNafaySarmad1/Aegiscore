<div align="center">

# ◆ AegisCore

**Enterprise University ERP Platform**

Built by [Apex Systems (Pvt. Ltd.)] · Licensed per institution · Deployed on university-owned infrastructure

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat-square&logo=postgresql)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat-square&logo=redis)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)
![Security](https://img.shields.io/badge/Security-25%2F25%20Fixed-22c55e?style=flat-square)
![Hardcode Audit](https://img.shields.io/badge/Hardcode%20Audit-EXIT%200-22c55e?style=flat-square)

</div>

---

## What is AegisCore?

AegisCore is a multi-tenant university ERP system that covers the entire university operation stack — academics, finance, HR, student lifecycle, research, hostel, transport, library, exams, accreditation, and more — deployed per institution on infrastructure the university owns and controls.

No shared cloud. No vendor lock-in. One license key, one onboarding wizard, one running system.

---

## Scale

| Metric | Count |
|--------|-------|
| Domain Entities | 113 |
| Database Tables (DbSets) | 303 |
| API Endpoints | ~350 |
| DI Registrations | 206 |
| Typed Repositories | 43 |
| Background Services | 5 |
| Cloud Provider Implementations | 4 (Azure / AWS / GCP / On-Premise) |
| Security Findings Fixed | 25 / 25 |
| Hardcode Audit Checks | 7 / 7 PASS |

---

## Modules

| # | Module | Entities | Status |
|---|--------|----------|--------|
| 1 | Core Academic (Departments, Courses, Enrollment, Attendance, Results) | 14 | ✅ |
| 2 | Finance / General Ledger (CoA, Journal Entries, Fiscal Periods) | 5 | ✅ |
| 3 | HR ERP (Employees, Payroll, Leave, Recruitment, Performance) | 13 | ✅ |
| 4 | Student Information System (Fees, Discipline, Counseling, Clearance) | 12 | ✅ |
| 5 | Research Management (Projects, Grants, Publications, Patents, Ethics) | 12 | ✅ |
| 6 | Outcome Based Education / OBE (CLOs, POs, Rubrics, Attainment) | 14 | ✅ |
| 7 | Accreditation Suite (HEC / ABET / PEC / WashingtonAccord) | 17 | ✅ |
| 8 | Library Management (Catalog, Circulation, Digital Resources) | — | ✅ |
| 9 | Hostel Management (Rooms, Allocations, Gate Logs) | — | ✅ |
| 10 | Transport Management (Routes, Buses, Student Assignments) | — | ✅ |
| 11 | Exam & Timetable Management | — | ✅ |
| 12 | Asset & Inventory Management | — | ✅ |
| 13 | Procurement & Vendor Management | — | ✅ |
| 14 | Budgeting & Financial Planning | — | ✅ |
| 15 | Multi-Campus Management | — | ✅ |
| 16 | Enterprise Config Center | — | ✅ |
| 17 | Role-Based & ABAC Authorization (Casbin) | — | ✅ |
| 18 | SAML / Microsoft Entra SSO | — | ✅ |
| 19 | Zero Trust / Cloudflare Tunnel | — | ✅ |
| 20 | Workflow Engine (4 pre-built templates) | — | ✅ |
| 21 | Notification Platform (Email / SMS / Push / In-App) | — | ✅ |
| 22 | AI Agent Suite (Student, Faculty, Finance, HR advisors) | — | ✅ |
| 23 | Post-Quantum Cryptography (ML-DSA / SPHINCS+ via Bouncy Castle) | — | ✅ |
| 24 | Login Audit + Anomaly Detection | — | ✅ |
| 25 | Backup Service (AES-256-GCM encrypted, provider-agnostic) | — | ✅ |
| 26 | Degree Verification (token-based, employer-facing) | — | ✅ |
| 27 | Semester Registration with Waitlisting | — | ✅ |
| 28 | Curriculum Governance (proposal → approval workflow) | — | ✅ |
| + | 18 more modules (P77–P112) | — | ✅ |

---

## Architecture

```
AegisCore.Domain          → 113 entities, 190+ enums, zero dependencies
AegisCore.Contracts       → 45 shared interfaces, references Domain only
AegisCore.Application     → Services, DTOs — references Domain + Contracts
AegisCore.Infrastructure  → Repositories, DbContext, providers — references Domain + Contracts
AegisCore.API             → Controllers, Middleware, Program.cs — references all above
AegisCore.Setup           → Blazor Server onboarding wizard (port 9000, self-destructs after go-live)
AegisCore.Web             → Blazor WASM frontend
AegisCore.Tests           → xUnit test project
```

Clean Architecture. No circular dependencies. Dependency chain: `Domain → Contracts → Application / Infrastructure → API`

---

## Cloud Provider Abstraction

Single environment variable switches the entire cloud provider stack:

```
AEGISCORE_CLOUD_PROVIDER=AZURE|AWS|GCP|ONPREMISE
```

| Service | Azure | AWS | GCP | On-Premise |
|---------|-------|-----|-----|------------|
| Secrets | Key Vault | Secrets Manager | Secret Manager | Env Vars |
| Storage | Blob / R2 | S3 | GCS | MinIO |
| Email | MS Graph | SES | Gmail API | SMTP |
| Calendar | MS Graph | — | Google Calendar | — |
| Identity | Entra ID | Cognito | Workspace | Local JWT |
| Push | FCM | FCM | FCM | FCM |

---

## Security

| Finding | Severity | Status |
|---------|----------|--------|
| XXE in SamlService | Critical | ✅ Fixed |
| Exception message leakage (7 locations) | Critical | ✅ Fixed |
| Missing CSRF protection | Critical | ✅ Fixed |
| Missing tenant query filters (16 tables) | High | ✅ Fixed |
| SAML signature wrapping | High | ✅ Fixed |
| SAML assertion replay | High | ✅ Fixed |
| JWT issuer/audience not enforced | High | ✅ Fixed |
| HS256 / alg:none algorithm allowed | Medium | ✅ Fixed |
| Request body size unlimited | Medium | ✅ Fixed |
| Missing HSTS | Low | ✅ Fixed |
| Swagger in production | Info | ✅ Fixed |
| Hardcoded secrets | Info | ✅ Fixed |
| + 13 more findings | Various | ✅ Fixed |

**25 / 25 findings fixed. `hardcode-audit.sh` exits 0.**

Middleware pipeline (in order): HSTS → HTTPS Redirect → Serilog → Rate Limiting → Auth Pulls → Session Binding → CORS → Authentication → Authorization → Turnstile → ABAC → GlobalException → Health Checks → SignalR → Controllers

---

## Deployment

AegisCore ships with a zero-touch onboarding wizard (`AegisCore.Setup`) that runs on port 9000 and closes itself permanently after go-live.

**11-step wizard:**
```
License Key → Institution Identity → PostgreSQL → Redis → File Storage →
Secrets Provider → Email/SMTP → SMS → Domain & DNS → First Admin → Go Live
```

Every university brings their own infrastructure. AegisCore provisions nothing on your behalf — it connects to what you own.

**Go-live requires 20 checks to pass. No partial deployments.**

```bash
# Staging deployment
git clone git@github.com:AbdulNafaySarmad1/Aegiscore.git
cd Aegiscore
docker build -t aegiscore/api:latest .
docker-compose -f docker-compose.uni.yml up -d

# Open onboarding wizard
open http://SERVER_IP:9000
```

---

## Infrastructure

```yaml
Services (docker-compose.uni.yml):
  aegiscore-api          → .NET 10 API (port 8080)
  aegiscore-setup        → Blazor onboarding wizard (port 9000, setup only)
  aegiscore-postgres     → PostgreSQL 16
  aegiscore-redis        → Redis 7
  aegiscore-elasticsearch→ Elasticsearch 8
  aegiscore-minio        → MinIO (on-premise storage)
  aegiscore-cloudflared  → Cloudflare tunnel (no open ports)
```

CI/CD: GitHub Actions → audit → Docker build → staging deploy → production deploy (manual approval gate)

---

## Requirements

| Component | Minimum Version |
|-----------|----------------|
| .NET SDK | 10.0 |
| PostgreSQL | 14+ |
| Redis | 7+ |
| Docker | 24+ |
| Docker Compose | 2.20+ |

---

## License

AegisCore is proprietary software. License keys are issued exclusively by Apex Systems (Pvt. Ltd.) on a per-institution basis.

**No license key = no deployment.** Contact [licensing@abdulnafaysarmad@gmail.com](mailto:abdulnafaysarmad@gmail.com)

---

## Built By

**Apex Systems (Pvt. Ltd.)**  
Pakistan · Singapore · UAE  


*Currently deployed *

---

<div align="center">
<sub>◆ AegisCore — Because universities deserve better than Oracle</sub>
</div>
