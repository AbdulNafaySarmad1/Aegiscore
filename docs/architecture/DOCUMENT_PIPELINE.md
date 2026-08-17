# AegisCore DOCUMENT_PIPELINE.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
End-to-end document workflows: accepted formats, fields, validation, preview/commit, authorization, auditability, PQC/signature, QR, retention/storage. Status: IMPLEMENTED / PARTIAL / PROPOSED / UNKNOWN.

## P1 — Official Transcript (IMPLEMENTED)

1. **Trigger**: Student `POST /api/student-documents/transcript/official` (`StudentDocumentsController.cs:28-109`), `[R=Student,SuperAdmin,Admin]`.
2. **Data**: transcript rows from Result/Grade records (Student not tenant-filtered directly; scoped via parent).
3. **Generation**: `BuildTranscriptPdf` PdfSharpCore (`:237-357`).
4. **Signature**: SLH-DSA archival PQC (`:334-343`); QR-encoded.
5. **Storage**: local disk `official-transcripts/{tenantId}/` (`:176-215`).
6. **Download**: `GET transcript/{documentId}/download` `[R=Student,...]` (`:112-130`).
7. **Verify**: `GET transcript/{documentId}/verify` `[Anon]` (`:133-158`) — hash/QR check vs stored `VerificationHash` (UNIQUE index).
8. **Audit**: generation/access logged (document + access log).
9. **Status**: IMPLEMENTED. Risk: synchronous generation (R8).

## P2 — Finance signed reports (IMPLEMENTED)

1. **Trigger**: `POST /api/finance/v2/reports/{type}/signed-{csv,xlsx,pdf,xbrl,ixbrl}` (`FinanceModuleEndpoints.cs:329-428`).
2. **Data**: `BuildReportAsync` (`:1804-1813`) — trial-balance/cash-position/financial-position/profit-loss/receivables-aging.
3. **Builders**: `ReportCsv` (`:1815-1840`), `ReportXlsx` hand-built OOXML zip (`:1845-1872`), `ReportPdf` (`:1892-1921`), `ReportXbrl`/iXBRL (`:1955-1985`). All **serialize full report then re-parse via JsonDocument** (double allocation; LB-04).
4. **Signature**: PQC + QR (PDF) (`:1895`), stored via `IR2StorageService` (`:1923-1953`).
5. **Authorization**: `RequireAuthorization()` on the `/api/finance/v2` group; role attributes.
6. **Auditability**: reports PQC-signed; ledger hash-chain + audit seals via workers.
7. **Status**: IMPLEMENTED. Risks: synchronous build, double serialization, per-doc signing.

## P3 — Fee challan (IMPLEMENTED)

1. `POST /api/student-portal/finance/challan/:demandId` → PDF + QR → `apiDownload` in `student-payment-action.tsx:42-56`.
2. Student-scoped demand; download allowed for owner role.
3. Status: IMPLEMENTED.

## P4 — OBE signed report (IMPLEMENTED)

1. `POST /api/obe/faculty/course/:id/signed-report` (`FacultyObeController`) → PDF + PQC + QR.
2. Underlying attainment computed by `OBEAttainmentService` (HP-03 N+1).
3. Status: IMPLEMENTED. Risk: compounding hot path (T10).

## P5 — CIS evidence processing (OBE) (IMPLEMENTED)

1. `POST /api/obe/v2/cis/:id/upload` + `/process` (`S5ObeController`): upload evidence → process → `ApproveCis`, `MapCloToPo`, attainment.
2. Status: IMPLEMENTED. Parser surface: document processing (SEC-11).

## P6 — Grade sheet import (manual/Excel/PDF/OCR) (IMPLEMENTED)

1. Faculty `POST /api/faculty-grade-entry/import-preview` → server parses CSV/XLSX/PDF (or OCR via Azure DocumentIntelligence) → returns preview rows → `import-commit` two-phase (`examinations/page.tsx:85-113`).
2. Format: template `GET .../template.csv`.
3. Validation: preview→commit gate; template-based.
4. Status: IMPLEMENTED. Parser surface: XLSX OOXML + PDF + OCR (SEC-11).

## P7 — Document vault (IMPLEMENTED)

1. `POST /api/document-vault/upload` 50MB, any type, tiered retention (Critical 50y / Important 20y / Standard 5y) (`DocumentVaultService.cs:38-43`), SHA-512 + RSA-PSS "PQC simulation" (`:235-242`), object-lock, access logs.
2. Authorization `[A]`; tier assignment server-side.
3. Status: IMPLEMENTED. Note: no content-type validation beyond size (SEC-11); portal upload sends no auth header (ROUTE §6).

## P8 — HEMIS export (PARTIAL/PROPOSED)

- `S5ObeController.Hemis` + `HemisExportService` (`ObeServices.cs:129-148`) — one-click Excel workbook mapping AegisCore fields to HEC HEMIS (S12 design).
- Status: verify actual workbook generation; mark PARTIAL until confirmed.

## P9 — Auditor evidence (IMPLEMENTED)

- `/api/auditor/*` (opaque token) — controls, samples, ledger, evidence download (`auditor-portal.tsx:52-57`).
- Status: IMPLEMENTED.

## Cross-cutting document pipeline properties

| Property | Behavior |
|---|---|
| Accepted formats | PDF (PdfSharpCore), CSV, XLSX (hand-built OOXML), XBRL/iXBRL, images (PNG/JPEG magic-byte), audio (WAV/MP3), docs (grade appeal) |
| Validation | magic bytes (images/PDF), traversal sanitization, size limits per surface, preview/commit for imports; document-vault = size only |
| Preview behavior | grade import preview, catalog review, data-import table mapping |
| Commit behavior | two-phase for imports; direct for most; append-only guards for ledger |
| Authorization | role attributes; verify/status endpoints anonymous |
| Auditability | AuditLog service, access logs, finance audit seals, PQC signatures |
| PQC/signature | SLH-DSA/ML-DSA signatures on docs/ledger; hybrid KEM transport on finance |
| QR | transcripts, challan, finance PDF, OBE reports |
| Retention/storage | vault tiers, transcript tenant folders, NDJSON audit archive, WAL + object-lock backups |

## Pipeline risks

1. Synchronous generation (R8) — transcript/OBE/finance reports block request thread.
2. Double serialization in report builders (LB-04).
3. OBE attainment N+1 upstream of report generation (HP-03).
4. Parser abuse surface on uploads (SEC-11).
5. HEMIS export status ambiguous (P8).
