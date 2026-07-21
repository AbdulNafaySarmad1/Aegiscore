#!/bin/bash
# AegisCore Pre-Deployment Hardcoding Audit
# Run from repo root: bash scripts/hardcode-audit.sh
# Exit 0 = PASS, Exit 1 = FAIL

set -euo pipefail
FAIL=0
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

G="grep -rn --include='*.cs' --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj --exclude='*Test*' --exclude='*Migration*' --exclude='*Seeder*'"

pass() { echo "  PASS"; }
warn() { echo "  WARN: $1"; }
fail() { echo "  FAIL: $1"; FAIL=1; }

echo "=== AegisCore Hardcoding Audit: $(date '+%Y-%m-%d %H:%M:%S') ==="

# [1] Institution names
echo -n "[1] Institution names..."
if git grep -n "SSUET\|Sir Syed University\|NED University\|UOK" -- '*.cs' -- ':!*Seeder*' -- ':!*Test*' 2>/dev/null | grep -q .; then fail "Hardcoded institution names"; else pass; fi

# [2] Secrets in committed files
echo -n "[2] Secrets..."
if git grep -n "sk-proj-\|sk-ant-\|Bearer eyJ" -- '*.cs' '*.json' '*.ts' ':!*appsettings.Development.json' ':!*Test*' 2>/dev/null | grep -q .; then fail "Potential secrets found"; else pass; fi

# [3] .env tracked by git
echo -n "[3] .env tracking..."
if git ls-files --error-unmatch .env 2>/dev/null; then fail ".env tracked by git"; else pass; fi

# [4] appsettings.json real values
echo -n "[4] appsettings secrets..."
if grep -n "password.*\"[^\"]\{8\}\|secret.*\"[^\"]\{8\}\|apikey.*\"[^\"]\{8\}" AegisCore.API/appsettings.json 2>/dev/null | grep -qv "placeholder\|your-\|\\$" 2>/dev/null; then fail "Real secrets in appsettings.json"; else pass; fi

# [5] R2 bucket names (hardcoded strings)
echo -n "[5] Hardcoded R2 bucket names..."
if git grep -n '"uni-academic-docs"\|"uni-legal-vault"\|"uni-student-photos"' -- '*.cs' ':!*Seeder*' ':!*Test*' | grep -q .; then fail "Hardcoded R2 bucket names"; else pass; fi

# [6] Hardcoded date strings
echo -n "[6] Hardcoded dates..."
if git grep -n '"2024-\|"2025-\|"2026-' -- '*.cs' ':!*Seeder*' ':!*Test*' ':!*Migration*' | grep -q .; then fail "Hardcoded dates"; else pass; fi

# [7] Hardcoded feature toggle comments
echo -n "[7] Feature toggle TODOs..."
if git grep -n "TODO: enable later\|disabled for now\|hardcoded true until" -- '*.cs' | grep -q .; then warn "Commented-out feature flags found"; else pass; fi

echo "=== Result: $([ $FAIL -eq 0 ] && echo 'PASS' || echo 'FAIL') ==="
exit $FAIL
