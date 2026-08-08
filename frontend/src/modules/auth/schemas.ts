import { z } from "zod";

/** Every message is a localization key (`t()`'d at render time), mirroring the backend's own
 * `errors.*` keys (`PasswordRules`/`RegisterUserValidator`) so client and server never drift. */
export const emailSchema = z.string().trim().min(1, "errors.email.required").email("errors.email.invalid");

export const passwordStrengthSchema = z
  .string()
  .min(1, "errors.password.required")
  .min(10, "errors.password.tooShort")
  .regex(/[A-Z]/, "errors.password.requiresUppercase")
  .regex(/[a-z]/, "errors.password.requiresLowercase")
  .regex(/[0-9]/, "errors.password.requiresDigit");

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, "errors.password.required"),
});
export type LoginFormValues = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  email: emailSchema,
  password: passwordStrengthSchema,
});
export type RegisterFormValues = z.infer<typeof registerSchema>;

export const requestPasswordResetSchema = z.object({
  email: emailSchema,
});
export type RequestPasswordResetFormValues = z.infer<typeof requestPasswordResetSchema>;

export const confirmPasswordResetSchema = z.object({
  newPassword: passwordStrengthSchema,
});
export type ConfirmPasswordResetFormValues = z.infer<typeof confirmPasswordResetSchema>;
