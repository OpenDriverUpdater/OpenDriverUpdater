using Xunit;

namespace OpenDriverUpdater.Core.Tests;

public class CatalogHardwareIdResearchTests
{
    [Fact]
    public void MicrosoftCatalogSearchResult_DoesNotContainHardwareIds()
    {
        var html = System.IO.File.ReadAllText("tests/fixtures/catalog_sample.html");
        // The sample contains title, provider, version, but no hardware IDs.
        // Real Catalog search results do not expose SupportedHardwareIds in the HTML.
        // Therefore current parser cannot populate SupportedHardwareIds from catalog metadata.
        Assert.DoesNotContain("PCI\\VEN", html);
    }
}
