import { expect, test, type Page } from "@playwright/test";
import { mkdir, rename, writeFile } from "node:fs/promises";
import { readFileSync } from "node:fs";
import path from "node:path";
import { env, login } from "./helpers";

// Records the external form integration end to end, with nothing staged: a response is filled in
// and submitted on the test form (a separate program standing in for JotForm, sending JotForm's
// webhook format), AegisCore's webhook receives it, the mapping screen shows that very submission
// arriving, and the outcome report — read before and after — changes by exactly what that one
// response says. The assertions below fail the recording if any of that is not true.

type Rating = { answers: number; mean: number; favourablePercent: number };
type Report = {
  respondents: number; answers: number; lastRecordedAt: string | null;
  courses: { code: string; respondents: number; clos: { code: string; rating: Rating }[]; satisfaction: { category: string; rating: Rating }[] }[];
  programOutcomes: { code: string; answers: number; percentOfScale: number }[];
};

const FORM = process.env.FORM_URL ?? "http://127.0.0.1:8095";
const SCORE: Record<string, number> = { "Strongly Disagree": 1, Disagree: 2, Neutral: 3, Agree: 4, "Strongly Agree": 5 };
// This respondent's answers: CLO1–CLO4, then every other rating item by position.
const CLO_ANSWERS = ["Strongly Agree", "Agree", "Neutral", "Strongly Agree"];
const itemAnswer = (n: number) => (n % 7 === 0 ? "Neutral" : n % 3 === 0 ? "Strongly Agree" : "Agree");

async function card(page: Page, title: string, subtitle: string, ms = 4500) {
  await page.evaluate(({ title, subtitle }) => {
    const el = document.createElement("section"); el.id = "capture-card";
    el.style.cssText = "position:fixed;inset:0;z-index:2147483647;background:linear-gradient(135deg,#07152d,#173c75);color:#fff;display:grid;place-content:center;justify-items:center;text-align:center;font-family:Inter,Arial,sans-serif;padding:0 10vw";
    el.innerHTML = `<div style="font-size:14px;letter-spacing:.18em;text-transform:uppercase;opacity:.75">AegisCore · external form integration · local test environment</div><h1 style="font-size:46px;margin:18px 0 10px;color:#fff">${title}</h1><p style="font-size:21px;opacity:.88;line-height:1.45;color:#fff;max-width:1100px">${subtitle}</p>`;
    document.body.appendChild(el);
  }, { title, subtitle });
  await page.waitForTimeout(ms);
  await page.locator("#capture-card").evaluate(el => el.remove());
}

async function caption(page: Page, step: string, text: string) {
  await page.evaluate(({ step, text }) => {
    document.getElementById("capture-caption")?.remove();
    const el = document.createElement("div"); el.id = "capture-caption";
    el.style.cssText = "position:fixed;left:24px;right:24px;bottom:22px;z-index:2147483646;background:rgba(7,21,45,.92);color:#fff;font:500 19px/1.45 Inter,Arial,sans-serif;padding:14px 20px;border-radius:8px;box-shadow:0 6px 24px rgba(0,0,0,.25);pointer-events:none";
    el.innerHTML = `<span style="display:inline-block;background:#ff6100;border-radius:4px;padding:2px 8px;margin-right:12px;font-size:14px;letter-spacing:.06em">${step}</span>${text}`;
    document.body.appendChild(el);
  }, { step, text });
}

async function scrollThrough(page: Page, distance: number, ms: number) {
  const steps = Math.max(1, Math.round(ms / 400));
  for (let i = 0; i < steps; i++) {
    await page.mouse.wheel(0, distance / steps);
    await page.waitForTimeout(400);
  }
}

async function readReport(page: Page) {
  const response = await page.request.get("/api/obe/v2/indirect");
  expect(response.status(), "outcome report").toBe(200);
  return await response.json() as Report;
}

