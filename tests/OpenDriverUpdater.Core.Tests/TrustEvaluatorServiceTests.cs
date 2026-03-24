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
    public void Evaluate_OfficialSignedStable_ReturnsVerified()
    {
        var candidate = new CandidateDriver
        {
            Title = "Intel Driver",
            Version = "2.0.0",
            SourceType = SourceType.Intel,
            IsSigned = true,
            IsBeta = false,
            Provider = "Intel",
            SupportedHardwareIds = ["PCI\\VEN_8086&DEV_1234"]
        };

        var result = _sut.Evaluate(candidate, CreateDevice());

        Assert.Equal(TrustLevel.Verified, result.Level);
        Assert.True(result.Points >= 80);
    }

    [Fact]
    public void Evaluate_UnsignedBeta_ReturnsLowTrust()
    {
        var candidate = new CandidateDriver
        {
            Title = "Community Driver",
            Version = "0.9.0-beta",
            SourceType = SourceType.Community,
            IsSigned = false,
            IsBeta = true,
            SupportedHardwareIds = ["PCI\\VEN_FFFF"]
        };

        var result = _sut.Evaluate(candidate, CreateDevice());

        Assert.True(result.Points < 50);
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

        Assert.Contains(result.Details, d => d.Contains("Direct Hardware ID match"));
    }
}
