#!/bin/bash

# AegisCore Directory Cleanup Script
# Run from: ~/AegisCore/Source Code/Aegiscore code/
# Usage: bash cleanup_codebase.sh

set -e

echo "🔍 Scanning for garbage files..."
echo ""

# Define legitimate directories and files to KEEP
KEEP=(
  "AegisCore.API"
  "AegisCore.Application"
  "AegisCore.Contracts"
  "AegisCore.Domain"
  "AegisCore.Infrastructure"
  "AegisCore.Setup"
  "AegisCore.Tests"
  "AegisCore.Web"
  "Dockerfile"
  "docker-compose.uni.yml"
  "docs"
  "scripts"
  "build.log"
  "package.json"
  "package-lock.json"
  "node_modules"
  "server.py"
  "wizard.html"
  "ghcr.io"
)

# First — dry run, show what WILL be deleted
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🗑️  FILES/DIRS THAT WILL BE DELETED:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

GARBAGE_COUNT=0

while IFS= read -r -d '' item; do
  basename_item=$(basename "$item")
  
  # Check if it's in the keep list
  SHOULD_KEEP=false
  for keep_item in "${KEEP[@]}"; do
    if [[ "$basename_item" == "$keep_item" ]]; then
      SHOULD_KEEP=true
      break
    fi
  done

  # Flag garbage — anything with spaces+special chars that looks like 
  # a command/query/code snippet that got turned into a filename
  if [[ "$SHOULD_KEEP" == "false" ]]; then
    # Check for obvious garbage patterns
    if [[ "$basename_item" =~ ^[\'\"\`\$\{\}\[\]\(\)\|\\\/\;\:\,\.\<\>\!\@\#\%\^\&\*\+\=\~\`] ]] || \
       [[ "$basename_item" =~ "sudo " ]] || \
       [[ "$basename_item" =~ "docker " ]] || \
       [[ "$basename_item" =~ "curl " ]] || \
       [[ "$basename_item" =~ "redis-cli" ]] || \
       [[ "$basename_item" =~ "psql " ]] || \
       [[ "$basename_item" =~ "SELECT " ]] || \
       [[ "$basename_item" =~ "ORDER BY" ]] || \
       [[ "$basename_item" =~ "USING " ]] || \
       [[ "$basename_item" =~ "MEMORY" ]] || \
       [[ "$basename_item" =~ "PostgreSQL" ]] || \
       [[ "$basename_item" =~ "Let's produce" ]] || \
       [[ "$basename_item" =~ "└" ]] || \
       [[ "$basename_item" =~ "contributor" ]] || \
       [[ "$basename_item" =~ "hostssl" ]] || \
       [[ "$basename_item" =~ "modelBuilder" ]] || \
       [[ "$basename_item" =~ "gpg " ]] || \
       [[ "$basename_item" =~ "certbot" ]] || \
       [[ "$basename_item" =~ "systemctl" ]] || \
       [[ "$basename_item" =~ "usermod" ]] || \
       [[ "$basename_item" =~ "package: " ]] || \
       [[ "$basename_item" =~ "save 60" ]] || \
       [[ "$basename_item" =~ "Uptime" ]] || \
       [[ "$basename_item" == "}" ]] || \
       [[ "$basename_item" =~ "FinanceOfficer" ]] || \
       [[ "$basename_item" =~ "Grade: string" ]]; then
      echo "  🗑️  \"$basename_item\""
      ((GARBAGE_COUNT++))
    fi
  fi
done < <(find . -maxdepth 1 -print0)

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 Total garbage items found: $GARBAGE_COUNT"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

if [ "$GARBAGE_COUNT" -eq 0 ]; then
  echo "✅ No garbage found! Directory is clean."
  exit 0
fi

# Ask for confirmation before deleting
read -p "⚠️  Delete all $GARBAGE_COUNT garbage items? (yes/no): " CONFIRM

if [[ "$CONFIRM" != "yes" ]]; then
  echo "❌ Aborted. Nothing deleted."
  exit 0
fi

echo ""
echo "🧹 Deleting garbage files..."
echo ""

DELETED_COUNT=0

while IFS= read -r -d '' item; do
  basename_item=$(basename "$item")
  
  SHOULD_KEEP=false
  for keep_item in "${KEEP[@]}"; do
    if [[ "$basename_item" == "$keep_item" ]]; then
      SHOULD_KEEP=true
      break
    fi
  done

  if [[ "$SHOULD_KEEP" == "false" ]]; then
    if [[ "$basename_item" =~ ^[\'\"\`\$\{\}\[\]\(\)\|\\\/\;\:\,\.\<\>\!\@\#\%\^\&\*\+\=\~\`] ]] || \
       [[ "$basename_item" =~ "sudo " ]] || \
       [[ "$basename_item" =~ "docker " ]] || \
       [[ "$basename_item" =~ "curl " ]] || \
       [[ "$basename_item" =~ "redis-cli" ]] || \
       [[ "$basename_item" =~ "psql " ]] || \
       [[ "$basename_item" =~ "SELECT " ]] || \
       [[ "$basename_item" =~ "ORDER BY" ]] || \
       [[ "$basename_item" =~ "USING " ]] || \
       [[ "$basename_item" =~ "MEMORY" ]] || \
       [[ "$basename_item" =~ "PostgreSQL" ]] || \
       [[ "$basename_item" =~ "Let's produce" ]] || \
       [[ "$basename_item" =~ "└" ]] || \
       [[ "$basename_item" =~ "contributor" ]] || \
       [[ "$basename_item" =~ "hostssl" ]] || \
       [[ "$basename_item" =~ "modelBuilder" ]] || \
       [[ "$basename_item" =~ "gpg " ]] || \
       [[ "$basename_item" =~ "certbot" ]] || \
       [[ "$basename_item" =~ "systemctl" ]] || \
       [[ "$basename_item" =~ "usermod" ]] || \
       [[ "$basename_item" =~ "package: " ]] || \
       [[ "$basename_item" =~ "save 60" ]] || \
       [[ "$basename_item" =~ "Uptime" ]] || \
       [[ "$basename_item" == "}" ]] || \
       [[ "$basename_item" =~ "FinanceOfficer" ]] || \
       [[ "$basename_item" =~ "Grade: string" ]]; then
      rm -rf "$item"
      echo "  ✅ Deleted: \"$basename_item\""
      ((DELETED_COUNT++))
    fi
  fi
done < <(find . -maxdepth 1 -print0)

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ CLEANUP COMPLETE"
echo "   Deleted: $DELETED_COUNT garbage items"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📁 Remaining directory contents:"
ls -la
