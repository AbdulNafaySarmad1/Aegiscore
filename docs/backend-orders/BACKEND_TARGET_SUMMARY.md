# Backend Target Summary

AegisCore will remain a .NET/PostgreSQL modular monolith unless measured evidence justifies an independent service boundary. The target is one canonical release that deploys many universities through tenant manifests, provider adapters, secret references, and deployment profiles rather than source forks.

## Target Shape

- PostgreSQL remains canonical, with explicit tenant ownership, tenant-qualified keys, constraints, transaction-safe RLS context, and migration ownership.
- Authentication supports local plus configured OIDC/SAML providers; authorization combines RBAC, contextual policy, object relationships, ownership, and workflow state behind one facade.
- Student, academic, finance, health, library, HR, service-desk, and facilities domains have one source of truth per business concept.
- Space and scheduled occupancy are canonical across timetable, exams, meetings, events, and maintenance.
- Documents use one tenant-safe storage/template/generation/verification platform while preserving domain authorization.
- Redis or Valkey provides compatible distributed state behind a product-neutral boundary.
- Docker and Podman run the same OCI images; Kubernetes is an optional profile, not a code fork.
- Heavy reports, documents, imports, floor-plan extraction, and bulk notifications execute through bounded, durable, idempotent jobs.
- OpenTelemetry, health, backup/restore, release metadata, migration coordination, and tested rollback are release requirements.

## Deliberate Non-Goals

- No database portability abstraction without evidence.
- No OpenFGA/SpiceDB introduction before PostgreSQL-backed relationships prove insufficient.
- No Go/Rust/R extraction without measured CPU, memory, isolation, or ecosystem justification.
- No ServiceNow clone, national EHR, or bank-grade product scope beyond university operational needs.
- No frontend beautification during backend transformation.
