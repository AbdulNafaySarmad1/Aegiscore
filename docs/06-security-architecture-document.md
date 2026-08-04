# Document 6 — Security Architecture Document
**Standards:** ISO/IEC 27001 + NIST CSF + OWASP ASVS Level 2

| Field | Value |
|-------|-------|
| Document ID | AEGIS-SEC-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. Information Security Policy Statement

Nocturne Systems (Pvt.) Ltd. is committed to protecting the confidentiality, integrity, and availability of all information assets entrusted to AegisCore by Pakistani public universities. This Security Architecture Document defines the controls, processes, and technologies implemented to achieve security objectives aligned with ISO/IEC 27001:2022, the NIST Cybersecurity Framework, and OWASP Application Security Verification Standard (ASVS) Level 2.

The security architecture is founded on the following principles:

1. **Defence in Depth:** Multiple layers of security controls are implemented such that the failure of any single control does not result in a security breach.
2. **Least Privilege:** Users and services are granted the minimum permissions necessary to perform their functions.
3. **Secure by Default:** All system components are configured with security-maximising defaults; insecure options must be explicitly enabled with justification.
4. **Privacy by Design:** Personal data protection is embedded into the architecture from inception, not added as an afterthought.
5. **Continuous Verification:** Security controls are continuously tested, monitored, and improved.

## 2. Asset Inventory and Classification

### 2.1 Data Assets

| Asset | Classification | Confidentiality | Integrity | Availability | Owner |
|-------|---------------|-----------------|-----------|--------------|-------|
| Student PII (name, CNIC, contact, academic records) | Restricted | High | High | High | Registrar |
| Faculty PII (name, employee ID, contact) | Restricted | High | High | Medium | HR Department |
| Financial transaction records | Restricted | High | High | High | Finance Officer |
| Authentication credentials (password hashes, MFA secrets) | Critical | Critical | Critical | High | Security Team |
| Audit logs | Restricted | High | Critical | Medium | Security Team |
| ML model parameters | Internal | Medium | High | Medium | Data Science Team |
| System configuration | Internal | High | High | High | System Administrator |
| Anonymised analytics data | Internal | Low | Medium | Medium | Analytics Team |

### 2.2 Infrastructure Assets

| Asset | Classification | Criticality |
|-------|---------------|-------------|
| Hetzner Helsinki EPYC Server (Primary) | Critical | Critical |
| Hetzner Nuremberg EPYC Server (DR) | Critical | Critical |
| Azure App Service (Staging/CI/CD) | High | High |
| PostgreSQL Database | Critical | Critical |
| Redis Cache | High | High |
| Infisical Secrets Manager | Critical | Critical |
| GitHub Actions CI/CD Pipeline | High | High |
| nginx Reverse Proxy | High | High |

### 2.3 Application Assets

| Asset | Classification | Criticality |
|-------|---------------|-------------|
| AegisCore API Server (.NET 10) | Critical | Critical |
| Blazor WebAssembly Frontend | High | High |
| Casbin ABAC Engine | Critical | Critical |
| Argon2id Authentication Module | Critical | Critical |
| SignalR Notification Hub | Medium | Medium |

## 3. Threat Model (STRIDE Methodology)

### 3.1 Spoofing Threats

| ID | Threat | Affected Component | Severity |
|----|--------|-------------------|----------|
| T-001 | Attacker impersonates a legitimate user via credential theft | Authentication Module | Critical |
| T-002 | Attacker forges JWT tokens to gain unauthorised access | Authentication Module | Critical |
| T-003 | Attacker spoofs tenant context to access cross-tenant data | Tenant Resolution Middleware | Critical |

### 3.2 Tampering Threats

| ID | Threat | Affected Component | Severity |
|----|--------|-------------------|----------|
| T-004 | Attacker modifies student grades or academic records | Student Lifecycle Module | High |
| T-005 | Attacker alters financial transaction records | Finance Module | Critical |
| T-006 | Attacker tampers with audit logs to conceal malicious activity | Audit Logging System | Critical |
| T-007 | Attacker modifies Casbin policies to escalate privileges | Authorisation Module | Critical |

### 3.3 Repudiation Threats

| ID | Threat | Affected Component | Severity |
|----|--------|-------------------|----------|
| T-008 | User denies performing a sensitive action (grade change, fee refund) | Audit Logging System | High |
| T-009 | Administrator denies making a policy change | Audit Logging System | High |

### 3.4 Information Disclosure Threats

| ID | Threat | Affected Component | Severity |
|----|--------|-------------------|----------|
| T-010 | Attacker accesses student PII through SQL injection | PostgreSQL Database | Critical |
| T-011 | Attacker intercepts data in transit via man-in-the-middle attack | Network Communication | Critical |
| T-012 | Attacker extracts data from Redis cache | Redis Cache | High |
| T-013 | Attacker accesses cross-tenant data through application bug | Multi-Tenant Architecture | Critical |
| T-014 | Attacker extracts secrets from Infisical | Secrets Management | Critical |

