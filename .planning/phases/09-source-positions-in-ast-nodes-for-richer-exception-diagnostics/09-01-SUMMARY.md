---
phase: 09-source-positions-in-ast-nodes-for-richer-exception-diagnostics
plan: "01"
subsystem: parser
tags:
  - csharp
  - records
  - ast
  - parser
  - source-positions
  - tdd
dependency_graph:
  requires: []
  provides:
    - HumlNode.Line and HumlNode.Column body properties (POS-01..POS-06, POS-09)
    - Position propagation from Token into every AST node in HumlParser
  affects:
    - src/Huml.Net/Parser/HumlNode.cs
    - src/Huml.Net/Parser/HumlParser.cs
    - tests/Huml.Net.Tests/Parser/HumlNodePositionTests.cs
tech_stack:
  added: []
  patterns:
    - Body-property equality exclusion via Equals(HumlNode?) override on abstract base record
    - Object-initialiser syntax for token position propagation on AST node construction
key_files:
  created:
    - tests/Huml.Net.Tests/Parser/HumlNodePositionTests.cs
  modified:
    - src/Huml.Net/Parser/HumlNode.cs
    - src/Huml.Net/Parser/HumlParser.cs
decisions:
  - "Overrode Equals(HumlNode?) and GetHashCode() on HumlNode to exclude Line/Column from equality — C# synthesized equality for derived records chains through base Equals, so body properties on the base ARE included without the override (corrects incorrect RESEARCH.md assumption)"
  - "Chose tk.Line for root HumlDocument Line (first non-version token's line, per plan)"
  - "Chose indicatorLine for nested HumlDocument (:: indicator's line) and HumlSequence (via ParseMultilineList/ParseVector)"
  - "Chose firstKeyLine for HumlInlineMapping (first key token's line)"
  - "Chose firstItemLine for inline HumlSequence (ParseInlineList first value token line)"
  - "Fixed inline dict test inputs: HUML uses 'key:: a: 1, b: 2' syntax; braces only for empty '{}'"
metrics:
  duration: "9 minutes"
  completed: "2026-05-02"
  tasks_completed: 3
  files_changed: 3
---

# Phase 09 Plan 01: Source Positions in AST Nodes (HumlNode + HumlParser) Summary

**One-liner:** Body-declared `Line`/`Column` int properties with Equals override on HumlNode abstract record, propagated from Token into all 20+ AST node construction sites in HumlParser.

## What Was Built

Three tasks executed following TDD discipline:

1. **Task 1 (RED):** Created `HumlNodePositionTests.cs` with 25 `[Fact]` tests covering POS-01..POS-06 and POS-09. File failed to compile intentionally (`.Line`/`.Column` not yet on `HumlNode`).

2. **Task 2 (GREEN — foundation):** Added `int Line { get; init; }` and `int Column { get; init; }` as body properties on the `HumlNode` abstract record. Overrode `Equals(HumlNode?)` and `GetHashCode()` to exclude these from equality. Default-value (POS-09) and equality-preservation (POS-06) tests turned green.

3. **Task 3 (GREEN — propagation):** Updated every AST node construction site in `HumlParser.cs` with object-initialiser `{ Line = ..., Column = ... }` syntax. Changed `ParseMultilineDict` and `ParseMultilineList` signatures to accept `indicatorLine`. All 25 position tests green.

## Files Modified

| File | Change | Lines |
|------|--------|-------|
| `src/Huml.Net/Parser/HumlNode.cs` | Added Line/Column body props + Equals/GetHashCode override | +33, -1 |
| `src/Huml.Net/Parser/HumlParser.cs` | Position propagation at all construction sites; method signature changes | +66, -38 |
| `tests/Huml.Net.Tests/Parser/HumlNodePositionTests.cs` | New file — 25 position tests | +305 |

## Test Count Delta

