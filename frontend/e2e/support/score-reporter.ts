import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import type { FullResult, Reporter, TestCase, TestResult } from "@playwright/test/reporter";

type Area = "UI/UX" | "Security" | "Flow";
type Entry = { title: string; project: string; passed: boolean; durationMs: number; error?: string };

/**
 * The full project list a canonical run (`npx playwright test`, no project/file filter) executes
 * — see playwright.config.ts. Any run that doesn't touch every one of these (e.g. a single-spec
 * or single-project invocation used to diagnose a specific failure) is a "focused run" and must
 * be labeled as such, never blended into a canonical score.
 */
const CANONICAL_PROJECTS = ["desktop-chromium", "mobile-chromium", "security-abuse-last"];

function currentCommitSha(): string {
  try {
    return execFileSync("git", ["rev-parse", "HEAD"], { encoding: "utf-8" }).trim();
  } catch {
    return "unknown";
  }
}

export default class ScoreReporter implements Reporter {
  private readonly entries: Record<Area, Entry[]> = { "UI/UX": [], Security: [], Flow: [] };

  onTestEnd(test: TestCase, result: TestResult): void {
    if (result.status === "skipped") return;
    const title = test.titlePath().join(" > ");
    const area: Area | undefined = title.includes("@uiux") ? "UI/UX" : title.includes("@security") ? "Security" : title.includes("@flow") ? "Flow" : undefined;
    if (!area) return;
    const uiScore = result.attachments.find((attachment) => attachment.name === "uiux-score" && attachment.body);
    if (area === "UI/UX" && uiScore?.body) {
      const checks = JSON.parse(uiScore.body.toString()) as Array<{ name: string; passed: boolean; evidence?: string }>;
      for (const check of checks) {
        this.entries[area].push({
          title: check.name,
          project: test.parent.project()?.name ?? "unknown",
          passed: check.passed,
          durationMs: result.duration,
          error: check.passed ? undefined : check.evidence,
        });
      }
      return;
    }
    this.entries[area].push({
      title: test.title,
      project: test.parent.project()?.name ?? "unknown",
      passed: result.status === "passed",
      durationMs: result.duration,
      error: result.error?.message,
    });
  }

  onEnd(result: FullResult): void {
    const weights: Record<Area, number> = { "UI/UX": 0.35, Security: 0.35, Flow: 0.30 };
    const areas = (Object.keys(this.entries) as Area[]).map((name) => {
      const checks = this.entries[name];
      const passed = checks.filter((entry) => entry.passed).length;
      return { name, score: checks.length ? Math.round((passed / checks.length) * 100) : 0, passed, total: checks.length, checks };
    });

    const projectsExecuted = [...new Set(Object.values(this.entries).flat().map((entry) => entry.project))].sort();
    const isCanonicalRun = CANONICAL_PROJECTS.every((project) => projectsExecuted.includes(project));
    const runType: "canonical" | "focused" = isCanonicalRun ? "canonical" : "focused";

    const isCompleteAudit = areas.every((area) => area.total > 0);
    const overall = isCompleteAudit && isCanonicalRun
      ? Math.round(areas.reduce((sum, area) => sum + area.score * weights[area.name], 0))
      : null;

    const generatedAt = new Date().toISOString();
    const commitSha = currentCommitSha();

    const report = {
      generatedAt,
      commitSha,
      runType,
      projectsExecuted,
      status: result.status,
      overall,
      weights,
      areas,
    };
    const directory = path.resolve("e2e-results");
    fs.mkdirSync(directory, { recursive: true });
    fs.writeFileSync(path.join(directory, "score.json"), `${JSON.stringify(report, null, 2)}\n`);
    fs.writeFileSync(path.join(directory, "score.md"), [
      "# B-United E2E audit score",
      "",
      `Run type: **${runType === "canonical" ? "canonical (all projects)" : "FOCUSED — diagnostic only, not canonical"}**`,
      `Generated: ${generatedAt}`,
      `Commit: \`${commitSha}\``,
      `Projects executed: ${projectsExecuted.length ? projectsExecuted.map((p) => `\`${p}\``).join(", ") : "(none)"}`,
      "",
      `Overall: **${overall === null ? (runType === "focused" ? "N/A (focused run — not a canonical score)" : "N/A (incomplete audit)") : `${overall}/100`}**`,
      "",
      "| Area | Score | Passed | Weight |",
      "| --- | ---: | ---: | ---: |",
      ...areas.map((area) => `| ${area.name} | ${area.score}/100 | ${area.passed}/${area.total} | ${Math.round(weights[area.name] * 100)}% |`),
      "",
      "A failed Security check is a release blocker regardless of the overall score.",
      "",
    ].join("\n"));
  }
}
