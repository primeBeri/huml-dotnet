# Plan: Unicode, RTL Support & Fixture Gap Analysis

> **Status**: Ready for GSD integration — not yet enacted.

## Context

A pre-implementation design discussion identified three areas for non-Latin/RTL character support. The HUML spec mandates UTF-8, prohibits `\u` escapes, and restricts bare keys to ASCII `[a-zA-Z][a-zA-Z0-9_-]*`. Quoted keys and string values support arbitrary Unicode.

**Current implementation status:**
- Bare key validation: **Correct** — ASCII range checks, not `char.IsLetter()`
- String scanning: **Correct** — passes Unicode through verbatim, no normalisation
- Error message: **NOT actionable** — non-ASCII at key position gives `"Unexpected character 'X'"` with no guidance
- Test coverage: **Minimal** — one fixture tests Greek/Chinese/emoji in strings; nothing for error paths, RTL, bidi, or quoted Unicode keys

**Goal**: Improve developer experience and document spec-compliant Unicode behaviour. No changes to what the lexer/parser accepts for valid documents.

---

## Part A: Contributable Test Fixtures (upstream to `huml-lang/tests`)

The `fixtures/v0.2` submodule (git submodule at `v0.2.0`) follows a two-format convention (per `fixtures/v0.2/README.md`):
- **Assertions**: JSON arrays — `{name, input, error}` — pass/fail parse tests
- **Documents**: `.huml` + `.json` pairs — round-trip content verification

Unicode test cases are authored in these formats first, so the same fixtures benefit go-huml, huml-rs, and pyhuml.

### New assertion file: `unicode.json`

```json
[
  {"name": "bare_arabic_key", "input": "اسم: \"value\"", "error": true},
  {"name": "bare_chinese_key", "input": "名: \"value\"", "error": true},
  {"name": "bare_cyrillic_key", "input": "Д: \"value\"", "error": true},
  {"name": "bare_devanagari_key", "input": "नाम: \"value\"", "error": true},
  {"name": "bare_emoji_key", "input": "🚀: \"value\"", "error": true},
  {"name": "quoted_arabic_key", "input": "\"اسم\": \"أحمد\"", "error": false},
  {"name": "quoted_chinese_key", "input": "\"名前\": \"太郎\"", "error": false},
  {"name": "quoted_cyrillic_key", "input": "\"Имя\": \"Иван\"", "error": false},
  {"name": "quoted_emoji_key", "input": "\"🚀\": \"launch\"", "error": false},
  {"name": "arabic_string_value", "input": "key: \"مرحبا\"", "error": false},
  {"name": "hebrew_string_value", "input": "key: \"שלום\"", "error": false},
  {"name": "chinese_string_value", "input": "key: \"你好世界\"", "error": false},
  {"name": "korean_string_value", "input": "key: \"안녕하세요\"", "error": false},
  {"name": "emoji_string_value", "input": "key: \"🚀🌍\"", "error": false},
  {"name": "mixed_ltr_rtl_string", "input": "key: \"Hello مرحبا World\"", "error": false},
  {"name": "rtl_mark_in_string", "input": "key: \"text\u200Fmore\"", "error": false},
  {"name": "ltr_mark_in_string", "input": "key: \"text\u200Emore\"", "error": false}
]
```

### New document pair: `unicode.huml` + `unicode.json`

**`unicode.huml`** — document with quoted Unicode keys and Unicode string values:

```huml
"名前": "太郎"
greeting: "مرحبا بالعالم"
emoji: "🚀🌍"
mixed: "Hello مرحبا World"
```

**`unicode.json`** — expected JSON parse output for round-trip verification:

```json
{
  "名前": "太郎",
  "greeting": "مرحبا بالعالم",
  "emoji": "🚀🌍",
  "mixed": "Hello مرحبا World"
}
```

### Contribution workflow

1. Fork the `huml-lang/tests` v0.2 repo
2. Add `assertions/unicode.json` and `documents/unicode.huml` + `documents/unicode.json`
3. PR upstream — these are parser-agnostic, so all HUML implementations can run them
4. Once merged, update the `fixtures/v0.2` submodule pointer in `huml-dotnet`

