import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { authApi } from "../../shared/auth/authApi";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Input } from "../../shared/design-system/Input";
import { PasswordInput } from "../../shared/design-system/PasswordInput";
import { applyApiErrorToForm } from "../../shared/forms/applyApiErrorToForm";
import { registerSchema, type RegisterFormValues } from "./schemas";

export function RegisterPage() {
  const { t } = useTranslation(["auth", "common"]);
  const [formError, setFormError] = useState<string | null>(null);
  const [registeredEmail, setRegisteredEmail] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) });

  const mutation = useMutation({
    mutationFn: (values: RegisterFormValues) => authApi.register(values.email, values.password),
    onSuccess: (result) => setRegisteredEmail(result.email),
    onError: (error: unknown) => {
      setFormError(applyApiErrorToForm(error, setError, t, ["email", "password"]));
    },
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    mutation.mutate(values);
  });

  if (registeredEmail) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <Card className="w-full max-w-sm text-center">
          <h1 className="text-lg font-semibold text-text-primary">{t("auth:verifyEmail.title")}</h1>
          <p className="mt-2 text-sm text-text-secondary">{t("auth:verifyEmail.pending")}</p>
          <Link to="/login" className="mt-4 inline-block text-sm font-medium text-primary hover:underline">
            {t("auth:login.title")}
          </Link>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-sm">
        <h1 className="text-lg font-semibold text-text-primary">{t("auth:register.title")}</h1>
        <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
          {formError && <Alert tone="danger" title={formError} />}
          <Input
            label={t("auth:fields.email")}
            type="email"
            autoComplete="email"
            error={errors.email ? t(errors.email.message ?? "") : undefined}
            {...register("email")}
          />
          <PasswordInput
            label={t("auth:fields.password")}
            autoComplete="new-password"
            toggleVisibilityLabel={t("auth:fields.togglePasswordVisibility")}
            hint={t("auth:fields.passwordRequirements")}
            error={errors.password ? t(errors.password.message ?? "") : undefined}
            {...register("password")}
          />
          <Button type="submit" variant="primary" disabled={isSubmitting || mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("auth:register.submit")}
          </Button>
        </form>
        <p className="mt-4 text-sm text-text-secondary">
          {t("auth:register.hasAccount")}{" "}
          <Link to="/login" className="font-medium text-primary hover:underline">
            {t("auth:register.loginLink")}
          </Link>
        </p>
      </Card>
    </div>
  );
}
