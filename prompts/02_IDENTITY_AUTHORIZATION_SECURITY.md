# PROMPT 02 — IDENTITY, AUTHORIZATION, SECURITY

Read and obey `prompts/00_MASTER_BACKEND_ARCHITECT.md` first.

Act as AegisCore's principal identity/security architect. Generate atomic DeepSeek V4 Pro orders under `docs/backend-orders/02-security/`. Do not implement production code.

## Verify current source

Audit authentication, JWT/session handling, refresh rotation, local auth, SAML, OIDC, SCIM, Entra/Google/Okta hooks, Casbin, ABAC, role resolution, tenant context, ownership checks, workflow approvals, upload security, webhooks, audit actor identity, secrets, middleware order, rate limiting, IDOR/BOLA risks, medical/confidential-data boundaries and admin paths.

## Target

Design a production authorization architecture with:

- tenant boundary as invariant;
- RBAC as broad role spine;
- ABAC for context/policy conditions;
- selective ReBAC for object relationships such as faculty→course, HOD→department, requester/assignee→ticket, owner→document;
- workflow/business-state checks;
- strong freshness only for privilege revocation/high-impact decisions where stale claims are dangerous;
- a unified authorization facade so domain code does not hard-wire one policy engine everywhere.

Do **not** introduce OpenFGA/SpiceDB simply because ReBAC exists. First evaluate PostgreSQL/EF-backed relationships and current scale. Preserve an adapter boundary so future replacement is possible.

## Identity federation

Generate orders for a provider-neutral model supporting combinations of:

- local auth;
- generic OIDC;
- SAML 2.0;
- Microsoft Entra ID;
- Google identity;
- Okta/other compliant IdPs;
- JIT provisioning and claim mapping where justified;
- SCIM lifecycle where justified;
- multiple configured IdPs per tenant;
- account linking/conflict handling;
- break-glass local administration.

Login buttons/options must derive from tenant configuration, not hard-coded provider assumptions.

## Confirmed-risk classes to re-verify

Specifically search for and produce orders where source confirms:

- endpoints/services trusting caller-supplied user/student/employee actor IDs instead of authenticated identity;
- resource ownership gaps;
- transcript/document access gaps;
- payroll/grade-appeal/registration authorization gaps;
- stale privileged JWT claims;
- incomplete tenant filtering;
- SAML signature/replay weaknesses;
- webhook replay/signature handling;
- upload/parser abuse;
- data-import SSRF;
- origin abuse controls on auth, uploads, expensive reports/documents and public verification.

Do not recreate aggressive global application rate limiting. Edge protection may exist; origin controls should be targeted and defense-in-depth.

## Sensitive domains

Define stricter authorization/privacy invariants for:

- finance;
- grade changes/finalization;
- HR grievances;
- medical records;
- mental health/counselor records;
- confidential service-desk tickets;
- tenant administration;
- document vault.

## Mandatory tests in generated orders

Include negative authorization tests, tenant A→tenant B access attempts, revoked-role freshness tests, ownership tests, workflow-state tests, forged actor-ID tests, malformed-token tests, provider callback validation and audit actor correctness.

## Final chat response

Return only:

`ORDERS_CREATED`, `CRITICAL_AUTH_GAPS`, `IDENTITY_TARGET`, `AUTHZ_TARGET`, `FIRST_ORDER`, `BLOCKERS`, `PRODUCTION_CODE_CHANGED:NO`.