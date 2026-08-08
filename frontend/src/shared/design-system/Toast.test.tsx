import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Toast } from "./Toast";

describe("Toast", () => {
  it("renders as a polite live region", () => {
    render(<Toast onDismiss={() => {}}>Saved successfully</Toast>);
    const toast = screen.getByRole("status");
    expect(toast).toHaveAttribute("aria-live", "polite");
    expect(toast).toHaveTextContent("Saved successfully");
  });

  it("dismiss button is reachable and activatable via keyboard", async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    render(<Toast onDismiss={onDismiss}>Saved successfully</Toast>);

    await user.tab();
    expect(screen.getByRole("button", { name: "Dismiss" })).toHaveFocus();

    await user.keyboard("{Enter}");
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });
});
