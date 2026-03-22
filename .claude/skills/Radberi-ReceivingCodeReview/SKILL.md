---
name: Radberi-ReceivingCodeReview
description: Use BEFORE implementing ANY code review feedback — from humans, subagents, or GitHub reviewers. Requires technical rigor and verification, not performative agreement or blind implementation. Trigger this skill whenever you receive review comments, PR feedback, Direction Check results, or Production Readiness findings, regardless of whether the feedback seems correct or questionable.
---

# Code Review Reception

Code review requires technical evaluation, not emotional performance.

**Core principle:** Verify before implementing. Ask before assuming. Technical correctness over social comfort.

## The Response Pattern

```
WHEN receiving code review feedback:

1. READ: Complete feedback without reacting
2. UNDERSTAND: Restate requirement in own words (or ask)
3. VERIFY: Check against codebase reality
4. EVALUATE: Technically sound for THIS codebase?
5. RESPOND: Technical acknowledgment or reasoned pushback
6. IMPLEMENT: One item at a time, test each
```

## Forbidden Responses

**NEVER** use performative agreement — it wastes tokens and signals you haven't verified:
- "You're absolutely right!" / "Great point!" / "Excellent feedback!"
- "Let me implement that now" (before verification)
- "Thanks for catching that!" or ANY gratitude expression

**INSTEAD:** Restate the technical requirement, ask clarifying questions, push back with reasoning if wrong, or just start working (actions > words). When feedback IS correct: "Fixed. [Brief description]" or "Good catch — [specific issue]. Fixed in [location]." or just fix it silently.

If you pushed back and were wrong: "You were right — I checked [X] and it does [Y]. Implementing now." State the correction factually and move on. No apology, no defending the pushback.

## Handling Unclear Feedback

If ANY item is unclear, stop — do not implement anything yet. Items may be related, so partial understanding leads to wrong implementation. Clarify all unclear items before touching code. Once all items are understood, implement in the priority order described below.

## Source-Specific Handling

### From Your Human Partner
- **Trusted** — implement after understanding
- **Still ask** if scope unclear
- **Skip to action** or technical acknowledgment

### From External Reviewers (including subagents)

Before implementing, verify each suggestion:
1. Technically correct for THIS codebase?
2. Breaks existing functionality?
3. Reason for current implementation?
4. Does reviewer understand full context?

**Subagent-specific note:** Subagent reviewers (Direction Check, Production Readiness) have full codebase access but may lack conversation context about WHY a design decision was made. Focus verification on whether the suggestion accounts for design intent, not just code correctness.

If suggestion seems wrong — push back with technical reasoning.
If can't easily verify — say so: "I can't verify this without [X]. Should I investigate/ask/proceed?"
If conflicts with prior architectural decisions — stop and discuss with user first.

## YAGNI Check

When a reviewer suggests "implementing properly" or adding features:

```
grep codebase for actual usage

IF unused: "This endpoint isn't called. Remove it (YAGNI)?"
IF used: Then implement properly
```

The reviewer and you both serve the user. If a feature isn't needed, don't add it.

## Implementation Order

For multi-item feedback:
1. Clarify anything unclear FIRST
2. Implement in priority order: blocking issues (breaks, security) → simple fixes (typos, imports) → complex fixes (refactoring, logic)
3. Test each fix individually
4. Verify no regressions

## When To Push Back

Push back when:
- Suggestion breaks existing functionality
- Reviewer lacks full context
- Violates YAGNI (unused feature)
- Technically incorrect for this stack (.NET 10, C# 14, Blazor Server)
- Conflicts with architectural decisions (Clean Architecture layers, Result pattern, etc.)

How: Use technical reasoning, reference working tests/code, ask specific questions. Involve user if architectural.

**Signal if uncomfortable pushing back:** "Strange things are afoot at the Circle K"

## GitHub Thread Replies

When replying to inline review comments on GitHub, reply in the comment thread (`gh api repos/{owner}/{repo}/pulls/{pr}/comments/{id}/replies`), not as a top-level PR comment.

## The Bottom Line

**External feedback = suggestions to evaluate, not orders to follow.**

Verify. Question. Then implement. No performative agreement. Technical rigor always.
