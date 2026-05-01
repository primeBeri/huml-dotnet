# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [0.1.0] - 2026-05-01

### Added
- **Parser:** Full HUML v0.1 and v0.2 recursive-descent parser validated against the shared `huml-lang/tests` fixture suite.
- **Lexer:** Single-pass tokeniser with version-gated rules; `ReadOnlySpan<char>` input, no intermediate string allocations on the hot path.
- **Serialiser:** Reflection-based `Huml.Serialize<T>()` emitting HUML text in source declaration order with `%HUML` version header.
- **Deserialiser:** `Huml.Deserialize<T>()` with full type coercion, `List<T>`, `T[]`, `Dictionary<string, T>`, and nested POCO support.
- **Attributes:** `[HumlProperty]` (key rename, `OmitIfDefault`, per-property `InlineMode`) and `[HumlIgnore]`.
- **Public API:** `System.Text.Json`-style static `Huml` facade with `Serialize`, `Deserialize`, and `Parse` overloads.
- **CI/NuGet:** GitHub Actions pipeline with SourceLink, MinVer, and OIDC Trusted Publishing.

[Unreleased]: https://github.com/primeBeri/huml-dotnet/compare/v0.1.0-alpha.1...HEAD
[0.1.0]: https://github.com/primeBeri/huml-dotnet/releases/tag/v0.1.0-alpha.1
