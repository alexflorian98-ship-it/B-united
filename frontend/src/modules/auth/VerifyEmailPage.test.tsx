import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../../shared/api/apiError";
import { authApi } from "../../shared/auth/authApi";
import { VerifyEmailPage } from "./VerifyEmailPage";

vi.mock("../../shared/auth/authApi");

function renderAt(path: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <VerifyEmailPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("VerifyEmailPage", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows an invalid-link state (with resend guidance) when there's no token in the URL", async () => {
    renderAt("/verify-email");

    expect(await screen.findByText("This verification link is invalid or has expired.")).toBeInTheDocument();
    expect(screen.getByText("Enter your email and we'll send a new verification link.")).toBeInTheDocument();
    expect(authApi.verifyEmail).not.toHaveBeenCalled();
  });

  it("verifies automatically and shows success for a valid token", async () => {
    vi.mocked(authApi.verifyEmail).mockResolvedValue(undefined);

    renderAt("/verify-email?token=valid-token");

    expect(await screen.findByText("Your email has been verified.")).toBeInTheDocument();
    expect(authApi.verifyEmail).toHaveBeenCalledWith("valid-token");
  });

  it("shows the invalid/expired state for a rejected token, exactly once (no retry loop)", async () => {
    vi.mocked(authApi.verifyEmail).mockRejectedValue(
      new ApiError(400, "EMAIL_VERIFICATION_TOKEN_INVALID", "errors.emailVerificationTokenInvalid", "corr-1"),
    );

    renderAt("/verify-email?token=expired-token");

    expect(await screen.findByText("This verification link is invalid or has expired.")).toBeInTheDocument();
    expect(authApi.verifyEmail).toHaveBeenCalledTimes(1);
  });

  it("lets the user request a new verification email from the invalid-link state", async () => {
    vi.mocked(authApi.resendVerification).mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderAt("/verify-email");
    await screen.findByText("This verification link is invalid or has expired.");

    await user.type(screen.getByLabelText("Email"), "ada@example.com");
    await user.click(screen.getByRole("button", { name: "Resend verification email" }));

    expect(await screen.findByText("If an account exists for this email, a new verification link has been sent.")).toBeInTheDocument();
    expect(authApi.resendVerification).toHaveBeenCalledWith("ada@example.com");
  });
});
