---
phase: 06-attributes-and-serializer-deserializer
plan: 02
subsystem: serialization
tags: [serializer, reflection, tdd, scalar-types, collections, string-escaping, indentation]

# Dependency graph
requires:
  - phase: 06-01
    provides: PropertyDescriptor cache, HumlPropertyAttribute, HumlIgnoreAttribute, HumlSerializeException
  - phase: 05-parser
    provides: HumlOptions, HumlSpecVersion
provides:
  - HumlSerializer: internal static class converting .NET objects to HUML text
  - Serialize(object?, HumlOptions?) and typed overload Serialize(object?, Type, HumlOptions?)
affects:
  - 07-static-api: HumlSerializer.Serialize is the implementation backing Huml.Serialize<T>()

# Tech tracking
tech-stack:
  added: []
  patterns:
    - EmitEntry dispatches: scalar uses ': value\n', complex uses '::\n' then body
    - Empty collection sentinel: ':: []' for lists/arrays, ':: {}' for dicts
    - OmitIfDefault via Activator.CreateInstance(valueType) for default comparison
    - String escaping in backslash-first order to avoid double-escaping
    - IsScalarValue predicate drives inline vs block emission decision
    - IsUnsupportedType check for delegates/pointers before PropertyDescriptor lookup

key-files:
  created:
    - src/Huml.Net/Serialization/HumlSerializer.cs
    - tests/Huml.Net.Tests/Serialization/HumlSerializerTests.cs
  modified: []

key-decisions:
  - "HumlSerializer is internal static class (not public) -- Phase 7 public API wraps it; internal keeps public surface minimal"
  - "SerializeValue dispatches by exact CLR type, not interface, to ensure correct priority (string before IEnumerable, IDictionary before IEnumerable)"
  - "EmitEntry materializes IEnumerable into List<object?> once to check empty without double-enumerating"
  - "Unserializable types (delegates, pointers) throw HumlSerializeException with descriptive message"

patterns-established:
  - "HumlSerializer: internal static class in Huml.Net.Serialization namespace"
  - "Version string map: V0_2 -> 'v0.2.0', V0_1 -> 'v0.1.0' (suppress CS0618 locally)"
  - "Indent helper: new string(' ', depth * 2) -- two-space per depth level"

requirements-completed: [SER-03, SER-04]

# Metrics
duration: 39min
completed: 2026-03-21
---

# Phase 6 Plan 2: HumlSerializer Summary

**HumlSerializer: internal static class converting .NET objects to HUML text with PropertyDescriptor-driven declaration-order property emission, full scalar type support, string escaping, nested POCO/collection blocks, and two-space indentation**

## Performance

- **Duration:** 39 min
- **Started:** 2026-03-21T09:25:02Z
- **Completed:** 2026-03-21T10:03:52Z
- **Tasks:** 2 (RED + GREEN TDD cycle)
- **Files modified:** 2

## Accomplishments

- `HumlSerializer.cs` (395 lines): full serializer with scalar dispatch, collection/dict blocks, nested POCO mapping, string escaping, OmitIfDefault, empty sentinel literals
- `HumlSerializerTests.cs` (453 lines): 34 tests covering header emission, declaration order, all scalar types, string escaping, [HumlProperty] rename, [HumlIgnore] exclusion, OmitIfDefault, lists, arrays, dictionaries, nested POCOs, two-space indentation, delegate exception, Unix newlines, and typed overload
- Build succeeds with 0 warnings, 0 errors across net8.0/net9.0/net10.0
- 102 non-deserializer tests pass (34 new + 68 existing) on net10.0

## Task Commits

Each task was committed atomically:

1. **Task 1: RED -- failing tests for HumlSerializer** - `825bdc6` (test)
2. **Task 2: GREEN -- implement HumlSerializer** - `96fe085` (feat)

_TDD discipline: separate RED and GREEN commits per discipline._

## Files Created/Modified

- `src/Huml.Net/Serialization/HumlSerializer.cs` - Internal static HumlSerializer; Serialize entry points; SerializeValue dispatch; SerializeMappingBody; EmitEntry; SerializeSequenceBody; SerializeDictionaryBody; EscapeString; FormatDouble; IsScalarValue; IsUnsupportedType; Indent; GetDefaultValue
- `tests/Huml.Net.Tests/Serialization/HumlSerializerTests.cs` - 34 tests across all serializer behaviors

## Decisions Made

- `HumlSerializer` is `internal static` class -- Phase 7 public API wraps it; internal scope prevents premature public API surface.
- `SerializeValue` dispatches by exact CLR type in priority order: null, string, bool, integer types, double, float, decimal, IDictionary, IEnumerable, POCO. This ordering avoids ambiguity (string implements IEnumerable but should be quoted, not iterated).
- `EmitEntry` materializes `IEnumerable` into `List<object?>` before deciding empty vs non-empty to avoid double-enumeration of non-replayable sources.
- Version string mapping (V0_1 -> "v0.1.0") suppresses CS0618 locally -- caller of HumlOptions.SpecVersion must handle the deprecated enum value correctly.
- Empty collection sentinels `:: []` and `:: {}` are emitted in EmitEntry, not in the body methods, keeping body methods single-purpose.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

**Parallel agent crash (out of scope):** The parallel 06-03 deserializer agent's test suite includes a test that causes the xUnit test host process to crash when run alongside the serializer tests. This is not caused by any change in this plan. Running tests with an exclude filter confirms 102 passing, 0 failures for all non-deserializer tests.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `HumlSerializer` is ready for the Phase 7 static API (`Huml.Serialize<T>()`) to call
- `Serialize(object?, Type, HumlOptions?)` typed overload is provided for the Phase 7 entry point
- All serialization behaviors tested: round-trip with deserializer (plan 03) is possible

## Known Stubs

None - all properties are wired and functional.

## Self-Check: PASSED

Both created files exist on disk. Both commits (825bdc6, 96fe085) found in git log. 34 serializer tests compile and 102 total non-deserializer tests pass on net10.0.
