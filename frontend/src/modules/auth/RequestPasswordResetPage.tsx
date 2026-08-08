import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { authApi } from "../../shared/auth/authApi";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Input } from "../../shared/design-system/Input";
import { requestPasswordResetSchema, type RequestPasswordResetFormValues } from "./schemas";

export function RequestPasswordResetPage() {
  const { t } = useTranslation(["auth", "common"]);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RequestPasswordResetFormValues>({ resolver: zodResolver(requestPasswordResetSchema) });

  // The backend deliberately responds identically whether or not the account exists (§22.a) —
  // the UI must never branch on the result either, or it would leak exactly what it's hiding.
  const mutation = useMutation({
    mutationFn: (values: RequestPasswordResetFormValues) => authApi.requestPasswordReset(values.email),
  });

  const onSubmit = handleSubmit((values) => mutation.mutate(values));

  if (mutation.isSuccess) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <Card className="w-full max-w-sm text-center">
          <h1 className="text-lg font-semibold text-text-primary">{t("auth:passwordReset.requestTitle")}</h1>
          <p className="mt-2 text-sm text-text-secondary">{t("auth:passwordReset.requestSent")}</p>
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
        <h1 className="text-lg font-semibold text-text-primary">{t("auth:passwordReset.requestTitle")}</h1>
        <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
          <Input
            label={t("auth:fields.email")}
            type="email"
            autoComplete="email"
            error={errors.email ? t(errors.email.message ?? "") : undefined}
            {...register("email")}
          />
          <Button type="submit" variant="primary" disabled={isSubmitting || mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("auth:passwordReset.requestSubmit")}
          </Button>
        </form>
        <Link to="/login" className="mt-4 inline-block text-sm font-medium text-primary hover:underline">
          {t("auth:login.title")}
        </Link>
      </Card>
    </div>
  );
}
