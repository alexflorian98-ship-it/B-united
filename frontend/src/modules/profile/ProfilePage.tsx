import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import i18n from "../../shared/i18n/i18n";
import { ApiError } from "../../shared/api/apiError";
import { authApi } from "../../shared/auth/authApi";
import { useAuthStore } from "../../shared/auth/authStore";
import { tokenStorage } from "../../shared/auth/tokenStorage";
import { Alert } from "../../shared/design-system/Alert";
import { Button } from "../../shared/design-system/Button";
import { Card } from "../../shared/design-system/Card";
import { Input } from "../../shared/design-system/Input";
import { Modal } from "../../shared/design-system/Modal";
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

  const exportMutation = useMutation({
    mutationFn: () => profileApi.exportData(),
    onSuccess: (data) => {
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `bunited-data-export-${new Date().toISOString().slice(0, 10)}.json`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    },
  });

  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [deletePassword, setDeletePassword] = useState("");
  const deleteMutation = useMutation({
    mutationFn: (currentPassword: string) => profileApi.deleteAccount(currentPassword),
    onSuccess: () => {
      clearSession();
      navigate("/login", { replace: true });
    },
  });
  const deleteErrorMessage =
    ApiError.isApiError(deleteMutation.error) && deleteMutation.error.code === "ACCOUNT_DELETION_PASSWORD_INVALID"
      ? t("profile:dataRights.deleteModal.errorInvalidPassword")
      : deleteMutation.isError
        ? t("common:errors.internalServerError")
        : undefined;

  const closeDeleteModal = () => {
    setDeleteModalOpen(false);
    setDeletePassword("");
    deleteMutation.reset();
  };

  const onConfirmDelete = () => {
    deleteMutation.mutate(deletePassword);
  };

  if (profileQuery.isLoading) {
    return (
      <div className="flex flex-col gap-3">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-10 w-full max-w-sm" />
        <Skeleton className="h-10 w-full max-w-sm" />
      </div>
    );
  }

  if (profileQuery.isError) {
    return <Alert tone="danger" title={t("common:errors.internalServerError")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("profile:title")}</h1>

      <Card className="max-w-lg">
        <div className="flex items-center gap-4">
          <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-full bg-primary font-serif text-xl font-semibold text-on-primary">
            {(profileQuery.data?.email ?? "?").charAt(0).toUpperCase()}
          </span>
          <Input label={t("profile:email")} value={profileQuery.data?.email ?? ""} disabled readOnly className="flex-1" />
        </div>
        <form onSubmit={onSubmit} noValidate className="mt-5 flex flex-col gap-4">
          {saved && !formState.isDirty && <Alert tone="success" title={t("profile:saved")} />}
          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("profile:timezone")}</span>
            <select
              {...register("timezone")}
              className="rounded-lg border border-border-default bg-surface px-3 py-2 text-sm text-text-primary"
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
              className="rounded-lg border border-border-default bg-surface px-3 py-2 text-sm text-text-primary"
            >
              <option value="ro">{t("common:language.ro")}</option>
              <option value="en">{t("common:language.en")}</option>
            </select>
          </label>
          <label className="flex min-h-11 items-center gap-2 text-sm text-text-primary">
            <input type="checkbox" className="h-4 w-4 accent-primary" {...register("emailNotificationsEnabled")} />
            {t("profile:emailNotifications")}
          </label>
          <Button type="submit" variant="primary" className="self-start" disabled={mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
          </Button>
        </form>
      </Card>

      <Card className="max-w-lg">
        <Button
          type="button"
          variant="secondary"
          onClick={() => logoutMutation.mutate()}
          disabled={logoutMutation.isPending}
        >
          {t("profile:logout")}
        </Button>
      </Card>

      <Card className="max-w-lg">
        <h2 className="text-lg font-semibold text-text-primary">{t("profile:dataRights.title")}</h2>

        <div className="mt-4 flex flex-col gap-2">
          <h3 className="text-sm font-medium text-text-primary">{t("profile:dataRights.exportTitle")}</h3>
          <p className="text-sm text-text-muted">{t("profile:dataRights.exportDescription")}</p>
          {exportMutation.isError && <Alert tone="danger" title={t("profile:dataRights.exportError")} />}
          <Button
            type="button"
            variant="secondary"
            className="self-start"
            onClick={() => exportMutation.mutate()}
            disabled={exportMutation.isPending}
          >
            {exportMutation.isPending ? t("profile:dataRights.exportPending") : t("profile:dataRights.exportButton")}
          </Button>
        </div>

        <div className="mt-6 flex flex-col gap-2 border-t border-border-default pt-6">
          <h3 className="text-sm font-medium text-danger">{t("profile:dataRights.deleteTitle")}</h3>
          <p className="text-sm text-text-muted">{t("profile:dataRights.deleteDescription")}</p>
          <Button type="button" variant="danger" className="self-start" onClick={() => setDeleteModalOpen(true)}>
            {t("profile:dataRights.deleteButton")}
          </Button>
        </div>
      </Card>

      <Modal open={deleteModalOpen} onClose={closeDeleteModal} title={t("profile:dataRights.deleteModal.title")}>
        <div className="flex w-full max-w-sm flex-col gap-4">
          <Alert tone="danger" title={t("profile:dataRights.deleteModal.warning")} />
          <Input
            type="password"
            label={t("profile:dataRights.deleteModal.passwordLabel")}
            value={deletePassword}
            onChange={(event) => setDeletePassword(event.target.value)}
            error={deleteErrorMessage}
            autoComplete="current-password"
          />
          <div className="flex gap-3">
            <Button
              type="button"
              variant="danger"
              onClick={onConfirmDelete}
              disabled={deleteMutation.isPending || deletePassword.length === 0}
            >
              {t("profile:dataRights.deleteModal.confirm")}
            </Button>
            <Button type="button" variant="secondary" onClick={closeDeleteModal} disabled={deleteMutation.isPending}>
              {t("profile:dataRights.deleteModal.cancel")}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
