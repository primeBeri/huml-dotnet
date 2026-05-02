using System.Text;
using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Provides the write-path context passed to <see cref="HumlConverter{T}.Write"/>.
/// Wraps the internal <see cref="StringBuilder"/> accumulator and serialiser options,
/// exposing append methods without leaking internal types.
/// </summary>
public sealed class HumlSerializerContext
{
    private readonly StringBuilder _sb;
    private readonly int _depth;

    /// <summary>The serialisation options active for the current operation.</summary>
    public HumlOptions Options { get; }

    internal HumlSerializerContext(StringBuilder sb, int depth, HumlOptions options)
    {
        _sb = sb;
        _depth = depth;
        Options = options;
    }

    /// <summary>
    /// Appends the serialised representation of <paramref name="value"/> using built-in
    /// type dispatch. Use for nested values whose HUML encoding is handled by the library.
    /// </summary>
    /// <param name="value">The value to serialise.</param>
    /// <remarks>
    /// Do NOT call this method with a value of the same type the calling converter handles —
    /// doing so causes infinite recursion. Use <see cref="AppendRaw"/> for the converter's
    /// own output type.
    /// </remarks>
    public void AppendSerializedValue(object? value)
        => HumlSerializer.SerializeValueInternal(_sb, value, _depth, Options);

    /// <summary>
    /// Appends a raw HUML fragment verbatim. Use when the converter produces custom HUML
    /// syntax that cannot be expressed through <see cref="AppendSerializedValue"/>.
    /// </summary>
    /// <param name="huml">A raw HUML string fragment. Must be trusted content; untrusted
    /// input can produce malformed HUML.</param>
    public void AppendRaw(string huml) => _sb.Append(huml);

    /// <summary>
    /// The current serialisation depth. Converters emitting indented block content can
    /// use this to compute indentation.
    /// </summary>
    public int Depth => _depth;
}
