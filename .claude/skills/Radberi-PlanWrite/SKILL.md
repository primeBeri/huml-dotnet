---
name: Radberi-PlanWrite
description: Use when planning multi-step implementation work — features, bugfixes, refactoring, or any task requiring multiple files or steps. Trigger when the user has requirements, specs, GitHub issues, backlog tasks, or describes a feature to build, before writing any code. Also use when the user says "plan this", "break this down", or asks how to approach a complex change.
---

# Writing Plans

## Recommended: Plan Mode

If not already in Plan Mode, suggest switching (`/plan`) for safety — it prevents accidental edits during research. Not required.

## Input Sources

Before planning, check for existing context that shapes the plan:

- **Backlog task**: `backlog search [keywords]` — read description, acceptance criteria, labels
- **GitHub issue**: read all info including screenshots/attachments
- **User conversation**: extract requirements from discussion history

Update these sources as you work: write plan summary on creation, set status to `In Progress` on start, add implementation notes on completion, add `Blocker` label with notes if blocked.

## Research & Discovery

- **Trace affected code**: identify files to modify, their callers, and downstream dependencies
- **Check existing tests**: understand current coverage to scope TDD work
- **Use Context7 MCP and Web Search** for up-to-date API/framework documentation
- **Ask clarifying questions** about scope, constraints, and success criteria

After research, suggest YAGNI simplifications where appropriate.

### Rules and Skills Review

Review `CLAUDE.md` and relevant `.claude/rules/` and `.claude/skills/rules-*` for architecture and code standards relevant to each task. Summarise these in each Task (per the [Task Format](#task-format) guidance) and link to the full rules.

See `references/skill-lookup.md` for the task-type to skill mapping.

## Plan Structure

### Required Plan Tasks

Every plan must include:

1. **Implementation tasks** — the core work
2. **Validation task** — run tests (and Playwright MCP manual checks for UI work), then commit
3. **Production Readiness & Simplification task** — invoke `Radberi-ProductionReadiness` agent (two passes: production readiness review, then code simplification with user approval)
4. **Rules Update task** — review if any `.claude/rules/` or `.claude/skills/rules-*` need updates based on work completed. This prevents knowledge rot — lessons learned during implementation should be captured for future sessions.

### Task Format

Each task includes **Complexity** and **Skills** lines. Complexity controls how deeply PlanExecute's Direction Check agent verifies the work (Trivial = skip, Standard = requirements only, Complex = requirements + edge cases + implications). Skills tell the executing agent which domain-specific guidance to load.

```md
**Task N:** [Description]
**Complexity:** Trivial | Standard | Complex
**Skills:** [comma-separated skill names or "None"]
**Description:** [TaskDescription, including file paths, relevant code snippets, and testing requirements]
**Acceptance Criteria:**
- [Conditions that must be met — these become the Direction Check's verification spec]...
```

### Complexity Tiers

| Tier | Criteria | Examples |
|------|----------|----------|
| **Trivial** | Single file, <10 lines, mechanical | Fix typo, config value |
| **Standard** | Clear spec, 1-3 files | Add validation, new component |
| **Complex** | Multiple files, architectural decisions | New feature, security changes |

**Default to Standard** if unsure. Err toward higher for security, public APIs, database changes.

### Example Plan

```md
> **Execution:** This plan is implemented via `Radberi-PlanExecute`.

**Task 1:** Add `ExportFormat` enum to Domain layer
**Complexity:** Trivial
**Skills:** None
**Description:** Create `SpaceHub.Domain/Enums/ExportFormat.cs` with `Csv`, `Excel` values.
**Acceptance Criteria:**
- Enum exists with both values
- File header present

**Task 2:** Implement `ExportRoomsQuery` handler
**Complexity:** Standard
**Skills:** Radberi-TDD
**Description:** Create query + handler in Application layer. Returns `Result<byte[]>`.
  See `GetRoomsQueryHandler` for patterns. Tests in `SpaceHub.Application.Tests`.
**Acceptance Criteria:**
- Failing test written first, then passing implementation
- Returns NotFound for invalid project
- Respects tenant filtering

**Task 3:** Add export button to Room Browser toolbar
**Complexity:** Standard
**Skills:** Radberi-TDD, rules-BlazorConfigureAwait
**Description:** Add `SfSplitButton` to toolbar in `RoomBrowser.razor`.
  Wire to `ExportRoomsQuery` via `ICircuitMediator`.
**Acceptance Criteria:**
- Button gated by `CanExportRooms` permission via `ProjectAuthorizeView`
- bUnit test verifies button renders/hides based on permission

**Task 4:** Validation — run all non-E2E tests, commit
**Task 5:** Production Readiness & Simplification — invoke `Radberi-ProductionReadiness` (two passes)
**Task 6:** Rules Update — review if any rules need updating
```

## Approval & Handoff

1. Display complete plan with exact file paths
2. Ask: "Does this plan look correct? Reply 'Approved' to proceed."
3. On approval, invoke `Skill(skill: "Radberi-PlanExecute")`
