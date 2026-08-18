import { expect, test } from "@playwright/test";

test.describe("@security browser security gates", () => {
  test("protected routes redirect anonymous users", async ({ page }) => {
    for (const route of ["/", "/profile", "/community", "/admin", "/admin/audit"]) {
      await page.goto(route);
      await expect(page).toHaveURL(/\/login/);
    }
  });

  test("API responses do not expose stack traces and send defensive headers", async ({ page, request }) => {
    const response = await request.get(`${process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1"}/definitely-not-a-route`);
    expect([401, 404]).toContain(response.status());
    expect(await response.text()).not.toMatch(/System\.| at [A-Za-z0-9_.]+\(|stack trace/i);
    expect(response.headers()["x-content-type-options"]).toBe("nosniff");
    await page.goto("/login");
    expect(await page.locator("input[type=password]").getAttribute("autocomplete")).toBe("current-password");
  });
});
