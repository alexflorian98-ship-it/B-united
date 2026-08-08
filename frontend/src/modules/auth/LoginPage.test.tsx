import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../../shared/api/apiError";
import { authApi } from "../../shared/auth/authApi";
import { useAuthStore } from "../../shared/auth/authStore";
import { LoginPage } from "./LoginPage";

vi.mock("../../shared/auth/authApi");

function renderLoginPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/login"]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div>Home page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("LoginPage", () => {
  beforeEach(() => {
    useAuthStore.setState({ status: "unauthenticated", accessToken: null, user: null });
    vi.clearAllMocks();
  });

  it("shows field validation errors instead of submitting an empty form", async () => {
    const user = userEvent.setup();
    renderLoginPage();

    await user.click(screen.getByRole("button", { name: "Log in" }));

    expect(await screen.findByText("Email is required.")).toBeInTheDocument();
    expect(authApi.login).not.toHaveBeenCalled();
  });

  it("logs in and navigates to the intended destination on success", async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      accessToken: "access-token",
      accessTokenExpiresAtUtc: new Date().toISOString(),
      refreshToken: "refresh-token",
      refreshTokenExpiresAtUtc: new Date().toISOString(),
    });

    const user = userEvent.setup();
    renderLoginPage();

    await user.type(screen.getByLabelText("Email"), "ada@example.com");
    await user.type(screen.getByLabelText("Password"), "StrongPass123");
    await user.click(screen.getByRole("button", { name: "Log in" }));

    await waitFor(() => expect(screen.getByText("Home page")).toBeInTheDocument());
    expect(useAuthStore.getState().status).toBe("authenticated");
  });

  it("shows the server error without revealing which field was wrong", async () => {
    vi.mocked(authApi.login).mockRejectedValue(
      new ApiError(400, "INVALID_CREDENTIALS", "errors.invalidCredentials", "corr-1"),
    );

    const user = userEvent.setup();
    renderLoginPage();

    await user.type(screen.getByLabelText("Email"), "ada@example.com");
    await user.type(screen.getByLabelText("Password"), "WrongPassword");
    await user.click(screen.getByRole("button", { name: "Log in" }));

    expect(await screen.findByText("The email or password is incorrect.")).toBeInTheDocument();
    expect(useAuthStore.getState().status).toBe("unauthenticated");
  });
});
