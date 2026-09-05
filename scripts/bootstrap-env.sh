#!/usr/bin/env sh
# -------------------------------------------------------------------
# bootstrap-env.sh — AegisCore environment generator
#
# Generates a hardened .env file with cryptographic secrets, database
# credentials, and deployment-specific configuration.
#
# Usage:
#   ./scripts/bootstrap-env.sh             # default: write .env in CWD
#   ./scripts/bootstrap-env.sh /path/.env  # write to a custom path
#
# Modes (interactive):
#   1. LOCAL  — all infra on this machine (full secrets, FILE provider)
#   2. CLOUD  — managed DB/Redis, external secret store
#   3. HYBRID — primary DB local, replica + object storage in cloud
#
# Safety: never overwrites an existing .env without confirmation.
# Requires: sh (POSIX), openssl, docker, docker compose (plugin or v2)
# -------------------------------------------------------------------
set -eu

# --------------- helpers ---------------
warn()  { printf '\033[33m[WARN]\033[0m  %s\n' "$*" >&2; }
err()   { printf '\033[31m[ERROR]\033[0m %s\n' "$*" >&2; exit 1; }
info()  { printf '\033[36m[INFO]\033[0m  %s\n' "$*" >&2; }
ok()    { printf '\033[32m[OK]\033[0m    %s\n' "$*" >&2; }

random_hex() { openssl rand -hex "$1"; }
random_b64() { openssl rand -base64 "$1" | tr -d '\n'; }

ask_deployment_mode() {
    echo "" >&2
    echo "Which deployment mode?" >&2
    echo "  (1) LOCAL  — everything on this machine" >&2
    echo "  (2) CLOUD  — managed services, external secrets" >&2
    echo "  (3) HYBRID — primary DB local, replica + storage in cloud" >&2
    printf "Enter 1, 2, or 3 [1]: " >&2
    read -r mode
    mode="${mode:-1}"
    case "$mode" in
        1|LOCAL|local)   echo "LOCAL" ;;
        2|CLOUD|cloud)   echo "CLOUD" ;;
        3|HYBRID|hybrid) echo "HYBRID" ;;
        *) err "Invalid choice: $mode" ;;
    esac
}

ask_secrets_provider() {
    echo "" >&2
    echo "Which secrets provider for CLOUD mode?" >&2
    echo "  [1] none                 (manage env vars manually)" >&2
    echo "  [2] azure-keyvault       (Azure Key Vault)" >&2
    echo "  [3] aws-secrets-manager  (AWS Secrets Manager)" >&2
    echo "  [4] gcp-secret-manager   (GCP Secret Manager)" >&2
    echo "  [5] infisical            (Infisical)" >&2
    echo "  [6] hashicorp-vault      (HashiCorp Vault)" >&2
    printf "Enter 1-6 [1]: " >&2
    read -r choice
    choice="${choice:-1}"
    case "$choice" in
        1) echo "NONE" ;;
        2) echo "AZURE_KEYVAULT" ;;
        3) echo "AWS_SECRETS_MANAGER" ;;
        4) echo "GCP_SECRET_MANAGER" ;;
        5) echo "INFISICAL" ;;
        6) echo "HASHICORP_VAULT" ;;
        *) err "Invalid choice: $choice" ;;
    esac
}

