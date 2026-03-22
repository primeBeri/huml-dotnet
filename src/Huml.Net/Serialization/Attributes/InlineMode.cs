namespace Huml.Net.Serialization;

/// <summary>
/// Specifies the per-property inline format override for collection serialisation.
/// </summary>
/// <remarks>
/// Use with <see cref="HumlPropertyAttribute.Inline"/> to control whether a specific
/// collection property is serialised inline, forced multiline, or inherits the global
/// <c>HumlOptions.CollectionFormat</c> setting.
/// </remarks>
public enum InlineMode
{
    /// <summary>
    /// Inherit the global <c>HumlOptions.CollectionFormat</c> setting (default).
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// Request inline format for this property's collection value. Scalar-only collections
    /// are emitted inline; non-scalar items silently fall back to multiline.
    /// </summary>
    Inline = 1,

    /// <summary>
    /// Force multiline format for this property's collection value, regardless of the global
    /// <c>HumlOptions.CollectionFormat</c> setting.
    /// </summary>
    Multiline = 2,
}
