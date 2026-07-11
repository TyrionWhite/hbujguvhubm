## Why

The repository's OpenCode and OpenSpec setup cannot yet be used consistently from a clean checkout: project-level configuration is not documented or validated, and the available OpenSpec commands do not cover a coherent end-to-end lifecycle. A narrow repository-local setup is needed while provider access, authentication, and host administration remain under the team's existing machine management.

## What Changes

- Add a portable project-level `opencode.json` for permissions and workflow discovery without model endpoints, credentials, private addresses, or machine-specific provider configuration.
- Document and verify compatibility with OpenSpec 1.6.0 and the current team-maintained patched OpenCode build without selecting or installing a public stable OpenCode release.
- Provide a complete and internally consistent OpenSpec command/skill lifecycle for proposing, continuing, updating, applying, syncing, and archiving changes.
- Change generated OpenSpec commands and skills only when a command is missing, the patched OpenCode build is incompatible, or validation demonstrates drift.
- Add repository-local OpenSpec configuration, contributor documentation, a Pull Request template, and non-destructive setup and drift validation.
- Verify the setup from a clean checkout and from a separate clone under a second Linux user while leaving Unity files unchanged.

## Capabilities

### New Capabilities

- `portable-team-tooling`: Repository-local installation, configuration, onboarding, and validation requirements for a reproducible OpenCode/OpenSpec setup.
- `openspec-change-lifecycle`: A complete, consistent set of OpenSpec workflows covering change creation through archival.

### Modified Capabilities

None. The repository has no existing capability specifications.

## Impact

The eventual implementation will affect root `opencode.json`, repository-local `.opencode` commands and skills, `openspec/config.yaml`, contributor documentation, a Pull Request template, and non-destructive validation tooling. Provider configuration, model endpoints, credentials, authentication, Linux users, systemd services, private network configuration, custom agents, agent handoff contracts, `AGENTS.md`, custom Unity policy skills, and all Unity project content are outside this change. Applying the change will not commit, push, merge, or open a Pull Request.
