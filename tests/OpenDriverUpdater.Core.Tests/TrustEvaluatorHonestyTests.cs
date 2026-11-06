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

    private static CandidateDriver Candidate(SourceType src, string provider, bool isBeta, params string[] hwids) => new()
    {
        SourceType = src,
        Provider = provider,
        IsBeta = isBeta,
        SupportedHardwareIds = hwids,
        SourceReportsSigned = true
    };

    [Fact]
    public void MicrosoftCatalog_Stable_ExactHwId_NoVerification_Trusted()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, "PCI\\VEN_10DE&DEV_1F02");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Trusted, score.Level);
    }

    [Fact]
    public void Same_WithBeta_LowersLevel()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", true, "PCI\\VEN_10DE&DEV_1F02");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Unknown, score.Level);
    }

    [Fact]
    public void NoHwIdMatch_EverythingElseMax_Untrusted()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, "PCI\\VEN_10DE&DEV_1F03");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Untrusted, score.Level);
        Assert.Equal(0, score.Points);
    }

    [Fact]
    public void HwIdMatch_WithHashAndSig_Verified()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, "PCI\\VEN_10DE&DEV_1F02");
        var verification = new VerificationResult(true, true);
        var score = _sut.Evaluate(cand, dev, verification);
        Assert.Equal(TrustLevel.Verified, score.Level);
    }

    [Fact]
    public void HashTrue_SigFalse_NotVerified()
    {
        var dev = Device("NVIDIA", "PCI\\VEN_10DE&DEV_1F02");
        var cand = Candidate(SourceType.MicrosoftCatalog, "nvidia", false, "PCI\\VEN_10DE&DEV_1F02");
        var verification = new VerificationResult(true, false);
        var score = _sut.Evaluate(cand, dev, verification);
        Assert.NotEqual(TrustLevel.Verified, score.Level);
    }

    [Fact]
    public void SourceReportsSigned_AloneAddsZeroPoints()
    {
        var dev = Device("INTEL", "PCI\\VEN_8086&DEV_1234");
        var cand = Candidate(SourceType.MicrosoftCatalog, "intel", false, "PCI\\VEN_8086&DEV_1234");
        cand.SourceReportsSigned = true;
        var score = _sut.Evaluate(cand, dev);
        Assert.Contains(score.Details, d => d.Contains("Source reports signed claim - 0 points"));
    }

    [Fact]
    public void HardwareIdComparison_IsCaseInsensitive()
    {
        var dev = Device("Intel", "pci\\ven_8086&dev_1234");
        var cand = Candidate(SourceType.MicrosoftCatalog, "intel", false, "PCI\\VEN_8086&DEV_1234");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Trusted, score.Level);
    }

    [Fact]
    public void Gate_NullIds_Untrusted()
    {
        var dev = Device("Intel", null!);
        var cand = Candidate(SourceType.MicrosoftCatalog, "intel", false);
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Untrusted, score.Level);
    }

    [Fact]
    public void Gate_EmptyIds_Untrusted()
    {
        var dev = Device("Intel", "");
        var cand = Candidate(SourceType.MicrosoftCatalog, "intel", false, "");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Untrusted, score.Level);
    }

    [Fact]
    public void Gate_WhitespaceIds_Untrusted()
    {
        var dev = Device("Intel", "   ");
        var cand = Candidate(SourceType.MicrosoftCatalog, "intel", false, "   ");
        var score = _sut.Evaluate(cand, dev);
        Assert.Equal(TrustLevel.Untrusted, score.Level);
    }
}
