using System.IO;
using Xunit;

namespace OpenDriverUpdater.Core.Tests;

/// <summary>
/// Research fixture test for Microsoft Update Catalog hardware-ID matching.
/// The sample page is located relative to the repository root so the test
/// also runs on non-Windows CI runners (no hardcoded D:\ path).
/// </summary>
public class CatalogHardwareIdResearchTests
{
    [Fact]
    public void MicrosoftCatalogSearchResult_DoesNotContainHardwareIds()
    {
        var fixturePath = FindFixture("catalog_sample.html");
        Assert.True(File.Exists(fixturePath), $"Catalog research fixture not found: {fixturePath}");

        var html = File.ReadAllText(fixturePath);
        Assert.DoesNotContain("PCI\\VEN", html);
    }

    private static string FindFixture(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "OpenDriverUpdater.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new FileNotFoundException(
                "Repository root (OpenDriverUpdater.sln) could not be located from the test assembly.",
                "OpenDriverUpdater.sln");
        }

        return Path.Combine(directory.FullName, "tests", "fixtures", fileName);
    }
}
