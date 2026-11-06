using Xunit;

namespace OpenDriverUpdater.Core.Tests;

public class CatalogHardwareIdResearchTests
{
    [Fact]
    public void MicrosoftCatalogSearchResult_DoesNotContainHardwareIds()
    {
        var fixturePath = "D:\\Games\\Projeler\\OpenDriverUpdater\\tests\\fixtures\\catalog_sample.html";
        var html = System.IO.File.ReadAllText(fixturePath);
        Assert.DoesNotContain("PCI\\VEN", html);
    }
}