### 3.5 Denial of Service Threats

| ID | Threat | Affected Component | Severity |
|----|--------|-------------------|----------|
| T-015 | Attacker overwhelms API with requests causing service degradation | API Server | High |
| T-016 | Attacker exhausts database connections | PostgreSQL Database | High |
| T-017 | Attacker floods Redis with data causing memory exhaustion | Redis Cache | Medium |

### 3.6 Elevation of Privilege Threats

| ID | Threat | Affected Component | Severity |
|----|--------|-------------------|----------|
| T-018 | Faculty user escalates to Registrar privileges | Authorisation Module | High |
| T-019 | Student user accesses administrative functions | Authorisation Module | High |
| T-020 | Attacker exploits software vulnerability to gain system access | API Server | Critical |

## 4. NIST CSF Mapping

### 4.1 Identify (ID)

| Subcategory | AegisCore Implementation |
|-------------|--------------------------|
| ID.AM-1: Physical devices and systems inventoried | Infrastructure asset inventory maintained in configuration management database |
| ID.AM-2: Software platforms and applications inventoried | All NuGet packages tracked with versions; SBOM generated per build |
| ID.AM-3: Organisational communication and data flows mapped | Data flow diagrams documented in Architecture Document |
| ID.AM-5: Resources prioritised based on classification | Asset classification per Section 2 of this document |
| ID.RA-1: Asset vulnerabilities identified and documented | Weekly dependency vulnerability scan via `dotnet list package --vulnerable` |
| ID.RA-2: Threat intelligence received from external sources | OWASP, CVE, and NVD feeds monitored |
| ID.RA-3: Threats identified and documented | STRIDE threat model per Section 3 of this document |

### 4.2 Protect (PR)

| Subcategory | AegisCore Implementation |
|-------------|--------------------------|
| PR.AC-1: Identities and credentials managed | Argon2id password hashing, JWT-based authentication, MFA for admin accounts |
| PR.AC-3: Remote access managed | All remote access via TLS 1.3; VPN required for infrastructure access |
| PR.AC-4: Access permissions managed | Casbin ABAC with attribute-based policies |
| PR.AC-5: Network integrity protected | TLS 1.3 for all external communication; firewall rules restrict database access |
| PR.DS-1: Data-at-rest protected | AES-256-GCM column-level encryption for PII; PostgreSQL RLS for tenant isolation |
| PR.DS-2: Data-in-transit protected | TLS 1.3 enforced; certificate pinning for internal services |
| PR.DS-5: Protections against data leaks | DLP controls via RLS; no cross-tenant queries permitted |
| PR.IP-1: Configuration baseline established | Infrastructure as Code (Docker, GitHub Actions); configuration versioned |
| PR.IP-3: Change management enforced | All changes via pull request with mandatory code review |
| PR.IP-12: Vulnerability management plan | Weekly scans; critical patches applied within 24 hours |

### 4.3 Detect (DE)

| Subcategory | AegisCore Implementation |
|-------------|--------------------------|
| DE.AE-1: Baseline of network operations established | Normal traffic patterns documented; anomaly detection configured |
| DE.AE-2: Detected events analysed | Centralised logging with structured log format; automated alerting |
| DE.AE-3: Event data collected and correlated | Audit logs aggregated; correlation rules for multi-step attacks |
| DE.CM-1: Network monitored | nginx access logs; application performance monitoring |
| DE.CM-7: Unauthorised activity monitored | Failed login attempts tracked; rate limiting triggers alerts |

### 4.4 Respond (RS)

| Subcategory | AegisCore Implementation |
|-------------|--------------------------|
| RS.RP-1: Response plan executed | Incident response procedure documented and tested annually |
| RS.CO-2: Incidents reported | Internal escalation matrix; HEC notification for data breaches |
| RS.AN-1: Notifications from detection systems investigated | On-call rotation for security alerts; 15-minute acknowledgement SLA |
| RS.MI-1: Incidents contained | Automated failover to DR node; ability to isolate compromised tenant |

### 4.5 Recover (RC)

| Subcategory | AegisCore Implementation |
|-------------|--------------------------|
| RC.RP-1: Recovery plan executed | Disaster recovery procedure documented; RTO < 1 hour, RPO < 5 minutes |
| RC.CO-3: Recovery activities communicated | Status page updated; institutional stakeholders notified |
| RC.IM-1: Recovery plans incorporate lessons learned | Post-incident review required; findings incorporated into architecture |

## 5. OWASP ASVS Level 2 Compliance

### 5.1 V2: Authentication Verification Requirements

