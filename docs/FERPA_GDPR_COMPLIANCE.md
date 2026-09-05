# FERPA and GDPR compliance operations

AegisCore provides FERPA- and GDPR-oriented controls as opt-in compliance modules. Their presence does not assert that an institution is legally subject to either regime, establish a lawful basis for processing, or certify compliance. Before using them, the institution must approve its policy, assign the `System.Admin` permission, define data categories and exact consent text, confirm its identity-to-student/faculty mappings, and validate retention requirements with its privacy or legal team.

## FERPA-oriented controls

When the institution uses the protected grade, attendance, transcript, and admission APIs, `RequireFerpaAudit` records permitted and denied access and enforces the configured admin, self, applicant, or assigned-faculty relationship. The administrative report is `GET /api/admin/ferpa-audit`. IP addresses are encrypted at rest.

Directory publication is also an institutional choice. Setting `Student.DirectoryOptOut` removes that student from search results and from subsequent index/rebuild operations. Integrators that publish a separate directory must enforce the same field.

## GDPR-oriented controls

Consent is explicit and is never inferred by the application. A consent record stores the exact text shown to the user, its category, timestamp, optional expiry, and an encrypted source IP. Erasure requests have a 30-day deadline and are processed by the compliance worker. DSAR exports contain the five documented files and expire after 24 hours.

The worker applies retention at 02:00 in each tenant's configured time zone. `compliance.audit_retention_years` defaults to six years; completed erasure-request metadata uses a fixed seven-year compliance retention period. Institutions must confirm these periods before production use.

## Licence heartbeat disclosure and offline mode

Unless offline licence mode is enabled, the scheduled licence heartbeat sends an HTTPS POST to `License:HeartbeatUrl`, `License:ServerUrl`, or `LICENSE_SERVER_URL`. The JSON payload contains only:

- `university_id`
- `university_name`
- `current_user_count`
- `suite` (currently `complete`)
- `timestamp`

When configured, the request also carries `X-AegisCore-License-Token` as an authentication header. It does not send student, employee, finance, grade, attendance, consent, or audit-record data.

For a disconnected installation, set either `License__OfflineMode=true` or `LICENSE_OFFLINE_MODE=true`. The daily job then validates the signed licence locally, creates no HTTP client, and transmits no heartbeat payload. A valid local HMAC verification secret remains required. Server-side revocation changes cannot reach a disconnected installation; operators must import renewed/revoked licence state through their approved offline process.

## Optional Cloudflare tunnel

The setup service no longer assumes Cloudflare Tunnel. `ProvisionDomain(..., useCloudflareTunnel: false)` leaves all tunnel identifiers unset and instructs the institution to point DNS at its own ingress, load balancer, or public IP. Passing `useCloudflareTunnel: true` explicitly creates the Cloudflare CNAME plan. Generated environment output includes a tunnel ID only for that explicit choice.

All infrastructure remains institution-owned. These options do not introduce an AegisCore- or Nocturne-hosted data dependency.
