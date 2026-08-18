# Claude Code task: run and assess the B-United E2E audit

Run the repository's Playwright audit against a clean non-Production demo stack. Do not weaken, skip, or rewrite a failing assertion merely to improve the score.

## Execution

1. From the repository root, run `docker compose up --build -d` and wait until `http://localhost:8080/health` is healthy.
2. From `frontend`, run `npm install` and `npx playwright install chromium`.
3. Run `npm run test:e2e`. Playwright starts Vite automatically and points it at `http://localhost:8080/api/v1`.
4. Read `frontend/e2e-results/score.md`, `score.json`, the HTML report, traces, screenshots, videos, browser console errors, and failed response details.
5. If infrastructure cannot start, report the audit as BLOCKED; do not convert missing infrastructure into a product score.

## Scoring and release decision

- UI/UX: 35%. Accessibility (axe serious/critical), responsive overflow, visible focus, semantic headings, and runtime stability.
- Security: 35%. Anonymous and role-based route protection, browser token storage, error leakage, defensive headers, and password-field behavior.
- Flow: 30%. Client sign-in/navigation, safe invalid-login feedback, and expert workflow access.
- A failed Security check is always a release blocker, even when the weighted score is high.
- The extended security project additionally probes anonymous access across every sensitive module family, SQL/time-based injection, reflected XSS, CRLF/header injection, path traversal, malformed identifiers, method override, unsupported media types, hostile-origin CORS, structured-error leakage, correlation IDs, and authentication rate limiting. The abuse project runs last so its deliberate 429 does not contaminate functional results.
- Recommended interpretation: 90-100 release candidate; 80-89 fix before release if practical; 70-79 not ready; below 70 critical rework.

## Required final response

Return:

1. Overall score and all three category scores.
2. PASS/FAIL/BLOCKED release decision.
3. Findings ordered by severity, each with evidence, affected route, reproduction steps, and a concrete remediation.
4. Separate false positives or environment failures from application defects.
5. Exact commands run and artifact paths.
6. Remaining coverage gaps. Explicitly note that this suite does not claim mathematical proof against every injection variant. It complements source review, authenticated two-user IDOR tests, webhook-signature tests, upload malware/content validation, dependency/SAST/DAST scanning, infrastructure TLS/header review, log inspection, database encryption verification, and an authorized external penetration test.

Use the seeded credentials only in non-Production: `demo.client@bunited.local`, `demo.expert@bunited.local`, and password `DemoAccount123!`. Never print tokens or sensitive response bodies.
