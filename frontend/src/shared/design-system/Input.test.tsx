import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { Input } from "./Input";

describe("Input", () => {
  it("associates the visible label with the input via htmlFor/id", () => {
    render(<Input label="Email" />);
    expect(screen.getByLabelText("Email")).toBeInTheDocument();
  });

  it("is focusable via Tab and accepts keyboard input", async () => {
    const user = userEvent.setup();
    render(<Input label="Email" />);

    await user.tab();
    const input = screen.getByLabelText("Email");
    expect(input).toHaveFocus();

    await user.keyboard("ada@example.com");
    expect(input).toHaveValue("ada@example.com");
  });

  it("marks the field invalid and links the error message for assistive tech", () => {
    render(<Input label="Email" error="Email is required." />);
    const input = screen.getByLabelText("Email");

    expect(input).toHaveAttribute("aria-invalid", "true");
    const errorMessage = screen.getByRole("alert");
    expect(errorMessage).toHaveTextContent("Email is required.");
    expect(input.getAttribute("aria-describedby")).toContain(errorMessage.id);
  });
});
