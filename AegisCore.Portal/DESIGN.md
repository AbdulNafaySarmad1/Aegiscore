# AegisCore Portal — Design System ("Register")

This document is the source of truth for how the portal looks and why. It was written
before the stylesheet, and the stylesheet (`src/styles/*.css`) implements it and nothing else.

Stack: Next.js 16 (App Router), plain CSS with custom properties (no Tailwind, no CSS-in-JS),
fonts self-hosted via `next/font/local`, PDF documents rendered with `@react-pdf/renderer`.

---

## 0. What a generic tool would have produced (and what we refused)

Asked for "university ERP redesign", the default output is predictable:

| Generic instinct | Why it fails here | What we did instead |
|---|---|---|
| Inter + `#2563EB` blue, white rounded cards on `#F8FAFC` | It is every SaaS admin template; a registrar has seen forty of them | Warm ink base, stone ground, brass/oxblood accents; ruled panels, not floating cards |
| A "dark navy gradient banner" on official screens | Reads as a marketing hero, not a record | A **running head** like a printed register: title in Caslon, record identifier in mono, one brass rule |
| Tracked-out ALL-CAPS eyebrow above every heading | Decoration pretending to be structure | Section titles set in Caslon italic; identifiers set in mono where they are actually identifiers |
| Status pills in pastel green/amber/red | Framework default; no institutional voice | Status marks are **typographic** (small-caps-free, ink text with a coloured rule on the left) |
| Cards lifting on hover, fade-slide-up on every section | Motion as garnish | One motion moment: the **seal** stamps in when a clearance/approval is recorded |
| Spinner in a box for loading | Says "software is thinking"; the institution's metaphor is paper | **Ruled skeletons** that match the layout being loaded (running head + ruled rows) |
| Print = `window.print()` on the HTML page | Browser chrome, broken pagination, no letterhead | Designed PDF documents with the same type system, generated client-side |

---

## 1. Tokens

### Colour (six named values; everything else is derived with `color-mix`)

| Token | Hex | Role |
|---|---|---|
| `--ink` | `#1B1A18` | Text, sidebar ground, primary buttons. Warm charcoal — never pure black. |
| `--stone` | `#E7E7E2` | Application ground behind panels. Cool, slightly green-grey stone; not cream. |
| `--paper` | `#F8F8F5` | Panel / content surface. Off-white paper, cooler than cream. |
| `--brass` | `#7D6122` | Institutional accent: active navigation mark, focus ring, rules under running heads, "attention" status. Text-safe (5.5:1 on paper). A brighter `--brass-bright #B4913F` exists **only** for decorative rules and the seal. |
| `--oxblood` | `#6F2230` | Holds, rejections, destructive actions, overdue. Seal-wax red. 10:1 on paper. |
| `--laurel` | `#2F5A3F` | Cleared, approved, paid, passed. Bottle green. 7.5:1 on paper. |

Derived: `--ink-2` (secondary text, 6.4:1), `--rule` (hairline), `--rule-strong`,
`*-tint` backgrounds are `color-mix(in srgb, <accent> 10%, var(--paper))`.

Dark scheme swaps ground/paper for `#151412` / `#1E1D1A`, text to `#ECEAE3`, and lifts
brass to `#C9A551` so it keeps 4.5:1. Nothing else changes.

High-contrast (`data-high-contrast`) keeps the same hue roles at maximum contrast.

### Type

| Role | Family | Notes |
|---|---|---|
| Display / running heads / document titles | **Libre Caslon Text** 400 (italic for section titles, 700 only for the wordmark and document titles) | Caslon is the typeface of diplomas, statutes, and university presses. Self-hosted woff2, latin subset. |
| UI chrome, body, tables, forms | **IBM Plex Sans** 400–700 variable | A grotesk designed for engineered systems; reads technical without reading "startup". |
| Identifiers only | **IBM Plex Mono** 400/500/600 | Student numbers, application IDs, course codes, timestamps, amounts in ledgers. Never on labels. |

Scale (px, 1.25 ratio anchored at 15): 12 · 13 · 15 · 17 · 21 · 26 · 33 · 41.
Body line-height 1.55; display line-height 1.12; Caslon gets `letter-spacing: -0.005em`, never tracked out.

### Space, rules, radius

