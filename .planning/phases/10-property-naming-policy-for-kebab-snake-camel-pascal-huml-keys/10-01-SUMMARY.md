---
phase: 10-property-naming-policy-for-kebab-snake-camel-pascal-huml-keys
plan: "01"
subsystem: serialization
tags: [naming-policy, property-descriptor, huml-options, tdd]
dependency_graph:
  requires:
    - Phase 07.14 (PropertyDescriptorCache with ByKey dictionary)
    - Phase 06 (HumlOptions, PropertyDescriptor, HumlPropertyAttribute)
  provides:
    - HumlNamingPolicy abstract class with KebabCase/SnakeCase/CamelCase/PascalCase singletons
    - HumlOptions.PropertyNamingPolicy property
    - (Type, HumlNamingPolicy?) compound cache key in PropertyDescriptor
  affects:
    - Phase 10-02 (wires policy into HumlSerializer/HumlDeserializer call sites)
tech_stack:
  added:
    - HumlNamingPolicy (new public abstract class in Huml.Net.Serialization)
  patterns:
    - TDD Red/Green: test file first, then implementation
    - Singleton static properties for four built-in policies
    - State-machine char-by-char separator algorithm (each uppercase = word boundary)
    - (Type, HumlNamingPolicy?) value-tuple cache key for policy-aware memoisation
key_files:
  created:
    - src/Huml.Net/Serialization/HumlNamingPolicy.cs
    - tests/Huml.Net.Tests/Serialization/HumlNamingPolicyTests.cs
  modified:
    - src/Huml.Net/Versioning/HumlOptions.cs
    - src/Huml.Net/Serialization/PropertyDescriptor.cs
    - tests/Huml.Net.Tests/Serialization/PropertyDescriptorTests.cs
    - tests/Huml.Net.Tests/Versioning/HumlOptionsTests.cs
decisions:
  - "Separate() algorithm treats every uppercase as its own word (prevWasAlpha=true triggers separator before any uppercase) — matches STJ-equivalent intent where URL -> u-r-l; the plan's original two-flag algorithm (prevWasLower || prevWasUpper&&nextIsLower) produced 'url' for all-caps inputs, not 'u-r-l'"
  - "HumlNamingPolicy XML doc uses <c>HumlOptions.PropertyNamingPolicy</c> plain text instead of <see cref=...> for PropertyNamingPolicy to avoid CS1574 forward-reference compile error since the property is added in the same plan"
  - "(Type, HumlNamingPolicy?) value-tuple as ConcurrentDictionary key uses struct equality — HumlNamingPolicy instances are singletons so reference equality gives correct cache partitioning"
metrics:
  duration: 5 minutes
  completed: 2026-05-02
  tasks_completed: 2
  files_modified: 6
---

# Phase 10 Plan 01: HumlNamingPolicy and PropertyDescriptor Cache Key Summary

HumlNamingPolicy abstract class with four singleton implementations (KebabCase, SnakeCase, CamelCase, PascalCase), PropertyNamingPolicy added to HumlOptions, and PropertyDescriptor cache key changed from Type to (Type, HumlNamingPolicy?) for policy-aware memoisation.

## Tasks Completed

| # | Task | Commit | Result |
|---|------|--------|--------|
| 1 (RED) | HumlNamingPolicyTests — 28 failing tests | 12b4b91 | All RED confirmed (CS0246 compile error) |
| 1 (GREEN) | HumlNamingPolicy.cs implementation | bf078dc | 28/28 PASS |
| 2 (RED) | PropertyDescriptorTests + HumlOptionsTests new methods | 6b24982 | RED confirmed (CS1501 + CS1061) |
| 2 (GREEN) | HumlOptions + PropertyDescriptor updates | ac001c8 | 392/400 PASS (8 fixture submodule pre-existing) |

## Key Decisions

1. **Algorithm fix (Rule 1 - Bug):** The plan's `Separate()` state machine (two flags: `prevWasLower`, `prevWasUpper&&nextIsLower`) produced `"url"` for input `"URL"` instead of `"u-r-l"`. Root cause: in all-uppercase sequences, `prevWasLower` is never set so no separator is inserted before the second or third uppercase character. Fixed to: insert separator before any uppercase when `sb.Length > 0 && prevWasAlpha` — a single flag that is true after any alphanumeric character. This correctly produces `u-r-l`, `profile-u-r-l`, `x-m-l-reader` per the plan's conversion table.

