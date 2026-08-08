import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { PasswordInput } from "./PasswordInput";

describe("PasswordInput", () => {
  it("masks the value by default", () => {
    render(<PasswordInput label="Password" toggleVisibilityLabel="Show password" />);
    expect(screen.getByLabelText("Password")).toHaveAttribute("type", "password");
  });

  it("reveals the value when the toggle is activated, and hides it again on a second press", async () => {
    const user = userEvent.setup();
    render(<PasswordInput label="Password" toggleVisibilityLabel="Show password" />);

    const toggle = screen.getByRole("button", { name: "Show password" });
    await user.click(toggle);
    expect(screen.getByLabelText("Password")).toHaveAttribute("type", "text");
    expect(toggle).toHaveAttribute("aria-pressed", "true");

    await user.click(toggle);
    expect(screen.getByLabelText("Password")).toHaveAttribute("type", "password");
    expect(toggle).toHaveAttribute("aria-pressed", "false");
  });

  it("marks the field invalid and links the error message for assistive tech", () => {
    render(<PasswordInput label="Password" toggleVisibilityLabel="Show password" error="Too short." />);
    const input = screen.getByLabelText("Password");

    expect(input).toHaveAttribute("aria-invalid", "true");
    const errorMessage = screen.getByRole("alert");
    expect(errorMessage).toHaveTextContent("Too short.");
    expect(input.getAttribute("aria-describedby")).toContain(errorMessage.id);
  });
});