- Before: 0 HumlNodePositionTests
- After: 25 HumlNodePositionTests (all green)
- Pre-existing tests: all unaffected (HumlNodeTests: 26, HumlParserTests, HumlDeserializerTests, etc. — no regressions)

## Equality Preservation (POS-06) Confirmation

The body-property approach from the research doc is CORRECT in concept but required an implementation adjustment. C# synthesized equality for derived records chains to the base `Equals()`, which means body properties on the base abstract record ARE included without intervention. Resolution: override `virtual bool Equals(HumlNode? other)` to return `EqualityContract == other.EqualityContract` (type-only match), and `GetHashCode()` to return `EqualityContract.GetHashCode()`. Derived record synthesized equality calls base `Equals()` for the inherited portion, then checks its own primary constructor parameters — so the override correctly excludes `Line`/`Column` while preserving structural equality on `Kind`, `Value`, `Key`, `Entries`, `Items`.

## Open Question Resolutions

- **HumlDocument position:** Root document uses `tk.Line` (first non-version token's line). Nested multiline dict blocks use `indicatorLine` passed from `ParseVector`. ✓
- **HumlSequence position:** Both multiline (from `ParseMultilineList`) and inline (from `ParseInlineList`) use the indicator line / first item line respectively. ✓
- **HumlInlineMapping position:** First key token's line and column, captured before the parse loop. ✓
- **Inline HumlSequence:** Uses `firstItemLine`/`firstItemColumn` captured before the loop in `ParseInlineList`. ✓

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] C# record equality chains through base Equals — body properties NOT automatically excluded**

- **Found during:** Task 2 verification (POS-06 equality tests failing)
- **Issue:** The research/patterns docs stated that body-declared `{ get; init; }` properties on an abstract base record are excluded from synthesized equality of derived records. This is INCORRECT for inheritance chains: the derived record calls `base.Equals()` which includes ALL base body properties. Verified empirically.
- **Fix:** Override `virtual bool Equals(HumlNode? other)` and `GetHashCode()` on `HumlNode` to return type-only equality, explicitly excluding `Line`/`Column`.
- **Files modified:** `src/Huml.Net/Parser/HumlNode.cs`
- **Commit:** d1844d7

**2. [Rule 1 - Bug] Test inputs used invalid HUML inline dict syntax with braces**

- **Found during:** Task 3 test run
- **Issue:** Two tests in `HumlNodePositionTests.cs` used `"obj:: { a: 1 }"` and `"items:: { a: 1, b: 2 }"`. HUML lexer only accepts `{}` as empty dict token; non-empty inline dicts use `key:: a: 1, b: 2` syntax (no braces).
- **Fix:** Changed test inputs to `"obj:: a: 1"` and `"items:: a: 1, b: 2"`.
- **Files modified:** `tests/Huml.Net.Tests/Parser/HumlNodePositionTests.cs`
- **Commit:** 7492ee7 (included in Task 3 commit)

## Known Stubs

None — all AST node construction sites propagate real token positions. HumlDeserializer `line: 0` call sites are intentionally out of scope for Plan 01 (they are Plan 02's responsibility, per the plan's `<done>` criteria).

## Threat Flags

None — this plan introduces no new network endpoints, auth paths, file access patterns, or schema changes at trust boundaries. Position metadata flows from existing `Token.Line`/`Token.Column` (LEX-02, present since Phase 3) into new body properties on AST nodes.

## Self-Check: PASSED

| Item | Status |
|------|--------|
| `src/Huml.Net/Parser/HumlNode.cs` | FOUND |
| `src/Huml.Net/Parser/HumlParser.cs` | FOUND |
| `tests/Huml.Net.Tests/Parser/HumlNodePositionTests.cs` | FOUND |
| `.planning/phases/09-.../09-01-SUMMARY.md` | FOUND |
| Commit 500e8bc (test RED) | FOUND |
| Commit d1844d7 (feat HumlNode) | FOUND |
| Commit 7492ee7 (feat HumlParser propagation) | FOUND |