- Spacing steps: 4, 8, 12, 16, 24, 32, 48, 64.
- Radius: **2px** on inputs/buttons, **0** on panels and tables. Panels are ruled, not rounded.
- Rules: 1px `--rule` hairlines; the running head carries a 2px brass rule; approved records carry a double rule (1px + 1px, 3px apart) — the printed-register convention for a closed entry.
- Shadow: none in the light scheme. Elevation is expressed by rules and ground contrast (stone vs paper). The only shadow is on the mobile navigation drawer and dialogs.

### Motion

- One moment: the **seal stamp** (`.seal[data-stamped]`) — scale 1.35→1 with 4° rotation over 420ms, `cubic-bezier(.2,.9,.3,1)`, when a clearance or approval is recorded.
- The running head's brass rule draws in from the left on first shell mount (280ms), once per session.
- Everything else: colour/border transitions ≤150ms. No entrance animation on panels, no hover lift. `prefers-reduced-motion` disables all of it.

---

## 2. Layout

### Concepts considered

**A. "Ledger"** — ink rail on the left, content on stone, full-width paper panels stacked
with rules between them. Dense, columnar. Good for tables; weak identity for record views.

```
┌──────┬──────────────────────────────────────────────┐
│ rail │  Title                                        │
│      │  ─────────────────────────────────────────── │
│ nav  │  ┌─ panel ──────────────────────────────────┐ │
│      │  │ th   th    th    th                       │ │
│      │  │ ───────────────────────────────────────── │ │
│      │  │ td   td    td    td                       │ │
│      │  └───────────────────────────────────────────┘ │
└──────┴──────────────────────────────────────────────┘
```

**B. "Register"** — same rail, but every page opens with a *running head* modelled on a
printed university register: title in Caslon on the left, the record's identifier in mono on
the right, a brass rule beneath. Below it, a two-column *record* layout: a wide column for the
body (documents, courses, rows) and a narrow *margin column* for facts and actions
(status, identifiers, approvers) — the way marginalia sit beside a ledger entry.

```
┌──────┬──────────────────────────────────────────────────────┐
│ rail │ Aisha Khan                          APP-2026-00017   │
│      │ Admissions · Fall 2026 intake       ═══════════════  │  ← brass rule
│ nav  │ ─────────────────────────────────────────────────── │
│      │ ┌ Documents ──────────────────┐  ┌ Record ─────────┐ │
│      │ │ Matric      approved   ...  │  │ Status  Admitted│ │
│      │ │ Intermediate approved  ...  │  │ Email   ...     │ │
│      │ │ National ID  pending   ...  │  │ Submitted ...   │ │
│      │ └─────────────────────────────┘  │ ─────────────── │ │
│      │ ┌ Admission test ─────────────┐  │ Move applicant  │ │
│      │ │ 84 / 100   Passed           │  │ [Shortlist]     │ │
│      │ └─────────────────────────────┘  └─────────────────┘ │
└──────┴──────────────────────────────────────────────────────┘
```

**C. "Dossier"** — no rail; a top band carrying the institution's name and a horizontal
module strip, content as a single centred column. Calm, but it throws away the nav density
that a registrar with 40 modules needs, and centred columns fight tabular data.

### Chosen: B, "Register"

Alignment is **left** throughout the application. Registrars scan columns; left alignment
keeps identifiers, names, and statuses in vertical rhythm. Centre alignment is used in exactly
two places: the document title block on generated PDFs, and the seal. Nothing is justified.

Tables are the primary object, not cards: hairline rules between rows, no zebra striping, a
1px ink rule under the header, mono for identifiers, and the row's status expressed as a
left-hand coloured rule on the first cell rather than a pill.

Information density on admin views: the running head carries the count and filters; the
table gets the full width; the margin column appears only on record (detail) pages.

---

## 3. Principles — what makes this unmistakably AegisCore

1. **It looks like the institution's own paperwork, digitised** — Caslon running heads,
   ruled panels, mono identifiers, a seal on approvals — not like a vendor's dashboard the
   institution happens to log into.
2. **Colour means something.** Brass is the institution (attention, active, official);
   oxblood is a hold; laurel is a clearance. There is no decorative colour anywhere, so when a
   registrar sees oxblood they stop.
3. **Motion is ceremony, not garnish.** The only thing that moves is the seal, and it moves
   because a person just approved something.
4. **The printed document is the same design.** A transcript downloaded from this portal is
   set in the same Caslon and Plex, with the same rules, so the record on screen and the
   record in the file cabinet are visibly the same artefact.

---

## 4. Review pass — "would I have produced this for any dashboard brief?"

