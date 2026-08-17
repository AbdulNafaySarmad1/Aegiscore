# AegisCore CLIENT_DOCUMENT_CUSTOMIZATION.md

Configurable, tenant-specific document generation design. Generated 2026-08-12 on branch `agent/prod-hardening`.
Analysis + design ONLY. No implementation.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

---

## 1. Current document architecture (audited)

| Document | Current approach | Files | Template-able? |
|---|---|---|---|
| Official transcript | PDF built in code (PdfSharpCore) + PQC + QR, synchronous | `StudentDocumentsController.cs:28-109, 237-357` | NO — layout fixed in C# |
| Finance reports (CSV/XLSX/PDF/XBRL/iXBRL) | builders in code, PQC-signed | `FinanceModuleEndpoints.cs:1815-1985` | NO |
| Fee challan | PDF + QR | `StudentDocumentsController`/finance | NO |
| OBE signed report | PDF + PQC + QR | `FacultyObeController` | NO |
| Admit cards / grade sheets / attendance reports / HR docs / certificates | NOT_FOUND as generators | — | N/A |
| WhatsApp templates | JSON file, static | `wwwroot/templates/whatsapp-templates.json` | partial (placeholder strings) |
| Document vault | storage tiers + SHA-512 + RSA-PSS | `DocumentVaultService.cs` | N/A |

**Verdict:** All documents are hard-coded in C#. A university's transcript/letterhead/grading design currently REQUIRES a code change → a fork or a release cycle. This is a direct blocker to zero-fork tenant deployment for document-heavy universities.

---

## 2. Requirements for the template system

Every generated document must preserve:
- document UUID (`Id`)
- tenant ID (`TenantId`)
- student/faculty identity
- issuer identity (who generated/signed)
- timestamp (`GeneratedAt`)
- QR verification payload
- PQC signature (SLH-DSA/ML-DSA)
- audit trail entry
- verification URL
- document version + template version

These are **metadata + signing wrappers** that the generation pipeline adds around any rendered body. The body (layout/content) is what must become configurable.

---

## 3. Template architecture options

| Option | Fit | Analysis |
|---|---|---|
| **HTML/CSS templates** | BEST for pilot | Render to PDF via headless Chromium (Playwright exists in portal for e2e) or a .NET HTML→PDF lib (e.g., `HtmlRenderer`/`PuppeteerSharp`). Rich layout, CSS print support, easy for designers, brandable. HTML is the recommended primary body format. |
| Razor templates | GOOD (in-ecosystem) | Razor views compiled per template = per-template compilation; tenant templates become Razor packages — heavier, more like code; risk of "templates as code." Use for system templates only. |
| Structured JSON (data → layout map) | PARTIAL | good for simple tables/forms (fee challan, grade sheet), poor for free-form branding-heavy docs (transcripts, certificates). Use JSON for data contracts + simple layouts. |
| PDF overlays | OPTIONAL | brand header/footer/letterhead as overlay PDF on top of generated content; useful for logo/letterhead without full re-layout. Complementary, not primary. |
| PDF form templates | NO | PdfSharpCore fixed layouts don't compose well for arbitrary university designs |

**Recommendation:** 
- **Primary:** HTML/CSS template per document per tenant, rendered to PDF (headless Chromium) — designers produce brand-compliant templates without code.
- **Simple tabular docs** (fee challan, grade sheet, admit card): can share the HTML pipeline with a table macro, or use JSON-driven generation.
- **Letterhead/branding:** header/footer overlay assets (logo, seal) configured per tenant.
- Keep the existing PdfSharpCore path as the FALLBACK/legacy generator behind the same orchestration contract so the pipeline is uniform.

### Security constraint on HTML templates
- Templates are tenant-provided HTML → must be sandboxed: no remote resources (or strict allow-list), no scripts, sanitized CSS. Render in a locked-down headless Chromium (or sanitize + use a layout-whitelist). This is a hostile-input boundary if tenants are untrusted; for a managed product, treat templates as semi-trusted with a validation gate (lint on upload).

---

## 4. Target pipeline

```
Domain event / user action
      │
      ▼
DocumentOrchestrator (existing controllers emit intent)
      │  resolves: tenant config → template selection (manifest documents.templates)
      ▼
TemplateResolver (tenant → document type → template version)
      │
      ▼
TemplateRenderer (HTML→PDF headless; fallback PdfSharpCore for legacy)
      │  injects: tenant branding (logo/colors/letterhead), document metadata
      ▼
Data binder: document UUID, tenant ID, student/faculty identity,
             issuer identity, timestamp, document version, template version
      │
      ▼
Signer: QR payload + PQC signature (existing PqcCryptographyService)
      ▼
Storage (IStorageService, tenant prefix) + Audit log
      ▼
Verification URL (existing transcript verify pattern)
```

### Metadata invariant
All bound into the signed payload / QR: `documentUuid, tenantId, issuer, subject, issuedAt, docVersion, templateVersion, contentHash`. Verification recomputes hash + checks PQC signature + looks up the stored verification record — same trust model as today's transcript verify.

---

## 5. What must be built

| Item | Current | Work | Blocker |
|---|---|---|---|
| Template storage (per-tenant, versioned) | NOT_FOUND | template store: object storage or DB, keyed `tenant/slug/doc-type/version` | YES for document-heavy pilots |
| Template resolver (manifest → template) | NOT_FOUND | uses `TENANT_CONFIGURATION` `documents.templates` | YES |
| HTML→PDF renderer (headless Chromium) | NOT_FOUND (portal has Playwright for tests only) | add renderer service (container/edge) | YES for HTML path |
| Branding overlay (logo/letterhead per tenant) | PARTIAL (`BrandingController` single logo) | per-tenant asset bundle | YES |
| Orchestration contract unifying generators | PARTIAL (controllers emit directly) | DocumentOrchestrator interface | P1 |
| Sandbox/validation of uploaded templates | NOT_FOUND | upload lint gate | security P1 |
| Template versioning + verification metadata | NOT_FOUND | add to template store + binder | YES |
| Migration of existing generators (transcript, challan, OBE, finance) onto the contract | IMPLEMENTED directly | refactor behind orchestrator | P2 (keep current output as fallback) |
| New generators: admit cards, grade sheets, attendance reports, certificates, HR docs | NOT_FOUND | build on the template pipeline | P2 (feature work) |

---

## 6. Sequencing

1. **P0:** Template store + resolver + HTML→PDF pipeline for ONE pilot document (transcript is the flagship) + branding overlay + verify/metadata binding.
2. **P1:** Extend to challan, grade sheet, OBE report; template upload/validation/lint gate; template versioning UI (or manifest-driven).
3. **P2:** Admit cards, certificates, HR docs; migrate finance reports; legacy PdfSharpCore behind the same contract.

**Decision:** templates are configuration/assets, NOT code forks. A university sends its transcript template + branding, gets a branded signed document from the same release. This is the document-side of the zero-fork promise.
