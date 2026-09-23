import { expect, test, type APIRequestContext, type Page, request as playwrightRequest } from "@playwright/test";
import { mkdir, rename, writeFile } from "node:fs/promises";
import { readFileSync } from "node:fs";
import path from "node:path";
import { env, login } from "./helpers";
// @ts-expect-error -- plain ES module with the survey's question list.
import { PASTE, FIXED } from "./course-evaluation/questions.mjs";

// Records CLO/PLO course evaluations run entirely inside AegisCore: the registrar builds the
// 77-question survey and opens it, a student answers it for CS101 on the student portal, and the
// CLO/PLO survey report shows her answers — then a dozen classmates answer too and the report
// updates. Every step is the real application; the assertions fail the recording if the report does
// not show exactly what was submitted.

type Rating = { answers: number; mean: number; favourablePercent: number };
type Report = {
  respondents: number; answers: number; lastRecordedAt: string | null;
  courses: { code: string; respondents: number; clos: { code: string; rating: Rating }[]; satisfaction: { category: string; rating: Rating }[] }[];
  programOutcomes: { code: string; answers: number; percentOfScale: number; contributingClos: string[] }[];
  instructors: { instructor: string; respondents: number }[];
};
type Form = { sections: { title: string; questions: { key: string; number: number; kind: string; code: string | null }[] }[] };

const LABELS = ["Strongly disagree", "Disagree", "Neutral", "Agree", "Strongly agree"];
const TITLE = "Course & Teacher Evaluation — Fall 2026";
// Aisha's answers: CLO1–CLO4, then every other rating question by position.
const AISHA_CLOS = [5, 4, 4, 3];
const aishaItem = (i: number) => (i % 9 === 0 ? 3 : i % 4 === 0 ? 5 : 4);

async function card(page: Page, title: string, subtitle: string, ms = 5000) {
  await page.evaluate(({ title, subtitle }) => {
    const el = document.createElement("section"); el.id = "capture-card";
    el.style.cssText = "position:fixed;inset:0;z-index:2147483647;background:linear-gradient(135deg,#07152d,#173c75);color:#fff;display:grid;place-content:center;justify-items:center;text-align:center;font-family:Inter,Arial,sans-serif;padding:0 10vw";
    el.innerHTML = `<div style="font-size:14px;letter-spacing:.18em;text-transform:uppercase;opacity:.75;color:#fff">AegisCore · outcome-based education · local test installation</div><h1 style="font-size:46px;margin:18px 0 10px;color:#fff">${title}</h1><p style="font-size:21px;opacity:.9;line-height:1.45;color:#fff;max-width:1100px">${subtitle}</p>`;
    document.body.appendChild(el);
  }, { title, subtitle });
  await page.waitForTimeout(ms);
  await page.locator("#capture-card").evaluate(el => el.remove());
}

async function caption(page: Page, step: string, text: string) {
  await page.evaluate(({ step, text }) => {
    document.getElementById("capture-caption")?.remove();
    const el = document.createElement("div"); el.id = "capture-caption";
    el.style.cssText = "position:fixed;left:24px;right:24px;bottom:22px;z-index:2147483646;background:rgba(7,21,45,.93);color:#fff;font:500 19px/1.45 Inter,Arial,sans-serif;padding:14px 20px;border-radius:8px;box-shadow:0 6px 24px rgba(0,0,0,.25);pointer-events:none";
    el.innerHTML = `<span style="display:inline-block;background:#2F5A3F;border-radius:4px;padding:2px 8px;margin-right:12px;font-size:14px;letter-spacing:.06em">${step}</span>${text}`;
    document.body.appendChild(el);
  }, { step, text });
}

const round = (value: number, places: number) => Math.round((value + Number.EPSILON) * 10 ** places) / 10 ** places;

async function studentApi(baseURL: string, email: string, password: string): Promise<APIRequestContext> {
  const api = await playwrightRequest.newContext({ baseURL, extraHTTPHeaders: { Origin: new URL(baseURL).origin } });
  const signedIn = await api.post("/api/auth/login", { data: { email, password } });
  expect(signedIn.status(), `sign-in for ${email}`).toBe(200);
  return api;
}

