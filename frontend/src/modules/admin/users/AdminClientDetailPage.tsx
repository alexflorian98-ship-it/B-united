import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { ApiError } from "../../../shared/api/apiError";
import { Alert } from "../../../shared/design-system/Alert";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { resolveMessageKey } from "../../../shared/forms/applyApiErrorToForm";
import { adminUsersApi } from "./adminUsersApi";

function statusToneForPurchase(status: string): "success" | "danger" | "warning" | "neutral" {
  if (status === "Succeeded") return "success";
  if (status === "Failed" || status === "Chargeback") return "danger";
  if (status === "Refunded") return "warning";
  return "neutral";
}

export function AdminClientDetailPage() {
  const { t } = useTranslation(["admin", "common"]);
  const { userId = "" } = useParams<{ userId: string }>();
  const client = useQueryClient();
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [selectedRoleToAssign, setSelectedRoleToAssign] = useState("");

  const detailQuery = useQuery({ queryKey: ["admin-client", userId], queryFn: () => adminUsersApi.getClient(userId) });
  const commerceQuery = useQuery({ queryKey: ["admin-client-commerce", userId], queryFn: () => adminUsersApi.getCommerceSummary(userId) });
  const rolesQuery = useQuery({ queryKey: ["admin-roles"], queryFn: adminUsersApi.listRoles });

  const invalidateClient = () => {
    client.invalidateQueries({ queryKey: ["admin-client", userId] });
    client.invalidateQueries({ queryKey: ["admin-clients"] });
  };

  const handleMutationError = (error: unknown) => {
    if (ApiError.isApiError(error)) {
      setMutationError(t(resolveMessageKey(error.messageKey)));
    } else {
      setMutationError(t("common:errors.internalServerError"));
    }
  };

  const assignRole = useMutation({
    mutationFn: (roleId: string) => adminUsersApi.assignRole(userId, roleId),
    onSuccess: () => {
      setMutationError(null);
      setSelectedRoleToAssign("");
      invalidateClient();
    },
    onError: handleMutationError,
  });

  const removeRole = useMutation({
    mutationFn: (roleId: string) => adminUsersApi.removeRole(userId, roleId),
    onSuccess: () => {
      setMutationError(null);
      invalidateClient();
    },
    onError: handleMutationError,
  });

  if (detailQuery.isLoading) return <Skeleton className="h-64 w-full" />;
  if (detailQuery.isError || !detailQuery.data) return <Alert tone="danger" title={t("common:errors.notFound")} />;

  const clientDetail = detailQuery.data;
  const assignedRoleIds = new Set(clientDetail.roles.map((role) => role.id));
  const assignableRoles = rolesQuery.data?.filter((role) => !assignedRoleIds.has(role.id)) ?? [];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{clientDetail.email}</h1>
        <p className="text-sm text-text-muted">{t("admin:clients.detail.joined", { date: new Date(clientDetail.createdAt).toLocaleDateString() })}</p>
      </div>

      {mutationError && <Alert tone="danger" title={mutationError} />}

      <Card className="flex flex-wrap items-center gap-3">
        <Badge tone={clientDetail.isActive ? "success" : "neutral"}>
          {clientDetail.isActive ? t("admin:clients.status.active") : t("admin:clients.status.inactive")}
        </Badge>
        <Badge tone={clientDetail.isEmailVerified ? "success" : "warning"}>
          {clientDetail.isEmailVerified ? t("admin:clients.detail.emailVerified") : t("admin:clients.detail.emailNotVerified")}
        </Badge>
      </Card>

      <section>
        <h2 className="text-lg font-semibold text-text-primary">{t("admin:clients.detail.roles")}</h2>
        <div className="mt-3 flex flex-wrap gap-2">
          {clientDetail.roles.length === 0 && <span className="text-sm text-text-muted">{t("admin:clients.detail.noRoles")}</span>}
          {clientDetail.roles.map((role) => (
            <span key={role.id} className="flex items-center gap-2 rounded-full border border-border-default bg-surface px-3 py-1.5 text-sm">
              {role.name}
              <button
                type="button"
                onClick={() => removeRole.mutate(role.id)}
                disabled={removeRole.isPending}
                aria-label={t("admin:clients.detail.removeRole", { role: role.name })}
                className="min-h-6 min-w-6 rounded-full text-text-muted hover:text-danger"
              >
                ×
              </button>
            </span>
          ))}
        </div>

        {assignableRoles.length > 0 && (
          <div className="mt-4 flex flex-wrap items-end gap-2">
            <label className="flex flex-col gap-1">
              <span className="text-sm font-medium text-text-primary">{t("admin:clients.detail.assignRole")}</span>
              <select
                value={selectedRoleToAssign}
                onChange={(event) => setSelectedRoleToAssign(event.target.value)}
                className="min-h-11 rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
              >
                <option value="">{t("admin:clients.detail.selectRole")}</option>
                {assignableRoles.map((role) => (
                  <option key={role.id} value={role.id}>
                    {role.name}
                  </option>
                ))}
              </select>
            </label>
            <Button
              variant="secondary"
              disabled={!selectedRoleToAssign || assignRole.isPending}
              onClick={() => assignRole.mutate(selectedRoleToAssign)}
            >
              {t("admin:clients.detail.assign")}
            </Button>
          </div>
        )}
      </section>

      <section>
        <h2 className="text-lg font-semibold text-text-primary">{t("admin:clients.detail.purchases")}</h2>
        {commerceQuery.isLoading && <Skeleton className="mt-3 h-32 w-full" />}
        {commerceQuery.isSuccess && commerceQuery.data.purchases.length === 0 && (
          <EmptyState title={t("admin:clients.detail.noPurchases")} />
        )}
        {commerceQuery.isSuccess && commerceQuery.data.purchases.length > 0 && (
          <div className="mt-3 flex flex-col gap-2">
            {commerceQuery.data.purchases.map((purchase) => (
              <Card key={purchase.purchaseId} className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-medium text-text-primary">{purchase.programTitleSnapshot ?? purchase.programSlug ?? purchase.programId}</span>
                <span>{purchase.amount.toFixed(2)} {purchase.currency}</span>
                <Badge tone={statusToneForPurchase(purchase.status)}>{purchase.status}</Badge>
                <span className="text-sm text-text-muted">{new Date(purchase.createdAt).toLocaleDateString()}</span>
              </Card>
            ))}
          </div>
        )}
      </section>

      <section>
        <h2 className="text-lg font-semibold text-text-primary">{t("admin:clients.detail.entitlements")}</h2>
        {commerceQuery.isSuccess && commerceQuery.data.entitlements.length === 0 && (
          <EmptyState title={t("admin:clients.detail.noEntitlements")} />
        )}
        {commerceQuery.isSuccess && commerceQuery.data.entitlements.length > 0 && (
          <div className="mt-3 flex flex-col gap-2">
            {commerceQuery.data.entitlements.map((entitlement) => (
              <Card key={entitlement.programId} className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-medium text-text-primary">{entitlement.programSlug ?? entitlement.programId}</span>
                <Badge tone={entitlement.status === "Active" ? "success" : "neutral"}>{entitlement.status}</Badge>
                <span className="text-sm text-text-muted">{new Date(entitlement.grantedAtUtc).toLocaleDateString()}</span>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
