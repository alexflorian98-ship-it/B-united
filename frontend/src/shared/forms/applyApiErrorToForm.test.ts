import { describe, expect, it, vi } from "vitest";
import { ApiError } from "../api/apiError";
import { applyApiErrorToForm } from "./applyApiErrorToForm";

interface FormValues {
  email: string;
}

describe("applyApiErrorToForm", () => {
  it("resolves generic backend error codes under the common namespace, not the caller's default namespace", () => {
    const t = vi.fn((key: string) => `translated:${key}`);
    const setError = vi.fn();
    const error = new ApiError(500, "INTERNAL_SERVER_ERROR", "errors.internalServerError", "corr-1");

    const message = applyApiErrorToForm<FormValues>(error, setError, t, ["email"]);

    expect(t).toHaveBeenCalledWith("common:errors.internalServerError");
    expect(message).toBe("translated:common:errors.internalServerError");
  });

  it("resolves a module-specific error code without a namespace prefix (relies on the caller's default namespace)", () => {
    const t = vi.fn((key: string) => `translated:${key}`);
    const setError = vi.fn();
    const error = new ApiError(400, "INVALID_CREDENTIALS", "errors.invalidCredentials", "corr-2");

    const message = applyApiErrorToForm<FormValues>(error, setError, t, ["email"]);

    expect(t).toHaveBeenCalledWith("errors.invalidCredentials");
    expect(message).toBe("translated:errors.invalidCredentials");
  });

  it("maps a field-level validation error onto the matching form field", () => {
    const t = vi.fn((key: string) => `translated:${key}`);
    const setError = vi.fn();
    const error = new ApiError(400, "VALIDATION_FAILED", "errors.validationFailed", "corr-3", [
      { field: "Email", messageKey: "errors.email.alreadyRegistered" },
    ]);

    applyApiErrorToForm<FormValues>(error, setError, t, ["email"]);

    expect(setError).toHaveBeenCalledWith("email", {
      type: "server",
      message: "translated:errors.email.alreadyRegistered",
    });
  });

  it("falls back to a generic message for a non-ApiError", () => {
    const t = vi.fn((key: string) => `translated:${key}`);
    const setError = vi.fn();

    const message = applyApiErrorToForm<FormValues>(new Error("boom"), setError, t, ["email"]);

    expect(message).toBe("translated:common:errors.internalServerError");
  });
});
