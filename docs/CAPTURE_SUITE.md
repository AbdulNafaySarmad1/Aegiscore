# UI evidence and demo recording

Run from `AegisCore.Portal` after `npm ci` and `npx playwright install chromium`.
The existing Playwright config is reused. Capture commands never start a mock server.

Provide `APP_URL`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `FACULTY_EMAIL`,
`FACULTY_PASSWORD`, `STUDENT_EMAIL`, `STUDENT_PASSWORD` through the process environment.
Use a disposable installation with real role accounts, accessible finance data,
an assigned course, enrolled student, assessment scheme, and no attendance entry today.
Do not use a production university database for the recording: it activates setup,
submits marks, and records attendance. Tokens and credentials are never committed.

```sh
npm run capture:screenshots
npm run capture:demo
```

Outputs are repository-root `screenshots/{role}/{page}.png`, `screenshots/INDEX.md`,
and `recordings/aegiscore-demo.webm`. Failed recordings use `_FAILED.webm`.
The screenshots include all discovered page roots plus the requested URLs and seven
sequential setup steps. Failed pages, denied access, redirects outside known aliases,
and failed API requests receive `_ERROR.png`. INDEX records HTTP evidence, including
N/A when no response exists. Role login happens once and storageState is reused.
`.auth`, screenshots, traces, and videos are private local artifacts; review for personal data before sharing.

`/grades` and `/timetable` are not page roots. `next.config.ts` redirects them to
`/examinations` and `/classes`; the capture navigates the requested path, asserts it
lands on the alias target, saves the file under the requested name (`grades.png`,
`timetable.png`), and records the mapping in the `Alias` column of `INDEX.md`.

`CAPTURE_MODE=visual-demo` is a synthetic, fully mocked run for environments with no
real installation. It never touches the real deliverables: its output goes to
`screenshots/_synthetic/` and `recordings/_synthetic/` (video
`aegiscore-demo-synthetic.mp4`). The Task 18A/18B artifacts — `screenshots/INDEX.md`,
`screenshots/{role}/*.png`, and `recordings/aegiscore-demo.webm` — are produced only by
`CAPTURE_MODE=screenshots` and `CAPTURE_MODE=demo` against a live `APP_URL`.

For dynamic pages set `CAPTURE_STUDENTID` and `CAPTURE_DEPARTMENTID` to authorized
fixture IDs; otherwise the suite records the unavailable fixture as an error.
Setup requires `AEGISCORE_SETUP_TOKEN` and a prepared, resumable wizard session with
valid database/SSO/AI/import fields and initial-user configuration. Captures rewind
with Back and save steps 1–6 sequentially; they never launch the installation.
Demo recording completes step 7. Supply `DEMO_COURSE_ID`, `DEMO_STUDENT_NUMBER`,
`DEMO_MARKS`; optionally `DEMO_LATITUDE` and `DEMO_LONGITUDE` for the campus polygon.
Only location is mocked; no API responses or authentication are fabricated.

For a fresh or partially prepared wizard, provide `WIZARD_FIELDS_JSON` as a mapping
from step number to visible field label/value. Secret values can be referenced by
environment variable name, for example `{"2":{"Password":{"env":"DB_PASS"}}}`.
Database and AI connection tests run through the UI. The demo fills the sole initial
Admin user's email/password from the required environment variables and confirms
activation. `ADMIN_NAME` is optional. Additional initial administrators must be
removed from the disposable recording fixture or configured through a separate flow.

The current seven-step wizard is identity (including branding/templates), database,
SSO, permissions, AI, import, review. The recording follows that implemented order;
it does not invent separate branding/file-template steps or remove database setup.
The test has a 235-second deadline and checks the encoded WebM's media duration
is below 240 seconds before assigning the successful filename. It records both
media duration and elapsed time in `recordings/run.json`.