| Requirement | Implementation |
|-------------|----------------|
| 2.1.1: Verify that user set passwords are at least 12 characters in length | Enforced: minimum 12 characters with complexity requirements |
| 2.1.2: Verify that passwords of at least 64 characters are permitted | Supported: VARCHAR(255) column; no upper limit enforced |
| 2.1.7: Verify that passwords submitted during login are compared against the stored hash | Argon2id verification; plaintext never stored |
| 2.2.1: Verify that anti-automation controls are effective | Rate limiting: 5 attempts per minute per IP; account lockout after 10 failures |
| 2.2.3: Verify that secure password recovery mechanism is in place | Email-based reset with time-limited tokens (15 minutes) |
| 2.3.1: Verify that system-generated initial passwords are securely randomised | Cryptographically secure random generator; forced change on first login |
| 2.4.1: Verify that passwords are stored using an approved cryptographic algorithm | Argon2id with memory=65536 KiB, iterations=3, parallelism=4 |
| 2.5.1: Verify that credentials are not exposed in URLs or logs | Passwords never logged; connection strings masked in logs |
| 2.8.1: Verify that MFA is available for sensitive operations | TOTP-based MFA for administrative accounts |

### 5.2 V3: Session Management Verification Requirements

| Requirement | Implementation |
|-------------|----------------|
| 3.1.1: Verify that the session token is never exposed in URL parameters | JWT transmitted in Authorization header only |
| 3.2.1: Verify that the application generates a new session token on authentication | New JWT issued on login; refresh token rotation |
| 3.2.4: Verify that session tokens are invalidated on logout | Refresh token deleted from Redis; access token short-lived (15 minutes) |
| 3.3.1: Verify that logout fully terminates the session | Both access and refresh tokens invalidated |
| 3.3.2: Verify that sessions timeout after a period of inactivity | 60-minute inactivity timeout enforced |
| 3.5.1: Verify that the application prevents concurrent sessions for sensitive operations | Configurable per role; enforced for admin accounts |

### 5.3 V4: Access Control Verification Requirements

| Requirement | Implementation |
|-------------|----------------|
| 4.1.1: Verify that the application enforces access control rules on every request | Casbin ABAC middleware intercepts all API requests |
| 4.1.2: Verify that access controls are enforced server-side | All authorisation decisions made server-side; Blazor UI is for UX only |
| 4.1.3: Verify that the principle of least privilege exists | Role hierarchy with granular permissions; ABAC attributes further constrain access |
| 4.1.5: Verify that access controls fail securely | Default deny; explicit allow required |
| 4.2.1: Verify that sensitive data is protected from unauthorised access | RLS enforces tenant isolation; column-level encryption for PII |
| 4.3.1: Verify that administrative interfaces are appropriately segregated | Admin endpoints require SuperAdmin role; separate admin dashboard |

### 5.4 V5: Validation, Sanitisation, and Encoding

| Requirement | Implementation |
|-------------|----------------|
| 5.1.1: Verify that the application defends against HTTP parameter pollution | ASP.NET Core model binding with strict typing |
| 5.1.2: Verify that frameworks protect against mass parameter assignment | Explicit model binding with [BindRequired] attributes |
| 5.2.1: Verify that all untrusted input is validated | Input validation on all API endpoints; FluentValidation library |
| 5.3.1: Verify that output encoding is context-appropriate | Blazor automatically HTML-encodes output; explicit encoding for JSON |

### 5.5 V6: Stored Cryptography

| Requirement | Implementation |
|-------------|----------------|
| 6.1.1: Verify that only approved cryptographic algorithms are used | Argon2id for passwords; AES-256-GCM for data encryption; TLS 1.3 for transport |
| 6.1.2: Verify that cryptographic modules fail securely | All crypto operations wrapped in try-catch; errors logged without sensitive data |
| 6.2.1: Verify that all cryptographic keys are managed securely | Infisical for key storage; keys never in code or config files |
| 6.2.4: Verify that key rotation procedures are defined | Annual key rotation policy; emergency rotation procedure for compromise |

### 5.6 V7: Error Handling and Logging

| Requirement | Implementation |
|-------------|----------------|
| 7.1.1: Verify that the application does not log sensitive data | PII masked in logs; passwords never logged |
| 7.1.2: Verify that logs are stored securely | Audit logs in PostgreSQL with append-only policy; tamper detection via checksums |
| 7.2.1: Verify that all authentication decisions are logged | Login success/failure, token refresh, logout all logged |
| 7.2.2: Verify that all access control decisions are logged | Casbin enforcement decisions logged with attributes |
| 7.4.1: Verify that a generic error message is shown to users | Production errors return generic message; details logged server-side |

### 5.7 V8: Data Protection

