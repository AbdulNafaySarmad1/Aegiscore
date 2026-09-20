#!/usr/bin/env bash
# ─── AegisCore PostgreSQL backup ─────────────────────────────────────────────
# Full pg_dump (custom format), verified, then encrypted with
#   openssl enc -aes-256-cbc -salt -pbkdf2
# which is the same format AegisCore.Infrastructure/Backup/BackupCipher.cs reads and writes,
# so a file from either side restores through the other. Optionally uploaded to S3.
#
# BACKUP_ENCRYPTION_KEY is REQUIRED. The script exits before touching the database when it is
# missing: an unencrypted dump of a university's records is never written to disk.
#
# The matching restore is deploy/scripts/restore.sh; .github/workflows/ci.yml runs both against a
# throwaway database on every push (the "backup-restore-drill" job).
set -euo pipefail

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="${BACKUP_DIR:-/opt/aegiscore/backups}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"
DB_HOST="${DB_HOST:-postgres}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${POSTGRES_DB:-aegiscore}"
DB_USER="${POSTGRES_USER:-aegiscore}"
S3_BUCKET="${S3_BACKUP_BUCKET:-aegiscore-backups}"
ENCRYPTION_KEY="${BACKUP_ENCRYPTION_KEY:-}"

log() { echo "[BACKUP] $(date +'%H:%M:%S') $*"; }

if [ -z "$ENCRYPTION_KEY" ]; then
    log "BACKUP_ENCRYPTION_KEY is not set. Refusing to write an unencrypted backup."
    exit 2
fi
if [ "${#ENCRYPTION_KEY}" -lt 16 ]; then
    log "BACKUP_ENCRYPTION_KEY must be at least 16 characters."
    exit 2
fi

mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"

PLAIN_FILE="$BACKUP_DIR/aegiscore-$TIMESTAMP.dump"
BACKUP_FILE="$PLAIN_FILE.enc"
trap 'rm -f "$PLAIN_FILE"' EXIT

log "Starting full backup of $DB_NAME"
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
    -f "$PLAIN_FILE"

# Verify the dump BEFORE encrypting it: pg_restore cannot list an encrypted file.
log "Verifying dump..."
pg_restore --list "$PLAIN_FILE" > /dev/null
log "Dump verification: PASSED"

log "Encrypting..."
openssl enc -aes-256-cbc -salt -pbkdf2 \
    -in "$PLAIN_FILE" \
    -out "$BACKUP_FILE" \
    -pass env:BACKUP_ENCRYPTION_KEY
rm -f "$PLAIN_FILE"
chmod 600 "$BACKUP_FILE"
sha256sum "$BACKUP_FILE" > "$BACKUP_FILE.sha256"

BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
log "Backup complete: $BACKUP_FILE ($BACKUP_SIZE)"

# Upload to S3 when credentials are present
if command -v aws &> /dev/null && [ -n "${AWS_ACCESS_KEY_ID:-}" ]; then
    log "Uploading to S3: s3://$S3_BUCKET/"
    aws s3 cp "$BACKUP_FILE" "s3://$S3_BUCKET/$(basename "$BACKUP_FILE")" --storage-class STANDARD_IA
    aws s3 cp "$BACKUP_FILE.sha256" "s3://$S3_BUCKET/$(basename "$BACKUP_FILE").sha256"
    log "S3 upload complete"
fi

# Retention: prune local files, then S3 (docs/compliance/09-retention-policy.md §4)
log "Cleaning backups older than $RETENTION_DAYS days..."
find "$BACKUP_DIR" -name "aegiscore-*.dump.enc*" -mtime +"$RETENTION_DAYS" -delete 2>/dev/null || true
find "$BACKUP_DIR" -name "aegiscore-*.sql.gz*" -mtime +"$RETENTION_DAYS" -delete 2>/dev/null || true

if command -v aws &> /dev/null && [ -n "${AWS_ACCESS_KEY_ID:-}" ]; then
    CUTOFF_EPOCH=$(date -d "$RETENTION_DAYS days ago" +%s)
    aws s3 ls "s3://$S3_BUCKET/" | while read -r line; do
        FILE_DATE=$(echo "$line" | awk '{print $1" "$2}')
        FILE_EPOCH=$(date -d "$FILE_DATE" +%s)
        FILE_NAME=$(echo "$line" | awk '{print $4}')
        if [ -n "$FILE_NAME" ] && [ "$FILE_EPOCH" -lt "$CUTOFF_EPOCH" ]; then
            aws s3 rm "s3://$S3_BUCKET/$FILE_NAME"
            log "Removed old S3 backup: $FILE_NAME"
        fi
    done
fi

log "Backup job complete"
echo "$BACKUP_FILE"
