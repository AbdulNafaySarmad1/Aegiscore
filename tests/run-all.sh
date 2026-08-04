#!/usr/bin/env bash
set -Eeuo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:18080}"
CONCURRENCY="${CONCURRENCY:-32}"
REPORT_DIR="${REPORT_DIR:-tests/reports}"
REPORT="$REPORT_DIR/latest.md"
WORK="$(mktemp -d)"
PASS_COUNT=0
FAIL_COUNT=0

mkdir -p "$REPORT_DIR"
trap 'rm -rf "$WORK"' EXIT
printf '# AegisCore Abuse and Limit Test Report\n\n' > "$REPORT"
printf -- '- Started: `%s`\n- Base URL: `%s`\n- Concurrency: `%s`\n\n' "$(date -u +%FT%TZ)" "$BASE_URL" "$CONCURRENCY" >> "$REPORT"

record() {
  local state="$1" label="$2" detail="${3:-}"
  if [[ "$state" == PASS ]]; then
    PASS_COUNT=$((PASS_COUNT + 1))
    printf '%s\n' "- [x] **PASS** $label${detail:+: $detail}" >> "$REPORT"
  else
    FAIL_COUNT=$((FAIL_COUNT + 1))
    printf '%s\n' "- [ ] **FAIL** $label${detail:+: $detail}" >> "$REPORT"
  fi
}

check_command() {
  local label="$1"; shift
  if "$@" >"$WORK/command.out" 2>"$WORK/command.err"; then
    record PASS "$label"
  else
    record FAIL "$label" "$(tail -c 500 "$WORK/command.err" | tr '\n' ' ')"
  fi
}

request_status() {
  local method="$1" path="$2" body="${3:-}" output="$WORK/response-$RANDOM.json"
  if [[ -n "$body" ]]; then
    curl -sS --max-time 20 -o "$output" -w '%{http_code}' -X "$method" \
      -H 'Content-Type: application/json' --data "$body" "$BASE_URL$path"
  else
    curl -sS --max-time 20 -o "$output" -w '%{http_code}' -X "$method" "$BASE_URL$path"
  fi
}

expect_status() {
  local label="$1" expected="$2" method="$3" path="$4" body="${5:-}" status
  if status="$(request_status "$method" "$path" "$body")" && [[ "$status" == "$expected" ]]; then
    record PASS "$label" "HTTP $status"
  else
    record FAIL "$label" "expected HTTP $expected, got ${status:-request failure}"
  fi
}

parallel_session_reads() {
  seq 1 "$CONCURRENCY" | xargs -P "$CONCURRENCY" -I{} curl -fsS --max-time 20 "$BASE_URL/api/wizard/session" -o /dev/null
}

printf '\n## Basic availability\n\n' >> "$REPORT"
expect_status "Portal health" 200 GET /portal-health
expect_status "Liveness health" 200 GET /health/live
expect_status "Installation health" 200 GET /health/installation

