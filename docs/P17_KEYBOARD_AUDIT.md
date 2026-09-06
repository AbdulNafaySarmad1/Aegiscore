# P17 Keyboard Navigation Audit

Audit date: 2026-09-06
Browser: headless Chromium via Playwright
Command: `npm run test:keyboard`

The automated keyboard suite uses real Tab, Shift+Tab, Enter, Escape, and shortcut key events. Network fixtures are limited to the data needed to render the workflow; focus behavior remains production code.

| Flow | Keyboard path exercised | Gap found | Resolution | Final result |
|---|---|---|---|---|
| Login | First Tab, skip-link activation, tab to email and password | Password toggle inside the label made the field name ambiguous | Added an explicit password field name and stateful Show/Hide toggle name | Pass |
| Dashboard | First Tab, skip target, Ctrl+K, visible search focus | Shell controls and current navigation lacked complete semantics | Added stable main target, names, state, and global focus styling | Pass |
| Grade entry | Select course, Tab to mark, type mark, Tab to submit | Course and per-student marks had no explicit names; headers lacked scope | Added control names and scoped column headers | Pass |
| Attendance mark | Toggle student status, open override, wrap focus, Escape, restore trigger | Student action state was implicit and modal focus escaped | Added dynamic action name/pressed state and reusable focus trap | Pass |
| Wizard steps 1–7 | Focus a control on each step and activate every Save and continue button with Enter | Setup main states were not valid skip-link targets | Added stable main targets; all existing fields remain label-wrapped | Pass |
| Data import | Traverse tabs, activate with Enter, focus/name connection fields | Tabs had no selected state; labels were visually adjacent only | Added tablist/tab semantics and associated labels | Pass |

No remaining keyboard traps were observed in these required flows.

The attendance test additionally proves that:

- focus moves into the dialog when it opens;
- Shift+Tab on the first control wraps to the last enabled control;
- Tab on the last enabled control wraps to the first;
- Escape closes the dialog; and
- focus returns to the Override trigger.

Manual follow-up remains required for screen-reader announcements, browser/assistive-technology combinations, user-uploaded templates, maps, charts, and external payment surfaces.
