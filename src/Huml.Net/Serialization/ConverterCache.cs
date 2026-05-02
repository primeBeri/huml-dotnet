using System.Collections.Concurrent;
using System.Reflection;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Resolves and caches the effective <see cref="HumlConverter"/> for a given (Type, options)
/// pair using three-level priority: property-level attribute (handled via PropertyDescriptor),
/// then type-level attribute, then HumlOptions.Converters.
/// </summary>
internal static class ConverterCache
{
    private static readonly ConcurrentDictionary<(Type, int), HumlConverter?> Cache = new();

    /// <summary>
    /// Returns the effective converter for <paramref name="targetType"/> from type-level
    /// [HumlConverter] attribute or the options Converters list, or <c>null</c> if none apply.
    /// Property-level converters are already resolved in PropertyDescriptor.Converter and must
    /// be checked by callers before invoking this method.
    /// </summary>
    internal static HumlConverter? TryGet(Type targetType, HumlOptions options)
    {
        // Level 2: type-level [HumlConverter] attribute on the target type
        var typeAttr = targetType.GetCustomAttribute<HumlConverterAttribute>();
        if (typeAttr != null)
            return GetOrCreate(typeAttr.ConverterType);

        // Level 3: HumlOptions.Converters — first CanConvert match wins
        foreach (var c in options.Converters)
            if (c.CanConvert(targetType)) return c;

        return null;
    }

    /// <summary>Clears the cache. Use in test teardown for isolation.</summary>
    internal static void ClearCache() => Cache.Clear();

    private static HumlConverter GetOrCreate(Type converterType)
    {
        // Cache key: use converterType.GetHashCode() as the int part (type is unique)
        var key = (converterType, converterType.GetHashCode());
        return Cache.GetOrAdd(key, static k =>
        {
            try
            {
                return (HumlConverter)Activator.CreateInstance(k.Item1)!;
            }
            catch (MissingMethodException)
            {
                throw new InvalidOperationException(
                    $"Converter type '{k.Item1.Name}' has no accessible parameterless constructor.");
            }
        })!;
    }
}
