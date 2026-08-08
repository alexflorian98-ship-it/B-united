import { useEffect, useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link, useSearchParams } from "react-router-dom";
import { authApi } from "../../shared/auth/authApi";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Input } from "../../shared/design-system/Input";

export function VerifyEmailPage() {
  const { t } = useTranslation(["auth", "common"]);
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");
  const attempted = useRef(false);

  const verifyMutation = useMutation({
    mutationFn: (rawToken: string) => authApi.verifyEmail(rawToken),
  });

  useEffect(() => {
    if (token && !attempted.current) {
      attempted.current = true;
      verifyMutation.mutate(token);
    }
    // Runs once per mounted token — verifyMutation is stable enough across renders that
    // including it would just cause an eslint-satisfying no-op re-run, not a real dependency.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  const [resendEmail, setResendEmail] = useState("");
  const resendMutation = useMutation({
    mutationFn: (email: string) => authApi.resendVerification(email),
  });

  if (!token || verifyMutation.isError) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <Card className="w-full max-w-sm">
          <h1 className="text-lg font-semibold text-text-primary">{t("auth:verifyEmail.title")}</h1>
          <div className="mt-3">
            <Alert tone="danger" title={t("auth:errors.emailVerificationTokenInvalid")} />
          </div>
          {resendMutation.isSuccess ? (
            <p className="mt-4 text-sm text-text-secondary">{t("auth:verifyEmail.resendSent")}</p>
          ) : (
            <form
              className="mt-4 flex flex-col gap-3"
              onSubmit={(event) => {
                event.preventDefault();
                resendMutation.mutate(resendEmail);
              }}
              noValidate
            >
              <p className="text-sm text-text-secondary">{t("auth:verifyEmail.resendGuidance")}</p>
              <Input
                label={t("auth:fields.email")}
                type="email"
                autoComplete="email"
                value={resendEmail}
                onChange={(event) => setResendEmail(event.target.value)}
                required
              />
              <Button type="submit" variant="secondary" disabled={resendMutation.isPending}>
                {resendMutation.isPending ? t("common:status.saving") : t("auth:verifyEmail.resend")}
              </Button>
            </form>
          )}
          <Link to="/login" className="mt-4 inline-block text-sm font-medium text-primary hover:underline">
            {t("auth:login.title")}
          </Link>
        </Card>
      </div>
    );
  }

  if (verifyMutation.isSuccess) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <Card className="w-full max-w-sm text-center">
          <h1 className="text-lg font-semibold text-text-primary">{t("auth:verifyEmail.title")}</h1>
          <p className="mt-2 text-sm text-success">{t("auth:verifyEmail.success")}</p>
          <Link to="/login" className="mt-4 inline-block text-sm font-medium text-primary hover:underline">
            {t("auth:login.title")}
          </Link>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-6" role="status" aria-live="polite">
      <p className="text-sm text-text-muted">{t("common:status.loading")}</p>
    </div>
  );
}
