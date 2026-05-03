[![CI](https://github.com/primeBeri/huml-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/primeBeri/huml-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Huml.Net.svg)](https://www.nuget.org/packages/Huml.Net/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# Huml.Net

A full-featured HUML v0.1/v0.2 parser, serialiser, and deserialiser for .NET with a System.Text.Json-style API and zero runtime dependencies.

> **Pre-1.0 alpha.** API may change before 1.0.0.

## Installation

```bash
dotnet add package Huml.Net
```

## Quick Start

### Example 1: Deserialise to POCO

```csharp
using Huml.Net;

var huml = """
    %HUML v0.2.0
    Host: "localhost"
    Port: 8080
    Debug: false
    """;

var config = Huml.Deserialize<ServerConfig>(huml);
// config.Host == "localhost", config.Port == 8080, config.Debug == false
```

### Example 2: Serialise from POCO

```csharp
using Huml.Net;

var config = new ServerConfig { Host = "prod.example.com", Port = 443 };
string huml = Huml.Serialize(config);
// %HUML v0.2.0
// Host: "prod.example.com"
// Port: 443
```

### Example 3: Attributes

```csharp
using Huml.Net;
using Huml.Net.Serialization;

public class ServerConfig
{
    [HumlProperty("host")]
    public string Host { get; set; } = string.Empty;

    [HumlProperty("port", OmitIfDefault = true)]
    public int Port { get; set; }

    [HumlIgnore]
    public string InternalToken { get; set; } = string.Empty;
}
```

### Example 4: Naming policy

```csharp
using Huml.Net;
using Huml.Net.Serialization;

public class ServerConfig
{
    public string HostName { get; set; } = string.Empty;
    public int MaxConnections { get; set; }
}

var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
var config = Huml.Deserialize<ServerConfig>("""
    %HUML v0.2.0
    host-name: "db.example.com"
    max-connections: 100
    """, options);
// config.HostName == "db.example.com", config.MaxConnections == 100
```

### Example 5: Populate existing instance

```csharp
using Huml.Net;

var defaults = new ServerConfig { HostName = "localhost", MaxConnections = 10 };
Huml.Populate("""
    %HUML v0.2.0
    max-connections: 50
    """, defaults, new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase });
// defaults.HostName is still "localhost" (not in the document)
// defaults.MaxConnections is now 50 (overwritten)
```

## Features

- Full HUML v0.1 and v0.2 spec compliance (validated against `huml-lang/tests` fixture suite)
- `System.Text.Json`-style static `Huml` facade
- Reflection-based serialisation with declaration-order property emission
- `[HumlProperty]` and `[HumlIgnore]` attributes
- Inline and multiline collection format control
- **Naming policy:** `HumlNamingPolicy.KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase` via `HumlOptions.PropertyNamingPolicy`
- **Enum support:** `HumlEnumValueAttribute` for custom member names; round-trips through quoted strings
- **Custom converters:** `HumlConverter<T>` abstract base, `[HumlConverter]` attribute, and `HumlOptions.Converters`
- **Populate:** `Huml.Populate<T>()` overlays a HUML document onto an existing object instance
- **AST source positions:** `Line` and `Column` on all AST nodes; `HumlDeserializeException` reports the source position
- Zero external runtime dependencies
- Multi-TFM: netstandard2.1, .NET 8, .NET 9, .NET 10

## HumlOptions

| Property                  | Type                      | Default     | Description                                                                                                                |
| ------------------------- | ------------------------- | ----------- | -------------------------------------------------------------------------------------------------------------------------- |
| `SpecVersion`             | `HumlSpecVersion`         | `V0_2`      | Which spec version to use when `VersionSource` is `Options`                                                                |
| `VersionSource`           | `VersionSource`           | `Options`   | `Options` = use `SpecVersion` property; `Header` = read `%HUML` directive from document                                    |
| `UnknownVersionBehaviour` | `UnknownVersionBehaviour` | `Throw`     | What happens when a `%HUML` header declares an unrecognised version                                                        |
| `CollectionFormat`        | `CollectionFormat`        | `Multiline` | Global default for collection serialisation format; per-property override via `[HumlProperty(Inline = InlineMode.Inline)]` |
| `MaxRecursionDepth`       | `int`                     | `64`        | Max nesting depth before `HumlParseException` is thrown                                                                    |
| `PropertyNamingPolicy` | `HumlNamingPolicy?`       | `null`      | Converts .NET property names to HUML keys. `null` = use property name as-is. Built-ins: `KebabCase`, `SnakeCase`, `CamelCase`, `PascalCase` |
| `Converters`           | `IList<HumlConverter>`    | `[]`        | Custom converters consulted during serialisation and deserialisation. First matching `CanConvert` wins.                                      |

## Compatibility

| Target         | Support           |
| -------------- | ----------------- |
| .NET 10        | Yes               |
| .NET 9         | Yes               |
| .NET 8         | Yes               |
| netstandard2.1 | Yes               |
| HUML v0.2      | Full              |
| HUML v0.1      | Full (deprecated) |

## Documentation

- [Options Reference](docs/options-reference.md)
- [Versioning Policy](docs/versioning.md)
- [AST Usage Guide](docs/ast-usage.md)
- [Error Handling](docs/error-handling.md)
- [Inline Serialisation](docs/inline-serialisation.md)
- [Naming Policy](docs/naming-policy.md)
- [Enum Serialisation](docs/enum-serialisation.md)
- [Custom Converters](docs/custom-converters.md)
- [Populate](docs/populate.md)

## Links

- [HUML Specification](https://huml.io)
- [Reference Implementation (Go)](https://github.com/huml-lang/go-huml)

## Project

- [Changelog](CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)
- [Security Policy](SECURITY.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Open issues / backlog](BACKLOG.md)

## Licence

MIT — see [LICENSE](LICENSE).
