#!/usr/bin/env bash
# ─── AegisCore PostgreSQL Backup ─────────────────────
# Full backup with WAL archiving, encrypted, to S3-compatible storage
set -euo pipefail

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="/opt/aegiscore/backups"
RETENTION_DAYS=30
DB_HOST="postgres"
DB_PORT="5432"
DB_NAME="${POSTGRES_DB:-aegiscore}"
DB_USER="${POSTGRES_USER:-aegiscore}"
S3_BUCKET="${S3_BACKUP_BUCKET:-aegiscore-backups}"
ENCRYPTION_KEY="${BACKUP_ENCRYPTION_KEY:-}"

log() { echo "[BACKUP] $(date +'%H:%M:%S') $*"; }

mkdir -p "$BACKUP_DIR"

log "Starting full backup of $DB_NAME"

BACKUP_FILE="$BACKUP_DIR/aegiscore-$TIMESTAMP.sql.gz"

PGPASSWORD="${POSTGRES_PASSWORD}" pg_dump \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    --no-owner \
    --no-acl \
    --clean \
    --if-exists \
    --format=custom \
    --verbose \
    -f "$BACKUP_FILE" 2>&1 | tail -5

BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
log "Backup complete: $BACKUP_FILE ($BACKUP_SIZE)"

# Encrypt if key available
if [ -n "$ENCRYPTION_KEY" ]; then
    log "Encrypting backup..."
    openssl enc -aes-256-cbc -salt -pbkdf2 \
        -in "$BACKUP_FILE" \
        -out "$BACKUP_FILE.enc" \
        -pass "pass:$ENCRYPTION_KEY"
    mv "$BACKUP_FILE.enc" "$BACKUP_FILE"
    log "Encryption complete"
fi

# Upload to S3
if command -v aws &> /dev/null && [ -n "$AWS_ACCESS_KEY_ID" ]; then
    log "Uploading to S3: s3://$S3_BUCKET/"
    aws s3 cp "$BACKUP_FILE" "s3://$S3_BUCKET/aegiscore-$TIMESTAMP.sql.gz" --storage-class STANDARD_IA
    log "S3 upload complete"
fi

# Verify backup
log "Verifying backup..."
pg_restore --list "$BACKUP_FILE" > /dev/null 2>&1
if [ $? -eq 0 ]; then
    log "Backup verification: PASSED"
else
    log "Backup verification: FAILED — ALERT!"
    exit 1
fi

# Cleanup old backups
log "Cleaning backups older than $RETENTION_DAYS days..."
find "$BACKUP_DIR" -name "aegiscore-*.sql.gz" -mtime +$RETENTION_DAYS -delete 2>/dev/null || true
find "$BACKUP_DIR" -name "aegiscore-*.sql.gz.enc" -mtime +$RETENTION_DAYS -delete 2>/dev/null || true

# Remove old S3 backups
if command -v aws &> /dev/null; then
    aws s3 ls "s3://$S3_BUCKET/" | while read -r line; do
        FILE_DATE=$(echo "$line" | awk '{print $1" "$2}')
        FILE_EPOCH=$(date -d "$FILE_DATE" +%s)
        CUTOFF_EPOCH=$(date -d "$RETENTION_DAYS days ago" +%s)
        FILE_NAME=$(echo "$line" | awk '{print $4}')
        if [ "$FILE_EPOCH" -lt "$CUTOFF_EPOCH" ]; then
            aws s3 rm "s3://$S3_BUCKET/$FILE_NAME"
            log "Removed old S3 backup: $FILE_NAME"
        fi
    done
fi

log "Backup job complete"
