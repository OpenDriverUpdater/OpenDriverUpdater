using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Cli.Output;

/// <summary>
/// Terminal tablo ve renkli çıktı renderer'ı.
/// </summary>
public static class ConsoleRenderer
{
    private const int ColDevice = 35;
    private const int ColClass = 12;
    private const int ColManufacturer = 20;
    private const int ColVersion = 18;
    private const int ColProvider = 20;

    public static void RenderDeviceTable(IReadOnlyList<DeviceInfo> devices)
    {
        if (devices.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No devices found.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"{"Device",-ColDevice} {"Class",-ColClass} {"Manufacturer",-ColManufacturer} {"Version",-ColVersion} {"Provider",-ColProvider}");
        Console.WriteLine(new string('─', ColDevice + ColClass + ColManufacturer + ColVersion + ColProvider));
        Console.ResetColor();

        foreach (var d in devices)
        {
            Console.Write($"{Truncate(d.Name, ColDevice),-ColDevice} ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"{d.Class,-ColClass} ");
            Console.ResetColor();
            Console.Write($"{Truncate(d.Manufacturer ?? "-", ColManufacturer),-ColManufacturer} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{Truncate(d.InstalledDriverVersion ?? "N/A", ColVersion),-ColVersion} ");
            Console.ResetColor();
            Console.WriteLine($"{Truncate(d.InstalledProvider ?? "-", ColProvider),-ColProvider}");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Total: {devices.Count} devices");
        Console.ResetColor();
    }

    public static void RenderRecommendations(IReadOnlyList<UpdateRecommendation> recommendations)
    {
        if (recommendations.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ All drivers are up to date.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"{"Device ID",-40} {"Installed",-15} {"Candidate",-15} {"Type",-14} {"Trust",-12} Reason");
        Console.WriteLine(new string('─', 110));
        Console.ResetColor();

        foreach (var r in recommendations)
        {
            var color = r.RecommendationType switch
            {
                Core.Enums.RecommendationType.Critical => ConsoleColor.Red,
                Core.Enums.RecommendationType.Recommended => ConsoleColor.Green,
                _ => ConsoleColor.White,
            };

            Console.Write($"{Truncate(r.DeviceId, 40),-40} ");
            Console.Write($"{Truncate(r.InstalledVersion ?? "N/A", 15),-15} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{Truncate(r.CandidateVersion, 15),-15} ");
            Console.ForegroundColor = color;
            Console.Write($"{r.RecommendationType,-14} ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"{r.TrustScore?.Level.ToString() ?? "?",-12} ");
            Console.ResetColor();
            Console.WriteLine(r.Reason ?? "");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Total: {recommendations.Count} updates available");
        Console.ResetColor();
    }

    private static string Truncate(string value, int maxLen) =>
        value.Length <= maxLen ? value : string.Concat(value.AsSpan(0, maxLen - 1), "…");
}
