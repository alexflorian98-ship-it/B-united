import { expect, test } from "@playwright/test";
import { EXPERT_AUTH_FILE, FLOW_CLIENT_AUTH_FILE } from "./global-setup";
import { watchRuntime } from "./support/audit";

test.describe("@flow critical journeys", () => {
  test.describe("authenticated as client", () => {
    // Reuses the session global-setup.ts already established for the demo client (one real
    // /auth/login call for the whole run) instead of driving the login form again here — the app
    // bootstraps this storageState's refresh token via /auth/refresh, which carries no rate limit.
    test.use({ storageState: FLOW_CLIENT_AUTH_FILE });

    test("@security client reaches product areas and is denied administrator access", async ({ page }) => {
      const errors = watchRuntime(page);
      await page.goto("/");
      for (const route of ["/programs", "/guidance", "/community", "/events", "/billing"]) {
        await page.goto(route);
        await expect(page.locator("main h1")).toBeVisible();
      }
      const persisted = await page.evaluate(() => JSON.stringify({ localStorage, sessionStorage }));
      expect(persisted).not.toMatch(/accessToken|bearer|DemoAccount123/i);
      for (const route of ["/admin", "/admin/clients", "/admin/billing", "/admin/audit"]) {
        await page.goto(route);
        await expect(page).toHaveURL(/\/forbidden/);
      }
      expect(errors, errors.join("\n")).toEqual([]);
    });
  });

  test("invalid login input is rejected client-side without sending credentials", async ({ page }) => {
    let loginRequests = 0;
    page.on("request", (request) => {
      if (request.url().includes("/auth/login")) loginRequests += 1;
    });
    await page.goto("/login");
    await page.getByLabel(/email/i).fill("not-an-email");
    await page.getByRole("textbox", { name: /parol|password/i }).fill("IncorrectPassword123!");
    await page.getByRole("button", { name: /conecteaz|sign in|log in/i }).click();
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByText(/valid|validă/i)).toBeVisible();
    expect(loginRequests).toBe(0);
  });

  test.describe("authenticated as expert", () => {
    test.use({ storageState: EXPERT_AUTH_FILE });

    test("expert can reach expert workflows", async ({ page }) => {
      await page.goto("/");
      for (const route of ["/admin", "/admin/questionnaires/queue", "/admin/events", "/admin/community"]) {
        await page.goto(route);
        await expect(page).not.toHaveURL(/\/forbidden|\/login/);
        await expect(page.locator("main h1")).toBeVisible();
      }
    });
  });
});
