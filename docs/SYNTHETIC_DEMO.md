# Synthetic screenshots and student lifecycle MP4

Run `npm run capture:visual-demo` from `AegisCore.Portal` with dependencies and
Playwright Chromium installed. FFmpeg and ffprobe must be available on PATH.
No production credentials, database, license authority or real payments are used.
The command starts a loopback-only Next.js demo and in-memory fixture API, then
stops both when Playwright finishes. Ports 3000 and 4319 must be free.

Outputs:

- `screenshots/INDEX.md`: 51 screenshots of the ten requested pages and seven
  setup steps across admin (the Vice Chancellor demo persona), faculty and student.
- Eight additional student lifecycle screenshots under `screenshots/student/`.
- `recordings/aegiscore-demo.mp4`: H.264, 1920×1080, under four minutes.
- Original WebM, machine-checked media metadata in `recordings/run.json`, and
  disclosure in `recordings/README.md`.

The MP4 follows fictional BSCS applicant Ali Hassan: account signup, a 4.5-second
pause before a synthetic entry-test result, admission acceptance, demo fee payment,
course enrollment, academic progress, final-year project/internship, graduation
clearance and alumni mentoring. Four academic years are simulated. There are no
added video titles, overlays, cuts, audio or speed changes; MP4 conversion only
changes the encoding/container. Demo disclosure is part of the application UI.

`/demo/student-journey` returns not-found unless NEXT_PUBLIC_DEMO_MODE is true.
It is an interactive local prototype, because production applicant signup,
entry-result publication and alumni pages do not yet exist. It does not call
production mutation APIs or establish that these backend workflows are implemented.
The original real-environment screenshot/demo commands remain separate and still
require their documented credentials. Local fixture HTTP 200 is not production
role authorization evidence. Missing fixtures/errors retain `_ERROR.png` names.
Finance charts use fixture responses; the live SignalR notification connection is
not simulated and may log a negotiation warning without affecting those charts.

Only a fictional name, email and throwaway password should be entered in the demo.
All capture output and saved demo storage states are ignored by Git.
