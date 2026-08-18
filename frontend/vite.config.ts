/// <reference types="vitest/config" />
import { defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

/**
 * Content-Security-Policy (security-gap-closure item #7). Injects the <meta> tag via
 * transformIndexHtml instead of writing it statically in index.html, because the policy that is
 * actually correct differs between `npm run dev` and `npm run build`:
 *
 * Source inventory (shared by both modes):
 *   - script-src: 'self' (Vite-built ES modules only — no inline <script>/event-handler
 *     attributes; React never emits either) plus YouTube's IFrame Player API loader
 *     (YouTubePlayer.tsx loads https://www.youtube.com/iframe_api, which loads player assets from
 *     s.ytimg.com).
 *   - style-src: 'self' (Tailwind's compiled stylesheet) plus Google Fonts' CSS endpoint. React's
 *     `style={{...}}` prop (used once, ProgressBar.tsx) sets CSS properties through the CSSOM
 *     `style` object, not an HTML `style="..."` attribute string, so it is NOT subject to
 *     style-src's inline-style restriction in a production build — no 'unsafe-inline' needed
 *     there.
 *   - img-src: 'self', data: (inline SVG/icon usage), and YouTube's thumbnail hosts.
 *   - font-src: 'self' plus Google Fonts' font-file host.
 *   - connect-src: 'self' plus the two local API origins README.md's own documented workflows
 *     point the SPA at (5080 for `dotnet run`, 8080 for the Docker Compose demo stack) plus
 *     Google Fonts' two hosts — the `<link rel="preconnect">` hints in index.html are themselves
 *     governed by connect-src, not style-src/font-src (confirmed via a real
 *     securitypolicyviolation while verifying this in csp.spec.ts).
 *   - frame-src: YouTube only (embedded video playback).
 *   - object-src/base-uri/form-action: locked down; nothing in this app uses <object>, a
 *     non-'self' <base>, or cross-origin form submission.
 *
 * Why dev and build differ: Vite's dev server injects CSS updates as inline <style> tags for
 * Hot Module Replacement and its client can use eval-like mechanisms for fast refresh — neither
 * is present in `npm run build`'s output, which emits plain <link rel="stylesheet"> files and
 * plain ES modules (no eval; see also src/shared/zodJitlessConfig.ts, which removes Zod's own
 * eval probe so the PRODUCTION build needs no 'unsafe-eval' either). Relaxing style-src/script-src
 * only in `serve` (dev) mode means the policy actually shipped to users (`npm run build`) stays
 * exactly as strict as the source inventory above describes — verified with zero violations by
 * csp.spec.ts against the real production build, not the dev server.
 *
 * Two directives are intentionally never set, in either mode: `frame-ancestors` (the CSP spec
 * requires browsers to ignore it when delivered via <meta> — it only takes effect as an HTTP
 * response header, which requires the real production static-host/reverse-proxy config; the
 * Api's own responses already send X-Frame-Options: DENY, see SecurityHeadersMiddleware) and
 * `upgrade-insecure-requests` (would break local HTTP dev/demo; only correct once the SPA is
 * actually served over HTTPS). Both remain BLOCKED — REQUIRES DEPLOYED DOMAIN in
 * docs/E2E_AUDIT_RESULT.md's coverage matrix, to be added at the production hosting layer.
 */
function cspMetaTagPlugin(): Plugin {
  return {
    name: 'csp-meta-tag',
    transformIndexHtml(_html, context) {
      const isDev = context.server !== undefined

      const directives = [
        `default-src 'self'`,
        // Dev-only: @vitejs/plugin-react injects an inline <script type="module"> preamble for
        // React Fast Refresh (blocked without 'unsafe-inline', which breaks the whole app with
        // "can't detect preamble" — confirmed while verifying this), and Vite's dev module
        // graph/HMR client can use eval-like mechanisms ('unsafe-eval'). Neither is emitted by
        // `npm run build`.
        `script-src 'self' https://www.youtube.com https://s.ytimg.com${isDev ? " 'unsafe-eval' 'unsafe-inline'" : ''}`,
        `style-src 'self' https://fonts.googleapis.com${isDev ? " 'unsafe-inline'" : ''}`,
        `img-src 'self' data: https://i.ytimg.com https://img.youtube.com`,
        `font-src 'self' https://fonts.gstatic.com`,
        `connect-src 'self' http://localhost:5080 http://localhost:8080 https://fonts.googleapis.com https://fonts.gstatic.com${isDev ? ' ws://localhost:5173' : ''}`,
        `frame-src https://www.youtube.com`,
        `object-src 'none'`,
        `base-uri 'self'`,
        `form-action 'self'`,
      ].join('; ')

      return [
        {
          tag: 'meta',
          attrs: { 'http-equiv': 'Content-Security-Policy', content: directives },
          injectTo: 'head-prepend',
        },
      ]
    },
  }
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss(), cspMetaTagPlugin()],
  test: {
    environment: 'jsdom',
    setupFiles: './src/setupTests.ts',
    globals: true,
    // Vitest's default include glob also matches the Playwright specs under e2e/ (*.spec.ts);
    // those belong to the separate `npm run test:e2e` runner, not this unit-test run.
    exclude: ['**/node_modules/**', '**/e2e/**'],
  },
})
