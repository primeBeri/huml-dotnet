using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Versioning;

public class SpecVersionPolicyTests
{
    [Fact]
    public void MinimumSupported_is_v0_1()
    {
        SpecVersionPolicy.MinimumSupported.Should().Be("v0.1");
    }

    [Fact]
    public void Latest_is_v0_2()
    {
        SpecVersionPolicy.Latest.Should().Be("v0.2");
    }

    [Fact]
    public void MinimumSupportedVersion_is_V0_1()
    {
#pragma warning disable CS0618
        SpecVersionPolicy.MinimumSupportedVersion.Should().Be(HumlSpecVersion.V0_1);
#pragma warning restore CS0618
    }

    [Fact]
    public void LatestVersion_is_V0_2()
    {
        SpecVersionPolicy.LatestVersion.Should().Be(HumlSpecVersion.V0_2);
    }
}
