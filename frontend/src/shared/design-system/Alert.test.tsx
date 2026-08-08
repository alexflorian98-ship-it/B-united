import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Alert } from "./Alert";

describe("Alert", () => {
  it("renders with the alert role so assistive tech announces it", () => {
    render(<Alert tone="danger" title="Something went wrong" />);
    expect(screen.getByRole("alert")).toHaveTextContent("Something went wrong");
  });

  it("dismiss button is keyboard-activatable", async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    render(<Alert tone="info" title="Heads up" onDismiss={onDismiss} />);

    await user.tab();
    expect(screen.getByRole("button", { name: "Dismiss" })).toHaveFocus();

    await user.keyboard("{Enter}");
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it("renders no dismiss button when onDismiss is not provided", () => {
    render(<Alert tone="info" title="Heads up" />);
    expect(screen.queryByRole("button", { name: "Dismiss" })).not.toBeInTheDocument();
  });
});
