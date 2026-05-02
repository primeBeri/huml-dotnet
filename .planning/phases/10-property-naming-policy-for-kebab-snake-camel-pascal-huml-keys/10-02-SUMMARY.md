---
phase: 10-property-naming-policy-for-kebab-snake-camel-pascal-huml-keys
plan: "02"
subsystem: serialization
tags: [naming-policy, deserializer, serializer, tdd, round-trip]
dependency_graph:
  requires:
    - Phase 10-01 (HumlNamingPolicy, PropertyNamingPolicy on HumlOptions, (Type, HumlNamingPolicy?) cache key)
  provides:
    - HumlSerializer wired to pass PropertyNamingPolicy to GetDescriptors
    - HumlDeserializer threaded with HumlOptions through all private recursive methods
    - GetLookup called with options.PropertyNamingPolicy in DeserializeMappingEntries
  affects:
    - All callers of Huml.Serialize / Huml.Deserialize that set PropertyNamingPolicy on HumlOptions
tech_stack:
  added: []
  patterns:
    - TDD Red/Green: test file first, then HumlSerializer, then HumlDeserializer
    - Options thread-through: null-coalesce at entry points, pass by value through recursive chain
key_files:
  created:
    - tests/Huml.Net.Tests/Serialization/NamingPolicyRoundTripTests.cs
  modified:
    - src/Huml.Net/Serialization/HumlSerializer.cs
    - src/Huml.Net/Serialization/HumlDeserializer.cs
decisions:
  - "Thread options through DeserializeDictionary even though the method does not itself use GetLookup — it recursively calls DeserializeNode which may encounter nested POCOs"
  - "CoerceScalar not changed — it has no recursive dispatch and no GetLookup call; adding options would be dead weight"
  - "IsStringKeyedDictionary not changed — pure type predicate, no options needed"
  - "null-coalesce pattern (opts = options ?? HumlOptions.Default) applied at entry points only — internal call chain uses non-null opts throughout"
metrics:
  duration: 4 minutes
  completed: 2026-05-02
  tasks_completed: 2
  files_modified: 3
---

# Phase 10 Plan 02: Wire PropertyNamingPolicy into HumlSerializer and HumlDeserializer Summary

End-to-end naming policy: HumlSerializer passes PropertyNamingPolicy to GetDescriptors (1 line), HumlDeserializer threads HumlOptions through all 4 private methods so GetLookup uses the same policy for symmetric round-trips.

## Tasks Completed

| # | Task | Commit | Result |
|---|------|--------|--------|
| 1 (RED) | NamingPolicyRoundTripTests.cs — 14 tests, 5 RED | ffed9f4 | 5 RED confirmed (serialize-only tests fail before wiring) |
| 1 (GREEN) | Wire HumlSerializer.SerializeMappingBody | 100dd20 | 9/14 PASS (7 serialize-only GREEN; round-trips still RED) |
| 2 (GREEN) | Thread HumlOptions through HumlDeserializer | 8331b21 | 14/14 PASS; 406/414 full suite (8 pre-existing fixture failures) |

## Key Decisions

1. **null-coalesce at entry points only:** Each of the 3 entry points in HumlDeserializer resolves `opts = options ?? HumlOptions.Default` once. This local is passed non-null through the entire recursive chain, eliminating repeated null-coalesce overhead on hot paths.

2. **CoerceScalar unchanged:** The scalar coercion method has no recursive DeserializeNode call and never calls GetLookup. Adding an `options` parameter would be dead weight. The plan explicitly noted this.

3. **DeserializeDictionary receives options:** Even though `DeserializeDictionary` itself doesn't call `GetLookup`, it recursively calls `DeserializeNode` which may encounter nested POCO values. The `options` parameter must flow through so nested POCOs resolve their properties using the same policy.

## Deviations from Plan

None — plan executed exactly as written.

## Verification

| Check | Result |
|-------|--------|
| `GetDescriptors` in HumlSerializer — 1 call, passes `options.PropertyNamingPolicy` | PASS |
| `GetLookup` in HumlDeserializer — 1 call, passes `options.PropertyNamingPolicy` | PASS |
| Private methods with `HumlOptions options` parameter — count 4 | PASS |
| NamingPolicyRoundTripTests (14 tests) all pass | PASS (14/14) |
| HumlDeserializerTests regression | PASS (no failures) |
| HumlStaticApiTests regression | PASS (no failures) |
| `dotnet build -warnaserror` (net10.0) | PASS (0 warnings, 0 errors) |
| Full suite net10.0 | 406/414 (8 pre-existing fixture submodule failures in worktree) |

## Known Stubs

None. All policies are fully wired end-to-end. A `Huml.Serialize` with `KebabCase` produces HUML text with kebab-case keys, and `Huml.Deserialize` with the same options maps those keys back to the correct properties.

## Threat Flags

None. This plan wires an in-process string transformation. No new network endpoints, auth paths, file access patterns, or external trust boundaries introduced. Per the plan's threat model, `ConvertName` operates on compile-time property names only.

## Self-Check: PASSED

Files exist:
- tests/Huml.Net.Tests/Serialization/NamingPolicyRoundTripTests.cs - FOUND
- src/Huml.Net/Serialization/HumlSerializer.cs - FOUND
- src/Huml.Net/Serialization/HumlDeserializer.cs - FOUND

Commits exist:
- ffed9f4 - test(10-02): add failing NamingPolicyRoundTripTests (14 tests, 5 RED)
- 100dd20 - feat(10-02): wire PropertyNamingPolicy into HumlSerializer.SerializeMappingBody
- 8331b21 - feat(10-02): thread HumlOptions through HumlDeserializer private methods
