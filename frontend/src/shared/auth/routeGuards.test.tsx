import { render, screen } from "@testing-library/react";
import type { ReactElement } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { RequireAuth } from "./RequireAuth";
import { RequireGuest } from "./RequireGuest";
import { RequirePermission } from "./RequirePermission";
import { useAuthStore } from "./authStore";

function renderGuarded(guard: ReactElement, initialPath: string) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={guard}>
          <Route path="/protected" element={<div>Protected content</div>} />
        </Route>
        <Route path="/login" element={<div>Login page</div>} />
        <Route path="/" element={<div>Home page</div>} />
        <Route path="/forbidden" element={<div>Forbidden page</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("RequireAuth", () => {
  afterEach(() => useAuthStore.setState({ status: "unauthenticated", accessToken: null, user: null }));

  it("redirects an unauthenticated visitor to /login instead of rendering the protected route", () => {
    useAuthStore.setState({ status: "unauthenticated" });
    renderGuarded(<RequireAuth />, "/protected");

    expect(screen.getByText("Login page")).toBeInTheDocument();
    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });

  it("renders the protected route for an authenticated user", () => {
    useAuthStore.setState({ status: "authenticated", user: { id: "u1", email: "a@example.com", permissions: [] } });
    renderGuarded(<RequireAuth />, "/protected");

    expect(screen.getByText("Protected content")).toBeInTheDocument();
  });
});

describe("RequireGuest", () => {
  afterEach(() => useAuthStore.setState({ status: "unauthenticated", accessToken: null, user: null }));

  it("keeps an already-authenticated user off a guest-only route", () => {
    useAuthStore.setState({ status: "authenticated", user: { id: "u1", email: "a@example.com", permissions: [] } });
    renderGuarded(<RequireGuest />, "/protected");

    expect(screen.getByText("Home page")).toBeInTheDocument();
    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });

  it("renders the guest route for an unauthenticated visitor", () => {
    useAuthStore.setState({ status: "unauthenticated" });
    renderGuarded(<RequireGuest />, "/protected");

    expect(screen.getByText("Protected content")).toBeInTheDocument();
  });
});

describe("RequirePermission", () => {
  afterEach(() => useAuthStore.setState({ status: "unauthenticated", accessToken: null, user: null }));

  it("sends an unauthenticated visitor to /login", () => {
    useAuthStore.setState({ status: "unauthenticated" });
    renderGuarded(<RequirePermission permission="content.create" />, "/protected");

    expect(screen.getByText("Login page")).toBeInTheDocument();
  });

  it("sends an authenticated user without the permission to /forbidden", () => {
    useAuthStore.setState({ status: "authenticated", user: { id: "u1", email: "a@example.com", permissions: ["content.view"] } });
    renderGuarded(<RequirePermission permission="content.create" />, "/protected");

    expect(screen.getByText("Forbidden page")).toBeInTheDocument();
  });

  it("renders the route for a user holding the required permission", () => {
    useAuthStore.setState({ status: "authenticated", user: { id: "u1", email: "a@example.com", permissions: ["content.create"] } });
    renderGuarded(<RequirePermission permission="content.create" />, "/protected");

    expect(screen.getByText("Protected content")).toBeInTheDocument();
  });
});
