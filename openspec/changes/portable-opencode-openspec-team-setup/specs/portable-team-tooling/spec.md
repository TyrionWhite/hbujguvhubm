## ADDED Requirements

### Requirement: Repository-local project source of truth
Repository-local files SHALL be the source of truth for OpenSpec workflows, commands, skills, portable project permissions, validation, and contributor documentation. Provider configuration, model endpoints, credentials, authentication, Linux users, systemd services, and private network configuration MUST remain machine-managed outside the repository.

#### Scenario: Contributor opens a clean checkout
- **WHEN** a contributor opens a clean checkout with the documented OpenSpec and patched OpenCode prerequisites already available
- **THEN** the repository supplies its project workflows, permissions, validation, and contributor guidance without requiring contributor-specific copies of those project files

#### Scenario: Machine-managed configuration is required for provider access
- **WHEN** OpenCode requires provider, model, endpoint, credential, authentication, service, user, or network configuration
- **THEN** the repository treats that configuration as an external prerequisite and does not install, duplicate, or manage it

### Requirement: Portable and non-secret OpenCode configuration
The project-level `opencode.json` SHALL contain only portable project configuration and permissions and MUST NOT contain model endpoints, private addresses, passwords, API keys, authentication material, or machine-specific file dependencies. Repository tooling MUST NOT read, copy, expose the contents of, or depend on `/etc/opencode/team.json`.

#### Scenario: Project configuration is inspected
- **WHEN** a contributor or validation tool reads the project-level OpenCode configuration
- **THEN** it contains no provider endpoint, credential, private-network value, or reference that is required only on the originating machine

#### Scenario: Machine-managed team configuration is unavailable
- **WHEN** `/etc/opencode/team.json` is absent or inaccessible in an otherwise supported environment
- **THEN** repository setup validation can still verify all project-owned files without attempting to access that path

### Requirement: Current tool compatibility verification
The setup SHALL document OpenSpec 1.6.0 and the team-maintained patched OpenCode build as prerequisites and SHALL verify the versions or capabilities required by repository workflows without selecting, installing, upgrading, or replacing OpenCode.

#### Scenario: Current team tools are compatible
- **WHEN** validation runs with OpenSpec 1.6.0 and a patched OpenCode build that provides the documented capabilities
- **THEN** it reports the observed tool identities and confirms compatibility

#### Scenario: OpenCode capabilities are incompatible
- **WHEN** the installed OpenCode build lacks a capability required by project configuration, commands, or skills
- **THEN** validation fails with the missing capability and team remediation guidance without installing another release

### Requirement: Portable paths
Repository workflows SHALL resolve repository, change, artifact, and optional store locations from the active environment or OpenSpec CLI output and MUST NOT depend on absolute paths from a contributor's machine.

#### Scenario: Repository is cloned to a different path
- **WHEN** the setup is used from a checkout whose absolute path differs from the original authoring environment
- **THEN** all repository-local lifecycle actions resolve the same logical project configuration and artifacts

#### Scenario: Optional external store is not permitted
- **WHEN** a contributor explicitly selects an external OpenSpec store but the host denies external-directory access
- **THEN** the workflow stops with actionable permission guidance and does not broaden access automatically

### Requirement: Contributor and Pull Request guidance
The repository SHALL document prerequisites, lifecycle actions, validation, troubleshooting, and generated-workflow maintenance in `CONTRIBUTING.md`, and SHALL provide a Pull Request template that requests the relevant OpenSpec change, validation evidence, and scope confirmation.

#### Scenario: New contributor follows onboarding
- **WHEN** a contributor follows `CONTRIBUTING.md`
- **THEN** they can verify the current team tooling and identify every supported lifecycle action without consulting private machine configuration

#### Scenario: Contributor prepares a Pull Request
- **WHEN** a contributor later opens a Pull Request through an explicitly authorized workflow
- **THEN** the template prompts for the OpenSpec reference, checks performed, and whether Unity files are in scope without exposing credentials or private configuration

### Requirement: Non-destructive multi-user validation
The repository SHALL provide validation that checks required files, configuration syntax and boundaries, tool compatibility, lifecycle coverage, and command/skill drift without installing tools, changing files, invoking Unity, or performing repository publication operations.

#### Scenario: Validation runs in the working checkout
- **WHEN** a contributor runs the documented validation command
- **THEN** it reports pass or actionable failures and leaves repository and Unity files unchanged

#### Scenario: Validation runs under a second Linux user
- **WHEN** validation runs from a separate clean clone under a pre-existing second Linux user with documented prerequisites
- **THEN** it succeeds without depending on the first user's checkout paths, project configuration copies, credentials, or private values

#### Scenario: Setup contains forbidden machine-specific material
- **WHEN** validation detects a private address, credential-like field, model endpoint, API key, password, or dependency on machine-managed team configuration in project-owned files
- **THEN** it fails and identifies the affected project file without printing secret values
