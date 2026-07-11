#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const expectedOpenSpecVersion = "1.6.0";
const patchedOpenCodeVersion = /^0\.0\.0--\d{12}$/;
const errors = [];

const workflowPairs = {
  propose: "openspec-propose",
  continue: "openspec-continue-change",
  update: "openspec-update-change",
  apply: "openspec-apply-change",
  sync: "openspec-sync-specs",
  archive: "openspec-archive-change",
};

const requiredFiles = [
  "opencode.json",
  "openspec/config.yaml",
  "CONTRIBUTING.md",
  ".github/pull_request_template.md",
  "scripts/check-openspec-workflow-drift.mjs",
  ...Object.entries(workflowPairs).flatMap(([action, skill]) => [
    `.opencode/commands/opsx-${action}.md`,
    `.opencode/skills/${skill}/SKILL.md`,
  ]),
];

function run(command, args, label) {
  const result = spawnSync(command, args, {
    cwd: root,
    encoding: "utf8",
    env: process.env,
  });

  if (result.error || result.status !== 0) {
    errors.push(`${label}: command failed`);
    return "";
  }

  return result.stdout.trim();
}

function read(relative) {
  const file = path.join(root, relative);
  if (!fs.existsSync(file) || !fs.statSync(file).isFile()) {
    errors.push(`${relative}: required file is missing`);
    return "";
  }
  return fs.readFileSync(file, "utf8");
}

for (const file of requiredFiles) read(file);

const opencodeText = read("opencode.json");
let opencodeConfig;
try {
  opencodeConfig = JSON.parse(opencodeText);
} catch {
  errors.push("opencode.json: invalid JSON");
}

if (opencodeConfig) {
  if (opencodeConfig.$schema !== "https://opencode.ai/config.json") {
    errors.push("opencode.json: expected OpenCode schema declaration is missing");
  }
  if (opencodeConfig.permission?.external_directory !== "deny") {
    errors.push("opencode.json: external_directory must default to deny");
  }
  if (!opencodeConfig.permission?.bash || !opencodeConfig.permission?.skill) {
    errors.push("opencode.json: portable bash or skill permissions are missing");
  }

  const forbiddenKeys = /^(?:provider|providers|model|models|endpoint|base_?url|api_?key|token|password|secret|credential|credentials|authorization|authentication)$/i;
  const visit = (value, location) => {
    if (!value || typeof value !== "object") return;
    for (const [key, child] of Object.entries(value)) {
      if (forbiddenKeys.test(key)) {
        errors.push(`opencode.json: forbidden machine-managed field at ${location}.${key}`);
      }
      visit(child, `${location}.${key}`);
    }
  };
  visit(opencodeConfig, "config");
}

const openspecConfig = read("openspec/config.yaml");
for (const token of [
  "schema: spec-driven",
  "context: |",
  "rules:",
  "proposal:",
  "design:",
  "specs:",
  "tasks:",
]) {
  if (!openspecConfig.includes(token)) {
    errors.push(`openspec/config.yaml: missing ${JSON.stringify(token)}`);
  }
}

const openspecList = run("openspec", ["list", "--json"], "openspec/config.yaml parse");
if (openspecList) {
  try {
    JSON.parse(openspecList);
  } catch {
    errors.push("openspec/config.yaml: OpenSpec did not return parseable JSON context");
  }
}

const openspecVersion = run("openspec", ["--version"], "OpenSpec version check");
if (openspecVersion && openspecVersion !== expectedOpenSpecVersion) {
  errors.push(`OpenSpec version: expected ${expectedOpenSpecVersion}, observed ${openspecVersion}`);
}

const opencodeVersion = run("opencode", ["--version"], "OpenCode version check");
if (opencodeVersion && !patchedOpenCodeVersion.test(opencodeVersion)) {
  errors.push("OpenCode version: installed build does not match the team-maintained patched build identity");
}

const opencodeHelp = run("opencode", ["--help"], "OpenCode capability check");
const opencodeDebugHelp = run("opencode", ["debug", "--help"], "OpenCode debug capability check");
for (const [label, text, tokens] of [
  ["OpenCode", opencodeHelp, ["opencode debug", "--pure", "--version"]],
  ["OpenCode debug", opencodeDebugHelp, ["debug config", "debug skill"]],
]) {
  for (const token of tokens) {
    if (text && !text.includes(token)) {
      errors.push(`${label} capability: missing ${JSON.stringify(token)}`);
    }
  }
}

const scannedFiles = requiredFiles.filter(
  (file) => !file.startsWith("scripts/"),
);
const forbiddenContent = [
  ["absolute machine path", /\/(?:etc|home|srv|Users)\//],
  ["private network address", /\b(?:10\.(?:\d{1,3}\.){2}\d{1,3}|127\.(?:\d{1,3}\.){2}\d{1,3}|169\.254\.(?:\d{1,3}\.)\d{1,3}|172\.(?:1[6-9]|2\d|3[01])\.(?:\d{1,3}\.)\d{1,3}|192\.168\.(?:\d{1,3}\.)\d{1,3})\b/],
  ["credential assignment", /^\s*["']?(?:api[_-]?key|token|password|secret|credential|authorization|auth)["']?\s*[:=]\s*\S+/im],
  ["credential token", /\b(?:AKIA[0-9A-Z]{16}|gh[pousr]_[A-Za-z0-9]{20,}|sk-[A-Za-z0-9]{20,})\b/],
  ["private key material", /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/],
];

for (const relative of scannedFiles) {
  const text = read(relative);
  const withoutSchemaUrl = text.replaceAll("https://opencode.ai/config.json", "");
  if (/https?:\/\//i.test(withoutSchemaUrl)) {
    errors.push(`${relative}: endpoint URL is not allowed in repository-owned tooling`);
  }
  for (const [category, pattern] of forbiddenContent) {
    if (pattern.test(text)) errors.push(`${relative}: contains ${category}`);
  }
}

const drift = spawnSync(process.execPath, [path.join(root, "scripts", "check-openspec-workflow-drift.mjs")], {
  cwd: root,
  encoding: "utf8",
  env: process.env,
});
if (drift.error || drift.status !== 0) {
  errors.push("OpenSpec workflow drift check: failed; run the drift checker for file-level details");
}

if (errors.length > 0) {
  console.error("Repository tooling validation failed:");
  for (const error of [...new Set(errors)]) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`OpenSpec ${openspecVersion}: compatible`);
console.log(`OpenCode ${opencodeVersion}: patched build capabilities available`);
console.log("Repository-owned configuration: valid and free of forbidden machine-managed values");
console.log("OpenSpec workflow drift check: passed");
console.log("Repository tooling validation passed without modifying files");
