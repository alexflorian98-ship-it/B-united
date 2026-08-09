/** Shares `Button`'s primary-variant styling with `react-router-dom`'s `<Link>`, which — being a
 * navigation element, not a `<button>` — can't use the `Button` component itself (nesting an
 * interactive element inside another is both invalid HTML and an accessibility hazard). */
export const primaryButtonLinkClassName =
  "inline-flex min-h-11 items-center justify-center gap-2 rounded-full bg-primary px-5 py-2.5 text-sm font-medium tracking-tight text-on-primary transition-colors duration-150 hover:bg-primary-hover";

export const secondaryButtonLinkClassName =
  "inline-flex min-h-11 items-center justify-center gap-2 rounded-full border border-border-strong bg-surface px-5 py-2.5 text-sm font-medium tracking-tight text-text-primary transition-colors duration-150 hover:border-primary hover:text-primary";
