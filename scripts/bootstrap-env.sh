#!/usr/bin/env sh
set -eu

output="${1:-.env}"
if [ -e "$output" ]; then
  echo "$output already exists; refusing to overwrite it." >&2
  exit 1
fi

if ! command -v openssl >/dev/null 2>&1; then
  echo "openssl is required to generate runtime secrets." >&2
  exit 1
fi

random_hex() {
  openssl rand -hex "$1"
}

random_b64() {
  openssl rand -base64 "$1" | tr -d '\n'
}

tenant_id="$(openssl rand -hex 16 | sed -E 's/(.{8})(.{4})(.{4})(.{4})(.{12})/\1-\2-\3-\4-\5/')"
certificate_password="$(random_b64 32)"
certificate_dir=".aegiscore"
certificate_path="$certificate_dir/data-protection.pfx"

umask 077
mkdir -p "$certificate_dir"
private_key="$certificate_dir/data-protection.key"
certificate="$certificate_dir/data-protection.crt"
openssl req -x509 -newkey rsa:3072 -sha256 -days 825 -nodes -subj "/CN=AegisCore Data Protection" -keyout "$private_key" -out "$certificate" >/dev/null 2>&1
PASSOUT="$certificate_password" openssl pkcs12 -export -out "$certificate_path" -inkey "$private_key" -in "$certificate" -passout env:PASSOUT >/dev/null 2>&1
rm -f "$private_key" "$certificate"
{
  echo "AEGISCORE_PORT=8080"
  echo "ASPNETCORE_ENVIRONMENT=Development"
  echo "AEGISCORE_DEMO_MODE=true"
  echo "AEGISCORE_INSTALLATION_COMPLETED=false"
  echo "TENANT_ID=$tenant_id"
  echo
  echo "POSTGRES_DB=aegiscore"
  echo "POSTGRES_USER=aegiscore"
  echo "POSTGRES_PASSWORD=$(random_hex 24)"
  echo "POSTGRES_ADMIN_USER=aegiscore_admin"
  echo "POSTGRES_ADMIN_PASSWORD=$(random_hex 24)"
  echo "REDIS_PASSWORD=$(random_hex 24)"
  echo
  echo "LICENSE_SERVER_URL=https://license.aegiscore.systems"
  echo "LICENSE_HMAC_SECRET=$(random_hex 32)"
  echo "AEGISCORE_ENCRYPTION_KEY=$(random_b64 32)"
  echo "AEGISCORE_PQC_KEY_BASE64=$(random_b64 32)"
  echo "AEGISCORE_JWT_SECRET=$(random_hex 48)"
  echo "GOVERNANCE_FEEDBACK_KEY_BASE64=$(random_b64 32)"
  echo "AEGISCORE_DATA_PROTECTION_CERTIFICATE_PATH=/run/aegiscore-secrets/data-protection.pfx"
  echo "AEGISCORE_DATA_PROTECTION_CERTIFICATE_PASSWORD=$certificate_password"
  echo
  echo "AI_PROVIDER=ollama"
  echo "OLLAMA_BASE_URL=http://host.docker.internal:11434/"
  echo "OLLAMA_MODEL=qwen2.5:7b"
  echo "OPENAI_API_KEY="
  echo "OPENAI_MODEL=gpt-5-mini"
} > "$output"

echo "Created $output with mode 600."
echo "Start AegisCore with: docker compose --env-file $output up -d --build --wait"
