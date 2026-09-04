# P8 — Wizard consolidation and security

## Grounding results

All audited paths were confirmed before changes. The audit's `wwwroot/setup/index.html` path was located at `AegisCore.API/wwwroot/setup/index.html`. `WizardShell.tsx` was exactly 1,059 lines and the canonical controller was already backed by `WizardSession` through `WizardSessionService`.

## Canonical seven-step flow

`WizardController` and the Portal `/setup` page now expose exactly these steps:

1. University identity
2. Database connection (connect, test, migrate)
3. SSO configuration
4. Dynamic permission tree
5. AI model configuration
6. Bulk students/faculty/courses import
7. Review and go-live

Permission and import endpoints formerly owned by the enhanced controller are under `/api/wizard/step4/*` and `/api/wizard/step6/*`. Arbitrary role names of 1–64 characters are accepted. Region/state choices come from `Wizard:Regions:{country}` configuration and fall back to free text; no country-specific list remains in the client.

## Security controls

- Uninstalled startup requires `AEGISCORE_SETUP_TOKEN` to be a minimum 32-byte value encoded as 64 hexadecimal characters or base64.
- `SetupBootstrapTokenMiddleware` uses constant-time comparison for `X-Setup-Token` on every `/api/wizard/*` and `/api/setup/*` request.
- Invalid tokens receive `401` and `Invalid setup token. Check AEGISCORE_SETUP_TOKEN.`
- `SetupRateLimitMiddleware` allows 10 wizard requests per minute per IP; excess requests receive `429`, `Retry-After: 60`, and `Too many setup attempts. Wait 60 seconds.`
- The persisted wizard session is keyed by the SHA-256 bootstrap-token hash, not by client IP.
- Global exception handling and the configured CORS policy both run in installation mode.
- `APP_URL` and `AEGISCORE_FRONTEND_URL` provide runtime origins; the former fixed university origin was removed.

## Deletion log

- `AegisCore.API/Controllers/SetupController.cs`
- `AegisCore.API/Controllers/EnhancedSetupController.cs`
- `AegisCore.Portal/src/components/setup-wizard.tsx`
- `AegisCore.Portal/src/pages/wizard/Step2_5PaymentGateways.tsx` (an additional obsolete Pages Router fragment discovered by the production route build)
- `AegisCore.Setup/` (the complete Blazor project)
- `AegisCore.API/wwwroot/setup/index.html`
- `wizard.html`
- `server.py` (the obsolete server for root `wizard.html`)

The old Portal entry points `/completion`, `/imports`, and `/onboarding` redirect to `/setup`; no duplicate wizard component is imported.
