---
name: Radberi-PlanExecute
description: Use when executing an implementation plan produced by Radberi-PlanWrite, or when resuming plan execution after a break. Trigger when the user says "execute the plan", "start implementing", approves a plan, or references a previously written plan to begin work on.
---

**After context compaction:** Re-read this skill AND `./spec-reviewer-prompt.md` before continuing.

# Session-Based Plan Execution

Execute plan tasks sequentially, with **Direction Checks** verifying each non-trivial task and **Production Readiness + Code Simplification** reviews at the end.

Direction Checks exist because without independent verification, implementers tend to silently miss requirements or over-engineer. The subagent reads actual code and compares it to the spec — catching drift early before it compounds across tasks.

## ⚠️ STOP — If You Catch Yourself Doing These

- ❌ Completing a non-trivial task without dispatching a Direction Check
- ❌ Claiming "all tasks complete" without Production Readiness review
- ❌ Skipping sub-agent dispatch because "it's faster"
- ❌ Telling the user the plan is "complete" without updating the related Backlog task or GitHub issue
- ❌ Ignoring out-of-scope issues found by reviewers — flag them to the user for backlog or immediate fix

## The Workflow

```
1. Setup
   └─ Locate plan file (from plan mode context or conversation)
   └─ Read plan → Extract tasks with complexity tiers
   └─ Create TodoWrite with all tasks
   └─ Identify first non-trivial task (for Direction Check agent creation)

2. Per Task
   └─ Implement task (invoke required skills)
   └─ Trivial tasks: Skip Direction Check
   └─ Standard/Complex tasks:
       └─ First time: Launch Direction Check agent (see ./spec-reviewer-prompt.md)
       └─ Subsequent: Resume stored agent ID
       └─ If issues: fix → re-check. Summarise issues and fixes.
       └─ If passes: next task.

3. Completion (after all tasks)
   └─ Launch Production Readiness agent with full files-changed list
   └─ If issues: fix → re-review. Summarise issues and fixes.
   └─ If passes: Resume same agent for Code Simplification pass
   └─ Present suggestions → User confirms → Apply
   └─ Invoke Radberi-FinishingDevelopmentBranch
```

## Review Agents — Warm on First Use

Pre-warming agents at setup caused timeouts on longer plans. Instead, create each agent when first needed and reuse it via `resume` for the rest of the session.

### Direction Check Agent

- **Created**: After the first non-trivial task completes
- **Prompt**: See `./spec-reviewer-prompt.md` — Pre-Warm + Resume templates
- **Reused**: `resume` with stored agent ID for every subsequent non-trivial task. This preserves the agent's understanding of the plan and codebase across tasks.
- **Store the agent ID** after first creation

### Production Readiness + Code Simplification Agent (`Radberi-ProductionReadiness`)

- **Created**: After all implementation tasks complete
- **First pass**: Production Readiness review (security, performance, reliability, maintainability)
- **Second pass**: Resume same agent for Code Simplification
- **Out-of-scope findings**: Existing bugs or gaps discovered during review should be flagged to the user — add to backlog or implement immediately if trivial

## Direction Check Depth

Complexity tiers are defined in PlanWrite. The depth table and check criteria live in `./spec-reviewer-prompt.md` — the orchestrator does not need to duplicate them here.

- **Trivial**: Skip entirely
- **Standard**: Requirements verification only
- **Complex**: Requirements + edge cases + security/data integrity implications

**Trivial Batching:** When multiple contiguous Trivial tasks appear, execute them all as a batch. Report completion as a group.

## Commit Cadence

Commit after each task passes its Direction Check (or after a trivial batch completes). This creates clean rollback points — if a later task goes wrong, earlier work is safely preserved.

## Progress Reporting

After each task (or trivial batch), briefly report to the user what was completed and what's next. Keep it to 1-2 sentences — enough to track progress without interrupting flow.

## Failure Handling

**Task failure:** Retry once with a targeted fix → if still fails, escalate to user.

**Review failure (Direction Check or Production Readiness):**
1. Invoke `Radberi-ReceivingCodeReview` to evaluate the feedback — verify each suggestion against codebase reality, apply YAGNI checks, push back on technically incorrect suggestions
2. Fix verified issues (defer suggestions that fail YAGNI or verification)
3. Resume the same review agent to re-check
4. Repeat until it passes
5. Summarise what was found, what was fixed, and what was deferred

## Integration

**Required skills:**
- `Radberi-PlanWrite` — Creates the plan
- `Radberi-ProductionReadiness` — Production Readiness + Code Simplification (two-pass agent)
- `Radberi-ReceivingCodeReview` — Evaluate review feedback before acting on it
- `Radberi-FinishingDevelopmentBranch` — Completion
