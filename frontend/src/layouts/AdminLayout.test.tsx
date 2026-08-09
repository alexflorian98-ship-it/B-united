import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { useAuthStore } from "../shared/auth/authStore";
import { AdminLayout } from "./AdminLayout";
import { ADMIN_SHELL_PERMISSIONS } from "./navigation";

function renderLayout() {
  return render(
    <MemoryRouter initialEntries={["/admin"]}>
      <AdminLayout>
        <p>Page content</p>
      </AdminLayout>
    </MemoryRouter>,
  );
}

describe("AdminLayout", () => {
  // An administrator-equivalent user (every admin permission) so the pre-existing tests below
  // keep exercising the full nav; permission-filtering itself is covered separately.
  beforeEach(() => {
    useAuthStore.setState({
      status: "authenticated",
      user: { id: "u1", email: "admin@example.com", permissions: ADMIN_SHELL_PERMISSIONS },
    });
  });
  afterEach(() => useAuthStore.setState({ status: "unauthenticated", accessToken: null, user: null }));

  it("renders the full admin nav in the sidebar and the page content", () => {
    renderLayout();

    for (const label of ["Dashboard", "Programs", "Questionnaires", "Clients", "Audit"]) {
      expect(screen.getByRole("link", { name: label })).toBeInTheDocument();
    }
    expect(screen.getByText("Page content")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to application" })).toHaveAttribute("href", "/");
  });

  it("the mobile drawer is closed by default and opens via the hamburger button", async () => {
    const user = userEvent.setup();
    renderLayout();

    const openButton = screen.getByRole("button", { name: "Open menu" });
    expect(openButton).toHaveAttribute("aria-expanded", "false");

    // Sidebar already renders the links once; opening the drawer adds a second copy.
    expect(screen.getAllByRole("link", { name: "Dashboard" })).toHaveLength(1);

    await user.click(openButton);

    expect(openButton).toHaveAttribute("aria-expanded", "true");
    expect(screen.getAllByRole("link", { name: "Dashboard" })).toHaveLength(2);
  });

  it("the drawer closes via its close button, reachable by keyboard", async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole("button", { name: "Open menu" }));
    expect(screen.getAllByRole("link", { name: "Dashboard" })).toHaveLength(2);

    const closeButton = screen.getByRole("button", { name: "Close menu" });
    closeButton.focus();
    await user.keyboard("{Enter}");

    expect(screen.getAllByRole("link", { name: "Dashboard" })).toHaveLength(1);
  });

  it("clicking a nav link inside the drawer closes it", async () => {
    const user = userEvent.setup();
    renderLayout();

    await user.click(screen.getByRole("button", { name: "Open menu" }));
    const drawerLinks = screen.getAllByRole("link", { name: "Programs" });
    await user.click(drawerLinks[drawerLinks.length - 1]);

    expect(screen.getAllByRole("link", { name: "Dashboard" })).toHaveLength(1);
  });
});

describe("AdminLayout nav filtering by permission", () => {
  afterEach(() => useAuthStore.setState({ status: "unauthenticated", accessToken: null, user: null }));

  it("a moderator-only account (chat.moderate) sees Community but not Programs or Billing", () => {
    useAuthStore.setState({
      status: "authenticated",
      user: { id: "u1", email: "moderator@example.com", permissions: ["chat.moderate"] },
    });
    renderLayout();

    expect(screen.getByRole("link", { name: "Community" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Programs" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Billing" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Events" })).not.toBeInTheDocument();
    // Destinations with no specific permission requirement remain visible to any admin.
    expect(screen.getByRole("link", { name: "Dashboard" })).toBeInTheDocument();
  });

  it("a billing-manager-only account (billing.manage) sees Billing but not Programs", () => {
    useAuthStore.setState({
      status: "authenticated",
      user: { id: "u1", email: "billing@example.com", permissions: ["billing.manage"] },
    });
    renderLayout();

    expect(screen.getByRole("link", { name: "Billing" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Programs" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Community" })).not.toBeInTheDocument();
  });

  it("an event-manager-only account (events.manage) sees Events without program-management access", () => {
    useAuthStore.setState({
      status: "authenticated",
      user: { id: "u1", email: "events@example.com", permissions: ["events.manage"] },
    });
    renderLayout();

    expect(screen.getByRole("link", { name: "Events" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Programs" })).not.toBeInTheDocument();
  });

  it("an expert account sees the questionnaire review queue without buyer entitlement permissions", () => {
    useAuthStore.setState({
      status: "authenticated",
      user: { id: "u1", email: "expert@example.com", permissions: ["questionnaire.review", "questionnaire.answer"] },
    });
    renderLayout();

    expect(screen.getByRole("link", { name: "Questionnaires" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Billing" })).not.toBeInTheDocument();
  });
});
