using Xunit;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Core.Services;

namespace OpenDriverUpdater.Core.Tests;

public class TrustEvaluatorServiceTests
{
    private readonly TrustEvaluatorService _sut = new();

    private static DeviceInfo CreateDevice(string manufacturer = "Intel") => new()
    {
        Id = "PCI\\VEN_8086&DEV_1234",
        Name = "Test Device",
        Manufacturer = manufacturer,
        HardwareIds = ["PCI\\VEN_8086&DEV_1234"]
    };

    [Fact]
    public void CandidateDriver_DefaultMatchBasis_IsQueryOnly()
    {
        var candidate = new CandidateDriver();
        Assert.Equal(HardwareIdMatchBasis.QueryOnly, candidate.MatchBasis);
    }

    [Fact]
    public void Evaluate_OfficialSignedStable_ReturnsTrusted()
    {
        var candidate = new CandidateDriver
        {
            Title = "Intel Driver",
            Version = "2.0.0",
            SourceType = SourceType.Intel,
            SourceReportsSigned = true,
            IsBeta = false,
            Provider = "Intel",
            SupportedHardwareIds = ["PCI\\VEN_8086&DEV_1234"],
            MatchBasis = HardwareIdMatchBasis.CatalogMetadata
        };

        var result = _sut.Evaluate(candidate, CreateDevice());

        Assert.Equal(TrustLevel.Trusted, result.Level);
        Assert.True(result.Points >= 50);
    }

    [Fact]
    public void Evaluate_UnsignedBeta_ReturnsLowTrust()
    {
        var candidate = new CandidateDriver
        {
            Title = "Community Driver",
            Version = "0.9.0-beta",
            SourceType = SourceType.Community,
            SourceReportsSigned = false,
            IsBeta = true,
            SupportedHardwareIds = ["PCI\\VEN_FFFF"]
        };

        var result = _sut.Evaluate(candidate, CreateDevice());

        Assert.Equal(TrustLevel.Untrusted, result.Level);
    }

    [Fact]
    public void Evaluate_DirectHardwareIdMatch_AddsPoints()
    {
        var candidate = new CandidateDriver
        {
            Title = "Test",
            Version = "1.0",
            SourceType = SourceType.Unknown,
            SupportedHardwareIds = ["PCI\\VEN_8086&DEV_1234"]
        };

        var result = _sut.Evaluate(candidate, CreateDevice());

        Assert.Contains(result.Details, d => d.Contains("Hardware ID match"));
    }
}
