import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import i18n from "../../shared/i18n/i18n";
import { authApi } from "../../shared/auth/authApi";
import { useAuthStore } from "../../shared/auth/authStore";
import { tokenStorage } from "../../shared/auth/tokenStorage";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Input } from "../../shared/design-system/Input";
import { Skeleton } from "../../shared/design-system/Skeleton";
import { profileApi, type Profile } from "./profileApi";

const TIMEZONE_OPTIONS = [
  "Europe/Bucharest",
  "Europe/London",
  "Europe/Berlin",
  "Europe/Paris",
  "America/New_York",
  "America/Los_Angeles",
  "Asia/Dubai",
  "UTC",
];

const profileFormSchema = z.object({
  timezone: z.string().min(1),
  preferredLanguage: z.enum(["ro", "en"]),
  emailNotificationsEnabled: z.boolean(),
});
type ProfileFormValues = z.infer<typeof profileFormSchema>;

export function ProfilePage() {
  const { t } = useTranslation(["profile", "common"]);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const clearSession = useAuthStore((state) => state.clearSession);
  const [saved, setSaved] = useState(false);

  const profileQuery = useQuery({ queryKey: ["profile"], queryFn: profileApi.get });

  const { register, handleSubmit, formState } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    values: profileQuery.data
      ? {
          timezone: profileQuery.data.timezone,
          preferredLanguage: profileQuery.data.preferredLanguage === "en" ? "en" : "ro",
          emailNotificationsEnabled: profileQuery.data.emailNotificationsEnabled,
        }
      : undefined,
  });

  const mutation = useMutation({
    mutationFn: (values: ProfileFormValues) => profileApi.update(values),
    onSuccess: (profile: Profile) => {
      queryClient.setQueryData(["profile"], profile);
      void i18n.changeLanguage(profile.preferredLanguage);
      setSaved(true);
    },
  });

  const onSubmit = handleSubmit((values) => {
    setSaved(false);
    mutation.mutate(values);
  });

  const logoutMutation = useMutation({
    mutationFn: async () => {
      const refreshToken = tokenStorage.getRefreshToken();
      if (refreshToken) {
        // Best-effort server-side revoke — logout must still succeed locally even if this
        // fails (offline, token already expired), see the finally-style clearSession below.
        await authApi.revoke(refreshToken).catch(() => undefined);
      }
    },
    onSettled: () => {
      clearSession();
      navigate("/login", { replace: true });
    },
  });

  if (profileQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3 p-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-full max-w-sm" />
        <Skeleton className="h-10 w-full max-w-sm" />
      </div>
    );
  }

  if (profileQuery.isError) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.internalServerError")} />
      </div>
    );
  }

  return (
    <div className="p-4">
      <Card className="max-w-sm">
        <h1 className="text-lg font-semibold text-text-primary">{t("profile:title")}</h1>
        <div className="mt-4">
          <Input label={t("profile:email")} value={profileQuery.data?.email ?? ""} disabled readOnly />
        </div>
        <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
          {saved && !formState.isDirty && <Alert tone="success" title={t("profile:saved")} />}
          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("profile:timezone")}</span>
            <select
              {...register("timezone")}
              className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
            >
              {TIMEZONE_OPTIONS.map((zone) => (
                <option key={zone} value={zone}>
                  {zone}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("profile:language")}</span>
            <select
              {...register("preferredLanguage")}
              className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
            >
              <option value="ro">{t("common:language.ro")}</option>
              <option value="en">{t("common:language.en")}</option>
            </select>
          </label>
          <label className="flex min-h-11 items-center gap-2 text-sm text-text-primary">
            <input type="checkbox" className="h-4 w-4" {...register("emailNotificationsEnabled")} />
            {t("profile:emailNotifications")}
          </label>
          <Button type="submit" variant="primary" disabled={mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
          </Button>
        </form>
      </Card>

      <Card className="mt-4 max-w-sm">
        <Button
          type="button"
          variant="secondary"
          onClick={() => logoutMutation.mutate()}
          disabled={logoutMutation.isPending}
        >
          {t("profile:logout")}
        </Button>
      </Card>
    </div>
  );
}
