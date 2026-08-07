#!/usr/bin/env bash
# ─── AegisCore Cache Warmup Script ────────────────────
# Runs before demo or after deploy to warm Redis caches
set -euo pipefail

API_URL="${1:-http://localhost:8080}"
AUTH_HEADER="${2:-}"

log() { echo "[WARM] $(date +'%H:%M:%S') $*"; }

warm_endpoint() {
    local path="$1"
    local label="$2"
    curl -fsS -o /dev/null -w "%{http_code}" "$API_URL$path" ${AUTH_HEADER:+-H "Authorization: Bearer $AUTH_HEADER"} 2>/dev/null
    echo " $label"
}

log "Warming caches for $API_URL"

warm_endpoint /health/ready "health"
warm_endpoint /api/wizard/demo-status "demo-status"
warm_endpoint /api/finance/v2/dashboard/executive "finance-dashboard" &
warm_endpoint /api/finance/v2/dashboard/operations "operations-dashboard" &
warm_endpoint /api/bi/dashboards "bi-dashboards" &
warm_endpoint /api/notifications/templates "notifications" &
warm_endpoint /api/curriculum/versions/latest/graph "curriculum" &
wait

log "Cache warmup complete"
