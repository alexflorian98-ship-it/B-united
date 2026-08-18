import AxeBuilder from "@axe-core/playwright";
import { expect, type Page, type TestInfo } from "@playwright/test";

export const demo = {
  clientEmail: process.env.E2E_CLIENT_EMAIL ?? "demo.client@bunited.local",
  expertEmail: process.env.E2E_EXPERT_EMAIL ?? "demo.expert@bunited.local",
  password: process.env.E2E_PASSWORD ?? "DemoAccount123!",
};

export async function login(page: Page, email = demo.clientEmail): Promise<void> {
  await page.goto("/login");
  await page.getByLabel(/email/i).fill(email);
  await page.getByRole("textbox", { name: /parol|password/i }).fill(demo.password);
  await page.getByRole("button", { name: /conecteaz|sign in|log in/i }).click();
  await expect(page).not.toHaveURL(/\/login/);
}

export function watchRuntime(page: Page): string[] {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(`pageerror: ${error.message}`));
  page.on("console", (message) => {
    if (message.type() === "error" && !message.text().includes("ERR_NETWORK_ACCESS_DENIED")) {
      errors.push(`console: ${message.text()}`);
    }
  });
  return errors;
}

export async function assertNoSeriousA11yViolations(page: Page, testInfo: TestInfo): Promise<void> {
  const result = await new AxeBuilder({ page }).analyze();
  const violations = result.violations.filter((item) => item.impact === "critical" || item.impact === "serious");
  await testInfo.attach("axe-results", {
    body: JSON.stringify(result.violations, null, 2),
    contentType: "application/json",
  });
  expect(violations, violations.map((item) => `${item.id}: ${item.help}`).join("\n")).toEqual([]);
}

export async function assertNoHorizontalOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow, `Page overflows horizontally by ${overflow}px`).toBeLessThanOrEqual(1);
}
