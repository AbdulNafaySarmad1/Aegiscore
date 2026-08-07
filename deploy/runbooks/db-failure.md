# Disaster Recovery Runbook — Database Failure

**Scenario**: PostgreSQL is unreachable, crashing, or corrupt.
**Severity**: 🔴 Critical
**RTO**: 20 minutes
**RPO**: 5 minutes (WAL archiving)

## Detection
- Grafana alert: `DatabaseUnreachable` fires
- Application logs show `NpgsqlException`, connection timeouts
- `pg_isready -h postgres` returns "no response"

## Recovery

### Scenario A: Connection exhaustion (pool full)

**Symptoms**: `FATAL: too many clients` in PostgreSQL logs

**Fix**:
```bash
# Check current connections
docker exec aegiscore-postgres psql -U aegiscore -c "
    SELECT count(*) AS connections,
           state
    FROM pg_stat_activity
    GROUP BY state;
"

# Kill idle connections older than 30 seconds
docker exec aegiscore-postgres psql -U aegiscore -c "
    SELECT pg_terminate_backend(pid)
    FROM pg_stat_activity
    WHERE state = 'idle' AND state_change < now() - interval '30 seconds'
      AND pid <> pg_backend_pid();
"

# Check PgBouncer
docker exec aegiscore-pgbouncer sh -c "echo 'SHOW POOLS;' | nc localhost 6432"

# Restart PgBouncer if stuck
docker compose -f docker-compose.prod.yml restart pgbouncer
```

### Scenario B: Database won't start

**Symptoms**: PostgreSQL container exiting immediately, crash logs

**Fix**:
```bash
# Check logs
docker logs --tail 100 aegiscore-postgres

# If corruption detected, restore from backup
cd /opt/aegiscore
docker compose stop api pgbouncer

# Restore latest backup
BACKUP_FILE=$(ls -t backups/aegiscore-*.sql.gz | head -1)
gunzip -c "$BACKUP_FILE" | docker exec -i aegiscore-postgres psql -U aegiscore -d aegiscore

# If data directory corruption, fresh restore
docker compose stop postgres
docker volume rm aegiscore_prod_postgres_data
docker compose up -d postgres
sleep 15

gunzip -c "$BACKUP_FILE" | docker exec -i aegiscore-postgres psql -U aegiscore -d aegiscore
docker compose up -d pgbouncer api
```

### Scenario C: WAL recovery from archive

**Applies when**: Point-in-time recovery needed after corruption

```bash
# Stop services
docker compose stop api pgbouncer postgres

# Create recovery.conf
cat > /tmp/recovery.conf << 'EOF'
restore_command = 'aws s3 cp s3://aegiscore-backups/wal/%f %p'
recovery_target_time = '2026-08-12 00:00:00 UTC'
recovery_target_action = 'promote'
EOF

# Copy to data directory and start
docker cp /tmp/recovery.conf aegiscore-postgres:/var/lib/postgresql/data/recovery.conf
docker compose start postgres

# Monitor recovery
docker logs -f aegiscore-postgres | grep -i recovery
```

### Post-Recovery Verification
```bash
# Verify row counts match expectations
docker exec aegiscore-postgres psql -U aegiscore -d aegiscore -c "
    SELECT 'Users' AS table_name, count(*) FROM \"Users\"
    UNION ALL SELECT 'Students', count(*) FROM \"StudentProfiles\"
    UNION ALL SELECT 'JournalEntries', count(*) FROM \"JournalEntries\";
"
```
