import { useId } from "react";
import { Input } from "../../shared/design-system/Input";
import type { ClientQuestion } from "./questionnaireApi";

export interface QuestionInputProps {
  question: ClientQuestion;
  value: string;
  onChange: (value: string) => void;
}

/** Renders the correct control for each of the five question types (docs/PROMPT.md §25–28).
 * MultiChoice stores its selection as a comma-separated list of option values, matching the
 * backend's `QuestionnaireAnswer.Value` convention. */
export function QuestionInput({ question, value, onChange }: QuestionInputProps) {
  const groupId = useId();
  const selectedValues = value ? value.split(",") : [];

  const label = (
    <p className="text-sm font-medium text-text-primary">
      {question.text}
      {question.isRequired && (
        <span aria-hidden="true" className="ml-1 text-danger">
          *
        </span>
      )}
    </p>
  );

  if (question.type === "Text") {
    return (
      <div className="flex flex-col gap-1">
        {label}
        <Input
          label=""
          aria-label={question.text}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          required={question.isRequired}
        />
      </div>
    );
  }

  if (question.type === "LongText") {
    return (
      <div className="flex flex-col gap-1">
        {label}
        <textarea
          aria-label={question.text}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          rows={4}
          className="rounded-lg border border-border-default bg-surface px-3 py-2 text-sm text-text-primary placeholder:text-text-muted"
        />
      </div>
    );
  }

  if (question.type === "SingleChoice" || question.type === "Scale") {
    return (
      <fieldset className="flex flex-col gap-1">
        <legend className="text-sm font-medium text-text-primary">
          {question.text}
          {question.isRequired && (
            <span aria-hidden="true" className="ml-1 text-danger">
              *
            </span>
          )}
        </legend>
        <div className="mt-2 flex flex-wrap gap-3">
          {question.options.map((option) => (
            <label key={option.value} className="flex min-h-11 items-center gap-2 text-sm text-text-secondary">
              <input
                type="radio"
                name={groupId}
                value={option.value}
                checked={value === option.value}
                onChange={() => onChange(option.value)}
                className="h-4 w-4 accent-primary"
              />
              {option.label}
            </label>
          ))}
        </div>
      </fieldset>
    );
  }

  // MultiChoice
  return (
    <fieldset className="flex flex-col gap-1">
      <legend className="text-sm font-medium text-text-primary">
        {question.text}
        {question.isRequired && (
          <span aria-hidden="true" className="ml-1 text-danger">
            *
          </span>
        )}
      </legend>
      <div className="mt-2 flex flex-wrap gap-3">
        {question.options.map((option) => (
          <label key={option.value} className="flex min-h-11 items-center gap-2 text-sm text-text-secondary">
            <input
              type="checkbox"
              checked={selectedValues.includes(option.value)}
              onChange={(e) => {
                const next = e.target.checked
                  ? [...selectedValues, option.value]
                  : selectedValues.filter((v) => v !== option.value);
                onChange(next.join(","));
              }}
              className="h-4 w-4 accent-primary"
            />
            {option.label}
          </label>
        ))}
      </div>
    </fieldset>
  );
}
