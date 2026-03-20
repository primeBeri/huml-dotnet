namespace Huml.Net.Versioning;

/// <summary>
/// Behaviour when a document header declares a spec version outside the support window.
/// </summary>
public enum UnknownVersionBehaviour
{
    /// <summary>Throw <see cref="Exceptions.HumlUnsupportedVersionException"/>.</summary>
    Throw,

    /// <summary>Silently use the latest supported version.</summary>
    UseLatest,

    /// <summary>Silently use the nearest older supported version.</summary>
    UsePrevious,
}