2. **XML doc forward reference:** The class-level `<remarks>` originally used `<see cref="Huml.Net.Versioning.HumlOptions.PropertyNamingPolicy"/>` which triggers CS1574 (could not resolve) because `PropertyNamingPolicy` doesn't exist at the time `HumlNamingPolicy.cs` is compiled in Task 1. Changed to `<c>HumlOptions.PropertyNamingPolicy</c>` plain text to avoid the error. When Task 2 adds the property, a full `<see cref>` could be restored, but the plain-text form is safe and clear.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Separate() all-uppercase acronym algorithm**

- **Found during:** Task 1, GREEN phase (tests KebabCase_splits_acronym_URL_to_u_r_l and KebabCase_converts_ProfileURL_to_profile_u_r_l failed)
- **Issue:** The plan's `Separate()` algorithm with two flags (`prevWasLower` and `prevWasUpper&&nextIsLower`) doesn't insert separators between consecutive uppercase letters unless the next character is lowercase. `URL` → `url`, `ProfileURL` → `profileurl`.
- **Fix:** Replaced two-flag approach with single `prevWasAlpha` flag. A separator is inserted before any uppercase character when `sb.Length > 0 && prevWasAlpha`. This matches the stated goal of treating each uppercase as its own word boundary.
- **Files modified:** `src/Huml.Net/Serialization/HumlNamingPolicy.cs`
- **Commit:** bf078dc

**2. [Rule 3 - Blocking] Removed forward-reference XML cref for PropertyNamingPolicy**

- **Found during:** Task 1, GREEN phase (CS1574 build error)
- **Issue:** `<see cref="Huml.Net.Versioning.HumlOptions.PropertyNamingPolicy"/>` in the HumlNamingPolicy class-level remarks resolved to a non-existent property (it's added in Task 2), causing `TreatWarningsAsErrors` to reject the build.
- **Fix:** Changed to `<c>HumlOptions.PropertyNamingPolicy</c>` plain text.
- **Files modified:** `src/Huml.Net/Serialization/HumlNamingPolicy.cs`
- **Commit:** bf078dc

## Verification

| Check | Result |
|-------|--------|
| `HumlNamingPolicy` class exists | PASS (grep returns 1) |
| Cache field is `ConcurrentDictionary<(Type, HumlNamingPolicy?)` | PASS (grep returns 1) |
| `HumlNamingPolicy? PropertyNamingPolicy` in HumlOptions | PASS (grep returns 1) |
| `policy?.ConvertName` in BuildDescriptors | PASS (grep returns 1) |
| `dotnet build -warnaserror` (net10.0) | PASS (0 warnings, 0 errors) |
| HumlNamingPolicyTests (28 tests) | PASS (28/28) |
| PropertyDescriptorTests (19 tests) | PASS (19/19) |
| HumlOptionsTests (23 tests) | PASS (23/23) |
| Full suite regression | 392/400 (8 pre-existing fixture submodule failures in worktree) |

## Known Stubs

None. All policy implementations are fully wired. PropertyNamingPolicy is stored on HumlOptions but not yet consumed by HumlSerializer/HumlDeserializer — that is intentional scope deferral to Plan 02, not a stub.

## Threat Flags

None. `ConvertName` operates on compile-time reflection metadata (PropertyInfo.Name), not user-supplied strings. No new network endpoints, auth paths, or file access patterns introduced.

## Self-Check: PASSED

Files exist:
- src/Huml.Net/Serialization/HumlNamingPolicy.cs - FOUND
- tests/Huml.Net.Tests/Serialization/HumlNamingPolicyTests.cs - FOUND

Commits exist:
- 12b4b91 - test(10-01): add failing tests for HumlNamingPolicy
- bf078dc - feat(10-01): implement HumlNamingPolicy
- 6b24982 - test(10-01): add failing tests for PropertyNamingPolicy
- ac001c8 - feat(10-01): add PropertyNamingPolicy to HumlOptions
