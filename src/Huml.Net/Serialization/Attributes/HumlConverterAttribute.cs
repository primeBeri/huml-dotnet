namespace Huml.Net.Serialization;

/// <summary>
/// Specifies the converter type to use when serialising or deserialising the annotated
/// property, class, or struct. The converter type must derive from <see cref="HumlConverter"/>.
/// </summary>
/// <remarks>
/// Priority order: property-level [HumlConverter] &gt; type-level [HumlConverter] &gt;
/// <c>HumlOptions.Converters</c> list &gt; built-in dispatch.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = true)]
public sealed class HumlConverterAttribute : Attribute
{
    /// <summary>
    /// The converter type to use. Must be a concrete type deriving from
    /// <see cref="HumlConverter"/> with a public parameterless constructor.
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>Initialises a new instance specifying the converter type.</summary>
    /// <param name="converterType">
    /// The converter type. Must derive from <see cref="HumlConverter"/>.
    /// </param>
    public HumlConverterAttribute(Type converterType) => ConverterType = converterType;
}
