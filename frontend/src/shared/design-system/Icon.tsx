import type { SVGAttributes } from "react";

export type IconName =
  | "home"
  | "programs"
  | "progress"
  | "questionnaire"
  | "events"
  | "community"
  | "profile"
  | "billing"
  | "admin"
  | "subscribers"
  | "notifications"
  | "audit"
  | "settings"
  | "chevron-right"
  | "chevron-left"
  | "check"
  | "close"
  | "play"
  | "calendar"
  | "warning"
  | "menu"
  | "logout";

const paths: Record<IconName, string> = {
  home: "M4 11.5 12 4l8 7.5M6 10v9a1 1 0 0 0 1 1h3v-6h4v6h3a1 1 0 0 0 1-1v-9",
  programs: "M5 4.5A1.5 1.5 0 0 1 6.5 3H16l3 3v13.5a1.5 1.5 0 0 1-1.5 1.5h-11A1.5 1.5 0 0 1 5 19.5v-15ZM9 9h6M9 12.5h6M9 16h4",
  progress: "M4 19V10M10 19V5M16 19v-7M22 19H2",
  questionnaire: "M6 3.5h9L19 8v12.5a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1v-16a1 1 0 0 1 1-1ZM14 3.5V8h5M8.5 12.5l1.5 1.5 3-3",
  events: "M4 8h16M4 8v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V8M4 8V6a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v2M8 3v4M16 3v4M8 13h3",
  community: "M4 5.5h12a2 2 0 0 1 2 2V13a2 2 0 0 1-2 2H10l-4 4v-4H4a1 1 0 0 1-1-1V7.5a2 2 0 0 1 1-2Z",
  profile: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM4.5 20.5a7.5 7.5 0 0 1 15 0",
  billing: "M3 6.5h18a1 1 0 0 1 1 1V17a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V7.5a1 1 0 0 1 1-1ZM2 10.5h20M6 14.5h4",
  admin: "M12 3 4 6.5V12c0 4.5 3.4 8.2 8 9 4.6-.8 8-4.5 8-9V6.5L12 3Zm-2.5 9 2 2 3.5-4",
  subscribers: "M9 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM2.5 20a6.5 6.5 0 0 1 13 0M17 6a3.5 3.5 0 0 1 0 7M18.5 13.5c2.5.4 4 1.9 4 4v2.5",
  notifications: "M12 3.5a5 5 0 0 0-5 5V13l-1.7 3.4a1 1 0 0 0 .9 1.6h11.6a1 1 0 0 0 .9-1.6L17 13V8.5a5 5 0 0 0-5-5ZM9.5 19.5a2.5 2.5 0 0 0 5 0",
  audit: "M6 3.5h9L19 8v12.5a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1v-16a1 1 0 0 1 1-1ZM9 12h6M9 15.5h6M9 8.5h2",
  settings: "M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm8-3a7.97 7.97 0 0 0-.3-2.2l2-1.6-2-3.4-2.4.9a8 8 0 0 0-1.9-1.1L15 2h-4l-.4 2.5a8 8 0 0 0-1.9 1.1l-2.4-.9-2 3.4 2 1.6A8 8 0 0 0 4 12c0 .8.1 1.5.3 2.2l-2 1.6 2 3.4 2.4-.9c.6.5 1.2.8 1.9 1.1L9 22h4l.4-2.5a8 8 0 0 0 1.9-1.1l2.4.9 2-3.4-2-1.6c.2-.7.3-1.4.3-2.2Z",
  "chevron-right": "m9 5 7 7-7 7",
  "chevron-left": "m15 5-7 7 7 7",
  check: "m5 13 4 4 10-10",
  close: "m6 6 12 12M18 6 6 18",
  play: "M7 4.5v15l13-7.5-13-7.5Z",
  calendar: "M4 8h16M4 8v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V8M4 8V6a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v2M8 3v4M16 3v4",
  warning: "M12 4 2.5 20h19L12 4Zm0 6.5v4.25M12 17.25h.01",
  menu: "M4 6h16M4 12h16M4 18h16",
  logout: "M9 4.5H6a1 1 0 0 0-1 1v13a1 1 0 0 0 1 1h3M15 15.5l4-4-4-4M19 11.5H8",
};

export interface IconProps extends Omit<SVGAttributes<SVGSVGElement>, "children"> {
  name: IconName;
  size?: number;
}

/** Inline line-icon set (24x24, `currentColor`) covering nav and common actions — kept as a
 * single source-of-truth map instead of scattering ad hoc inline SVGs across screens. */
export function Icon({ name, size = 20, className = "", ...props }: IconProps) {
  return (
    <svg
      aria-hidden="true"
      focusable="false"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.75}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      {...props}
    >
      <path d={paths[name]} />
    </svg>
  );
}
