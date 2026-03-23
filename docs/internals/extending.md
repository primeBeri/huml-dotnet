# Extending the Library

This document provides checklists for adding new AST nodes, token types, and supported
serialisation/deserialisation types to Huml.Net. All extension work is internal — the public API
surface is the static `Huml` facade in `src/Huml.Net/Huml.cs` and the AST node hierarchy; no
other pipeline classes are exposed to consumers.

## Adding a New AST Node

1. Create the new record in `src/Huml.Net/Parser/` — one file per type. The Meziantou MA0048
   analyser rule is active under `TreatWarningsAsErrors`, so multiple types in one file will
   cause a build failure:

   ```csharp
   public sealed record MyNode(...) : HumlNode;
   ```

2. Inherit from `HumlNode` directly (the abstract base record). Do not extend `HumlDocument` or
   any other concrete node type.

3. Update the parser production sites in `src/Huml.Net/Parser/HumlParser.cs` to construct and
   return the new node where the grammar requires it.

4. Update `HumlDeserializer.DeserializeNode()` in
   `src/Huml.Net/Serialization/HumlDeserializer.cs` — add an `if (node is MyNode ...)` branch
   so the deserialiser knows how to map the new node to .NET objects.

5. Update `HumlSerializer.SerializeValue()` in `src/Huml.Net/Serialization/HumlSerializer.cs`
   if the new node has a serialiser counterpart (i.e. if a .NET type should round-trip through
   `MyNode`).

6. Add unit tests covering the new node: parser tests that assert the node is produced for the
   correct input, deserialiser tests that assert the correct .NET value is returned, and
   serialiser tests if applicable.

**Worked example: `HumlInlineMapping`** (added in Phase 07.2)

```csharp
// src/Huml.Net/Parser/HumlInlineMapping.cs
public sealed record HumlInlineMapping(IReadOnlyList<HumlNode> Entries) : HumlNode;
```

`HumlInlineMapping` represents an inline `{key: value}` block. It extends `HumlNode` directly
rather than `HumlDocument` so that deserialiser dispatch can distinguish inline mapping blocks
from root and nested multiline mapping blocks (which both return `HumlDocument`). Three dispatch
sites were updated when it was introduced: the parser (`HumlParser.cs`), the deserialiser
(`HumlDeserializer.cs`), and the test assertions.

## Adding a New Token Type

1. Add a new member to the `TokenType` enum in `src/Huml.Net/Lexer/TokenType.cs`.

2. Update `src/Huml.Net/Lexer/Lexer.cs` — either add a dedicated `Scan*` method for the new
   token or extend the dispatch in `NextToken()` to emit the new `TokenType` for the appropriate
   input characters.

3. Update `src/Huml.Net/Parser/HumlParser.cs` to consume the new token. Add production methods
   or extend existing ones so the Parser correctly handles the new token in the grammar.

4. Add Lexer unit tests in `tests/Huml.Net.Tests/Lexer/LexerTests.cs` to confirm the new token
   is emitted for the correct input.

5. Add Parser unit tests in `tests/Huml.Net.Tests/Parser/HumlParserTests.cs` to confirm the
   parser handles the new token correctly.

**Current `TokenType` members for reference (18 members):**

| Group | Members |
|-------|---------|
| Structural | `Eof`, `Error` |
| Directive | `Version` |
| Keys | `Key`, `QuotedKey` |
| Indicators | `ScalarIndicator`, `VectorIndicator`, `ListItem`, `Comma` |
| Scalars | `String`, `Int`, `Float`, `Bool`, `Null`, `NaN`, `Inf` |
| Empty collections | `EmptyList`, `EmptyDict` |

## Adding a New Supported Type

This covers adding support for serialising or deserialising a .NET type that Huml.Net does not
currently handle (e.g. `DateTimeOffset`, `Guid`, or a new numeric type).

1. Update the type dispatch in `HumlSerializer.SerializeValue()` in
   `src/Huml.Net/Serialization/HumlSerializer.cs`. Add the new type check before the final POCO
   fallback. Note: `string` must remain before the `IEnumerable<T>` check — `string` implements
   `IEnumerable<char>` and would otherwise be serialised as a character sequence.

2. Update `HumlDeserializer.CoerceScalar()` for scalar types (e.g. a type that maps directly
   to a HUML scalar literal), or `HumlDeserializer.DeserializeNode()` for structured types (e.g.
   a type that maps to a `HumlDocument` or `HumlSequence` node).
   File: `src/Huml.Net/Serialization/HumlDeserializer.cs`

3. Update consumer-facing type mapping documentation if a `docs/serialisation.md` guide exists.

4. Add round-trip tests in `tests/Huml.Net.Tests/HumlStaticApiTests.cs` or a new test file.
   A round-trip test serialises a .NET value to HUML text and then deserialises it back,
   asserting value equality.

## Key Conventions

- **`internal sealed` for pipeline classes.** The Lexer, Parser, Serialiser, and Deserialiser are
  all `internal sealed`. Consumers never access them directly. New pipeline helpers must follow
  the same visibility rule.

- **`public sealed record` for AST nodes.** Records provide structural equality and immutability
  by default. Do not use `class` for new AST node types.

- **No external runtime dependencies.** Any new code must use only BCL types. Adding an external
  package to `src/Huml.Net/Huml.Net.csproj` is not permitted.

- **One type per file.** The Meziantou MA0048 analyser rule is enforced under
  `TreatWarningsAsErrors`. Placing multiple types in a single file will fail the build.

- **`TreatWarningsAsErrors` is active across all TFMs.** New code must compile cleanly on
  `netstandard2.1`, `net8.0`, `net9.0`, and `net10.0` with zero warnings.

- **British English spelling** in all documentation, comments, and string literals visible to
  contributors. Use `serialisation`, `deserialisation`, `behaviour`, `recognised` — not
  their American equivalents.
