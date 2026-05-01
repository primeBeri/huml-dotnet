# Documentation Strategy for Huml.Net First Public Release

## Context

Huml.Net is approaching its first NuGet release. The current README is a 2-line placeholder. There are no docs, no CONTRIBUTING.md, no CHANGELOG, and no public backlog. This plan creates the full documentation suite needed for a credible open-source launch.

**Key constraint:** `README.md` is packed into the `.nupkg` (line 16 of `Huml.Net.csproj`), so it must be self-contained for nuget.org readers. Links to `docs/` must use absolute GitHub URLs.

---

## File Structure

```
huml-dotnet/
  README.md                    -- Rewrite (main entry point, also in NuGet package)
  CHANGELOG.md                 -- Keep a Changelog format
  CONTRIBUTING.md              -- Contribution guidelines (AI-friendly)
  BACKLOG.md                   -- Public backlog + issue triage workflow
  docs/
    serialisation.md           -- Consumer: serialisation guide (type mapping, attributes)
    deserialisation.md         -- Consumer: deserialisation guide (supported types, errors)
    versioning.md              -- Consumer: version handling (HumlOptions, support window)
    ast-and-parse-api.md       -- Consumer: low-level AST / Parse API guide
    internals/
      pipeline.md              -- Developer: end-to-end pipeline overview (Lexer -> Parser -> AST -> Ser/Deser)
      version-gates.md         -- Developer: how version gates work + how to add a new spec version
      extending.md             -- Developer: how to add new AST nodes, token types, type mappings
```

**11 files total.** 4 at root, 4 consumer docs in `docs/`, 3 developer docs in `docs/internals/`.

---

## Implementation Order

### Consumer docs (steps 1-8)

| Step | File | Why this order |
|------|------|----------------|
| 1 | `README.md` | Blocks NuGet publish. Highest visibility. |
| 2 | `CHANGELOG.md` | Required for release -- users expect release notes. |
| 3 | `CONTRIBUTING.md` | Must exist before repo goes public. |
| 4 | `BACKLOG.md` | Sets expectations for issue -> backlog workflow. |
| 5 | `docs/serialisation.md` | First thing users look up after quick-start. |
| 6 | `docs/deserialisation.md` | Natural follow-up. |
| 7 | `docs/versioning.md` | Important but less urgent for new users. |
| 8 | `docs/ast-and-parse-api.md` | Advanced use case, lowest urgency. |

### Developer docs (steps 9-11)

| Step | File | Why this order |
|------|------|----------------|
| 9 | `docs/internals/pipeline.md` | Core orientation for contributors -- the "start here" doc. |
| 10 | `docs/internals/version-gates.md` | Most common extension scenario -- adding a new spec version. |
| 11 | `docs/internals/extending.md` | Reference for adding new AST nodes, token types, type mappings. |

---

## Step 1: README.md

**Badges row** (top):
- NuGet version badge
- NuGet downloads badge
- CI status badge (`ci.yml`)
- Licence badge (MIT)

**Sections:**

