# Plan: Claude Rule — Fixture Gap Analysis

> **Status**: Ready to implement.

## Purpose

Create a Claude rule file at `.claude/rules/fixture-gaps.md` that defines a repeatable process for:
1. Identifying language/implementation-agnostic gaps in the official `huml-lang/tests` fixture suite
2. Adding those gaps to `fixtures/extensions/` so they run locally and can be contributed upstream

This rule should be invoked whenever new tests are written, a new spec version is released, or the upstream fixtures are updated.

---

## Rule File to Create

**Path**: `.claude/rules/fixture-gaps.md`

---

## Rule Contents

The rule should cover the following sections:

### 1. What counts as a fixture gap

A gap is a parse behaviour that:
- Is **language/implementation-agnostic** — tests only "does this input parse successfully or throw a parse error?"
- Is **not already covered** by the upstream assertion files (by input string, not just name)
- Is **deterministic** — the same input must produce the same result in every compliant HUML parser
- Does **not require knowledge of parsed values** — if the test requires checking what was parsed (token types, AST nodes, property values), it is .NET-specific and does not belong in fixtures

**Not gaps** (exclude these):
- Tests that verify specific token types, AST node types, or property values
- Tests that verify exception message text
- Tests that verify .NET serialization/deserialization behaviour
- Tests that verify allocation counts or performance
- Tests that verify `HumlOptions` defaults or .NET enum values
- Error cases that are version-agnostic in concept but differ by spec version (add to the correct version's extensions, not both)

### 2. How to audit for gaps

**Step 1** — Read all upstream fixture assertion files:
- `fixtures/v0.1/assertions/*.json`
- `fixtures/v0.2/assertions/*.json`

Build a mental (or literal) set of covered input strings.

**Step 2** — Read all .NET test files:
- `tests/Huml.Net.Tests/Lexer/LexerTests.cs`
- `tests/Huml.Net.Tests/Parser/HumlParserTests.cs`
- `tests/Huml.Net.Tests/HumlStaticApiTests.cs`
- Any new test files added since the last audit

**Step 3** — For each test, classify:
- Does it only assert "error or no error"? → **Candidate**
- Does it assert .NET-specific behaviour? → **Not a candidate**

**Step 4** — Cross-reference candidates against the upstream covered set:
- Match by normalised input string (strip trailing `\n` for comparison if the concept is the same)
- If an input is semantically equivalent to one already covered (same concept, different incidental value like key name), mark as **covered**
- Only flag as a gap if the **behaviour being tested** is genuinely absent from the upstream suite

### 3. Output format

Gaps go into `fixtures/extensions/` mirroring the upstream structure:

```
fixtures/
  extensions/
    v0.1/
      assertions/
        <category>.json    ← one file per logical group
    v0.2/
      assertions/
        <category>.json
      documents/
        <name>.huml        ← document fixtures (optional)
        <name>.json        ← expected JSON output
```

Each assertion file is a JSON array:
```json
[
  {"name": "descriptive_snake_case_name", "input": "...", "error": true},
  {"name": "another_case", "input": "...", "error": false}
]
```

Naming conventions:
- File names: `unicode.json`, `gaps.json`, `keywords.json`, etc. — logical groupings, not one file per test
- Test names: `snake_case`, descriptive, no abbreviations
- Do NOT duplicate names already in the upstream files (names need not be globally unique per the spec, but duplication causes confusion)

### 4. SharedSuiteTests integration

After adding extension files, verify that `SharedSuiteTests.LoadFixtures()` scans `fixtures/extensions/{version}/assertions/` and that the `.csproj` includes the directory with `CopyToOutputDirectory = PreserveNewest`. The test count for `V01_fixture_passes` and `V02_fixture_passes` should increase by the number of new cases.

### 5. Contribution workflow

Extension fixtures are a staging area for upstream contribution:

1. When a group of fixtures is stable and tests pass, open a PR to `huml-lang/tests`
2. Once merged, update the submodule pointer (`fixtures/v0.1` or `fixtures/v0.2`)
3. Remove the corresponding extension file — it is now covered by the submodule
4. Run `dotnet test` to confirm no regressions

---

## Files to Create

| File | Purpose |
|------|---------|
| `.claude/rules/fixture-gaps.md` | The rule itself — Claude follows this when auditing tests |

No other files are created by this plan. The rule is the deliverable.

## Verification

After creating the rule file:
1. Read it back and confirm it covers all five sections above
2. Manually invoke it on the existing `LexerTests.cs` to confirm it correctly identifies the 11 gaps already documented in `PLAN-unicode-rtl.md` — if the rule produces a different set, revise it