| Requirement | Implementation |
|-------------|----------------|
| 8.1.1: Verify that the application protects sensitive data from being cached | Cache-Control headers set appropriately; sensitive data not cached in Redis |
| 8.2.1: Verify that all sensitive data is encrypted in transit | TLS 1.3 enforced; HSTS header set |
| 8.3.1: Verify that sensitive data is encrypted at rest | AES-256-GCM for PII columns; full-disk encryption on Hetzner servers |
| 8.3.4: Verify that backups are encrypted | Database backups encrypted before transfer to Nuremberg |

### 5.8 V9: Communication Security

| Requirement | Implementation |
|-------------|----------------|
| 9.1.1: Verify that TLS is used for all client connectivity | TLS 1.3 enforced; TLS 1.2 minimum for internal services |
| 9.1.2: Verify that up-to-date TLS configuration is used | Strong cipher suites only; HSTS with max-age=31536000 |
| 9.2.1: Verify that connections to external systems are authenticated | API keys or client certificates for all external integrations |

### 5.9 V10: Malicious Code

| Requirement | Implementation |
|-------------|----------------|
| 10.2.1: Verify that the application does not execute uploaded files | File uploads stored as blobs; never executed server-side |
| 10.3.1: Verify that integrity checks are performed on serialised objects | JSON deserialisation with type validation; no binary serialisation |

### 5.10 V11: Business Logic

| Requirement | Implementation |
|-------------|----------------|
| 11.1.1: Verify that the application processes business logic flows in the correct order | State machine validation for multi-step workflows (admissions, enrolment) |
| 11.1.2: Verify that the application prevents users from bypassing workflow steps | Server-side workflow state enforcement |

### 5.11 V12: Files and Resources

| Requirement | Implementation |
|-------------|----------------|
| 12.1.1: Verify that file uploads are validated for type and size | Accept attribute on file inputs; server-side validation of MIME type and size |
| 12.3.1: Verify that file metadata is not exposed | File serving via API with access control; no direct file system access |

### 5.12 V13: API and Web Service

| Requirement | Implementation |
|-------------|----------------|
| 13.1.1: Verify that all API endpoints require authentication | All endpoints except /auth/login and /health require valid JWT |
| 13.1.2: Verify that API rate limiting is in place | Rate limiting per endpoint; headers X-RateLimit-* returned |
| 13.2.1: Verify that RESTful services validate content type | Content-Type: application/json enforced; 415 returned for invalid types |

### 5.13 V14: Configuration

| Requirement | Implementation |
|-------------|----------------|
| 14.1.1: Verify that the application build and deployment is automated | GitHub Actions CI/CD pipeline |
| 14.2.1: Verify that all components are up to date | Weekly dependency scan; automated patch application |
| 14.2.2: Verify that unnecessary features are disabled | Minimal API surface; unused endpoints not registered |
| 14.4.1: Verify that HTTP security headers are set | CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy configured |

## 6. Authentication Architecture

### 6.1 Argon2id Parameter Selection

Argon2id is selected as the password hashing algorithm based on its resistance to both side-channel and GPU-based attacks. The following parameters are configured:

- **Memory:** 65,536 KiB (64 MiB) — sufficient to make GPU-based attacks economically infeasible while remaining performant on server hardware.
- **Iterations:** 3 — provides adequate computational cost without excessive login latency.
- **Parallelism:** 4 — utilises multiple CPU cores available on EPYC processors.
- **Salt Length:** 16 bytes — cryptographically secure random salt per password.
- **Hash Length:** 32 bytes — sufficient for collision resistance.

These parameters result in approximately 500ms hashing time on the target EPYC hardware, providing a balance between security and user experience.

### 6.2 JWT Lifecycle

1. **Issuance:** Upon successful authentication, an access token (15-minute expiry) and refresh token (7-day expiry) are issued.
2. **Access Token:** Contains claims: `sub` (user ID), `tenant_id`, `role`, `email`. Signed with RS256 using a private key stored in Infisical.
3. **Refresh Token:** Opaque token stored in Redis with user ID mapping. Used to obtain new access tokens without re-authentication.
4. **Rotation:** Each refresh token use invalidates the previous refresh token and issues a new one (refresh token rotation).
5. **Revocation:** On logout or password change, all refresh tokens for the user are deleted from Redis.
6. **Validation:** Every API request validates: token signature, expiry, issuer, audience, and that the user account is still active.

### 6.3 Session Management

- Sessions are stateless — no server-side session storage beyond refresh tokens in Redis.
- Inactivity timeout: 60 minutes (enforced by access token expiry).
- Absolute session limit: 12 hours (enforced by requiring re-authentication after refresh token chain exceeds 12 hours).
- Concurrent session control: Configurable per role; administrators limited to 3 concurrent sessions.

## 7. Authorisation Architecture (Casbin ABAC)

### 7.1 Casbin Model (model.conf)