| Part | First draft | Verdict | Revised to |
|---|---|---|---|
| Colour | Charcoal + brass + a "success green" + a "danger red" | Brass/oxblood was fine; but a generic green/red pair beside them was exactly the pastel-pill default | Laurel and oxblood are *named, institutional* hues at fixed contrast, and status is expressed with rules + text, never filled pills. Warning is a brass tint — there is no fourth "amber" |
| Serif | Fraunces / Newsreader (opsz, expressive) | Both are the current fashionable editorial serifs — would have shipped on any "premium" brief | **Libre Caslon Text**: the face of diplomas and statutes; deliberately not fashionable |
| Grotesk | Inter / Geist | The two most default sans faces in AI-generated UI | **IBM Plex Sans** with Plex Mono as its native sibling for identifiers |
| Section titles | Tracked-out caps eyebrow labels | Kill-list item, and my first instinct anyway | Caslon italic section titles at 17px; identifiers in mono; no eyebrow above headings |
| Page header | Title + subtitle + button row | Interchangeable with every admin template | The **running head**: title left, record identifier right in mono, brass rule; count/filters live on the rule line |
| Cards | Panels with `border-radius: 8px` + hairline + shadow | Still "cards", just squarer | Radius 0, no shadow, ruled top edge; the *margin column* replaces "sidebar cards" on record pages |
| Loading | Skeleton with shimmer | Every app has this | Ruled skeleton: a running-head skeleton plus rows drawn as rules, no shimmer, static pulse on the brass rule only |
| Motion | Page transition fade + slide | Kill-list | Removed. Seal stamp on approval; running-head rule draw on first mount only |
| Approved state | Green banner "Graduation approved" | Toast-like | A **sealed record**: double rule, seal mark, approver attribution line — the closed-entry convention |
| Empty state | Icon + "No data" | Mood, not invitation | A ruled line with the next action as a button: "No applicants match. Clear filters" |

---

## 5. Component vocabulary (class names are the API)

- `.register-head` — running head (title, identifier, rule, meta line, actions).
- `.panel` — ruled paper surface. `.panel > .panel-title` is a Caslon italic heading.
- `.record` — two-column record layout: `.record-body` + `.record-margin`.
- `.facts` — definition list for the margin column (`dt` Plex 12px ink-2, `dd` 15px, mono via `.mono`).
- `.ledger` — the table. `.ledger tbody tr[data-status="…"]` draws the left status rule.
- `.mark` — typographic status mark (`data-tone="brass|oxblood|laurel|ink"`).
- `.btn` — `.btn-primary` (ink), `.btn-secondary` (paper, ruled), `.btn-danger` (oxblood), `.btn-quiet`.
- `.seal` — the approval seal; `data-stamped` triggers the motion.
- `.ruled-loading` — loading skeleton that mirrors the layout it replaces.
- `.notice` — inline error/warning/info line (`data-tone`), never a filled box.
- `.field` — labelled input wrapper with hint and error slots.

Legacy class names from earlier iterations (`.card`, `.kpi-card`, `.surface`, `.finance-*`,
`.wizard-*`, `.badge`, `.table-wrap`, `.audit-*`) remain defined so every existing screen is
styled under the same tokens; new screens use the vocabulary above.

---

## 6. Voice

Buttons say what they do: "Approve Library clearance", "Record rejection", "Download transcript (PDF)".
Errors say what happened and what to do next, without apologising: "The university API is not
reachable. Check the network and try again." Empty states are invitations: "No applicants match
these filters." + "Clear filters".

---

## 7. Handback — what shipped, what is rough, what was deliberately not done

### Shipped (verified at 1440px and 400px against the fixture API, plus the repo's own suites)

- **Token system** (`src/styles/tokens.css`) with every legacy variable name aliased onto it, so
  the ~90 screens not rewritten render in the same palette and type. Dark scheme, high-contrast
  and text-size preferences persist and are applied before first paint (no theme flash).
- **Fonts** self-hosted via `next/font/local` (`src/fonts/*.woff2`, latin subset, ~230 KB total);
  TTFs for the PDF renderer under `public/fonts/`, fetched only when a PDF is requested.
- **Shell** (`AppShell.tsx`): ink rail, masthead with module search (⌘K), drawer on phones with
  scrim + Escape + body-scroll lock, narrow rail on tablets. Page-transition motion removed.
  Dead controls removed (notification bell, avatar chevron with no menu).
- **Rewritten screens**: home (`/`), admissions list, applicant record, graduation review,
  alumni record, student dashboard/profile/transcript, students directory (`RecordTable`),
  paginated table, login, activation, 404, route and root error pages.
