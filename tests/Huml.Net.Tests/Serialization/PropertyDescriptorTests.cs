using AwesomeAssertions;
using Huml.Net.Serialization;
using Xunit;

// Alias to avoid potential namespace ambiguities
using HumlPropertyAttribute = Huml.Net.Serialization.HumlPropertyAttribute;
using HumlIgnoreAttribute = Huml.Net.Serialization.HumlIgnoreAttribute;

namespace Huml.Net.Tests.Serialization;

public class PropertyDescriptorTests
{
    // ── Test POCOs ────────────────────────────────────────────────────────────

    private class SimplePoco
    {
        public string? Zebra { get; set; }
        public int Alpha { get; set; }
        public bool Middle { get; set; }
    }

    private class IgnoredPoco
    {
        public string? Visible { get; set; }
        [HumlIgnore]
        public string? Hidden { get; set; }
    }

    private class RenamedPoco
    {
        [HumlProperty("custom_name")]
        public string? Original { get; set; }
    }

    private class InitOnlyPoco
    {
        public string? Name { get; init; }
    }

    private class InheritedPoco : SimplePoco
    {
        public string? Extra { get; set; }
    }

    private class OmitDefaultPoco
    {
        [HumlProperty(OmitIfDefault = true)]
        public int Value { get; set; }
    }

    private class KebabPoco
    {
        public string? FullName { get; set; }
        public int MaxDepth { get; set; }
    }

    private class AttributeWinsPoco
    {
        [HumlProperty("explicit-key")]
        public string? PropertyName { get; set; }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    public PropertyDescriptorTests()
    {
        // Clear cache before each test for isolation
        PropertyDescriptor.ClearCache();
    }

    [Fact]
    public void GetDescriptors_SimplePoco_ReturnsInDeclarationOrder()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        descriptors.Should().HaveCount(3);
        descriptors[0].HumlKey.Should().Be("Zebra");
        descriptors[1].HumlKey.Should().Be("Alpha");
        descriptors[2].HumlKey.Should().Be("Middle");
    }

