import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { Alert } from "../../../shared/design-system/Alert";
import { Button } from "../../../shared/design-system/Button";
import { Card } from "../../../shared/design-system/Card";
import { Input } from "../../../shared/design-system/Input";
import { StatusBadge } from "../../../shared/design-system/StatusBadge";
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
            selection.type === "program" ? "bg-background text-primary" : "text-text-primary"
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
                  selection.type === "section" && selection.sectionId === section.id ? "bg-background text-primary" : "text-text-primary"
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
                    {item.type === ContentItemType.Video ? "🎬" : "📄"} {itemTitle(item, language, t)}
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

  return (
    <Card className="max-w-xl">
      <h2 className="text-sm font-semibold text-text-primary">
        {item.type === ContentItemType.Video ? t("admin:content.video") : t("admin:content.richText")} — {language.toUpperCase()}
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
        type: type as 0 | 1,
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
