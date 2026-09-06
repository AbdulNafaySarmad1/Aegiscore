import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

type PageTarget = { name: string; route: string };

const riskPages: PageTarget[] = [
  { name: "login", route: "/login" },
  { name: "dashboard", route: "/dashboard" },
  { name: "students", route: "/students" },
  { name: "grades", route: "/grades" },
  { name: "attendance", route: "/attendance" },
  { name: "finance", route: "/finance" },
  { name: "timetable", route: "/timetable" },
  { name: "setup", route: "/setup" },
  { name: "admin", route: "/admin" },
  { name: "profile", route: "/profile" }
];

const allPages: PageTarget[] = [
  { name: "home", route: "/" },
  { name: "achievements", route: "/achievements" },
  { name: "admin-campus", route: "/admin/campus" },
  { name: "attendance", route: "/attendance" },
  { name: "audit", route: "/audit" },
  { name: "auditor", route: "/auditor" },
  { name: "board", route: "/board" },
  { name: "cafeteria", route: "/cafeteria" },
  { name: "catalog", route: "/catalog" },
  { name: "classes", route: "/classes" },
  { name: "completion", route: "/completion" },
  { name: "convocations", route: "/convocations" },
  { name: "data-import", route: "/data-import" },
  { name: "defaulter-prediction", route: "/defaulter-prediction" },
  { name: "document-vault", route: "/document-vault" },
  { name: "elections", route: "/elections" },
  { name: "examinations", route: "/examinations" },
  { name: "finance-department", route: "/finance/department/11111111-1111-1111-1111-111111111111" },
  { name: "finance-gateways", route: "/finance/gateways" },
  { name: "finance-operations", route: "/finance/operations" },
  { name: "finance", route: "/finance" },
  { name: "foreign-students", route: "/foreign-students" },
  { name: "freezes", route: "/freezes" },
  { name: "grade-appeals", route: "/grade-appeals" },
  { name: "grade-distributions", route: "/grade-distributions" },
  { name: "imports", route: "/imports" },
  { name: "login", route: "/login" },
  { name: "mental-health", route: "/mental-health" },
  { name: "onboarding", route: "/onboarding" },
  { name: "placement", route: "/placement" },
  { name: "plagiarism", route: "/plagiarism" },
  { name: "prerequisite-conflicts", route: "/prerequisite-conflicts" },
  { name: "proctoring", route: "/proctoring" },
  { name: "readiness", route: "/readiness" },
  { name: "replication", route: "/replication" },
  { name: "setup", route: "/setup" },
  { name: "student-attendance", route: "/student/attendance" },
  { name: "student-dashboard", route: "/student/dashboard" },
  { name: "student-finance", route: "/student/finance/11111111-1111-1111-1111-111111111111" },
  { name: "student-profile", route: "/student/profile" },
  { name: "student-transcript", route: "/student/transcript" },
  { name: "sync-lag", route: "/sync-lag" },
  { name: "transport", route: "/transport" },
  { name: "visiting-faculty", route: "/visiting-faculty" },
  { name: "voice", route: "/voice" }
];

const reportDirectory = path.join(process.cwd(), "accessibility-reports");
const targets = process.env.AXE_SCOPE === "high-risk" ? riskPages : allPages;
const demoSession = JSON.stringify({
  email: "accessibility-test@example.invalid",
  label: "Vice Chancellor",
  desc: "Accessibility Test Administrator"
});

async function prepare(page: Page, route: string) {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.addInitScript(value => {
    localStorage.setItem("aegiscore_demo_session", value);
    localStorage.setItem("aegis-font-scale", "medium");
  }, demoSession);
  const response = await page.goto(route, { waitUntil: "domcontentloaded" });
  expect(response?.status(), `${route} must resolve to a Portal page`).toBeLessThan(400);
  const setupRoutes = new Set(["/login", "/setup", "/completion", "/imports", "/onboarding"]);
  if (setupRoutes.has(route) || route.startsWith("/student/finance/")) {
    await page.locator("#main-content").waitFor({ state: "visible" });
  } else {
    await page.locator(".shell-main").waitFor({ state: "visible" });
  }
  await page.waitForTimeout(500);
}

test.describe("WCAG 2.1 AA", () => {
  test.setTimeout(60_000);
  test.beforeAll(async () => {
    await mkdir(reportDirectory, { recursive: true });
  });

  for (const target of targets) {
    test(`${target.route} has no automated WCAG 2.1 AA violations`, async ({ page }) => {
      await prepare(page, target.route);
      const results = await new AxeBuilder({ page })
        .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
        .analyze();
      const report = {
        page: target.route,
        testedAt: new Date().toISOString(),
        violations: results.violations.map(violation => ({
          rule: violation.id,
          impact: violation.impact,
          help: violation.help,
          helpUrl: violation.helpUrl,
          nodes: violation.nodes.map(node => ({
            element: node.target.join(" "),
            html: node.html,
            fix: node.failureSummary
          }))
        }))
      };
      await writeFile(
        path.join(reportDirectory, `${target.name}.json`),
        JSON.stringify(report, null, 2),
        "utf8"
      );

      expect(
        report.violations,
        `${target.route} violations:\n${report.violations
          .map(violation => `${violation.impact}: ${violation.rule} (${violation.nodes.length})`)
          .join("\n")}`
      ).toEqual([]);
    });
  }
});
