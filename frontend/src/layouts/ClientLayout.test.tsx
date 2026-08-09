import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { ClientLayout } from "./ClientLayout";
import { useAuthStore } from "../shared/auth/authStore";
import { WellKnownPermissions } from "../shared/permissions/wellKnownPermissions";

afterEach(() => useAuthStore.setState({ status: "unauthenticated", accessToken: null, user: null }));

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <ClientLayout>
        <p>Page content</p>
      </ClientLayout>
    </MemoryRouter>,
  );
}

describe("ClientLayout", () => {
  it("renders the full client nav and the page content", () => {
    renderAt("/");

    // Each nav item appears twice: once in the tablet+ sidebar, once in the mobile bottom nav.
    expect(screen.getAllByRole("link", { name: "Home" })).toHaveLength(2);
    expect(screen.getAllByRole("link", { name: "Programs" })).toHaveLength(2);
    expect(screen.getAllByRole("link", { name: "My Guidance" })).toHaveLength(1); // sidebar only
    expect(screen.getAllByRole("link", { name: "Billing" })).toHaveLength(1); // sidebar only
    expect(screen.getByText("Page content")).toBeInTheDocument();
  });

  it("marks the active route with aria-current", () => {
    renderAt("/programs");

    const activeLinks = screen.getAllByRole("link", { name: "Programs" });
    for (const link of activeLinks) {
      expect(link).toHaveAttribute("aria-current", "page");
    }
    const homeLinks = screen.getAllByRole("link", { name: "Home" });
    for (const link of homeLinks) {
      expect(link).not.toHaveAttribute("aria-current");
    }
  });

  it("restricts the mobile bottom nav to the priority subset", () => {
    renderAt("/");

    // "My Guidance" and "Billing" are sidebar-only (not in the mobile priority set).
    expect(screen.queryAllByRole("link", { name: "My Guidance" })).toHaveLength(1);
    expect(screen.queryAllByRole("link", { name: "Billing" })).toHaveLength(1);
  });

  it("shows administration navigation only to an authorized user", () => {
    const user = { id: "admin-1", email: "admin@example.com", permissions: [WellKnownPermissions.ContentCreate] };
    useAuthStore.setState({ status: "authenticated", accessToken: "token", user });
    renderAt("/");

    expect(screen.getByRole("link", { name: "Administration" })).toHaveAttribute("href", "/admin");
    expect(screen.getByRole("link", { name: "Admin" })).toHaveAttribute("href", "/admin");
  });
});
