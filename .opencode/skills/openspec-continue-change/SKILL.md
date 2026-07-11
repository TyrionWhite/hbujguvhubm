---
name: openspec-continue-change
description: Continue an existing OpenSpec change by creating its next ready planning artifacts. Use when the user wants to resume or complete an existing change's artifacts without implementation.
allowed-tools: Bash(openspec:*)
license: MIT
compatibility: Requires openspec CLI.
metadata:
  author: openspec
  version: "1.0"
  generatedBy: "1.6.0"
---

Continue an existing OpenSpec change. Create planning artifacts only; never implement repository changes.

**Store selection:** If the user names a store (a store is a standalone OpenSpec repo registered on this machine) or the work lives in one, run `openspec store list --json` to discover registered store ids, then pass `--store <id>` on commands that accept it. Preserve the selected store on every follow-up command. Without a store, commands act on the nearest local `openspec/` root.

**Input**: Optionally specify a change name. If omitted and the change cannot be inferred unambiguously, run `openspec list --json` and use the **question tool** to let the user select.

**Steps**

1. Run `openspec status --change "<name>" --json` and use its `planningHome`, `changeRoot`, `artifactPaths`, `actionContext`, `applyRequires`, and artifact dependency graph.
2. If every requested artifact is complete, report status and stop without editing files.
3. Select an artifact whose status is `ready`, then run `openspec instructions <artifact-id> --change "<name>" --json`.
4. Read every completed dependency from the concrete paths reported by status or instructions. Apply the returned context, rules, instruction, and template without copying context/rule blocks into the artifact.
5. Write only the instructed planning artifact. For a glob output, derive concrete files from the capability names and schema instructions; never write to the glob pattern itself.
6. Re-run status after each artifact. Continue in dependency order until the user's requested artifacts are complete or, when no narrower target was requested, every artifact in `applyRequires` is complete.
7. Show final status, created artifacts, and the next available lifecycle action.

**Guardrails**

- Planning artifacts only. Do not modify implementation files, Unity content, main specifications, task completion state, or archive state.
- Use CLI-reported artifact ids and concrete paths; do not assume spec-driven filenames.
- Do not create blocked artifacts or invent files outside schema instructions.
- Ask with the **question tool** when required information or confirmation is missing.
- Never start apply, sync, archive, commit, push, merge, or Pull Request operations automatically.