### Interim: local fixture extensions

Until the upstream PR is merged, a local `fixtures/extensions/` directory mirrors the fixture format:

```
fixtures/
  v0.1/                          ← git submodule (upstream, pinned at v0.1.0)
  v0.2/                          ← git submodule (upstream, pinned at v0.2.0)
  extensions/
    v0.2/
      assertions/
        unicode.json             ← proposed upstream fixture
      documents/
        unicode.huml             ← proposed upstream fixture
        unicode.json             ← proposed upstream fixture
```

`SharedSuiteTests.LoadFixtures()` is extended to also scan `fixtures/extensions/{version}/assertions/`, so the tests run immediately without waiting for the upstream PR. The `.csproj` needs a `<Content>` include for the extensions directory.

---

## Part B: Huml.Net-Specific Changes

### 1. Actionable error for non-ASCII letters at key position

**File**: `src/Huml.Net/Lexer/Lexer.cs` (line ~133)

Before the catch-all `ThrowParseError($"Unexpected character '{ch}'.")`, insert:

```csharp
// Error path only — char.IsLetter is NOT used in acceptance logic
if (char.IsLetter(ch))
{
    ThrowParseError(
        $"Bare keys must start with [a-zA-Z]. "
        + $"Non-Latin characters are supported in quoted keys: \"{ch}\": \"value\"");
}

ThrowParseError($"Unexpected character '{ch}'.");
```

**Why this is safe**: `char.IsLetter(ch)` only runs after all acceptance checks have failed — it cannot change what the lexer accepts. Emoji (surrogate high bytes, e.g. `0xD83D`) return `false` from `char.IsLetter`, so they still get the generic error, which is correct.

### 2. XML doc on `IsLetter` explaining the deliberate ASCII restriction

**File**: `src/Huml.Net/Lexer/Lexer.cs` (line ~984)

```csharp
/// <summary>
/// ASCII-only letter check per the HUML spec — bare keys use <c>[a-zA-Z][a-zA-Z0-9_-]*</c>.
/// Non-Latin characters are supported via quoted keys.
/// </summary>
private static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
```

### 3. Lexer Unicode tests

**New file**: `tests/Huml.Net.Tests/Lexer/LexerUnicodeTests.cs`

Follows existing pattern from `LexerTests.cs`: `LexAll` helper returning `List<Token>`, `HumlLexer` alias, AwesomeAssertions.

| Test | Input | Assertion |
|------|-------|-----------|
| `NonAsciiLetter_AtKeyPosition_ThrowsActionableError` | `"اسم: \"v\"\n"` | `HumlParseException` message contains `"Bare keys must start with [a-zA-Z]"` and `"quoted keys"` |
| `ChineseCharacter_AtKeyPosition_ThrowsActionableError` | `"名: \"v\"\n"` | Same pattern |
| `CyrillicCharacter_AtKeyPosition_ThrowsActionableError` | `"Д: \"v\"\n"` | Same pattern |
| `Emoji_AtKeyPosition_ThrowsGenericError` | `"🚀: \"v\"\n"` | `HumlParseException` message contains `"Unexpected character"` (not the quoted-key hint) |
| `ArabicText_InQuotedString_LexesCorrectly` | `"key: \"مرحبا\"\n"` | `TokenType.String`, value == `"مرحبا"` |
| `HebrewText_InQuotedString_LexesCorrectly` | `"key: \"שלום\"\n"` | `TokenType.String`, value == `"שלום"` |
| `ArabicText_InQuotedKey_LexesAsQuotedKey` | `"\"اسم\": \"v\"\n"` | `TokenType.QuotedKey`, value == `"اسم"` |
| `ChineseText_InQuotedKey_LexesAsQuotedKey` | `"\"名前\": \"v\"\n"` | `TokenType.QuotedKey`, value == `"名前"` |
| `Emoji_InQuotedString_LexesCorrectly` | `"key: \"🚀🌍\"\n"` | `TokenType.String`, value == `"🚀🌍"` |
| `Emoji_InQuotedKey_LexesAsQuotedKey` | `"\"🚀\": \"v\"\n"` | `TokenType.QuotedKey`, value == `"🚀"` |
| `BidiControlChars_InQuotedString_PassThrough` | String with U+200F, U+200E | Characters preserved verbatim in token value |
| `MixedLtrRtl_InQuotedString_PreservesContent` | `"key: \"Hello مرحبا World\"\n"` | Value == `"Hello مرحبا World"` |
| `NonAsciiError_ReportsCorrectLineAndColumn` | `"a: 1\nب: 2\n"` | `ex.Line == 2`, `ex.Column == 0` |

