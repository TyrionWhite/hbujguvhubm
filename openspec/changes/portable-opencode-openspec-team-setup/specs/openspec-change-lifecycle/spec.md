## ADDED Requirements

### Requirement: Complete change lifecycle
The repository SHALL provide discoverable command and skill workflows for proposing a change, continuing incomplete artifacts, updating existing artifacts, applying tasks, syncing specifications, and archiving completed work.

#### Scenario: Contributor follows a change from idea to archive
- **WHEN** a contributor uses the documented lifecycle in order
- **THEN** each completed action identifies an available next action through archive without referencing a missing workflow

#### Scenario: Contributor resumes an incomplete change
- **WHEN** a change exists with one or more incomplete artifacts
- **THEN** the continue workflow reads status and creates only artifacts whose dependencies are satisfied until the requested readiness point is reached

### Requirement: CLI-derived artifact and store context
Every lifecycle workflow SHALL use OpenSpec status, instructions, and action context to select concrete artifact paths and SHALL preserve an explicitly selected registered store across store-aware commands.

#### Scenario: Schema uses non-default artifact names
- **WHEN** OpenSpec reports artifact identifiers or paths that differ from spec-driven defaults
- **THEN** the workflow reads and writes the CLI-reported artifacts rather than assumed filenames

#### Scenario: Registered store is selected
- **WHEN** the user names a registered OpenSpec store and access is available
- **THEN** every store-aware command in that action uses the resolved store identifier without recording machine-specific store paths in project files

### Requirement: Explicit planning and implementation boundary
Proposal, continue, and update workflows SHALL modify planning artifacts only. Apply SHALL require complete schema-defined context, announce its implementation scope, and mark each task complete only after the corresponding work is verified.

#### Scenario: Planning reaches apply-ready status
- **WHEN** proposal or continue completes all schema-required planning artifacts
- **THEN** it reports readiness without modifying implementation files, Unity files, task completion state, main specifications, or archive state

#### Scenario: Apply is requested before readiness
- **WHEN** a contributor requests implementation while required artifacts are incomplete
- **THEN** the workflow reports the blocking artifacts and does not edit implementation files

#### Scenario: Applied task has not been verified
- **WHEN** implementation for a task is incomplete or its required verification has not passed
- **THEN** apply leaves that task unchecked and reports the remaining work or blocker

### Requirement: Apply excludes repository publication and lifecycle finalization
Apply workflows MUST NOT commit, push, merge, open a Pull Request, sync main specifications, or archive a change. Those operations require separate explicit user actions outside apply.

#### Scenario: Apply completes all implementation tasks
- **WHEN** the final task is implemented and verified
- **THEN** apply reports completion and the available next actions without performing publication, sync, or archive operations

### Requirement: Explicit synchronization semantics
Sync SHALL be a separately requested planning action that validates delta specifications and reports implementation task state before modifying main specifications. Incomplete implementation tasks SHALL NOT by themselves block an explicit sync because synchronization does not assert implementation completion.

#### Scenario: Sync is requested with incomplete tasks
- **WHEN** valid delta specifications exist and implementation tasks remain incomplete
- **THEN** sync reports the incomplete task state and may update main specifications only because the user explicitly requested sync

#### Scenario: Sync validation fails
- **WHEN** delta specifications are invalid or their target capability cannot be resolved
- **THEN** sync stops before modifying main specifications and reports the validation error

### Requirement: Archive requires completed implementation
Archive SHALL be a separately requested action that stops when strict validation fails or implementation tasks remain incomplete.

#### Scenario: Incomplete change is submitted for archival
- **WHEN** validation fails or one or more implementation tasks remain incomplete
- **THEN** archive does not move the change and reports each blocking condition

#### Scenario: Completed change is archived
- **WHEN** all required artifacts and tasks are complete and strict validation succeeds
- **THEN** archive preserves the finalized planning artifacts and provides an auditable completed state

### Requirement: Preserve generated workflows unless change is justified
Existing OpenSpec-generated commands and skills SHALL remain unchanged unless implementation identifies a missing lifecycle action, a concrete incompatibility with the patched OpenCode build, a broken lifecycle reference, an unsafe mutation boundary, or drift against the repository's finite paired-workflow contract.

#### Scenario: Generated pair passes validation
- **WHEN** an existing command and skill pair satisfies supported-host and drift validation
- **THEN** implementation leaves that generated pair unchanged

#### Scenario: Generated workflow requires a targeted correction
- **WHEN** validation identifies a concrete missing action, incompatibility, broken reference, unsafe boundary, or contract mismatch
- **THEN** implementation changes only the affected workflow behavior and records the reason in reviewable repository history

### Requirement: Finite command and skill drift validation
Validation SHALL check paired lifecycle action coverage, store propagation, CLI-derived paths, readiness checks, mutation boundaries, supported host tool references, and next-action identifiers without claiming full semantic equivalence of Markdown instructions.

#### Scenario: Paired workflows satisfy the contract
- **WHEN** every command and skill pair contains the required finite contract elements
- **THEN** drift validation passes without rewriting either file

#### Scenario: Paired workflows diverge
- **WHEN** a command or skill omits or contradicts a required finite contract element
- **THEN** drift validation fails and identifies the action and contract element that differ
