namespace Huml.Net.Parser;

/// <summary>
/// Represents a mapping block in the HUML AST. Used for both the document root
/// and for every nested multiline mapping block introduced by the <c>::</c> vector indicator.
/// </summary>
/// <remarks>
/// A single type is used for all multiline mapping contexts to keep the AST hierarchy shallow.
/// Inline <c>{ key: value }</c> notation and empty <c>{}</c> dicts produce a
/// <see cref="HumlInlineMapping"/> node instead.
/// </remarks>
/// <param name="Entries">The mapping entries or list items in this block.</param>
public sealed record HumlDocument(IReadOnlyList<HumlNode> Entries) : HumlNode;