### 4. Parser Unicode tests

**New file**: `tests/Huml.Net.Tests/Parser/ParserUnicodeTests.cs`

Follows existing pattern from `HumlParserTests.cs`: `HumlParser` alias, `new HumlParser(input, HumlOptions.Default).Parse()`.

| Test | Input | Assertion |
|------|-------|-----------|
| `Parse_QuotedKeyWithArabic_ReturnsMappingWithCorrectKey` | `"\"اسم\": \"أحمد\""` | `HumlMapping` key == `"اسم"`, scalar value == `"أحمد"` |
| `Parse_QuotedKeyWithChinese_ReturnsMappingWithCorrectKey` | `"\"名前\": \"太郎\""` | Mapping key and value correct |
| `Parse_QuotedKeyWithEmoji_ReturnsMappingWithCorrectKey` | `"\"🚀\": \"launch\""` | Mapping key == `"🚀"` |
| `Parse_RtlStringValue_PreservesContent` | `"msg: \"مرحبا بالعالم\""` | `HumlScalar` value is the Arabic string |
| `Parse_MixedScriptDocument_ParsesAllMappings` | Multi-line with ASCII bare keys + Unicode quoted values | Correct entry count and values |
| `Parse_BidiControlCharsInValue_Preserved` | String value with bidi marks | Characters survive parsing unchanged |
| `RoundTrip_UnicodeStringValues_Preserved` | Serialize then deserialize a POCO with Arabic/Chinese values | Equality |
| `RoundTrip_Emoji_Preserved` | Serialize then deserialize a POCO with emoji string value | Equality |

Round-trip tests use a private `UnicodePoco` class with ASCII property names to avoid the pre-existing serializer key-quoting gap (see Out of Scope below).

---

## Out of Scope (tracked separately)

| Issue | Reason |
|-------|--------|
| Serializer key quoting | `HumlSerializer.EmitEntry` (line ~189) emits keys bare — a `Dictionary<string, T>` with non-ASCII keys would produce invalid HUML. Pre-existing bug, separate task. |
| Bidi rejection/warning | Spec doesn't mandate it; pass-through matches go-huml reference behaviour. Future linting layer (`Huml.Net.Linting`). |
| Unicode normalisation | Spec explicitly prohibits — accept whatever Unicode the file contains unchanged. |
| Column tracking for surrogate pairs | `_col` counts `char` units (UTF-16 code units), so non-BMP characters (emoji) report one extra column. Consistent with most editors; complexity not justified. |

---

## Files Modified

| File | Change |
|------|--------|
| `src/Huml.Net/Lexer/Lexer.cs` | Non-ASCII letter error branch (~line 133); XML doc on `IsLetter` (~line 984) |
| `tests/Huml.Net.Tests/SharedSuiteTests.cs` | Extend `LoadFixtures` to also scan `fixtures/extensions/{version}/assertions/` for both v0.1 and v0.2 |
| `tests/Huml.Net.Tests/Huml.Net.Tests.csproj` | Add `<Content>` include for `fixtures/extensions/**` with `CopyToOutputDirectory` |
| `fixtures/extensions/v0.1/assertions/gaps.json` | **NEW** — 11 contributable general fixture gaps (v0.1-compatible, same content as v0.2) |
| `fixtures/extensions/v0.2/assertions/unicode.json` | **NEW** — 17 contributable Unicode/RTL assertion fixtures |
| `fixtures/extensions/v0.2/assertions/gaps.json` | **NEW** — 11 contributable general fixture gaps (see Part C) |
| `fixtures/extensions/v0.2/documents/unicode.huml` | **NEW** — contributable document fixture |
| `fixtures/extensions/v0.2/documents/unicode.json` | **NEW** — expected JSON parse output for round-trip verification |
| `tests/Huml.Net.Tests/Lexer/LexerUnicodeTests.cs` | **NEW** — 13 .NET-specific lexer Unicode tests (error messages, token type/value assertions) |
| `tests/Huml.Net.Tests/Parser/ParserUnicodeTests.cs` | **NEW** — 8 .NET-specific parser/round-trip Unicode tests |

