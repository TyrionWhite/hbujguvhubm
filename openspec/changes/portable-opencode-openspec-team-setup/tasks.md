## 1. Establish the Portable Project Configuration

- [x] 1.1 Document OpenSpec 1.6.0 and the team-maintained patched OpenCode build as prerequisites, including the build identity and host capabilities that validation must observe without selecting or installing another OpenCode release.
- [x] 1.2 Reconcile project-level `opencode.json` with portable permissions and remove model endpoints, private addresses, credentials, authentication material, and dependencies on machine-managed configuration.
- [x] 1.3 Configure `openspec/config.yaml` with concise project context and artifact rules that preserve the repository/machine boundary and planning-versus-implementation boundary.
- [x] 1.4 Compare the pre-existing OpenCode backup with the active project configuration and remove it only after confirming that it contains no unique portable settings or private material.

## 2. Complete and Verify the OpenSpec Lifecycle

- [x] 2.1 Inventory the propose, continue, update, apply, sync, and archive command/skill pairs against the OpenSpec 1.6.0 generator metadata, recording concrete missing actions, patched-host incompatibilities, broken references, unsafe boundaries, or drift while leaving compliant generated files unchanged.
- [x] 2.2 Add matching repository-local continue command and skill entry points that use CLI-reported status, dependencies, paths, and store context to resume only ready artifacts.
- [x] 2.3 Correct existing generated command or skill files only where the inventory demonstrates a concrete incompatibility or drift, preserving CLI-derived artifact paths, supported interaction tools, explicit store propagation, and valid next-action references.
- [x] 2.4 Ensure propose, continue, and update remain planning-only, and ensure apply reads complete planning context, announces scope, and checks off tasks only after their implementation is verified.
- [x] 2.5 Ensure apply never commits, pushes, merges, opens a Pull Request, syncs main specifications, or archives a change, and reports those operations as separate explicit actions when relevant.
- [x] 2.6 Ensure sync validates delta specifications and reports incomplete task state before an explicitly requested sync, while archive blocks on incomplete tasks or failed strict validation.
- [x] 2.7 Add drift checks for paired lifecycle coverage, store propagation, CLI-derived paths, readiness checks, mutation boundaries, supported host tools, and next-action identifiers without rewriting compliant generated files.

## 3. Add Contributor and Pull Request Guidance

- [x] 3.1 Expand `CONTRIBUTING.md` with patched-build and OpenSpec prerequisites, repository/machine ownership boundaries, all six lifecycle actions, external-store permission handling, validation, troubleshooting, and generated-workflow maintenance.
- [x] 3.2 Add a Pull Request template that requests the related OpenSpec change, validation evidence, implementation scope, and confirmation of whether Unity content is affected without automating Pull Request creation.
- [x] 3.3 Add root `AGENTS.md` as repository-owned contributor and coding-agent policy covering repository and Unity boundaries, Git and Pull Request restrictions, OpenSpec planning and implementation boundaries, Unity asset and serialization safety, maintainable modular architecture, authoritative gameplay-state ownership, deterministic state resolution, diagnostics, useful documentation, testability, regression expectations, validation, and final reporting.
- [x] 3.4 Review `AGENTS.md` against the actual repository and Unity project structure, confirm it introduces no custom agent definitions, handoff contracts, or Unity-specific policy skills, and confirm this scope changes no Unity implementation or serialized content.

## 4. Implement Non-Destructive Validation

- [x] 4.1 Add a repository-local validation script that checks required files, parses `opencode.json` and `openspec/config.yaml`, verifies OpenSpec 1.6.0 and patched OpenCode capabilities, and rejects machine-specific paths, endpoint fields, private addresses, and credential-like material without printing secret values.
- [x] 4.2 Make validation invoke the finite command/skill drift checks and fail with actionable file and contract details while never installing tools, changing repository files, or invoking Unity.
- [x] 4.3 Run the validation script and strict OpenSpec validation in the working checkout, resolving implementation defects while stopping for an explicit planning update if an approved artifact is incorrect.
- [x] 4.4 Test onboarding and validation from a separate clean clone with isolated project configuration and only the documented machine-managed prerequisites.
- [x] 4.5 Repeat clean-clone validation under a pre-existing second Linux user and confirm it does not depend on the first user's checkout path, project configuration copies, credentials, or private values.
- [x] 4.6 Compare repository status and path-scoped diffs before and after all validation runs to confirm no Unity content or generated Unity files changed.
- [x] 4.7 Review the apply execution record and final repository state to confirm that no commit, push, merge, Pull Request creation, specification sync, or archive operation occurred during implementation.
- [x] 4.8 Extend repository validation to require root `AGENTS.md` and verify its required policy coverage without rewriting the file, inspecting machine-managed secrets, invoking Unity, or changing repository state.
- [x] 4.9 Run repository tooling validation and strict OpenSpec validation after adding `AGENTS.md`, compare status and path-scoped diffs before and after, and confirm no Unity content, custom agents, custom Unity policy skills, publication operations, specification sync, or archive operation were introduced.
