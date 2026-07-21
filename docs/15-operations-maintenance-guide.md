# Document 15 — Operations & Maintenance Guide
**Standard:** ISO/IEC/IEEE 12207

| Field | Value |
|-------|-------|
| Document ID | AEGIS-OPS-001 |
| Version | 1.0.0 |
| Date | 2025-01-15 |
| Author | Abdul Nafay Sarmad, Founder & CEO |
| Status | Approved |

## Revision History

| Version | Date | Author | Description |
|---------|------|--------|-------------|
| 1.0.0 | 2025-01-15 | Abdul Nafay Sarmad | Initial release |

## 1. Operations Overview

This Operations & Maintenance Guide defines the procedures for operating and maintaining the AegisCore platform in alignment with ISO/IEC/IEEE 12207 software life cycle processes. It covers daily, weekly, and monthly operational checklists, maintenance categories, change management, incident management, patch management, disaster recovery, SLA monitoring, and decommissioning procedures.

## 2. Daily Operations Checklist

| # | Task | Procedure | Success Criteria | Escalation |
|---|------|-----------|------------------|------------|
| 1 | Health check verification | `curl -f https://api.aegiscore.pk/health` | HTTP 200 with `{"status":"ok"}` | If failed, check individual component health |
| 2 | Database health check | `curl -f https://api.aegiscore.pk/health/database` | `{"database":"connected"}` | If failed, check PostgreSQL service |
| 3 | Redis health check | `curl -f https://api.aegiscore.pk/health/redis` | `{"redis":"connected"}` | If failed, check Redis service |
| 4 | Backup verification | Check backup logs at `/var/log/aegiscore/backup.log` | Last backup completed successfully | If failed, manually trigger backup |
| 5 | Disk space check | `df -h` on all servers | All volumes < 75% used | If > 75%, investigate and clean up |
| 6 | Log review | Check application logs for ERROR entries | No new critical errors | Investigate any new errors |
| 7 | Alert triage | Review monitoring dashboard for active alerts | All alerts acknowledged | Escalate unresolved alerts |
| 8 | Certificate expiry check | `openssl s_client -connect api.aegiscore.pk:443 | openssl x509 -noout -dates` | Expiry > 30 days away | If < 30 days, verify auto-renewal |

## 3. Weekly Operations Checklist

| # | Task | Procedure | Success Criteria |
|---|------|-----------|------------------|
| 1 | Performance trend review | Review APM dashboard for response time, throughput, error rate trends | No degradation from baseline |
| 2 | Certificate expiry check (all domains) | Verify all TLS certificates | All > 30 days to expiry |
| 3 | Dependency vulnerability scan | `dotnet list package --vulnerable` | No new critical/high CVEs |
| 4 | Database vacuum analyse | `psql -U aegiscore -d aegiscore -c "VACUUM ANALYSE;"` | Completed without errors |
| 5 | Backup restore test | Restore latest backup to test database; verify integrity | Restore successful; data verified |
| 6 | Audit log review | Review audit logs for suspicious activity | No unauthorised access detected |
| 7 | Licence status review | Check licence expiry dates for all tenants | No licences expiring within 30 days |

## 4. Monthly Operations Checklist

| # | Task | Procedure | Success Criteria |
|---|------|-----------|------------------|
| 1 | DR failover test | Execute full DR failover procedure (Section 9) | Failover < 1 hour; data integrity verified |
| 2 | Licence audit | Verify student headcount against licenced capacity for all tenants | All tenants within licenced limits |
| 3 | Capacity review | Analyse resource utilisation trends; project 3-month capacity needs | No capacity constraints projected |
| 4 | Security patch audit | Verify all OS and application security patches applied | All critical patches applied |
| 5 | User access review | Review administrative user accounts; deactivate unused accounts | All accounts justified and active |
| 6 | Policy audit | Review Casbin policies for appropriateness | No excessive permissions |
| 7 | Full database vacuum | `psql -U aegiscore -d aegiscore -c "VACUUM FULL ANALYSE;"` (during maintenance window) | Completed without errors |

## 5. Maintenance Categories

### 5.1 Corrective Maintenance

**Definition:** Reactive maintenance to fix defects discovered in production.

