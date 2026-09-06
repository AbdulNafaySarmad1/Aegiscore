# Accessibility

## Accessibility Statement

Standard: WCAG 2.1 Level AA

Last tested: 2026-09-06

Testing method: axe-core automated testing with Playwright plus manual keyboard-navigation workflows.

Known limitations:

- Automated tests cannot prove full WCAG conformance; assistive-technology and usability testing with disabled users is still recommended before a public production launch.
- The isolated CI job starts the Portal without the API stack. It verifies rendered loading, empty, and error states; the keyboard suite supplies controlled data for grade entry and attendance. Fully populated tenant-specific states should also be tested in an integrated staging environment.
- Maps, charts, uploaded document previews, and third-party payment surfaces require manual screen-reader validation with the actual institution configuration.
- User-authored colours and HTML document templates can introduce inaccessible content; institutions must review them before publication.

How to test locally:

```bash
cd AegisCore.Portal
npm ci
npm run dev
npx axe http://localhost:3000 --include="main"
```

Or use the axe DevTools browser extension (free tier).

The maintained repository suites are:

```bash
npm run test:a11y       # every current Portal page root
npm run test:a11y:ci    # the ten highest-risk URLs used in CI
npm run test:keyboard   # required keyboard-only workflows
```

How to test keyboard navigation:

- Tab through every interactive element.
- Ensure focus is always visible.
- Ensure modal focus remains trapped while open, Escape closes it, and focus returns to its trigger.
- Ensure the skip-to-content link appears on the first Tab and moves focus to main content.
- Confirm controls can be activated with Enter or Space and that status/error changes are announced.
