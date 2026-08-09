import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { EmptyState } from "../../../shared/design-system/EmptyState";
import { Input } from "../../../shared/design-system/Input";
import { Skeleton } from "../../../shared/design-system/Skeleton";
import { adminUsersApi } from "./adminUsersApi";

const PAGE_SIZE = 20;

export function AdminClientListPage() {
  const { t } = useTranslation(["admin", "common"]);
  const [search, setSearch] = useState("");
  const [roleId, setRoleId] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const rolesQuery = useQuery({ queryKey: ["admin-roles"], queryFn: adminUsersApi.listRoles });
  const clientsQuery = useQuery({
    queryKey: ["admin-clients", search, roleId, page],
    queryFn: () => adminUsersApi.listClients(search || null, roleId, page, PAGE_SIZE),
  });

  const totalPages = clientsQuery.data ? Math.max(1, Math.ceil(clientsQuery.data.totalCount / PAGE_SIZE)) : 1;

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold text-text-primary tablet:text-3xl">{t("admin:clients.title")}</h1>

      <div className="flex flex-col gap-3 tablet:flex-row tablet:items-end">
        <Input
          label={t("admin:clients.search")}
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
          placeholder={t("admin:clients.searchPlaceholder")}
        />
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:clients.roleFilter")}</span>
          <select
            value={roleId ?? ""}
            onChange={(event) => {
              setRoleId(event.target.value || null);
              setPage(1);
            }}
            className="min-h-11 rounded-md border border-border-default px-3 py-2 text-sm text-text-primary"
          >
            <option value="">{t("admin:clients.allRoles")}</option>
            {rolesQuery.data?.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      {clientsQuery.isLoading && <Skeleton className="h-64 w-full" />}

      {clientsQuery.isError && <EmptyState title={t("admin:clients.loadError")} />}

      {clientsQuery.isSuccess && clientsQuery.data.items.length === 0 && (
        <EmptyState title={t("admin:clients.noClients")} description={t("admin:clients.noClientsDescription")} />
      )}

      {clientsQuery.isSuccess && clientsQuery.data.items.length > 0 && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border-default bg-surface shadow-sm">
            <table className="w-full text-left text-sm">
              <caption className="sr-only">{t("admin:clients.tableCaption")}</caption>
              <thead className="bg-background text-xs uppercase tracking-wide text-text-muted">
                <tr>
                  <th scope="col" className="px-3 py-2">{t("admin:clients.columns.email")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:clients.columns.roles")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:clients.columns.status")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:clients.columns.joined")}</th>
                  <th scope="col" className="px-3 py-2">{t("admin:clients.columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {clientsQuery.data.items.map((client) => (
                  <tr key={client.id} className="border-t border-border-default">
                    <td className="px-3 py-2 font-medium text-text-primary">{client.email}</td>
                    <td className="px-3 py-2">
                      <div className="flex flex-wrap gap-1">
                        {client.roles.length === 0 ? "—" : client.roles.map((role) => <Badge key={role.id} tone="neutral">{role.name}</Badge>)}
                      </div>
                    </td>
                    <td className="px-3 py-2">
                      <Badge tone={client.isActive ? "success" : "neutral"}>
                        {client.isActive ? t("admin:clients.status.active") : t("admin:clients.status.inactive")}
                      </Badge>
                    </td>
                    <td className="px-3 py-2 text-text-muted">{new Date(client.createdAt).toLocaleDateString()}</td>
                    <td className="px-3 py-2">
                      <Link to={`/admin/clients/${client.id}`}>
                        <Button variant="secondary">{t("admin:clients.view")}</Button>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between gap-3">
            <span className="text-sm text-text-muted">{t("admin:clients.pageOf", { page, totalPages })}</span>
            <div className="flex gap-2">
              <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>
                {t("admin:clients.previous")}
              </Button>
              <Button variant="secondary" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>
                {t("admin:clients.next")}
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
