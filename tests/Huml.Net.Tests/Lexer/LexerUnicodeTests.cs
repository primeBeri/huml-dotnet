using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Lexer;
using Huml.Net.Versioning;
using Xunit;
using HumlLexer = Huml.Net.Lexer.Lexer;

namespace Huml.Net.Tests.Lexer;

public class LexerUnicodeTests
{
    private static List<Token> LexAll(string input, HumlOptions? options = null)
    {
        var lexer = new HumlLexer(input, options ?? HumlOptions.Default);
        var tokens = new List<Token>();
        Token t;
        do { t = lexer.NextToken(); tokens.Add(t); } while (t.Type != TokenType.Eof);
        return tokens;
    }

    // -----------------------------------------------------------------------
    // Non-ASCII bare key error tests — actionable message
    // -----------------------------------------------------------------------

    [Fact]
    public void NonAsciiLetter_AtKeyPosition_ThrowsActionableError()
    {
        var act = () => LexAll("اسم: \"v\"\n");
        var ex = act.Should().Throw<HumlParseException>().Which;
        ex.Message.Should().Contain("Bare keys must start with [a-zA-Z]");
        ex.Message.Should().Contain("quoted keys");
    }

    [Fact]
    public void ChineseCharacter_AtKeyPosition_ThrowsActionableError()
    {
        var act = () => LexAll("名: \"v\"\n");
        act.Should().Throw<HumlParseException>()
            .Which.Message.Should().Contain("Bare keys must start with [a-zA-Z]");
    }

    [Fact]
    public void CyrillicCharacter_AtKeyPosition_ThrowsActionableError()
    {
        var act = () => LexAll("Д: \"v\"\n");
        act.Should().Throw<HumlParseException>()
            .Which.Message.Should().Contain("Bare keys must start with [a-zA-Z]");
    }

    [Fact]
    public void Emoji_AtKeyPosition_ThrowsGenericError()
    {
        // Emoji surrogate high byte returns false from char.IsLetter — falls through to generic error
        var act = () => LexAll("🚀: \"v\"\n");
        var ex = act.Should().Throw<HumlParseException>().Which;
        ex.Message.Should().Contain("Unexpected character");
        ex.Message.Should().NotContain("Bare keys must start with [a-zA-Z]");
    }

    // -----------------------------------------------------------------------
    // Unicode in quoted strings — correct token type and value
    // -----------------------------------------------------------------------

    [Fact]
    public void ArabicText_InQuotedString_LexesCorrectly()
    {
        var tokens = LexAll("key: \"مرحبا\"\n");
        var stringToken = tokens.First(t => t.Type == TokenType.String);
        stringToken.Value.Should().Be("مرحبا");
    }

    [Fact]
    public void HebrewText_InQuotedString_LexesCorrectly()
    {
        var tokens = LexAll("key: \"שלום\"\n");
        var stringToken = tokens.First(t => t.Type == TokenType.String);
        stringToken.Value.Should().Be("שלום");
    }

    [Fact]
    public void ArabicText_InQuotedKey_LexesAsQuotedKey()
    {
        var tokens = LexAll("\"اسم\": \"v\"\n");
        var quotedKeyToken = tokens.First(t => t.Type == TokenType.QuotedKey);
        quotedKeyToken.Value.Should().Be("اسم");
    }

    [Fact]
    public void ChineseText_InQuotedKey_LexesAsQuotedKey()
    {
        var tokens = LexAll("\"名前\": \"v\"\n");
        var quotedKeyToken = tokens.First(t => t.Type == TokenType.QuotedKey);
        quotedKeyToken.Value.Should().Be("名前");
    }

    [Fact]
    public void Emoji_InQuotedString_LexesCorrectly()
    {
        var tokens = LexAll("key: \"🚀🌍\"\n");
        var stringToken = tokens.First(t => t.Type == TokenType.String);
        stringToken.Value.Should().Be("🚀🌍");
    }

    [Fact]
    public void Emoji_InQuotedKey_LexesAsQuotedKey()
    {
        var tokens = LexAll("\"🚀\": \"v\"\n");
        var quotedKeyToken = tokens.First(t => t.Type == TokenType.QuotedKey);
        quotedKeyToken.Value.Should().Be("🚀");
    }

    // -----------------------------------------------------------------------
    // Bidi control characters and mixed LTR/RTL
    // -----------------------------------------------------------------------

    [Fact]
    public void BidiControlChars_InQuotedString_PassThrough()
    {
        var input = $"key: \"text\u200Fmore\u200Eend\"\n";
        var tokens = LexAll(input);
        var stringToken = tokens.First(t => t.Type == TokenType.String);
        stringToken.Value.Should().Contain("\u200F");
        stringToken.Value.Should().Contain("\u200E");
    }

    [Fact]
    public void MixedLtrRtl_InQuotedString_PreservesContent()
    {
        var tokens = LexAll("key: \"Hello مرحبا World\"\n");
        var stringToken = tokens.First(t => t.Type == TokenType.String);
        stringToken.Value.Should().Be("Hello مرحبا World");
    }

    // -----------------------------------------------------------------------
    // Error location accuracy
    // -----------------------------------------------------------------------

    [Fact]
    public void NonAsciiError_ReportsCorrectLineAndColumn()
    {
        var act = () => LexAll("a: 1\nب: 2\n");
        var ex = act.Should().Throw<HumlParseException>().Which;
        ex.Line.Should().Be(2);
        ex.Column.Should().Be(0);
    }
}
