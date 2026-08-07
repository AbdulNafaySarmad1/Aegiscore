# AegisCore Disaster & Continuity Recovery Runbook

## Pre-Flight Checks

```bash
# Verify primary health
curl -sf http://localhost:8080/health/ready || echo "PRIMARY DOWN"

# Check replica lag
psql "$DCR_DB_CONNECTION" -c "SELECT now() - pg_last_xact_replay_timestamp() AS replica_lag;"

# Verify WAL archiving
aws s3 ls "s3://${DCR_WAL_ARCHIVE_BUCKET}/${DCR_WAL_ARCHIVE_PREFIX}" --recursive | tail -5

# Check latest snapshot
aws s3 ls "s3://${DCR_SNAPSHOT_BUCKET}/${DCR_SNAPSHOT_PREFIX}" --recursive | tail -5
```

## Manual Failover Procedure

```bash
# 1. Verify primary is unreachable
curl -sf http://primary:8080/health/ready
if [ $? -ne 0 ]; then
  echo "Primary unreachable — initiating failover"
fi

# 2. Promote replica
psql "$DCR_DB_CONNECTION" -c "SELECT pg_promote();"

# 3. Verify promotion
psql "$DCR_DB_CONNECTION" -c "SELECT pg_is_in_recovery();"  # should return 'f'

# 4. Update environment
export DB_HOST="$DCR_DB_HOST"
export DB_NAME="$DCR_DB_NAME"

# 5. Restart application
docker compose restart app

# 6. Verify application health
curl -sf http://localhost:8080/health/ready && echo "FAILOVER COMPLETE"
```

## Point-in-Time Recovery (PITR)

```bash
# 1. Set recovery target
TARGET_TIME="2026-01-15 14:30:00 UTC"
RESTORE_HOST="restore-db"

# 2. Restore latest base snapshot
pg_restore --host="$RESTORE_HOST" --no-owner --no-acl \
  --dbname="${DB_NAME}" "/backups/latest-snapshot.dump"

# 3. Configure recovery.conf on restore host
cat > /var/lib/postgresql/data/pgdata/recovery.conf <<EOF
restore_command = 'aws s3 cp s3://${DCR_WAL_ARCHIVE_BUCKET}/${DCR_WAL_ARCHIVE_PREFIX}%f %p'
recovery_target_time = '${TARGET_TIME}'
recovery_target_action = 'promote'
EOF

# 4. Start Postgres in recovery mode
pg_ctl start -D /var/lib/postgresql/data/pgdata

# 5. Monitor recovery progress
tail -f /var/log/postgresql/postgresql.log | grep "recovery"

# 6. After promotion, verify data integrity
psql "$RESTORE_DB" -c "SELECT count(*) FROM \"JournalEntries\";"

# 7. Point application to restored instance
export DB_HOST="$RESTORE_HOST"
docker compose restart app
```

## Snapshot Management

```bash
# Create manual snapshot
pg_basebackup -h "$DB_HOST" -U "$DB_USER" -D "/tmp/pg-snapshot" -Ft -z -P

# Upload to object storage
aws s3 cp "/tmp/pg-snapshot.tar.gz" \
  "s3://${DCR_SNAPSHOT_BUCKET}/${DCR_SNAPSHOT_PREFIX}$(date +%Y%m%d-%H%M%S).tar.gz"

# List snapshots with age
aws s3 ls "s3://${DCR_SNAPSHOT_BUCKET}/${DCR_SNAPSHOT_PREFIX}"

# Delete snapshots older than retention period
aws s3 ls "s3://${DCR_SNAPSHOT_BUCKET}/${DCR_SNAPSHOT_PREFIX}" | \
  awk -v cutoff="$(date -d "${DCR_SNAPSHOT_RETENTION_DAYS:-30} days ago" +%Y-%m-%d)" \
  '$1 < cutoff {print $NF}' | \
  xargs -I{} aws s3 rm "s3://${DCR_SNAPSHOT_BUCKET}/${DCR_SNAPSHOT_PREFIX}{}"
```

## Health Monitoring Script

```bash
#!/bin/bash
# File: scripts/dcr-health-check.sh
# Cron: */1 * * * * /opt/aegiscore/scripts/dcr-health-check.sh

WEBHOOK="${DCR_ALERT_WEBHOOK}"
PRIMARY_HOST="${DB_HOST}"
REPLICA_HOST="${DCR_DB_HOST}"

# Check primary
if ! pg_isready -h "$PRIMARY_HOST" -U "$DB_USER" -q; then
  FAILURES=$((FAILURES + 1))
  echo "[DCR] Primary unreachable — failure ${FAILURES}/${DCR_FAILOVER_THRESHOLD_FAILURES:-3}"
fi

if [ "$FAILURES" -ge "${DCR_FAILOVER_THRESHOLD_FAILURES:-3}" ]; then
  if [ "${DCR_FAILOVER_AUTO:-false}" = "true" ]; then
    echo "[DCR] Threshold reached — triggering auto-failover"
    curl -X POST "$WEBHOOK" -d '{"event":"auto_failover_initiated","timestamp":"'"$(date -Iseconds)"'"}'
  else
    echo "[DCR] Threshold reached — manual failover required"
    curl -X POST "$WEBHOOK" -d '{"event":"manual_failover_required","timestamp":"'"$(date -Iseconds)"'"}'
  fi
fi
```
