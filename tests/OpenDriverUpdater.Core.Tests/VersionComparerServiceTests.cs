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
    public void Compare_NullInstalled_ReturnsNotNewer()
    {
        // Yüklü sürüm bilinmiyorsa karşılaştırma yapılamaz. "Sürücü hiç yok"
        // durumu DriverResolverService.HasNoDriverInstalled içinde ele alınır.
        var result = _sut.Compare(null, "1.0.0");
        Assert.False(result.IsNewer);
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
    // ── Regression: unparseable versions must never compare as newer ──
    // Microsoft Update Catalog'un Version sütunu sürücü kayıtlarının
    // çoğunda "n/a" döner. Önceki ordinal string karşılaştırma geri
    // dönüşünde "n/a" her sayısal sürümden büyük sıralanıyordu — bu da
    // fiilen her Catalog sonucunu güncelleme olarak öneriyordu.

    [Theory]
    [InlineData("10.0.26100.8737", "n/a")]
    [InlineData("10.0.26100.8737", "unknown")]
    [InlineData("10.0.26100.8737", "Windows 11")]
    public void Compare_UnparseableCandidate_ReturnsNotNewer(string installed, string candidate)
    {
        var result = _sut.Compare(installed, candidate);
        Assert.False(result.IsNewer);
    }

    [Fact]
    public void Compare_UnparseableInstalled_ReturnsNotNewer()
    {
        var result = _sut.Compare("n/a", "10.1.34.10");
        Assert.False(result.IsNewer);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Compare_BlankInstalledVersion_ReturnsNotNewer(string installed)
    {
        var result = _sut.Compare(installed, "10.1.34.10");
        Assert.False(result.IsNewer);
    }

    // ── Gerçek sürücü sürümleriyle doğrulama ──────────────────────────

    [Theory]
    [InlineData("2.1.5.7", "2.1.5.10")]         // Intel I225-V NIC
    [InlineData("1.41.1340.0", "1.41.1423.0")]  // Intel Thunderbolt
    [InlineData("10.1.34.9", "10.1.34.10")]     // sayısal, sözlüksel değil
    public void Compare_RealDriverVersions_ComparedNumerically(string installed, string candidate)
    {
        var result = _sut.Compare(installed, candidate);
        Assert.True(result.IsNewer);
    }

    [Fact]
    public void Compare_PopulatesBothVersionsOnResult()
    {
        var result = _sut.Compare("2.1.5.7", "2.1.5.10");

        Assert.Equal("2.1.5.7", result.InstalledVersion);
        Assert.Equal("2.1.5.10", result.CandidateVersion);
        Assert.False(string.IsNullOrWhiteSpace(result.Delta));
    }
}
