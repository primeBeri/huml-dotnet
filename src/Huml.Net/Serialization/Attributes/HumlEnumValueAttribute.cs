namespace Huml.Net.Serialization;

/// <summary>
/// Overrides the HUML string name used to represent an individual enum member
/// during serialisation and deserialisation.
/// </summary>
/// <remarks>
/// When applied to an enum member, the specified <see cref="Name"/> is used instead
/// of the member name (or any transform applied by <see cref="HumlNamingPolicy"/>).
/// If both <see cref="HumlEnumValueAttribute"/> and a naming policy are present,
/// the attribute name always takes precedence.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class HumlEnumValueAttribute : Attribute
{
    /// <summary>The HUML string name for this enum member.</summary>
    public string Name { get; }

    /// <summary>Initialises a new instance with the override name.</summary>
    /// <param name="name">The HUML name to use instead of the member name.</param>
    public HumlEnumValueAttribute(string name) => Name = name;
}
