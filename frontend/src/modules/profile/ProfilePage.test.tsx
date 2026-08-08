import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { authApi } from "../../shared/auth/authApi";
import { useAuthStore } from "../../shared/auth/authStore";
import { tokenStorage } from "../../shared/auth/tokenStorage";
import { ProfilePage } from "./ProfilePage";
import { profileApi } from "./profileApi";

vi.mock("./profileApi");
vi.mock("../../shared/auth/authApi");

function renderProfilePage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/profile"]}>
        <Routes>
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/login" element={<div>Login page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const baseProfile = {
  userId: "user-1",
  email: "ada@example.com",
  timezone: "Europe/Bucharest",
  preferredLanguage: "ro" as const,
  emailNotificationsEnabled: true,
};

describe("ProfilePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ status: "authenticated", accessToken: "token", user: { id: "user-1", email: "ada@example.com", permissions: [] } });
    vi.mocked(profileApi.get).mockResolvedValue(baseProfile);
  });

  it("loads and displays the current profile", async () => {
    renderProfilePage();

    expect(await screen.findByDisplayValue("ada@example.com")).toBeInTheDocument();
  });

  it("saves changes and shows a confirmation", async () => {
    vi.mocked(profileApi.update).mockResolvedValue({ ...baseProfile, timezone: "UTC", preferredLanguage: "en" });
    const user = userEvent.setup();
    renderProfilePage();

    await screen.findByDisplayValue("ada@example.com");
    await user.selectOptions(screen.getByLabelText("Timezone"), "UTC");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Your profile has been saved.")).toBeInTheDocument();
    expect(profileApi.update).toHaveBeenCalledWith(
      expect.objectContaining({ timezone: "UTC", preferredLanguage: "ro", emailNotificationsEnabled: true }),
    );
  });

  it("logs out, clears the session, and redirects to /login", async () => {
    const setRefreshToken = vi.spyOn(tokenStorage, "getRefreshToken").mockReturnValue("refresh-token");
    vi.mocked(authApi.revoke).mockResolvedValue(undefined);
    const user = userEvent.setup();
    renderProfilePage();

    await screen.findByDisplayValue("ada@example.com");
    await user.click(screen.getByRole("button", { name: "Log out" }));

    expect(await screen.findByText("Login page")).toBeInTheDocument();
    expect(authApi.revoke).toHaveBeenCalledWith("refresh-token");
    expect(useAuthStore.getState().status).toBe("unauthenticated");

    setRefreshToken.mockRestore();
  });
});