**Examples in AegisCore context:**
- Fixing a bug where attendance records are not saved for courses with > 200 students
- Resolving a database connection pool exhaustion issue under high load
- Patching a security vulnerability discovered through penetration testing

**Procedure:**
1. Defect reported via issue tracking system.
2. Triage: assess severity and impact.
3. Develop fix in feature branch.
4. Test fix in staging environment.
5. Deploy fix via hotfix or scheduled release pipeline.
6. Verify fix in production.
7. Update documentation if applicable.

### 5.2 Adaptive Maintenance

**Definition:** Modifications to keep the system usable in a changed environment.

**Examples in AegisCore context:**
- Updating banking gateway integration when 1Link API changes
- Modifying HEC reporting formats when regulatory requirements change
- Adapting to new PostgreSQL major version with breaking changes

**Procedure:**
1. Identify environmental change.
2. Assess impact on AegisCore.
3. Develop adaptation in feature branch.
4. Test thoroughly in staging with new environment.
5. Deploy with appropriate change management.
6. Communicate changes to affected stakeholders.

### 5.3 Perfective Maintenance

**Definition:** Enhancements to improve performance, maintainability, or user experience.

**Examples in AegisCore context:**
- Implementing Redis Cluster for improved cache scalability
- Adding new analytics dashboard for Vice Chancellors
- Optimising database queries based on performance analysis

**Procedure:**
1. Enhancement requested via product backlog.
2. Prioritised by Product Owner.
3. Developed in feature branch.
4. Tested in staging.
5. Deployed in scheduled release.
6. User training if applicable.

### 5.4 Preventive Maintenance

**Definition:** Proactive maintenance to prevent future issues.

**Examples in AegisCore context:**
- Refactoring code to reduce technical debt
- Upgrading NuGet packages before vulnerabilities are discovered
- Adding database indexes based on query pattern analysis

**Procedure:**
1. Identify preventive opportunity through monitoring or code review.
2. Schedule in development sprint.
3. Implement and test.
4. Deploy in scheduled release.

## 6. Change Management Procedure

### 6.1 Change Classification

| Change Type | Examples | Approval Required | Deployment Window |
|-------------|----------|-------------------|-------------------|
| Standard | Routine configuration update, minor bug fix | Team Lead | Any time |
| Normal | New feature, database migration, package upgrade | Product Owner + Tech Lead | Scheduled release |
| Emergency | Critical security patch, production outage fix | CTO (can be post-approval) | Immediate |

### 6.2 Change Request Process

1. **Submit:** Change requester submits change request with description, justification, impact assessment, and rollback plan.
2. **Review:** Tech Lead reviews technical feasibility and risk.
3. **Approve:** Appropriate authority approves based on change type.
4. **Implement:** Developer implements change in feature branch.
5. **Test:** Change tested in staging environment.
6. **Schedule:** Deployment scheduled based on change type.
7. **Deploy:** Change deployed via CI/CD pipeline.
8. **Verify:** Post-deployment verification (health checks, smoke tests).
9. **Document:** Change recorded in change log.

### 6.3 Emergency Change Procedure

1. Issue identified (e.g., critical security vulnerability, production outage).
2. On-call engineer assesses and escalates to CTO.
3. CTO authorises emergency change (verbal approval acceptable; documented within 24 hours).
4. Fix developed and tested on staging (expedited testing).
5. Fix deployed immediately.
6. Post-deployment verification.
7. Full testing conducted post-deployment.
8. Root cause analysis conducted within 48 hours.

## 7. Incident Management Procedure

### 7.1 Incident Severity Levels

| Severity | Definition | Response Time | Resolution Time | Examples |
|----------|-----------|---------------|-----------------|----------|
| P1 — Critical | System down; all users affected | 15 minutes | 2 hours | Database failure; security breach |
| P2 — High | Major feature unavailable; many users affected | 30 minutes | 4 hours | Payment processing down; login failure |
| P3 — Medium | Feature impaired; workaround exists | 2 hours | 24 hours | Report generation slow; minor UI bug |
| P4 — Low | Cosmetic issue; minimal impact | 8 hours | Next release | Typo; colour inconsistency |

### 7.2 Incident Response Flow

