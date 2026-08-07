# Disaster Recovery Runbook — App Container Crash

**Scenario**: The AegisCore API container has crashed or is unresponsive.
**Severity**: 🔴 Critical
**RTO**: 2 minutes

## Detection
- Grafana alert: `AppDown` fires (health/live failing)
- `docker ps` shows no `aegiscore-api` or status=restarting constantly
- Users report 502/503 errors

## Recovery

### Step 1 — Check container status (15 seconds)
```bash
cd /opt/aegiscore
docker ps -a --filter name=aegiscore-api
docker logs --tail 50 aegiscore-api
```

### Step 2 — Restart container (30 seconds)
```bash
docker compose -f docker-compose.prod.yml up -d --force-recreate api
```

### Step 3 — Verify health (60 seconds)
```bash
# Wait for container to initialize
for i in $(seq 1 12); do
    if curl -fsS http://localhost:8080/health/ready; then
        echo "✓ Healthy after ${i}0s"
        exit 0
    fi
    sleep 10
done
echo "✗ Container failed to become healthy — escalate to DBA"
```

### Step 4 — If restart fails, redeploy (2 minutes)
```bash
docker compose -f docker-compose.prod.yml pull api
docker compose -f docker-compose.prod.yml up -d api
```

### Step 5 — If redeploy fails, rollback to previous version
```bash
./deploy/scripts/rollback.sh
```

### Root Cause Analysis
- Check `docker logs aegiscore-api --tail 200 | grep -i error`
- Check `journalctl -u docker --since "10 minutes ago" | grep -i oom` for Out-Of-Memory kills
- Check Grafana → Memory Usage panel for leak pattern
- Check if recent config change broke startup (review Git log for last 30 min)
