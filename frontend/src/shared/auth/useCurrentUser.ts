import { useAuthStore } from "./authStore";

export function useCurrentUser() {
  return useAuthStore((state) => state.user);
}

export function useIsAuthenticated() {
  return useAuthStore((state) => state.status === "authenticated");
}

export function useHasPermission(permission: string) {
  return useAuthStore((state) => state.user?.permissions.includes(permission) ?? false);
}
