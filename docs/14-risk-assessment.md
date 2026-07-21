# Document 14 — Risk Assessment
**Standard:** ISO/IEC 27005 (aligned with ISO/IEC 27001)

| Field | Value |
|-------|-------|
| Document ID | AEGIS-RISK-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. Risk Assessment Scope and Methodology

### 1.1 Scope

This Risk Assessment covers all information assets associated with the AegisCore platform, including data assets, infrastructure assets, and application assets as defined in the Security Architecture Document (AEGIS-SEC-001). The assessment considers technical, operational, regulatory, market, and financial risks.

### 1.2 Methodology

The risk assessment follows ISO/IEC 27005 guidelines and employs a qualitative risk analysis approach:

1. **Asset Identification:** All information assets are identified and classified.
2. **Threat Identification:** Threats are identified using STRIDE methodology and operational experience.
3. **Vulnerability Identification:** Vulnerabilities are identified per asset based on architecture review and security testing.
4. **Risk Analysis:** Each risk is evaluated for Likelihood (1–5) and Impact (1–5), producing a Risk Score (1–25).
5. **Risk Evaluation:** Risks are compared against the risk appetite threshold (≥ 12 requires treatment).

### 1.3 Risk Appetite

| Risk Score | Classification | Required Action |
|------------|---------------|-----------------|
| 1–4 | Low | Accept; monitor |
| 5–8 | Medium | Monitor; consider mitigation |
| 9–11 | Elevated | Mitigation required within 6 months |
| 12–16 | High | Mitigation required within 3 months |
| 17–25 | Critical | Immediate mitigation required |

## 2. Asset Identification

Assets are fully documented in the Security Architecture Document (AEGIS-SEC-001), Section 2. Key assets assessed for risk:

| Asset ID | Asset | Type | Classification |
|----------|-------|------|----------------|
| A-001 | Student PII Database | Data | Restricted |
| A-002 | Financial Transaction Records | Data | Restricted |
| A-003 | Authentication Credentials | Data | Critical |
| A-004 | Audit Logs | Data | Restricted |
| A-005 | PostgreSQL Database Server | Infrastructure | Critical |
| A-006 | Redis Cache Server | Infrastructure | High |
| A-007 | API Application Server | Infrastructure | Critical |
| A-008 | nginx Reverse Proxy | Infrastructure | High |
| A-009 | Infisical Secrets Manager | Infrastructure | Critical |
| A-010 | GitHub Actions CI/CD | Infrastructure | High |
| A-011 | Blazor Frontend Application | Application | High |
| A-012 | Casbin ABAC Engine | Application | Critical |
| A-013 | Argon2id Authentication Module | Application | Critical |

## 3. Threat Catalogue

| Threat ID | Threat Description | Category |
|-----------|-------------------|----------|
| T-001 | SQL injection attack on API endpoints | Technical |
| T-002 | Cross-site scripting (XSS) via user input | Technical |
| T-003 | Credential brute force attack | Technical |
| T-004 | JWT token forgery or tampering | Technical |
| T-005 | Cross-tenant data access via application bug | Technical |
| T-006 | Denial of Service (DDoS) attack | Technical |
| T-007 | Insider threat — malicious administrator | Operational |
| T-008 | Insider threat — accidental data exposure | Operational |
| T-009 | Infrastructure failure — Helsinki data centre outage | Operational |
| T-010 | Infrastructure failure — disk corruption | Operational |
| T-011 | Third-party dependency vulnerability (NuGet package CVE) | Technical |
| T-012 | Secrets leakage via code repository or logs | Operational |
| T-013 | HEC regulatory non-compliance | Regulatory |
| T-014 | Data sovereignty violation (data stored outside approved jurisdiction) | Regulatory |
| T-015 | Competitor entry with superior product | Market |
| T-016 | Loss of key personnel (founder, lead developer) | Operational |
| T-017 | Currency devaluation affecting infrastructure costs | Financial |
| T-018 | Institution non-payment or contract dispute | Financial |
| T-019 | Social engineering attack on administrative staff | Operational |
| T-020 | Ransomware attack on production servers | Technical |
| T-021 | ML model poisoning or bias leading to incorrect predictions | Technical |
| T-022 | SignalR connection hijacking | Technical |

## 4. Vulnerability Identification

