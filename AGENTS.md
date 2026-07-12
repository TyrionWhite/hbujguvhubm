# Coding Agent Guide

## Project Structure

- The repository root contains project tooling and contributor policy: `CONTRIBUTING.md`, `opencode.json`, `scripts/`, `.opencode/`, and `openspec/`.
- The Unity project root is `My project/`, using Unity `6000.3.0f1` as recorded in `My project/ProjectSettings/ProjectVersion.txt`.
- Unity content is under `My project/Assets/`. Project-authored runtime code is primarily in `My project/Assets/_Game/Scripts/`; project editor tooling is in `My project/Assets/_Game/Editor/`; scenes, settings, and shaders are under `My project/Assets/_Game/` and `My project/Assets/Scenes/`.
- `My project/Assets/TutorialInfo/` and `My project/Assets/Readme.asset` are Unity tutorial content, not repository tooling.
- Unity package declarations are in `My project/Packages/`; Unity-wide serialized configuration is in `My project/ProjectSettings/`.
- OpenSpec configuration, active changes, archived changes, and main specifications live in `openspec/config.yaml`, `openspec/changes/`, `openspec/changes/archive/`, and `openspec/specs/`.
- Repository-local OpenSpec commands are in `.opencode/commands/`; their paired skills are in `.opencode/skills/`. Validation scripts are in `scripts/`.

## Working Principles

- Inspect the relevant implementation, call sites, serialized references, and nearby patterns before editing. Do not infer architecture from a file name alone.
- Make the smallest focused change that satisfies the approved scope. Avoid unrelated cleanup, formatting, renames, and refactors.
- Preserve behavior outside the requested scope. State verified facts separately from assumptions, unknowns, and recommendations in plans and reports.
- Do not invent missing requirements. Ask when behavior, ownership, or acceptance criteria are unclear.
- Stop and request approval if the work requires an architectural change not present in the approved request or OpenSpec design, including moving state ownership, introducing a new subsystem, or changing cross-system contracts.
- Do not modify machine-managed configuration or add credentials, host-specific paths, network values, or provider configuration to the repository.

## Git Safety

- Do not commit, push, merge, rewrite history, create or close Pull Requests, switch branches, sync specifications, or archive OpenSpec changes unless the user explicitly requests that exact action.
- Treat commit, push, Pull Request creation, specification sync, and OpenSpec archive as separate actions. Never perform one because another was requested.
- The repository owner must approve and merge Pull Requests. Agents must not merge on the owner's behalf.
- Never discard, overwrite, or revert unrelated worktree changes. If concurrent edits conflict with the requested work, stop and ask.
- Before completion, show the changed files with `git status --short` and `git diff --name-only`, inspect `git diff`, and run `git diff --check`.

## OpenSpec Workflow

- Use OpenSpec for implementation work that adds or changes product behavior, architecture, cross-system contracts, or repository workflows. Create or select the change before implementation; its planning artifacts define scope and task order.
- Use `/opsx-explore` for investigation only. Use `/opsx-propose`, `/opsx-continue`, and `/opsx-update` for planning artifacts only; they must not modify Unity or implementation files.
- Use `/opsx-apply <change-name>` only when implementation is explicitly requested. Read the paths returned by `openspec status --change "<change-name>" --json` and `openspec instructions apply --change "<change-name>" --json`, then apply only approved tasks in order.
- Keep planning and implementation separate. If implementation exposes a design problem or requires work outside the approved tasks, pause and request an artifact update rather than silently expanding scope.
- Mark a task complete only after its implementation and required validation succeed. Do not check off partial, unverified, or blocked work.
- Strictly validate a change before implementation, sync, or archive with `openspec validate <change-name> --strict`.
- Specification sync and archive require separate explicit requests. Apply must not commit, push, create a Pull Request, sync, or archive; sync must not archive or publish; archive must not sync or publish.
- Preserve repository-local OpenSpec command/skill pairs. Change generated workflows only for a demonstrated gap or incompatibility, and keep command and skill behavior aligned.

## Unity Safety

- Preserve every tracked `.meta` file and its GUID. Do not delete or regenerate metadata merely to resolve an import issue.
- Move or rename a Unity asset together with its `.meta` file. Prefer Unity Editor asset operations when available; otherwise move the pair without editing the GUID.
- Do not edit generated or ignored Unity directories: `My project/Library/`, `My project/Temp/`, `My project/Obj/`, `My project/Build/`, `My project/Builds/`, `My project/Logs/`, `My project/UserSettings/`, `My project/MemoryCaptures/`, `My project/Recordings/`, or `My project/.utmp/`.
- Avoid unnecessary edits to scenes, prefabs, serialized `.asset` files, `My project/Assets/InputSystem_Actions.inputactions`, package lock files, and `My project/ProjectSettings/`. These files can create broad serialized diffs.
- Inspect serialized diffs carefully. Do not hand-edit asset GUID references unless the requested change requires it and the relationship has been verified.
- Editor scripts belong under an `Editor/` directory; runtime scripts must not depend on `UnityEditor`.
- Do not claim compilation, import, scene, prefab, Edit Mode, Play Mode, or gameplay validation unless it was actually performed in the Unity Editor.

## C# And Gameplay Guidance

