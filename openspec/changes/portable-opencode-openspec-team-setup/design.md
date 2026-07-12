## Context

The repository contains a Unity project alongside root `opencode.json` and `AGENTS.md`, repository-local OpenSpec state, and OpenSpec commands and skills under `.opencode/`. OpenSpec 1.6.0 and a team-maintained patched OpenCode build are used in the current environment, but a clean checkout does not document or validate the complete project-level tooling and coding-agent policy contract. The generated workflow set also lacks a continue action and contains references that must be checked against the patched host.

The team has approved a narrow portability change. Repository files will define OpenSpec workflows, commands, skills, portable permissions, and contributor guidance. Provider configuration, model endpoints, credentials, authentication, Linux users, systemd services, and private network configuration remain machine-managed and must not be copied into or required by the repository.

## Goals / Non-Goals

**Goals:**

- Make project-level OpenCode permissions and OpenSpec workflows discoverable and verifiable from a clean checkout.
- Provide propose, continue, update, apply, sync, and archive actions through consistent repository-local command and skill entry points.
- Verify required behavior under OpenSpec 1.6.0 and the team-maintained patched OpenCode build without embedding machine-managed configuration.
- Add contributor guidance, a Pull Request template, and non-destructive validation, including validation from a separate clone under a second Linux user.
- Establish root `AGENTS.md` as repository-owned contributor and coding-agent policy for safe, maintainable work in the repository and Unity project.
- Preserve generated OpenSpec content unless a concrete gap, incompatibility, or verified drift requires a targeted change.

**Non-Goals:**

- Selecting, installing, or distributing a public stable OpenCode release.
- Managing provider configuration, model endpoints, credentials, authentication, `/etc/opencode/team.json`, Linux accounts, systemd services, or private network configuration.
- Defining repository-local planning, implementation, review, or verification agents or agent handoff contracts.
- Creating custom Unity policy skills or changing any Unity asset, package, project setting, generated file, or gameplay code.
- Committing, pushing, merging, or opening a Pull Request while this change is applied.

## Decisions

### Define an explicit repository and machine configuration boundary

The repository source of truth consists of project-level `opencode.json` permissions without model endpoints, root `AGENTS.md`, `.opencode` OpenSpec commands and skills, `openspec/config.yaml`, contributor documentation, the Pull Request template, and validation tooling. These files must use repository-relative or CLI-resolved paths and must not contain private addresses, passwords, API keys, or a dependency on `/etc/opencode/team.json`.

Provider and model selection, endpoints, credentials, authentication, host users, services, and private networking remain machine-managed. The setup may document that compatible machine configuration is a prerequisite, but it will neither inspect secret values nor prescribe their storage. Making a machine-wide file the repository contract was rejected because it is not portable and risks exposing private configuration.

### Verify current capabilities without choosing an OpenCode distribution

Validation will require OpenSpec 1.6.0 and will identify the installed team-maintained patched OpenCode build. It will check the OpenCode behaviors this repository relies on, including project configuration discovery and command/skill loading, rather than attempting to install or replace that build. Contributor documentation will state these prerequisites and direct unsupported-build questions to the team-maintained distribution process.

Selecting a public stable release and distribution channel is deferred to a separate team decision. A bootstrap installer is therefore rejected for this change; it could silently replace required patches or make an unapproved distribution choice.

### Make only targeted changes to generated workflows

The OpenSpec 1.6.0 generated commands and skills remain the baseline. Implementation will inventory action coverage and generator metadata, add the missing continue command/skill pair, and alter existing generated files only when a supported-host incompatibility, broken next-step reference, unsafe mutation boundary, or measurable command/skill drift is demonstrated.

Drift validation will compare a finite contract rather than claim full semantic equivalence of Markdown. It will check lifecycle action coverage, paired entry points, store propagation, CLI-derived path handling, readiness and mutation boundaries, and valid next-action identifiers. Wholesale rewriting was rejected because it increases maintenance burden and obscures future generator updates.

### Keep lifecycle mutations explicit

