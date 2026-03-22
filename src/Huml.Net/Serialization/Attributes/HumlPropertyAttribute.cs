namespace Huml.Net.Serialization;

/// <summary>
/// Customises how a property is represented in a HUML document.
/// </summary>
/// <remarks>
/// When <see cref="Name"/> is provided, the HUML key name overrides the property name.
/// When <see cref="OmitIfDefault"/> is <c>true</c>, the property is omitted during
/// serialisation when its value equals the CLR default for its type.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HumlPropertyAttribute : Attribute
{
    /// <summary>
    /// The HUML key name to use instead of the property name, or <c>null</c> to use the property name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// When <c>true</c>, the property is omitted from the serialised output if its value
    /// equals the CLR default for its type (e.g., <c>0</c> for integers, <c>null</c> for
    /// reference types). Defaults to <c>false</c>.
    /// </summary>
    public bool OmitIfDefault { get; init; }

    /// <summary>
    /// Per-property inline format override for collection properties.
    /// <see cref="InlineMode.Inline"/> requests inline format (with silent multiline fallback for non-scalar items);
    /// <see cref="InlineMode.Multiline"/> forces multiline regardless of the global setting;
    /// <see cref="InlineMode.Inherit"/> (default) inherits the global <c>HumlOptions.CollectionFormat</c> setting.
    /// Has no effect on non-collection properties (scalars, POCOs).
    /// </summary>
    public InlineMode Inline { get; init; } = InlineMode.Inherit;

    /// <summary>
    /// Initialises a new instance with an optional HUML key name override.
    /// </summary>
    /// <param name="name">The HUML key name, or <c>null</c> to use the property name.</param>
    public HumlPropertyAttribute(string? name = null) => Name = name;
}
