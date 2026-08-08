import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { AdminLayout } from "./AdminLayout";

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
  it("renders the full admin nav in the sidebar and the page content", () => {
    renderLayout();

    for (const label of ["Dashboard", "Programs", "Questionnaires", "Subscribers", "Audit", "Settings"]) {
      expect(screen.getByRole("link", { name: label })).toBeInTheDocument();
    }
    expect(screen.getByText("Page content")).toBeInTheDocument();
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
