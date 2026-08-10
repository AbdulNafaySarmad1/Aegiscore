# Golden Demo Runbook

## Prepare

1. Create the isolated environment file: `cp .env.demo.example .env.demo`.
2. Keep the disposable `AEGISCORE_DEMO_PASSWORD` when using the one-click persona cards; the reset generates fresh database, Redis, JWT, signing, and encryption secrets automatically.
3. Validate safeguards: `./scripts/reset-golden-demo.sh --check`.
4. Reset, migrate, seed, and start: `./scripts/reset-golden-demo.sh --confirm-reset-aegiscore-golden-demo`.
5. Open `http://localhost:8080` and use the users in `docs/GOLDEN_DEMO_USERS.md`.

## Presentation

1. **Applicant / Student:** sign in as Ali. Explain that his 2022 application is authoritatively linked to the same account now linked to his student and alumni records.
2. **Admissions Officer:** sign in as Sana. Show the published BSCS merit decision: Matric 26.40 + FSc 25.20 + entry test 32.80 = 84.40, rank 7 of 40 seats, allocated offer. State the recorded legacy eligibility/policy limitations.
3. **Registrar:** sign in as Hira. Describe the persisted user-to-student link, BSCS cohort, course enrollment, satisfied showcase degree requirement, graduated state, and alumni profile.
4. **Finance:** sign in as Khalid. Show the PKR 145,000 Spring 2026 demand, settled bank transfer, and zero outstanding balance.
5. **Teacher:** sign in as Imran. Show `CS-201`, 36-student roster, Monday timetable, 12 attendance sessions, five assessment components, marks, workload, and completed teaching period.
6. **Student telemetry / grade explanation:** return to Ali and open **Performance**. Use **Explore your records** to open DSA telemetry and **Why did I get B+ in CS-201?** The weighted score is 78.92 and the distance to A is 1.08 points.
7. **Examination Officer:** sign in as Ayesha. Show the midterm/final schedules, published result, and resolved re-totaling appeal with no grade adjustment.
8. **HOD:** sign in as Farhan. Show department-scoped DSA assignment, workload, cohort, and course telemetry context.
9. **Dean:** sign in as Nadia Rahman. Discuss authorized aggregate academic oversight without exposing peer identities.
10. **OBE / QA:** sign in as Maham. Show `CS201-CLO-2` mapped strongly to `PO-CS-2` and the seeded attainment context.
11. **HR:** sign in as Nadia Shah. Show Imran's permanent assistant-professor employee record and historical workload.
12. **Auditor:** sign in as Usman. Trace immutable analytics evidence and audit events for admission publication, student linkage, fee reconciliation, grade publication, OBE verification, and HR verification.
13. **Admin only if asked:** sign in as Bilal to discuss tenant identity/RBAC. Do not use this account as a shortcut for normal workflows.
14. **Graduation / alumni closure:** finish as Ali. His current student status is Graduated, the showcase degree audit is satisfied, and `ACGU-AL-2026-1042` closes the same person's story.

## Truthful Boundaries

- Comparison population is selected and stored by the backend snapshot. There is no client-side selector or reconstruction.
- No trained predictive model exists. The demo deliberately shows no prediction/model/SHAP snapshot state.
- Admissions policy versions and structured eligibility outcomes were not persisted by the legacy schema; the explanation says so.
- The legacy enrollment model has no section or semester field and prevents repeat attempts. The demo uses the closest legitimate cohort/course/timetable records and does not claim a richer transition model.
- The degree audit row demonstrates the showcase course requirement. It does not fabricate a complete 130-credit transcript unsupported by the current attempt model.
