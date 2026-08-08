# Frontend API Handoff

## Transport

- Browser calls should use the Portal's same-origin `/api/*` and `/hubs/*` rewrites. The Portal already forwards those paths to `AEGISCORE_API_INTERNAL_URL`.
- Server-side Portal calls must forward the incoming `cookie` header. `src/lib/server-api.ts` already does this.
- Direct cross-site cookie use is not the supported deployment shape. Authentication cookies use `SameSite=Lax`; deploy the Portal rewrite and API over HTTPS.
- Existing non-browser integrations may continue to send `Authorization: Bearer <accessToken>`.
- Local JWT issuance and validation share `Jwt:Issuer` (default `https://aegiscore.pk`) and `Auth:Audience` (default `aegiscore-api`).

## Authentication

### Login

`POST /api/auth/login`

```json
{
  "email": "user@example.edu",
  "password": "..."
}
```

Successful responses retain the existing JSON token contract:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "tokenType": "Bearer",
  "expiresIn": 900
}
```

The same response also sets HttpOnly cookies:

- `aegiscore_access_token`: path `/`, 15-minute lifetime.
- `aegiscore_refresh_token`: path `/api/auth`, seven-day lifetime.
- Both cookies use `SameSite=Lax`; production cookies are `Secure`.

### Refresh

`POST /api/auth/refresh`

- Browser clients may send `{}`; the refresh token is read from the HttpOnly cookie.
- Bearer-style clients may continue to send `{ "refreshToken": "..." }`.
- Refresh tokens are consumed atomically and rotated. Reuse returns HTTP 401.
- A successful response returns the existing JSON token shape and replaces both cookies.

### Logout

`POST /api/auth/logout`

- Browser clients may send `{}`.
- Bearer-style clients may send `{ "refreshToken": "..." }`.
- Logout is idempotent, clears both cookies, and revokes the presented refresh-token family when it exists.

## Session

`GET /api/session` requires authentication and returns:

```json
{
  "userId": "guid",
  "tenantId": "guid",
  "studentId": null,
  "departmentId": null,
  "name": "user@example.edu",
  "roles": ["Student"],
  "permissions": ["Academic:Attendance:Read"]
}
```

- `roles` contains canonical backend role names and excludes role assignments that had expired when the access token was issued.
- `permissions` contains active assignments only and uses `Module:Resource:Action` strings.
- `studentId` and `departmentId` are nullable because the current identity model has no authoritative user-to-student or user-to-department relationship for local accounts.
- Seeded canonical role names are `SuperAdmin`, `VC`, `Registrar`, `Finance`, `ExamController`, `ITHead`, `HR`, `Faculty`, `Student`, `HOD`, `Admin`, and `Dean`.

## Authorization

- ASP.NET endpoint authorization is authoritative. Controller role attributes, permission policies, and the authenticated fallback policy run before Casbin.
- Casbin retains a narrow anonymous allowlist but no longer rejects requests that already passed authenticated endpoint authorization.
- Expired role assignments are excluded from newly issued JWTs and permission evaluation.

## SignalR

- Hubs: `/hubs/statemachine`, `/hubs/notifications`, and `/hubs/finance`.
- Hubs accept the standard SignalR `access_token` query parameter only under `/hubs/*`. Normal API routes do not accept query-string access tokens.
- Cookie authentication also works through the Portal's `/hubs/*` rewrite.
- Notification delivery is grouped by authenticated user; finance delivery is grouped by tenant.
- State-machine subscriptions are tenant-prefixed server-side. Clients cannot publish transition events through the hub.

## KPI Contracts

- `GET /api/finance/v2/dashboard/executive` exists for finance-capable roles. Use `kpis.revenue`, `kpis.expenses`, `kpis.cashPosition`, and `feeFunnel.{demanded,collected,overdue,open,paid}`. Collection rate must be derived from `collected / demanded`; outstanding should use `overdue` only if that is the intended UI label.
- Portal source `/api/attendance/report/student` is incomplete. The backend route requires `/api/attendance/report/student/{studentId}` and the session currently cannot supply an authoritative student ID.
- Portal source `/api/timetable/faculty` is incomplete. The backend route requires `/api/timetable/faculty/{facultyId}`.
- Portal sources `/api/obe/attainment`, `/api/grade-appeals?status=submitted`, `/api/classes?summary`, `/api/readiness`, and `/api/defaulter-prediction?summary` do not match available backend routes.
- No backend route was found for Portal sources `/api/grades/cgpa`, `/api/student/finance`, `/api/placement?summary`, `/api/portal-health`, or `/api/imports?status=failed`.
- Until those contracts are reconciled, KPI cards should retain their existing unavailable/placeholder state rather than infer or fabricate values.

## Errors

- Authentication failures return HTTP 401; authorization failures return HTTP 403.
- Validation failures return HTTP 400 and rate limits return HTTP 429.
- Error payloads expose a stable `error` or `message` field; unexpected failures include a trace identifier without stack details.

## Known Backend Constraint

- EF reports pending model changes. No migration was generated or applied during this handoff because the collided legacy/v2 tables require an explicit data-preserving migration design.
- A deployment should use either local JWT signing (`Jwt:PrivateKey` plus `Jwt:PublicKey`, or a non-production symmetric secret) or an external `Auth:Authority`. Enabling an external authority means locally issued login tokens are not accepted by that authority-based validation configuration.
