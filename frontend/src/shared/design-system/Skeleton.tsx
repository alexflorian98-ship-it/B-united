import type { HTMLAttributes } from "react";

/** Loading placeholder. `aria-hidden` so screen readers don't announce empty shapes. */
export function Skeleton({ className = "", ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div aria-hidden="true" className={`animate-pulse rounded-md bg-border-default ${className}`} {...props} />;
}
