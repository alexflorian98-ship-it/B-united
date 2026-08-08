import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { IconButton } from "./IconButton";

describe("IconButton", () => {
  it("exposes its accessible name via the required label prop", () => {
    render(<IconButton label="Close menu">×</IconButton>);
    expect(screen.getByRole("button", { name: "Close menu" })).toBeInTheDocument();
  });

  it("is keyboard-activatable", async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(
      <IconButton label="Close menu" onClick={onClick}>
        ×
      </IconButton>,
    );

    await user.tab();
    await user.keyboard("{Enter}");

    expect(onClick).toHaveBeenCalledTimes(1);
  });
});
