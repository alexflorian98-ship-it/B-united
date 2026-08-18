import { expect, test } from "@playwright/test";
import { CSP_CLIENT_AUTH_FILE } from "./global-setup";

/**
 * Security-gap-closure item #7: proves the SPA still loads and its critical flows still work
 * under the Content-Security-Policy shipped in index.html, and fails on any UNEXPECTED CSP
 * violation. `securitypolicyviolation` fires for a `<meta>`-delivered policy exactly like an
 * HTTP-header-delivered one, so this is a faithful test of the real directive set even though
 * this project has no reverse-proxy layer to attach headers to.
 */
test.describe("@security content-security-policy", () => {
  test.describe("authenticated journey", () => {
    // Reuses the session global-setup.ts already established instead of driving the login form
    // again — keeps this run's real /auth/login traffic at the 2 calls global-setup makes.
    test.use({ storageState: CSP_CLIENT_AUTH_FILE });

    test("the SPA loads and the client journey works with zero CSP violations", async ({ page }) => {
      const violations: string[] = [];
      await page.addInitScript(() => {
        (window as unknown as { __cspViolations: string[] }).__cspViolations = [];
        document.addEventListener("securitypolicyviolation", (event) => {
          (window as unknown as { __cspViolations: string[] }).__cspViolations.push(
            `${event.violatedDirective}: blocked ${event.blockedURI}`,
          );
        });
      });

      await page.goto("/");

      for (const route of ["/", "/programs", "/events", "/guidance", "/community", "/billing", "/profile"]) {
        await page.evaluate((nextRoute) => {
          window.history.pushState({}, "", nextRoute);
          window.dispatchEvent(new PopStateEvent("popstate"));
        }, route);
        await page.waitForLoadState("networkidle");
      }

      await expect(page.locator("main h1")).toBeVisible();

      const collected = await page.evaluate(() => (window as unknown as { __cspViolations: string[] }).__cspViolations);
      for (const violation of collected) {
        violations.push(violation);
      }

      expect(violations, `Unexpected CSP violations:\n${violations.join("\n")}`).toEqual([]);
    });
  });

  test("the CSP meta tag is present and includes the mandatory directives", async ({ page }) => {
    await page.goto("/");

    const content = await page.locator('meta[http-equiv="Content-Security-Policy"]').getAttribute("content");
    expect(content).toBeTruthy();

    for (const directive of [
      "default-src",
      "script-src",
      "style-src",
      "img-src",
      "font-src",
      "connect-src",
      "frame-src",
      "object-src 'none'",
      "base-uri 'self'",
      "form-action 'self'",
    ]) {
      expect(content, `Missing directive: ${directive}`).toContain(directive);
    }

    // The two production-topology-dependent directives are deliberately absent from the <meta>
    // tag in BOTH dev and production (see index.html's comment) — asserting their absence here
    // keeps this test honest about what the meta-delivered policy can and cannot enforce, rather
    // than silently passing on a directive that a <meta> tag would ignore anyway per the CSP spec.
    expect(content).not.toContain("frame-ancestors");
    expect(content).not.toContain("upgrade-insecure-requests");

    // 'unsafe-eval'/'unsafe-inline' are deliberately present ONLY in `npm run dev` (Vite's React
    // Fast Refresh preamble + HMR need them; see vite.config.ts's csp-meta-tag plugin) and absent
    // from `npm run build`'s output — detect which mode is actually running via the module script
    // tag Vite emits differently per mode, so this single test is correct against either target
    // this suite might run against, instead of silently skipping the stricter production check.
    const scriptSrc = await page.locator('script[type="module"]').first().getAttribute("src");
    const isProductionBuild = scriptSrc !== null && scriptSrc.includes("/assets/");
    if (isProductionBuild) {
      expect(content).not.toContain("unsafe-eval");
      expect(content).not.toContain("unsafe-inline");
    }
  });
});
