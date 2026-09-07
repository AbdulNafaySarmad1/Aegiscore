import { test, expect } from "@playwright/test";
import { mkdir, writeFile, rename } from "node:fs/promises";
import { pathToFileURL } from "node:url";
import path from "node:path";
import { env, login, openWizard, nextWizardStep, prepareWizardStep } from "./helpers";

test("record the complete AegisCore admin demo", async ({ browser }) => {
  test.setTimeout(235_000);
  const course = env("DEMO_COURSE_ID");
  const student = env("DEMO_STUDENT_NUMBER");
  const marks = env("DEMO_MARKS");
  env("ADMIN_EMAIL"); env("ADMIN_PASSWORD"); env("AEGISCORE_SETUP_TOKEN");
  const output = path.resolve(__dirname, "../../../recordings");
  await mkdir(output, { recursive: true });
  const context = await browser.newContext({ baseURL: env("APP_URL"), viewport: { width: 1920, height: 1080 },
    recordVideo: { dir: path.join(output, ".raw"), size: { width: 1920, height: 1080 } },
    permissions: ["geolocation"], geolocation: { latitude: Number(process.env.DEMO_LATITUDE ?? "24.8615"), longitude: Number(process.env.DEMO_LONGITUDE ?? "67.0099") } });
  const page = await context.newPage();
  page.setDefaultTimeout(10_000);
  const video = page.video()!;
  const started = Date.now();
  let complete = false;
  let durationSeconds: number | null = null;
  try {
    await openWizard(page);
    for (let step = 1; step <= 7; step++) {
      await expect(page.getByText(`STEP ${step} OF 7`, { exact: true })).toBeVisible();
      await prepareWizardStep(page, step);
      if (step === 7) {
        await expect(page.getByLabel("Role *", { exact: true })).toHaveCount(1);
        await page.getByLabel("Role *", { exact: true }).selectOption("Admin");
        await page.getByLabel("Admin name", { exact: false }).fill(process.env.ADMIN_NAME ?? env("ADMIN_EMAIL"));
        await page.getByLabel("Admin email", { exact: false }).fill(env("ADMIN_EMAIL"));
        await page.getByLabel("Admin password", { exact: false }).fill(env("ADMIN_PASSWORD"));
        await page.getByLabel("I confirm this configuration", { exact: false }).check();
      }
      await page.waitForTimeout(1000);
      if (step < 7) await nextWizardStep(page, step);
    }
    const activated = page.waitForResponse(response => new URL(response.url()).pathname === "/api/wizard/complete");
    await page.getByRole("button", { name: "Review and go live" }).click();
    expect((await activated).ok()).toBe(true);
    await expect(page.getByText("Your university instance is live.")).toBeVisible();
    await page.waitForTimeout(1000);
    await login(page, "admin");
    await page.waitForTimeout(1000);
    await page.goto("/students");
    await expect(page.getByRole("cell", { name: student, exact: true })).toBeVisible();
    await page.waitForTimeout(1000);
    await page.goto("/grades");
    // `/grades` is an alias; next.config.ts redirects it to the canonical grade-entry route.
    await expect(page, "/grades must redirect to /examinations").toHaveURL(/\/examinations(?:[/?#]|$)/);
    await page.getByRole("combobox", { name: "Course", exact: true }).selectOption(course);
    const row = page.getByRole("row").filter({ hasText: student });
    await row.getByRole("spinbutton").first().fill(marks);
    const saved = page.waitForResponse(response => new URL(response.url()).pathname === "/api/examination/marks" && response.request().method() === "PUT");
    await page.getByRole("button", { name: "Submit All Marks" }).click();
    expect((await saved).ok()).toBe(true);
    await expect(page.getByText("All marks submitted successfully.")).toBeVisible();
    await page.waitForTimeout(1000);
    await page.goto("/attendance");
    await page.getByRole("combobox", { name: "Course", exact: true }).selectOption(course);
    await page.getByLabel("Student for check-in").selectOption(student);
    const checked = page.waitForResponse(response => new URL(response.url()).pathname === "/api/attendance/checkin");
    await page.getByRole("button", { name: "Mark Attendance", exact: true }).click();
    expect((await checked).ok()).toBe(true);
    await expect(page.getByRole("button", { name: "Attendance marked" })).toBeVisible();
    await page.waitForTimeout(1000);
    expect((await page.goto("/finance"))?.ok()).toBe(true);
    expect((await page.request.get("/api/finance/v2/dashboard/executive")).ok(), "Finance data must be available").toBe(true);
    await page.waitForTimeout(1000);
    await page.goto("/reports");
    await page.getByRole("button", { name: "Generate report" }).click();
    await expect(page.getByRole("status")).toHaveText("Report generated.");
    await page.waitForTimeout(1000);
    expect(Date.now() - started).toBeLessThan(230_000);
    complete = true;
  } finally {
    await context.close();
    const candidate = path.join(output, "aegiscore-demo_FAILED.webm");
    await video.saveAs(candidate);
    if (complete) {
      complete = false;
      const inspection = await browser.newContext();
      try {
        const media = await inspection.newPage();
        await media.goto(pathToFileURL(candidate).href);
        await expect.poll(() => media.locator("video").evaluate((element: HTMLVideoElement) => Number.isFinite(element.duration))).toBe(true);
        durationSeconds = await media.locator("video").evaluate((element: HTMLVideoElement) => element.duration);
        expect(durationSeconds).toBeGreaterThan(0);
        expect(durationSeconds).toBeLessThan(240);
        await rename(candidate, path.join(output, "aegiscore-demo.webm"));
        complete = true;
      } finally { await inspection.close(); }
    }
    await writeFile(path.join(output, "run.json"), JSON.stringify({ complete, durationSeconds, elapsedSeconds: (Date.now() - started) / 1000, viewport: "1920x1080", mockGeolocation: true }, null, 2));
  }
});