print_secrets_provider_instructions() {
    provider="$1"
    echo "" >&2
    info "Secrets provider: $provider"
    case "$provider" in
        AZURE_KEYVAULT)
            echo "  Set these env vars:" >&2
            echo "    AZURE_KEY_VAULT_URL=https://<vault-name>.vault.azure.net/" >&2
            echo "    AZURE_TENANT_ID=<tenant-id>" >&2
            echo "    AZURE_CLIENT_ID=<client-id>" >&2
            echo "    AZURE_CLIENT_SECRET=<client-secret>" >&2
            ;;
        AWS_SECRETS_MANAGER)
            echo "  Set these env vars:" >&2
            echo "    AWS_REGION=<region>" >&2
            echo "    AWS_ACCESS_KEY_ID=<access-key>" >&2
            echo "    AWS_SECRET_ACCESS_KEY=<secret-key>" >&2
            ;;
        GCP_SECRET_MANAGER)
            echo "  Set these env vars:" >&2
            echo "    GOOGLE_PROJECT_ID=<project-id>" >&2
            echo "    GOOGLE_APPLICATION_CREDENTIALS=/path/to/service-account.json" >&2
            ;;
        INFISICAL)
            echo "  Set these env vars:" >&2
            echo "    INFISICAL_URL=https://app.infisical.com" >&2
            echo "    INFISICAL_TOKEN=<service-token>" >&2
            echo "    INFISICAL_WORKSPACE_ID=<workspace-id>" >&2
            echo "    INFISICAL_ENVIRONMENT=<env-name>" >&2
            ;;
        HASHICORP_VAULT)
            echo "  Set these env vars:" >&2
            echo "    VAULT_ADDR=https://vault.example.com" >&2
            echo "    VAULT_TOKEN=<token>" >&2
            echo "    VAULT_MOUNT_PATH=secret" >&2
            echo "    VAULT_SECRET_PATH=aegiscore" >&2
            ;;
    esac
}

mask_value() {
    # Print the key and a masked value (first 4 chars + ****)
    key="$1" value="$2"
    if [ -z "$value" ]; then
        printf '  %s = (empty)\n' "$key"
    else
        printf '  %s = %s****\n' "$key" "$(printf '%s' "$value" | cut -c1-4)"
    fi
}

# --------------- OS detection ---------------
OS="$(uname -s)"
case "$OS" in
    Linux)  ok "Detected OS: Linux" ;;
    Darwin) ok "Detected OS: macOS" ;;
    *)      err "Unsupported OS: $OS. AegisCore requires Linux or macOS." ;;
esac

# --------------- dependency checks ---------------
check_cmd() {
    cmd="$1" label="${2:-$1}"
    if ! command -v "$cmd" >/dev/null 2>&1; then
        err "$label is required but not found. Please install it and try again."
    fi
}
check_cmd openssl "openssl"
check_cmd docker   "docker"

# Prefer docker compose (plugin), fall back to docker-compose (v2 standalone)
if docker compose version >/dev/null 2>&1; then
    ok "docker compose (plugin) found"
elif command -v docker-compose >/dev/null 2>&1; then
    ok "docker-compose (standalone) found"
else
    err "docker compose (plugin) or docker-compose (v2) is required but not found."
fi

# --------------- output path ---------------
OUTPUT="${1:-.env}"
if [ -e "$OUTPUT" ]; then
    printf '\033[33m%s\033[0m' "$OUTPUT already exists. Overwrite? (y/N) " >&2
    read -r confirm
    case "$confirm" in
        [Yy]|[Yy][Ee][Ss]) ok "Overwriting $OUTPUT" ;;
        *) err "Aborted. $OUTPUT was not modified." ;;
    esac
fi

info "Generating AegisCore secrets..."

# --------------- common crypto secrets ---------------
tenant_id="$(openssl rand -hex 16 | sed -E 's/(.{8})(.{4})(.{4})(.{4})(.{12})/\1-\2-\3-\4-\5/')"
certificate_password="$(random_b64 32)"
certificate_dir=".aegiscore"
certificate_path="$certificate_dir/data-protection.pfx"

umask 077
mkdir -p "$certificate_dir"
private_key="$certificate_dir/data-protection.key"
certificate="$certificate_dir/data-protection.crt"
openssl req -x509 -newkey rsa:3072 -sha256 -days 825 -nodes \
    -subj "/CN=AegisCore Data Protection" \
    -keyout "$private_key" -out "$certificate" >/dev/null 2>&1
PASSOUT="$certificate_password" openssl pkcs12 -export \
    -out "$certificate_path" -inkey "$private_key" -in "$certificate" \
    -passout env:PASSOUT >/dev/null 2>&1
rm -f "$private_key" "$certificate"
ok "Data-protection certificate created at $certificate_path"

# --------------- deployment mode ---------------
DEPLOYMENT_MODE="$(ask_deployment_mode)"
ok "Deployment mode: $DEPLOYMENT_MODE"

