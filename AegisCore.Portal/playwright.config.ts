import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  timeout: 30000,
  expect: { timeout: 10000 },
  retries: 1,
  workers: 1,
  reporter: [
    ["html", { outputFolder: "playwright-report", open: "never" }],
    ["json", { outputFile: "playwright-report/results.json" }],
    ["list"],
  ],
  webServer: {
    command: "npm run dev",
    url: "http://localhost:3000",
    reuseExistingServer: false,
    timeout: 60000,
    // E2E runs offline (no backend): demo mode enables the isolated preview
    // identity so authenticated-shell tests can exercise the portal.
    env: { NEXT_PUBLIC_DEMO_MODE: "true", NEXT_PUBLIC_DEMO_OFFLINE_PREVIEW: "true" },
  },
  use: {
    baseURL: "http://localhost:3000",
    channel: "chromium",
    headless: true,
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    trace: "on-first-retry",
  },
});
