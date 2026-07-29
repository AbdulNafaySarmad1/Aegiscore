#!/usr/bin/env bash
set -euo pipefail

echo "Starting Nocturne backend..."
docker compose --env-file .env.example up -d --build

deadline=$((SECONDS + 90))
until curl -fsS http://localhost:8080/health >/dev/null; do
  if [ "$SECONDS" -ge "$deadline" ]; then
    docker compose --env-file .env.example ps
    exit 1
  fi
  sleep 2
done

url="http://localhost:8080"
echo "Backend ready: $url"
case "${OSTYPE:-}" in
  darwin*) open "$url" ;;
  msys*|win32*) start "$url" ;;
  *) xdg-open "$url" >/dev/null 2>&1 || echo "Open $url in your browser." ;;
esac
