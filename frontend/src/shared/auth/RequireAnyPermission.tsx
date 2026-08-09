import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";

export interface RequireAnyPermissionProps {
  /** The route renders if the user holds at least one of these permissions. */
  permissions: string[];
}

/** Same UX-only caveat as `RequirePermission` — the server independently re-checks the exact
 * permission required by each endpoint on every request; this only avoids showing UI the user
 * can't actually use for any of it. Use this (instead of stacking several `RequirePermission`s)
 * when a destination is reachable by any one of several specialized administrative permissions,
 * e.g. the shared `/admin` shell itself. */
export function RequireAnyPermission({ permissions }: RequireAnyPermissionProps) {
  const status = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);
  const location = useLocation();

  if (status !== "authenticated") {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  const hasAnyPermission = permissions.some((permission) => user?.permissions.includes(permission));
  if (!hasAnyPermission) {
    return <Navigate to="/forbidden" replace />;
  }

  return <Outlet />;
}
