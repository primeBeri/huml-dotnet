namespace Huml.Net.Versioning;

/// <summary>
/// Internal constants defining the HUML spec version support window.
/// All version-policy references in the library must use these constants
/// — never inline literal version strings.
/// </summary>
internal static class SpecVersionPolicy
{
    /// <summary>The oldest spec version in the current support window.</summary>
    public const string MinimumSupported = "v0.1";

    /// <summary>The newest spec version supported by this library build.</summary>
    public const string Latest = "v0.2";

    /// <summary>The enum value corresponding to <see cref="MinimumSupported"/>.</summary>
    /// <remarks>Requires <see cref="HumlSpecVersion"/> to remain <c>int</c>-backed for <c>const</c> legality.</remarks>
#pragma warning disable CS0618
    public const HumlSpecVersion MinimumSupportedVersion = HumlSpecVersion.V0_1;
#pragma warning restore CS0618

    /// <summary>The enum value corresponding to <see cref="Latest"/>.</summary>
    public const HumlSpecVersion LatestVersion = HumlSpecVersion.V0_2;
}
