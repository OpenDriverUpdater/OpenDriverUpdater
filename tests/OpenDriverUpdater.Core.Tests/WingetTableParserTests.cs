using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Core.Software;
using Xunit;

namespace OpenDriverUpdater.Core.Tests;

public class WingetTableParserTests
{
    private const string ListOutput =
        "   - \r   \\ \r" +
        "Name                                   Id                               Version        Available     Source\n" +
        "------------------------------------------------------------------------------------------------------------\n" +
        "Git                                    Git.Git                          2.44.0         2.47.1        winget\n" +
        "Notepad++ (64-bit x64)                 Notepad++.Notepad++              8.6.2                        winget\n" +
        "Microsoft Visual C++ 2015-2022 Redist… Microsoft.VCRedist.2015+.x64     14.38.33135.0  14.42.34433.0 winget\n" +
        "Some Legacy Tool                       ARP\\Machine\\X64\\{1234-ABCD}      1.0\n" +
        "Really Long Package Name               Contoso.SuperLongPackageIdenti…  3.1            3.2           winget\n" +
        "\n";

    [Fact]
    public void Parse_ReadsAllRows()
    {
        var apps = WingetTableParser.Parse(ListOutput);
        Assert.Equal(5, apps.Count);
    }

    [Fact]
    public void Parse_ReadsColumns()
    {
        var git = WingetTableParser.Parse(ListOutput).Single(a => a.Id == "Git.Git");
        Assert.Equal("Git", git.Name);
        Assert.Equal("2.44.0", git.InstalledVersion);
        Assert.Equal("2.47.1", git.AvailableVersion);
        Assert.Equal("winget", git.Source);
        Assert.True(git.HasUpdate);
        Assert.True(git.IsTracked);
    }

    [Fact]
    public void Parse_UpToDateTrackedApp()
    {
        var npp = WingetTableParser.Parse(ListOutput).Single(a => a.Id == "Notepad++.Notepad++");
        Assert.Equal("Notepad++ (64-bit x64)", npp.Name);
        Assert.False(npp.HasUpdate);
        Assert.True(npp.IsTracked);
    }

    [Fact]
    public void Parse_UntrackedApp_HasNoSource()
    {
        var legacy = WingetTableParser.Parse(ListOutput).Single(a => a.Name == "Some Legacy Tool");
        Assert.Equal(@"ARP\Machine\X64\{1234-ABCD}", legacy.Id);
        Assert.False(legacy.IsTracked);
        Assert.False(legacy.HasUpdate);
    }

    [Fact]
    public void Parse_DetectsTruncatedId()
    {
        var app = WingetTableParser.Parse(ListOutput).Single(a => a.Name == "Really Long Package Name");
        Assert.True(app.IsIdTruncated);
        Assert.Equal("Contoso.SuperLongPackageIdenti", WingetTextUtil.StripEllipsis(app.Id));
    }

    [Fact]
    public void Parse_FourColumns_WhenNoUpdatesAvailable()
    {
        const string output =
            "Name      Id           Version Source\n" +
            "-------------------------------------\n" +
            "7-Zip     7zip.7zip    24.08   winget\n";
        var app = Assert.Single(WingetTableParser.Parse(output));
        Assert.Equal("24.08", app.InstalledVersion);
        Assert.Equal("winget", app.Source);
        Assert.Null(app.AvailableVersion);
    }

    [Fact]
    public void Parse_WideCharacters_KeepColumnsAligned()
    {
        // "微信" is 2 characters but 4 display cells; winget pads by display cells.
        const string output =
            "Name     Id             Version Available Source\n" +
            "------------------------------------------------\n" +
            "微信     Tencent.WeChat 3.9     3.9.12    winget\n";
        var app = Assert.Single(WingetTableParser.Parse(output));
        Assert.Equal("微信", app.Name);
        Assert.Equal("Tencent.WeChat", app.Id);
        Assert.Equal("3.9.12", app.AvailableVersion);
    }

    [Fact]
    public void Parse_UpgradeOutput_IgnoresFooterAndReadsSecondTable()
    {
        const string output =
            "Name  Id       Version Available Source\n" +
            "---------------------------------------\n" +
            "Git   Git.Git  2.44.0  2.47.1    winget\n" +
            "1 upgrades available.\n" +
            "\n" +
            "The following packages have an upgrade available, but require explicit targeting for upgrade:\n" +
            "Name  Id       Version Available Source\n" +
            "---------------------------------------\n" +
            "Zoom  Zoom.Zoom 5.0    6.0       winget\n";
        var apps = WingetTableParser.Parse(output);
        Assert.Equal(new[] { "Git.Git", "Zoom.Zoom" }, apps.Select(a => a.Id).ToArray());
    }

    [Fact]
    public void Parse_NoTable_ReturnsEmpty()
    {
        Assert.Empty(WingetTableParser.Parse("No installed package found matching input criteria."));
        Assert.Empty(WingetTableParser.Parse(string.Empty));
    }
}
