---
phase: 11-enum-serialisation-and-deserialisation-support
plan: "01"
subsystem: serialization
tags: [enum, attribute, cache, tdd-red, contracts]
dependency_graph:
  requires: []
  provides:
    - HumlEnumValueAttribute (public sealed class, AttributeTargets.Field)
    - EnumNameCache (internal static, GetName/TryParse/ClearCache)
    - EnumSerializationTests (19 test stubs, RED state)
  affects:
    - src/Huml.Net/Serialization/Attributes/
    - src/Huml.Net/Serialization/
    - tests/Huml.Net.Tests/Serialization/
tech_stack:
  added: []
  patterns:
    - ConcurrentDictionary keyed by (Type, HumlNamingPolicy?) — same as PropertyDescriptor
    - GetOrAdd with static lambda to avoid closure allocation
    - Bidirectional enum name cache (ToHuml + FromHuml + FromHumlCI)
    - Attribute-wins-over-policy precedence (mirrors HumlProperty over PropertyNamingPolicy)
key_files:
  created:
    - src/Huml.Net/Serialization/Attributes/HumlEnumValueAttribute.cs
    - src/Huml.Net/Serialization/EnumNameCache.cs
    - tests/Huml.Net.Tests/Serialization/EnumSerializationTests.cs
  modified: []
decisions:
  - "XML doc cref 'Huml.Net.Exceptions.HumlSerializeException' requires short form 'Exceptions.HumlSerializeException' within the Huml.Net.Serialization namespace to resolve correctly (CS1574 fix)"
metrics:
  duration: "3m 7s"
  completed: "2026-05-02"
  tasks_completed: 3
  tasks_total: 3
  files_created: 3
  files_modified: 0
---

# Phase 11 Plan 01: Enum Serialisation Contracts Summary

**One-liner:** HumlEnumValueAttribute (field-level name override), EnumNameCache (bidirectional enum name cache keyed by (Type, HumlNamingPolicy?)), and 19 RED-state test stubs covering ENUM-SER-01..06, ENUM-DES-01..08, ENUM-RT-01..05.

## Tasks Completed

| # | Name | Commit | Files |
|---|------|--------|-------|
| 1 | Create HumlEnumValueAttribute.cs | d55148f | src/Huml.Net/Serialization/Attributes/HumlEnumValueAttribute.cs |
| 2 | Create EnumNameCache.cs | 08c1077 | src/Huml.Net/Serialization/EnumNameCache.cs |
| 3 | Create EnumSerializationTests.cs | 250a9e9 | tests/Huml.Net.Tests/Serialization/EnumSerializationTests.cs |

## Verification Results

```
dotnet build src/Huml.Net/Huml.Net.csproj → Build succeeded. 0 Warning(s), 0 Error(s) (all 4 TFMs)
dotnet build tests/Huml.Net.Tests/Huml.Net.Tests.csproj → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet test --filter "FullyQualifiedName~EnumSerialization" → Failed: 14, Passed: 5, Total: 19 (RED state expected)
dotnet test --filter "FullyQualifiedName!~EnumSerialization" → Passed: 802, Failed: 0 (no regressions)
```

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed XML doc cref path for HumlSerializeException**
- **Found during:** Task 2 (EnumNameCache.cs)
- **Issue:** XML doc comment used `<see cref="Huml.Net.Exceptions.HumlSerializeException"/>` which could not be resolved by the compiler from within the `Huml.Net.Serialization` namespace. CS1574 error caused a build failure (TreatWarningsAsErrors active).
- **Fix:** Changed to `<see cref="Exceptions.HumlSerializeException"/>` — the short relative namespace form that the compiler resolves correctly from the `Huml.Net.Serialization` namespace.
- **Files modified:** src/Huml.Net/Serialization/EnumNameCache.cs
- **Commit:** 08c1077 (included in Task 2 commit)

## Known Stubs

None. The three new files are the intended deliverable. EnumSerializationTests.cs tests are in RED state by design — Wave 2 (Plan 02) wires the serialiser and deserialiser branches to turn them GREEN.

## Key Decisions

1. XML doc `cref` for exception types in the same assembly must use the relative namespace path (`Exceptions.HumlSerializeException`), not the fully qualified path (`Huml.Net.Exceptions.HumlSerializeException`), when written from within a different child namespace (`Huml.Net.Serialization`).

## Threat Surface Scan

No new network endpoints, auth paths, file access patterns, or schema changes introduced. EnumNameCache operates on a closed, compile-time-defined set of enum members and cannot be externally injected. No threat flags raised.

## Self-Check: PASSED

Files exist:
- FOUND: src/Huml.Net/Serialization/Attributes/HumlEnumValueAttribute.cs
- FOUND: src/Huml.Net/Serialization/EnumNameCache.cs
- FOUND: tests/Huml.Net.Tests/Serialization/EnumSerializationTests.cs

Commits exist:
- FOUND: d55148f (feat(11-01): add HumlEnumValueAttribute for per-member enum name overrides)
- FOUND: 08c1077 (feat(11-01): add EnumNameCache bidirectional enum name cache)
- FOUND: 250a9e9 (test(11-01): add 19 enum serialization test stubs in RED state)
