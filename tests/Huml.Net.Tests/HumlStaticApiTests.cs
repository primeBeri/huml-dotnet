using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Parser;
using Xunit;

namespace Huml.Net.Tests;

public class HumlStaticApiTests
{
    // ── Test POCO ─────────────────────────────────────────────────────────────

    private class RoundTripPoco
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    // ── Serialize<T> ─────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_Generic_ReturnsHumlStringWithExpectedKeyValuePairs()
    {
        var poco = new RoundTripPoco { Name = "Alice", Age = 42 };

        var result = Huml.Serialize(poco);

        result.Should().Contain("Name: \"Alice\"");
        result.Should().Contain("Age: 42");
    }

    // ── Serialize(object?, Type, options) ────────────────────────────────────

    [Fact]
    public void Serialize_ObjectTypeOverload_ReturnsHumlStringWithVersionHeader()
    {
        var poco = new RoundTripPoco { Name = "Alice", Age = 42 };

        var result = Huml.Serialize(poco, typeof(RoundTripPoco));

        result.Should().StartWith("%HUML v0.2.0");
        result.Should().Contain("Name: \"Alice\"");
    }

    // ── Deserialize<T>(string) ────────────────────────────────────────────────

    [Fact]
    public void Deserialize_String_RoundTripsPocoToEqualPropertyValues()
    {
        var poco = new RoundTripPoco { Name = "Alice", Age = 42 };
        var huml = Huml.Serialize(poco);

        var result = Huml.Deserialize<RoundTripPoco>(huml);

        result.Name.Should().Be("Alice");
        result.Age.Should().Be(42);
    }

    // ── Deserialize<T>(ReadOnlySpan<char>) ───────────────────────────────────

    [Fact]
    public void Deserialize_Span_RoundTripsPocoToEqualPropertyValues()
    {
        var poco = new RoundTripPoco { Name = "Alice", Age = 42 };
        var huml = Huml.Serialize(poco);

        var result = Huml.Deserialize<RoundTripPoco>(huml.AsSpan());

        result.Name.Should().Be("Alice");
        result.Age.Should().Be(42);
    }

    // ── Deserialize(string, Type) ─────────────────────────────────────────────

    [Fact]
    public void Deserialize_Untyped_ReturnsCastableObjectWithCorrectProperties()
    {
        var poco = new RoundTripPoco { Name = "Alice", Age = 42 };
        var huml = Huml.Serialize(poco);

        var result = Huml.Deserialize(huml, typeof(RoundTripPoco));

        var cast = result.Should().BeOfType<RoundTripPoco>().Subject;
        cast.Name.Should().Be("Alice");
        cast.Age.Should().Be(42);
    }

    // ── Parse(string) ─────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ValidInput_ReturnsHumlDocumentWithNonEmptyEntries()
    {
        var result = Huml.Parse("key: true\n");

        result.Should().BeOfType<HumlDocument>();
        result.Entries.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_TabIndentedInput_ThrowsHumlParseException()
    {
        var act = () => Huml.Parse("\tkey: true\n");

        act.Should().Throw<HumlParseException>();
    }

    // ── Deserialize error path ────────────────────────────────────────────────

    [Fact]
    public void Deserialize_InvalidHuml_ThrowsHumlParseException()
    {
        var act = () => Huml.Deserialize<RoundTripPoco>("\tkey: true");

        act.Should().Throw<HumlParseException>();
    }
}
