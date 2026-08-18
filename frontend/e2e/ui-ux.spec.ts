import { expect, test, type BrowserContext, type Page } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { UIUX_DESKTOP_CLIENT_AUTH_FILE, UIUX_MOBILE_CLIENT_AUTH_FILE } from "./global-setup";
import { watchRuntime } from "./support/audit";

type UiCheck = { name: string; passed: boolean; evidence?: string };

test.describe("@uiux client experience", () => {
  let context: BrowserContext;
  let auditPage: Page;

  test.beforeAll(async ({ browser }, testInfo) => {
    // Reuses the session global-setup.ts already established for the demo client instead of
    // driving the login form again — keeps this run's real /auth/login traffic at the 2 calls
    // global-setup makes. This spec runs under both desktop-chromium and mobile-chromium, each as
    // its own browser context; refresh tokens are single-use/rotating, so each project gets its
    // own distinct pre-minted token rather than both racing to consume the same one.
    const authFile = testInfo.project.name === "mobile-chromium" ? UIUX_MOBILE_CLIENT_AUTH_FILE : UIUX_DESKTOP_CLIENT_AUTH_FILE;
    context = await browser.newContext({ storageState: authFile });
    auditPage = await context.newPage();
    await auditPage.goto("/");
  });
  test.afterAll(async () => context.close());

  // Playwright's own test-callback contract requires the first argument to literally be an
  // object destructuring pattern (it statically inspects the signature to know which fixtures a
  // test needs) — this test uses none, so the pattern must stay empty rather than be replaced
  // with a named/prefixed parameter.
  // eslint-disable-next-line no-empty-pattern
  test("all primary routes are accessible, stable, responsive, and keyboard usable", async ({}, testInfo) => {
    test.setTimeout(60_000);
    const checks: UiCheck[] = [];
    const runtimeErrors = watchRuntime(auditPage);

    for (const route of ["/", "/programs", "/events", "/guidance", "/community", "/billing", "/profile"]) {
      await auditPage.evaluate((nextRoute) => {
        window.history.pushState({}, "", nextRoute);
        window.dispatchEvent(new PopStateEvent("popstate"));
      }, route);
      await auditPage.waitForLoadState("networkidle");
      await auditPage.waitForTimeout(150);
      checks.push({ name: `${route} main landmark`, passed: await auditPage.locator("main").isVisible() });
      checks.push({ name: `${route} h1`, passed: await auditPage.locator("h1").isVisible() });
      const overflow = await auditPage.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      checks.push({ name: `${route} responsive overflow`, passed: overflow <= 1, evidence: `${overflow}px overflow` });

      const axe = await new AxeBuilder({ page: auditPage }).analyze();
      const serious = axe.violations.filter((item) => item.impact === "critical" || item.impact === "serious");
      checks.push({
        name: `${route} WCAG serious/critical`,
        passed: serious.length === 0,
        evidence: serious.map((item) => `${item.id}: ${item.help}`).join("; ") || undefined,
      });
      await testInfo.attach(`axe-${route.replaceAll("/", "-") || "home"}`, {
        body: JSON.stringify(axe.violations, null, 2), contentType: "application/json",
      });
    }

    await auditPage.evaluate(() => {
      window.history.pushState({}, "", "/programs");
      window.dispatchEvent(new PopStateEvent("popstate"));
    });
    await auditPage.keyboard.press("Tab");
    const focused = auditPage.locator(":focus-visible");
    const focusCount = await focused.count();
    const box = focusCount > 0 ? await focused.first().boundingBox() : null;
    checks.push({ name: "visible keyboard focus", passed: focusCount > 0 });
    checks.push({ name: "keyboard target width", passed: (box?.width ?? 0) >= 24, evidence: `${box?.width ?? 0}px` });
    checks.push({ name: "keyboard target height", passed: (box?.height ?? 0) >= 24, evidence: `${box?.height ?? 0}px` });
    checks.push({ name: "runtime stability", passed: runtimeErrors.length === 0, evidence: runtimeErrors.join("; ") || undefined });

    await testInfo.attach("uiux-score", {
      body: JSON.stringify(checks), contentType: "application/json",
    });

    for (const check of checks) {
      expect.soft(check.passed, `${check.name}${check.evidence ? ` — ${check.evidence}` : ""}`).toBe(true);
    }
  });
});