- **States**: every rewritten async boundary has a ruled loading skeleton that mirrors the
  layout (no layout shift when data lands), an error line with "Try again", and an empty state
  with the next action. Loading is `aria-busy`/`role=status`; saves are announced via a live region.
- **Dialogs**: `window.prompt` replaced with a focus-trapped `PromptDialog` (reason required on
  rejection, optional note on pipeline moves).
- **PDF**: `src/lib/pdf/transcript.tsx` — A4 transcript with letterhead, repeated table header,
  page numbers, signature rules, set in Caslon/Plex/Plex Mono. Generated client-side from the
  verified overview; rendered and checked at 11 and 40 courses (paginates correctly).
- **Tests**: `tests/accessibility.spec.ts` (axe, WCAG 2.1 AA, all 45 routes) and
  `tests/keyboard-accessibility.spec.ts` (6 flows) pass. `next build` passes. The lifecycle
  capture's locators (`"50 applicants"`, `"Approve Library clearance"`, `"Graduation approved."`,
  `/^ALM-2030-/`, `"130 / 130"`, `"3.30"`) were preserved and checked for strict-mode
  uniqueness. The fixture server gained lifecycle fixtures and a role switch (`/__fixture/role/<Role>`)
  so these flows can be exercised without the .NET API.

### Rough — production needs these

- **Institution identity is now config-driven** — `GET /api/config/portal-preferences` returns
  `universityName`, `universityShortName` and `sealUrl`; `GET /api/branding/client-logo` returns the
  crest. `useInstitution()` in `TenantPreferencesProvider` is the only place the portal reads them,
  and the rail, gate pages, `<Seal>` and the transcript PDF all consume it. Two things remain for
  the backend: nothing yet writes `identity.university_name` from the setup wizard (set it via
  `POST /api/config`), and no upload path exists for `Branding:SealUrl` (set it in configuration).
  The student self-service PDF never prints the seal; only an `official` copy does.
- **Legacy screens** (finance, examinations, wizard, apply, terminal, ~80 others) are restyled
  through `legacy.css`, not rewritten. They look like the same product but still carry their own
  inline layout and older copy; `nchart` axis labels clip at narrow widths as before.
- **Home-page figures** render only KPIs with a resolver in `src/lib/kpi-sources.ts`; a 403
  shows "Not available to your role", a network failure "Not reachable right now", never a number.
  All four Admin/VC figures are now wired: *Total enrolment* (`GET /api/students/summary`),
  *Fee collection rate* (`GET /api/finance/v2/dashboard/executive`, read-only for VC via
  `CanViewExecutiveFinance`), *Accreditation score* (`GET /api/readiness/current`) and
  *Faculty to student ratio* (`GET /api/faculty/summary` + students summary; teaching ranks only).
  Data notes for production: (1) the readiness engine is real — it scores a framework from the
  evidence recorded against its criteria (`POST /api/readiness/frameworks…`, then
  `POST /api/readiness/compute/{frameworkId}`) and refuses to score a framework with no criteria,
  so `/readiness/current` says "not assessed" until the QEC enters the framework; a stored row with
  no `CriterionScoresJson` is still shown as "unevidenced", never as a score; (2) there is one fee ledger, `FeeDemands` (the finance v2 model that installments, payments and plans hang off): the earlier `StudentFeeDues` ledger was folded into it, so the executive dashboard, the student finance line and graduation clearance all read the same rows.
- **`/readiness`** is rebuilt on the register vocabulary and reads `/api/readiness/current`
  (its previous `GET /api/readiness/compute` and `POST check-evidence-freshness` never existed).
- **Masthead search** filters the rail in place; it does not yet search records.
- **"Keep me signed in"** on the login form is not wired to the auth API (it was not before either).
- **Dark scheme** was checked visually, not by axe; brass in dark mode is `#C9A551` (4.6:1 on `#1E1D1A`).

### Deliberately not done

- No second motion moment. The seal stamps on approval and the running head's rule draws once
  per session; nothing else animates.
- No component library or CSS framework introduced. The register vocabulary is ~600 lines of
  CSS with flat specificity; the Tailwind class strings left over in `paginated-table.tsx` were
  removed, not made to work.
- No Urdu/Arabic font. The RTL locale path still works; it falls back to the system font.
- The unused `portal-shell.tsx` was left alone (nothing imports it) rather than styled or deleted.
