import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { request as playwrightRequest } from "@playwright/test";
import { demo } from "./support/audit";

// package.json declares "type": "module", so this file has no CommonJS __dirname — derive the
// equivalent from import.meta.url instead.
const dirname = path.dirname(fileURLToPath(import.meta.url));

/**
 * `/auth/login` is limited to 5 requests per IP per minute (the "auth" rate-limit policy — see
 * src/BuildingBlocks/Security/Abuse/RateLimitingExtensions.cs). Every spec that previously drove
 * the login form itself (flow.spec.ts x2, csp.spec.ts, ui-ux.spec.ts) added its own real POST to
 * that endpoint; combined with Playwright's own test scheduling across projects not strictly
 * honoring project declaration order, that could push the shared-IP request count high enough
 * for abuse.spec.ts's deliberate 429 test (or unlucky interleaving) to starve a later project's
 * login before it ever got a token.
 *
 * This global setup authenticates directly against the API instead of letting each spec drive the
 * login form itself. Each spec that needs to be authenticated loads a storageState file with a
 * persisted refresh token; the app's SessionProvider bootstraps from it via `/auth/refresh` on
 * load. BUT refresh tokens are single-use and rotating (P1.37.a) — a NEW browser context consumes
 * and rotates whatever token its storageState file holds, so two different contexts can never
 * safely load the exact same token (the second one to bootstrap gets rejected as reuse), AND a
 * refresh token cannot be "forked" into multiple still-valid children (calling `/auth/refresh`
 * itself consumes the input token, so chaining refreshes only ever yields one still-unused token
 * at the end of the chain, not one per link). Every spec below that needs its own browser context
 * therefore gets its OWN real `/auth/login` call — 5 total for the whole canonical run (4 for the
 * client identity's 4 independent browser contexts across csp.spec.ts/flow.spec.ts/
 * ui-ux.spec.ts's two projects, 1 for the expert identity), still within the 5/minute cap on that
 * endpoint — and the explicit project `dependencies` in playwright.config.ts additionally
 * guarantee abuse.spec.ts still runs strictly last regardless of scheduling order.
 */
const authDir = path.resolve(dirname, ".auth");

interface TokenPair {
  accessToken: string;
  refreshToken: string;
}

/**
 * Two consecutive canonical runs within the same fixed 60s rate-limit window would otherwise
 * both need the full 5-login budget from the same IP and the second run's global setup would
 * itself get a 429 — a real fragility, confirmed live. Rather than touch the limiter, this backs
 * off and retries using the server's own `Retry-After` header: a legitimate wait for the window
 * to roll over, not a bypass of the control being tested elsewhere in this same suite
 * (abuse.spec.ts).
 */
async function login(email: string, apiBaseUrl: string, attempt = 1): Promise<TokenPair> {
  const context = await playwrightRequest.newContext();
  try {
    const response = await context.post(`${apiBaseUrl}/auth/login`, {
      data: { email, password: demo.password },
    });
    if (response.status() === 429 && attempt <= 2) {
      const retryAfterSeconds = Number(response.headers()["retry-after"] ?? "60");
      const waitMs = Math.min(Math.max(retryAfterSeconds, 1), 65) * 1000;
      // eslint-disable-next-line no-console -- global setup has no test reporter to log through
      console.log(`[global-setup] /auth/login rate-limited for ${email}, waiting ${waitMs}ms before retry ${attempt}/2...`);
      await new Promise((resolve) => setTimeout(resolve, waitMs));
      return login(email, apiBaseUrl, attempt + 1);
    }
    if (!response.ok()) {
      throw new Error(`Global setup login failed for ${email}: HTTP ${response.status()}`);
    }
    return (await response.json()) as TokenPair;
  } finally {
    await context.dispose();
  }
}

function writeStorageState(fileName: string, refreshToken: string, appOrigin: string): void {
  fs.mkdirSync(authDir, { recursive: true });
  fs.writeFileSync(
    path.join(authDir, fileName),
    `${JSON.stringify(
      {
        cookies: [],
        origins: [
          {
            origin: appOrigin,
            localStorage: [{ name: "bunited.refreshToken", value: refreshToken }],
          },
        ],
      },
      null,
      2,
    )}\n`,
  );
}

/**
 * Mints one distinct, never-reused refresh token per requested file name for a single identity —
 * a separate real `/auth/login` per file. Refresh tokens cannot be forked (calling
 * `/auth/refresh` itself consumes its input token and yields exactly one new one, so chaining
 * only ever produces one still-unused token at the very end of the chain), so a fresh login per
 * consumer is the only way to hand out N independently-valid tokens for the same identity.
 */
async function saveDistinctAuthStates(email: string, fileNames: string[], apiBaseUrl: string, appOrigin: string): Promise<void> {
  for (const fileName of fileNames) {
    const tokenPair = await login(email, apiBaseUrl);
    writeStorageState(fileName, tokenPair.refreshToken, appOrigin);
  }
}

export default async function globalSetup(): Promise<void> {
  const apiBaseUrl = process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1";
  const appOrigin = process.env.E2E_BASE_URL ?? "http://localhost:5173";

  // One distinct token per browser context that will bootstrap from it: csp.spec.ts,
  // flow.spec.ts's client describe block, and ui-ux.spec.ts's beforeAll (once per project it
  // runs under — desktop-chromium AND mobile-chromium both match ui-ux.spec.ts).
  await saveDistinctAuthStates(
    demo.clientEmail,
    ["csp-client.json", "flow-client.json", "ui-ux-desktop-client.json", "ui-ux-mobile-client.json"],
    apiBaseUrl,
    appOrigin,
  );
  await saveDistinctAuthStates(demo.expertEmail, ["expert.json"], apiBaseUrl, appOrigin);
}

export const CSP_CLIENT_AUTH_FILE = path.join(authDir, "csp-client.json");
export const FLOW_CLIENT_AUTH_FILE = path.join(authDir, "flow-client.json");
export const UIUX_DESKTOP_CLIENT_AUTH_FILE = path.join(authDir, "ui-ux-desktop-client.json");
export const UIUX_MOBILE_CLIENT_AUTH_FILE = path.join(authDir, "ui-ux-mobile-client.json");
export const EXPERT_AUTH_FILE = path.join(authDir, "expert.json");
