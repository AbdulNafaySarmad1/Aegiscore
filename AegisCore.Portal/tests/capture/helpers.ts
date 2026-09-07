import { expect, type Page } from "@playwright/test";

export const roles = ["admin", "faculty", "student"] as const;
export type Role = typeof roles[number];
export function env(name: string) {
  const value = process.env[name];
  if (!value) throw new Error(`Missing required environment variable: ${name}`);
  return value;
}
export async function login(page: Page, role: Role) {
  // Setup activation restarts the API; wait for its readiness before signing in.
  await expect.poll(async () => {
    try { return (await page.request.get("/health/ready")).ok(); } catch { return false; }
  }, { timeout: 30_000 }).toBe(true);
  await page.goto("/login");
  await page.getByLabel("University email").fill(env(`${role.toUpperCase()}_EMAIL`));
  await page.getByLabel("Password", { exact: true }).fill(env(`${role.toUpperCase()}_PASSWORD`));
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/);
  // Confirm a server-authenticated session; a local demo card is insufficient.
  const session = await page.request.get("/api/session");
  expect(session.status(), `${role} backend session`).toBe(200);
  const data = await session.json();
  const accepted = role === "admin" ? ["admin", "superadmin", "vc", "vice chancellor"] : [role];
  expect(data.roles.some((name: string) => accepted.includes(name.toLowerCase())), `Expected ${role} role`).toBe(true);
}

export async function openWizard(page: Page) {
  const token = env("AEGISCORE_SETUP_TOKEN");
  await page.goto("/setup");
  const tokenInput = page.getByLabel("Setup token", { exact: false });
  if (await tokenInput.isVisible()) {
    await tokenInput.fill(token);
    await page.getByRole("button", { name: "Begin installation" }).click();
  }
  await expect(page.getByText(/STEP [1-7] OF 7/)).toBeVisible();
  for (let index = 0; index < 6 && !(await page.getByText("STEP 1 OF 7", { exact: true }).isVisible()); index++)
    await page.getByRole("button", { name: "Back", exact: true }).click();
}

export async function nextWizardStep(page: Page, step: number) {
  const response = page.waitForResponse(r => new URL(r.url()).pathname === `/api/wizard/step/${step}/save` && r.request().method() === "POST");
  await page.getByRole("button", { name: "Save and continue" }).click();
  expect((await response).ok(), `Wizard step ${step} save`).toBe(true);
  await expect(page.getByText(`STEP ${step + 1} OF 7`, { exact: true })).toBeVisible();
}

// Optional per-step UI input for a fresh installation, supplied securely at runtime.
// Values can reference environment variables: {"2":{"Password":{"env":"DB_PASS"}}}.
export async function prepareWizardStep(page: Page, step: number) {
  const steps = JSON.parse(process.env.WIZARD_FIELDS_JSON ?? "{}");
  const fields = steps[String(step)] ?? {};
  for (const [label, configured] of Object.entries(fields)) {
    const value = typeof configured === "object" && configured !== null && "env" in configured
      ? env(String(configured.env)) : configured;
    const field = page.getByLabel(label, { exact: false });
    if (typeof value === "boolean") await field.setChecked(value);
    else if (await field.evaluate(element => element.tagName === "SELECT")) await field.selectOption(String(value));
    else await field.fill(String(value));
  }
  if (step === 2 || step === 5) {
    const testButton = page.getByRole("button", { name: /Test Connection/i });
    if (await testButton.isVisible()) {
      await testButton.click();
      await expect(testButton).toBeEnabled();
    }
  }
}