1. **Detection:** Incident detected via monitoring alert, user report, or manual observation.
2. **Triage:** On-call engineer assesses severity within response time SLA.
3. **Escalation:** If P1 or P2, escalate to CTO and relevant team leads.
4. **Containment:** Isolate affected component; apply temporary fix if available.
5. **Resolution:** Implement permanent fix; test; deploy.
6. **Verification:** Verify resolution with health checks and user confirmation.
7. **Communication:** Update status page; notify affected stakeholders.
8. **Post-Mortem:** Conduct root cause analysis within 48 hours (P1/P2) or 1 week (P3).
9. **Improvement:** Implement preventive measures identified in post-mortem.

## 8. Patch Management Policy

### 8.1 Patch Categories

| Category | Examples | Testing Required | Deployment Timeline |
|----------|----------|------------------|---------------------|
| Critical Security | CVE score ≥ 9.0 | Expedited testing | Within 24 hours |
| High Security | CVE score 7.0–8.9 | Standard testing | Within 1 week |
| Medium Security | CVE score 4.0–6.9 | Standard testing | Within 1 month |
| Low Security | CVE score < 4.0 | Standard testing | Next scheduled release |
| Functional Update | New feature, bug fix | Full testing | Scheduled release |

### 8.2 Patch Application Procedure

1. **Identify:** Vulnerability scan or vendor notification identifies available patch.
2. **Assess:** Evaluate impact and compatibility with AegisCore.
3. **Test:** Apply patch in staging environment; run full test suite.
4. **Schedule:** Schedule deployment based on patch category.
5. **Deploy:** Apply patch via CI/CD pipeline.
6. **Verify:** Post-deployment health checks and smoke tests.
7. **Document:** Record patch in change log.

### 8.3 NuGet Package Updates

NuGet packages are updated via the following process:
1. Weekly `dotnet list package --vulnerable` scan.
2. Review available updates and release notes.
3. Update packages in feature branch.
4. Run full test suite.
5. Merge and deploy in next scheduled release.
6. Critical security updates follow emergency change procedure.

### 8.4 OS and Docker Base Image Updates

1. Monitor Ubuntu security advisories and Docker Hub for base image updates.
2. Test updated base image in staging.
3. Rebuild Docker images with updated base.
4. Deploy updated containers with rolling update (zero downtime).

## 9. Disaster Recovery Procedure

### 9.1 Scenario: Primary Helsinki Node Failure

**Trigger:** Helsinki node becomes unreachable (health check fails for > 5 minutes).

**Procedure:**
1. **Detection:** Monitoring alert triggers; on-call engineer acknowledges within 15 minutes.
2. **Assessment:** Verify Helsinki is truly down (attempt SSH, ping, API health check).
3. **Decision:** CTO authorises failover to Nuremberg.
4. **Database Failover:**
   ```bash
   # On Nuremberg standby
   pg_ctl promote -D /var/lib/postgresql/16/main
   ```
5. **Redis Failover:**
   ```bash
   # On Nuremberg replica
   redis-cli REPLICAOF NO ONE
   ```
6. **DNS Update:** Update DNS A record for `api.aegiscore.pk` to point to Nuremberg IP.
7. **Application Restart:** Restart API containers on Nuremberg (if not already running).
8. **Verification:** Run health checks; verify tenant isolation; test critical workflows.
9. **Communication:** Update status page; notify institutional stakeholders.
10. **Target:** RTO < 1 hour; RPO < 5 minutes.

### 9.2 Scenario: Both Hetzner Nodes Failure

**Trigger:** Both Helsinki and Nuremberg unreachable.

**Procedure:**
1. Deploy to Azure App Service from latest backup.
2. Restore database from latest backup to Azure PostgreSQL.
3. Update DNS to point to Azure.
4. Verify functionality.
5. **Target:** RTO < 4 hours; RPO < 24 hours (daily backup).

### 9.3 Failback Procedure

After Helsinki node is restored:
1. Re-establish replication: Nuremberg → Helsinki.
2. Allow replication to catch up.
3. Schedule maintenance window.
4. Perform controlled failover back to Helsinki.
5. Update DNS.
6. Verify functionality.

## 10. SLA Monitoring

### 10.1 Uptime Calculation

Uptime is calculated monthly:
