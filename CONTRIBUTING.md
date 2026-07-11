# Contributing

## Tooling prerequisites

This repository uses:

- OpenSpec 1.6.0.
- The team-maintained patched OpenCode build. The currently validated build reports `0.0.0--202607102347`.

OpenCode must support project-level `opencode.json`, repository-local commands in `.opencode/commands/`, repository-local skills in `.opencode/skills/`, and permission prompts. This repository verifies those capabilities but does not install, upgrade, or select an OpenCode distribution.

Repository files define project permissions, OpenSpec workflows, validation, and contributor guidance. Provider and model selection, credentials, authentication, host services, operating-system users, and network configuration remain machine-managed prerequisites outside the repository.

The repository must remain usable without copying machine-managed configuration into the checkout. Keep project configuration free of provider details, credentials, private network values, and host administration settings.

## Starting a task

Update the base branch:

```bash
git switch main
git pull --ff-only origin main
```

Create or select an OpenSpec change before implementation. Planning artifacts are the source of truth for scope and task order.

## OpenSpec lifecycle

The repository provides matching command and skill workflows for six actions:

| Action | Command | Purpose |
| --- | --- | --- |
| Propose | `/opsx-propose` | Create a new change and its planning artifacts. Does not implement repository changes. |
| Continue | `/opsx-continue` | Resume an existing change by creating only its next ready planning artifacts. |
| Update | `/opsx-update` | Revise existing planning artifacts after confirmation. Does not implement repository changes. |
| Apply | `/opsx-apply` | Implement approved tasks in order and check them off only after validation. Does not publish, sync, or archive. |
| Sync | `/opsx-sync` | Explicitly validate and merge delta specifications into main specifications without archiving. |
| Archive | `/opsx-archive` | Archive a strictly valid change only after every implementation task is complete. Does not sync specifications automatically. |

Pass a change name when more than one active change could apply:

```bash
/opsx-continue example-change
/opsx-apply example-change
```

Planning, implementation, specification sync, archive, and repository publication are separate actions. A workflow must not start the next action automatically.

### External OpenSpec stores

Repository-local OpenSpec state is the default. If work belongs to a registered external store, name that store explicitly so the workflow can resolve its identifier with `openspec store list --json` and preserve `--store <id>` on supported commands.

Project permissions deny external-directory access by default. If the host does not grant access to the selected store, stop and request the narrow host-level permission needed for that store. Do not broaden project permissions or record a machine-specific store path in repository files.

## Validation

Run the complete non-destructive repository tooling check:

```bash
node scripts/validate-repository-tooling.mjs
```

Run the command/skill contract check after changing any OpenSpec workflow:

```bash
node scripts/check-openspec-workflow-drift.mjs
```

Strictly validate an OpenSpec change before implementation, sync, or archive:

```bash
openspec validate <change-name> --strict
```

Validation must be non-destructive. Review `git status` before and after validation and confirm that no unexpected files, especially files under `My project/`, changed.

## Troubleshooting

### Tool version or capability mismatch

Confirm `openspec --version` reports `1.6.0` and record the value from `opencode --version`. If the patched OpenCode build does not load project configuration, commands, skills, or permission prompts, stop and use the team's machine-managed support process. Do not install or select another OpenCode release from this repository.

### Command is missing

Confirm the corresponding file exists under `.opencode/commands/` and its paired skill exists under `.opencode/skills/`. Reopen the project in OpenCode if files were added after the session started, then run the drift check.

### External store is denied

Confirm the intended store was explicitly selected. If external-directory access is denied, leave repository permissions unchanged and request host access for only that store.

### Drift check fails

Use the reported file and contract element to compare the command with its paired skill. Correct only the demonstrated missing action, incompatible host tool, path/store handling, mutation boundary, or next-action reference.

## Maintaining generated workflows

The OpenSpec skills record `generatedBy: "1.6.0"`. Treat those generated files as the baseline:

1. Inventory all six command/skill pairs before editing.
2. Leave a generated pair unchanged when the drift check passes and no patched-host incompatibility is reproduced.
3. Make the smallest targeted correction for a concrete missing action, incompatibility, broken reference, unsafe mutation boundary, or drift failure.
4. Add both a command and a skill when introducing a required lifecycle action.
5. Run the drift check and strict OpenSpec validation after every workflow change.

When upgrading OpenSpec, regenerate in a disposable location first, compare generator metadata and behavior, and review intentional project-specific safeguards before replacing repository files.
