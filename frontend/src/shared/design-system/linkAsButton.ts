/** Shares `Button`'s primary-variant styling with `react-router-dom`'s `<Link>`, which — being a
 * navigation element, not a `<button>` — can't use the `Button` component itself (nesting an
 * interactive element inside another is both invalid HTML and an accessibility hazard). */
export const primaryButtonLinkClassName =
  "inline-flex items-center justify-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-white transition-colors duration-150 hover:bg-primary-hover";

export const secondaryButtonLinkClassName =
  "inline-flex items-center justify-center gap-2 rounded-md border border-border-default bg-surface px-4 py-2 text-sm font-medium text-text-primary transition-colors duration-150 hover:border-border-strong";