# --------------- generate env sections ---------------
registry_db_password="$(random_hex 24)"
{
    echo "# ===== AegisCore environment — generated by bootstrap-env.sh ====="
    echo "# Deployment mode: $DEPLOYMENT_MODE"
    echo "# Generated: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
    echo ""
    echo "AEGISCORE_PORT=8080"
    echo "ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}"
    echo "AEGISCORE_DEMO_MODE=true"
    echo "AEGISCORE_INSTALLATION_COMPLETED=false"
    echo "TENANT_ID=$tenant_id"
    echo "DEPLOYMENT_MODE=$DEPLOYMENT_MODE"
    echo ""

    case "$DEPLOYMENT_MODE" in
        LOCAL)
            echo "SECRETS_PROVIDER=FILE"
            echo ""
            echo "DB_HOST=postgres"
            echo "DB_PORT=5432"
            echo "DB_NAME=aegiscore"
            echo "DB_USER=aegiscore"
            echo "DB_PASS=$registry_db_password"
            echo "POSTGRES_DB=aegiscore"
            echo "POSTGRES_USER=aegiscore"
            echo "POSTGRES_PASSWORD=$registry_db_password"
            echo "POSTGRES_ADMIN_USER=aegiscore_admin"
            echo "POSTGRES_ADMIN_PASSWORD=$(random_hex 24)"
            echo "REDIS_PASSWORD=$(random_hex 24)"
            echo ""
            ;;

        CLOUD)
            SECRETS_PROVIDER="$(ask_secrets_provider)"
            echo "SECRETS_PROVIDER=$SECRETS_PROVIDER"
            echo ""
            echo "# ----- Managed Database Connection -----"
            echo "# Fill these with your cloud provider's connection details:"
            echo "DATABASE_URL="
            echo "DB_HOST="
            echo "DB_PORT=5432"
            echo "DB_NAME=aegiscore"
            echo "DB_USER="
            echo "DB_PASS="
            echo "POSTGRES_DB=aegiscore"
            echo "POSTGRES_USER="
            echo "POSTGRES_PASSWORD="
            echo ""
            echo "# ----- Managed Redis Connection -----"
            echo "REDIS_URL="
            echo "REDIS_PASSWORD="
            echo ""
            print_secrets_provider_instructions "$SECRETS_PROVIDER"
            ;;

        HYBRID)
            echo "SECRETS_PROVIDER=FILE"
            echo ""
            echo "# ----- Primary Database (local) -----"
            echo "DB_HOST=postgres"
            echo "DB_PORT=5432"
            echo "DB_NAME=aegiscore"
            echo "DB_USER=aegiscore"
            echo "DB_PASS=$registry_db_password"
            echo "POSTGRES_DB=aegiscore"
            echo "POSTGRES_USER=aegiscore"
            echo "POSTGRES_PASSWORD=$registry_db_password"
            echo "POSTGRES_ADMIN_USER=aegiscore_admin"
            echo "POSTGRES_ADMIN_PASSWORD=$(random_hex 24)"
            echo "REDIS_PASSWORD=$(random_hex 24)"
            echo ""
            echo "# ----- Cloud Replica (managed) -----"
            echo "# Provide the read-replica connection details for cross-region sync:"
            echo "DATABASE_REPLICA_URL="
            echo "DATABASE_REPLICA_USER="
            echo "DATABASE_REPLICA_PASSWORD="
            echo ""
            echo "# ----- S3-Compatible Object Storage (WAL archiving) -----"
            echo "STORAGE_ENDPOINT="
            echo "STORAGE_ACCESS_KEY="
            echo "STORAGE_SECRET_KEY="
            echo "WAL_ARCHIVE_BUCKET=aegiscore-wal"
            echo "WAL_ARCHIVE_PREFIX=wal"
            echo ""
            printf '\033[33m%s\033[0m\n' "[WARN]  HYBRID mode: fill DATABASE_REPLICA_URL and STORAGE_* before deploying." >&2
            ;;
    esac

    echo "LICENSE_SERVER_URL=https://license.aegiscore.systems"
    echo "LICENSE_OFFLINE_MODE=false"
    echo "LICENSE_HMAC_SECRET=$(random_hex 32)"
    echo "AEGISCORE_ENCRYPTION_KEY=$(random_b64 32)"
    echo "PQC_MLKEM_PUBLIC_KEY=$(random_b64 32)"
    echo "PQC_MLKEM_PRIVATE_KEY=$(random_b64 32)"
    echo "PQC_MLDSA_PUBLIC_KEY=$(random_b64 32)"
    echo "PQC_MLDSA_PRIVATE_KEY=$(random_b64 32)"
    echo "AEGISCORE_PQC_KEY_BASE64=$(random_b64 32)"
    echo "AEGISCORE_JWT_SECRET=$(random_hex 48)"
    echo "APP_URL=${APP_URL:-http://localhost:3000}"
    echo "APP_ENV=${ASPNETCORE_ENVIRONMENT:-Development}"
    echo "APP_SECRET=$(random_hex 48)"
    echo "SESSION_LIFETIME=900"
    echo "AEGISCORE_SETUP_TOKEN=$(random_hex 32)"
    echo "CF_TURNSTILE_SITE_KEY="
    echo "CF_TURNSTILE_SECRET_KEY="
    echo "SSO_PROVIDER=local"
    echo "SSO_CLIENT_ID="
    echo "SSO_CLIENT_SECRET="
    echo "SSO_TENANT_ID="
    echo "SSO_CALLBACK_URL="
    echo "GOVERNANCE_FEEDBACK_KEY_BASE64=$(random_b64 32)"
    echo "AEGISCORE_DATA_PROTECTION_CERTIFICATE_PATH=/run/aegiscore-secrets/data-protection.pfx"
    echo "AEGISCORE_DATA_PROTECTION_CERTIFICATE_PASSWORD=$certificate_password"
    echo ""
    echo "AI_PROVIDER=ollama"
    echo "AI_ENDPOINT=http://host.docker.internal:11434/"
    echo "AI_MODEL=qwen2.5:7b"
    echo "AI_API_KEY="

} > "$OUTPUT"

