namespace Huml.Net.Parser;

/// <summary>Base type for all HUML AST nodes.</summary>
/// <remarks>
/// <see cref="Line"/> and <see cref="Column"/> are declared in the record body (not in a
/// primary constructor). To ensure they are excluded from the auto-generated <c>Equals</c> and
/// <c>GetHashCode</c> of every derived record, this base record overrides <c>Equals(HumlNode?)</c>
/// and <c>GetHashCode</c> to ignore these properties. Two nodes representing the same HUML value
/// parsed from different source positions remain structurally equal — only primary-constructor
/// parameters of the concrete derived type participate in equality.
/// </remarks>
public abstract record HumlNode
{
    /// <summary>1-based line number in the source document where this node begins, or 0 if unknown.</summary>
    public int Line { get; init; }

    /// <summary>0-based column position in the source document where this node begins, or 0 if unknown.</summary>
    public int Column { get; init; }

    /// <summary>
    /// Overrides base-record equality to exclude <see cref="Line"/> and <see cref="Column"/>
    /// from the equality check. Derived record types synthesise their own
    /// <c>Equals(DerivedType?)</c> that calls this method for the base portion; by returning
    /// <see langword="true"/> for any two nodes of the same runtime type, we ensure only the
    /// derived type's primary-constructor parameters contribute to structural equality.
    /// </summary>
    public virtual bool Equals(HumlNode? other) =>
        other is not null && EqualityContract == other.EqualityContract;

    /// <summary>
    /// Returns a hash code based only on the runtime type, consistent with the overridden
    /// <see cref="Equals(HumlNode?)"/> that excludes <see cref="Line"/> and <see cref="Column"/>.
    /// Derived types override this further to incorporate their primary-constructor parameters.
    /// </summary>
    public override int GetHashCode() => EqualityContract.GetHashCode();
}