1. **Title + one-liner** -- "Huml.Net -- the first-party .NET implementation of HUML"
2. **What is HUML?** -- 2-3 sentences. Links to [huml.io](https://huml.io) (spec) and [github.com/huml-lang](https://github.com/huml-lang) (organisation, sibling implementations: `go-huml`, `huml-rs`). **Include a small HUML syntax sample** so readers see the format without leaving the page (e.g. a `ServerConfig` document showing scalars, a nested block, and a list).
3. **What is Huml.Net?** -- First-party .NET implementation. Zero runtime deps. Targets `netstandard2.1` / `net8.0` / `net9.0` / `net10.0`. API modelled on `System.Text.Json`.
4. **Installation** -- `dotnet add package Huml.Net` + PackageReference XML
5. **Quick Start** -- Three focused examples:
   - Serialise a POCO -> HUML string (show both the C# and the HUML output)
   - Deserialise HUML string -> typed object
   - Parse to AST for validation
   - Each with a small DTO (e.g. `ServerConfig`) and the corresponding HUML output
6. **System.Text.Json Comparison** -- Table:

   | System.Text.Json | Huml.Net |
   |---|---|
   | `JsonSerializer.Serialize<T>()` | `Huml.Serialize<T>()` |
   | `JsonSerializer.Deserialize<T>()` | `Huml.Deserialize<T>()` |
   | `JsonSerializerOptions` | `HumlOptions` |
   | `JsonDocument.Parse()` | `Huml.Parse()` |
   | `[JsonPropertyName]` | `[HumlProperty]` |
   | `[JsonIgnore]` | `[HumlIgnore]` |

7. **Documentation** -- Two subsections:
   - **For library consumers** -- Bulleted links to the four `docs/` guides (absolute GitHub URLs)
   - **For contributors** -- Bulleted links to the three `docs/internals/` guides
8. **Supported Frameworks** -- `netstandard2.1`, `net8.0`, `net9.0`, `net10.0`
9. **Contributing** -- Brief paragraph, link to `CONTRIBUTING.md`
10. **Licence** -- MIT, link to `LICENSE`

**Critical files:** `src/Huml.Net/Huml.cs` (public API signatures must match examples exactly)

---

## Step 2: CHANGELOG.md

[Keep a Changelog](https://keepachangelog.com/) format with SemVer.

- `[Unreleased]` section at top
- `[0.1.0-preview] - YYYY-MM-DD` entry listing all "Added" items for the initial pre-release:
  - Static facade API
  - v0.1 + v0.2 spec support with auto-detection
  - Serialisation with attributes
  - Deserialisation to POCOs, arrays, `List<T>`, `Dictionary<string, T>`
  - AST access
  - Version handling options
  - Multi-target framework support
  - Zero runtime dependencies
  - Full fixture suite coverage

Date left as placeholder -- filled at tag time.

---

## Step 3: CONTRIBUTING.md

Sections:

1. **Welcome** -- Contributions welcome, including AI-assisted ones
2. **Getting Started** -- Fork, clone with `--recurse-submodules`, branch from `main`
3. **Development Setup** -- .NET 8/9/10 SDKs, `dotnet build`, `dotnet test`
4. **Code Standards** -- C# 13, `.editorconfig`, `TreatWarningsAsErrors`
5. **Testing Requirements**:
   - xUnit v3 (3.2.2)
   - **AwesomeAssertions only** (never FluentAssertions)
   - Must pass both fixture suites (v0.1 and v0.2)
   - Must pass on all three target frameworks (net8.0/9.0/10.0)
6. **Constraints** -- No external runtime deps in core. Linting belongs in future `Huml.Net.Linting`.
7. **AI-Assisted Contributions** -- Explicitly welcome, with conditions:
   - Must be human-reviewed before submission
   - Must pass full test stack
   - Must pass both fixture suites
   - Submitted as PR for consideration
8. **Understanding the Codebase** -- Link to `docs/internals/` with one-line descriptions of each guide (pipeline overview, version gates, extending the library). This is how CONTRIBUTING.md bridges to the developer docs.
9. **Pull Request Process** -- Fork -> branch -> PR against `main`. Describe what and why. Link issues.

---

## Step 4: BACKLOG.md

Sections:

1. **Purpose** -- Public roadmap visibility
2. **How It Works**:
   - Users report bugs / request features via GitHub Issues
   - Maintainer triages and promotes accepted items to this backlog
   - Internal planning (GSD) is not publicly exposed
   - This file provides transparency into planned/in-progress/done work
3. **Backlog** -- Categorised markdown table with columns: **Category** (Bug / Feature / Enhancement), **Item**, **Issue**, **Status** (Planned / In Progress / Done)
   - Items grouped by category
   - Each row links to its GitHub Issue
   - Initial content: empty table with a note that the backlog will be populated as issues are triaged after the first public release

---

## Step 5: docs/serialisation.md

1. **Overview** -- `Huml.Serialize<T>()` and `Huml.Serialize(object?, Type)` entry points
2. **Type Mapping Table** -- Complete .NET -> HUML mapping (string, bool, int, long, float, double, decimal, NaN, infinity, null, collections, POCOs, dictionaries, empty collections)
3. **Property Order** -- Declaration order, base-class first, `MetadataToken` within each type
4. **Attributes** -- `[HumlProperty("name")]`, `[HumlProperty(OmitIfDefault = true)]`, `[HumlIgnore]` with code examples
5. **Version Directive** -- `%HUML vX.Y.Z` header emission
6. **Scalar Formatting** -- String escaping rules
7. **Error Handling** -- `HumlSerializeException` for unsupported types

**Critical files:** `src/Huml.Net/Serialization/HumlSerializer.cs`

---

## Step 6: docs/deserialisation.md

1. **Overview** -- Three overloads including `ReadOnlySpan<char>`
2. **Supported Target Types** -- Scalars, nullable types, collections (`T[]`, `List<T>`, `IEnumerable<T>`), dictionaries, POCOs
3. **POCO Mapping Rules** -- Case-sensitive key matching, `[HumlProperty]` renaming, unknown keys skipped, `init`-only rejected
4. **Scalar Coercion** -- ScalarKind -> .NET type mapping
5. **Error Handling** -- `HumlParseException` (syntax) vs `HumlDeserializeException` (mapping), with diagnostic properties
6. **Limitations** -- No polymorphic deserialisation, no custom converters

**Critical files:** `src/Huml.Net/Serialization/HumlDeserializer.cs`

---

## Step 7: docs/versioning.md

1. **Spec Versions** -- V0_1 (deprecated), V0_2 (current)
2. **HumlOptions** -- All four properties with examples
3. **Preset Configurations** -- `HumlOptions.Default` vs `HumlOptions.AutoDetect` vs custom
4. **Support Window Policy** -- Rolling 3-version window (pre-1.0), deprecation process
5. **Version Directive** -- `%HUML v0.2.0` header line behaviour
6. **HumlUnsupportedVersionException** -- When thrown, how to handle

**Critical files:** `src/Huml.Net/Versioning/HumlOptions.cs`, `src/Huml.Net/Versioning/SpecVersionPolicy.cs`

---

## Step 8: docs/ast-and-parse-api.md

1. **Overview** -- `Huml.Parse()` returns `HumlDocument`
2. **AST Node Hierarchy** -- Table of all node types and their properties
3. **ScalarKind Enum** -- All 7 members with runtime type of `Value` for each
4. **Walking the AST** -- C# pattern matching example over `HumlNode` subtypes
5. **Validation Use Case** -- Parse-only to check validity
6. **HumlDocument vs HumlInlineMapping** -- When each is produced

**Critical files:** `src/Huml.Net/Parser/` (all node files)

---

## Step 9: docs/internals/pipeline.md

High-level pipeline overview for contributors. **Not** a code walkthrough -- focuses on data flow and extension points.

1. **Pipeline Diagram** -- ASCII/text diagram: `Input string -> Lexer -> Token stream -> HumlParser -> HumlDocument (AST) -> HumlSerializer / HumlDeserializer -> output`
2. **Lexer** -- Single-pass, line-oriented tokeniser. Pull-based (produces tokens on demand). Key responsibilities: indentation tracking, comment validation, string/number parsing. Entry point: `Lexer.cs` constructor + `NextToken()`.
3. **Parser** -- Recursive-descent, consumes token stream. Produces immutable AST (`sealed record` nodes). Manages indent-level stack for nesting. Entry point: `HumlParser.cs`.
4. **AST** -- Immutable `sealed record` hierarchy. `HumlDocument` is root. All nodes in `Parser/` directory.
5. **Serialiser** -- Reflects over .NET objects, emits HUML text. Uses `PropertyDescriptor` cache. Handles version header, indentation, type dispatch.
6. **Deserialiser** -- Walks AST, maps to .NET types via reflection. Uses `PropertyDescriptor` for POCO mapping. Handles collection dispatch, scalar coercion.
7. **Where things live** -- Quick file-path reference table for each component.

**Critical files:** All `internal sealed` classes in `src/Huml.Net/`

---

## Step 10: docs/internals/version-gates.md

Guide for the most common contributor task: adding support for a new HUML spec version.

1. **How Version Gates Work** -- The `>= HumlSpecVersion.V0_2` pattern. No forked classes -- single code path with conditional branches. All gates use `>=` comparisons making them searchable.
2. **Step-by-Step: Adding a New Spec Version** -- Checklist:
   - Add enum member to `HumlSpecVersion` (e.g. `V0_3 = 3`)
   - Update `SpecVersionPolicy.Latest`
   - Add fixture submodule (`fixtures/v0.3/` pinned to `tests@v0.3`)
   - Add shared suite test method in `SharedSuiteTests.cs`
   - Add version gates in Lexer/Parser for each behavioural change
   - Update `HumlOptions.Default` if the new version becomes the default
3. **Deprecation Process** -- `[Obsolete]` on exiting enum member -> grace period -> removal. Reference `HumlSpecVersion.V0_1` as a worked example.
4. **Testing Version Gates** -- How to test both sides of a gate. `#pragma warning disable CS0618` pattern for testing deprecated versions.

**Critical files:** `src/Huml.Net/Versioning/HumlSpecVersion.cs`, `src/Huml.Net/Versioning/SpecVersionPolicy.cs`, `tests/Huml.Net.Tests/SharedSuiteTests.cs`

---

## Step 11: docs/internals/extending.md

Reference for structural extensions to the library.

1. **Adding a New AST Node** -- Checklist:
   - Create `public sealed record` in `src/Huml.Net/Parser/`
   - Inherit from `HumlNode`
   - Update parser production sites
   - Update serialiser/deserialiser dispatch
   - Add tests
   - Reference `HumlInlineMapping` as a worked example (Phase 07.2-03)
2. **Adding a New Token Type** -- Checklist:
   - Add member to `TokenType` enum
   - Update Lexer to emit the token
   - Update Parser to consume it
   - Add lexer + parser tests
3. **Adding a New Supported Type for Serialisation/Deserialisation** -- Checklist:
   - Update type dispatch in `HumlSerializer`
   - Update type dispatch in `HumlDeserializer`
   - Update `docs/serialisation.md` type mapping table
   - Add round-trip tests
4. **Key Conventions** -- Internal classes are `internal sealed`. AST nodes are `public sealed record`. No external runtime dependencies.

**Critical files:** `src/Huml.Net/Parser/HumlInlineMapping.cs` (worked example), `src/Huml.Net/Lexer/TokenType.cs`, `src/Huml.Net/Serialization/HumlSerializer.cs`, `src/Huml.Net/Serialization/HumlDeserializer.cs`

---

## Verification

After all files are written:

1. **Build check** -- `dotnet build` (ensures README doesn't break NuGet packing)
2. **Link check** -- Verify all absolute GitHub URLs resolve (manual or script)
3. **Code example accuracy** -- Each README/docs code example should compile against the current API signatures in `Huml.cs`
4. **British English** -- All docs use British spelling (serialisation, deserialisation, behaviour)
5. **NuGet preview** -- `dotnet pack` and inspect the `.nupkg` to confirm README renders correctly

---

## Maintenance Notes

- **README code examples** use the stable `Huml` static facade -- unlikely to change
- **CHANGELOG** should be updated in every PR that changes user-visible behaviour
- **docs/ guides** should be updated when public API types or signatures change
- **Type mapping table** in serialisation.md is fragile -- must be updated when new types are supported
- **Version support window** in versioning.md must be updated when `HumlSpecVersion` enum changes
