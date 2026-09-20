#!/usr/bin/env bash
# ─── AegisCore PostgreSQL restore ────────────────────────────────────────────
# Decrypts a backup written by deploy/scripts/backup.sh (or by the in-app BackupService — same
# format) and restores it into the target database. Refuses to restore over a database that is
# not empty unless RESTORE_FORCE=1, so a mistyped target does not destroy a live installation.
#
#   BACKUP_ENCRYPTION_KEY=... DB_HOST=... POSTGRES_DB=target ./restore.sh /path/aegiscore-....dump.enc
#
# Exit codes: 2 usage/key error, 3 checksum or decryption failure, 4 target not empty, 5 restore failed.
set -euo pipefail

BACKUP_FILE="${1:-}"
DB_HOST="${DB_HOST:-postgres}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${POSTGRES_DB:-aegiscore}"
DB_USER="${POSTGRES_USER:-aegiscore}"
ENCRYPTION_KEY="${BACKUP_ENCRYPTION_KEY:-}"

log() { echo "[RESTORE] $(date +'%H:%M:%S') $*"; }

[ -n "$BACKUP_FILE" ] && [ -f "$BACKUP_FILE" ] || { log "Usage: restore.sh <backup.dump.enc>"; exit 2; }
[ -n "$ENCRYPTION_KEY" ] || { log "BACKUP_ENCRYPTION_KEY is not set."; exit 2; }

if [ -f "$BACKUP_FILE.sha256" ]; then
    log "Checking SHA-256..."
    (cd "$(dirname "$BACKUP_FILE")" && sha256sum -c "$(basename "$BACKUP_FILE").sha256" > /dev/null) || { log "Checksum mismatch — the file was altered or truncated."; exit 3; }
fi

PLAIN_FILE="$(mktemp "${TMPDIR:-/tmp}/aegiscore-restore.XXXXXX")"
trap 'rm -f "$PLAIN_FILE"' EXIT
chmod 600 "$PLAIN_FILE"

log "Decrypting..."
openssl enc -d -aes-256-cbc -pbkdf2 -in "$BACKUP_FILE" -out "$PLAIN_FILE" -pass env:BACKUP_ENCRYPTION_KEY \
    || { log "Decryption failed: wrong key or corrupted file."; exit 3; }
pg_restore --list "$PLAIN_FILE" > /dev/null || { log "Decrypted file is not a pg_dump archive."; exit 3; }

export PGPASSWORD="${POSTGRES_PASSWORD}"
TABLES=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "select count(*) from information_schema.tables where table_schema='public'")
if [ "${TABLES:-0}" -gt 0 ] && [ "${RESTORE_FORCE:-0}" != "1" ]; then
    log "Target database $DB_NAME already has $TABLES tables. Set RESTORE_FORCE=1 to restore over it."
    exit 4
fi

log "Restoring into $DB_NAME on $DB_HOST..."
# --clean --if-exists drops objects the archive is about to recreate; --no-owner/--no-acl because
# the restoring role is the application role, not the original owner.
pg_restore -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
    --clean --if-exists --no-owner --no-acl --exit-on-error "$PLAIN_FILE" \
    || { log "pg_restore failed."; exit 5; }

log "Restore complete. Record the drill in docs/compliance/exercises/ (date, file, row counts checked)."
