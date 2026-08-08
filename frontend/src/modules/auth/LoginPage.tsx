import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { authApi } from "../../shared/auth/authApi";
import { useAuthStore } from "../../shared/auth/authStore";
import { sanitizeRedirectTarget } from "../../shared/auth/redirect";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Input } from "../../shared/design-system/Input";
import { PasswordInput } from "../../shared/design-system/PasswordInput";
import { applyApiErrorToForm } from "../../shared/forms/applyApiErrorToForm";
import { loginSchema, type LoginFormValues } from "./schemas";

export function LoginPage() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const location = useLocation();
  const setSession = useAuthStore((state) => state.setSession);
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) });

  const mutation = useMutation({
    mutationFn: (values: LoginFormValues) => authApi.login(values.email, values.password),
    onSuccess: (tokenPair) => {
      const destination = sanitizeRedirectTarget((location.state as { from?: string } | null)?.from);
      setSession(tokenPair);
      navigate(destination, { replace: true });
    },
    onError: (error: unknown) => {
      setFormError(applyApiErrorToForm(error, setError, t, ["email", "password"]));
    },
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    mutation.mutate(values);
  });

  return (
    <div className="flex min-h-screen items-center justify-center p-6">
      <Card className="w-full max-w-sm">
        <h1 className="text-lg font-semibold text-text-primary">{t("auth:login.title")}</h1>
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
            autoComplete="current-password"
            toggleVisibilityLabel={t("auth:fields.togglePasswordVisibility")}
            error={errors.password ? t(errors.password.message ?? "") : undefined}
            {...register("password")}
          />
          <Button type="submit" variant="primary" disabled={isSubmitting || mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("auth:login.submit")}
          </Button>
        </form>
        <p className="mt-4 text-sm text-text-secondary">
          {t("auth:login.noAccount")}{" "}
          <Link to="/register" className="font-medium text-primary hover:underline">
            {t("auth:login.registerLink")}
          </Link>
        </p>
        <p className="mt-2 text-sm">
          <Link to="/password-reset/request" className="font-medium text-primary hover:underline">
            {t("auth:passwordReset.requestTitle")}
          </Link>
        </p>
      </Card>
    </div>
  );
}
