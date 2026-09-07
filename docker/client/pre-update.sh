#!/bin/sh
set -eu
# EX_TEMPFAIL tells Watchtower to skip this replica when its peer is unavailable.
wget -q -T 5 -O /dev/null "http://${UPDATE_PEER:?}:8080/api/runtime/ready" || exit 75
touch /tmp/aegiscore-draining
# Allow ingress health checks to remove this replica before Docker stops it.
sleep 5