chmod 600 "$OUTPUT"

# --------------- masked summary ---------------
echo ""
info "----- Generated secrets (masked) -----"
while IFS='=' read -r key value; do
    case "$key" in
        ""|\#*) continue ;;
        DB_PASS|POSTGRES_PASSWORD|POSTGRES_ADMIN_PASSWORD|REDIS_PASSWORD|LICENSE_HMAC_SECRET|\
        APP_SECRET|AEGISCORE_SETUP_TOKEN|PQC_MLKEM_PRIVATE_KEY|PQC_MLDSA_PRIVATE_KEY|\
        AEGISCORE_ENCRYPTION_KEY|AEGISCORE_PQC_KEY_BASE64|AEGISCORE_JWT_SECRET|\
        GOVERNANCE_FEEDBACK_KEY_BASE64|AEGISCORE_DATA_PROTECTION_CERTIFICATE_PASSWORD|\
        STORAGE_ACCESS_KEY|STORAGE_SECRET_KEY|DATABASE_REPLICA_PASSWORD)
            mask_value "$key" "$value"
            ;;
        TENANT_ID)
            printf '  %s = %s\n' "$key" "$value"
            ;;
    esac
done < "$OUTPUT"
echo ""

ok "Created $OUTPUT (mode 600)"
echo ""
case "$DEPLOYMENT_MODE" in
    LOCAL)
        echo "Next step:  ./scripts/deploy.sh local" >&2
        ;;
    CLOUD)
        echo "Next step: Fill in DATABASE_URL, REDIS_URL, POSTGRES_USER, POSTGRES_PASSWORD, and REDIS_PASSWORD in $OUTPUT, then run:" >&2
        echo "  ./scripts/deploy.sh cloud" >&2
        ;;
    HYBRID)
        echo "Next step: Fill in DATABASE_REPLICA_URL and STORAGE_* in $OUTPUT, then run:" >&2
        echo "  ./scripts/deploy.sh hybrid" >&2
        ;;
esac
