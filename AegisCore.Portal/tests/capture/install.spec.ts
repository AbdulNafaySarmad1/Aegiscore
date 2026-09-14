import { test, expect } from "@playwright/test";
import { readFileSync, existsSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { randomBytes } from "node:crypto";

test("complete the local installation through the real wizard", async ({ page, baseURL }) => {
  test.setTimeout(240_000);
  expect(baseURL).toBe("http://localhost:8080");
  expect(process.env.PILOT_CAPTURE_ACK).toBe("dedicated-demo");
  const stateDirectory = resolve("../.local/pilot");
  const settings = Object.fromEntries(readFileSync(resolve(stateDirectory, "runtime.env"), "utf8")
    .split(/\r?\n/).filter(line => line.includes("=")).map(line => [line.slice(0, line.indexOf("=")), line.slice(line.indexOf("=") + 1)]));
  const accountPath = resolve(stateDirectory, "capture-admin.json");
  const account = existsSync(accountPath) ? JSON.parse(readFileSync(accountPath, "utf8")) : {
    email: "administrator@pilot.example.invalid", password: randomBytes(24).toString("base64url"),
  };
  if (!existsSync(accountPath)) writeFileSync(accountPath, JSON.stringify(account));
  await page.goto("/setup");
  await page.getByLabel("Setup token", { exact: false }).fill(settings.AEGISCORE_SETUP_TOKEN);
  const session = page.waitForResponse(r => r.url().endsWith("/api/wizard/session"));
  await page.getByRole("button", { name: "Begin installation" }).click();
  expect((await session).status()).toBe(200);
  const stepText = await page.getByText(/STEP [1-7] OF 7/).textContent();
  const initial = Number(stepText?.match(/STEP (\d)/)?.[1]);
  for (let step = initial; step <= 7; step++) {
    await expect(page.getByText(`STEP ${step} OF 7`, { exact: true })).toBeVisible();
    if (step === 1) {
      for (const [label, value] of Object.entries({ "University name": "Fictional Pilot University", "Short name": "FPU", Timezone: "Asia/Karachi", "Primary contact": "Fictional Administrator", "Contact email": account.email }))
        await page.getByLabel(label, { exact: false }).fill(value);
    } else if (step === 2) {
      for (const [label, value] of Object.entries({ Host: "aegiscore-pilot-postgres", Port: "5432", "Database name": "aegiscore_pilot", Username: "aegiscore_pilot", Password: "local-fictional-pilot-only" }))
        await page.getByLabel(label, { exact: false }).fill(value);
      await page.getByLabel("Require database TLS").uncheck();
      const database = page.waitForResponse(r => r.url().endsWith("/api/wizard/step2/test"));
      await page.getByRole("button", { name: "Connect, test and migrate" }).click();
      const result = await database;
      expect(result.status()).toBe(200);
      expect((await result.json()).success).toBe(true);
    } else if (step === 3) {
      await page.getByLabel("SSO provider", { exact: false }).selectOption("local");
    } else if (step === 5) {
      await page.getByLabel("AI provider", { exact: false }).selectOption("disabled");
    } else if (step === 6) {
      await page.getByLabel("Skip data import for now").check();
    } else if (step === 7) {
      await page.getByLabel("Admin name", { exact: false }).fill("Fictional Administrator");
      await page.getByLabel("Admin email", { exact: false }).fill(account.email);
      await page.getByLabel("Admin password", { exact: false }).fill(account.password);
      await page.getByLabel("I confirm this configuration", { exact: false }).check();
    }
    const saved = page.waitForResponse(r => r.url().endsWith(`/api/wizard/step/${step}/save`) && r.request().method() === "POST");
    await page.getByRole("button", { name: step === 7 ? /Activate|Go.live|Launch/i : "Save and continue" }).click();
    const response = await saved;
    expect(response.status(), `Wizard step ${step}: ${await response.text()}`).toBe(200);
  }
  await expect(page.getByRole("heading", { name: "Your university instance is live." })).toBeVisible();
  await page.screenshot({ path: resolve("../.local/pilot/wizard-completed.png"), fullPage: true });
});
