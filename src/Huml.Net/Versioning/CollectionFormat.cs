namespace Huml.Net.Versioning;

/// <summary>
/// Controls whether collections are serialised in inline or multiline format.
/// </summary>
public enum CollectionFormat
{
    /// <summary>Collections are emitted as indented multiline blocks (default).</summary>
    Multiline = 0,

    /// <summary>
    /// Scalar-only collections are emitted inline (<c>key:: v1, v2, v3</c>).
    /// Collections containing non-scalar items (POCOs, nested collections, dictionaries
    /// with complex values) silently fall back to <see cref="Multiline"/>.
    /// </summary>
    Inline = 1,
}
