import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { Alert } from "../../../shared/design-system/Alert";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { Input } from "../../../shared/design-system/Input";
import { StatusBadge } from "../../../shared/design-system/StatusBadge";
import {
  adminQuestionnaireApi,
  type QuestionDetail,
  type QuestionTypeName,
  type QuestionnaireDetail,
} from "./adminQuestionnaireApi";

const LANGUAGES = ["ro", "en"] as const;
const QUESTION_TYPES: QuestionTypeName[] = ["Text", "LongText", "SingleChoice", "MultiChoice", "Scale"];
const CHOICE_TYPES: QuestionTypeName[] = ["SingleChoice", "MultiChoice", "Scale"];

type Selection = { type: "questionnaire" } | { type: "question"; questionId: string };

export function AdminQuestionnaireEditorPage() {
  const { t } = useTranslation(["admin", "common"]);
  const { questionnaireId = "" } = useParams<{ questionnaireId: string }>();
  const queryClient = useQueryClient();
  const [selection, setSelection] = useState<Selection>({ type: "questionnaire" });
  const [language, setLanguage] = useState<string>("ro");

  const questionnaireQuery = useQuery({
    queryKey: ["admin-questionnaire", questionnaireId],
    queryFn: () => adminQuestionnaireApi.get(questionnaireId),
  });

  useEffect(() => {
    if (questionnaireQuery.data) setLanguage(questionnaireQuery.data.defaultLanguage);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [questionnaireQuery.data?.id]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["admin-questionnaire", questionnaireId] });

  const statusMutation = useMutation({
    mutationFn: (action: "publish" | "unpublish" | "archive") =>
      action === "publish"
        ? adminQuestionnaireApi.publish(questionnaireId)
        : action === "unpublish"
          ? adminQuestionnaireApi.unpublish(questionnaireId)
          : adminQuestionnaireApi.archive(questionnaireId),
    onSuccess: invalidate,
  });

  const addQuestionMutation = useMutation({
    mutationFn: (type: QuestionTypeName) =>
      adminQuestionnaireApi.addQuestion(questionnaireId, type, true, t("admin:questionnaires.newQuestionText"), null),
    onSuccess: (questionId) => {
      invalidate();
      setSelection({ type: "question", questionId });
    },
  });

  const reorderQuestionsMutation = useMutation({
    mutationFn: (orderedQuestionIds: string[]) => adminQuestionnaireApi.reorderQuestions(questionnaireId, orderedQuestionIds),
    onSuccess: invalidate,
  });

  const deleteQuestionMutation = useMutation({
    mutationFn: (questionId: string) => adminQuestionnaireApi.deleteQuestion(questionId),
    onSuccess: () => {
      invalidate();
      setSelection({ type: "questionnaire" });
    },
  });

  if (questionnaireQuery.isLoading) {
    return <div className="p-4 text-sm text-text-muted">{t("common:status.loading")}</div>;
  }

  if (questionnaireQuery.isError || !questionnaireQuery.data) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.notFound")} />
      </div>
    );
  }

  const questionnaire = questionnaireQuery.data;

  const moveQuestion = (index: number, direction: -1 | 1) => {
    const ids = questionnaire.questions.map((q) => q.id);
    const target = index + direction;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    reorderQuestionsMutation.mutate(ids);
  };

  return (
    <div className="flex min-h-screen flex-col desktop:flex-row">
      {/* Structure */}
      <aside className="w-full shrink-0 border-b border-border-default bg-surface p-3 desktop:w-72 desktop:border-b-0 desktop:border-r">
        <button
          type="button"
          onClick={() => setSelection({ type: "questionnaire" })}
          className={`mb-2 w-full rounded-md px-2 py-2 text-left text-sm font-semibold ${
            selection.type === "questionnaire" ? "bg-primary/10 text-primary" : "text-text-primary"
          }`}
        >
          {t("admin:questionnaires.settings")}
        </button>

        {questionnaire.questions.map((question, index) => (
          <div key={question.id} className="mb-1 flex items-center gap-1">
            <button
              type="button"
              onClick={() => setSelection({ type: "question", questionId: question.id })}
              className={`flex-1 truncate rounded-md px-2 py-2 text-left text-sm ${
                selection.type === "question" && selection.questionId === question.id ? "bg-primary/10 text-primary" : "text-text-primary"
              }`}
            >
              {questionText(question, language, t)}
            </button>
            <button type="button" aria-label={t("admin:questionnaires.moveUp")} disabled={index === 0} onClick={() => moveQuestion(index, -1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
              ↑
            </button>
            <button type="button" aria-label={t("admin:questionnaires.moveDown")} disabled={index === questionnaire.questions.length - 1} onClick={() => moveQuestion(index, 1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
              ↓
            </button>
          </div>
        ))}

        <label className="mt-2 flex flex-col gap-1">
          <span className="text-xs font-medium text-text-muted">{t("admin:questionnaires.addQuestion")}</span>
          <div className="flex gap-2">
            <select id="new-question-type" defaultValue="Text" className="flex-1 rounded-md border border-border-default px-2 py-2 text-sm">
              {QUESTION_TYPES.map((type) => (
                <option key={type} value={type}>
                  {t(`admin:questionnaires.questionTypes.${type}`)}
                </option>
              ))}
            </select>
            <Button
              variant="secondary"
              onClick={() => {
                const select = document.getElementById("new-question-type") as HTMLSelectElement;
                addQuestionMutation.mutate(select.value as QuestionTypeName);
              }}
              disabled={addQuestionMutation.isPending}
            >
              {t("admin:questionnaires.add")}
            </Button>
          </div>
        </label>
      </aside>

      {/* Editor */}
      <main className="flex-1 p-4">
        {selection.type === "questionnaire" && (
          <QuestionnaireEditorForm questionnaire={questionnaire} language={language} onSaved={invalidate} />
        )}
        {selection.type === "question" &&
          (() => {
            const question = questionnaire.questions.find((q) => q.id === selection.questionId);
            return question ? (
              <QuestionEditorForm
                question={question}
                language={language}
                onSaved={invalidate}
                onDelete={() => deleteQuestionMutation.mutate(selection.questionId)}
              />
            ) : (
              <div className="text-sm text-text-muted">{t("common:status.loading")}</div>
            );
          })()}
      </main>

      {/* Properties */}
      <aside className="w-full shrink-0 border-t border-border-default bg-surface p-3 desktop:w-64 desktop:border-t-0 desktop:border-l">
        <h2 className="text-xs font-semibold uppercase text-text-muted">{t("admin:questionnaires.properties")}</h2>

        <label className="mt-3 flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:questionnaires.fields.defaultLanguage")}</span>
          <select value={language} onChange={(e) => setLanguage(e.target.value)} className="rounded-md border border-border-default px-3 py-2 text-sm">
            {LANGUAGES.map((lang) => (
              <option key={lang} value={lang}>
                {t(`common:language.${lang}`)} {questionnaire.translations.some((tr) => tr.language === lang) ? "✓" : `(${t("admin:questionnaires.missing")})`}
              </option>
            ))}
          </select>
        </label>

        <div className="mt-4">
          <StatusBadge
            status={questionnaire.status === "Published" ? "success" : questionnaire.status === "Archived" ? "neutral" : "warning"}
            label={t(`admin:questionnaires.status.${questionnaire.status.toLowerCase()}`)}
          />
        </div>

        <div className="mt-3 flex flex-col gap-2">
          {questionnaire.status === "Draft" && (
            <Button variant="primary" onClick={() => statusMutation.mutate("publish")} disabled={statusMutation.isPending}>
              {t("admin:questionnaires.publish")}
            </Button>
          )}
          {questionnaire.status === "Published" && (
            <Button variant="secondary" onClick={() => statusMutation.mutate("unpublish")} disabled={statusMutation.isPending}>
              {t("admin:questionnaires.unpublish")}
            </Button>
          )}
          {questionnaire.status !== "Archived" && (
            <Button variant="danger" onClick={() => statusMutation.mutate("archive")} disabled={statusMutation.isPending}>
              {t("admin:questionnaires.archive")}
            </Button>
          )}
        </div>

        <Link to="/admin/questionnaires/queue" className="mt-6 block text-sm font-medium text-primary">
          {t("admin:questionnaires.viewQueue")}
        </Link>
      </aside>
    </div>
  );
}

function questionText(question: QuestionDetail, language: string, t: (key: string) => string): string {
  return question.translations.find((tr) => tr.language === language)?.text ?? `(${t("admin:questionnaires.missing")})`;
}

function QuestionnaireEditorForm({
  questionnaire,
  language,
  onSaved,
}: {
  questionnaire: QuestionnaireDetail;
  language: string;
  onSaved: () => void;
}) {
  const { t } = useTranslation(["admin", "common"]);
  const translation = questionnaire.translations.find((tr) => tr.language === language);
  const [title, setTitle] = useState(translation?.title ?? "");
  const [description, setDescription] = useState(translation?.description ?? "");

  useEffect(() => {
    setTitle(translation?.title ?? "");
    setDescription(translation?.description ?? "");
  }, [translation?.title, translation?.description]);

  const mutation = useMutation({
    mutationFn: () => adminQuestionnaireApi.upsertTranslation(questionnaire.id, language, title, description),
    onSuccess: onSaved,
  });

  return (
    <Card className="max-w-xl">
      <h2 className="text-sm font-semibold text-text-primary">{t("admin:questionnaires.settings")} — {language.toUpperCase()}</h2>
      <div className="mt-3 flex flex-col gap-3">
        <Input label={t("admin:questionnaires.fields.title")} value={title} onChange={(e) => setTitle(e.target.value)} />
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:questionnaires.fields.description")}</span>
          <textarea rows={4} value={description} onChange={(e) => setDescription(e.target.value)} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary" />
        </label>
        <Button variant="primary" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
        </Button>
      </div>
    </Card>
  );
}

function QuestionEditorForm({
  question,
  language,
  onSaved,
  onDelete,
}: {
  question: QuestionDetail;
  language: string;
  onSaved: () => void;
  onDelete: () => void;
}) {
  const { t } = useTranslation(["admin", "common"]);
  const translation = question.translations.find((tr) => tr.language === language);
  const [text, setText] = useState(translation?.text ?? "");
  const [helpText, setHelpText] = useState(translation?.helpText ?? "");
  const [newOptionValue, setNewOptionValue] = useState("");
  const [newOptionLabel, setNewOptionLabel] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setText(translation?.text ?? "");
    setHelpText(translation?.helpText ?? "");
  }, [translation?.text, translation?.helpText]);

  const mutation = useMutation({
    mutationFn: () => adminQuestionnaireApi.upsertQuestionTranslation(question.id, language, text, helpText || null),
    onSuccess: onSaved,
  });

  const addOptionMutation = useMutation({
    mutationFn: () => adminQuestionnaireApi.addQuestionOption(question.id, newOptionValue, newOptionLabel),
    onSuccess: () => {
      setNewOptionValue("");
      setNewOptionLabel("");
      onSaved();
    },
    onError: () => setError(t("admin:questionnaires.addOptionError")),
  });

  const deleteOptionMutation = useMutation({
    mutationFn: (optionId: string) => adminQuestionnaireApi.deleteQuestionOption(optionId),
    onSuccess: onSaved,
  });

  const isChoiceType = CHOICE_TYPES.includes(question.type);

  return (
    <Card className="max-w-xl">
      <h2 className="text-sm font-semibold text-text-primary">
        {t(`admin:questionnaires.questionTypes.${question.type}`)} — {language.toUpperCase()}
      </h2>
      {error && <Alert tone="danger" title={error} />}
      <div className="mt-3 flex flex-col gap-3">
        <Input label={t("admin:questionnaires.fields.questionText")} value={text} onChange={(e) => setText(e.target.value)} />
        <Input label={t("admin:questionnaires.fields.helpText")} value={helpText} onChange={(e) => setHelpText(e.target.value)} />
        <Button variant="primary" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
        </Button>

        {isChoiceType && (
          <div className="mt-4 border-t border-border-default pt-3">
            <h3 className="text-sm font-semibold text-text-primary">{t("admin:questionnaires.options")}</h3>
            <ul className="mt-2 flex flex-col gap-1">
              {question.options.map((option) => (
                <li key={option.id} className="flex items-center justify-between gap-2 text-sm">
                  <span>
                    {option.translations.find((tr) => tr.language === language)?.label ?? option.value} ({option.value})
                  </span>
                  <button type="button" onClick={() => deleteOptionMutation.mutate(option.id)} className="text-xs text-danger">
                    {t("admin:questionnaires.deleteOption")}
                  </button>
                </li>
              ))}
            </ul>
            <div className="mt-2 flex gap-2">
              <input
                placeholder={t("admin:questionnaires.fields.optionValue")}
                value={newOptionValue}
                onChange={(e) => setNewOptionValue(e.target.value)}
                className="w-24 rounded-md border border-border-default px-2 py-2 text-sm"
              />
              <input
                placeholder={t("admin:questionnaires.fields.optionLabel")}
                value={newOptionLabel}
                onChange={(e) => setNewOptionLabel(e.target.value)}
                className="flex-1 rounded-md border border-border-default px-2 py-2 text-sm"
              />
              <Button
                variant="secondary"
                onClick={() => addOptionMutation.mutate()}
                disabled={addOptionMutation.isPending || !newOptionValue || !newOptionLabel}
              >
                {t("admin:questionnaires.add")}
              </Button>
            </div>
          </div>
        )}

        <Button variant="danger" className="mt-4 self-start" onClick={onDelete}>
          {t("admin:questionnaires.deleteQuestion")}
        </Button>
      </div>
    </Card>
  );
}