| Asset ID | Vulnerability | Related Threats |
|----------|--------------|-----------------|
| A-001 | Insufficient input validation on search endpoints | T-001, T-005 |
| A-001 | Missing encryption on backup files | T-010, T-012 |
| A-002 | Lack of transaction anomaly detection | T-007 |
| A-003 | Weak password policy not enforced for legacy accounts | T-003 |
| A-004 | Audit logs stored in same database as operational data | T-007, T-010 |
| A-005 | Single point of failure — primary database | T-009 |
| A-006 | Redis not clustered — single-threaded bottleneck | T-006 |
| A-007 | Dependency on NuGet packages with potential CVEs | T-011 |
| A-009 | Infisical token exposure via environment variables | T-012 |
| A-010 | GitHub Actions workflow injection vulnerability | T-012 |
| A-012 | Casbin policy misconfiguration could grant excessive permissions | T-007 |
| A-013 | Argon2id parameters not tuned for future hardware | T-003 |

## 5. Risk Matrix

### 5.1 Likelihood Scale

| Score | Likelihood | Description |
|-------|-----------|-------------|
| 1 | Very Low | < 5% probability in 12 months |
| 2 | Low | 5–20% probability in 12 months |
| 3 | Medium | 20–50% probability in 12 months |
| 4 | High | 50–80% probability in 12 months |
| 5 | Very High | > 80% probability in 12 months |

### 5.2 Impact Scale

| Score | Impact | Description |
|-------|--------|-------------|
| 1 | Negligible | No noticeable effect on operations |
| 2 | Minor | Minor disruption; recoverable within hours |
| 3 | Moderate | Significant disruption; recoverable within days |
| 4 | Major | Severe disruption; recoverable within weeks; reputational damage |
| 5 | Catastrophic | Business-threatening; data loss; regulatory action |

## 6. Risk Register

| Risk ID | Description | Asset | Threat | Vulnerability | L | I | Score | Current Controls | Residual Risk | Treatment | Owner | Review Date |
|---------|-------------|-------|--------|---------------|---|---|-------|------------------|---------------|-----------|-------|-------------|
| R-001 | SQL injection exposes student PII | A-001 | T-001 | Insufficient input validation | 2 | 5 | 10 | Parameterised queries; input validation; WAF | 6 | Mitigate | Security Lead | 2025-04-15 |
| R-002 | XSS attack compromises user sessions | A-011 | T-002 | User input rendered without encoding | 3 | 4 | 12 | Blazor auto-encoding; CSP headers | 6 | Mitigate | Dev Lead | 2025-04-15 |
| R-003 | Brute force attack cracks weak passwords | A-003 | T-003 | Weak password policy | 4 | 4 | 16 | Rate limiting; account lockout; Argon2id | 8 | Mitigate | Security Lead | 2025-03-15 |
| R-004 | JWT forgery grants unauthorised access | A-013 | T-004 | Weak JWT signing key | 1 | 5 | 5 | RS256; Infisical key storage; short expiry | 3 | Accept | Security Lead | 2025-07-15 |
| R-005 | Cross-tenant data access via bug | A-001 | T-005 | RLS not applied to all tables | 2 | 5 | 10 | RLS; global query filters; integration tests | 4 | Accept | Dev Lead | 2025-07-15 |
| R-006 | DDoS attack overwhelms API servers | A-007 | T-006 | Limited auto-scaling capacity | 3 | 3 | 9 | Rate limiting; nginx connection limits; auto-scaling | 6 | Mitigate | DevOps Lead | 2025-06-15 |
| R-007 | Malicious admin deletes audit logs | A-004 | T-007 | Audit logs in same DB as operational data | 2 | 4 | 8 | Append-only audit table; separate backup | 4 | Accept | Security Lead | 2025-07-15 |
| R-008 | Accidental data exposure via misconfigured permissions | A-001 | T-008 | Complex Casbin policy management | 3 | 4 | 12 | Policy tester; change approval workflow | 6 | Mitigate | Security Lead | 2025-04-15 |
| R-009 | Helsinki data centre outage | A-005 | T-009 | Single point of failure | 2 | 5 | 10 | Nuremberg DR; Azure fallback; DR testing | 4 | Accept | DevOps Lead | 2025-07-15 |
| R-010 | Disk corruption causes data loss | A-005 | T-010 | Insufficient backup verification | 1 | 5 | 5 | Daily backups; WAL archiving; weekly restore test | 2 | Accept | DevOps Lead | 2025-07-15 |
| R-011 | NuGet package CVE exploited | A-007 | T-011 | Dependency on third-party packages | 4 | 4 | 16 | Weekly vulnerability scan; automated patching | 8 | Mitigate | Dev Lead | 2025-03-15 |
| R-012 | Secrets leaked via code repository | A-009 | T-012 | Accidental commit of secrets | 3 | 5 | 15 | Infisical; .gitignore; pre-commit hooks | 6 | Mitigate | Security Lead | 2025-03-15 |
| R-013 | HEC non-compliance penalty | A-001 | T-013 | Changing regulatory requirements | 2 | 4 | 8 | HEC partnership; compliance monitoring | 4 | Accept | CEO | 2025-07-15 |
| R-014 | Data sovereignty violation | A-001 | T-014 | Data stored outside approved jurisdiction | 1 | 5 | 5 | Hetzner EU data centres; contractual guarantees | 2 | Accept | CEO | 2025-07-15 |
| R-015 | Competitor captures market share | N/A | T-015 | First-mover advantage erosion | 3 | 3 | 9 | Unique localisation; HEC alignment; competitive pricing | 6 | Mitigate | CEO | 2025-06-15 |
| R-016 | Loss of founder or lead developer | N/A | T-016 | Key person dependency | 2 | 4 | 8 | Documentation; knowledge sharing; succession planning | 4 | Accept | CEO | 2025-07-15 |
| R-017 | PKR devaluation increases infrastructure costs | N/A | T-017 | Foreign currency exposure | 4 | 3 | 12 | Multi-currency treasury; fixed-price contracts | 8 | Mitigate | CFO | 2025-04-15 |
| R-018 | Institution non-payment | N/A | T-018 | Customer financial distress | 3 | 3 | 9 | Upfront annual payment; contract terms | 6 | Mitigate | CFO | 2025-06-15 |
| R-019 | Social engineering compromises admin account | A-003 | T-019 | Insufficient security awareness training | 3 | 5 | 15 | MFA for admin; security training; phishing tests | 8 | Mitigate | Security Lead | 2025-03-15 |
| R-020 | Ransomware encrypts production servers | A-005 | T-020 | Insufficient endpoint protection | 2 | 5 | 10 | Immutable backups; network segmentation; EDR | 4 | Accept | Security Lead | 2025-07-15 |
| R-021 | ML model bias causes incorrect predictions | A-011 | T-021 | Insufficient model validation | 2 | 3 | 6 | Model versioning; A/B testing; human review | 3 | Accept | Data Science Lead | 2025-07-15 |
| R-022 | SignalR connection hijacking | A-007 | T-022 | WebSocket connection not authenticated | 1 | 3 | 3 | SignalR authentication; TLS; connection tokens | 2 | Accept | Dev Lead | 2025-07-15 |

