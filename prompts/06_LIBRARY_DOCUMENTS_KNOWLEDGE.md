# PROMPT 06 — LIBRARY, DOCUMENTS, KNOWLEDGE

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as principal library/document backend architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/06-library-documents/`. Do not implement production code.

## Library standard

Treat Library as a genuine circulation/catalogue subsystem, not merely a `Book` CRUD table.

Audit and complete where required:

- catalogue/title/edition/copy-item distinctions;
- ISBN/identifiers;
- authors/publishers/subjects/categories;
- shelf/location;
- barcode/QR;
- copy availability/status;
- checkout/borrow;
- return;
- renewal;
- hold/reservation queue;
- due dates;
- overdue/fines;
- lost/damaged/replacement;
- waivers/adjustments with audit;
- student/faculty borrowing policies and limits;
- circulation history;
- inventory/stocktake;
- search/filter/pagination;
- e-resources/digital links where licensed;
- notifications;
- reports/analytics.

A student backend must be able to answer reliably:

- What do I currently have checked out?
- What is overdue?
- What fines do I owe?
- Can I renew?
- Can I place/cancel a hold?
- Where is the physical copy?
- When is a held copy expected back?
- What have I borrowed historically?

## Document platform standard

Audit and complete the common document pipeline:

- upload/storage abstraction;
- authorization/ownership;
- MIME + magic-byte validation;
- parser hardening;
- metadata;
- document type/version;
- immutable/verifiable identifiers;
- QR verification;
- PQC signature metadata where applicable;
- audit;
- retention policy;
- download authorization;
- template versioning;
- async generation for expensive documents.

Universities must be able to provide their own templates for transcripts, challans, grade sheets, admit cards, certificates, OBE reports, attendance reports, HR forms and other documents without client-specific source forks.

## Template architecture

Generate orders for a provider-neutral, versioned template pipeline. Determine from current source whether HTML/CSS/Razor/structured JSON/PDF overlay or a hybrid is best. Preserve canonical data and verification metadata independently from presentation.

Every generated document should be able to record, as applicable:

- TenantId;
- Document UUID;
- subject/student/employee identity;
- issuer/actor;
- generated timestamp;
- template ID/version;
- verification URI/QR;
- PQC/signature/hash metadata;
- audit/correlation ID.

## Drawing/document distinction

Facilities floor-plan ingestion is owned by Prompt 08, but common upload/storage/parser-security primitives may be shared. Do not duplicate storage/upload infrastructure.

## Tests

Orders must include circulation concurrency tests (same copy cannot be checked out twice), hold-queue ordering, fine calculation policy tests, ownership/privacy, cross-tenant access, malformed upload/parser tests, template version regression, and document verification tests.

## Final chat response

Return only:

`ORDERS_CREATED`, `LIBRARY_COMPLETENESS_CURRENT`, `DOCUMENT_PIPELINE_CURRENT`, `P0_GAPS`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.