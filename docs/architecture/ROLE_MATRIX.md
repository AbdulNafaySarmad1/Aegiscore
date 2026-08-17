# AegisCore ROLE_MATRIX.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.

## 1. Canonical roles (backend `[Authorize(Roles=...)]`)

Backend role strings in JWTs (`JwtTokenService.cs:64-70`): `SuperAdmin`, `Admin`, `VC`, `Registrar`, `ExamController`, `Finance`, `Finance Controller`, `CFO`, `HR`, `HRManager`, `IT`, `ITHead`, `HOD`, `Dean`, `Faculty`, `Employee`, `Student`, `OBECoordinator`, `Auditor`, `Chancellor` (board), plus module roles (`Security`, `Welfare`, etc. per RoleConfig).

## 2. Role capability matrix (from `docs/ROLE_CAPABILITIES.md` + code)

| Role | Can do (representative) | Explicit boundaries |
|---|---|---|
| **SuperAdmin** | Everything; Casbin `/*` bypass; license activate/override; admin/replication/sync-lag/zero-trust; developer keys/webhooks | Full admin |
| **Admin** | Casbin `/*`; identity/role/permission CRUD; most admin surfaces | Inherits below |
| **VC** | Executive dashboard, KPIs, board, governance; `allowedRoutes = ["/*"]` | High-level |
| **Registrar** | Degree audit, curriculum, examinations, student records, SCIM | — |
| **ExamController** | Grade approval queue, examinations, freeze, grade appeals, convocations | No finance |
| **Dean** | Cross-department academic oversight | — |
| **HOD** | Own-department grade approval queue, courses, attendance, appeals | **Department-scoped** (see `GradeApprovalDiscoveryController.cs:72-80`) |
| **Faculty** | Grade entry (own courses), attendance mark, OBE entry, timetables | Own courses only (data-level) |
| **Employee** | HR self-service | — |
| **Student** | Self portal, registration, transcript, attendance check-in, OBE attainment, own finance | Self only |
| **Finance / Finance Controller / CFO** | Fee structures, vouchers, reports, gateway management | Finance domain |
| **HR / HRManager** | HR records | HR domain |
| **OBECoordinator** | OBE / CIS processing | OBE domain |
| **Auditor** | Compliance, finance audit via `/api/auditor` opaque token | Read+review |
| **Chancellor** | Board dashboard (`/api/board`) | Board data |

### Role inheritance (frontend `src/lib/roles.ts`)
`ROLE_PRIORITY`: SuperAdmin(100) > VC(90) > Registrar(80) > ExamController(75) > Finance(70) > HR(60) > IT(55) > HOD(50) > Dean(45) > Faculty(40) > Employee(30) > Student(20).
Aliases: Finance Controller/CFO→Finance; HRManager/HRHead→HR; ITHead/IT→IT; Employee→Faculty; default→Student.
Doc `ROLE_CAPABILITIES.md`: inheritance cascades `Student<Faculty<HOD<Dean` and `Finance Officer<Finance Manager`.

## 3. Enforcement stack (backend)

1. `UseAuthentication` (JWT) + `FallbackPolicy = RequireAuthenticatedUser`.
2. `[Authorize(Roles=...)]` — dominant, role-string based.
3. `CasbinAuthorizationMiddleware` — in-memory `IEnforcer` (`role, path, method`; `keyMatch`+`regexMatch`); SuperAdmin/admin → `/*,.*`; anonymous allow-list for health/auth/wizard/setup/verify paths.
4. `AbacMiddleware` — `X-Tenant-Id` header must equal `tenant_id` claim.
5. **Unused**: `[RequirePermission]` + `PermissionPolicyProvider` + `PermissionAuthorizationHandler` (would do a DB query per request).

## 4. Frontend role model (UI-only)

- `src/lib/roles.ts` — `getRoleProfile(roles)`, per-role `dashboardModules`, `kpis`, `quickActions`, `allowedRoutes`.
- `src/lib/use-roles.ts` — decodes JWT from `localStorage["aegiscore_access_token"]` (claims: MS role URI, `role`, `roles`); helpers `isStudentOnly`, `canManage`.
- **No server-side route guard.** `canAccessRoute` is defined but never called; AppShell only hides menu items. Frontend role gating is cosmetic; backend is the enforcement boundary. Confirmed: `src/i18n/messages.ts:53` "Tenant and role access is enforced by the API."

## 5. ABAC/RBAC risks (see SECURITY_BOUNDARIES.md for detail)

- Role strings in JWT are client-decodable; backend must always re-authorize (it does via attributes + Casbin).
- `IgnoreQueryFilters()` usages mean tenant/role scoping is app-code dependent in places.
- `[RequirePermission]` infra unused → no granular permission checks today; coarse role checks only.
- Webhook + auditor-token endpoints bypass JWT — must be validated separately (they use HMAC/token).
- `DemoIdentityMiddleware` injects a `SuperAdmin` principal in demo mode (non-prod only).
- Casbin anonymous allow-list includes `/api/session` → session endpoint relies on auth state it doesn't verify itself (defense-in-depth concern).

## 6. Grade approval roles (newest feature, `GradeApprovalDiscoveryController`)

- Queue access: `HOD, ExamController, Admin, SuperAdmin` (attribute) **and** data-scoped:
  - Admin/SuperAdmin/ExamController → all courses (`:74-75`).
  - HOD → courses in own department only (Faculty→DepartmentId) (`:77-80`).
- This is the correct pattern (role + data scope). Note `AllowedCourses` is loaded as a full `List<Guid>` (`:75, :79`) — fine at 10k scale, monitor for large course catalogs.
