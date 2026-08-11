# AegisCore FILE_UPLOAD_FORMATS.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
Inventory of every upload/import surface. Status: IMPLEMENTED / PARTIAL / PROPOSED / UNKNOWN.

Global notes: Kestrel `MaxRequestBodySize` 50MB (`Program.cs:56`). Shared validator `AegisCore.Infrastructure/Security/UploadValidationService.cs` — `SanitizeFileName` (14-26), `HasDirectoryTraversal` (28-36), `ValidateMagicBytes` (38-65, PNG/JPEG/PDF magic bytes), default 10MB limit (line 9). Storage: `AEGISCORE_STORAGE_LOCAL_PATH`/`Storage:LocalPath`/`storage` (local) or R2/S3/Azure/GCP providers.

## 1. Upload surfaces

| Surface | Endpoint | Formats | Size limit | Validation | Commit | Status |
|---|---|---|---|---|---|---|
| Document vault | `POST /api/document-vault/upload` | any (tiered: Degree/Certification/Financial/Grade/Quiz/Notice/Attendance/ProfilePicture/General) | `[RequestSizeLimit(50MB)]` (`DocumentVaultController.cs:49`) | none beyond Kestrel cap (no magic-byte on generic vault) | tiered retention (Critical 50y/Important 20y/Standard 5y), SHA-512+RSA-PSS "PQC simulation", object-lock, access log | IMPLEMENTED |
| Profile picture | `POST /api/profile-pictures/upload` | JPEG/PNG | 5MB | magic bytes + face-presence heuristic + editing heuristic (entropy) | stored `profiles/{tenantId}/{userId}/{ts}.jpg` | IMPLEMENTED |
| Student avatar | `POST /api/student-profile-media/me/avatar` | image | (portal FormData) | service-side | student profile media | IMPLEMENTED |
| Branding | `POST /api/wizard/upload-branding/{type}` | image | 10MB | `UploadValidationService` (`WizardController.cs:153-163`) | storage + secret | IMPLEMENTED |
| Grade sheet (Excel/PDF/CSV manual) | `POST /api/faculty-grade-entry/import-preview` + `import-commit`; template `GET .../template.csv` | CSV / XLSX / PDF | Kestrel cap | preview→commit two-phase; format templates | grades + moderation | IMPLEMENTED |
| OCR upload | `POST /api/faculty-grade-entry/ocr-upload` | PDF/images | `[RequestSizeLimit]` | Azure DocumentIntelligence parse | grades | IMPLEMENTED |
| CIS evidence (OBE) | `POST /api/obe/v2/cis/:id/upload` + `/process` | doc/PDF | — | upload→process pipeline | CLO mapping, attainment | IMPLEMENTED |
| Grade appeal attachment | `POST /api/grade-appeals` | PDF/DOC | — | FormData | appeal record | IMPLEMENTED |
| Proctoring face | `POST /api/proctoring/face-analysis` | JPEG/PNG | ≤10MB | none (face-analysis endpoint) | analysis record | IMPLEMENTED |
| Voice audio | `POST /api/voice/audio` | WAV/MP3 | ≤10MB | none | transcript record | IMPLEMENTED |
| Setup wizard templates | `POST /api/setup/enhanced/uploads/:type/validate`; `GET /api/setup/enhanced/templates/:key` | XLSX/CSV | — | validate endpoint | setup import | IMPLEMENTED |
| Voucher attachments | `/api/finance/v2/vouchers/{id}/attachments` | any | — | `IR2StorageService` + PQC | voucher attachment | IMPLEMENTED |
| Vendor documents | `/api/finance/v2/vendors/{id}/documents` | any | — | R2 + PQC | vendor doc | IMPLEMENTED |

## 2. Import surfaces (structured)

| Import | Endpoint | Expected fields/columns | Validation | Status |
|---|---|---|---|---|
| Data import (external DB) | `/api/data-import/*` connections/jobs/templates | table/column mapping, `IsQuerySafe` guard (`DataImportController.cs:21-34`) | SQL-safety check; ADO.NET external conn (30s) | IMPLEMENTED |
| CSV format templates | `GET /api/data-import/formats/:fileName` | served from `/formats/` dir; strict UTF-8, ISO 8601 dates, plain digits, all-or-nothing (per `formats/README.txt`) | all-or-nothing; bad rows rejected | IMPLEMENTED |
| Catalog ingestion | `/api/catalog/ingest` + jobs | course catalog records | job pipeline + review | IMPLEMENTED |
| Demo data import | `POST /api/wizard/import-demo-data` | golden demo manifest (`scripts/demo-data-manifest.md`) | demo only | IMPLEMENTED |

## 3. Export / download surfaces

| Export | Endpoint | Formats | PQC/QR | Status |
|---|---|---|---|---|
| Official transcript | `POST /api/student-documents/transcript/official` → `GET .../download` | PDF | SLH-DSA sign + QR | IMPLEMENTED |
| Transcript verify | `GET .../verify` | HTML/JSON | hash verify | IMPLEMENTED |
| Finance reports | `POST /api/finance/v2/reports/{type}/signed-csv\|xlsx\|pdf\|xbrl\|ixbrl` | CSV/XLSX/PDF/XBRL/iXBRL | PQC-signed + QR (PDF) | IMPLEMENTED |
| Fee challan | `POST /api/student-portal/finance/challan/:demandId` | PDF | QR | IMPLEMENTED |
| OBE signed report | `POST /api/obe/faculty/course/:id/signed-report` | PDF | PQC + QR | IMPLEMENTED |
| Grade-sheet template | `GET /api/faculty-grade-entry/course/:id/template.csv` | CSV | — | IMPLEMENTED |
| Data-import templates | `GET /api/data-import/formats/:name` | CSV | — | IMPLEMENTED |
| HEMIS export | `S5ObeController.Hemis` | Excel workbook (design) | — | PARTIAL/PROPOSED (S12 design; endpoint exists in S5Obe) |
| Audit archive | `Security/LoginAuditService` | NDJSON to storage | — | IMPLEMENTED |
| Backup (DR) | Quartz `AutomatedBackupJob` + WAL | archive | object-lock | IMPLEMENTED |

## 4. Format specifications (CSV, from `formats/README.txt`)

- Strict UTF-8 without BOM; ISO 8601 dates; plain-digit numerics; all-or-nothing validation (bad row → whole file rejected).
- Individual format templates enumerate exact columns per template file in `/formats/`.

## 5. Gaps / notes

- Document vault generic upload has **no content validation** (only size) — intended for any document type but is a parser/abuse surface (SEC-11).
- Portal `document-vault` upload sends **no auth header** (raw fetch relying on cookies) — runtime-verify auth path (`ROUTE_MATRIX.md` §6).
- HEMIS export endpoint behavior is PARTIAL — verify actual workbook generation vs design-only.
- Retention/storage: vault tiers (50/20/5y), transcripts under `official-transcripts/{tenantId}/`, audit archive NDJSON, WAL backups in object storage.