printf '\n## Wizard state and trust boundaries\n\n' >> "$REPORT"
SESSION_JSON="$(curl -fsS --max-time 20 "$BASE_URL/api/wizard/session")"
SESSION_ID="$(printf '%s' "$SESSION_JSON" | sed -n 's/.*"sessionId":"\([^"]*\)".*/\1/p')"
if [[ "$SESSION_ID" =~ ^[0-9a-fA-F-]{36}$ ]]; then record PASS "Wizard session creation" "$SESSION_ID"; else record FAIL "Wizard session creation" "missing valid session id"; fi
if ! printf '%s' "$SESSION_JSON" | grep -q '"licenseKey"'; then record PASS "Wizard session redacts license keys"; else record FAIL "Wizard session redacts license keys" "credential field returned"; fi
expect_status "Invalid step rejected" 400 POST /api/wizard/step/99/save "{\"sessionId\":\"$SESSION_ID\",\"data\":{}}"
expect_status "Malformed JSON rejected" 400 POST /api/wizard/step/0/save '{"sessionId":'
expect_status "Unknown mode rejected" 422 POST /api/wizard/mode "{\"sessionId\":\"$SESSION_ID\",\"mode\":\"root\"}"
expect_status "Live mode without license rejected" 422 POST /api/wizard/mode "{\"sessionId\":\"$SESSION_ID\",\"mode\":\"live\"}"
expect_status "Cross-session save rejected" 422 POST /api/wizard/step/0/save "{\"sessionId\":\"00000000-0000-0000-0000-000000000000\",\"data\":{\"mode\":\"demo\"}}"
expect_status "Demo mode transition succeeds" 200 POST /api/wizard/mode "{\"sessionId\":\"$SESSION_ID\",\"mode\":\"demo\"}"

printf '\n## Abuse and resource limits\n\n' >> "$REPORT"
check_command "Concurrent wizard session reads ($CONCURRENCY)" parallel_session_reads
expect_status "Gateway configuration blocked in demo" 422 POST /api/wizard/gateways/raast/configure '{"MERCHANT_ID":"redacted"}'
expect_status "Finance posture remains gated before activation" 503 GET /api/finance/v2/security-posture

large_payload="$WORK/large.json"
{
  printf '{"sessionId":"%s","data":{"mode":"demo","padding":"' "$SESSION_ID"
  dd if=/dev/zero bs=262144 count=1 2>/dev/null | tr '\0' 'a'
  printf '"}}'
} > "$large_payload"
large_status="$(curl -sS --max-time 20 -o /dev/null -w '%{http_code}' -X POST -H 'Content-Type: application/json' --data-binary "@$large_payload" "$BASE_URL/api/wizard/step/0/save" || true)"
if [[ "$large_status" == 413 ]]; then record PASS "256 KiB wizard payload rejected at boundary" "HTTP $large_status"; else record FAIL "256 KiB wizard payload rejected at boundary" "HTTP ${large_status:-request failure}"; fi

printf '\n## Software verification\n\n' >> "$REPORT"
check_command "Backend unit and integration tests" dotnet test AegisCore.Tests/AegisCore.Tests.csproj -m:1 -p:UseSharedCompilation=false --no-restore --no-build
check_command "Frontend strict typecheck" npm --prefix AegisCore.Portal run typecheck
check_command "Frontend lint" npm --prefix AegisCore.Portal run lint
check_command "Frontend production build" npm --prefix AegisCore.Portal run build
check_command "Frontend dependency audit" npm --prefix AegisCore.Portal audit --audit-level=high
check_command "PQC backend/frontend interoperability" bash -c "dotnet run --project AegisCore.LedgerVerifier/AegisCore.LedgerVerifier.csproj --no-build -- --generate-ml-dsa-fixture '$WORK/pqc-interop.json' && node tests/verify-pqc-interop.mjs '$WORK/pqc-interop.json'"
check_command "EF model has no pending changes" dotnet ef migrations has-pending-model-changes --project AegisCore.Infrastructure/AegisCore.Infrastructure.csproj --startup-project AegisCore.API/AegisCore.API.csproj --no-build

printf '\n## Container verification\n\n' >> "$REPORT"
check_command "Compose services healthy" bash -c "docker compose -p aegiscore-ci --env-file .env.ci ps -a | grep -E 'healthy' | grep -c 'aegiscore-ci-' | grep -q '^4$'"

printf '\n## Summary\n\n- Passed: `%s`\n- Failed: `%s`\n' "$PASS_COUNT" "$FAIL_COUNT" >> "$REPORT"
if [[ "$FAIL_COUNT" -gt 0 ]]; then
  printf 'AegisCore chained tests failed: %s checks failed. Report: %s\n' "$FAIL_COUNT" "$REPORT" >&2
  exit 1
fi
printf 'AegisCore chained tests passed: %s checks. Report: %s\n' "$PASS_COUNT" "$REPORT"
