import { test, expect, type Page, type Response } from "@playwright/test";
import { mkdir, writeFile, chmod, readdir } from "node:fs/promises";
import path from "node:path";
import { roles, login, openWizard, nextWizardStep, prepareWizardStep, env } from "./helpers";

const root = path.resolve(__dirname, "../../..");
const output = path.join(root, "screenshots");
type Entry = { role: string; page: string; file: string; status: string; final: string; alias: string; error: string };
const required = ["/login", "/dashboard", "/students", "/grades", "/attendance", "/finance", "/timetable", "/admissions", "/reports", "/admin/settings"];
// `/grades` and `/timetable` are not real page roots. AegisCore.Portal/next.config.ts
// redirects them to the canonical routes below; the capture navigates the requested
// path and must land on the alias target, and INDEX.md records the mapping.
const aliases: Record<string, string> = { "/grades": "/examinations", "/timetable": "/classes" };

async function routes(directory: string, prefix = ""): Promise<string[]> {
  const entries = await readdir(directory, { withFileTypes: true });
  const found = entries.some(entry => entry.name === "page.tsx") ? [prefix || "/"] : [];
  for (const entry of entries.filter(entry => entry.isDirectory() && entry.name !== "api")) {
    const segment = entry.name.replace(/\[([^\]]+)\]/g, (_, name) => process.env[`CAPTURE_${name.toUpperCase()}`] ?? `missing-${name}`);
    found.push(...await routes(path.join(directory, entry.name), `${prefix}/${segment}`));
  }
  return found;
}

test("capture every Portal page for admin, faculty, and student", async ({ browser }) => {
  test.setTimeout(30 * 60_000);
  for (const role of roles) { env(`${role.toUpperCase()}_EMAIL`); env(`${role.toUpperCase()}_PASSWORD`); }
  const entries: Entry[] = [];
  const pages = [...new Set([...required, ...await routes(path.join(root, "AegisCore.Portal/src/app"))])].filter(route => route !== "/setup");
  await mkdir(output, { recursive: true });
  await mkdir(path.join(root, "AegisCore.Portal/.auth"), { recursive: true, mode: 0o700 });
  async function index() {
    const escape = (value: string) => value.replaceAll("|", "\\|").replaceAll("\n", " ");
    await writeFile(path.join(output, "INDEX.md"), `# AegisCore screenshots\n\nCaptured ${new Date().toISOString()}. Viewport 1920×1080. Status contains document and failed API responses; unavailable responses are N/A. Alias shows requested-path → canonical-route redirects (e.g. \`/grades\` → \`/examinations\`); the file keeps the requested page name.\n\n| Role | Page | Alias | File | HTTP status | Final path | Error |\n|---|---|---|---|---|---|---|\n` + entries.map(e => `| ${e.role} | ${e.page} | ${escape(e.alias) || "—"} | [${e.file}](${e.file}) | ${escape(e.status)} | ${escape(e.final)} | ${escape(e.error)} |`).join("\n") + "\n");
  }
  async function capture(page: Page, role: string, route: string, name?: string, action?: () => Promise<void>, prerequisite = "") {
    let status = "N/A";
    let error = prerequisite;
    const apiFailures: string[] = [];
    const pageError = () => { error ||= "Unhandled page error"; };
    const listener = (response: Response) => {
      const pathname = new URL(response.url()).pathname;
      if (response.request().isNavigationRequest() || pathname.startsWith("/api/wizard/")) status = String(response.status());
      if (response.status() >= 400 && pathname.startsWith("/api/")) apiFailures.push(`${pathname}: ${response.status()}`);
    };
    page.on("response", listener);
    page.on("pageerror", pageError);
    try {
      if (action) await action();
      else {
        const response = await page.goto(route, { waitUntil: "domcontentloaded" });
        status = response ? String(response.status()) : "N/A";
        if (response && response.status() >= 400) error ||= `HTTP ${response.status()}`;
      }
      await page.waitForTimeout(1000);
      const expectedPath = aliases[route] ?? route;
      const landedPath = new URL(page.url()).pathname;
      if (route in aliases && landedPath !== expectedPath) error ||= `Alias redirect missing: expected ${route} → ${expectedPath}, landed on ${landedPath}`;
      else if (!(route in aliases) && landedPath !== route) error ||= "Unexpected redirect";
      if (await page.locator('[role="alert"], .error-display, .wizard-error').filter({ visible: true }).count()) error ||= "Visible page error";
      if (apiFailures.length) error ||= "API request failed";
    } catch { error ||= "Navigation or wizard step failed"; }
    finally { page.off("response", listener); page.off("pageerror", pageError); }
    const basename = name ?? (route === "/" ? "home" : route.slice(1).replaceAll("/", "_"));
    const file = `${role}/${basename}${error ? "_ERROR" : ""}.png`;
    await page.screenshot({ path: path.join(output, file), fullPage: true, animations: "disabled" });
    entries.push({ role, page: name ?? route, file, status: [status, ...apiFailures].join("; "), final: new URL(page.url()).pathname, alias: route in aliases ? `${route} → ${aliases[route]}` : "", error });
    await index();
    return error;
  }
  const authFailures: string[] = [];
  try {
    for (const role of roles) {
      await mkdir(path.join(output, role), { recursive: true });
      const auth = await browser.newContext({ baseURL: env("APP_URL"), viewport: { width: 1920, height: 1080 } });
      const page = await auth.newPage();
      await capture(page, role, "/login");
      let authError = "";
      try { await login(page, role); } catch { authError = "Role authentication failed"; authFailures.push(role); }
      const stateFile = path.join(root, `AegisCore.Portal/.auth/${role}.json`);
      await auth.storageState({ path: stateFile }); await chmod(stateFile, 0o600); await auth.close();
      const context = await browser.newContext({ baseURL: env("APP_URL"), storageState: stateFile, viewport: { width: 1920, height: 1080 } });
      const current = await context.newPage();
      try {
        for (const route of pages.filter(route => route !== "/login")) await capture(current, role, route, undefined, undefined, authError);
        let wizardError = "";
        for (let step = 1; step <= 7; step++) {
          wizardError = await capture(current, role, "/setup", `setup_step_${step}`, async () => {
            if (step === 1) await openWizard(current);
            else if (!wizardError) await nextWizardStep(current, step - 1);
            await expect(current.getByText(`STEP ${step} OF 7`, { exact: true })).toBeVisible();
            await prepareWizardStep(current, step);
          }, wizardError || authError);
        }
      } finally { await context.close(); }
    }
  } finally { await index(); }
  expect(authFailures, "Captures are not role-authenticated for these roles").toEqual([]);
  expect(entries).toHaveLength(roles.length * (pages.length + 7));
});
