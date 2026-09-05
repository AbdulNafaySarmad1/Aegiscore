import { expect, test, type Page } from "@playwright/test";

const demoSession = JSON.stringify({
  email: "responsive-test@example.invalid",
  label: "Student",
  desc: "Responsive Test User",
});

async function openDashboard(page: Page) {
  await page.addInitScript(value => localStorage.setItem("aegiscore_demo_session", value), demoSession);
  await page.goto("/");
  await expect(page.locator(".shell")).toBeVisible();
}

for (const viewport of [
  { name: "iPad", width: 768, height: 1024 },
  { name: "Samsung Tab S8", width: 800, height: 1280 },
]) {
  test(`${viewport.name} uses the tablet interaction tier`, async ({ page }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await openDashboard(page);

    const layout = await page.evaluate(() => {
      const host = document.createElement("section");
      host.innerHTML = `
        <div class="form-grid"><label>Field<input value="value"></label><label>Field<input value="value"></label></div>
        <div role="dialog">Dialog</div>
        <div class="table-wrap"><table><thead><tr><th scope="col">Name</th></tr></thead><tbody><tr><td>Value</td></tr></tbody></table></div>
        <button type="button">Action</button>`;
      document.body.append(host);
      const sidebar = document.querySelector<HTMLElement>(".sidebar")!;
      const menu = document.querySelector<HTMLElement>(".mobile-menu-button")!;
      const kpi = document.querySelector<HTMLElement>(".kpi-grid")!;
      const form = host.querySelector<HTMLElement>(".form-grid")!;
      const dialog = host.querySelector<HTMLElement>("[role=dialog]")!;
      const table = host.querySelector<HTMLElement>("table")!;
      const row = host.querySelector<HTMLElement>("tbody tr")!;
      const button = host.querySelector<HTMLElement>("button")!;
      const result = {
        sidebarWidth: sidebar.getBoundingClientRect().width,
        menuDisplay: getComputedStyle(menu).display,
        kpiColumns: getComputedStyle(kpi).gridTemplateColumns.split(" ").length,
        formColumns: getComputedStyle(form).gridTemplateColumns.split(" ").length,
        dialogWidth: dialog.getBoundingClientRect().width,
        tableDisplay: getComputedStyle(table).display,
        rowDisplay: getComputedStyle(row).display,
        buttonWidth: button.getBoundingClientRect().width,
        buttonHeight: button.getBoundingClientRect().height,
      };
      host.remove();
      return result;
    });

    expect(layout.sidebarWidth).toBe(64);
    expect(layout.menuDisplay).toBe("none");
    expect(layout.kpiColumns).toBe(2);
    expect(layout.formColumns).toBe(1);
    expect(layout.dialogWidth).toBeCloseTo(viewport.width * 0.9, 0);
    expect(layout.tableDisplay).toBe("block");
    expect(layout.rowDisplay).toBe("grid");
    expect(layout.buttonWidth).toBeGreaterThanOrEqual(44);
    expect(layout.buttonHeight).toBeGreaterThanOrEqual(44);
  });
}

test("iPad Pro landscape boundary uses the desktop tier", async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 1366 });
  await openDashboard(page);

  const layout = await page.evaluate(() => {
    const sidebar = document.querySelector<HTMLElement>(".sidebar")!;
    const menu = document.querySelector<HTMLElement>(".mobile-menu-button")!;
    const kpi = document.querySelector<HTMLElement>(".kpi-grid")!;
    return {
      sidebarWidth: sidebar.getBoundingClientRect().width,
      menuDisplay: getComputedStyle(menu).display,
      kpiColumns: getComputedStyle(kpi).gridTemplateColumns.split(" ").length,
    };
  });

  expect(layout.sidebarWidth).toBe(260);
  expect(layout.menuDisplay).toBe("none");
  expect(layout.kpiColumns).toBe(4);
});
