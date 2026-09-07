#!/bin/sh
set -eu
case "${AEGISCORE_COMPONENT:-api}" in
  api) exec dotnet /app/AegisCore.API.dll ;;
  application) exec node /app/client/application.cjs ;;
  ingress) exec haproxy -W -db -f /app/client/haproxy.cfg ;;
  *) echo 'Unknown AEGISCORE_COMPONENT' >&2; exit 1 ;;
esac
