# Docker Optimization Guide

## Applied

- Multi-stage .NET 10 build.
- Project files copied before source to preserve restore cache.
- Runtime image contains only published API output.
- Non-root `appuser` runs the API.
- `curl` health check calls `/health`.
- `.dockerignore` excludes frontend, mobile, generated dependency folders, build output, local env files, and media artifacts.
- `scripts/start-and-open.sh` starts Compose, waits for health, and opens localhost.

## Local Commands

```bash
docker build -t nocturne-api:optimized .
docker compose --env-file .env.example up -d --build
curl http://localhost:8080/health
docker compose --env-file .env.example down
```

## Notes

The current .NET 10 base images are Debian-based because the official .NET 10 Alpine runtime can vary by platform availability. Use Alpine only after validating native dependencies such as PDF/OCR, gRPC, and globalization behavior.
