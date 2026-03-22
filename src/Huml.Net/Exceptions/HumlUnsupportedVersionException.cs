using Huml.Net.Versioning;

namespace Huml.Net.Exceptions;

/// <summary>
/// Thrown when a HUML document declares a spec version outside the supported range
/// and <see cref="UnknownVersionBehaviour"/> is <see cref="UnknownVersionBehaviour.Throw"/>.
/// </summary>
public sealed class HumlUnsupportedVersionException : Exception
{
    /// <summary>The version string as declared in the document header.</summary>
    public string DeclaredVersion { get; }

    /// <summary>
    /// Initialises a new instance with the declared version string.
    /// The message includes the support window from <see cref="SpecVersionPolicy"/>.
    /// </summary>
    /// <param name="declaredVersion">The version string from the document header (e.g. "v0.3").</param>
    public HumlUnsupportedVersionException(string declaredVersion)
        : base(
            $"Unsupported HUML spec version '{declaredVersion}'. " +
            $"Supported range: {SpecVersionPolicy.MinimumSupported} \u2013 {SpecVersionPolicy.Latest}.")
    {
        DeclaredVersion = declaredVersion;
    }
}
