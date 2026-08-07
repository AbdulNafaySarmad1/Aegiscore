# Disaster Recovery Runbook — DDoS Attack

**Scenario**: Sustained malicious traffic overwhelming the application.
**Severity**: 🔴 Critical
**RTO**: 5 minutes
**Target**: Deflect attack while keeping legitimate users online.

## Detection
- Grafana alert: `AttackWarning` (>50 failed logins/min) or `RateLimitFrequent`
- Traffic spike in Caddy access logs or Cloudflare analytics
- Users report slow responses or timeouts
- Unusual patterns: 1000s of requests from single IP, non-browser User-Agents

## Immediate Action — Cloudflare (1 minute)

### 1. Enable Under Attack Mode
```
Cloudflare Dashboard → aegiscore.pk → Quick Actions → Under Attack Mode: ON
```
This enables JS challenge for ALL visitors. Legitimate users see a 5-second JS challenge page before accessing the site. Bots are stopped immediately.

### 2. Apply WAF Emergency Rule
```
Security → WAF → Create Rule:
  Field: URI Path → contains → /api/auth
  Action: Block
  (Blocks credential stuffing on login endpoint)
```

### 3. Block Attacker IPs
Identify top attacking IPs from Cloudflare Analytics or Caddy logs:
```bash
docker logs aegiscore-caddy 2>&1 | awk '{print $1}' | sort | uniq -c | sort -rn | head -20
```
Block in Cloudflare: Security → WAF → IP Access Rules → Block

### 4. Rate Limit Escalation
```
Security → WAF → Rate Limiting Rules:
  - URI: /api/auth/login → 3 req/5min/IP → Block
  - URI: /api/* → 50 req/1min/IP → Block  
  - URI: /* → 200 req/1min/IP → Challenge
```

## If Attack Continues — Application Layer (2 minutes)
```bash
# SSH to production
cd /opt/aegiscore

# Enable strict rate limiting in app
docker exec aegiscore-api sh -c '
    export AEGISCORE_RATE_LIMIT_STRICT=true
'

# Scale up if under resource exhaustion
docker compose -f docker-compose.prod.yml up -d --scale api=3
```

## Post-Attack Actions
- Export Cloudflare security events for forensic analysis
- Review compromised accounts (any successful logins during attack window)
- Force password reset for any accounts that logged in during attack
- Update WAF rules based on attack patterns observed
- Disable Under Attack Mode once traffic normalizes
