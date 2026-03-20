using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Versioning;

public class HumlOptionsTests
{
    [Fact]
    public void Default_SpecVersion_is_V0_2()
    {
        HumlOptions.Default.SpecVersion.Should().Be(HumlSpecVersion.V0_2);
    }

    [Fact]
    public void Default_VersionSource_is_Options()
    {
        HumlOptions.Default.VersionSource.Should().Be(VersionSource.Options);
    }

    [Fact]
    public void Default_UnknownVersionBehaviour_is_Throw()
    {
        HumlOptions.Default.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.Throw);
    }

    [Fact]
    public void AutoDetect_VersionSource_is_Header()
    {
        HumlOptions.AutoDetect.VersionSource.Should().Be(VersionSource.Header);
    }

    [Fact]
    public void AutoDetect_SpecVersion_is_V0_2()
    {
        HumlOptions.AutoDetect.SpecVersion.Should().Be(HumlSpecVersion.V0_2);
    }

    [Fact]
    public void AutoDetect_UnknownVersionBehaviour_is_Throw()
    {
        HumlOptions.AutoDetect.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.Throw);
    }

    [Fact]
    public void Custom_options_via_object_initialiser()
    {
        var options = new HumlOptions
        {
            SpecVersion = HumlSpecVersion.V0_2,
            VersionSource = VersionSource.Header,
            UnknownVersionBehaviour = UnknownVersionBehaviour.UseLatest,
        };

        options.SpecVersion.Should().Be(HumlSpecVersion.V0_2);
        options.VersionSource.Should().Be(VersionSource.Header);
        options.UnknownVersionBehaviour.Should().Be(UnknownVersionBehaviour.UseLatest);
    }
}
