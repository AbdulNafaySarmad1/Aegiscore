#!/usr/bin/env sh
set -eu

ROOT="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
cd "$ROOT"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required." >&2
  exit 1
fi
if ! docker compose version >/dev/null 2>&1; then
  echo "Docker Compose v2 is required." >&2
  exit 1
fi

if [ ! -f .env ]; then
  echo "No .env found; generating a local demo environment..."
  printf '1\n' | sh ./scripts/bootstrap-env.sh .env
fi

# The local demo override deliberately runs ASP.NET in the non-production Demo
# environment, marks installation complete, seeds the golden universe, and
# configures the IoBM attendance geofence. Secrets still come from .env.
docker compose -f docker-compose.yml -f docker-compose.local-demo.yml config --quiet
docker compose -f docker-compose.yml -f docker-compose.local-demo.yml up -d --build

PORT="${AEGISCORE_PORT:-8080}"
URL="http://127.0.0.1:${PORT}"

printf 'Waiting for AegisCore at %s' "$URL"
i=0
while [ "$i" -lt 90 ]; do
  if curl -fsS --max-time 4 "$URL/health/live" >/dev/null 2>&1; then
    echo ""
    echo "AegisCore demo is ready."
    echo "Open: $URL/login"
    echo "Golden student: student@iobm.demo / Koenigsegg\$One1"
    echo "Faculty: faculty@iobm.demo / Pagani\$Huayra3"
    echo "Finance: finance@iobm.demo / Porsche\$GT3RS"
    echo "Registrar: registrar@iobm.demo / Ferrari\$Roma22"
    echo "Vice Chancellor: vc@iobm.demo / Lambo\$Aventador1"
    exit 0
  fi
  printf '.'
  i=$((i + 1))
  sleep 4
done

echo ""
echo "AegisCore did not become healthy in time. Recent container status/logs:" >&2
docker compose -f docker-compose.yml -f docker-compose.local-demo.yml ps >&2 || true
docker compose -f docker-compose.yml -f docker-compose.local-demo.yml logs --no-color --tail=160 api portal >&2 || true
exit 1
