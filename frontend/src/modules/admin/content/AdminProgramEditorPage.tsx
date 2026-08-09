import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { Alert } from "../../../shared/design-system/Alert";
import { Badge } from "../../../shared/design-system/Badge";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { Input } from "../../../shared/design-system/Input";
import { StatusBadge } from "../../../shared/design-system/StatusBadge";
import { ApiError } from "../../../shared/api/apiError";
import { resolveMessageKey } from "../../../shared/forms/applyApiErrorToForm";
import {
  adminContentApi,
  ContentItemType,
  ProgramStatus,
  type ContentItemDetail,
  type ProgramDetail,
  type SectionDetail,
} from "./adminContentApi";

const LANGUAGES = ["ro", "en"] as const;

type Selection = { type: "program" } | { type: "section"; sectionId: string } | { type: "item"; sectionId: string; itemId: string };

export function AdminProgramEditorPage() {
  const { t } = useTranslation(["admin", "content", "common"]);
  const { programId = "" } = useParams<{ programId: string }>();
  const queryClient = useQueryClient();
  const [selection, setSelection] = useState<Selection>({ type: "program" });
  const [language, setLanguage] = useState<string>("ro");

  const programQuery = useQuery({ queryKey: ["admin-program", programId], queryFn: () => adminContentApi.getProgram(programId) });

  useEffect(() => {
    if (programQuery.data) setLanguage(programQuery.data.defaultLanguage);
    // Only reset the selected language when a different program loads, not on every background
    // refetch of the same program (which would fight the admin's own language selection).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [programQuery.data?.id]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["admin-program", programId] });

  const statusMutation = useMutation({
    mutationFn: (action: "publish" | "unpublish" | "archive") =>
      action === "publish"
        ? adminContentApi.publishProgram(programId)
        : action === "unpublish"
          ? adminContentApi.unpublishProgram(programId)
          : adminContentApi.archiveProgram(programId),
    onSuccess: invalidate,
  });

  const addSectionMutation = useMutation({
    // The backend requires a non-empty description per translation (docs/PROMPT.md §18–22) —
    // a real placeholder, not an empty string, so creation succeeds; the admin edits it
    // immediately afterward via the Editor pane, which auto-selects the new section.
    mutationFn: () =>
      adminContentApi.addSection(programId, {
        language,
        title: t("admin:content.newSectionTitle"),
        description: t("admin:content.newSectionDescription"),
      }),
    onSuccess: (sectionId) => {
      invalidate();
      setSelection({ type: "section", sectionId });
    },
  });

  const reorderSectionsMutation = useMutation({
    mutationFn: (orderedSectionIds: string[]) => adminContentApi.reorderSections(programId, orderedSectionIds),
    onSuccess: invalidate,
  });

  const deleteSectionMutation = useMutation({
    mutationFn: (sectionId: string) => adminContentApi.deleteSection(sectionId),
    onSuccess: () => {
      invalidate();
      setSelection({ type: "program" });
    },
  });

  const reorderItemsMutation = useMutation({
    mutationFn: (input: { sectionId: string; orderedContentItemIds: string[] }) =>
      adminContentApi.reorderContentItems(input.sectionId, input.orderedContentItemIds),
    onSuccess: invalidate,
  });

  const deleteItemMutation = useMutation({
    mutationFn: (itemId: string) => adminContentApi.deleteContentItem(itemId),
    onSuccess: () => {
      invalidate();
      setSelection({ type: "program" });
    },
  });

  if (programQuery.isLoading) {
    return <div className="p-4 text-sm text-text-muted">{t("common:status.loading")}</div>;
  }

  if (programQuery.isError || !programQuery.data) {
    return (
      <div className="p-4">
        <Alert tone="danger" title={t("common:errors.notFound")} />
      </div>
    );
  }

  const program = programQuery.data;

  const moveSection = (index: number, direction: -1 | 1) => {
    const ids = program.sections.map((s) => s.id);
    const target = index + direction;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    reorderSectionsMutation.mutate(ids);
  };

  const moveItem = (section: SectionDetail, index: number, direction: -1 | 1) => {
    const ids = section.items.map((i) => i.id);
    const target = index + direction;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    reorderItemsMutation.mutate({ sectionId: section.id, orderedContentItemIds: ids });
  };

  return (
    <div className="flex min-h-screen flex-col desktop:flex-row">
      {/* Structure */}
      <aside className="w-full shrink-0 border-b border-border-default bg-surface p-3 desktop:w-72 desktop:border-b-0 desktop:border-r">
        <button
          type="button"
          onClick={() => setSelection({ type: "program" })}
          className={`mb-2 w-full rounded-md px-2 py-2 text-left text-sm font-semibold ${
            selection.type === "program" ? "bg-primary/10 text-primary" : "text-text-primary"
          }`}
        >
          {t("admin:content.programSettings")}
        </button>

        {program.sections.map((section, sectionIndex) => (
          <div key={section.id} className="mb-2">
            <div className="flex items-center gap-1">
              <button
                type="button"
                onClick={() => setSelection({ type: "section", sectionId: section.id })}
                className={`flex-1 truncate rounded-md px-2 py-2 text-left text-sm font-medium ${
                  selection.type === "section" && selection.sectionId === section.id ? "bg-primary/10 text-primary" : "text-text-primary"
                }`}
              >
                {sectionTitle(section, language, t)}
              </button>
              <button type="button" aria-label={t("admin:content.moveUp")} disabled={sectionIndex === 0} onClick={() => moveSection(sectionIndex, -1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
                ↑
              </button>
              <button type="button" aria-label={t("admin:content.moveDown")} disabled={sectionIndex === program.sections.length - 1} onClick={() => moveSection(sectionIndex, 1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
                ↓
              </button>
            </div>
            <ul className="ml-3 border-l border-border-default pl-2">
              {section.items.map((item, itemIndex) => (
                <li key={item.id} className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => setSelection({ type: "item", sectionId: section.id, itemId: item.id })}
                    className={`flex-1 truncate rounded-md px-2 py-1 text-left text-xs ${
                      selection.type === "item" && selection.itemId === item.id ? "bg-background text-primary" : "text-text-secondary"
                    }`}
                  >
                    {item.type === ContentItemType.Video ? "🎬" : item.type === ContentItemType.Quiz ? "❓" : "📄"} {itemTitle(item, language, t)}
                  </button>
                  <button type="button" aria-label={t("admin:content.moveUp")} disabled={itemIndex === 0} onClick={() => moveItem(section, itemIndex, -1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
                    ↑
                  </button>
                  <button type="button" aria-label={t("admin:content.moveDown")} disabled={itemIndex === section.items.length - 1} onClick={() => moveItem(section, itemIndex, 1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
                    ↓
                  </button>
                </li>
              ))}
            </ul>
            <AddContentItemForm sectionId={section.id} language={language} onAdded={invalidate} />
          </div>
        ))}

        <Button variant="secondary" onClick={() => addSectionMutation.mutate()} disabled={addSectionMutation.isPending}>
          {t("admin:content.addSection")}
        </Button>
      </aside>

      {/* Editor */}
      <main className="flex-1 p-4">
        {selection.type === "program" && <ProgramEditorForm program={program} language={language} onSaved={invalidate} />}
        {selection.type === "section" &&
          (() => {
            const section = program.sections.find((s) => s.id === selection.sectionId);
            // The new section's ID is selected immediately on creation, but the query
            // invalidation that would bring it into `program.sections` is still in flight for
            // one render — show a brief loading state instead of crashing on `undefined`.
            return section ? (
              <SectionEditorForm
                section={section}
                language={language}
                onSaved={invalidate}
                onDelete={() => deleteSectionMutation.mutate(selection.sectionId)}
              />
            ) : (
              <div className="text-sm text-text-muted">{t("common:status.loading")}</div>
            );
          })()}
        {selection.type === "item" &&
          (() => {
            const item = program.sections.find((s) => s.id === selection.sectionId)?.items.find((i) => i.id === selection.itemId);
            return item ? (
              <ItemEditorForm
                item={item}
                language={language}
                onSaved={invalidate}
                onDelete={() => deleteItemMutation.mutate(selection.itemId)}
              />
            ) : (
              <div className="text-sm text-text-muted">{t("common:status.loading")}</div>
            );
          })()}
      </main>

      {/* Properties */}
      <aside className="w-full shrink-0 border-t border-border-default bg-surface p-3 desktop:w-64 desktop:border-t-0 desktop:border-l">
        <h2 className="text-xs font-semibold uppercase text-text-muted">{t("admin:content.properties")}</h2>

        <label className="mt-3 flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:content.fields.defaultLanguage")}</span>
          <select value={language} onChange={(e) => setLanguage(e.target.value)} className="rounded-md border border-border-default px-3 py-2 text-sm">
            {LANGUAGES.map((lang) => (
              <option key={lang} value={lang}>
                {t(`common:language.${lang}`)} {program.translations.some((tr) => tr.language === lang) ? "✓" : `(${t("admin:content.missing")})`}
              </option>
            ))}
          </select>
        </label>

        <div className="mt-4">
          <StatusBadge
            status={program.status === ProgramStatus.Published ? "success" : program.status === ProgramStatus.Archived ? "neutral" : "warning"}
            label={t(`admin:content.status.${program.status === ProgramStatus.Published ? "published" : program.status === ProgramStatus.Archived ? "archived" : "draft"}`)}
          />
        </div>

        <div className="mt-3 flex flex-col gap-2">
          {program.status === ProgramStatus.Draft && (
            <Button variant="primary" onClick={() => statusMutation.mutate("publish")} disabled={statusMutation.isPending}>
              {t("admin:content.publish")}
            </Button>
          )}
          {program.status === ProgramStatus.Published && (
            <>
              <Button variant="secondary" onClick={() => statusMutation.mutate("unpublish")} disabled={statusMutation.isPending}>
                {t("admin:content.unpublish")}
              </Button>
              <Button variant="danger" onClick={() => statusMutation.mutate("archive")} disabled={statusMutation.isPending}>
                {t("admin:content.archive")}
              </Button>
            </>
          )}
          {program.status === ProgramStatus.Draft && (
            <Button variant="danger" onClick={() => statusMutation.mutate("archive")} disabled={statusMutation.isPending}>
              {t("admin:content.archive")}
            </Button>
          )}
        </div>
      </aside>
    </div>
  );
}

function sectionTitle(section: SectionDetail, language: string, t: (key: string) => string): string {
  return section.translations.find((tr) => tr.language === language)?.title ?? `(${t("admin:content.missing")})`;
}

function itemTitle(item: ContentItemDetail, language: string, t: (key: string) => string): string {
  return item.translations.find((tr) => tr.language === language)?.title ?? `(${t("admin:content.missing")})`;
}

function ProgramEditorForm({ program, language, onSaved }: { program: ProgramDetail; language: string; onSaved: () => void }) {
  const { t } = useTranslation(["admin", "common"]);
  const translation = program.translations.find((tr) => tr.language === language);
  const [title, setTitle] = useState(translation?.title ?? "");
  const [shortDescription, setShortDescription] = useState(translation?.shortDescription ?? "");
  const [description, setDescription] = useState(translation?.description ?? "");

  useEffect(() => {
    setTitle(translation?.title ?? "");
    setShortDescription(translation?.shortDescription ?? "");
    setDescription(translation?.description ?? "");
  }, [translation?.title, translation?.shortDescription, translation?.description]);

  const mutation = useMutation({
    mutationFn: () => adminContentApi.upsertProgramTranslation(program.id, { language, title, shortDescription, description }),
    onSuccess: onSaved,
  });

  return (
    <Card className="max-w-xl">
      <h2 className="text-sm font-semibold text-text-primary">{t("admin:content.programSettings")} — {language.toUpperCase()}</h2>
      <div className="mt-3 flex flex-col gap-3">
        <Input label={t("admin:content.fields.title")} value={title} onChange={(e) => setTitle(e.target.value)} />
        <Input label={t("admin:content.fields.shortDescription")} value={shortDescription} onChange={(e) => setShortDescription(e.target.value)} />
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:content.fields.description")}</span>
          <textarea rows={6} value={description} onChange={(e) => setDescription(e.target.value)} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary" />
        </label>
        <Button variant="primary" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
        </Button>
      </div>
    </Card>
  );
}

function SectionEditorForm({ section, language, onSaved, onDelete }: { section: SectionDetail; language: string; onSaved: () => void; onDelete: () => void }) {
  const { t } = useTranslation(["admin", "common"]);
  const translation = section.translations.find((tr) => tr.language === language);
  const [title, setTitle] = useState(translation?.title ?? "");
  const [description, setDescription] = useState(translation?.description ?? "");

  useEffect(() => {
    setTitle(translation?.title ?? "");
    setDescription(translation?.description ?? "");
  }, [translation?.title, translation?.description]);

  const mutation = useMutation({
    mutationFn: () => adminContentApi.upsertSectionTranslation(section.id, { language, title, description }),
    onSuccess: onSaved,
  });

  return (
    <Card className="max-w-xl">
      <h2 className="text-sm font-semibold text-text-primary">{t("admin:content.section")} — {language.toUpperCase()}</h2>
      <div className="mt-3 flex flex-col gap-3">
        <Input label={t("admin:content.fields.title")} value={title} onChange={(e) => setTitle(e.target.value)} />
        <label className="flex flex-col gap-1">
          <span className="text-sm font-medium text-text-primary">{t("admin:content.fields.description")}</span>
          <textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary" />
        </label>
        <div className="flex gap-2">
          <Button variant="primary" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
          </Button>
          <Button variant="danger" onClick={onDelete}>
            {t("admin:content.deleteSection")}
          </Button>
        </div>
      </div>
    </Card>
  );
}

function ItemEditorForm({ item, language, onSaved, onDelete }: { item: ContentItemDetail; language: string; onSaved: () => void; onDelete: () => void }) {
  const { t } = useTranslation(["admin", "common"]);
  const translation = item.translations.find((tr) => tr.language === language);
  const [title, setTitle] = useState(translation?.title ?? "");
  const [body, setBody] = useState(translation?.body ?? "");

  useEffect(() => {
    setTitle(translation?.title ?? "");
    setBody(translation?.body ?? "");
  }, [translation?.title, translation?.body]);

  const mutation = useMutation({
    mutationFn: () => adminContentApi.upsertContentItemTranslation(item.id, { language, title, body: item.type === ContentItemType.RichText ? body : null }),
    onSuccess: onSaved,
  });

  const itemTypeLabel =
    item.type === ContentItemType.Video
      ? t("admin:content.video")
      : item.type === ContentItemType.Quiz
        ? t("admin:content.quiz.label")
        : t("admin:content.richText");

  return (
    <Card className="max-w-xl">
      <h2 className="text-sm font-semibold text-text-primary">
        {itemTypeLabel} — {language.toUpperCase()}
      </h2>
      <div className="mt-3 flex flex-col gap-3">
        <Input label={t("admin:content.fields.title")} value={title} onChange={(e) => setTitle(e.target.value)} />
        {item.type === ContentItemType.RichText && (
          <label className="flex flex-col gap-1">
            <span className="text-sm font-medium text-text-primary">{t("admin:content.fields.body")}</span>
            <textarea rows={8} value={body} onChange={(e) => setBody(e.target.value)} className="rounded-md border border-border-default px-3 py-2 text-sm text-text-primary" />
          </label>
        )}
        {item.type === ContentItemType.Video && (
          <p className="text-xs text-text-muted">{t("admin:content.videoReferenceNotEditable")}</p>
        )}
        <div className="flex gap-2">
          <Button variant="primary" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending ? t("common:status.saving") : t("common:actions.save")}
          </Button>
          <Button variant="danger" onClick={onDelete}>
            {t("admin:content.deleteItem")}
          </Button>
        </div>
      </div>

      {item.type === ContentItemType.Quiz && <QuizBuilder contentItemId={item.id} language={language} />}
    </Card>
  );
}

/** Local, in-session representation of a quiz question/option being authored — see the
 * `QuizBuilder` docstring below for why this can't be backed by the real admin read model yet. */
interface QuizOptionState {
  id: string;
  isCorrect: boolean;
  labels: Record<string, string>;
}

interface QuizQuestionState {
  id: string;
  texts: Record<string, string>;
  options: QuizOptionState[];
}

function describeQuizError(error: unknown, t: (key: string) => string): string {
  if (ApiError.isApiError(error)) {
    return t(resolveMessageKey(error.messageKey));
  }
  return t("common:errors.internalServerError");
}

/**
 * Admin quiz-question/option builder for a `Quiz` content item.
 *
 * KNOWN LIMITATION (reported, not silently worked around): `GetProgramDetailHandler` /
 * `ProgramDetailDto` (the only admin read of a program's content items) does not include quiz
 * question/option data at all, and there is no dedicated admin "get quiz detail" endpoint either.
 * Every mutation below round-trips to the real backend correctly and is durably persisted, but
 * this component has no way to re-fetch what already exists for a quiz item — so its list of
 * questions/options is scoped to this editing session only (reset whenever `contentItemId`
 * changes) and will appear empty again after a reload or after navigating away and back, even
 * though the previously authored questions are still there and will be served to clients. This is
 * a real Phase 1 backend gap, called out explicitly rather than faked with client-only state that
 * would look correct but silently lie after a refresh.
 */
function QuizBuilder({ contentItemId, language }: { contentItemId: string; language: string }) {
  const { t } = useTranslation(["admin", "common"]);
  const [questions, setQuestions] = useState<QuizQuestionState[]>([]);
  const [newQuestionText, setNewQuestionText] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setQuestions([]);
    setNewQuestionText("");
    setError(null);
  }, [contentItemId]);

  const addQuestionMutation = useMutation({
    mutationFn: () => adminContentApi.addQuizQuestion(contentItemId, { language, text: newQuestionText }),
    onSuccess: (questionId) => {
      setQuestions((prev) => [...prev, { id: questionId, texts: { [language]: newQuestionText }, options: [] }]);
      setNewQuestionText("");
      setError(null);
    },
    onError: (err) => setError(describeQuizError(err, t)),
  });

  const deleteQuestionMutation = useMutation({
    mutationFn: (questionId: string) => adminContentApi.deleteQuizQuestion(questionId),
    onSuccess: (_result, questionId) => {
      setQuestions((prev) => prev.filter((q) => q.id !== questionId));
      setError(null);
    },
    onError: (err) => setError(describeQuizError(err, t)),
  });

  const moveQuestion = (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= questions.length) return;
    const reordered = [...questions];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    adminContentApi
      .reorderQuizQuestions(contentItemId, reordered.map((q) => q.id))
      .then(() => {
        setQuestions(reordered);
        setError(null);
      })
      .catch((err) => setError(describeQuizError(err, t)));
  };

  return (
    <div className="mt-4 border-t border-border-default pt-3">
      <h3 className="text-sm font-semibold text-text-primary">{t("admin:content.quiz.questions")}</h3>
      <div className="mt-2">
        <Alert tone="warning" title={t("admin:content.quiz.sessionOnlyWarningTitle")}>
          {t("admin:content.quiz.sessionOnlyWarningBody")}
        </Alert>
      </div>
      {error && (
        <div className="mt-2">
          <Alert tone="danger" title={error} />
        </div>
      )}

      {questions.length === 0 ? (
        <p className="mt-2 text-sm text-text-muted">{t("admin:content.quiz.noQuestionsThisSession")}</p>
      ) : (
        <ul className="mt-3 flex flex-col gap-3">
          {questions.map((question, index) => (
            <li key={question.id}>
              <QuizQuestionEditor
                question={question}
                language={language}
                index={index}
                total={questions.length}
                onMove={(direction) => moveQuestion(index, direction)}
                onDelete={() => deleteQuestionMutation.mutate(question.id)}
                onQuestionTextsChanged={(texts) => setQuestions((prev) => prev.map((q) => (q.id === question.id ? { ...q, texts } : q)))}
                onOptionsChanged={(options) => setQuestions((prev) => prev.map((q) => (q.id === question.id ? { ...q, options } : q)))}
                onError={setError}
              />
            </li>
          ))}
        </ul>
      )}

      <div className="mt-3 flex gap-2">
        <input
          value={newQuestionText}
          onChange={(e) => setNewQuestionText(e.target.value)}
          placeholder={t("admin:content.quiz.fields.questionText")}
          className="flex-1 rounded-md border border-border-default px-2 py-2 text-sm"
        />
        <Button variant="secondary" onClick={() => addQuestionMutation.mutate()} disabled={addQuestionMutation.isPending || !newQuestionText}>
          {t("admin:content.quiz.addQuestion")}
        </Button>
      </div>
    </div>
  );
}

function QuizQuestionEditor({
  question,
  language,
  index,
  total,
  onMove,
  onDelete,
  onQuestionTextsChanged,
  onOptionsChanged,
  onError,
}: {
  question: QuizQuestionState;
  language: string;
  index: number;
  total: number;
  onMove: (direction: -1 | 1) => void;
  onDelete: () => void;
  onQuestionTextsChanged: (texts: Record<string, string>) => void;
  onOptionsChanged: (options: QuizOptionState[]) => void;
  onError: (message: string | null) => void;
}) {
  const { t } = useTranslation(["admin", "common"]);
  const [text, setText] = useState(question.texts[language] ?? "");
  const [newOptionLabel, setNewOptionLabel] = useState("");
  const [newOptionCorrect, setNewOptionCorrect] = useState(false);

  useEffect(() => {
    setText(question.texts[language] ?? "");
    setNewOptionLabel("");
    setNewOptionCorrect(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [language, question.id]);

  const saveTextMutation = useMutation({
    mutationFn: () => adminContentApi.upsertQuizQuestionTranslation(question.id, { language, text }),
    onSuccess: () => {
      onQuestionTextsChanged({ ...question.texts, [language]: text });
      onError(null);
    },
    onError: (err) => onError(describeQuizError(err, t)),
  });

  const hasCorrectOption = question.options.some((o) => o.isCorrect);

  const addOptionMutation = useMutation({
    mutationFn: () => adminContentApi.addQuizOption(question.id, { language, label: newOptionLabel, isCorrect: newOptionCorrect }),
    onSuccess: (optionId) => {
      onOptionsChanged([...question.options, { id: optionId, isCorrect: newOptionCorrect, labels: { [language]: newOptionLabel } }]);
      setNewOptionLabel("");
      setNewOptionCorrect(false);
      onError(null);
    },
    onError: (err) => onError(describeQuizError(err, t)),
  });

  const deleteOptionMutation = useMutation({
    mutationFn: (optionId: string) => adminContentApi.deleteQuizOption(optionId),
    onSuccess: (_result, optionId) => {
      onOptionsChanged(question.options.filter((o) => o.id !== optionId));
      onError(null);
    },
    onError: (err) => onError(describeQuizError(err, t)),
  });

  const moveOption = (idx: number, direction: -1 | 1) => {
    const target = idx + direction;
    if (target < 0 || target >= question.options.length) return;
    const reordered = [...question.options];
    [reordered[idx], reordered[target]] = [reordered[target], reordered[idx]];
    adminContentApi
      .reorderQuizOptions(question.id, reordered.map((o) => o.id))
      .then(() => {
        onOptionsChanged(reordered);
        onError(null);
      })
      .catch((err) => onError(describeQuizError(err, t)));
  };

  return (
    <Card>
      <div className="flex items-center justify-between gap-2">
        <span className="text-xs font-semibold uppercase text-text-muted">
          {t("admin:content.quiz.question")} {index + 1}
        </span>
        <div className="flex gap-1">
          <button type="button" aria-label={t("admin:content.moveUp")} disabled={index === 0} onClick={() => onMove(-1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
            ↑
          </button>
          <button type="button" aria-label={t("admin:content.moveDown")} disabled={index === total - 1} onClick={() => onMove(1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
            ↓
          </button>
        </div>
      </div>

      <div className="mt-2 flex gap-2">
        <input
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={t("admin:content.quiz.fields.questionText")}
          className="flex-1 rounded-md border border-border-default px-2 py-2 text-sm"
        />
        <Button variant="secondary" onClick={() => saveTextMutation.mutate()} disabled={saveTextMutation.isPending || !text}>
          {saveTextMutation.isPending ? t("common:status.saving") : t("common:actions.save")}
        </Button>
      </div>

      <div className="mt-3">
        <h4 className="text-xs font-semibold uppercase text-text-muted">{t("admin:content.quiz.options")}</h4>
        {question.options.length === 0 ? (
          <p className="mt-1 text-xs text-text-muted">{t("admin:content.quiz.noOptionsYet")}</p>
        ) : (
          <ul className="mt-1 flex flex-col gap-1">
            {question.options.map((option, idx) => (
              <li key={option.id} className="flex items-center justify-between gap-2 text-sm">
                <span className="flex items-center gap-2">
                  <span aria-hidden="true">{option.isCorrect ? "●" : "○"}</span>
                  {option.labels[language] ?? `(${t("admin:content.missing")})`}
                  {option.isCorrect && <Badge tone="success">{t("admin:content.quiz.correctAnswer")}</Badge>}
                </span>
                <span className="flex items-center gap-1">
                  <button type="button" aria-label={t("admin:content.moveUp")} disabled={idx === 0} onClick={() => moveOption(idx, -1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
                    ↑
                  </button>
                  <button type="button" aria-label={t("admin:content.moveDown")} disabled={idx === question.options.length - 1} onClick={() => moveOption(idx, 1)} className="min-h-8 min-w-8 text-text-muted disabled:opacity-30">
                    ↓
                  </button>
                  <button type="button" onClick={() => deleteOptionMutation.mutate(option.id)} className="text-xs text-danger">
                    {t("admin:content.quiz.deleteOption")}
                  </button>
                </span>
              </li>
            ))}
          </ul>
        )}

        <div className="mt-2 flex flex-col gap-2 rounded-md border border-border-default p-2">
          <input
            value={newOptionLabel}
            onChange={(e) => setNewOptionLabel(e.target.value)}
            placeholder={t("admin:content.quiz.fields.optionLabel")}
            className="rounded-md border border-border-default px-2 py-2 text-sm"
          />
          <label className="flex items-center gap-2 text-xs text-text-secondary">
            <input
              type="checkbox"
              checked={newOptionCorrect}
              disabled={hasCorrectOption}
              onChange={(e) => setNewOptionCorrect(e.target.checked)}
            />
            {t("admin:content.quiz.markAsCorrect")}
          </label>
          {hasCorrectOption && <p className="text-xs text-text-muted">{t("admin:content.quiz.correctAlreadySetHint")}</p>}
          <Button variant="secondary" onClick={() => addOptionMutation.mutate()} disabled={addOptionMutation.isPending || !newOptionLabel}>
            {t("admin:content.quiz.addOption")}
          </Button>
        </div>
      </div>

      <Button variant="danger" className="mt-3" onClick={onDelete}>
        {t("admin:content.quiz.deleteQuestion")}
      </Button>
    </Card>
  );
}

function AddContentItemForm({ sectionId, language, onAdded }: { sectionId: string; language: string; onAdded: () => void }) {
  const { t } = useTranslation(["admin", "common"]);
  const [isOpen, setIsOpen] = useState(false);
  const [type, setType] = useState<number>(ContentItemType.RichText);
  const [title, setTitle] = useState("");
  const [videoReference, setVideoReference] = useState("");
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    // The backend requires a non-empty body for a RichText item (docs/PROMPT.md §18–22) — this
    // quick-add form doesn't collect one yet, so send a real placeholder the admin immediately
    // edits afterward via the Editor pane (same reasoning as the section quick-add above).
    mutationFn: () =>
      adminContentApi.addContentItem(sectionId, {
        type: type as 0 | 1 | 2,
        isRequired: true,
        language,
        title,
        body: type === ContentItemType.RichText ? t("admin:content.newItemBody") : null,
        videoReference: type === ContentItemType.Video ? videoReference : null,
      }),
    onSuccess: () => {
      setIsOpen(false);
      setTitle("");
      setVideoReference("");
      setError(null);
      onAdded();
    },
    onError: () => setError(t("admin:content.addItemError")),
  });

  if (!isOpen) {
    return (
      <button type="button" onClick={() => setIsOpen(true)} className="ml-3 mt-1 text-xs font-medium text-primary hover:underline">
        {t("admin:content.addItem")}
      </button>
    );
  }

  return (
    <div className="ml-3 mt-2 flex flex-col gap-2 rounded-md border border-border-default p-2">
      {error && <Alert tone="danger" title={error} />}
      <select value={type} onChange={(e) => setType(Number(e.target.value))} className="rounded-md border border-border-default px-2 py-1 text-xs">
        <option value={ContentItemType.RichText}>{t("admin:content.richText")}</option>
        <option value={ContentItemType.Video}>{t("admin:content.video")}</option>
        <option value={ContentItemType.Quiz}>{t("admin:content.quiz.label")}</option>
      </select>
      <input
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        placeholder={t("admin:content.fields.title")}
        className="rounded-md border border-border-default px-2 py-1 text-xs"
      />
      {type === ContentItemType.Video && (
        <input
          value={videoReference}
          onChange={(e) => setVideoReference(e.target.value)}
          placeholder={t("admin:content.fields.videoReference")}
          className="rounded-md border border-border-default px-2 py-1 text-xs"
        />
      )}
      <div className="flex gap-2">
        <Button variant="primary" onClick={() => mutation.mutate()} disabled={mutation.isPending || !title}>
          {t("common:actions.save")}
        </Button>
        <Button variant="secondary" onClick={() => setIsOpen(false)}>
          {t("common:actions.cancel")}
        </Button>
      </div>
    </div>
  );
}
