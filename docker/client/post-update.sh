#!/bin/sh
set -eu
# Intentionally wait without a timeout: a failed release must not stop the other replica.
until wget -q -T 5 -O /dev/null http://127.0.0.1:8080/api/runtime/ready; do sleep 2; done
