#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const commandsDir = path.join(root, ".opencode", "commands");
const skillsDir = path.join(root, ".opencode", "skills");

const pairs = {
  propose: "openspec-propose",
  continue: "openspec-continue-change",
  update: "openspec-update-change",
  apply: "openspec-apply-change",
  sync: "openspec-sync-specs",
  archive: "openspec-archive-change",
};

const required = {
  propose: [
    "applyRequires",
    "question tool",
    "todowrite tool",
    "Planning artifacts only",
  ],
  continue: [
    "artifactPaths",
    "applyRequires",
    "question tool",
    "Planning artifacts only",
  ],
  update: [
    "existingOutputPaths",
    "/opsx-continue",
    "question tool",
    "Never implement repository changes",
  ],
  apply: [
    "instructions apply",
    "contextFiles",
    "mark a task complete only after",
    "Never commit, push, merge, open a Pull Request, sync specifications, or archive",
  ],
  sync: [
    "validate \"<name>\" --strict",
    "instructions apply",
    "planningHome.changesDir",
    "Never archive, commit, push, merge, or open a Pull Request",
  ],
  archive: [
    "instructions apply",
    "validate \"<name>\" --strict",
    "--skip-specs",
    "Never archive incomplete",
    "Never sync specifications",
  ],
};

const forbidden = [
  "AskUserQuestion",
  "TodoWrite",
  "general-purpose",
  "/opsx-new",
  "openspec/specs/",
];

const errors = [];
const commandNames = new Set(
  fs
    .readdirSync(commandsDir)
    .filter((name) => name.startsWith("opsx-") && name.endsWith(".md"))
    .map((name) => name.slice(0, -3)),
);

function checkFile(action, file, isSkill) {
  const relative = path.relative(root, file);

  if (!fs.existsSync(file)) {
    errors.push(`${relative}: missing ${action} workflow`);
    return;
  }

  const text = fs.readFileSync(file, "utf8");
  const common = ["--store <id>", "openspec status --change"];

  for (const token of [...common, ...required[action]]) {
    if (!text.includes(token)) {
      errors.push(`${relative}: missing contract element ${JSON.stringify(token)}`);
    }
  }

  if (!/keep it on follow-ups|preserve[^\n]*store/i.test(text)) {
    errors.push(`${relative}: does not preserve explicit store selection`);
  }

  for (const token of forbidden) {
    if (text.includes(token)) {
      errors.push(`${relative}: contains unsupported reference ${JSON.stringify(token)}`);
    }
  }

  if (/\/(?:home|srv|Users)\//.test(text)) {
    errors.push(`${relative}: contains an absolute contributor path`);
  }

  for (const match of text.matchAll(/\/opsx-([a-z-]+)/g)) {
    const command = `opsx-${match[1]}`;
    if (!commandNames.has(command)) {
      errors.push(`${relative}: references missing command /${command}`);
    }
  }

  if (isSkill && !text.includes('generatedBy: "1.6.0"')) {
    errors.push(`${relative}: missing OpenSpec 1.6.0 generator metadata`);
  }
}

for (const [action, skillName] of Object.entries(pairs)) {
  checkFile(action, path.join(commandsDir, `opsx-${action}.md`), false);
  checkFile(action, path.join(skillsDir, skillName, "SKILL.md"), true);
}

if (errors.length > 0) {
  console.error("OpenSpec workflow drift check failed:");
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`OpenSpec workflow drift check passed for ${Object.keys(pairs).length} command/skill pairs.`);