Proposal, continuation, and update actions modify planning artifacts only. Apply requires complete schema-defined planning context, announces its implementation scope, and marks a task complete only after its work is verified. Apply must not commit, push, merge, open a Pull Request, sync specifications, or archive the change.

Sync is an explicit planning action: it validates delta specifications and reports incomplete implementation tasks, but may proceed after an explicit sync request because it does not claim implementation completion. Archive is a separate explicit action and must stop when strict validation fails or implementation tasks remain incomplete. This separates main-spec maintenance from implementation while preventing archive from recording unfinished work as complete.

### Validate without mutating the repository or Unity project

A repository-local validation script will check required files, parse project-level configuration, reject forbidden machine-specific or secret material, verify tool versions and required host capabilities, and evaluate the finite command/skill drift contract. It must not install tools, modify files, invoke Unity, or require access to machine-managed secret configuration.

Acceptance will run first in the working checkout, then in a separate clean clone with isolated user configuration under a pre-existing second Linux user. The second user and clone location are test prerequisites managed outside the repository. Before/after status and path-scoped diffs will confirm that validation did not alter Unity files. This is preferred over a container because the patched OpenCode build and second-user host behavior are the compatibility target.

### Keep collaboration guidance repository-local

`CONTRIBUTING.md` will document prerequisites, lifecycle usage, validation, troubleshooting, and generated-workflow maintenance. A repository Pull Request template will request relevant OpenSpec change references, validation evidence, and confirmation that Unity scope is accurate. Adding the template does not create or submit a Pull Request.

### Establish repository-owned coding-agent policy

Root `AGENTS.md` will define the repository and Unity project boundaries that contributors and coding agents must respect. It will preserve Git and Pull Request restrictions; OpenSpec planning, implementation, sync, and archive boundaries; and Unity safety for assets, `.meta` files, GUIDs, generated directories, scenes, prefabs, and serialized content. It will not define custom agents, handoff contracts, provider behavior, or custom Unity policy skills.

The policy will also set general engineering expectations for small composable modules, authoritative gameplay-state and movement-output ownership, deterministic state resolution, observable diagnostics, rationale-focused comments and public API contracts, independently testable logic, regression scenarios, non-destructive validation, and accurate final reporting. This guidance belongs in the same repository-owned setup because it governs how the configured tools safely extend the game, while implementation-specific architecture and gameplay changes remain subject to separately approved OpenSpec changes.

## Risks / Trade-offs

- [The patched OpenCode build has no public stable identifier] -> Report its available version/build identity and verify required capabilities; defer distribution selection.
- [Generated OpenSpec files can change after upgrades] -> Record the OpenSpec 1.6.0 baseline and make only evidence-backed edits protected by finite drift checks.
- [Static scans can miss novel secret formats or flag examples] -> Keep repository configuration free of endpoint and credential fields, scan known forbidden values and key patterns, and require human diff review.
- [Second-user validation depends on host preparation] -> Treat the Linux user and patched tool installation as machine-managed prerequisites and fail with actionable guidance when unavailable.
- [Sync before implementation can make main specs describe planned behavior] -> Require an explicit sync request, report task state, and reserve archive for fully implemented changes.
- [A Pull Request template may be mistaken for publication automation] -> Document that it is guidance only and keep all publication operations outside apply.

## Migration Plan

1. Record the OpenSpec 1.6.0 and patched OpenCode capability baseline without installing or replacing either tool.
2. Reconcile project-level `opencode.json` and `openspec/config.yaml` with the repository/machine boundary.
3. Inventory generated command/skill pairs, add continue, and make only validated compatibility or drift corrections.
4. Add non-destructive validation, contributor guidance, root `AGENTS.md`, and the Pull Request template.
5. Validate in the working checkout and a separate clone under a pre-existing second Linux user without invoking Unity.
6. Review status and diffs to confirm only approved tooling, planning, documentation, and template paths changed.

Rollback consists of reverting these repository-local tooling and documentation changes. No machine configuration or Unity rollback is required.

## Open Questions

None within this change. Selection and distribution of a public stable OpenCode release, custom agents, handoff contracts, and Unity policy automation are deferred to separately approved changes.
