import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../../shared/api/apiError";
import { authApi } from "../../shared/auth/authApi";
import { RegisterPage } from "./RegisterPage";

vi.mock("../../shared/auth/authApi");

function renderRegisterPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/register"]}>
        <RegisterPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("RegisterPage", () => {
  beforeEach(() => vi.clearAllMocks());

  it("rejects a weak password before ever calling the API", async () => {
    const user = userEvent.setup();
    renderRegisterPage();

    await user.type(screen.getByLabelText("Email"), "ada@example.com");
    await user.type(screen.getByLabelText("Password"), "weak");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByText("Password must be at least 10 characters.")).toBeInTheDocument();
    expect(authApi.register).not.toHaveBeenCalled();
  });

  it("shows the verification-pending screen on success, without logging the user in", async () => {
    vi.mocked(authApi.register).mockResolvedValue({ userId: "user-1", email: "ada@example.com" });

    const user = userEvent.setup();
    renderRegisterPage();

    await user.type(screen.getByLabelText("Email"), "ada@example.com");
    await user.type(screen.getByLabelText("Password"), "StrongPass123");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByText("Check your inbox for a verification link.")).toBeInTheDocument();
  });

  it("shows a duplicate-email error against the email field", async () => {
    vi.mocked(authApi.register).mockRejectedValue(
      new ApiError(400, "VALIDATION_FAILED", "errors.validationFailed", "corr-1", [
        { field: "Email", messageKey: "errors.email.alreadyRegistered" },
      ]),
    );

    const user = userEvent.setup();
    renderRegisterPage();

    await user.type(screen.getByLabelText("Email"), "taken@example.com");
    await user.type(screen.getByLabelText("Password"), "StrongPass123");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByText("An account with this email already exists.")).toBeInTheDocument();
  });
});
