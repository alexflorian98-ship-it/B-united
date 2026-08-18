import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig, devices } from "@playwright/test";

// package.json declares "type": "module", so this file has no CommonJS __dirname — derive the
// equivalent from import.meta.url instead.
const dirname = path.dirname(fileURLToPath(import.meta.url));

const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:5173";

// Reuse the local Playwright browser cache when present so a developer/CI runner doesn't have to
// remember to export PLAYWRIGHT_BROWSERS_PATH by hand before every run.
const localBrowsersPath = path.resolve(dirname, "..", ".playwright-browsers");
if (!process.env.PLAYWRIGHT_BROWSERS_PATH && fs.existsSync(localBrowsersPath)) {
  process.env.PLAYWRIGHT_BROWSERS_PATH = localBrowsersPath;
}

export default defineConfig({
  testDir: "./e2e",
  outputDir: "./e2e-results/artifacts",
  globalSetup: "./e2e/global-setup.ts",
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  // Authentication is deliberately limited to five attempts per IP/minute. One worker keeps
  // the audit deterministic and prevents the test harness itself from looking like an attack.
  // globalSetup authenticates each demo identity exactly once (2 total /auth/login calls) and
  // every spec below reuses that session via storageState, so the real login endpoint is never
  // hit again until abuse.spec.ts deliberately exhausts it — and the explicit `dependencies`
  // below guarantee that happens strictly after every other project has finished, regardless of
  // Playwright's internal file-scheduling order.
  workers: 1,
  timeout: 30_000,
  expect: { timeout: 8_000 },
  reporter: [
    ["list"],
    ["html", { outputFolder: "e2e-results/html", open: "never" }],
    ["json", { outputFile: "e2e-results/results.json" }],
    ["./e2e/support/score-reporter.ts"],
  ],
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [
    { name: "desktop-chromium", testIgnore: /abuse\.spec\.ts/, use: { ...devices["Desktop Chrome"] } },
    {
      name: "mobile-chromium",
      testMatch: /ui-ux\.spec\.ts/,
      use: { ...devices["Pixel 7"] },
    },
    {
      name: "security-abuse-last",
      testMatch: /abuse\.spec\.ts/,
      use: { ...devices["Desktop Chrome"] },
      // Deliberately exhausts the login rate limiter — must never run before a project that
      // still needs a working, un-throttled `/auth/login` endpoint.
      dependencies: ["desktop-chromium", "mobile-chromium"],
    },
  ],
  webServer: process.env.E2E_SKIP_WEBSERVER === "1" ? undefined : {
    // Invoke the local `vite` binary directly rather than `npm run dev`: on Windows, `npm run`
    // spawns cmd.exe -> npm -> node, and terminating the top process Playwright tracks does not
    // reliably terminate the grandchild Vite process, leaving `npx playwright test` hanging after
    // the run instead of exiting cleanly. Calling the binary directly gives Playwright a single
    // process it can actually terminate.
    command: "node node_modules/vite/bin/vite.js --host localhost",
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      ...process.env,
      VITE_API_BASE_URL: process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1",
    },
  },
});
