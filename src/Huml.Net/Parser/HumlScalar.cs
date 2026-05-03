namespace Huml.Net.Parser;

/// <summary>
/// Represents a scalar value.
/// </summary>
/// <param name="Kind">The kind of scalar (string, integer, float, bool, null, nan, or inf).</param>
/// <param name="Value">
/// The runtime value of the scalar.
/// <list type="bullet">
///   <item><description><see cref="ScalarKind.Null"/>: always <c>null</c>.</description></item>
///   <item><description><see cref="ScalarKind.NaN"/>: the raw token string <c>"nan"</c>.</description></item>
///   <item><description><see cref="ScalarKind.Inf"/>: the raw token string <c>"+inf"</c>, <c>"-inf"</c>, or <c>"inf"</c>.</description></item>
///   <item><description>All other kinds: the parsed .NET value (<c>string</c>, <c>long</c>, <c>double</c>, <c>bool</c>).</description></item>
/// </list>
/// </param>
public sealed record HumlScalar(ScalarKind Kind, object? Value) : HumlNode;
