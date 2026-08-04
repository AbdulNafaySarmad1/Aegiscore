# Repository Split Report

Date: 2026-07-26

## Created Working Trees

- `aegiscore-backend/`: API, application, contracts, domain, infrastructure, tests, plugin SDK/CLI, Docker, Compose, Kubernetes, backend CI.
- `aegiscore-frontend/`: React/Vite portal without `node_modules` or `dist`.
- `aegiscore-mobile/`: Expo app under `react-native/`, native Kotlin under `android/`, native Swift under `ios/`.
- `aegiscore-android/`: standalone native Android app with Android-specific README, gitignore, and CI workflows.

## Added Split Artifacts

- Backend README and start script.
- Frontend Dockerfile, README, and env example.
- Mobile README and env example.
- Android README, contributing guide, gitignore, and GitHub Actions build/deploy workflows.
- Root and backend Docker ignore rules that exclude generated output, secrets, portal/mobile source, manuals, and local IDE files from backend image context.

## Validation

- `aegiscore-backend`: `dotnet test AegisCore.slnx -m:1 -p:BuildInParallel=false` passed, 51 tests; NuGet vulnerability audit found no vulnerable packages.
- `aegiscore-frontend`: `npm run build` passed; full `npm audit` found 0 vulnerabilities.
- `aegiscore-mobile/react-native`: `npm run typecheck` passed; `npm test` passed, 1 test; full `npm audit` found 0 vulnerabilities.
- Root Compose stack: `docker compose --env-file .env.example up -d --build` built and started API/PostgreSQL/Redis; `/health` returned `Healthy`; `/swagger/v1/swagger.json` returned OpenAPI `3.0.1`.

## Not Performed

- No source directories were removed from the monolith.
- No git repositories were initialized or pushed.
- No GitHub private repositories were created.

Those actions are intentionally deferred because they alter repository ownership/history and require explicit credentials/approval.
