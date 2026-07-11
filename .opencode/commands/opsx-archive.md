---
description: Archive a completed and strictly validated OpenSpec change
---

Archive a completed OpenSpec change. Archival is explicit and never runs as part of apply or sync.

**Store selection:** If the user names a store (a store is a standalone OpenSpec repo registered on this machine) or the work lives in one, run `openspec store list --json` to discover registered store ids, then preserve `--store <id>` on every store-aware follow-up command. Without a store, commands act on the nearest local `openspec/` root.

**Input**: Optionally specify a change name after `/opsx-archive`. If omitted and the change cannot be inferred unambiguously, run `openspec list --json` and use the **question tool** to let the user select an active change.

**Steps**

1. Run `openspec status --change "<name>" --json` and use its schema, planning home, change root, artifact paths, and action context.
2. If any artifact is not `done`, stop and report the incomplete artifacts. Confirmation does not override this block.
3. Run `openspec instructions apply --change "<name>" --json`. If any implementation task is incomplete, stop and report the remaining tasks. Confirmation does not override this block.
4. Run `openspec validate "<name>" --strict`. Stop if strict validation fails.
5. Report whether delta specifications exist and that archive will not sync them. Synchronization requires a separate explicit `/opsx-sync` action.
6. Run `openspec archive "<name>" --yes --skip-specs --json`, preserving the selected store flag when applicable. Use the CLI-reported archive location; do not construct paths or move files manually.
7. Report the change name, schema, archive location, strict validation result, completed task state, and that spec sync was skipped.

**Guardrails**

- Never archive incomplete artifacts or tasks.
- Never bypass strict validation.
- Never sync specifications, commit, push, merge, or open a Pull Request as part of archive.
- Use CLI-reported context and results rather than hardcoded paths.
