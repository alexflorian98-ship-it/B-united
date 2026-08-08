import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";

export interface RequirePermissionProps {
  permission: string;
}

/** Same UX-only caveat as `RequireAuth` — the server independently re-checks this permission on
 * every request; this only avoids showing UI the user can't actually use. */
export function RequirePermission({ permission }: RequirePermissionProps) {
  const status = useAuthStore((state) => state.status);
  const user = useAuthStore((state) => state.user);
  const location = useLocation();

  if (status !== "authenticated") {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  if (!user?.permissions.includes(permission)) {
    return <Navigate to="/forbidden" replace />;
  }

  return <Outlet />;
}
