# P17 WCAG 2.1 AA Accessibility Audit

Audit date: 2026-09-06
Target: WCAG 2.1 Level AA
Automation: `@axe-core/playwright` 4.13.0 with `wcag2a`, `wcag2aa`, `wcag21a`, and `wcag21aa` tags.

The prompt inventory said 43 pages. The current source tree contains 45 `page.tsx` roots, so all 45 were included. Redirect-only roots were tested at their destination. Dynamic finance routes used non-production UUID fixtures. JSON evidence is generated in `AegisCore.Portal/accessibility-reports/` and uploaded by CI.

## Findings before remediation

| Page | Rule | Element | Impact | Fix |
|---|---|---|---|---|
| `/grade-appeals` | `color-contrast` | Four sidebar domain labels | Serious | Added dark-surface domain label colours meeting 4.5:1. |
| `/plagiarism` | `color-contrast` | Four sidebar domain labels | Serious | Applied the same shared AppShell token correction. |
| `/login` | `color-contrast` | Login footer | Serious | Replaced the subtle token with the higher-contrast muted token. |
| `/finance` | `color-contrast` | Error alert | Serious | Darkened the shared danger token from `#DC2626` to `#B91C1C`. |
| `/` | `color-contrast` | Hero time | Serious | Raised the shared light-theme subtle token to neutral 600. |
| `/achievements` | `select-name` | Leaderboard period selector | Critical | Added an accessible name. |
| `/board` | `scrollable-region-focusable` | Alerts and benchmark regions | Serious | Added keyboard focus and named region landmarks. |
| `/catalog` | `label` | Catalog import file input | Critical | Added an accessible name. |
| `/convocations` | `select-name` | Status filter | Critical | Added an accessible name. |
| `/data-import` | `color-contrast` | Muted import guidance | Serious | Raised the shared muted token contrast. |
| `/defaulter-prediction` | `color-contrast` | Six warning values | Serious | Replaced the low-contrast orange with the warning token. |
| `/elections` | `select-name` | Election tally selector | Critical | Added an accessible name. |
| `/finance/gateways` | `color-contrast` | Error detail | Serious | Raised shared muted text contrast on danger surfaces. |
| `/freezes` | `color-contrast` | Error detail | Serious | Applied the shared muted-token correction. |
| `/grade-appeals` | `color-contrast` | Error detail | Serious | Applied the shared muted-token correction. |
| `/mental-health` | `color-contrast` | Error detail | Serious | Applied the shared muted-token correction. |
| `/prerequisite-conflicts` | `color-contrast` | Error detail | Serious | Applied the shared muted-token correction. |
| `/readiness` | `color-contrast` | Error detail | Serious | Applied the shared muted-token correction. |
| `/voice` | `color-contrast` | Error detail | Serious | Applied the shared muted-token correction. |
| `/student/transcript` | `color-contrast` | Nine table headings | Serious | Raised muted heading text contrast and added column scope. |

Impact totals before fixes: Critical 4 page-rule findings (4 affected nodes), Serious 16 page-rule findings (35 affected nodes), Moderate 0, Minor 0.

## Complete page inventory after remediation

Every row below produced zero automated WCAG 2.1 AA violations in the final full scan.

