import { test, expect } from "@playwright/test";
import { env, login } from "./helpers";

// This suite is deliberately real-server-only. These are lifecycle boundary
// chapters, not a claim that the complete admission-to-alumni journey was recorded.
test.beforeEach(async ({ baseURL }) => {
  const url = new URL(baseURL!);
  expect(["localhost", "127.0.0.1"]).toContain(url.hostname);
  expect(env("PILOT_CAPTURE_ACK")).toBe("dedicated-demo");
});

test("an enrolled student cannot self-declare graduation", async ({ page }) => {
  await login(page, "student");
  const profileResponse = page.waitForResponse(r => r.url().includes("/api/student-lifecycle/alumni/me"));
  await page.goto("/alumni");
  expect((await profileResponse).status()).toBe(404);
  await expect(page.getByRole("status")).toContainText("after the university approves graduation");
  const status = await page.evaluate(async userId => {
    const response = await fetch("/api/student-lifecycle/alumni", {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId, graduationYear: 2030 }),
    });
    return response.status;
  }, env("PILOT_STUDENT_ID"));
  expect(status).toBe(403);
  await page.reload();
  await expect(page.getByRole("status")).toContainText("after the university approves graduation");
});

test("registrar cannot create alumni for an ungraduated student", async ({ page }) => {
  await login(page, "admin");
  const status = await page.evaluate(async userId => {
    const response = await fetch("/api/student-lifecycle/alumni", {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId, graduationYear: 2030 }),
    });
    return response.status;
  }, env("PILOT_STUDENT_ID"));
  expect(status).toBe(409);
});
