using Xunit;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Core.Services;

namespace OpenDriverUpdater.Core.Tests;

public class TrustEvaluatorHonestyTests
{
    private readonly TrustEvaluatorService _sut = new();

    private static DeviceInfo Device(string manufacturer, params string[] hwid) => new()
    {
        Manufacturer = manufacturer,
        HardwareIds = hwid
    };

    private static CandidateDriver Candidate(SourceType src, string provider, bool isBeta, HardwareIdMatchBasis basis, params string[] hwids) => new()
    {
        SourceType = src,
        Provider = provider,
        IsBeta = isBeta,
        SupportedHardwareIds = hwids,
        SourceReportsSigned = true,
        MatchBasis = basis
    };

    [Fact]
    public void QueryOnly_Official_Stable => Unknown()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, HardwareIdMatchBasis.QueryOnly, "PCI\\VEN_10DE&DEV_1F02");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Unknown, score.Level);
    }

    [Fact]
    public void CatalogMetadata_Official_Stable => Trusted()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, HardwareIdMatchBasis.CatalogMetadata, "PCI\\VEN_10DE&DEV_1F02");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Trusted, score.Level);
    }

    [Fact]
    public void InfFile_WithVerification => Verified()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, HardwareIdMatchBasis.InfFile, "PCI\\VEN_10DE&DEV_1F02");
        var verification = new VerificationResult(true, true);
        var score = _sut.Evaluate(cand, dev, verification);
        Assert.Equal(TrustLevel.Verified, score.Level);
    }

    [Fact]
    public void InfFile_PartialVerification_NotVerified()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, HardwareIdMatchBasis.InfFile, "PCI\\VEN_10DE&DEV_1F02");
        var verification = new VerificationResult(true, false);
        var score = _sut.Evaluate(cand, dev, verification);
        Assert.NotEqual(TrustLevel.Verified, score.Level);
    }

    [Fact]
    public void NoHwIdMatch_Untrusted()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, HardwareIdMatchBasis.QueryOnly, "PCI\\VEN_10DE&DEV_1F03");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Untrusted, score.Level);
    }

    [Fact]
    public void Gate_NullIds_Untrusted()
    {
        var dev = Device("Intel", null!);
        var cand = Candidate(SourceType.MicrosoftCatalog, "intel", false, HardwareIdMatchBasis.QueryOnly);
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Untrusted, score.Level);
    }
}
