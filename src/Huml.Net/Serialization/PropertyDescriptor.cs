using System.Collections.Concurrent;
using System.Reflection;

namespace Huml.Net.Serialization;

/// <summary>
/// Per-type cached property metadata used by the serialiser and deserialiser.
/// </summary>
/// <remarks>
/// Properties are ordered base-class-first within each type, then by <c>MetadataToken</c>
/// (declaration order). Properties decorated with <see cref="HumlIgnoreAttribute"/> are excluded.
/// <see cref="HumlPropertyAttribute"/> name overrides and <c>OmitIfDefault</c> flags are resolved once at
/// build time and cached.
/// </remarks>
internal sealed record PropertyDescriptor(
    string HumlKey,
    PropertyInfo Property,
    bool OmitIfDefault,
    bool IsInitOnly,
    object? DefaultValue,
    bool? Inline)
{
    // ── Cache ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pairs the ordered array (for serialiser declaration-order traversal) with the keyed
    /// dictionary (for deserialiser O(1) lookup) in a single
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> entry.
    /// </summary>
    private sealed record PropertyDescriptorCache(
        PropertyDescriptor[] Ordered,
        Dictionary<string, PropertyDescriptor> ByKey);

    private static readonly ConcurrentDictionary<Type, PropertyDescriptorCache> Cache = new();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the cached array of <see cref="PropertyDescriptor"/> entries for <paramref name="type"/>.
    /// Properties are ordered base-class-first, then by declaration order within each type.
    /// </summary>
    internal static PropertyDescriptor[] GetDescriptors(Type type) =>
        Cache.GetOrAdd(type, BuildDescriptors).Ordered;

    /// <summary>
    /// Returns the cached dictionary of <see cref="PropertyDescriptor"/> entries for
    /// <paramref name="type"/>, keyed by <see cref="HumlKey"/> with ordinal comparison.
    /// Used by the deserialiser for O(1) key lookup.
    /// </summary>
    internal static Dictionary<string, PropertyDescriptor> GetLookup(Type type) =>
        Cache.GetOrAdd(type, BuildDescriptors).ByKey;

    /// <summary>
    /// Clears the descriptor cache. Intended for use in test isolation only.
    /// </summary>
    internal static void ClearCache() => Cache.Clear();

    // ── Private implementation ────────────────────────────────────────────────

    private static PropertyDescriptorCache BuildDescriptors(Type type)
    {
        // Walk the inheritance chain from root to derived, collecting types in order.
        var typeChain = new List<Type>();
        var current = type;
        while (current != null && current != typeof(object))
        {
            typeChain.Insert(0, current); // prepend so base comes first
            current = current.BaseType;
        }

        var result = new List<PropertyDescriptor>();

        foreach (var t in typeChain)
        {
            // DeclaredOnly: each type contributes its own properties only.
            // Sort by MetadataToken to get declaration order within this type.
            var props = t.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Array.Sort(props, (a, b) => a.MetadataToken.CompareTo(b.MetadataToken));

            foreach (var prop in props)
            {
                // Exclude [HumlIgnore] properties
                if (prop.GetCustomAttribute<HumlIgnoreAttribute>() != null)
                    continue;

                // Resolve [HumlProperty] name and OmitIfDefault
                var humlProp = prop.GetCustomAttribute<HumlPropertyAttribute>();
                string humlKey = (humlProp?.Name is { Length: > 0 } name) ? name : prop.Name;
                bool omitIfDefault = humlProp?.OmitIfDefault ?? false;
                bool? inline = humlProp?.Inline switch
                {
                    InlineMode.Inline    => true,
                    InlineMode.Multiline => false,
                    _                   => null,
                };

                // Detect init-only setter via IsExternalInit custom modifier
                bool isInitOnly = DetectInitOnly(prop);

                object? defaultValue = omitIfDefault
                    ? (prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null)
                    : null;

                result.Add(new PropertyDescriptor(humlKey, prop, omitIfDefault, isInitOnly, defaultValue, inline));
            }
        }

        var ordered = result.ToArray();

        // Build the keyed dictionary for O(1) deserialiser lookup.
        // last-write-wins on duplicate HumlKey — duplicate keys are an application-level misuse.
        var byKey = new Dictionary<string, PropertyDescriptor>(ordered.Length, StringComparer.Ordinal);
        foreach (var d in ordered)
            byKey[d.HumlKey] = d;

        return new PropertyDescriptorCache(ordered, byKey);
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="property"/> has an <c>init</c>-only setter.
    /// Detection is based on the <c>IsExternalInit</c> required custom modifier on the setter's
    /// return parameter — the same mechanism the C# compiler uses.
    /// </summary>
    private static bool DetectInitOnly(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod(nonPublic: false);
        if (setMethod == null)
            return false;

        var modifiers = setMethod.ReturnParameter.GetRequiredCustomModifiers();
        foreach (var m in modifiers)
        {
            if (string.Equals(m.FullName, "System.Runtime.CompilerServices.IsExternalInit", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
