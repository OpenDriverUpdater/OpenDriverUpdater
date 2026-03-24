using System.CommandLine;

namespace OpenDriverUpdater.Cli.Commands;

public static class DownloadCommand
{
    public static Command Create()
    {
        var deviceIdArg = new Argument<string>("device-id", "İndirilecek sürücünün cihaz ID'si");
        var outputOption = new Option<string>("--output", () => "./downloads", "İndirme dizini");

        var command = new Command("download", "Seçilen sürücüyü indirir") { deviceIdArg, outputOption };

        command.SetHandler(async (string deviceId, string output) =>
        {
            var services = Program.BuildServices();
            var devices = await services.Scanner.ScanAsync();
            var recommendations = await services.Resolver.ResolveAsync(devices);

            var match = recommendations.FirstOrDefault(r =>
                r.DeviceId.Contains(deviceId, StringComparison.OrdinalIgnoreCase));

            if (match?.Candidate is null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"No update found for device: {deviceId}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Downloading {match.Candidate.Title}...");

            var progress = new Progress<double>(p =>
            {
                Console.Write($"\r  Progress: {p:P0}    ");
            });

            var package = await services.DownloadManager.DownloadAsync(match.Candidate, output, progress);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Downloaded to: {package.FilePath} (SHA256: {package.Hash})");
            Console.ResetColor();
        }, deviceIdArg, outputOption);

        return command;
    }
}
