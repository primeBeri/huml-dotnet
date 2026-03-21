namespace Huml.Net.Lexer;

/// <summary>A single lexical token produced by the HUML lexer.</summary>
public readonly record struct Token
{
    /// <summary>The classification of this token.</summary>
    public TokenType Type { get; init; }

    /// <summary>The raw text value for this token, or <c>null</c> for structural tokens.</summary>
    public string? Value { get; init; }

    /// <summary>1-based line number where the token starts.</summary>
    public int Line { get; init; }

    /// <summary>0-based column position where the token starts.</summary>
    public int Column { get; init; }

    /// <summary>Indentation level (in spaces) of the line containing this token.</summary>
    public int Indent { get; init; }

    /// <summary>Whether whitespace preceded this token on the same line.</summary>
    public bool SpaceBefore { get; init; }
}
