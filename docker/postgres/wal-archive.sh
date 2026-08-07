#!/bin/sh
# -------------------------------------------------------------------
# wal-archive.sh — Postgres WAL segment archiver
#
# Called by Postgres archive_command on each WAL segment ready to be
# archived. Uploads the segment to S3-compatible object storage.
#
# Arguments (passed by Postgres):
#   $1 — %p : absolute path to the WAL segment file
#   $2 — %f : filename of the WAL segment
#
# Environment variables (must be set):
#   STORAGE_ENDPOINT      — S3-compatible endpoint URL
#   STORAGE_ACCESS_KEY    — access key
#   STORAGE_SECRET_KEY    — secret key
#   WAL_ARCHIVE_BUCKET    — bucket name
#   WAL_ARCHIVE_PREFIX    — key prefix inside the bucket (e.g. "wal")
#
# Supported backends (via S3 interop):
#   AWS S3, MinIO, Google Cloud Storage, Azure Blob Storage
# -------------------------------------------------------------------
set -eu

WAL_PATH="$1"
WAL_FILE="$2"

# --------------- validation ---------------
if [ -z "${STORAGE_ENDPOINT:-}" ]; then
    echo "ERROR: STORAGE_ENDPOINT is not set" >&2
    exit 1
fi
if [ -z "${STORAGE_ACCESS_KEY:-}" ]; then
    echo "ERROR: STORAGE_ACCESS_KEY is not set" >&2
    exit 1
fi
if [ -z "${STORAGE_SECRET_KEY:-}" ]; then
    echo "ERROR: STORAGE_SECRET_KEY is not set" >&2
    exit 1
fi
if [ -z "${WAL_ARCHIVE_BUCKET:-}" ]; then
    echo "ERROR: WAL_ARCHIVE_BUCKET is not set" >&2
    exit 1
fi

PREFIX="${WAL_ARCHIVE_PREFIX:-wal}"
S3_PATH="s3://${WAL_ARCHIVE_BUCKET}/${PREFIX}/${WAL_FILE}"

export AWS_ACCESS_KEY_ID="${STORAGE_ACCESS_KEY}"
export AWS_SECRET_ACCESS_KEY="${STORAGE_SECRET_KEY}"
export AWS_DEFAULT_REGION="${AWS_DEFAULT_REGION:-us-east-1}"

# --------------- upload ---------------
AWS_ARGS=""
if [ -n "${STORAGE_ENDPOINT}" ]; then
    AWS_ARGS="--endpoint-url ${STORAGE_ENDPOINT}"
fi

# shellcheck disable=SC2086
if ! aws s3 cp "$WAL_PATH" "$S3_PATH" --no-progress ${AWS_ARGS} 2>/tmp/wal-archive.err; then
    echo "ERROR: Failed to upload WAL segment $WAL_FILE to $S3_PATH" >&2
    cat /tmp/wal-archive.err >&2 2>/dev/null || true
    exit 1
fi

exit 0
