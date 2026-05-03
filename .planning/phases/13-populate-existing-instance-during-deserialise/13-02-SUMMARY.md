---
phase: 13-populate-existing-instance-during-deserialise
plan: "02"
subsystem: deserialisation
tags: [populate, tdd-green, implementation, config-overlay]

dependency-graph:
  requires:
    - "13-01-SUMMARY.md (Populate<T> public stubs and 13 RED-state HumlPopulateTests)"
    - "12-03-SUMMARY.md (HumlConverter<T>, ConverterCache, PropertyDescriptor infrastructure)"
  provides:
    - "HumlDeserializer.Populate<T> — fully implemented entry point (struct guard, null guard, parse, delegate)"
    - "HumlDeserializer.PopulateMappingEntries — full overlay loop with converter dispatch, init-only/read-only guards"
    - "All 13 HumlPopulateTests GREEN across net8.0, net9.0, net10.0"
  affects:
    - "src/Huml.Net/Serialization/HumlDeserializer.cs"

tech-stack:
  added: []
  patterns:
    - "TDD GREEN phase: replaced NotSupportedException stubs with production implementations"
    - "Structural copy of DeserializeMappingEntries loop for PopulateMappingEntries with sync comment"
    - "Struct guard before null guard before parsing (typeof(T).IsValueType checked first)"
    - "Replace semantics for collections via fresh DeserializeNode allocation assigned via SetValue"

key-files:
  created: []
  modified:
    - src/Huml.Net/Serialization/HumlDeserializer.cs

decisions:
  - "Struct guard fires before null guard (POP-08 requirement: struct check fires even before null check)"
  - "Manual null check used (if (existing is null) throw new ArgumentNullException(nameof(existing))) — ArgumentNullException.ThrowIfNull not available on netstandard2.1"
  - "PopulateMappingEntries is a separate method (not merged into DeserializeMappingEntries) — keeps single responsibility clear"
  - "Sync comment added twice (method header + above converter dispatch block) to alert maintainers"
  - "Replace semantics for collections naturally achieved: DeserializeNode returns new objects, SetValue overwrites the property"

metrics:
  duration: "12m"
  completed: "2026-05-03"
  tasks-completed: 2
  tasks-total: 2
  files-modified: 1
  files-created: 0
---

# Phase 13 Plan 02: Populate Existing Instance — GREEN Implementation Summary

**One-liner:** `HumlDeserializer.Populate<T>` and `PopulateMappingEntries` fully implemented; all 13 HumlPopulateTests driven from RED to GREEN with struct/null guards, O(1) property lookup, full converter priority chain, and replace semantics for collections.

## Tasks Completed

| # | Name | Commit | Key Files |
|---|------|--------|-----------|
| 1 | Implement Populate<T> entry point with struct and null guards | a54024e | src/Huml.Net/Serialization/HumlDeserializer.cs |
| 2 | Implement PopulateMappingEntries — drive all 13 tests to GREEN | 4b7a4b7 | src/Huml.Net/Serialization/HumlDeserializer.cs |

## Verification Results

- `dotnet build --configuration Release` across all 4 TFMs (netstandard2.1, net8.0, net9.0, net10.0) → Build succeeded, 0 Warning(s), 0 Error(s)
- `dotnet test --filter "FullyQualifiedName~HumlPopulateTests"` across net8.0, net9.0, net10.0 → 13 passed, 0 failed on every TFM
- `dotnet test --framework net10.0` → 462 passed, 8 failed (all 8 are pre-existing worktree submodule failures: fixture/v0.1 and v0.2 not initialised in worktree — not regressions from this plan)
- `grep -v "^/" src/Huml.Net/Serialization/HumlDeserializer.cs | grep -c "NotImplementedException"` → 0 (no remaining stubs)
- `grep -c "SetValue(existing" src/Huml.Net/Serialization/HumlDeserializer.cs` → 1 (in PopulateMappingEntries)
- `grep -c "keep in sync with DeserializeMappingEntries" src/Huml.Net/Serialization/HumlDeserializer.cs` → 2 (method header + converter dispatch block)
- `grep -c "public static void Populate" src/Huml.Net/Huml.cs` → 2 (string + span overloads)
- `grep -c "[Fact]" tests/Huml.Net.Tests/Serialization/HumlPopulateTests.cs` → 13

## Deviations from Plan

None — plan executed exactly as written.

## TDD Gate Compliance

This plan is the GREEN phase of the two-plan TDD structure for Phase 13.

| Gate | Commit | Status |
|------|--------|--------|
| RED (test commit) | b4e82ec (Plan 01) | PASSED — 13 tests added, all failing with NotSupportedException |
| GREEN (feat commit) | a54024e + 4b7a4b7 | PASSED — all 13 tests pass on net8.0, net9.0, net10.0 |
| REFACTOR | — | Not required — implementation is clean; sync comments serve the maintenance concern |

## Known Stubs

None. All NotSupportedException stubs have been replaced with production implementations.

## Threat Flags

No new trust boundaries or security-relevant surfaces introduced. The `PopulateMappingEntries` method:
- Only writes to properties declared on the target type (STRIDE T-13-06: no escalation path)
- Filtered through PropertyDescriptor.GetLookup — [HumlIgnore] properties absent from lookup
- Guarded by IsInitOnly and SetMethod checks before any reflection write
- Parser validates input before any property writes (T-13-05: MaxRecursionDepth protection inherited)

## Self-Check: PASSED

- `src/Huml.Net/Serialization/HumlDeserializer.cs` — FOUND (contains Populate<T> and PopulateMappingEntries implementations)
- Commit a54024e — FOUND
- Commit 4b7a4b7 — FOUND
- All 13 HumlPopulateTests pass on all TFMs — VERIFIED
- Zero warnings on all 4 TFMs — VERIFIED
- No NotImplementedException or NotSupportedException stubs remaining — VERIFIED
