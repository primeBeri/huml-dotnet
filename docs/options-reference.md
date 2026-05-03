# HumlOptions Reference

`HumlOptions` controls parsing, serialisation, and version behaviour in Huml.Net.
Three convenience instances are provided for common scenarios: `HumlOptions.Default`, `HumlOptions.LatestSupported`, and `HumlOptions.AutoDetect`.
All properties use `init`-only setters, making instances immutable after construction.

## Properties

| Property                  | Type                      | Default     | Valid Values                        | Behaviour                                                                                                                  |
| ------------------------- | ------------------------- | ----------- | ----------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `SpecVersion`             | `HumlSpecVersion`         | `V0_2`      | `V0_1`, `V0_2`                      | Selects which spec grammar to apply when `VersionSource` is `Options`                                                      |
| `VersionSource`           | `VersionSource`           | `Options`   | `Options`, `Header`                 | `Options` = use `SpecVersion` property; `Header` = read `%HUML` directive from document                                    |
| `UnknownVersionBehaviour` | `UnknownVersionBehaviour` | `Throw`     | `Throw`, `UseLatest`, `UsePrevious` | What happens when a `%HUML` header declares an unrecognised version                                                        |
| `CollectionFormat`        | `CollectionFormat`        | `Multiline` | `Multiline`, `Inline`               | Global default for collection serialisation format; per-property override via `[HumlProperty(Inline = InlineMode.Inline)]` |
| `MaxRecursionDepth`       | `int`                     | `64`        | `1`–`1024`                          | Max nesting depth before `HumlParseException` is thrown                                                                    |
| `PropertyNamingPolicy` | `HumlNamingPolicy?`    | `null`  | `null` or any `HumlNamingPolicy` instance | Converts .NET property names to HUML keys during serialisation and deserialisation. `null` = property name used as-is. Built-ins: `HumlNamingPolicy.KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase`. A `[HumlProperty]` name override always takes precedence. |
| `Converters`           | `IList<HumlConverter>` | `[]`    | any list of `HumlConverter` instances     | Custom converters consulted during serialisation and deserialisation when no `[HumlConverter]` attribute is present. First converter whose `CanConvert` returns `true` wins. Do not modify this list after passing options to any `Huml.*` method.                    |

## Convenience Instances

| Instance                      | SpecVersion | VersionSource | UnknownVersionBehaviour | CollectionFormat | MaxRecursionDepth | PropertyNamingPolicy | Converters |
| ----------------------------- | ----------- | ------------- | ----------------------- | ---------------- | ----------------- | -------------------- | ---------- |
| `HumlOptions.Default`         | V0_2        | Header        | Throw                   | Multiline        | 64                | null                 | []         |
| `HumlOptions.LatestSupported` | V0_2        | Options       | Throw                   | Multiline        | 64                | null                 | []         |
| `HumlOptions.AutoDetect`      | V0_2        | Header        | Throw                   | Multiline        | 64                | null                 | []         |

`HumlOptions.Default` reads the `%HUML vX.Y.Z` header from the document to determine the spec version.
If no header is present, it falls back to `V0_2`. `HumlOptions.AutoDetect` is a reference-equal alias for `Default`.

`HumlOptions.LatestSupported` ignores the `%HUML` header and always uses `V0_2` rules — use this when you want deterministic version behaviour regardless of document content.

## Examples

```csharp
using Huml.Net;
using Huml.Net.Versioning;

// Read version from document header; throw if unrecognised
var result = Huml.Deserialize<MyDto>(humlText, HumlOptions.AutoDetect);

// Read version from header; fall back to latest if unrecognised
var lenient = new HumlOptions
{
    VersionSource = VersionSource.Header,
    UnknownVersionBehaviour = UnknownVersionBehaviour.UseLatest,
};
var result2 = Huml.Deserialize<MyDto>(humlText, lenient);

// Always use v0.2 rules, ignoring any %HUML header
var result3 = Huml.Deserialize<MyDto>(humlText, HumlOptions.LatestSupported);
```

## Notes

- Passing `null` for `options` in any `Huml.*` method is equivalent to passing `HumlOptions.Default` (header-aware auto-detect).
- `MaxRecursionDepth` throws `ArgumentOutOfRangeException` at construction time if the value is outside `[1, 1024]`.
- `CollectionFormat.Inline` is silently ignored for collection properties that contain non-scalar items — those always emit in multiline format.
- `PropertyNamingPolicy` applies only to .NET property names — it does not affect `Dictionary<string, T>` string keys or `[HumlProperty]` explicit names.
- The `Converters` list is checked in order; the first converter whose `CanConvert(type)` returns `true` is used. A property-level or type-level `[HumlConverter]` attribute always takes precedence over `Converters`.
