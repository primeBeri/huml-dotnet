namespace Huml.Net.Serialization;

/// <summary>
/// Marks a property to be excluded from HUML serialisation and deserialisation.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HumlIgnoreAttribute : Attribute { }