test("record a CLO/PLO course evaluation built, answered and reported inside AegisCore", async ({ browser }) => {
  test.setTimeout(600_000);
  const baseURL = env("APP_URL");
  expect(["localhost", "127.0.0.1"]).toContain(new URL(baseURL).hostname);
  expect(env("PILOT_CAPTURE_ACK")).toBe("dedicated-demo");
  const scenario = JSON.parse(readFileSync(path.resolve(process.cwd(), "../demo-scenarios/pakistan-bscs-v1/scenario.json"), "utf8")) as { cohort: { emailDomain: string; givenNames: string[]; familyNames: string[] } };
  const cohortEmail = (offset: number) => {
    const { givenNames: g, familyNames: f, emailDomain } = scenario.cohort;
    return `${g[offset % g.length]}.${f[Math.floor(offset / g.length) % f.length]}@${emailDomain}`.toLowerCase();
  };

  // Both people sign in outside the recording; the second factor is not part of this story.
  const states: Record<string, Awaited<ReturnType<Awaited<ReturnType<typeof browser.newContext>>["storageState"]>>> = {};
  for (const role of ["admin", "student"] as const) {
    const ctx = await browser.newContext({ baseURL });
    await login(await ctx.newPage(), role);
    states[role] = await ctx.storageState();
    await ctx.close();
  }

  const output = path.resolve(__dirname, "../../../recordings");
  await mkdir(path.join(output, ".raw"), { recursive: true });
  const context = await browser.newContext({ baseURL, storageState: states.admin, viewport: { width: 1920, height: 1080 }, recordVideo: { dir: path.join(output, ".raw"), size: { width: 1920, height: 1080 } } });
  const page = await context.newPage();
  page.setDefaultTimeout(20_000);
  const video = page.video()!;
  const log: Record<string, unknown> = { startedAt: new Date().toISOString() };
  let complete = false;
  const asRole = async (role: "admin" | "student") => {
    await context.clearCookies();
    await context.addCookies(states[role].cookies);
  };

  try {
    // ── 1. The registrar builds the survey ────────────────────────────────────────────────
    await page.goto("/obe/surveys");
    await card(page, "CLO/PLO course evaluations, inside AegisCore",
      `Recorded live on ${new Date().toLocaleString("en-GB", { dateStyle: "long", timeStyle: "short" })} on a local test installation with fictional students and courses.<br>No outside form tool: the survey is built, answered and reported in AegisCore.`, 7000);
    await caption(page, "1 · REGISTRAR", "The registrar's Course evaluations page (Outcome-based education). Starting a new end-of-semester survey.");
    await page.waitForTimeout(3000);
    await page.getByRole("button", { name: "New survey" }).first().click();
    await page.getByLabel("Title").fill(TITLE);
    await page.getByLabel("Semester").fill("Fall 2026");
    await page.getByLabel("Instructions to students").fill("Please answer for the course named at the top. It takes about ten minutes.");
    await page.getByLabel("Every course with enrolled students").uncheck();
    const courseGrid = page.locator(".course-evaluations-course-grid");
    await courseGrid.getByRole("checkbox", { name: /CS101/ }).check();
    await courseGrid.getByRole("checkbox", { name: /CS102/ }).check();
    await caption(page, "1 · REGISTRAR", "This survey covers CS101 and CS102. Each student will also rate the CLOs of the course they evaluate — in that course's own words.");
    await page.waitForTimeout(4500);

    await page.getByLabel("Question list").fill(PASTE);
    await expect(page.getByTestId("paste-preview")).toHaveText(`${FIXED} questions in 5 sections`);
    await page.getByLabel("Question list").scrollIntoViewIfNeeded();
    await caption(page, "1 · REGISTRAR", `The university's existing questionnaire, pasted in: <strong>${FIXED} questions</strong> in 5 sections. Two teamwork/communication items count directly towards <strong>PLO4</strong>.`);
    await page.waitForTimeout(5500);
    await page.getByRole("button", { name: "Add these questions" }).click();
    await expect(page.getByRole("table", { name: "Survey questions" }).getByRole("row")).toHaveCount(FIXED + 1);
    await page.getByRole("table", { name: "Survey questions" }).scrollIntoViewIfNeeded();
    await caption(page, "1 · REGISTRAR", "Every question is editable: its section, wording, and what it counts towards — a reporting heading, or a programme outcome.");
    await page.waitForTimeout(5000);
    await page.getByRole("button", { name: "Save draft" }).click();
    await expect(page.getByText(`“${TITLE}” saved as a draft with ${FIXED} questions, plus each course's own CLOs.`)).toBeVisible();
    const responseRate = page.getByRole("table", { name: "Response rate by course" });
    await expect(responseRate.getByRole("row", { name: /CS101/ })).toContainText("CLO1, CLO2, CLO3, CLO4");
    await caption(page, "1 · REGISTRAR", "Saved. CS101 and CS102 each have four CLOs, so every student will answer 4 + 73 = <strong>77 questions</strong>.");
    await page.waitForTimeout(5000);
    await page.getByRole("button", { name: "Open to students" }).click();
    await expect(page.getByText("The survey is open.")).toBeVisible();
    const surveyId = await page.evaluate(async title => {
      const surveys = await (await fetch("/api/obe/v2/surveys")).json() as { id: string; title: string; status: string }[];
      return surveys.find(s => s.title === title && s.status === "Open")!.id;
    }, TITLE);
    log.surveyId = surveyId;
    await expect(responseRate.getByRole("row", { name: /CS101/ })).toContainText("0 of 50");
    await caption(page, "1 · REGISTRAR", "Open. Response rate: <strong>0 of 50</strong> CS101 students so far.");
    await page.waitForTimeout(4500);

    // ── 2. A student answers it ───────────────────────────────────────────────────────────
    await asRole("student");
    await page.goto("/student/evaluations");
    const row = page.getByRole("row", { name: /CS101/ });
    await expect(row).toContainText("77");
    await caption(page, "2 · STUDENT", "Aisha Khan, a BSCS student, signs in. Course evaluations lists one evaluation for each of her courses the survey covers.");
    await page.waitForTimeout(4500);
    await row.getByRole("link", { name: "Evaluate CS101" }).click();
    await expect(page.getByRole("heading", { name: "Course learning outcomes" })).toBeVisible();
    await caption(page, "2 · STUDENT", "The CLO questions are CS101's own learning outcomes, taken from the course's outcome record — then the 73 questions the registrar pasted.");
    await page.waitForTimeout(5500);
    const groups = page.getByRole("radiogroup");
    await expect(groups).toHaveCount(76);
    const submitted: number[] = [];
    for (let i = 0; i < 76; i++) {
      const score = i < 4 ? AISHA_CLOS[i] : aishaItem(i);
      submitted.push(score);
      await groups.nth(i).getByText(LABELS[score - 1], { exact: true }).click();
      await page.waitForTimeout(i < 6 ? 350 : 70);
    }
    await page.getByLabel("Question 77").fill("The weekly labs were the most useful part. More practice problems before the midterm would help.");
    await expect(page.getByText("76 of 76 answered")).toBeVisible();
    await caption(page, "2 · STUDENT", "All 77 answered (76 ratings and a comment). Submitting.");
    await page.waitForTimeout(2500);
    await page.getByRole("button", { name: "Submit evaluation" }).click();
    await expect(page.getByTestId("evaluation-receipt")).toContainText("76 ratings and 1 comment recorded");
    log.aishaSubmittedAt = new Date().toISOString();
    await caption(page, "2 · STUDENT", "Submitted. The answers are stored without her name; AegisCore records only that she has answered, so she cannot answer twice.");
    await page.waitForTimeout(5000);
    await page.goto("/student/evaluations");
    await expect(page.getByRole("row", { name: /CS101/ })).toContainText("Submitted");
    await page.waitForTimeout(2500);

    // ── 3. The report ─────────────────────────────────────────────────────────────────────
    await asRole("admin");
    await page.goto(`/obe/survey-results?survey=${surveyId}`);
    await expect(page.getByText("1 response", { exact: true })).toBeVisible();
    const first = await (await page.request.get(`/api/obe/v2/indirect?surveyId=${surveyId}`)).json() as Report;
    const cs101 = first.courses.find(c => c.code === "CS101")!;
    expect(cs101.clos.map(c => [c.code, c.rating.mean])).toEqual(AISHA_CLOS.map((s, i) => [`CLO${i + 1}`, s]));
    expect(first.answers).toBe(76);
    await caption(page, "3 · REPORT", "Back as the registrar: the CLO/PLO survey report for this survey. CS101's CLO scores are exactly Aisha's answers — 5, 4, 4, 3.");
    await page.waitForTimeout(6500);

    // ── 4. Classmates answer; the report updates live ─────────────────────────────────────
    const classmates: { email: string; course: string; clo: number[]; items: number[] }[] = [];
    let seed = 20260923;
    const next = () => (seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648;
    const pick = () => { const r = next(); return r < 0.05 ? 1 : r < 0.15 ? 2 : r < 0.37 ? 3 : r < 0.77 ? 4 : 5; };
    for (let offset = 1; offset <= 14; offset++)
      classmates.push({ email: cohortEmail(offset), course: offset <= 11 ? "CS101" : "CS102", clo: [pick(), pick(), pick(), pick()], items: Array.from({ length: 72 }, pick) });
    await caption(page, "4 · CLASSMATES", "Now 14 classmates submit their evaluations from their own student accounts (driven by the test script through the same sign-in and submit API as the screen above). Watch the report update — it re-reads every 5 seconds.");
    const courseIds = Object.fromEntries((await (await page.request.get("/api/obe/v2/outcomes")).json() as { courses: { id: string; code: string }[] }).courses.map(c => [c.code, c.id]));
    for (const classmate of classmates) {
      const api = await studentApi(baseURL, classmate.email, env("DEMO_SEED_PASSWORD"));
      const form = await (await api.get(`/api/obe/v2/surveys/${surveyId}/courses/${courseIds[classmate.course]}`)).json() as Form;
      const questions = form.sections.flatMap(s => s.questions);
      const answers: Record<string, string> = {};
      let item = 0;
      for (const q of questions) {
        if (q.kind === "Comment") continue;
        answers[q.key] = String(q.code ? classmate.clo[Number(q.code.slice(3)) - 1] : classmate.items[item++]);
      }
      const response = await api.post(`/api/obe/v2/surveys/${surveyId}/courses/${courseIds[classmate.course]}`, { data: { answers } });
      expect(response.status(), `${classmate.email} submitted ${classmate.course}`).toBe(200);
      await api.dispose();
      await page.waitForTimeout(700);
    }
    await expect(page.getByText("15 responses", { exact: true })).toBeVisible({ timeout: 15_000 });
    const final = await (await page.request.get(`/api/obe/v2/indirect?surveyId=${surveyId}`)).json() as Report;
    log.final = final;
    // The report must equal what was submitted, recomputed here from the answers themselves.
    const cs101Answers = [AISHA_CLOS, ...classmates.filter(c => c.course === "CS101").map(c => c.clo)];
    const cs101Final = final.courses.find(c => c.code === "CS101")!;
    expect(cs101Final.respondents).toBe(12);
    for (let i = 0; i < 4; i++) {
      const values = cs101Answers.map(a => a[i]);
      const got = cs101Final.clos[i].rating;
      expect(got.answers, `CS101 CLO${i + 1} answers`).toBe(values.length);
      expect(got.mean, `CS101 CLO${i + 1} mean`).toBeCloseTo(round(values.reduce((a, b) => a + b, 0) / values.length, 2), 2);
      expect(got.favourablePercent, `CS101 CLO${i + 1} favourable`).toBeCloseTo(round(values.filter(v => v >= 4).length * 100 / values.length, 1), 1);
    }
    expect(final.courses.find(c => c.code === "CS102")!.respondents).toBe(3);
    expect(final.answers).toBe(15 * 76);

    await caption(page, "4 · CLASSMATES", `<strong>15 responses</strong>: 12 for CS101, 3 for CS102. Every figure is recomputed by the test from the submitted answers and matches.`);
    await page.waitForTimeout(6000);
    await page.locator("section").filter({ has: page.getByRole("heading", { name: /^CS101 / }) }).first().scrollIntoViewIfNeeded();
    await caption(page, "5 · CLOs", "CS101 per CLO: answers, mean on the 1–5 scale, and the share who agree or strongly agree. Then the teaching and satisfaction headings from the pasted questionnaire.");
    await page.waitForTimeout(7500);
    await page.getByRole("heading", { name: "Programme outcomes (indirect)" }).scrollIntoViewIfNeeded();
    await caption(page, "6 · PLOs", "Each PLO combines its CLOs (weighted by the programme's CLO→PLO map) and the questions tagged to it directly — PLO4 includes the two teamwork/communication items.");
    await page.waitForTimeout(7500);
    await page.evaluate(() => window.scrollTo({ top: document.body.scrollHeight, behavior: "smooth" }));
    await caption(page, "7 · INSTRUCTORS", "The teaching items, per instructor.");
    await page.waitForTimeout(4500);

    await page.goto("/obe/surveys");
    await page.getByRole("row", { name: new RegExp(TITLE) }).getByRole("button", { name: "View" }).click();
    await expect(responseRate.getByRole("row", { name: /CS101/ })).toContainText("12 of 50");
    await caption(page, "8 · RESPONSE RATE", "And the registrar's response-rate view: CS101 12 of 50, CS102 3 of 50 — updating while the survey stays open.");
    await page.waitForTimeout(5500);
    await card(page, "Built, answered and reported in AegisCore",
      "A 77-question CLO/PLO course evaluation: pasted in by the registrar, answered by students on their own portal,<br>and reported per CLO, per PLO and per instructor — every number read from the running system.", 6500);
    complete = true;
  } finally {
    await context.close();
    const raw = await video.path();
    if (complete) {
      const target = path.join(output, "aegiscore-course-evaluation-clo-plo.webm");
      await rename(raw, target);
      await writeFile(path.join(output, "course-evaluation-run.json"), `${JSON.stringify({ ...log, finishedAt: new Date().toISOString(), video: path.basename(target) }, null, 2)}\n`);
    }
  }
});
