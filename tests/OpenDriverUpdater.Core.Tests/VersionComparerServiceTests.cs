using Xunit;
using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Core.Services;

namespace OpenDriverUpdater.Core.Tests;

public class VersionComparerServiceTests
{
    private readonly VersionComparerService _sut = new();

    [Fact]
    public void Compare_NewerCandidate_ReturnsIsNewer()
    {
        var result = _sut.Compare("1.0.0", "2.0.0");
        Assert.True(result.IsNewer);
    }

    [Fact]
    public void Compare_SameVersion_ReturnsNotNewer()
    {
        var result = _sut.Compare("1.0.0", "1.0.0");
        Assert.False(result.IsNewer);
    }

    [Fact]
    public void Compare_OlderCandidate_ReturnsNotNewer()
    {
        var result = _sut.Compare("3.0.0", "1.0.0");
        Assert.False(result.IsNewer);
    }

    [Fact]
    public void Compare_NullInstalled_ReturnsIsNewer()
    {
        var result = _sut.Compare(null, "1.0.0");
        Assert.True(result.IsNewer);
    }

    [Fact]
    public void Compare_NullCandidate_ReturnsNotNewer()
    {
        var result = _sut.Compare("1.0.0", null);
        Assert.False(result.IsNewer);
    }

    [Fact]
    public void Compare_WithVPrefix_ParsesCorrectly()
    {
        var result = _sut.Compare("v1.0.0", "v2.0.0");
        Assert.True(result.IsNewer);
    }

    [Fact]
    public void Compare_FourPartVersion_Works()
    {
        var result = _sut.Compare("10.0.19041.1", "10.0.19041.2");
        Assert.True(result.IsNewer);
    }
}
