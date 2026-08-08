import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { Alert } from "../../../shared/design-system/Alert";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { Input } from "../../../shared/design-system/Input";
import { applyApiErrorToForm } from "../../../shared/forms/applyApiErrorToForm";
import { contentApi } from "../../content/contentApi";
import { adminContentApi } from "./adminContentApi";

const schema = z.object({
  domainId: z.string().min(1),
  slug: z.string().min(1).regex(/^[a-z0-9]+(-[a-z0-9]+)*$/),
  defaultLanguage: z.enum(["ro", "en"]),
  title: z.string().min(1),
  shortDescription: z.string().min(1),
  description: z.string().min(1),
});
type FormValues = z.infer<typeof schema>;

export function AdminNewProgramPage() {
  const { t } = useTranslation(["admin", "content", "common"]);
  const navigate = useNavigate();
  const domainsQuery = useQuery({ queryKey: ["content-domains"], queryFn: contentApi.listDomains });

  const { register, handleSubmit, setError, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { defaultLanguage: "ro" },
  });
  const [formError, setFormError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (values: FormValues) => adminContentApi.createProgram(values),
    onSuccess: (programId) => navigate(`/admin/programs/${programId}`, { replace: true }),
    onError: (error: unknown) => setFormError(applyApiErrorToForm(error, setError, t, ["domainId", "slug", "defaultLanguage", "title", "shortDescription", "description"])),
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    mutation.mutate(values);
  });

  return (
    <div className="p-4">
      <Card className="max-w-lg">
        <h1 className="text-lg font-semibold text-text-primary">{t("admin:content.newProgram")}</h1>
        <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
          {formError && <Alert tone="danger" title={formError} />}

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("content:domainFilter")}</span>
            <select {...register("domainId")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
              <option value="">—</option>
              {domainsQuery.data?.map((domain) => (
                <option key={domain.id} value={domain.id}>
                  {t(`content:domains.${domain.slug}`)}
                </option>
              ))}
            </select>
          </label>

          <Input label={t("admin:content.fields.slug")} {...register("slug")} error={formState.errors.slug ? t("admin:content.fields.slugInvalid") : undefined} />

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:content.fields.defaultLanguage")}</span>
            <select {...register("defaultLanguage")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
              <option value="ro">{t("common:language.ro")}</option>
              <option value="en">{t("common:language.en")}</option>
            </select>
          </label>

          <Input label={t("admin:content.fields.title")} {...register("title")} />
          <Input label={t("admin:content.fields.shortDescription")} {...register("shortDescription")} />

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:content.fields.description")}</span>
            <textarea rows={4} {...register("description")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary" />
          </label>

          <Button type="submit" variant="primary" disabled={mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
