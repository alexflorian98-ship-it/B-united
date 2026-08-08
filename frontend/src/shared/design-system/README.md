# shared/design-system

Design tokens and base primitives (Button, Input, Card, Modal, Table, etc. — see docs/PROMPT.md section 57).

## Design tokens (P1.29)

Tokens live in [`src/index.css`](../../index.css) as a Tailwind v4 `@theme` block — CSS custom
properties that Tailwind turns into matching utility classes automatically
(`--color-primary` → `bg-primary` / `text-primary` / `border-primary`, etc.).

Categories: `background`/`surface`/`surface-raised`, `border-default`/`border-strong`,
`text-primary`/`text-secondary`/`text-muted`, `primary`/`primary-hover`,
`success`/`warning`/`danger`/`info`, `focus-ring`, plus radius, shadow, `tablet`/`desktop`
breakpoint aliases, and motion duration/easing tokens.

**Dark mode**: explicitly deferred — no dark-mode requirement exists in the product spec.
Revisit if one is requested rather than half-building a second palette speculatively.

### Usage rules

- **Never use an arbitrary Tailwind value** (`text-[#123456]`, `p-[13px]`, `shadow-[0_2px_4px_#000]`)
  for anything a token already covers. If a color/spacing/radius/shadow value isn't a token,
  that's a signal to add the token, not to reach for square-bracket syntax.
- **Semantic tokens over raw Tailwind palette classes.** Use `bg-surface`/`text-text-primary`
  instead of `bg-white`/`text-gray-900` — the semantic name is what makes a future palette
  change (or dark mode) a token edit instead of a find-and-replace across every component.
- **Don't hand-roll focus states.** `:focus-visible` is styled globally in `index.css`; only
  override it if a component has a specific, documented reason to.
- **Respect `prefers-reduced-motion`.** It's already handled globally — don't add
  transitions/animations that bypass it (e.g. via `!important` or JS-driven timing that
  ignores the media query).
