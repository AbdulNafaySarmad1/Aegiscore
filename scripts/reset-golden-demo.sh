#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${GOLDEN_DEMO_ENV_FILE:-${ROOT}/.env.demo}"
MODE="${1:-}"
readonly PROJECT="aegiscore-golden-demo"
readonly EXPECTED_DATABASE="aegiscore_golden_demo"
readonly SENTINEL_LABEL="com.aegiscore.golden-demo"

fail() {
  printf 'Golden demo reset refused: %s\n' "$1" >&2
  exit 1
}

[[ -f "${ENV_FILE}" ]] || fail "${ENV_FILE} does not exist. Create it from .env.demo.example."

read_env_value() {
  local key="$1"
  local required="${2:-true}"
  local line value="" found=false
  while IFS= read -r line || [[ -n "${line}" ]]; do
    line="${line%$'\r'}"
    [[ "${line}" == "${key}="* ]] || continue
    [[ "${found}" == false ]] || fail "${key} must appear only once in ${ENV_FILE}."
    value="${line#*=}"
    if [[ "${value}" == \"*\" && "${value}" == *\" ]]; then
      value="${value:1:${#value}-2}"
    elif [[ "${value}" == \'*\' && "${value}" == *\' ]]; then
      value="${value:1:${#value}-2}"
    fi
    found=true
  done < "${ENV_FILE}"
  [[ "${found}" == true || "${required}" == false ]] || fail "${key} is required in ${ENV_FILE}."
  ENV_VALUE="${value}"
}

read_env_value DEPLOYMENT_MODE; DEPLOYMENT_MODE="${ENV_VALUE}"
read_env_value ASPNETCORE_ENVIRONMENT; ASPNETCORE_ENVIRONMENT="${ENV_VALUE}"
read_env_value AEGISCORE_DEMO_MODE; AEGISCORE_DEMO_MODE="${ENV_VALUE}"
read_env_value AEGISCORE_INJECT_DEMO_IDENTITY; AEGISCORE_INJECT_DEMO_IDENTITY="${ENV_VALUE}"
read_env_value AEGISCORE_SEED_GOLDEN_DEMO; AEGISCORE_SEED_GOLDEN_DEMO="${ENV_VALUE}"
read_env_value POSTGRES_DB; POSTGRES_DB="${ENV_VALUE}"
read_env_value POSTGRES_ADMIN_USER; POSTGRES_ADMIN_USER="${ENV_VALUE}"
read_env_value TENANT_ID; TENANT_ID="${ENV_VALUE}"
read_env_value AEGISCORE_PORT false; AEGISCORE_PORT="${ENV_VALUE:-8080}"
read_env_value AEGISCORE_BIND_ADDRESS false; AEGISCORE_BIND_ADDRESS="${ENV_VALUE:-127.0.0.1}"
read_env_value DATABASE_URL false; DATABASE_URL="${ENV_VALUE}"
read_env_value AEGISCORE_DB_CONNECTION false; AEGISCORE_DB_CONNECTION="${ENV_VALUE}"

[[ "${DEPLOYMENT_MODE:-}" == "LOCAL" ]] || fail "DEPLOYMENT_MODE must be LOCAL."
[[ "${ASPNETCORE_ENVIRONMENT:-}" == "Development" ]] || fail "ASPNETCORE_ENVIRONMENT must be exactly Development."
[[ "${AEGISCORE_DEMO_MODE:-}" == "true" ]] || fail "AEGISCORE_DEMO_MODE must be true."
[[ "${AEGISCORE_INJECT_DEMO_IDENTITY:-}" == "false" ]] || fail "AEGISCORE_INJECT_DEMO_IDENTITY must be false."
[[ "${AEGISCORE_SEED_GOLDEN_DEMO:-}" == "true" ]] || fail "AEGISCORE_SEED_GOLDEN_DEMO must be true."
[[ "${POSTGRES_DB:-}" == "${EXPECTED_DATABASE}" ]] || fail "POSTGRES_DB must be ${EXPECTED_DATABASE}."
[[ "${AEGISCORE_BIND_ADDRESS}" == "127.0.0.1" || "${AEGISCORE_BIND_ADDRESS}" == "::1" ]] || fail "AEGISCORE_BIND_ADDRESS must be loopback-only."
[[ -z "${DATABASE_URL:-}" ]] || fail "DATABASE_URL must be unset; external databases are prohibited."
[[ -z "${AEGISCORE_DB_CONNECTION:-}" ]] || fail "AEGISCORE_DB_CONNECTION must be unset; use the isolated Compose database."
[[ -z "${DOCKER_HOST:-}" ]] || fail "DOCKER_HOST must be unset; remote Docker engines are prohibited."
[[ -z "${DOCKER_CONTEXT:-}" ]] || fail "DOCKER_CONTEXT must be unset; select the local context explicitly before reset."

docker_context="$(docker context show)"
docker_endpoint="$(docker context inspect "${docker_context}" --format '{{.Endpoints.docker.Host}}')"
[[ "${docker_endpoint}" == unix://* ]] || fail "Docker context ${docker_context} is not a local Unix-socket context."

command -v openssl >/dev/null 2>&1 || fail "openssl is required to generate disposable demo secrets."
export POSTGRES_ADMIN_PASSWORD="$(openssl rand -hex 32)"
export POSTGRES_PASSWORD="$(openssl rand -hex 32)"
export REDIS_PASSWORD="$(openssl rand -hex 32)"
export AEGISCORE_JWT_SECRET="$(openssl rand -hex 32)"
export LICENSE_HMAC_SECRET="$(openssl rand -hex 32)"
export AEGISCORE_ENCRYPTION_KEY="$(openssl rand -hex 32)"
export AEGISCORE_PQC_KEY_BASE64="$(openssl rand -base64 32)"
export GOVERNANCE_FEEDBACK_KEY_BASE64="$(openssl rand -base64 32)"

compose=(docker compose -p "${PROJECT}" --env-file "${ENV_FILE}" -f "${ROOT}/docker-compose.yml")
"${compose[@]}" config -q

verify_project_resources() {
  local id label
  while IFS= read -r id; do
    [[ -n "${id}" ]] || continue
    label="$(docker inspect --format "{{ index .Config.Labels \"${SENTINEL_LABEL}\" }}" "${id}")"
    [[ "${label}" == "true" ]] || fail "container ${id} belongs to ${PROJECT} but lacks the golden-demo sentinel."
  done < <(docker ps -aq --filter "label=com.docker.compose.project=${PROJECT}")
  while IFS= read -r id; do
    [[ -n "${id}" ]] || continue
    label="$(docker volume inspect --format "{{ index .Labels \"${SENTINEL_LABEL}\" }}" "${id}")"
    [[ "${label}" == "true" ]] || fail "volume ${id} belongs to ${PROJECT} but lacks the golden-demo sentinel."
  done < <(docker volume ls -q --filter "label=com.docker.compose.project=${PROJECT}")
  while IFS= read -r id; do
    [[ -n "${id}" ]] || continue
    label="$(docker network inspect --format "{{ index .Labels \"${SENTINEL_LABEL}\" }}" "${id}")"
    [[ "${label}" == "true" ]] || fail "network ${id} belongs to ${PROJECT} but lacks the golden-demo sentinel."
  done < <(docker network ls -q --filter "label=com.docker.compose.project=${PROJECT}")
}

verify_project_resources

if [[ "${MODE}" == "--check" ]]; then
  printf 'Golden demo reset safeguards passed for project %s and database %s.\n' "${PROJECT}" "${EXPECTED_DATABASE}"
  exit 0
fi
[[ "${MODE}" == "--confirm-reset-aegiscore-golden-demo" ]] || fail "pass --confirm-reset-aegiscore-golden-demo (or --check)."

printf 'Resetting isolated Compose project %s only.\n' "${PROJECT}"
"${compose[@]}" down --volumes --remove-orphans
"${compose[@]}" up -d --build --wait

"${compose[@]}" exec -T postgres psql -X -v ON_ERROR_STOP=1 \
  -v tenant_id="${TENANT_ID}" --username "${POSTGRES_ADMIN_USER}" --dbname "${POSTGRES_DB}" <<'SQL'
DO $$
BEGIN
  IF current_database() <> 'aegiscore_golden_demo' THEN
    RAISE EXCEPTION 'Unexpected database: %', current_database();
  END IF;
END $$;

SELECT "MigrationId"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20260809120000_AddAnalyticsIntelligencePlatform',
  '20260810120000_LinkUserStudentIdentity',
  '20260810121000_LinkAdmissionApplicant',
  '20260810122000_LinkUserStaffDepartment'
)
ORDER BY "MigrationId";

SELECT count(*) AS golden_users FROM "Users"
WHERE "TenantId" = :'tenant_id'::uuid;
SELECT count(*) AS telemetry_snapshots FROM "AnalyticsStudentTelemetrySnapshots"
WHERE "TenantId" = :'tenant_id'::uuid;
SELECT count(*) AS model_snapshots FROM "AnalyticsModelPredictionSnapshots"
WHERE "TenantId" = :'tenant_id'::uuid;
SQL

printf 'Golden demo is ready at http://localhost:%s\n' "${AEGISCORE_PORT:-8080}"