## 7. Risk Treatment Plan (Risks ≥ 12)

| Risk ID | Risk Score | Treatment | Actions | Timeline | Owner |
|---------|-----------|-----------|---------|----------|-------|
| R-003 | 16 | Mitigate | Enforce 12-char minimum password; implement password strength meter; mandatory MFA for all admin accounts | 2025-03-15 | Security Lead |
| R-011 | 16 | Mitigate | Automate NuGet vulnerability scanning in CI/CD; establish 24-hour SLA for critical CVE patches | 2025-03-15 | Dev Lead |
| R-012 | 15 | Mitigate | Implement pre-commit secret scanning; quarterly secret rotation audit; Infisical access review | 2025-03-15 | Security Lead |
| R-019 | 15 | Mitigate | Conduct quarterly security awareness training; implement phishing simulation programme; enforce MFA for all users | 2025-03-15 | Security Lead |
| R-002 | 12 | Mitigate | Implement strict CSP headers; conduct XSS-focused penetration test; add output encoding review to code review checklist | 2025-04-15 | Dev Lead |
| R-008 | 12 | Mitigate | Implement policy change approval workflow; add policy testing to CI/CD; conduct quarterly policy audit | 2025-04-15 | Security Lead |
| R-017 | 12 | Mitigate | Establish PKR-denominated pricing; hedge foreign currency exposure; maintain 6-month operating reserve | 2025-04-15 | CFO |

## 8. Residual Risk Acceptance Statement

After application of the risk treatment plan, all risks with a residual score ≥ 12 have been reduced to acceptable levels (residual score ≤ 8). The residual risks are accepted by management with ongoing monitoring as defined in the Risk Review and Monitoring Schedule.

## 9. Risk Review and Monitoring Schedule

| Activity | Frequency | Owner |
|----------|-----------|-------|
| Risk register review | Quarterly | CTO |
| Vulnerability scan | Weekly | DevOps Lead |
| Penetration test | Quarterly | Security Lead |
| DR failover test | Monthly | DevOps Lead |
| Security awareness training | Quarterly | Security Lead |
| Policy audit | Quarterly | Security Lead |
| Financial risk review | Monthly | CFO |
| Regulatory compliance review | Bi-annually | CEO |
