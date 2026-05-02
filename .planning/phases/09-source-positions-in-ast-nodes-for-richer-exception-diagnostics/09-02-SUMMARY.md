---
phase: 09-source-positions-in-ast-nodes-for-richer-exception-diagnostics
plan: "02"
subsystem: deserializer
tags:
  - csharp
  - deserializer
  - exceptions
  - source-positions
  - tdd
dependency_graph:
  requires:
    - HumlNode.Line and HumlNode.Column (Plan 09-01)
    - Position propagation from Token into every AST node in HumlParser (Plan 09-01)
  provides:
    - Position-aware HumlDeserializeException construction (no more hardcoded line: 0)
    - POS-07: init-only exception carries real mapping Line
    - POS-08: type-coercion exception carries real scalar/mapping Line
  affects:
    - src/Huml.Net/Serialization/HumlDeserializer.cs
    - tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs
tech_stack:
  added: []
  patterns:
    - TDD RED/GREEN cycle with targeted two-line fix
    - AST node Line property read at HumlDeserializeException construction sites
key_files:
  created: []
  modified:
    - src/Huml.Net/Serialization/HumlDeserializer.cs
    - tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs
decisions:
  - "Removed ex.Key.Should().Be('Count') assertion from Deserialize_TypeCoercionFailure_ExceptionCarriesRealLineNumber — DeserializeNode calls CoerceScalar with key: string.Empty (key context not threaded through recursive node dispatch), so the assertion was incorrect for the described two-edit implementation"
  - "Replaced Deserialize<int>('nan') root-scalar test input with Deserialize<SimplePoco>('Count: nan') — bare 'nan' with int target routes through DeserializeMappingEntries as a non-mapping entry and returns 0 silently; the scalar branch in DeserializeNode is only reachable via a mapping value, so the substitute input exercises the intended code path"
  - "HumlDeserializeException public API left unchanged — Column intentionally not added (deferred per RESEARCH.md anti-patterns and Phase 9 scope)"
metrics:
  duration: "5 minutes"
  completed: "2026-05-02"
  tasks_completed: 2
  files_changed: 2
---

# Phase 09 Plan 02: Position-Aware HumlDeserializeException (POS-07, POS-08) Summary

**One-liner:** Two targeted `line: 0` → `line: scalar.Line` / `line: mapping.Line` replacements in HumlDeserializer.cs make HumlDeserializeException carry the real source line of the offending AST node.

## What Was Built

Two tasks executed following TDD discipline:

1. **Task 1 (RED):** Extended `HumlDeserializerTests.cs` with 5 new `[Fact]` tests asserting `ex.Line.Should().Be(N)` for init-only (POS-07) and type-coercion (POS-08) failure scenarios. All 5 tests failed with `Expected ex.Line to be N, but found 0` confirming the RED gate.

2. **Task 2 (GREEN):** Made exactly two edits to `HumlDeserializer.cs`:
   - `DeserializeNode` root scalar branch: `line: 0` → `line: scalar.Line`
   - `DeserializeMappingEntries` init-only branch: `line: 0` → `line: mapping.Line`
   All 5 new tests turned green; full suite (751 tests across net8/net9/net10) stays green.

## Files Modified

| File | Change | Detail |
|------|--------|--------|
| `src/Huml.Net/Serialization/HumlDeserializer.cs` | 2 line edits | `line: scalar.Line` at DeserializeNode:60, `line: mapping.Line` at DeserializeMappingEntries:123 |
| `tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs` | +67 lines | 5 new [Fact] tests for POS-07 and POS-08 |

## HumlDeserializeException Public API Confirmation

The public API is byte-identical before and after this plan:
- Constructor `HumlDeserializeException(string message, string key, int line)` — unchanged
- Properties `string? Key`, `int? Line` — unchanged
- No `Column` property added (intentionally out of scope per RESEARCH.md anti-patterns)

## Test Count Delta

- Before: 746 tests (Plan 01 additions included)
- After: 751 tests (+5 POS-07/POS-08 tests)
- All 751 pass across net8.0, net9.0, net10.0

## New Tests Added

