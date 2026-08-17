# AegisCore POLYGLOT_DECISION_MATRIX.md

Polyglot architecture decision record for C#/.NET, R, Go, and Rust. Generated 2026-08-12 on branch `agent/prod-hardening`.
Default classification is `KEEP_DOTNET`. A new runtime requires evidence, not aesthetics.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_MEASUREMENT`, `INFERRED`, `MEASUREMENT_REQUIRED`, `NOT_FOUND`.

---

## 1. Guiding rule

Every new runtime creates: deployment complexity, observability complexity, CI/CD complexity, security-patching requirements, developer complexity, network boundaries, failure modes, serialization overhead, and distributed-tracing requirements.

**The default is `KEEP_DOTNET` for all ERP domain logic.** Current measurements show the bottleneck is the **API process CPU inside .NET** (Argon2 logins, EF SQL logging, synchronous PDF/PQC — `BASELINE_RESULTS.md` §5, §7), NOT a limitation of the .NET runtime itself. A language extraction would NOT fix any measured bottleneck; only code-level optimizations would.

---

## 2. Component-by-component matrix

| COMPONENT | CURRENT LANGUAGE | RECOMMENDED LANGUAGE | CLASSIFICATION | WHY | BENEFIT | NEW COMPLEXITY | EVIDENCE | CONFIDENCE | WHEN TO RECONSIDER |
|---|---|---|---|---|---|---|---|---|---|
| Core ERP domains (student, faculty, admissions, registrar, attendance, grading, exams) | C# | C# | KEEP_DOTNET | Monolith + EF + shared DbContext + tenant filter; extraction breaks tenant/transactional invariants | none (avoid loss) | 0 | `AEGISCORE_TECHNICAL_PROFILE.md` §13 | 95 | Never without a measured bottleneck at a clean boundary |
| OBE attainment | C# | C# | OPTIMIZE_DOTNET | N+1 + in-memory AVG → SQL aggregates in the same EF context | removes hundreds of queries per report | low | `ObeServices.cs:83-126` | 90 | after A2 (SQL aggregation) fails to reach target |
| Grading grace-marks | C# | C# | OPTIMIZE_DOTNET | per-student requery → batch evaluation in .NET | constant queries per batch | low | `GradingService.cs:93-116` | 85 | never |
| Attendance aggregates | C# | C# | OPTIMIZE_DOTNET | COUNT aggregate instead of load-all | removes full row loads | low | `AttendanceRepository.cs:13-24` | 85 | never |
| Finance report builders | C# | C# | OPTIMIZE_DOTNET | single-pass serialization + async off-request; not a runtime problem | latency + thread unblock | medium | `FinanceModuleEndpoints.cs:1815-1985`, LB-04 | 80 | if report volume needs dedicated capacity — that's a scaling/queueing decision, still .NET |
| License middleware/lease | C# | C# | OPTIMIZE_DOTNET | cache/signed-lease in-process or Redis | kills per-request DB read (HP-01) | low | `LicenseService.cs:238-242`, `BASELINE_RESULTS.md` §3 | 90 | never |
| Local auth / Argon2 login | C# | C# | OPTIMIZE_DOTNET | Argon2 is intentionally slow; CPU cost is inherent. Options: reduce EF logging, add rate limits, increase replicas | throughput | low | LB-01, `BASELINE_RESULTS.md` §2 | 85 | never (moving auth to another runtime adds a security boundary for no gain) |
| Statistical / institutional analytics | C# (deleted ns) | C# | KEEP_DOTNET (rebuild) | The deleted `.NET` analytics engine was the right shape; R adds an R runtime + serialization hop for the same statistics | avoids R runtime | 0 vs R | deleted `AegisCore.Infrastructure/Analytics` (tests broken), docs `ANALYTICS_BACKEND_REPORT.md` | 75 | only if heavy statistical model experimentation becomes a core differentiator — then R as an OFF-request worker only |
| Retention/dropout-risk modelling | C# (defaulter ML exists) | C# | KEEP_DOTNET | `DefaulterMlEngine` exists in .NET; prediction is a lightweight model call, not statistical computing | no new runtime | 0 | `ExtendedPlatformControllers4.cs` (`api/defaulter-ml`) | 80 | if deep learning / heavy ML enters scope |
| Cohort / retention / forecasting analytics | — | C# | MEASURE_FIRST | No analytics engine currently ships. Evaluate .NET implementation (recommended) vs R worker | — | — | NOT_FOUND (engine deleted) | 60 | after pilot shows analytics demand |
| Central license authority (future) | — | Go? | MEASURE_FIRST | Lightweight network service, independent scaling/lifecycle — valid GO characteristics but needs license-server traffic data; confidence < 80 | independent scaling | medium | `AEGISCORE_TECHNICAL_PROFILE.md` §15 (GO-1) | 55 | when > N universities generate license traffic and the in-process lease cache (A1) proves insufficient |
| Telemetry ingestion (future) | — | Go? | MEASURE_FIRST | high-volume ingest if ever built; currently NO ingestion exists | — | — | GO-2 (§15) | 40 | only if built and measured > threshold |
| Webhook ingestion | C# | C# | KEEP_DOTNET | existing HMAC+PQC+Redis idempotency already in .NET; low volume for university scale | avoid new boundary | 0 | `FinanceWebhookController.cs` | 85 | never |
| PQC signing/verification | C# | C# | KEEP_DOTNET | BouncyCastle SLH-DSA/ML-DSA in-process; queue exists; moving to Rust adds a crypto boundary with key-distribution complexity and no measured benefit | none | high | `Security/Pqc/*` | 90 | only for a separate high-assurance signer appliance — not justified by data |
| Document PDF/XLSX/OCR parsing | C# | C# | MEASURE_FIRST (Rust conditional) | managed .NET/Azure parsing; hostile-parser hardening in Rust is technically defensible but requires evidence of parser-abuse impact | — | high | SEC-11, `AEGISCORE_TECHNICAL_PROFILE.md` §16 | 45 | after pentest shows exploitable parser bugs with no .NET mitigation |
| Untrusted document parsing | C# | C# | KEEP_DOTNET (harden) | .NET is memory-safe; OWASP hardening (magic bytes, size caps, sandboxed load) is the first response; Rust only if memory-safety is demonstrably required | — | 0 | `UploadValidationService.cs`, SEC-11 | 70 | see RUST_CANDIDATE section |
| Identity (OIDC/SAML/local) | C# | C# | KEEP_DOTNET | ASP.NET auth handlers + existing JWT/Casbin/ABAC stack; provider adapters are HTTP work, .NET-native | — | 0 | `AuthController`, `SamlService`, JwtBearer | 95 | never |
| Notifications (email/WhatsApp/SMS/Teams) | C# | C# | KEEP_DOTNET | provider abstraction is an interface change, not a runtime change | — | 0 | `EmailNotificationService.cs` | 95 | never |
| Payment gateways (RAAST/Stripe/etc.) | C# | C# | KEEP_DOTNET | typed adapters + IHttpClientFactory + Polly already present; adding Go = new payment-processing boundary | — | 0 | `PaymentGatewayAdapters.cs` | 95 | never |
| Frontend (Next.js) | TS | TS | KEEP | react-query/SWR + consolidation is the fix, not a rewrite | — | 0 | `ROUTE_MATRIX.md` §4 | 90 | never |
| Search (Elasticsearch) | C# (NEST EOL) | C# | OPTIMIZE_DOTNET | migrate NEST 7.17.5 → Elastic.Clients.Elasticsearch | removes EOL dependency | low | `docs/DEPENDENCY_AUDIT.md`, SEC-18 | 90 | never |
| Infrastructure/edge | Shell/YAML/Caddy | — | KEEP | not a code-runtime decision | — | — | `deploy/`, `k8s/` | 90 | n/a |

---

## 3. R analytics engine assessment

Candidate classification per `§12` of the brief:

| Candidate | Classification | Rationale |
|---|---|---|
| Institutional statistics | KEEP_DOTNET | plain aggregates; .NET SQL handles it |
| Student-performance statistics | KEEP_DOTNET | same |
| Cohort analysis | KEEP_DOTNET | SQL/window functions |
| Retention analysis | KEEP_DOTNET | counts/cohort ratios |
| Dropout-risk modelling | KEEP_DOTNET | existing `DefaulterMlEngine` in .NET |
| OBE statistics | KEEP_DOTNET | CLO/PLO attainment → SQL aggregates (A2) |
| CLO/PLO statistical analysis | KEEP_DOTNET | above |
| Forecasting (enrolment/merit) | KEEP_DOTNET | linear/time-series in .NET is sufficient at pilot scale |
| Admissions/merit analytics | KEEP_DOTNET | weighted scoring already in .NET |
| Cafeteria demand forecasting | KEEP_DOTNET | simple seasonal model; current service exists (`CafeteriaService`) |
| Resource-demand forecasting | KEEP_DOTNET | same |
| Management analytics | KEEP_DOTNET | dashboards in portal |
| Research reporting | KEEP_DOTNET | document pipeline in .NET |
| Statistical experimentation | R_CANDIDATE (off-path only) | ONLY if heavy model prototyping is a product differentiator; still `MEASURE_FIRST` today |

**Decision:** No R worker is justified today. If one ever is, the only acceptable shape is:

```
.NET API → queue/background job → R worker → result persisted → .NET API → portal/BI
```

R must NOT be on latency-sensitive HTTP paths. The current deleted .NET analytics engine was the correct shape; rebuild it in .NET.

---

## 4. Go candidates

Per brief §13. `MEASURE_FIRST` unless evidence exists.

| CANDIDATE | CURRENT IMPL | WHY EXTRACT | SCALING BOUNDARY | DEPLOYMENT BOUNDARY | LATENCY BENEFIT | MEMORY BENEFIT | FAILURE ISOLATION | NETWORK COST | CONSISTENCY COST | OPS COMPLEXITY | MEASUREMENT SUPPORTING | CONFIDENCE |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Central license authority | .NET heartbeat + in-process middleware; license server is external HTTPS | independent scaling/lifecycle of license service | many-university license traffic | separate service deployment | none on request path (lease cache does that) | minimal | isolate license outage from ERP | adds network hop for heartbeats (already external) | license state consistency between authority and ERP | new service + secrets + DNS | none (no license-server traffic data) | 55 → MEASURE_FIRST |
| Telemetry ingestion | NOT_FOUND | high-volume ingest if built | > millions events/s (unlikely at university scale) | separate service | — | — | — | — | — | — | none | 40 → MEASURE_FIRST |
| Deployment heartbeat / tenant registry | NOT_FOUND (deploy scripts only) | future factory telemetry | — | — | — | — | — | — | — | — | none | 35 → MEASURE_FIRST |
| Webhook ingestion | .NET `FinanceWebhookController` + Redis idempotency | NOT justified | low volume | — | none | none | none | none | — | — | none | 15 → DO NOT EXTRACT |

**Verdict:** No Go extraction is justified by any measurement today. `MEASURE_FIRST` applies to license authority and (if ever built) telemetry ingestion.

---

## 5. Rust candidates

Per brief §14. .NET is memory-managed; "security" alone is not justification.

| CANDIDATE | CLASSIFICATION | WHY |
|---|---|---|
| Untrusted document parsing (PDF/XLSX/OCR) | MEASURE_FIRST (conditional RUST_CANDIDATE) | Only if pentest (STRIX) demonstrates exploitable parser bugs that cannot be mitigated in .NET. Current parsers are managed (PdfSharpCore) or Azure-managed (DocumentIntelligence). |
| PQC signing/verification service | KEEP_DOTNET | BouncyCastle in-process; no measured need for a separate crypto boundary |
| Cryptographic boundary / native security component | KEEP_DOTNET | .NET CSP/BouncyCastle already high-assurance; Rust adds key-distribution + interface complexity with zero measured benefit |
| Secure deployment agent | MEASURE_FIRST | if an agent is ever built, a small Rust agent is plausible, but no agent exists today |
| Signature verification | KEEP_DOTNET | ledger verifier is .NET (`AegisCore.LedgerVerifier`) and works |
| Hardware/IoT security | DO_NOT_EXTRACT | no hardware/IoT in scope |
| Sandboxed parser | MEASURE_FIRST | same as document parsing — evidence first |

**Verdict:** No Rust extraction justified today. If the STRIX pentest surfaces an exploitable parser path with no .NET mitigation, a **single high-assurance Rust parser boundary** (PDF or XLSX decompressor) becomes the one defensible extraction — but it must be proven, not assumed.

---

## 6. Hard rules

1. No ERP domain extraction to Go/Rust/R without a measured bottleneck at a clean boundary.
2. R only as an off-request analytics worker behind a queue, and only if analytics demand justifies it.
3. Go only for an independent lightweight network service (license authority is the only plausible one; MEASURE_FIRST).
4. Rust only for a hostile-input boundary with proven exploit + no .NET mitigation.
5. Re-visit this matrix after the pilot, after OTel instrumentation, and after the STRIX pentest — the evidence base will be stronger then.
