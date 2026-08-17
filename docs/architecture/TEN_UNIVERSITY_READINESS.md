# AegisCore TEN_UNIVERSITY_READINESS.md

Zero-fork ten-university deployment assessment. Generated 2026-08-12 on branch `agent/prod-hardening`.
Analysis ONLY. No implementation.

Evidence classes: `CONFIRMED_BY_CODE`, `CONFIRMED_BY_CONFIG`, `INFERRED`, `NOT_FOUND`, `MEASUREMENT_REQUIRED`, `CLIENT_INFORMATION_REQUIRED`.

---

## 1. The question

> "If ten universities signed contracts today and wanted direct deployment, could we deploy ten independent AegisCore instances from the same canonical release without creating ten source-code forks?"

**ANSWER: PARTIALLY.**

Ten independent instances from one release: **YES** — the architecture already supports isolated instances from one image (env/config-driven, Docker + compose + k8s). No source fork is required to stand up ten instances today.

Ten **product-configured** instances (branded, identity-wired, document-customized, per-tenant configured) from one release without code changes: **NOT YET** — because tenant configuration, identity, branding, and document templates are not yet manifest/tenant-driven.

**No fork is required for ANY of it** — the missing pieces are product features to be built, not per-university code. The forks that currently happen (hard-coded IoBM branding, fixed document layouts, per-install env sprawl) are consequences of missing product features, not an inherent requirement.

---

## 2. Exact blockers preventing full YES

| # | Blocker | Why it blocks 10-university direct deployment | Category | Fix |
|---|---|---|---|---|
| TB-1 | No tenant manifest (env-var-driven single-instance config) | Each university needs bespoke env mapping + manual setup; no repeatable "profile → deploy" | PRODUCTIZATION | `TENANT_CONFIGURATION.md` (PB-20) |
| TB-2 | `TenantConfigService` not tenant-scoped | Config is instance-global; multi-tenant logical model can't differ per tenant | PRODUCTIZATION | per-tenant config resolution (PB-20) |
| TB-3 | Identity: OIDC stub, SAML signature unverified, no SSO buttons from config | Each university's IdP integration requires new code until the OIDC adapter exists | PRODUCTIZATION + INTEGRATIONS | `IDENTITY_FEDERATION.md` (PB-08, PB-07) |
| TB-4 | Hard-coded IoBM branding in portal | Every new university sees IoBM unless code is edited | PRODUCTIZATION | tenant branding bundle (PB-10, PB-21) |
| TB-5 | No document template system | University-specific transcripts/letterheads require code changes | PRODUCTIZATION | `CLIENT_DOCUMENT_CUSTOMIZATION.md` (PB-37, PB-43) |
| TB-6 | No deployment factory / tenant registry / deployment record | Ten deployments = ten manual operations; no repeatability, no rollback integration, no audit of who runs what | PRODUCTIZATION/DEVOPS | `DEPLOYMENT_FACTORY.md` (PB-40, PB-41) |
| TB-7 | Tenant isolation gaps (~185 entities) | Same-image shared deployment is unsafe for cross-tenant isolation (each instance is per-DB today, but the gap weakens defense-in-depth and blocks a future shared-DB model) | SECURITY | `DATA_MODEL.md` §2 (PB-01) |
| TB-8 | License per-request DB read + external heartbeat | Fail-closed offline behavior is risky for on-prem universities; per-request DB read adds cost ×10 instances | PERFORMANCE | PB-03; offline tolerance design |
| TB-9 | Institutional analytics engine missing | If analytics is a purchase driver, ten universities can't get it yet | PRODUCT | PB-44 |
| TB-10 | HR/faculty UI + admissions UI missing | Some universities will want these on day 1; backend exists, UI doesn't | PRODUCT | PB-42, PB-48 |
| TB-11 | **No unified service desk** | Universities expect staff/student support channels (HR/IT/facilities/finance tickets); only specialized ticket systems exist and no department-agnostic core | PRODUCT | `UNIFIED_SERVICE_DESK.md` (PB-52..PB-57) |

> **ADDED 2026-08-12 (service-desk audit):** TB-11 captures the missing unified service desk (audit + design in `UNIFIED_SERVICE_DESK.md`). It is a commercial-completeness item (like TB-9/TB-10), classified **P2** — not a blocker to ten direct deployments, but required for a credible "university ERP" offering. New ticket entities must be `ITenantScoped` to respect TB-7.

---

## 3. Minimum architectural work to reach YES

Ordered (each unlocks the next):

1. **Tenant manifest + per-tenant config resolution** (TB-1, TB-2) — the keystone. Without this, nothing else scales. (PB-20)
2. **Tenant branding bundle** (TB-4) — visible proof of "same product, different university." (PB-21, PB-10)
3. **OIDC generic adapter + SSO buttons from config** (TB-3) — identity is the hardest integration; solve once, reuse for Entra/Google/Okta. (PB-08; PB-07 for SAML)
4. **Document template pipeline** (TB-5) — flagship transcript first, then challan/grade-sheet/OBE. (PB-37, PB-43)
5. **Deployment factory + deployment record** (TB-6) — repeatability, rollback, audit. (PB-40, PB-41)
6. **Tenant-scoping audit** (TB-7) — completes defense-in-depth; enables optional future shared-DB model. (PB-01)
7. **License lease cache + offline tolerance** (TB-8) — operational safety ×10. (PB-03)
8. **Analytics + HR/admissions UI** (TB-9, TB-10) — commercial completeness. (PB-44, PB-42, PB-48)
9. **Unified service desk** (TB-11) — staff/student support channels from config; reuse WorkflowEngine + maintenance-ticket model. (PB-52..PB-55; consolidation PB-56/PB-57 later)

Estimated shape of the work (no monetary estimates): the 120-day pilot backlog P0+P1 covers items 1-5 and the security/perf items. Items 6-9 are P0/P2. **The ten-university target is reachable within the pilot timeframe's productization scope if P0+P1 are executed.**

---

## 4. Deployment model recommendation for ten universities

- **Isolated instance per university (recommended now):** ten deployments of the same image, each with its own DB + manifest + assets + secrets. Strong isolation, simple ops, no shared-DB risk. The Deployment Factory makes this mechanical.
- **Shared logical tenancy (future, optional):** after TB-7 is resolved, one cluster with tenant-scoped data becomes viable for smaller deployments. NOT recommended as the pilot model.
- Never create per-university forks: every differentiation flows through manifest + assets + templates.

---

## 5. Validation test (design)

For each university in a dry-run of the factory:
1. Create manifest + branding + templates + secrets.
2. Deploy same images.
3. Migrate + bootstrap.
4. Smoke tests: login (local), SSO (if configured), transcript generation, grade entry + approval, OBE report, finance report, verify endpoints.
5. Verify branding is correct, documents render from university templates, audit trail records tenant correctly.
6. Record deployment + rollback check.

PASS = ten independent branded, configured, signed-document-producing instances from one release with no source edits.
