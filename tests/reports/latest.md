# AegisCore Abuse and Limit Test Report

- Started: `2026-07-31T12:31:44Z`
- Base URL: `http://127.0.0.1:18080`
- Concurrency: `32`


## Basic availability

- [x] **PASS** Portal health: HTTP 200
- [x] **PASS** Liveness health: HTTP 200
- [x] **PASS** Installation health: HTTP 200

## Wizard state and trust boundaries

- [x] **PASS** Wizard session creation: dbc1e875-8d3e-4255-841d-7dc31b418e96
- [x] **PASS** Wizard session redacts license keys
- [x] **PASS** Invalid step rejected: HTTP 400
- [x] **PASS** Malformed JSON rejected: HTTP 400
- [x] **PASS** Unknown mode rejected: HTTP 422
- [x] **PASS** Live mode without license rejected: HTTP 422
- [x] **PASS** Cross-session save rejected: HTTP 422
- [x] **PASS** Demo mode transition succeeds: HTTP 200

## Abuse and resource limits

- [x] **PASS** Concurrent wizard session reads (32)
- [x] **PASS** Gateway configuration blocked in demo: HTTP 422
- [x] **PASS** Finance posture remains gated before activation: HTTP 503
- [x] **PASS** 256 KiB wizard payload rejected at boundary: HTTP 413

## Software verification

- [x] **PASS** Backend unit and integration tests
- [x] **PASS** Frontend strict typecheck
- [x] **PASS** Frontend lint
- [x] **PASS** Frontend production build
- [x] **PASS** Frontend dependency audit
- [x] **PASS** EF model has no pending changes

## Container verification

- [x] **PASS** Compose services healthy

## Summary

- Passed: `22`
- Failed: `0`
