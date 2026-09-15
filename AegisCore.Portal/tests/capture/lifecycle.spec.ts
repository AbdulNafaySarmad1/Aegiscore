import { test, expect, type Page } from "@playwright/test";
import { mkdir, rename, writeFile } from "node:fs/promises";
import path from "node:path";
import { env, login, type Role } from "./helpers";

async function chapter(page: Page, title: string, subtitle: string) {
  await page.evaluate(({ title, subtitle }) => {
    const card = document.createElement("section"); card.id = "aegiscore-chapter";
    card.style.cssText = "position:fixed;inset:0;z-index:999999;background:linear-gradient(135deg,#07152d,#173c75);color:white;display:grid;place-content:center;text-align:center;font-family:Inter,Arial,sans-serif";
    card.innerHTML = `<div style="font-size:14px;letter-spacing:.18em;text-transform:uppercase;opacity:.75">Fictional National University</div><h1 style="font-size:52px;margin:18px 0 8px">${title}</h1><p style="font-size:22px;opacity:.85">${subtitle}</p>`;
    document.body.appendChild(card);
  }, { title, subtitle });
  await page.waitForTimeout(4200);
  await page.locator("#aegiscore-chapter").evaluate(element => element.remove());
}

async function hold(page: Page, milliseconds = 5000) {
  await page.waitForTimeout(milliseconds);
}

async function reviewPage(page: Page, milliseconds = 7000) {
  await hold(page, Math.floor(milliseconds / 3));
  await page.evaluate(() => window.scrollTo({ top: Math.min(document.body.scrollHeight, 700), behavior: "smooth" }));
  await hold(page, Math.floor(milliseconds / 3));
  await page.evaluate(() => window.scrollTo({ top: 0, behavior: "smooth" }));
  await hold(page, Math.ceil(milliseconds / 3));
}

async function switchRole(page: Page, role: Role) {
  await page.context().clearCookies();
  await login(page, role);
}

