export interface BrandMarkProps {
  /** Use on a dark (primary-colored) surface vs. a light surface. */
  tone?: "on-dark" | "on-light";
  appName: string;
}

/** The "B" monogram + wordmark, shared between the client and admin sidebars. */
export function BrandMark({ tone = "on-dark", appName }: BrandMarkProps) {
  const isDark = tone === "on-dark";
  return (
    <div className="flex items-center gap-2.5">
      <span
        className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg font-serif text-lg font-semibold ${
          isDark ? "bg-accent text-on-accent" : "bg-primary text-on-primary"
        }`}
        aria-hidden="true"
      >
        B
      </span>
      <span
        className={`font-serif text-base font-semibold tracking-wide ${isDark ? "text-on-primary" : "text-text-primary"}`}
      >
        {appName}
      </span>
    </div>
  );
}
