using AwesomeAssertions;
using Huml.Net.Parser;
using Huml.Net.Versioning;
using Xunit;

// Alias to avoid collision between the Huml.Net.Tests.Parser namespace and the parser class.
using HumlParser = Huml.Net.Parser.HumlParser;

namespace Huml.Net.Tests.Parser;

public class HumlNodePositionTests
{
    // ── Default values (POS-09) ──────────────────────────────────────────────

    [Fact]
    public void HumlScalar_default_Line_and_Column_are_zero()
    {
        var node = new HumlScalar(ScalarKind.Null, null);
        node.Line.Should().Be(0);
        node.Column.Should().Be(0);
    }

    [Fact]
    public void HumlMapping_default_Line_and_Column_are_zero()
    {
        var node = new HumlMapping("k", new HumlScalar(ScalarKind.Null, null));
        node.Line.Should().Be(0);
        node.Column.Should().Be(0);
    }

    [Fact]
    public void HumlSequence_default_Line_and_Column_are_zero()
    {
        var node = new HumlSequence(Array.Empty<HumlNode>());
        node.Line.Should().Be(0);
        node.Column.Should().Be(0);
    }

    [Fact]
    public void HumlDocument_default_Line_and_Column_are_zero()
    {
        var node = new HumlDocument(Array.Empty<HumlNode>());
        node.Line.Should().Be(0);
        node.Column.Should().Be(0);
    }

    [Fact]
    public void HumlInlineMapping_default_Line_and_Column_are_zero()
    {
        var node = new HumlInlineMapping(Array.Empty<HumlNode>());
        node.Line.Should().Be(0);
        node.Column.Should().Be(0);
    }

    // ── Equality preservation (POS-06) ───────────────────────────────────────

