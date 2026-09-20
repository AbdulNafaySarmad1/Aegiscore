#!/usr/bin/env bash
# ─── Backup / restore drill ──────────────────────────────────────────────────
# Proves, against a real PostgreSQL server holding the real AegisCore schema, that:
#   1. backup.sh writes an encrypted archive and no plaintext dump survives on disk;
#   2. restore.sh brings that archive up in a DIFFERENT, empty database;
#   3. every table came back, and a marker row written before the backup is present after it.
#
# CI runs this on every push (.github/workflows/ci.yml, job "Database Migration (postgres)").
# Operators run it quarterly against their own installation and file the output under
# docs/compliance/exercises/ — that record is the evidence ISO 27001 A.8.13 asks for.
#
#   DB_HOST=localhost DB_PORT=5432 POSTGRES_USER=u POSTGRES_PASSWORD=p POSTGRES_DB=source \
#   BACKUP_ENCRYPTION_KEY=... ./backup-restore-drill.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export DB_HOST="${DB_HOST:-postgres}" DB_PORT="${DB_PORT:-5432}"
export POSTGRES_USER="${POSTGRES_USER:-aegiscore}" POSTGRES_PASSWORD="${POSTGRES_PASSWORD:?}"
SOURCE_DB="${POSTGRES_DB:-aegiscore}"
TARGET_DB="${DRILL_TARGET_DB:-${SOURCE_DB}_drill_$(date +%s)}"
export BACKUP_DIR="${BACKUP_DIR:-$(mktemp -d "${TMPDIR:-/tmp}/aegiscore-drill.XXXXXX")}"
export BACKUP_ENCRYPTION_KEY="${BACKUP_ENCRYPTION_KEY:?BACKUP_ENCRYPTION_KEY is required}"
export PGPASSWORD="$POSTGRES_PASSWORD"

log() { echo "[DRILL] $(date +'%H:%M:%S') $*"; }
q() { psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d "$1" -tAc "$2"; }

MARKER="drill-$(date +%Y%m%dT%H%M%S)-$RANDOM"
log "Source: $SOURCE_DB  Target: $TARGET_DB  Marker: $MARKER"

# 1. A marker row in a real, tenant-scoped table so the drill checks data, not just DDL.
q "$SOURCE_DB" "insert into \"TranslationKeys\" (\"Id\",\"TenantId\",\"Key\",\"Module\",\"Description\",\"CreatedAt\") values (gen_random_uuid(), '00000000-0000-0000-0000-000000000000', '$MARKER', 'drill', 'backup drill marker', now())" > /dev/null
SOURCE_TABLES=$(q "$SOURCE_DB" "select count(*) from information_schema.tables where table_schema='public'")

# 2. Backup.
BACKUP_FILE=$(POSTGRES_DB="$SOURCE_DB" "$HERE/backup.sh" | tail -n1)
[ -f "$BACKUP_FILE" ] || { log "backup.sh did not produce a file"; exit 1; }
head -c 8 "$BACKUP_FILE" | grep -q "Salted__" || { log "backup file is not encrypted"; exit 1; }
if ls "$BACKUP_DIR"/*.dump 2>/dev/null | grep -q .; then log "plaintext dump left on disk"; exit 1; fi
log "Encrypted archive: $BACKUP_FILE ($(du -h "$BACKUP_FILE" | cut -f1))"

# 3. Restore into a fresh database.
q postgres "create database \"$TARGET_DB\"" > /dev/null
cleanup() { q "$SOURCE_DB" "delete from \"TranslationKeys\" where \"Key\"='$MARKER'" > /dev/null || true; q postgres "drop database if exists \"$TARGET_DB\" with (force)" > /dev/null || true; }
trap cleanup EXIT
POSTGRES_DB="$TARGET_DB" "$HERE/restore.sh" "$BACKUP_FILE"

# 4. Assert.
TARGET_TABLES=$(q "$TARGET_DB" "select count(*) from information_schema.tables where table_schema='public'")
FOUND=$(q "$TARGET_DB" "select count(*) from \"TranslationKeys\" where \"Key\"='$MARKER'")
log "Tables: source=$SOURCE_TABLES restored=$TARGET_TABLES  Marker rows restored: $FOUND"
[ "$SOURCE_TABLES" = "$TARGET_TABLES" ] || { log "FAIL: table count differs"; exit 1; }
[ "$FOUND" = "1" ] || { log "FAIL: marker row missing after restore"; exit 1; }
log "PASS — restore verified. Elapsed: ${SECONDS}s"
