import { expect, test, type Page, type Route } from "@playwright/test";

const demoSession = JSON.stringify({
  email: "accessibility-test@example.invalid",
  label: "Vice Chancellor",
  desc: "Accessibility Test Administrator"
});

async function json(route: Route, value: unknown, status = 200) {
  await route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(value)
  });
}

async function authenticate(page: Page) {
  await page.addInitScript(value => {
    localStorage.setItem("aegiscore_demo_session", value);
    localStorage.setItem("aegis-font-scale", "medium");
  }, demoSession);
}

async function expectVisibleFocus(page: Page) {
  const visible = await page.evaluate(() => {
    const element = document.activeElement;
    if (!(element instanceof HTMLElement)) return false;
    const style = getComputedStyle(element);
    return style.outlineStyle !== "none" || style.boxShadow !== "none";
  });
  expect(visible).toBe(true);
}

test("login supports skip navigation and keyboard-only form traversal", async ({ page }) => {
  await page.goto("/login");
  await page.keyboard.press("Tab");
  await expect(page.locator(".skip-link")).toBeFocused();
  await expectVisibleFocus(page);
  await page.keyboard.press("Enter");
  await expect(page.locator("#main-content")).toBeFocused();

  const email = page.getByLabel("University email");
  for (let index = 0; index < 8 && !(await email.evaluate(node => node === document.activeElement)); index += 1) {
    await page.keyboard.press("Tab");
  }
  await expect(email).toBeFocused();
  await page.keyboard.type("keyboard@example.invalid");
  await page.keyboard.press("Tab");
  await expect(page.getByLabel("Password", { exact: true })).toBeFocused();
});

test("dashboard exposes skip navigation, visible focus, and the search shortcut", async ({ page }) => {
  await authenticate(page);
  await page.route("**/api/**", route => json(route, { message: "Unavailable in isolated keyboard audit" }, 503));
  await page.goto("/dashboard");
  await page.keyboard.press("Tab");
  await expect(page.locator(".skip-link")).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.locator("#main-content")).toBeFocused();
  await page.keyboard.press("Control+k");
  await expect(page.getByRole("searchbox", { name: "Search modules" })).toBeFocused();
  await expectVisibleFocus(page);
});

test("grade entry is labelled and operable with the keyboard", async ({ page }) => {
  await authenticate(page);
  await page.route("**/api/**", async route => {
    const pathname = new URL(route.request().url()).pathname;
    if (pathname === "/api/courses/my") {
      await json(route, [{ id: "course-1", code: "CS101", title: "Systems" }]);
    } else if (pathname === "/api/examination/schemes/course-1") {
      await json(route, { id: "scheme-1", components: [{ id: "assessment-1", name: "Midterm", totalMarks: 30, weightPercentage: 30 }] });
    } else if (pathname === "/api/attendance/roster/course-1") {
      await json(route, { students: [{ enrollmentId: "enrol-1", studentId: "student-1", name: "Ada Learner", universityId: "STU-001" }] });
    } else {
      await json(route, { message: "Unavailable in isolated keyboard audit" }, 503);
    }
  });

  await page.goto("/grades");
  const course = page.getByRole("combobox", { name: "Course" });
  await course.selectOption("course-1");
  const marks = page.getByRole("spinbutton", { name: "Ada Learner Midterm marks out of 30" });
  await expect(marks).toBeVisible();
  await course.focus();
  await page.keyboard.press("Tab");
  await expect(marks).toBeFocused();
  await page.keyboard.press("Control+a");
  await page.keyboard.type("18");
  await expect(marks).toHaveValue("18");
  await page.keyboard.press("Tab");
  await expect(page.getByRole("button", { name: "Submit All Marks" })).toBeFocused();
});

