#!/usr/bin/env bash
# ─── AegisCore Emergency Rollback ─────────────────────
# Restores previous deployment. Target: <30 seconds.
set -euo pipefail

CADDY_API="http://localhost:2019"
DEPLOY_DIR="/opt/aegiscore"
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.prod.yml"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

log() { echo "[ROLLBACK] $(date +'%H:%M:%S') $*"; }

log "EMERGENCY ROLLBACK INITIATED"

# Find previous working container
PREVIOUS=$(docker ps -a --filter "name=aegiscore-api-" --format '{{.Names}} {{.State}}' | grep -v "$(docker ps --filter 'name=aegiscore-api' --format '{{.Names}}' | head -1)" | head -1 | awk '{print $1}')

if [ -n "$PREVIOUS" ]; then
    log "Restarting previous container: $PREVIOUS"
    docker start "$PREVIOUS"

    for i in $(seq 1 15); do
        if docker exec "$PREVIOUS" curl -fsS http://localhost:8080/health/ready > /dev/null 2>&1; then
            log "Previous container healthy after ${i}s"
            break
        fi
        sleep 1
    done

    # Switch Caddy back to previous container
    log "Switching Caddy back to $PREVIOUS"
    curl -sS -X POST "$CADDY_API/config/apps/http/servers/srv0/routes/0/handle/0/routes/0/handle/0/upstreams" \
        -H "Content-Type: application/json" \
        -d "[{\"dial\": \"$PREVIOUS:8080\"}]"

    log "Rollback complete"
else
    log "No previous container found — attempting fresh deploy"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" pull api
    docker compose -f "$COMPOSE_FILE" up -d --remove-orphans api

    for i in $(seq 1 30); do
        if curl -fsS http://localhost:8080/health/ready > /dev/null 2>&1; then
            log "Fresh deploy healthy after ${i}s"
            break
        fi
        sleep 1
    done
    log "Fresh deploy complete"
fi