test("record the real 50-student admission-to-alumni rehearsal", async ({ browser }) => {
  test.setTimeout(300_000);
  const baseURL = env("APP_URL");
  expect(["localhost", "127.0.0.1"]).toContain(new URL(baseURL).hostname);
  expect(env("PILOT_CAPTURE_ACK")).toBe("dedicated-demo");
  const output = path.resolve(__dirname, "../../../recordings"); await mkdir(output, { recursive: true });
  const context = await browser.newContext({ baseURL, viewport: { width: 1920, height: 1080 }, recordVideo: { dir: path.join(output, ".raw"), size: { width: 1920, height: 1080 } } });
  const page = await context.newPage(); page.setDefaultTimeout(15_000); const video = page.video()!;
  let complete = false; let studentId = ""; let applicantId = "";
  try {
    await login(page, "admin");
    const seed = await page.evaluate(async () => { const response = await fetch("/api/admin/demo-seed", { method: "POST", headers: { "Content-Type": "application/json" }, body: "{}" }); return { status: response.status, body: await response.text() }; });
    expect(seed.status, seed.body).toBe(200);
    await chapter(page, "Admissions · 2026", "OTP verified · documents approved · admission test passed");
    await page.goto("/admin/admissions");
    await expect(page.getByText("50 applicants")).toBeVisible();
    await reviewPage(page, 7000);
    const aisha = page.getByRole("row").filter({ hasText: "Aisha Khan" }); await expect(aisha).toContainText("Admitted");
    const href = await aisha.getByRole("link").getAttribute("href"); applicantId = href!.split("/").pop()!; await aisha.getByRole("link").click();
    await expect(page.getByRole("heading", { name: "Aisha Khan" })).toBeVisible(); await expect(page.getByText("84%", { exact: true })).toBeVisible(); await expect(page.getByText("Passed", { exact: true })).toBeVisible();
    await expect(page.getByText("Approved", { exact: true })).toHaveCount(4); await reviewPage(page, 9000);
    await page.goto("/students"); await expect(page.getByText("50 records")).toBeVisible(); const studentRow = page.getByRole("row", { name: /FNU-2026-001 Aisha Khan/ }); await expect(studentRow).toBeVisible();
    await reviewPage(page, 7000);
    const rows = await page.request.get("/api/students"); expect(rows.status()).toBe(200); const directory = await rows.json(); expect(directory.total).toBe(50); studentId = directory.items.find((row: { firstName: string; lastName: string }) => row.firstName === "Aisha" && row.lastName === "Khan").id;

    await switchRole(page, "student");
    await chapter(page, "Academic progress · 2026–2030", "Four-year time jump · persisted semester, fee, attendance and result records");
    await page.goto("/student/dashboard"); await expect(page.getByRole("heading", { name: "Welcome, Aisha Khan" })).toBeVisible();
    const overviewResponse = await page.request.get("/api/student/overview"); expect(overviewResponse.status()).toBe(200); const overview = await overviewResponse.json();
    expect(overview.student.id).toBe(studentId); expect(overview.admission.approvedDocuments).toBe(4); expect(overview.admission.testScore).toBe(84);
    expect(overview.academics.earnedCredits).toBe(130); expect(overview.academics.cgpa).toBe(3.3); expect(overview.finance.outstanding).toBe(0); expect(overview.attendance.length).toBe(3);
    await expect(page.getByText("130 / 130")).toBeVisible(); await reviewPage(page, 10000);
    await page.goto("/student/transcript"); await expect(page.getByText("3.30", { exact: true })).toBeVisible(); await expect(page.getByRole("row").filter({hasText:"Final Year Project"})).toContainText("B+");
    await reviewPage(page, 10000);
    const forbidden = await page.evaluate(async id => (await fetch("/api/student-lifecycle/alumni", { method:"POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({userId:id,graduationYear:2030}) })).status, studentId); expect(forbidden).toBe(403);
    const forbiddenClearance = await page.evaluate(async id => (await fetch(`/api/graduation/${id}/clearances/Library`, {method:"POST"})).status, studentId); expect(forbiddenClearance).toBe(403);
    const forbiddenApproval = await page.evaluate(async id => (await fetch(`/api/graduation/${id}/approve`, {method:"POST"})).status, studentId); expect(forbiddenApproval).toBe(403);
    await page.goto(`/graduation/${studentId}`); await expect(page.getByText("Library clearance is required.")).toBeVisible(); await expect(page.getByRole("button", { name: "Approve Library clearance" })).toHaveCount(0); await hold(page, 7000);

    await switchRole(page, "admin"); await chapter(page, "Graduation review · 2030", "University-owned clearances and approval");
    await page.goto(`/graduation/${studentId}`);
    const premature = await page.evaluate(async id => (await fetch(`/api/graduation/${id}/approve`, {method:"POST"})).status, studentId); expect(premature).toBe(409);
    await hold(page, 7000);
    for (const department of ["Library", "Finance", "Department"]) { const response = page.waitForResponse(r => r.url().includes(`/clearances/${department}`)); await page.getByRole("button", { name: `Approve ${department} clearance` }).click(); expect((await response).status()).toBe(200); await hold(page, 2500); }
    const approved = page.waitForResponse(r => r.url().endsWith(`/graduation/${studentId}/approve`)); await page.getByRole("button", { name: "Approve graduation" }).click(); expect((await approved).status()).toBe(200); await expect(page.getByText("Graduation approved.")).toBeVisible();
    const duplicate = await page.evaluate(async id => (await fetch(`/api/graduation/${id}/approve`, {method:"POST"})).status, studentId); expect(duplicate).toBe(200);
    await hold(page, 8000);

    await switchRole(page, "student"); await chapter(page, "Alumni · 2030", "Approved university record · student access retained");
    await page.goto("/alumni"); await expect(page.getByText("2030", { exact: true })).toBeVisible(); await expect(page.getByText(/^ALM-2030-/)).toBeVisible();
    await page.reload(); await expect(page.getByText(/^ALM-2030-/)).toBeVisible();
    const alumni = await page.request.get("/api/student-lifecycle/alumni/me"); expect(alumni.status()).toBe(200); expect((await alumni.json()).userId).toBe(studentId);
    await hold(page, 12000);
    complete = true;
  } finally {
    await context.close(); const failed = path.join(output, "aegiscore-50-student-lifecycle_FAILED.webm"); await video.saveAs(failed);
    if (complete) await rename(failed, path.join(output, "aegiscore-50-student-lifecycle.webm"));
    await writeFile(path.join(output, "lifecycle-run.json"), JSON.stringify({ complete, viewport:"1920x1080", cohort:50, focalStudent:"Aisha Khan", studentId, applicantId, backend:"PostgreSQL", syntheticFixtures:false }, null, 2));
  }
});