test("attendance controls and override dialog preserve keyboard focus", async ({ page }) => {
  await authenticate(page);
  await page.route("**/api/**", async route => {
    const pathname = new URL(route.request().url()).pathname;
    if (pathname === "/api/courses/my") {
      await json(route, [{ id: "course-1", code: "CS101", title: "Systems" }]);
    } else if (pathname === "/api/attendance/roster/course-1") {
      await json(route, {
        students: [{
          enrollmentId: "enrol-1",
          name: "Ada Learner",
          universityId: "STU-001",
          attendanceId: "attendance-1",
          locationStatus: "OFF_CAMPUS",
          isManualOverride: false
        }]
      });
    } else {
      await json(route, { message: "Unavailable in isolated keyboard audit" }, 503);
    }
  });

  await page.goto("/attendance");
  const course = page.getByRole("combobox", { name: "Course" });
  await course.selectOption("course-1");
  const toggle = page.getByRole("button", { name: "Mark Ada Learner absent" });
  await expect(toggle).toBeVisible();
  await toggle.focus();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("button", { name: "Mark Ada Learner present" })).toBeFocused();

  const override = page.getByRole("button", { name: "Override" });
  await override.focus();
  await page.keyboard.press("Enter");
  const dialog = page.getByRole("dialog", { name: "Override off-campus check-in" });
  await expect(dialog).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Reason *" })).toBeFocused();
  await page.keyboard.press("Shift+Tab");
  await expect(page.getByRole("button", { name: "Cancel" })).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(page.getByRole("textbox", { name: "Reason *" })).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(dialog).toBeHidden();
  await expect(override).toBeFocused();
});

test("all seven setup steps can be traversed without a mouse", async ({ page }) => {
  await page.addInitScript(() => sessionStorage.setItem("aegiscore_setup_token", "keyboard-audit-token"));
  await page.route("**/api/wizard/**", async route => {
    const pathname = new URL(route.request().url()).pathname;
    if (pathname === "/api/wizard/session") {
      await json(route, {
        sessionId: "session-1",
        currentStep: 1,
        licenseMode: "demo",
        data: {
          step1: { licenseMode: "demo" },
          step2: {},
          step3: { provider: "local" },
          step4: {},
          step5: { provider: "none", connectionTested: true },
          step6: { skipImport: true },
          step7: {
            users: [{ name: "Admin", email: "admin@example.invalid", password: "safe-test-value", role: "Admin" }],
            confirmed: true
          }
        }
      });
      return;
    }
    const match = pathname.match(/^\/api\/wizard\/step\/(\d+)\/save$/);
    if (match) {
      await json(route, { nextStep: Math.min(Number(match[1]) + 1, 7) });
      return;
    }
    await json(route, {});
  });

  await page.goto("/setup");
  for (let stepNumber = 1; stepNumber <= 7; stepNumber += 1) {
    await expect(page.getByText(`STEP ${stepNumber} OF 7`)).toBeVisible();
    const controls = page.locator(".wizard-content").locator("input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), a[href]");
    expect(await controls.count(), `step ${stepNumber} must expose keyboard controls`).toBeGreaterThan(0);
    await controls.first().focus();
    await expectVisibleFocus(page);
    if (stepNumber < 7) {
      const next = page.getByRole("button", { name: /Save and continue/ });
      await next.focus();
      await page.keyboard.press("Enter");
    }
  }
});

test("data import tabs and connection fields have a keyboard path", async ({ page }) => {
  await authenticate(page);
  await page.route("**/api/**", async route => {
    const pathname = new URL(route.request().url()).pathname;
    if (pathname === "/api/data-import/connections") await json(route, []);
    else await json(route, { message: "Unavailable in isolated keyboard audit" }, 503);
  });

  await page.goto("/data-import");
  const importTab = page.getByRole("tab", { name: "Database Import" });
  const templatesTab = page.getByRole("tab", { name: "Download Templates" });
  await importTab.focus();
  await expectVisibleFocus(page);
  await page.keyboard.press("Tab");
  await expect(templatesTab).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(templatesTab).toHaveAttribute("aria-selected", "true");
  await importTab.click();
  await page.getByLabel("Connection Name").focus();
  await page.keyboard.type("Legacy database");
  await page.keyboard.press("Tab");
  await expect(page.getByLabel("Database Type")).toBeFocused();
});
