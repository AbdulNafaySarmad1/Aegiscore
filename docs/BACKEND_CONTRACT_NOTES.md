# Backend Contract Notes

## 2026-08-09 Backend Hardening

### Browser authentication and session

- Login and refresh now set HttpOnly access/refresh cookies while retaining the existing JSON Bearer-token response.
- Refresh and logout accept either the existing `refreshToken` request property or the refresh cookie. Browser clients may post an empty JSON object.
- Refresh-token consumption is atomic; a concurrently reused token returns HTTP 401.
- Logout is idempotent and clears browser cookies even when no active refresh token is found.
- `GET /api/session` now includes active permission keys in `Module:Resource:Action` form.
- JWT issuance and permission evaluation exclude expired role assignments.
- Local JWT issuer/audience values now match validation configuration; symmetric validation now verifies issuer as well as audience.
- SignalR accepts `access_token` only for `/hubs/*`; state-machine clients can subscribe within their tenant but can no longer publish transitions.
- Detailed frontend usage and known KPI route mismatches are recorded in `docs/FRONTEND_API_HANDOFF.md`.

### Election ballot token

- Endpoint: `POST /api/campus/v2/elections/{electionId}/ballot`
- Previous request: `{ "electionId": "...", "choices": [...] }`
- New request: `{ "electionId": "...", "choices": [...], "voterToken": "..." }`
- Reason: the previous endpoint did not authenticate ballot eligibility and allowed repeated voting. The token is now single-use and only its SHA-256 hash is persisted.
- The existing issue-token response property remains named `tokenHash` for response-shape compatibility, but its value is now the one-time plaintext bearer token. Clients must submit it as `voterToken`; it cannot be recovered after issuance.
- Requests whose route election ID differs from the body election ID now return HTTP 400.

### Legacy election tally

- Endpoint: `POST /api/elections/{electionId}/tally`
- Previous implementation manufactured a President result with 150 votes even when no tally had been persisted.
- New behavior returns only persisted results for the tenant-scoped election. It may return an empty result set until a real tally process stores results.

### Semester identifiers

- Endpoints: legacy defaulter forecast, Dean's List processing, leaderboard generation, and MORA submission routes.
- Previous implementations accepted free-form semester labels while writing entities that now require `Guid SemesterId`.
- New behavior requires the existing string parameter to contain a non-empty GUID and returns HTTP 400 for invalid values.
- Parameter names and routes are unchanged.

### Frontend impact

- Portal source was not modified.
- Any consumer of the v2 election ballot endpoint must add `voterToken` to the request body.
- The current Portal election page uses a separate legacy route shape that was already inconsistent with the backend route inventory; frontend reconciliation remains required.
