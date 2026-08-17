# AegisCore Backend Transformation Prompt Pack

This folder is the control-plane for turning the current AegisCore backend into a production-grade, repeatably deployable university ERP backend without spending premium-model tokens on bulk coding.

## Operating model

1. **V4 Flash / Graphify = scout** — repository mapping, evidence gathering, gap discovery. It does not implement production code.
2. **GPT-5.6 Sol / Opus 5 = chief architect** — reads repository intelligence + current source, makes decisions, and writes atomic implementation orders.
3. **DeepSeek V4 Pro = implementation worker** — executes one approved order at a time. It is not allowed to redesign the order or improvise architecture.
4. **QA model = independent release gate** — verifies the implemented backend as a black box and against source-level invariants before frontend polish begins.
5. **Frontend model/team** comes after the backend contracts and capabilities are stable.

## Run order

Use these prompts in sequence:

- `00_MASTER_BACKEND_ARCHITECT.md`
- `01_PLATFORM_TENANCY_PROVIDER_DEPLOYMENT.md`
- `02_IDENTITY_AUTHORIZATION_SECURITY.md`
- `03_STUDENT_ACADEMICS_OBE_EXAMS.md`
- `04_FINANCE_PAYMENTS_PROCUREMENT.md`
- `05_HEALTH_WELFARE_MEDICAL.md`
- `06_LIBRARY_DOCUMENTS_KNOWLEDGE.md`
- `07_HR_FACULTY_SERVICE_DESK.md`
- `08_FACILITIES_CAMPUS_OPERATIONS.md`
- `09_INTEGRATIONS_OPERABILITY_RELEASE.md`
- `10_DEEPSEEK_V4_PRO_EXECUTION_CONTRACT.md`
- `11_QA_BACKEND_RELEASE_GATE.md`

## Non-negotiable product target

AegisCore is a **configurable university ERP product**, not a one-client source tree. Ten universities should be deployable from one canonical release with configuration/provider/deployment profiles instead of ten forks.

Customization must include reasonable provider substitution, for example:

- Redis or Valkey through a cache/distributed-state abstraction where semantics permit.
- Docker or Podman through OCI-compatible images and deployment tooling that does not hard-code Docker-only behavior.
- S3 / Cloudflare R2 / Azure Blob / GCS / MinIO where the storage abstraction supports them.
- Local auth / OIDC / SAML / Entra / Google / Okta through standards/provider adapters.
- SMTP / Microsoft Graph / other mail adapters.
- AWS / Azure / GCP / private cloud / on-prem deployment profiles.

Do not create abstraction theatre: PostgreSQL remains the canonical database unless there is a demonstrated reason to support another engine.

## Evidence discipline

The repository source is ground truth. Existing Graphify and `docs/architecture/` artifacts are navigation/intelligence, not proof that a feature works.

Every architect-generated implementation order must classify concrete symbols as one of:

- `VERIFIED_EXISTING`
- `TO_BE_CREATED`
- `TO_BE_MODIFIED`
- `TO_BE_MIGRATED`
- `TO_BE_DEPRECATED`
- `TO_BE_REMOVED`

An order must never tell the worker to modify a class, method, route, table, or interface that was not verified in current source.

## Where generated implementation orders go

The architect should write generated orders under:

`docs/backend-orders/`

Recommended structure:

- `00-master/`
- `01-platform/`
- `02-security/`
- `03-academics/`
- `04-finance/`
- `05-health/`
- `06-library-documents/`
- `07-hr-service-desk/`
- `08-facilities/`
- `09-operability/`
- `corrections/`
- `qa/`

The implementation worker should not modify the prompt pack.

## Definition of success

Backend readiness does **not** mean merely `dotnet build` returning 0. The target backend must be correct, tenant-safe, secure, observable, recoverable, provider-configurable, migration-safe, concurrency-safe, repeatably deployable, and deep enough that a frontend team can expose the university's workflows without inventing missing business logic.