    [Fact]
    public void HumlScalar_equality_ignores_position()
    {
        var a = new HumlScalar(ScalarKind.String, "hello") { Line = 1, Column = 0 };
        var b = new HumlScalar(ScalarKind.String, "hello") { Line = 5, Column = 10 };
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void HumlMapping_equality_ignores_position()
    {
        var scalar = new HumlScalar(ScalarKind.Integer, 42L);
        var a = new HumlMapping("key", scalar) { Line = 1, Column = 0 };
        var b = new HumlMapping("key", scalar) { Line = 3, Column = 4 };
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void HumlSequence_equality_ignores_position()
    {
        var items = Array.Empty<HumlNode>();
        var a = new HumlSequence(items) { Line = 1, Column = 0 };
        var b = new HumlSequence(items) { Line = 7, Column = 2 };
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void HumlDocument_equality_ignores_position()
    {
        var entries = Array.Empty<HumlNode>();
        var a = new HumlDocument(entries) { Line = 1, Column = 0 };
        var b = new HumlDocument(entries) { Line = 10, Column = 5 };
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void HumlInlineMapping_equality_ignores_position()
    {
        var entries = Array.Empty<HumlNode>();
        var a = new HumlInlineMapping(entries) { Line = 2, Column = 0 };
        var b = new HumlInlineMapping(entries) { Line = 9, Column = 3 };
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    // ── HumlScalar position propagation (POS-01) ─────────────────────────────

    [Fact]
    public void Parse_RootIntegerScalar_HumlScalarHasLine1Column0()
    {
        var doc = new HumlParser("123", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var scalar = doc.Entries[0] as HumlScalar;
        scalar.Should().NotBeNull();
        scalar!.Line.Should().Be(1);
        scalar.Column.Should().Be(0);
    }

    [Fact]
    public void Parse_StringScalarOnLineTwo_HumlScalarHasLine2()
    {
        var doc = new HumlParser("%HUML v0.2.0\n\"hello\"", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var scalar = doc.Entries[0] as HumlScalar;
        scalar.Should().NotBeNull();
        scalar!.Line.Should().Be(2);
    }

    [Fact]
    public void Parse_BoolScalar_HumlScalarHasMatchingPosition()
    {
        var doc = new HumlParser("true", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var scalar = doc.Entries[0] as HumlScalar;
        scalar.Should().NotBeNull();
        scalar!.Line.Should().Be(1);
        scalar.Column.Should().Be(0);
    }

    // ── HumlMapping position propagation (POS-02) ────────────────────────────

    [Fact]
    public void Parse_SimpleMapping_HumlMappingHasKeyTokenPosition()
    {
        var doc = new HumlParser("name: \"alice\"", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0] as HumlMapping;
        mapping.Should().NotBeNull();
        mapping!.Line.Should().Be(1);
        mapping.Column.Should().Be(0);
    }

    [Fact]
    public void Parse_MappingOnSecondLine_HumlMappingHasLine2()
    {
        var doc = new HumlParser("first: 1\nsecond: 2", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(2);
        var second = doc.Entries[1] as HumlMapping;
        second.Should().NotBeNull();
        second!.Line.Should().Be(2);
    }

    [Fact]
    public void Parse_QuotedKeyMapping_HumlMappingHasQuotedKeyPosition()
    {
        var doc = new HumlParser("\"key with space\": 42", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0] as HumlMapping;
        mapping.Should().NotBeNull();
        mapping!.Line.Should().Be(1);
        mapping.Column.Should().Be(0);
    }

    [Fact]
    public void Parse_InlineDictMapping_HumlMappingHasKeyTokenPosition()
    {
        var doc = new HumlParser("items:: { a: 1, b: 2 }", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var outer = doc.Entries[0] as HumlMapping;
        outer.Should().NotBeNull();

        var inlineMapping = outer!.Value as HumlInlineMapping;
        inlineMapping.Should().NotBeNull();

        inlineMapping!.Entries.Should().HaveCount(2);
        foreach (var entry in inlineMapping.Entries)
        {
            var innerMapping = entry as HumlMapping;
            innerMapping.Should().NotBeNull();
            innerMapping!.Line.Should().Be(1);
        }
    }

    // ── HumlSequence position propagation (POS-03) ───────────────────────────

    [Fact]
    public void Parse_MultilineList_HumlSequenceHasIndicatorLine()
    {
        var doc = new HumlParser("items::\n  - 1\n  - 2", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0] as HumlMapping;
        mapping.Should().NotBeNull();

        var seq = mapping!.Value as HumlSequence;
        seq.Should().NotBeNull();
        seq!.Line.Should().Be(1);
        seq.Column.Should().Be(0);
    }

    [Fact]
    public void Parse_RootMultilineList_HumlSequenceHasFirstItemLine()
    {
        var doc = new HumlParser("- 1\n- 2", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var seq = doc.Entries[0] as HumlSequence;
        seq.Should().NotBeNull();
        seq!.Line.Should().Be(1);
    }

    [Fact]
    public void Parse_InlineList_HumlSequenceHasFirstValuePosition()
    {
        var doc = new HumlParser("items:: 1, 2, 3", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0] as HumlMapping;
        mapping.Should().NotBeNull();

        var seq = mapping!.Value as HumlSequence;
        seq.Should().NotBeNull();
        seq!.Line.Should().Be(1);
    }

    // ── HumlDocument position propagation (POS-04) ───────────────────────────

    [Fact]
    public void Parse_RootDocument_HumlDocumentHasFirstTokenLine()
    {
        var doc = new HumlParser("name: \"a\"\nage: 1", HumlOptions.Default).Parse();

        doc.Line.Should().Be(1);
        doc.Column.Should().Be(0);
    }

    [Fact]
    public void Parse_NestedDocument_HumlDocumentHasIndicatorLine()
    {
        var doc = new HumlParser("outer::\n  inner: 1", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var outer = doc.Entries[0] as HumlMapping;
        outer.Should().NotBeNull();

        var nested = outer!.Value as HumlDocument;
        nested.Should().NotBeNull();
        nested!.Line.Should().Be(1);
    }

    [Fact]
    public void Parse_DocumentAfterVersionHeader_HumlDocumentHasLine2()
    {
        var doc = new HumlParser("%HUML v0.2.0\nname: \"a\"", HumlOptions.Default).Parse();

        doc.Line.Should().Be(2);
    }

    // ── HumlInlineMapping position propagation (POS-05) ──────────────────────

    [Fact]
    public void Parse_InlineDict_HumlInlineMappingHasFirstKeyLine()
    {
        var doc = new HumlParser("obj:: { a: 1 }", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0] as HumlMapping;
        mapping.Should().NotBeNull();

        var inlineMapping = mapping!.Value as HumlInlineMapping;
        inlineMapping.Should().NotBeNull();
        inlineMapping!.Line.Should().Be(1);
    }

    [Fact]
    public void Parse_EmptyInlineDict_HumlInlineMappingHasIndicatorLine()
    {
        var doc = new HumlParser("obj:: {}", HumlOptions.Default).Parse();

        doc.Entries.Should().HaveCount(1);
        var mapping = doc.Entries[0] as HumlMapping;
        mapping.Should().NotBeNull();

        var inlineMapping = mapping!.Value as HumlInlineMapping;
        inlineMapping.Should().NotBeNull();
        inlineMapping!.Line.Should().Be(1);
    }
}
