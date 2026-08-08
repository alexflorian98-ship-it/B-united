import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusBadge } from "./StatusBadge";

describe("StatusBadge", () => {
  it("always renders a visible text label, not just a color", () => {
    render(<StatusBadge status="success" label="Verified" />);
    expect(screen.getByText("Verified")).toBeInTheDocument();
  });

  it("hides the decorative icon from assistive tech", () => {
    const { container } = render(<StatusBadge status="warning" label="Pending" />);
    expect(container.querySelector('[aria-hidden="true"]')).toBeInTheDocument();
  });
});
