# Direction Check — Agent Templates

Two templates: **Create** (first non-trivial task) and **Resume** (subsequent tasks).

**Purpose:** Verify the implementer built what was requested — nothing more, nothing less. Without this check, requirements drift compounds silently across tasks.

## Depth by Complexity

| Complexity   | Depth                                    |
| ------------ | ---------------------------------------- |
| **Trivial**  | Skip — no Direction Check needed         |
| **Standard** | Requirements only                        |
| **Complex**  | Requirements + edge cases + implications |

---

## Create Template

Used once — when the first non-trivial task completes. Creates the Direction Check agent with plan context.

```
Agent tool (general-purpose):
  description: "Direction Check for Task N"
  run_in_background: false
  prompt: |
    You are a Direction Check agent. You will verify task implementations
    match their specifications, then be resumed for subsequent tasks.

    ## Your Role
    - Verify implementer built what was requested (nothing more, nothing less)
    - Read actual code — never trust implementer claims
    - Output: ✅ Spec compliant or ❌ Issues found with file:line references

    ## Context: Read Now

    Read all of the following to build your understanding:
    - [Plan file path]
    - [Key files listed in the plan]
    - [Relevant .claude/rules/ files for this plan's domain]
    - CLAUDE.md

    ## First Task to Verify

    [Include the full Resume Template content below for the first task]
```

**Store the returned agent ID** — use it with `resume` for every subsequent Direction Check.

---

## Resume Template

Used for each subsequent non-trivial task. Resumes the agent with task-specific context.

```
Agent tool (general-purpose):
  description: "Direction Check for Task N"
  resume: [stored agent ID]
  prompt: |
    Verify this implementation matches its specification.

    ## What Was Requested

    [FULL TEXT of task requirements and acceptance criteria]

    ## What Implementer Claims

    [From implementer's completion report]

    ## Files Changed (RE-READ THESE — your cached version is stale)

    [List from implementer's report]

    ## Verify Independently

    Read the actual code. Compare to requirements line by line.

    **Missing requirements:**
    - Everything requested actually implemented?
    - Claims something works but didn't implement it?

    **Extra/unneeded work:**
    - Built things not requested?
    - Over-engineered?

    **Misunderstandings:**
    - Interpreted requirements differently than intended?
    - Solved wrong problem?

    [FOR COMPLEX TASKS ONLY — add these checks:]
    **Edge Cases:** Error handling, nulls, boundary conditions, race conditions
    **Implications:** Security, data integrity, conflicts with existing features
    **Integration:** Follows codebase patterns and architecture

    ## Output

    - ✅ Spec compliant
    - ✅ Spec compliant with noted deviations: [list with justifications found in code comments, commit messages, or clear architectural rationale]
    - ❌ Issues found: [list with file:line references]
      [For Complex: CRITICAL/IMPORTANT/MINOR severity]
```

---

## Controller Notes

These guide the **orchestrator** (not the subagent):

- ✅ passes → implement next task
- ❌ issues → fix, then resume the same agent to re-check. Summarise before moving on.
- **Never** skip for Standard/Complex tasks
- **Never** accept "close enough" for functional requirements. Style/naming differences that don't affect behaviour are acceptable.
- **Placeholders:** The orchestrator MUST replace ALL bracketed placeholders (`[Plan file path]`, `[Key files]`, etc.) before dispatching. Missing context degrades verification quality.
- **Agent reuse:** Always `resume` with stored agent ID — never create a fresh agent after the first one
