import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "./authStore";

/** Keeps an already-logged-in user off login/register/reset screens. */
export function RequireGuest() {
  const status = useAuthStore((state) => state.status);

  if (status === "authenticated") {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