- Runtime gameplay components currently live in `My project/Assets/_Game/Scripts/`, including player control, camera follow, lighting, projected shadows, occlusion, and URP material helpers. `My project/Assets/_Game/Editor/IsometricLevelBuilder.cs` is editor-only level construction code.
- Identify one authoritative owner for each piece of mutable gameplay state before changing it. The owner computes and writes the state; other systems should consume it or submit intent rather than independently overwrite it.
- Keep raw input, gameplay state, locomotion state, animation state, and output values conceptually distinct. Raw device input should become gameplay intent; locomotion should own movement state; animation should derive presentation state; reported speed or velocity should have a single defined source.
- Do not let multiple update loops or components independently assign the same state, transform, speed, velocity, or animator value without an explicit arbitration design.
- Preserve unrelated gameplay behavior, timing, collision semantics, inspector defaults, and public contracts when making a focused fix.
- Before changing or removing a public field, property, method, or serialized field, inspect all C# references and relevant scenes, prefabs, and `.asset` files. Renaming a serialized field requires an intentional migration strategy; do not assume source compilation proves serialized data is safe.
- Follow the existing source style unless the approved change includes a style migration. Keep editor-only APIs out of runtime assemblies.

## Maintainable Architecture

- Prefer small modules with one clearly defined responsibility. Use composition instead of growing controller classes that contain unrelated behavior.
- Separate input collection, gameplay intent, state resolution, movement calculation, animation output, and skill execution, even when nearby code coordinates those concerns.
- Give each mutable gameplay value one authoritative owner. Other systems should read the value or submit requests through a narrow public API or explicit interface.
- Do not allow multiple components to independently write movement speed, velocity, grounded state, locomotion state, or animation state.
- Avoid hidden dependencies, global mutable state, and assumptions about Unity execution order. Make required dependencies and sequencing explicit.
- Do not perform broad rewrites unless repository evidence shows that an incremental correction would remain unsafe. Obtain approval for any required architectural update before implementation.

## State-Machine Design

- Model mutually exclusive gameplay modes as explicit states when practical instead of representing them as combinations of loosely related booleans.
- Document state ownership, valid transitions, transition guards, and state priority where these are not self-evident from a small local implementation.
- Treat input as transient player intent, not persistent gameplay state.
- Resolve simultaneous action requests deterministically with explicit priority or arbitration rules.
- Do not let grounded locomotion, airborne locomotion, crouch, sprint, skills, knockback, and disabled or dead states compete as independent writers.
- Centralize transition logic in one authoritative state resolver or route all transition requests through one.

## Debuggability

- Make important state transitions observable through focused diagnostics or editor-visible state where useful.
- Use names that distinguish raw input, requested intent or state, resolved state, calculated output, final output, and configuration.
- Optional diagnostic messages should identify the previous state, requested transition, resulting state, and any rejection reason without spamming normal runtime output.
- Do not leave temporary debug output enabled in production paths.
- When fixing a bug, record the root cause and the invariant that prevents recurrence in the change artifacts, regression test, or other durable documentation appropriate to the scope.

## Comments And Documentation

- Comments should explain rationale, constraints, invariants, contracts, state transitions, or non-obvious behavior. Do not restate obvious syntax or readable code.
- Keep comments close to the code they describe, and update or remove them when behavior changes.
- Use XML documentation for public APIs when consumers need contract, parameter, return-value, side-effect, or exception information.
- Document shared-input behavior when one physical input can request different or multiple actions depending on state.
- Remove execution-order dependencies where practical; document them only when they cannot be removed.
- TODO comments must state a concrete reason and intended follow-up. Do not add vague TODOs.

## Testability

- Keep state resolution and movement calculations separate from Unity input and `MonoBehaviour` lifecycle methods where practical.
- Prefer pure functions or independently testable classes for rules such as state priority, speed selection, transition validity, and skill eligibility.
- For every behavioral fix, add automated regression coverage where practical or describe a regression scenario that would fail before the fix and pass afterward.
- Cover boundary transitions such as grounded to airborne, airborne to grounded, input pressed during transitions, and multiple simultaneous requests when relevant.
- Do not claim automated, Unity Editor, or Play Mode testing unless it was actually performed.

## Change Quality

Before implementation:

- Identify the current authoritative owners of affected state.
- Identify every writer and reader of values being changed.
- Establish the root cause instead of patching only the visible symptom.
- Decide whether the smallest safe change is local or requires an approved architectural update.

Before completion:

- Verify that the change introduced no duplicate state or output writers.
- Verify that naming distinguishes input, requested state, resolved state, calculated output, and final output.
- Verify that comments describe intent, constraints, or invariants rather than obvious implementation.
- Report architectural debt that remains outside the approved scope.

## Validation And Reporting

- Run the complete non-destructive repository tooling validation when repository tooling, OpenSpec workflows, or contributor policy changes:

  ```bash
  node scripts/validate-repository-tooling.mjs
  ```

- After changing any repository-local OpenSpec command or skill, also run:

  ```bash
  node scripts/check-openspec-workflow-drift.mjs
  ```

- Validate an affected OpenSpec change with:

  ```bash
  openspec validate <change-name> --strict
  ```

- Always run the Git whitespace/error check before completion:

  ```bash
  git diff --check
  ```

- Repository validation must be non-destructive. Compare `git status --short` before and after commands and investigate unexpected Unity changes.
- No repository script substitutes for Unity Editor validation. For Unity changes, report the required Editor checks, such as opening the project with the recorded Unity version, confirming script compilation and a clean Console, inspecting affected serialized objects, and exercising relevant behavior in Play Mode.
- The completion report must list changed files, validations performed and their outcomes, validations not performed, remaining risks, and any required Unity Editor or Play Mode tests. Never describe an unperformed check as passing.
