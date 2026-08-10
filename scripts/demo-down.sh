#!/usr/bin/env sh
set -eu
ROOT="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
cd "$ROOT"
docker compose -f docker-compose.yml -f docker-compose.local-demo.yml down --remove-orphans "$@"