    [Fact]
    public void GetDescriptors_ExcludesHumlIgnoreProperties()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(IgnoredPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].HumlKey.Should().Be("Visible");
    }

    [Fact]
    public void GetDescriptors_ResolvesHumlPropertyName()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(RenamedPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].HumlKey.Should().Be("custom_name");
    }

    [Fact]
    public void GetDescriptors_DetectsInitOnlyProperty()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(InitOnlyPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].IsInitOnly.Should().BeTrue();
    }

    [Fact]
    public void GetDescriptors_RegularProperty_IsNotInitOnly()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        descriptors[0].IsInitOnly.Should().BeFalse();
    }

    [Fact]
    public void GetDescriptors_ReturnsSameArrayReferenceOnSecondCall()
    {
        var first = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));
        var second = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        object.ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void GetDescriptors_InheritedPoco_ReturnsBasePropertiesBeforeDerived()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(InheritedPoco));

        // Base class (SimplePoco) properties first, then InheritedPoco's Extra
        descriptors.Should().HaveCount(4);
        descriptors[0].HumlKey.Should().Be("Zebra");
        descriptors[1].HumlKey.Should().Be("Alpha");
        descriptors[2].HumlKey.Should().Be("Middle");
        descriptors[3].HumlKey.Should().Be("Extra");
    }

    [Fact]
    public void GetDescriptors_OmitIfDefault_ReflectsAttribute()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(OmitDefaultPoco));

        descriptors.Should().HaveCount(1);
        descriptors[0].OmitIfDefault.Should().BeTrue();
    }

    [Fact]
    public void GetDescriptors_DefaultProperty_OmitIfDefaultIsFalse()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(SimplePoco));

        descriptors[0].OmitIfDefault.Should().BeFalse();
    }

    [Fact]
    public void GetLookup_ReturnsDictionaryKeyedByHumlKey()
    {
        var lookup = PropertyDescriptor.GetLookup(typeof(SimplePoco));

        lookup.Should().HaveCount(3);
        lookup.Should().ContainKey("Zebra");
        lookup.Should().ContainKey("Alpha");
        lookup.Should().ContainKey("Middle");
        lookup["Zebra"].HumlKey.Should().Be("Zebra");
    }

    [Fact]
    public void GetLookup_ReturnsSameReferenceOnSecondCall()
    {
        var first = PropertyDescriptor.GetLookup(typeof(SimplePoco));
        var second = PropertyDescriptor.GetLookup(typeof(SimplePoco));

        object.ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void GetLookup_RenamedProperty_UsesHumlKeyNotPropertyName()
    {
        var lookup = PropertyDescriptor.GetLookup(typeof(RenamedPoco));

        lookup.Should().ContainKey("custom_name");
        lookup.Should().NotContainKey("Original");
    }

    [Fact]
    public void ClearCache_ClearsLookupDictionary()
    {
        var first = PropertyDescriptor.GetLookup(typeof(SimplePoco));
        PropertyDescriptor.ClearCache();
        var second = PropertyDescriptor.GetLookup(typeof(SimplePoco));

        object.ReferenceEquals(first, second).Should().BeFalse();
    }

    // ── Naming policy tests (NP-07, NP-08) ───────────────────────────────────

    [Fact]
    public void GetDescriptors_WithKebabCasePolicy_TransformsKeys()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(KebabPoco), HumlNamingPolicy.KebabCase);

        descriptors.Should().HaveCount(2);
        descriptors[0].HumlKey.Should().Be("full-name");
        descriptors[1].HumlKey.Should().Be("max-depth");
    }

    [Fact]
    public void GetDescriptors_HumlPropertyAttributeWinsOverPolicy()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(AttributeWinsPoco), HumlNamingPolicy.KebabCase);

        descriptors.Should().HaveCount(1);
        descriptors[0].HumlKey.Should().Be("explicit-key"); // attribute, NOT "property-name"
    }

    [Fact]
    public void GetDescriptors_DifferentPolicies_ReturnSeparateCacheEntries()
    {
        var noPolicy = PropertyDescriptor.GetDescriptors(typeof(KebabPoco), null);
        var withKebab = PropertyDescriptor.GetDescriptors(typeof(KebabPoco), HumlNamingPolicy.KebabCase);

        object.ReferenceEquals(noPolicy, withKebab).Should().BeFalse();
        noPolicy[0].HumlKey.Should().Be("FullName");    // identity
        withKebab[0].HumlKey.Should().Be("full-name");  // kebab
    }

    [Fact]
    public void GetDescriptors_SamePolicyCached_ReturnsSameReference()
    {
        var first = PropertyDescriptor.GetDescriptors(typeof(KebabPoco), HumlNamingPolicy.KebabCase);
        var second = PropertyDescriptor.GetDescriptors(typeof(KebabPoco), HumlNamingPolicy.KebabCase);

        object.ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void ClearCache_ClearsAllPolicyEntries()
    {
        var before = PropertyDescriptor.GetDescriptors(typeof(KebabPoco), HumlNamingPolicy.KebabCase);
        PropertyDescriptor.ClearCache();
        var after = PropertyDescriptor.GetDescriptors(typeof(KebabPoco), HumlNamingPolicy.KebabCase);

        object.ReferenceEquals(before, after).Should().BeFalse();
    }

    [Fact]
    public void GetLookup_WithKebabCasePolicy_KeysAreTransformed()
    {
        var lookup = PropertyDescriptor.GetLookup(typeof(KebabPoco), HumlNamingPolicy.KebabCase);

        lookup.Should().ContainKey("full-name");
        lookup.Should().ContainKey("max-depth");
        lookup.Should().NotContainKey("FullName");
        lookup.Should().NotContainKey("MaxDepth");
    }
}
