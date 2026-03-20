namespace Huml.Net.Versioning;

/// <summary>Determines where the spec version is read from.</summary>
public enum VersionSource
{
    /// <summary>Use the <c>SpecVersion</c> property from <c>HumlOptions</c> (explicit, caller-provided).</summary>
    Options,

    /// <summary>Read from the <c>%HUML vX.Y.Z</c> document header directive.</summary>
    Header,
}
