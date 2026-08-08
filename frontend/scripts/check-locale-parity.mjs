#!/usr/bin/env node
// Fails (non-zero exit) if any `ro`/`en` locale namespace file is missing on one side, or has
// mismatched keys — see docs/DEVELOPMENT_INSTRUCTIONS.md §8 ("ro and en locale files MUST
// maintain key parity in the same change"). Run via `npm run check:locale-parity`.

import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const localesDir = path.resolve(__dirname, "..", "src", "locales");
const languages = ["ro", "en"];

function flattenKeys(value, prefix = "") {
  if (value === null || typeof value !== "object" || Array.isArray(value)) {
    return [prefix];
  }
  return Object.entries(value).flatMap(([key, child]) =>
    flattenKeys(child, prefix ? `${prefix}.${key}` : key),
  );
}

async function loadNamespaceFiles(language) {
  const dir = path.join(localesDir, language);
  const entries = await readdir(dir);
  return entries.filter((entry) => entry.endsWith(".json")).sort();
}

async function main() {
  const [roFiles, enFiles] = await Promise.all(languages.map(loadNamespaceFiles));
  const allNamespaces = [...new Set([...roFiles, ...enFiles])].sort();

  let hasError = false;

  for (const namespaceFile of allNamespaces) {
    const missingIn = [];
    if (!roFiles.includes(namespaceFile)) missingIn.push("ro");
    if (!enFiles.includes(namespaceFile)) missingIn.push("en");

    if (missingIn.length > 0) {
      hasError = true;
      console.error(`✗ ${namespaceFile}: missing in ${missingIn.join(", ")}`);
      continue;
    }

    const [roContent, enContent] = await Promise.all(
      languages.map((language) =>
        readFile(path.join(localesDir, language, namespaceFile), "utf-8").then(JSON.parse),
      ),
    );

    const roKeys = new Set(flattenKeys(roContent));
    const enKeys = new Set(flattenKeys(enContent));

    const missingInEn = [...roKeys].filter((key) => !enKeys.has(key));
    const missingInRo = [...enKeys].filter((key) => !roKeys.has(key));

    if (missingInEn.length > 0 || missingInRo.length > 0) {
      hasError = true;
      console.error(`✗ ${namespaceFile}:`);
      for (const key of missingInEn) console.error(`    missing in en: ${key}`);
      for (const key of missingInRo) console.error(`    missing in ro: ${key}`);
    }
  }

  if (hasError) {
    console.error("\nLocale key parity check failed.");
    process.exit(1);
  }

  console.log(`✓ Locale key parity OK (${allNamespaces.length} namespace files, ${languages.join("/")}).`);
}

main();
