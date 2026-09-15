import { test, expect } from "@playwright/test";
import { readFileSync, existsSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { randomBytes } from "node:crypto";

test("complete the local installation through the real wizard", async ({ page, baseURL }) => {
  test.setTimeout(240_000);
  expect(baseURL).toBe("http://localhost:8080");
  expect(process.env.PILOT_CAPTURE_ACK).toBe("dedicated-demo");
  const scenarioPath = process.env.DEMO_SCENARIO_PATH;
  if (!scenarioPath) throw new Error("DEMO_SCENARIO_PATH is required.");
  const scenario = JSON.parse(readFileSync(resolve(scenarioPath), "utf8"));
  const administrator = scenario.accounts.find((value: { kind: string }) => value.kind === "Administrator");
  if (!administrator) throw new Error("The scenario has no administrator account.");
  const stateDirectory = resolve("../.local/pilot");
  const settings = Object.fromEntries(readFileSync(resolve(stateDirectory, "runtime.env"), "utf8")
    .split(/\r?\n/).filter(line => line.includes("=")).map(line => [line.slice(0, line.indexOf("=")), line.slice(line.indexOf("=") + 1)]));
  const accountPath = resolve(stateDirectory, "capture-admin.json");
  const savedAccount = existsSync(accountPath) ? JSON.parse(readFileSync(accountPath, "utf8")) : null;
  const account = savedAccount?.email === administrator.email ? savedAccount :
    { email: administrator.email, password: `${randomBytes(24).toString("base64url")}!Aa9` };
  if (account !== savedAccount) writeFileSync(accountPath, JSON.stringify(account));
  // A fresh PostgreSQL database applies the full migration chain before the API
  // starts accepting wizard sessions. The portal can render while that happens.
  await expect.poll(async () => {
    try { return (await page.request.get("/health/live")).status(); } catch { return 0; }
  }, { timeout: 90_000 }).toBe(200);
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
      for (const [label, value] of Object.entries({ "University name": scenario.institution.name, "Short name": scenario.institution.shortName, Timezone: scenario.institution.timezone, "Primary contact": `${administrator.firstName} ${administrator.lastName}`, "Contact email": account.email }))
        await page.getByLabel(label, { exact: false }).fill(value);
    } else if (step === 2) {
      const connection = Object.fromEntries(settings.AEGISCORE_DB_CONNECTION.split(";").map((part: string) => part.split("=", 2)));
      for (const [label, value] of Object.entries({ Host: connection.Host, Port: connection.Port, "Database name": connection.Database, Username: connection.Username, Password: connection.Password }))
        await page.getByLabel(label, { exact: false }).fill(value);
      if (!scenario.integrations.databaseTls) await page.getByLabel("Require database TLS").uncheck();
      const database = page.waitForResponse(r => r.url().endsWith("/api/wizard/step2/test"));
      await page.getByRole("button", { name: "Connect, test and migrate" }).click();
      const result = await database;
      expect(result.status()).toBe(200);
      expect((await result.json()).success).toBe(true);
    } else if (step === 3) {
      await page.getByLabel("SSO provider", { exact: false }).selectOption(scenario.integrations.ssoProvider);
    } else if (step === 5) {
      await page.getByLabel("AI provider", { exact: false }).selectOption(scenario.integrations.aiProvider);
    } else if (step === 6) {
      await page.getByLabel("Skip data import for now").check();
    } else if (step === 7) {
      await page.getByLabel("Admin name", { exact: false }).fill(`${administrator.firstName} ${administrator.lastName}`);
      await page.getByLabel("Admin email", { exact: false }).fill(account.email);
      await page.getByLabel("Admin password", { exact: false }).fill(account.password);
      await page.getByLabel("I confirm this configuration", { exact: false }).check();
    }
    const saved = page.waitForResponse(r => r.url().endsWith(`/api/wizard/step/${step}/save`) && r.request().method() === "POST");
    await page.getByRole("button", { name: step === 7 ? /Activate|Go.live|Launch/i : "Save and continue" }).click();
    const response = await saved;
    const sent = response.request().postDataJSON() as { data?: Record<string, unknown> };
    expect(response.status(), `Wizard step ${step} fields [${Object.keys(sent.data ?? {}).join(", ")}]: ${await response.text()}`).toBe(200);
  }
  await expect(page.getByRole("heading", { name: "Your university instance is live." })).toBeVisible();
  await page.screenshot({ path: resolve("../.local/pilot/wizard-completed.png"), fullPage: true });
});
