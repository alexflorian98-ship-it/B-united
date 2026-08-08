import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuthStore } from "./authStore";

/** Route-guard UX control only — the API independently enforces authentication on every
 * protected endpoint regardless of what the client shows (docs/DEVELOPMENT_INSTRUCTIONS.md §6:
 * "Client-side route guards ... MUST NEVER be treated as authorization"). */
export function RequireAuth() {
  const status = useAuthStore((state) => state.status);
  const location = useLocation();

  if (status !== "authenticated") {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  return <Outlet />;
}
