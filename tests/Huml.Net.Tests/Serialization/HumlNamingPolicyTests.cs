using AwesomeAssertions;
using Huml.Net.Serialization;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlNamingPolicyTests
{
    // ── KebabCase tests ───────────────────────────────────────────────────────

    [Fact]
    public void KebabCase_converts_FullName_to_full_name()
    {
        HumlNamingPolicy.KebabCase.ConvertName("FullName").Should().Be("full-name");
    }

    [Fact]
    public void KebabCase_converts_firstName_to_first_name()
    {
        HumlNamingPolicy.KebabCase.ConvertName("firstName").Should().Be("first-name");
    }

    [Fact]
    public void KebabCase_splits_acronym_URL_to_u_r_l()
    {
        HumlNamingPolicy.KebabCase.ConvertName("URL").Should().Be("u-r-l");
    }

    [Fact]
    public void KebabCase_converts_ProfileURL_to_profile_u_r_l()
    {
        HumlNamingPolicy.KebabCase.ConvertName("ProfileURL").Should().Be("profile-u-r-l");
    }

    [Fact]
    public void KebabCase_converts_XMLReader_to_x_m_l_reader()
    {
        HumlNamingPolicy.KebabCase.ConvertName("XMLReader").Should().Be("x-m-l-reader");
    }

    [Fact]
    public void KebabCase_converts_single_uppercase_A_to_a()
    {
        HumlNamingPolicy.KebabCase.ConvertName("A").Should().Be("a");
    }

    [Fact]
    public void KebabCase_converts_Id_to_id()
    {
        HumlNamingPolicy.KebabCase.ConvertName("Id").Should().Be("id");
    }

    [Fact]
    public void KebabCase_converts_MaxDepth_to_max_depth()
    {
        HumlNamingPolicy.KebabCase.ConvertName("MaxDepth").Should().Be("max-depth");
    }

    // ── SnakeCase tests ───────────────────────────────────────────────────────

    [Fact]
    public void SnakeCase_converts_FullName_to_full_name()
    {
        HumlNamingPolicy.SnakeCase.ConvertName("FullName").Should().Be("full_name");
    }

    [Fact]
    public void SnakeCase_splits_acronym_URL_to_u_r_l()
    {
        HumlNamingPolicy.SnakeCase.ConvertName("URL").Should().Be("u_r_l");
    }

    [Fact]
    public void SnakeCase_converts_MaxDepth_to_max_depth()
    {
        HumlNamingPolicy.SnakeCase.ConvertName("MaxDepth").Should().Be("max_depth");
    }

    // ── CamelCase tests ───────────────────────────────────────────────────────

    [Fact]
    public void CamelCase_converts_FullName_to_fullName()
    {
        HumlNamingPolicy.CamelCase.ConvertName("FullName").Should().Be("fullName");
    }

    [Fact]
    public void CamelCase_keeps_firstName_unchanged()
    {
        HumlNamingPolicy.CamelCase.ConvertName("firstName").Should().Be("firstName");
    }

    [Fact]
    public void CamelCase_converts_URL_by_lowercasing_first_letter_only()
    {
        HumlNamingPolicy.CamelCase.ConvertName("URL").Should().Be("uRL");
    }

    [Fact]
    public void CamelCase_converts_single_uppercase_A_to_a()
    {
        HumlNamingPolicy.CamelCase.ConvertName("A").Should().Be("a");
    }

    // ── PascalCase tests ──────────────────────────────────────────────────────

    [Fact]
    public void PascalCase_converts_fullName_to_FullName()
    {
        HumlNamingPolicy.PascalCase.ConvertName("fullName").Should().Be("FullName");
    }

    [Fact]
    public void PascalCase_keeps_FullName_unchanged()
    {
        HumlNamingPolicy.PascalCase.ConvertName("FullName").Should().Be("FullName");
    }

    [Fact]
    public void PascalCase_converts_url_to_Url()
    {
        HumlNamingPolicy.PascalCase.ConvertName("url").Should().Be("Url");
    }

    [Fact]
    public void PascalCase_converts_single_lowercase_a_to_A()
    {
        HumlNamingPolicy.PascalCase.ConvertName("a").Should().Be("A");
    }

    // ── Singleton tests ───────────────────────────────────────────────────────

    [Fact]
    public void KebabCase_returns_same_singleton_instance()
    {
        object.ReferenceEquals(HumlNamingPolicy.KebabCase, HumlNamingPolicy.KebabCase).Should().BeTrue();
    }

    [Fact]
    public void SnakeCase_returns_same_singleton_instance()
    {
        object.ReferenceEquals(HumlNamingPolicy.SnakeCase, HumlNamingPolicy.SnakeCase).Should().BeTrue();
    }

    [Fact]
    public void CamelCase_returns_same_singleton_instance()
    {
        object.ReferenceEquals(HumlNamingPolicy.CamelCase, HumlNamingPolicy.CamelCase).Should().BeTrue();
    }

    [Fact]
    public void PascalCase_returns_same_singleton_instance()
    {
        object.ReferenceEquals(HumlNamingPolicy.PascalCase, HumlNamingPolicy.PascalCase).Should().BeTrue();
    }

    // ── Subclassing test ──────────────────────────────────────────────────────

    [Fact]
    public void HumlNamingPolicy_can_be_subclassed_with_custom_implementation()
    {
        var custom = new IdentityPolicy();
        custom.ConvertName("HelloWorld").Should().Be("HelloWorld");
    }

    private sealed class IdentityPolicy : HumlNamingPolicy
    {
        public override string ConvertName(string name) => name;
    }

    // ── Empty string edge case ────────────────────────────────────────────────

    [Fact]
    public void KebabCase_converts_empty_string_to_empty_string()
    {
        HumlNamingPolicy.KebabCase.ConvertName("").Should().Be("");
    }

    [Fact]
    public void SnakeCase_converts_empty_string_to_empty_string()
    {
        HumlNamingPolicy.SnakeCase.ConvertName("").Should().Be("");
    }

    [Fact]
    public void CamelCase_converts_empty_string_to_empty_string()
    {
        HumlNamingPolicy.CamelCase.ConvertName("").Should().Be("");
    }

    [Fact]
    public void PascalCase_converts_empty_string_to_empty_string()
    {
        HumlNamingPolicy.PascalCase.ConvertName("").Should().Be("");
    }
}