| Page | Rule | Element | Impact | Fix |
|---|---|---|---|---|
| `/` | None detected | — | — | No automated fix required |
| `/achievements` | None detected | — | — | No automated fix required |
| `/admin/campus` | None detected | — | — | No automated fix required |
| `/attendance` | None detected | — | — | Labels, state names, live regions, and dialog keyboard behavior audited |
| `/audit` | None detected | — | — | No automated fix required |
| `/auditor` | None detected | — | — | No automated fix required |
| `/board` | None detected | — | — | No automated fix required |
| `/cafeteria` | None detected | — | — | No automated fix required |
| `/catalog` | None detected | — | — | No automated fix required |
| `/classes` | None detected | — | — | No automated fix required |
| `/completion` | None detected | — | — | Redirect destination `/setup` audited |
| `/convocations` | None detected | — | — | No automated fix required |
| `/data-import` | None detected | — | — | Tabs, labels, table scope, status, and keyboard path corrected |
| `/defaulter-prediction` | None detected | — | — | No automated fix required |
| `/document-vault` | None detected | — | — | No automated fix required |
| `/elections` | None detected | — | — | No automated fix required |
| `/examinations` | None detected | — | — | Course/mark labels, table scope, and status semantics corrected |
| `/finance/department/:departmentId` | None detected | — | — | UUID fixture used |
| `/finance/gateways` | None detected | — | — | Dialog focus trap and restoration added |
| `/finance/operations` | None detected | — | — | No automated fix required |
| `/finance` | None detected | — | — | Error contrast corrected |
| `/foreign-students` | None detected | — | — | No automated fix required |
| `/freezes` | None detected | — | — | No automated fix required |
| `/grade-appeals` | None detected | — | — | Shared sidebar contrast corrected |
| `/grade-distributions` | None detected | — | — | No automated fix required |
| `/imports` | None detected | — | — | Redirect destination `/setup` audited |
| `/login` | None detected | — | — | Footer contrast and form semantics corrected |
| `/mental-health` | None detected | — | — | No automated fix required |
| `/onboarding` | None detected | — | — | Redirect destination `/setup` audited |
| `/placement` | None detected | — | — | No automated fix required |
| `/plagiarism` | None detected | — | — | Shared sidebar contrast corrected |
| `/prerequisite-conflicts` | None detected | — | — | No automated fix required |
| `/proctoring` | None detected | — | — | No automated fix required |
| `/readiness` | None detected | — | — | No automated fix required |
| `/replication` | None detected | — | — | No automated fix required |
| `/setup` | None detected | — | — | Valid skip target added; all seven steps keyboard-tested |
| `/student/attendance` | None detected | — | — | No automated fix required |
| `/student/dashboard` | None detected | — | — | Skip navigation and search shortcut keyboard-tested |
| `/student/finance/:studentId` | None detected | — | — | UUID fixture used |
| `/student/profile` | None detected | — | — | No automated fix required |
| `/student/transcript` | None detected | — | — | No automated fix required |
| `/sync-lag` | None detected | — | — | No automated fix required |
| `/transport` | None detected | — | — | No automated fix required |
| `/visiting-faculty` | None detected | — | — | No automated fix required |
| `/voice` | None detected | — | — | No automated fix required |

Post-fix impact totals: Critical 0, Serious 0, Moderate 0, Minor 0.

## Top twenty violations: before and after

| # | Page and rule | Before | After |
|---:|---|---|---|
| 1 | `/grade-appeals` contrast | Sidebar labels used generic low-contrast domain colours | `data-domain` selectors supply dark-surface AA colours |
| 2 | `/plagiarism` contrast | Same shared sidebar colours failed on navy | Same shared token correction |
| 3 | `/login` contrast | Footer used `var(--subtle)` | Footer uses `var(--muted)` |
| 4 | `/finance` contrast | `#DC2626` error text on pale red | `--danger: #B91C1C` |
| 5 | `/` contrast | Hero time used neutral 400 | Light `--subtle` uses neutral 600 |
| 6 | `/achievements` select name | Unnamed leaderboard `select` | `aria-label="Leaderboard period"` |
| 7 | `/board` scroll region | Overflow containers were not keyboard reachable | Named `role="region"` containers use `tabIndex={0}` |
| 8 | `/catalog` input label | File input depended on surrounding copy | `aria-label="Catalog file"` |
| 9 | `/convocations` select name | Unnamed status filter | `aria-label="Filter by status"` |
| 10 | `/data-import` contrast | Small muted copy fell below 4.5:1 | Light `--muted` uses neutral 600 |
| 11 | `/defaulter-prediction` contrast | Six values used `#d98a20` | Values use `var(--warning)` |
| 12 | `/elections` select name | Unnamed election selector | `aria-label="Select election tally"` |
| 13 | `/finance/gateways` error contrast | Error detail used low-contrast muted text | Shared muted token exceeds 4.5:1 |
| 14 | `/freezes` error contrast | Same shared `ErrorDisplay` issue | Shared token correction |
| 15 | `/grade-appeals` error contrast | Same shared `ErrorDisplay` issue | Shared token correction |
| 16 | `/mental-health` error contrast | Same shared `ErrorDisplay` issue | Shared token correction |
| 17 | `/prerequisite-conflicts` error contrast | Same shared `ErrorDisplay` issue | Shared token correction |
| 18 | `/readiness` error contrast | Same shared `ErrorDisplay` issue | Shared token correction |
| 19 | `/voice` error contrast | Same shared `ErrorDisplay` issue | Shared token correction |
| 20 | `/student/transcript` contrast | Nine headings used muted text on a muted surface | Higher-contrast muted token plus `scope="col"` |

## Test environment note

The page scan starts the Next.js Portal exactly as CI does. The isolated scan has no API container, so network-dependent pages render their maintained loading, empty, or error state. The separate keyboard suite intercepts only the endpoints needed to render populated grade-entry and attendance controls and all seven setup steps. Integrated staging should additionally scan institution-specific populated data, uploaded templates, maps, and third-party payment UI.
