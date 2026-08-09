import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { Alert } from "../../../shared/design-system/Alert";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { Input } from "../../../shared/design-system/Input";
import { applyApiErrorToForm } from "../../../shared/forms/applyApiErrorToForm";
import { adminQuestionnaireApi } from "./adminQuestionnaireApi";

const schema = z.object({
  defaultLanguage: z.enum(["ro", "en"]),
  title: z.string().min(1),
  description: z.string().min(1),
});
type FormValues = z.infer<typeof schema>;

export function AdminNewQuestionnairePage() {
  const { t } = useTranslation(["admin", "common"]);
  const navigate = useNavigate();

  const { register, handleSubmit, setError } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { defaultLanguage: "ro" },
  });
  const [formError, setFormError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (values: FormValues) => adminQuestionnaireApi.create(values.defaultLanguage, values.title, values.description),
    onSuccess: (questionnaireId) => navigate(`/admin/questionnaires/${questionnaireId}`, { replace: true }),
    onError: (error: unknown) => setFormError(applyApiErrorToForm(error, setError, t, ["defaultLanguage", "title", "description"])),
  });

  const onSubmit = handleSubmit((values) => {
    setFormError(null);
    mutation.mutate(values);
  });

  return (
    <div>
      <Card className="max-w-lg">
        <h1 className="text-2xl font-semibold text-text-primary">{t("admin:questionnaires.newQuestionnaire")}</h1>
        <form onSubmit={onSubmit} noValidate className="mt-4 flex flex-col gap-4">
          {formError && <Alert tone="danger" title={formError} />}

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:questionnaires.fields.defaultLanguage")}</span>
            <select {...register("defaultLanguage")} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary">
              <option value="ro">{t("common:language.ro")}</option>
              <option value="en">{t("common:language.en")}</option>
            </select>
          </label>

          <Input label={t("admin:questionnaires.fields.title")} {...register("title")} />

          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:questionnaires.fields.description")}</span>
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
