namespace Huml.Net.Versioning;

/// <summary>Identifies a HUML specification version.</summary>
public enum HumlSpecVersion : int
{
    /// <summary>
    /// HUML specification v0.1.
    /// </summary>
    /// <remarks>Deprecated: v0.1 will be dropped from the support window when v0.3 ships.
    /// Migrate to <see cref="V0_2"/>.</remarks>
    [Obsolete("HumlSpecVersion.V0_1 is deprecated. HUML v0.1 will leave the support window " +
              "when v0.3 ships. Migrate to HumlSpecVersion.V0_2.")]
    V0_1 = 1,

    /// <summary>HUML specification v0.2.</summary>
    V0_2 = 2,
}
