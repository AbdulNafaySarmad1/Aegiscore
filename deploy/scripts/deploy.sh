#!/usr/bin/env bash
# ─── AegisCore Blue-Green Deployment ──────────────────
# Usage: ./deploy.sh [blue-green|direct]
# Zero-downtime deployment via Caddy upstream swap
set -euo pipefail

DEPLOY_DIR="/opt/aegiscore"
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.prod.yml"
HEALTH_URL="http://localhost:8080/health/ready"
CADDY_API="http://localhost:2019"
MODE="${1:-direct}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

log() { echo "[$(date +'%H:%M:%S')] $*"; }

deploy_direct() {
    log "Direct deployment started"
    cd "$DEPLOY_DIR"
    docker compose -f "$COMPOSE_FILE" pull api
    docker compose -f "$COMPOSE_FILE" up -d --remove-orphans api

    for i in $(seq 1 30); do
        if curl -fsS "$HEALTH_URL" > /dev/null 2>&1; then
            log "Health check passed after ${i}s"
            return 0
        fi
        sleep 1
    done
    log "ERROR: Health check failed after 30s"
    return 1
}

deploy_blue_green() {
    log "Blue-Green deployment started"

    local active=$(docker inspect -f '{{.Name}}' $(docker ps -q --filter "name=aegiscore-api") 2>/dev/null | sed 's/^.*_//')
    local new_color="green"
    local new_container="aegiscore-api-green"
    local caddy_upstream="aegiscore-api:8080"

    if [[ "$active" == "green" ]]; then
        new_color="blue"
        new_container="aegiscore-api-blue"
    fi
    log "Active: $active → Deploying to: $new_color"

    cd "$DEPLOY_DIR"
    COMPOSE_PROJECT_NAME="aegiscore-$new_color" docker compose -f "$COMPOSE_FILE" -p "aegiscore-$new_color" pull api
    COMPOSE_PROJECT_NAME="aegiscore-$new_color" docker compose -f "$COMPOSE_FILE" -p "aegiscore-$new_color" up -d --remove-orphans api

    for i in $(seq 1 60); do
        if curl -fsS "http://localhost:9080/health/ready" > /dev/null 2>&1; then
            log "New deployment healthy after ${i}s"
            break
        fi
        sleep 1
    done

    log "Switching Caddy upstream to $new_color"
    curl -sS -X POST "$CADDY_API/config/apps/http/servers/srv0/routes/0/handle/0/routes/0/handle/0/upstreams" \
        -H "Content-Type: application/json" \
        -d "[{\"dial\": \"$new_container:8080\"}]"

    log "Waiting 30s for drain..."
    sleep 30

    log "Stopping previous deployment ($active)"
    docker stop "aegiscore-api-$active" 2>/dev/null || true

    log "Blue-Green deployment complete — new: $new_color"
}

log "AegisCore Deployment — Mode: $MODE"

case "$MODE" in
    blue-green) deploy_blue_green ;;
    direct) deploy_direct ;;
    *) log "Unknown mode: $MODE"; exit 1 ;;
esac

log "Deployment successful"