test("record a live JotForm-format submission reaching the CLO/PLO report", async ({ browser }) => {
  test.setTimeout(420_000);
  const baseURL = env("APP_URL");
  expect(["localhost", "127.0.0.1"]).toContain(new URL(baseURL).hostname);
  expect(env("PILOT_CAPTURE_ACK")).toBe("dedicated-demo");
  const { sourceId } = JSON.parse(readFileSync(path.resolve(process.cwd(), "../.local/pilot/form-webhook.json"), "utf8")) as { sourceId: string };

  // Sign in outside the recording (the second factor is not part of this story), then record.
  const signIn = await browser.newContext({ baseURL });
  const signInPage = await signIn.newPage();
  await login(signInPage, "admin");
  const storageState = await signIn.storageState();
  await signIn.close();

  const output = path.resolve(__dirname, "../../../recordings");
  await mkdir(path.join(output, ".raw"), { recursive: true });
  const context = await browser.newContext({ baseURL, storageState, viewport: { width: 1920, height: 1080 }, recordVideo: { dir: path.join(output, ".raw"), size: { width: 1920, height: 1080 } } });
  const page = await context.newPage();
  page.setDefaultTimeout(20_000);
  const video = page.video()!;
  const log: Record<string, unknown> = { startedAt: new Date().toISOString() };
  let complete = false;
  try {
    await page.goto("/obe/survey-results");
    await card(page, "A survey response, from an outside form tool to the CLO/PLO report",
      `Recorded live on ${new Date().toLocaleString("en-GB", { dateStyle: "long", timeStyle: "short" })} against a local test installation with fictional data.<br>The form is a <strong>local stand-in for JotForm</strong> that sends JotForm's webhook format; nothing is simulated inside AegisCore.`, 7000);

    // 1. The report before.
    const before = await readReport(page);
    log.before = { respondents: before.respondents, answers: before.answers };
    await expect(page.getByText(`${before.respondents} response${before.respondents === 1 ? "" : "s"}`, { exact: true })).toBeVisible();
    await caption(page, "1 · BEFORE", `The outcome survey report in AegisCore now: <strong>${before.respondents} responses</strong> so far. It updates itself every 5 seconds.`);
    await page.waitForTimeout(3500);
    await scrollThrough(page, 700, 3000);
    await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" }));
    await page.waitForTimeout(1500);

    // 2. The external form and its webhook setting.
    await page.goto(`${FORM}/settings/webhooks`);
    await expect(page.getByTestId("configured-webhook")).toContainText("/api/integrations/forms/");
    await caption(page, "2 · THE FORM TOOL", "A separate program on its own port — a stand-in for JotForm. Its webhook points at AegisCore's endpoint for this institution; the token in the URL is hidden.");
    await page.waitForTimeout(6000);

    // 3. A respondent fills in and submits all 77 questions.
    await page.goto(FORM);
    await expect(page.getByRole("heading", { level: 1 })).toContainText("Course & Teacher Evaluation");
    await caption(page, "3 · A STUDENT RESPONDS", "The 77-question course and teacher evaluation. Answering every question now, for CS101.");
    await page.waitForTimeout(2500);
    await page.getByLabel("Which course is this evaluation for?").selectOption("CS101 - Programming Fundamentals");
    const groups = page.getByRole("radiogroup");
    const count = await groups.count();
    expect(count, "rating questions on the form").toBe(74);
    const submitted: number[] = [];
    for (let i = 0; i < count; i++) {
      const answer = i < 4 ? CLO_ANSWERS[i] : itemAnswer(i);
      submitted.push(SCORE[answer]);
      await groups.nth(i).getByLabel(answer, { exact: true }).check();
      await page.waitForTimeout(i < 8 ? 250 : 60);
    }
    await page.getByLabel("Any other comments?").fill("Labs were the most useful part of the course.");
    await page.waitForTimeout(1200);
    await page.getByRole("button", { name: "Submit" }).click();
    await expect(page.getByTestId("webhook-status")).toHaveText("HTTP 200");
    const submissionId = (await page.getByTestId("submission-id").textContent())!.trim();
    await expect(page.getByTestId("webhook-reply")).toContainText('"status":"Recorded"');
    log.submissionId = submissionId;
    log.submittedAt = new Date().toISOString();
    await caption(page, "3 · SUBMITTED", `The form tool posted the response to AegisCore and got <strong>HTTP 200</strong> back. Submission <strong>${submissionId}</strong>.`);
    await page.waitForTimeout(5500);

    // 4. The mapping screen: the same submission has arrived, and every question has a meaning.
    await page.goto("/admin/integrations/forms");
    const row = page.getByRole("row").filter({ hasText: submissionId });
    await expect(row).toBeVisible();
    await expect(row).toContainText("Recorded");
    await expect(row).toContainText("CS101");
    await expect(row).toContainText("74 of 77");
    await caption(page, "4 · RECEIVED", `IT Lead view. Submission <strong>${submissionId}</strong> arrived seconds ago: course CS101, <strong>74 of 77</strong> answers recorded (course, semester and the comment are context, not ratings).`);
    await row.scrollIntoViewIfNeeded();
    await page.waitForTimeout(6500);
    await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" }));
    await expect(page.getByTestId("mapping-summary")).toContainText("77 questions · 0 unmapped");
    await page.getByRole("group", { name: "Filter questions" }).scrollIntoViewIfNeeded();
    await caption(page, "5 · THE MAPPING", "What each of the form's 77 questions means in AegisCore — loaded from the question list, searchable, and mappable in bulk.");
    await page.waitForTimeout(3500);
    await page.getByRole("group", { name: "Filter questions" }).getByLabel("Show").selectOption("Clo");
    await caption(page, "5 · THE MAPPING", "Filtered to the questions that rate a CLO: q3–q6 → CLO1–CLO4 of whichever course the response names.");
    await page.waitForTimeout(5000);
    await page.getByRole("group", { name: "Filter questions" }).getByLabel("Show").selectOption("all");
    await page.getByLabel("Search").fill("instructor");
    await caption(page, "5 · THE MAPPING", "Searching the 77 questions: the instructor items are teaching / satisfaction items under the “Instructor” heading.");
    await page.waitForTimeout(4500);
    await page.getByLabel("Search").fill("");

    // 5. The report after: exactly one more response, and CS101's CLO answers moved by this one.
    await page.goto("/obe/survey-results");
    await expect(page.getByText(`${before.respondents + 1} responses`, { exact: true })).toBeVisible({ timeout: 15_000 });
    const after = await readReport(page);
    log.after = after;
    expect(after.respondents).toBe(before.respondents + 1);
    expect(after.answers).toBe(before.answers + submitted.length);
    const pfBefore = before.courses.find(c => c.code === "CS101");
    const pfAfter = after.courses.find(c => c.code === "CS101")!;
    for (let i = 0; i < 4; i++) {
      const code = `CLO${i + 1}`;
      const was = pfBefore?.clos.find(c => c.code === code)?.rating ?? { answers: 0, mean: 0, favourablePercent: 0 };
      const now = pfAfter.clos.find(c => c.code === code)!.rating;
      expect(now.answers, `${code} answers`).toBe(was.answers + 1);
      const expected = (was.mean * was.answers + submitted[i]) / (was.answers + 1);
      // Both means are rounded to 2 places, so rebuilding the old total from the rounded mean is
      // exact only to within 0.005·n/(n+1) + 0.005 < 0.01. The exact check is done afterwards on the raw data.
      expect(Math.abs(now.mean - expected), `${code} mean moves by exactly this response`).toBeLessThan(0.01);
    }
    const cs102Before = before.courses.find(c => c.code === "CS102");
    const cs102After = after.courses.find(c => c.code === "CS102");
    expect(cs102After?.respondents ?? 0, "CS102 is untouched by a CS101 response").toBe(cs102Before?.respondents ?? 0);
    log.cs101 = pfAfter.clos.map(c => ({ code: c.code, answers: c.rating.answers, mean: c.rating.mean }));

    await caption(page, "6 · AFTER", `The same report: <strong>${after.respondents} responses</strong> (was ${before.respondents}). Latest response ${new Date(after.lastRecordedAt!).toLocaleTimeString()} — the one just submitted.`);
    await page.waitForTimeout(5500);
    const cs101Panel = page.locator("section").filter({ has: page.getByRole("heading", { name: /^CS101 / }) }).first();
    await cs101Panel.scrollIntoViewIfNeeded();
    await caption(page, "6 · AFTER", `CS101: each CLO now has one more answer — CLO1 ${pfAfter.clos[0].rating.answers}, CLO2 ${pfAfter.clos[1].rating.answers}, CLO3 ${pfAfter.clos[2].rating.answers}, CLO4 ${pfAfter.clos[3].rating.answers} — and the means include this response. CS102 is unchanged.`);
    await page.waitForTimeout(7000);
    await page.getByRole("heading", { name: "Programme outcomes (indirect)" }).scrollIntoViewIfNeeded();
    await caption(page, "7 · PROGRAMME OUTCOMES", "CLO answers roll up to the PLOs through the programme's CLO→PLO map, with the same strength weights as direct attainment.");
    await page.waitForTimeout(6500);
    await page.evaluate(() => window.scrollTo({ top: document.body.scrollHeight, behavior: "smooth" }));
    await caption(page, "8 · BY INSTRUCTOR", "The teaching items, per instructor.");
    await page.waitForTimeout(4500);
    await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" }));
    await card(page, "One response, received and reported",
      `Form tool → webhook (HTTP 200) → field mapping → CLO/PLO report, in the time it took to switch screens.<br>Submission ${submissionId}. Every number on screen was read from the running system.`, 6000);
    complete = true;
  } finally {
    await context.close();
    const raw = await video.path();
    if (complete) {
      const target = path.join(output, "aegiscore-form-integration-clo-plo.webm");
      await rename(raw, target);
      await writeFile(path.join(output, "form-integration-run.json"), `${JSON.stringify({ ...log, finishedAt: new Date().toISOString(), sourceId, video: path.basename(target) }, null, 2)}\n`);
    }
  }
});
