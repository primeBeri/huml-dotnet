---
phase: 12-custom-converter-api
plan: 02
subsystem: serialization
tags: [converter, serializer, dispatch, humlconverter, green]

# Dependency graph
requires:
  - phase: 12-custom-converter-api
    plan: 01
    provides: HumlConverter, HumlConverter<T>, HumlSerializerContext, ConverterCache, PropertyDescriptor.Converter
provides:
  - SerializeValueInternal (extracted from SerializeValue with internal visibility)
  - Converter dispatch in SerializeValueInternal (type-level + options-level via ConverterCache.TryGet)
  - Converter dispatch in EmitEntry (property-level via converterOverride parameter)
  - ThreadStatic re-entry guard (_activeConverterTypes)
  - IsScalarValue(value, options) — converter-handled types treated as scalar
  - 13 CONV-* tests GREEN (CONV-REG-01..05, CONV-SER-01..05, CONV-ERR-01..03)
affects:
  - 12-03-PLAN.md (deserialiser wiring — CONV-DES-* and CONV-RT-* remain skipped)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "SerializeValue is now a thin wrapper over SerializeValueInternal (internal visibility for HumlSerializerContext)"
    - "Converter priority: property-level (EmitEntry converterOverride) > type-level [HumlConverter] > HumlOptions.Converters"
    - "[ThreadStatic] HashSet<Type> _activeConverterTypes re-entry guard prevents infinite recursion"
    - "IsScalarValue accepts optional HumlOptions; converter-handled types classified as scalar (inline after key:)"
    - "TaggedPointConverter separate from PointConverter — generic HumlConverter<T> casts to T, so each type needs its own converter"

key-files:
  modified:
    - src/Huml.Net/Serialization/HumlSerializer.cs
    - tests/Huml.Net.Tests/Serialization/HumlConverterTests.cs

key-decisions:
  - "SerializeValue renamed to thin wrapper: avoids changing all internal call sites while providing SerializeValueInternal for HumlSerializerContext.AppendSerializedValue"
  - "IsScalarValue updated to accept HumlOptions: converter-handled types must be classified as scalar so EmitEntry emits key: value (not key:: block)"
  - "AllDictionaryValuesAreScalar updated to forward options: consistent converter-awareness in inline dict detection"
  - "TaggedPointConverter added: HumlConverter<T>.WriteObject casts to T — TaggedPoint cannot be cast to Point, separate converter required for separate types"
  - "Re-entry guard uses [ThreadStatic] HashSet<Type> — thread-safe without locks; tracks both SerializeValueInternal and EmitEntry paths"

# Metrics
duration: 15min
completed: 2026-05-03
---

# Phase 12 Plan 02: Serialiser Converter Dispatch Summary

**SerializeValueInternal extracted from SerializeValue, converter dispatch wired at three priority levels (property > type-level > options), ThreadStatic re-entry guard added, and 13 CONV-* tests turned GREEN**

## Performance

- **Duration:** 15 min
- **Started:** 2026-05-02T23:16:45Z
- **Completed:** 2026-05-03T00:00:00Z (approx)
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Extracted `SerializeValueInternal` from `SerializeValue` as an `internal static` method so `HumlSerializerContext.AppendSerializedValue` can call it directly
- Added converter dispatch at top of `SerializeValueInternal`: `ConverterCache.TryGet(value.GetType(), options)` fires before all built-in dispatch for type-level `[HumlConverter]` and `HumlOptions.Converters`
- Extended `EmitEntry` with `HumlConverter? converterOverride` parameter for property-level converter dispatch (highest priority)
- Updated `SerializeMappingBody` to pass `desc.Converter` to `EmitEntry`
- Updated `IsScalarValue` to accept `HumlOptions?` and return `true` for converter-handled types (so they emit `key: value` inline, not `key::` block)
- Updated `AllDictionaryValuesAreScalar` to forward `options` for consistent converter-awareness
- Added `[ThreadStatic] HashSet<Type> _activeConverterTypes` re-entry guard in both `SerializeValueInternal` and `EmitEntry`
- Unskipped 13 tests: CONV-REG-01..05, CONV-SER-01..05, CONV-ERR-01..03 — all GREEN
- Added `TaggedPointConverter` helper type to test file (Rule 1 bug fix — see Deviations)
- Full suite: 836 passed, 9 skipped, 0 failed across net8.0/net9.0/net10.0

## Task Commits

Each task was committed atomically:

1. **Task 1: Extract SerializeValueInternal and add converter dispatch** - `7f81a9b` (feat)
2. **Task 2: Unskip CONV-SER-*, CONV-REG-01..05, CONV-ERR-01..03** - `9cf9f39` (feat)

## Files Created/Modified

- `src/Huml.Net/Serialization/HumlSerializer.cs` — SerializeValueInternal extracted, ConverterCache.TryGet dispatch, EmitEntry converterOverride, IsScalarValue options param, re-entry guard
- `tests/Huml.Net.Tests/Serialization/HumlConverterTests.cs` — 13 tests unskipped, TaggedPointConverter helper type added

## Exact Changes to HumlSerializer.cs

### SerializeValueInternal extraction

`SerializeValue` becomes a one-liner delegating to `SerializeValueInternal`:
```csharp
private static void SerializeValue(...) => SerializeValueInternal(...);
internal static void SerializeValueInternal(StringBuilder sb, object? value, int depth, HumlOptions options, Type? declaredType = null) { ... }
```

### Converter dispatch in SerializeValueInternal

