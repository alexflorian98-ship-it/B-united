import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { authApi } from "../../shared/auth/authApi";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { PasswordInput } from "../../shared/design-system/PasswordInput";
import { applyApiErrorToForm } from "../../shared/forms/applyApiErrorToForm";
import { confirmPasswordResetSchema, type ConfirmPasswordResetFormValues } from "./schemas";

export function ConfirmPasswordResetPage() {
  const { t } = useTranslation(["auth", "common"]);
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ConfirmPasswordResetFormValues>({ resolver: zodResolver(confirmPasswordResetSchema) });

  const mutation = useMutation({
    mutationFn: (values: ConfirmPasswordResetFormValues) => authApi.confirmPasswordReset(token ?? "", values.newPassword),
    onError: (error: unknown) => {
      setFormError(applyApiErrorToForm(error, setError, t, ["newPassword"]));
    },
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    mutation.mutate(values);
  });

  if (!token || (mutation.isError && !mutation.isPending)) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <Card className="w-full max-w-sm">
          <h1 className="text-lg font-semibold text-text-primary">{t("auth:passwordReset.confirmTitle")}</h1>
          <div className="mt-3">
            <Alert tone="danger" title={t("auth:errors.passwordResetTokenInvalid")} />
          </div>
          <Link to="/password-reset/request" className="mt-4 inline-block text-sm font-medium text-primary hover:underline">
            {t("auth:passwordReset.requestTitle")}
          </Link>
        </Card>
      </div>
    );
  }

  if (mutation.isSuccess) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <Card className="w-full max-w-sm text-center">
          <h1 className="text-lg font-semibold text-text-primary">{t("auth:passwordReset.confirmTitle")}</h1>
          <p className="mt-2 text-sm text-success">{t("auth:passwordReset.confirmSuccess")}</p>
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
        <h1 className="text-lg font-semibold text-text-primary">{t("auth:passwordReset.confirmTitle")}</h1>
        <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
          {formError && <Alert tone="danger" title={formError} />}
          <PasswordInput
            label={t("auth:fields.newPassword")}
            autoComplete="new-password"
            toggleVisibilityLabel={t("auth:fields.togglePasswordVisibility")}
            hint={t("auth:fields.passwordRequirements")}
            error={errors.newPassword ? t(errors.newPassword.message ?? "") : undefined}
            {...register("newPassword")}
          />
          <Button type="submit" variant="primary" disabled={isSubmitting || mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("auth:passwordReset.confirmSubmit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