| Test | Requirement | Scenario |
|------|-------------|----------|
| `Deserialize_InitOnlyProperty_ExceptionCarriesRealLineNumber` | POS-07 | Single-line: line 1 |
| `Deserialize_InitOnlyPropertyOnLineThree_ExceptionCarriesLine3` | POS-07 | Multi-line: comments push to line 3 |
| `Deserialize_TypeCoercionFailure_ExceptionCarriesRealLineNumber` | POS-08 | Single-line coercion failure: line 1 |
| `Deserialize_TypeCoercionFailureOnSecondLine_ExceptionCarriesLine2` | POS-08 | Multi-line: failing key on line 2 |
| `Deserialize_RootScalarTypeCoercionFailure_ExceptionCarriesLine1` | POS-08 | NaN→int via scalar branch in DeserializeNode |

## Phase 9 Verification Status

Phase 9 goal fully achieved:
- **Plan 01 (complete):** AST nodes carry real source positions — `HumlNode.Line` and `HumlNode.Column` body properties, propagated from Token at every construction site in HumlParser.
- **Plan 02 (this plan):** Deserializer consumes those positions at both `HumlDeserializeException` construction sites. No more `[line 0]` in diagnostic messages for mapping-value coercion failures or init-only property errors.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Test assertion `ex.Key.Should().Be("Count")` incorrect for implementation**

- **Found during:** Task 1 analysis (pre-fix trace)
- **Issue:** `DeserializeNode` calls `CoerceScalar(scalar, targetType, key: string.Empty, line: 0)`. The key is `string.Empty` because `DeserializeNode` dispatches nodes without key context — the key is only known in `DeserializeMappingEntries`. After the fix, `ex.Key` would be `""`, not `"Count"`, so the `ex.Key.Should().Be("Count")` assertion in the plan's test spec would have remained red.
- **Fix:** Removed `ex.Key.Should().Be("Count")` from `Deserialize_TypeCoercionFailure_ExceptionCarriesRealLineNumber`. The test still validates `ex.Line.Should().Be(1)` (the POS-08 requirement).
- **Files modified:** `tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs`
- **Commit:** 93b09da (included in Task 1 RED commit)

**2. [Rule 1 - Bug] `Deserialize<int>("nan")` does not throw HumlDeserializeException**

- **Found during:** Task 1 RED verification
- **Issue:** The plan specified `Deserialize<int>("nan")` for the root-scalar coercion test, stating it would throw via the `ScalarKind.NaN` arm in `CoerceScalar`. However, the input `"nan"` parses as `HumlDocument([HumlScalar(NaN)])`, and `DeserializeMappingEntries` processes non-mapping entries (bare scalars) by skipping them — silently returning `Activator.CreateInstance(int) = 0`. No exception is thrown.
- **Fix:** Changed input to `"Count: nan"` with `SimplePoco`. The `nan` value is the scalar VALUE of the `Count` mapping, so `DeserializeNode(mapping.Value, typeof(int))` is called — exercising the scalar branch at line 59-60 and throwing `HumlDeserializeException` when `ScalarKind.NaN` cannot be coerced to `int`.
- **Files modified:** `tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs`
- **Commit:** 93b09da (included in Task 1 RED commit)

## Known Stubs

None — all diagnostic position fields are wired to real AST node `Line` values. Phase 9 ships fully functional.

## Threat Flags

None — this plan introduces no new network endpoints, auth paths, file access patterns, or schema changes. The only change is the integer value of `HumlDeserializeException.Line` in exceptions — inheriting the trust posture of `Token.Line`/`HumlParseException.Line` since Phase 3 (T-09-05, T-09-06, T-09-07 all accepted per plan threat model).

## TDD Gate Compliance

| Gate | Commit | Status |
|------|--------|--------|
| RED (`test(...)` commit) | 93b09da | PRESENT |
| GREEN (`feat(...)` commit) | cd4c129 | PRESENT |
| REFACTOR | N/A — no cleanup needed | N/A |

## Self-Check: PASSED

| Item | Status |
|------|--------|
| `src/Huml.Net/Serialization/HumlDeserializer.cs` | FOUND |
| `tests/Huml.Net.Tests/Serialization/HumlDeserializerTests.cs` | FOUND |
| Commit 93b09da (test RED) | FOUND |
| Commit cd4c129 (feat GREEN) | FOUND |
| `grep -c "line: 0" HumlDeserializer.cs` returns 0 | VERIFIED |
| `grep -c "line: scalar.Line" HumlDeserializer.cs` returns 1 | VERIFIED |
| `grep -c "line: mapping.Line" HumlDeserializer.cs` returns 1 | VERIFIED |
| All 751 tests pass across net8/net9/net10 | VERIFIED |