## Verification

1. `dotnet build --configuration Release` — zero warnings from StyleCop, Meziantou, SonarAnalyzer
2. `dotnet test` — all tests pass: existing suite + shared extension fixtures + .NET-specific tests
3. Confirm extension fixtures load: test count in `V02_fixture_passes` increases by 28 (17 Unicode + 11 gaps)
4. Validate both JSON extension files are valid standalone JSON

---

## Part C: General Fixture Gaps Identified from Existing .NET Tests

A full audit of all 73 parse-agnostic test cases in the .NET test suite was cross-referenced against the upstream `fixtures/v0.1/assertions/mixed.json` and `fixtures/v0.2/assertions/mixed.json`. The following 11 cases are **not covered by either upstream file** and should be contributed as a separate `gaps.json` fixture file.

### How these were identified

Every test case in `LexerTests.cs`, `HumlParserTests.cs`, and `HumlStaticApiTests.cs` that only asserts "error or no error" (no .NET-specific token types, AST nodes, or exception message text) was treated as a candidate. Candidates were then matched against the upstream fixture input strings. Inputs testing the same concept with different incidental values (e.g. `"key: 0xFF"` vs `"key: 0xCAFEBABE"`) were marked as covered. Only genuinely uncovered cases are listed below.

### New fixture file: `fixtures/extensions/v0.2/assertions/gaps.json`

```json
[
  // Case-insensitive keywords — tested in LexerTests.cs but absent from both fixture files
  {"name": "bool_true_uppercase", "input": "key: TRUE", "error": false},
  {"name": "bool_false_uppercase", "input": "key: FALSE", "error": false},
  {"name": "null_uppercase", "input": "key: NULL", "error": false},

  // Tab indentation — tested in HumlStaticApiTests.cs; not in any upstream fixture
  {"name": "tab_indentation_at_line_start", "input": "\tkey: \"value\"", "error": true},

  // Quoted key with integer value — upstream only has quoted key with string/bool value
  {"name": "quoted_key_with_integer_value", "input": "\"my-key\": 42", "error": false},

  // Root scalars — upstream has root_scalar: "123" (integer) but not float/nan/inf/hex as root
  {"name": "root_float_scalar", "input": "3.14", "error": false},
  {"name": "root_nan_scalar", "input": "nan", "error": false},
  {"name": "root_inf_scalar", "input": "inf", "error": false},
  {"name": "root_hex_scalar", "input": "0xFF", "error": false},

  // Multiline list with integer items — upstream only has string items in multiline lists
  {"name": "multiline_list_integer_items", "input": "list::\n  - 1\n  - 2\n  - 3", "error": false},

  // Ambiguous empty vector (bare, no trailing content) — upstream has "key:: # comment"
  // but not the bare "key::" without trailing space or comment
  {"name": "ambiguous_empty_vector_bare", "input": "key::", "error": true}
]
```

> **Note on odd-space indentation**: The .NET test `Parse_BadIndentation_ThrowsHumlParseException` uses 3-space indentation (`"outer::\n   inner: 1"`). The upstream suite covers 4-space (too much) and 0-space (too little). Whether 3-space is worth a fixture depends on whether the spec defines "exactly 2 spaces" vs "consistent non-zero indentation". Left as a follow-up question for the `huml-lang` maintainers.

### Same cases for v0.1 extensions

The same `gaps.json` applies to `fixtures/extensions/v0.1/assertions/gaps.json` with identical content — both versions have the same gaps and all 11 cases are version-agnostic (no backtick/triple-quote differences).
