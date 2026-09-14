import { defineConfig } from "@playwright/test";

const capture = process.env.CAPTURE_MODE;
const visualDemo = capture === "visual-demo";
if (capture && !visualDemo && !process.env.APP_URL) throw new Error("APP_URL is required for captures");

export default defineConfig({
  testDir: "./tests",
  testMatch: capture ? `**/capture/${capture}.spec.ts` : "**/*.spec.ts",
  testIgnore: capture ? [] : ["**/capture/**"],
  // The full dashboard needs extra time for a cold Next.js development compilation.
  timeout: 60000,
  expect: { timeout: 10000 },
  retries: capture ? 0 : 1,
  workers: 1,
  reporter: [
    ["html", { outputFolder: "playwright-report", open: "never" }],
    ["json", { outputFile: "playwright-report/results.json" }],
    ["list"],
  ],
  webServer: capture && !visualDemo ? undefined : {
    command: visualDemo ? "node tests/capture/start-visual-demo.mjs" : "npm run dev",
    url: visualDemo ? "http://127.0.0.1:3000/login" : "http://localhost:3000",
    env: {
      AEGISCORE_API_INTERNAL_URL: "http://127.0.0.1:9",
      NEXT_PUBLIC_DEMO_MODE: "true",
    },
    reuseExistingServer: false,
    timeout: 180000,
  },
  use: {
    baseURL: visualDemo ? "http://127.0.0.1:3000" : capture ? process.env.APP_URL : "http://localhost:3000",
    viewport: { width: 1920, height: 1080 },
    channel: "chromium",
    headless: true,
    screenshot: "only-on-failure",
    video: capture === "demo" || capture === "lifecycle" || visualDemo ? { mode: "on", size: { width: 1920, height: 1080 } } : capture ? "off" : "retain-on-failure",
    trace: capture ? "off" : "on-first-retry",
  },
});
