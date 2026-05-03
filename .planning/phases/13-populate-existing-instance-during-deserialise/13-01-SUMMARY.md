---
phase: 13-populate-existing-instance-during-deserialise
plan: "01"
subsystem: deserialisation
tags: [populate, tdd-red, api, stubs]

dependency-graph:
  requires:
    - "12-03-SUMMARY.md (HumlConverter<T> infrastructure — PropertyDescriptor, ConverterCache)"
  provides:
    - "Huml.Populate<T>(string, T, HumlOptions?) — public void overload (stub)"
    - "Huml.Populate<T>(ReadOnlySpan<char>, T, HumlOptions?) — public void overload (stub)"
    - "HumlDeserializer.Populate<T> — internal entry point stub"
    - "HumlDeserializer.PopulateMappingEntries — private overlay stub"
    - "HumlPopulateTests.cs — 13 RED-state test methods for POP-01..POP-13"
  affects: []

tech-stack:
  added: []
  patterns:
    - "TDD RED phase: stubs throw NotSupportedException (NotImplementedException blocked by MA0025)"
    - "Two-overload string/span API duality mirrors existing Deserialize<T> pattern"
    - "Test file uses PropertyDescriptor.ClearCache() + ConverterCache.ClearCache() in constructor"

key-files:
  created:
    - tests/Huml.Net.Tests/Serialization/HumlPopulateTests.cs
  modified:
    - src/Huml.Net/Huml.cs
    - src/Huml.Net/Serialization/HumlDeserializer.cs

decisions:
  - "NotSupportedException used instead of NotImplementedException for stubs (MA0025 analyser blocks NotImplementedException)"
  - "Ambiguous Activator.CreateInstance cref removed from PopulateMappingEntries XML doc to avoid CS0419"
  - "13 tests cover POP-01..POP-13; POP-14 (XML docs) is verified by zero CS1591 warnings at build time"

metrics:
  duration: "3m 7s"
  completed: "2026-05-03"
  tasks-completed: 2
  tasks-total: 2
  files-modified: 2
  files-created: 1
---

# Phase 13 Plan 01: Populate Existing Instance — Contract Stubs and RED Tests Summary

**One-liner:** Two `void Populate<T>` overloads added to `Huml.cs` and `HumlDeserializer.cs` with `NotSupportedException` stubs; 13 RED-state tests in `HumlPopulateTests.cs` lock the POP-01..POP-13 API contracts.

## Tasks Completed

| # | Name | Commit | Key Files |
|---|------|--------|-----------|
| 1 | Add Populate<T> public stubs to Huml.cs and internal stubs to HumlDeserializer.cs | 4f33203 | src/Huml.Net/Huml.cs, src/Huml.Net/Serialization/HumlDeserializer.cs |
| 2 | Create HumlPopulateTests.cs with RED-state test stubs for all POP-* requirements | b4e82ec | tests/Huml.Net.Tests/Serialization/HumlPopulateTests.cs |

## Verification Results

- `dotnet build` → Build succeeded, 0 warnings, 0 errors across all 4 TFMs
- `dotnet test --filter "FullyQualifiedName~HumlPopulateTests"` → 13 tests, all FAILED (RED state — NotSupportedException from stubs)
- `dotnet test --framework net10.0` → 845 passed, 13 failed (no regressions; only the new Populate tests fail)
- `grep -c "public static void Populate" src/Huml.Net/Huml.cs` → 2
- `grep -c "\[Fact\]" tests/Huml.Net.Tests/Serialization/HumlPopulateTests.cs` → 13
- `grep "FluentAssertions"` in test file → no match (AwesomeAssertions only)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Used NotSupportedException instead of NotImplementedException for stubs**
- **Found during:** Task 1 (first build attempt)
- **Issue:** Meziantou analyser rule MA0025 blocks `NotImplementedException` in production code — build failed with errors on all TFMs
- **Fix:** Changed both stubs to throw `NotSupportedException` instead of `NotImplementedException`, which satisfies MA0025 ("Implement the functionality or raise NotSupportedException or PlatformNotSupportedException")
- **Files modified:** src/Huml.Net/Serialization/HumlDeserializer.cs
- **Commit:** 4f33203

**2. [Rule 1 - Bug] Removed ambiguous Activator.CreateInstance cref from XML docs**
- **Found during:** Task 1 (first build attempt)
- **Issue:** `<see cref="Activator.CreateInstance"/>` caused CS0419 on net10.0 (ambiguous overload reference)
- **Fix:** Replaced the cref with plain prose ("Does not construct a new instance") to avoid the ambiguity
- **Files modified:** src/Huml.Net/Serialization/HumlDeserializer.cs
- **Commit:** 4f33203

## Known Stubs

The following stubs are intentional — this is a TDD RED-phase plan. Plan 02 will implement them:

| Stub | File | Line | Reason |
|------|------|------|--------|
| `HumlDeserializer.Populate<T>` | src/Huml.Net/Serialization/HumlDeserializer.cs | ~68 | RED state: throws NotSupportedException until Plan 02 implements it |
| `HumlDeserializer.PopulateMappingEntries` | src/Huml.Net/Serialization/HumlDeserializer.cs | ~79 | RED state: throws NotSupportedException until Plan 02 implements it |

These stubs do NOT prevent the plan's goal (locking the API contract and establishing RED-state tests). Plan 02 is responsible for driving to GREEN.

## TDD Gate Compliance

This plan is the RED phase of the two-plan TDD structure for Phase 13.

| Gate | Commit | Status |
|------|--------|--------|
| RED (test commit) | b4e82ec | PASSED — 13 tests added, all failing with NotSupportedException |
| GREEN (feat commit) | — | Deferred to Plan 02 |
| REFACTOR | — | Deferred to Plan 02 |

## Threat Flags

No new trust boundaries or security-relevant surfaces introduced by stubs. The `Populate<T>` public entry points accept untrusted `huml` strings — same surface as `Deserialize<T>` which already has `MaxRecursionDepth` protection. No new attack surface.

## Self-Check: PASSED

- `src/Huml.Net/Huml.cs` — FOUND (contains 2 public Populate<T> overloads)
- `src/Huml.Net/Serialization/HumlDeserializer.cs` — FOUND (contains Populate<T> and PopulateMappingEntries stubs)
- `tests/Huml.Net.Tests/Serialization/HumlPopulateTests.cs` — FOUND (13 [Fact] methods)
- Commit 4f33203 — FOUND
- Commit b4e82ec — FOUND