Inserted immediately after null guard, before string check:
```csharp
if (ConverterCache.TryGet(value.GetType(), options) is { } converter)
{
    // re-entry guard + WriteObject call
}
```

### EmitEntry converterOverride parameter

```csharp
private static void EmitEntry(..., HumlConverter? converterOverride = null)
{
    if (converterOverride != null)
    {
        // re-entry guard + AppendKey + key: + WriteObject + \n
        return;
    }
    if (IsScalarValue(value, options)) { ... }
    ...
}
```

### SerializeMappingBody passes desc.Converter

```csharp
EmitEntry(sb, indent, desc.HumlKey, propValue, depth, options, desc.Inline, desc.Converter);
```

### IsScalarValue with options

```csharp
private static bool IsScalarValue(object? value, HumlOptions? options = null)
{
    // ... existing checks ...
    if (options != null && ConverterCache.TryGet(value.GetType(), options) != null) return true;
    return false;
}
```

### Re-entry guard

```csharp
[ThreadStatic]
private static HashSet<Type>? _activeConverterTypes;
```

## Tests Now Passing

| Test | Status |
|------|--------|
| CONV-REG-01: EmptyConverters_DoesNotAffectDefaultSerialisation | PASS |
| CONV-REG-02: PropertyLevel_HumlConverterAttribute_CachedInPropertyDescriptor | PASS |
| CONV-REG-03: TypeLevel_HumlConverterAttribute_UsedWhenTypeAppearsAsTarget | PASS |
| CONV-REG-04: Priority_PropertyLevel_WinsOverTypeLevel | PASS |
| CONV-REG-05: HumlConverterAttribute_WithNonHumlConverterType_ThrowsInvalidOperationException | PASS |
| CONV-SER-01: OptionsLevel_Converter_InvokedBeforeBuiltinDispatch | PASS |
| CONV-SER-02: AppendSerializedValue_UsesBuiltinDispatch | PASS |
| CONV-SER-03: AppendRaw_EmitsVerbatimFragment | PASS |
| CONV-SER-04: PropertyLevel_Converter_Write_InvokedForThatPropertyOnly | PASS |
| CONV-SER-05: TypeLevel_Converter_Write_InvokedForEveryOccurrence | PASS |
| CONV-ERR-01: ConverterWithNoParameterlessCtor_ThrowsInvalidOperationException | PASS |
| CONV-ERR-02: Converter_CanConvertFalse_NeverInvoked | PASS |
| CONV-ERR-03: FirstMatchWins_InConvertersList | PASS |

## Tests Remaining Skipped (Plan 12-03)

- CONV-DES-01..05 (deserialiser wiring)
- CONV-RT-01..04 (round-trip tests)

## Decisions Made

- **SerializeValue as thin wrapper:** All internal call sites already use `SerializeValue`. Making it a wrapper over `SerializeValueInternal` keeps the refactor minimal — no cascading changes needed to internal call sites.
- **IsScalarValue options param:** Converter-handled types must be classified as scalar; otherwise `EmitEntry` would try to emit them as `key::` POCO blocks. Making `options` optional (nullable default) preserves backward compatibility in non-option-aware call paths.
- **AllDictionaryValuesAreScalar updated:** Inline dict detection uses the same scalar predicate — must be converter-aware to correctly classify converter-handled dict values.
- **TaggedPointConverter added:** See Deviations section below.
- **Re-entry guard placement:** Both paths (SerializeValueInternal type-level dispatch and EmitEntry property-level dispatch) need the guard to prevent infinite recursion from either entry point.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Added TaggedPointConverter to fix type-level converter test failures**
- **Found during:** Task 2 (test run after unskipping)
- **Issue:** `TaggedPoint` had `[HumlConverter(typeof(PointConverter))]` but `PointConverter` is `HumlConverter<Point>`. Its `WriteObject` casts `value` to `(Point)` — `TaggedPoint` is a different type and is not castable. `InvalidCastException` thrown during `CONV-REG-03` and `CONV-SER-05`.
- **Fix:** Added `TaggedPointConverter : HumlConverter<TaggedPoint>` to the test file and changed `TaggedPoint`'s `[HumlConverter]` attribute to use `TaggedPointConverter`. Both converters emit the same `"X,Y"` format — behavior is identical. The mismatch was an error in the original test fixture stub design.
- **Files modified:** tests/Huml.Net.Tests/Serialization/HumlConverterTests.cs
- **Verification:** `dotnet test --filter "FullyQualifiedName~HumlConverterTests"` exits 0 with 13 passed, 9 skipped.
- **Committed in:** `9cf9f39` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 bug in test fixture)
**Impact:** All fixes required for tests to pass. No scope creep.

## Known Stubs

None — all 13 serialiser-side tests are GREEN. Remaining 9 skips (CONV-DES-*, CONV-RT-*) are intentionally deferred to Plan 12-03 (deserialiser wiring).

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: dos | src/Huml.Net/Serialization/HumlSerializer.cs | Re-entry guard (T-12-02, T-12-05) implemented: [ThreadStatic] HashSet<Type> _activeConverterTypes detects second entry for same type in both SerializeValueInternal and EmitEntry |

## Self-Check: PASSED

- `src/Huml.Net/Serialization/HumlSerializer.cs` exists: FOUND
- `tests/Huml.Net.Tests/Serialization/HumlConverterTests.cs` exists: FOUND
- Commit `7f81a9b` exists: FOUND
- Commit `9cf9f39` exists: FOUND
- `dotnet test` full suite: 836 passed, 9 skipped, 0 failed across all TFMs

---
*Phase: 12-custom-converter-api*
*Completed: 2026-05-03*
