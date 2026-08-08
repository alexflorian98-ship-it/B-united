import { QueryClient } from "@tanstack/react-query";
import { ApiError } from "./apiError";

/**
 * Shared TanStack Query defaults. Does not retry 4xx `ApiError`s (they're expected/business
 * errors — retrying "wrong password" or "not found" wastes a round trip and delays the error
 * the user actually needs to see); retries other failures (network blips, 5xx) a couple of times.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        if (ApiError.isApiError(error) && error.status < 500) {
          return false;
        }
        return failureCount < 2;
      },
    },
    mutations: {
      retry: false,
    },
  },
});
