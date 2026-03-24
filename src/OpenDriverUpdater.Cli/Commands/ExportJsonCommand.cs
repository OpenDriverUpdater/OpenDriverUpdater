using System.CommandLine;
using System.Text.Json;

namespace OpenDriverUpdater.Cli.Commands;

public static class ExportJsonCommand
{
    public static Command Create()
    {
        var outputOption = new Option<string>("--output", () => "scan-result.json", "Çıkış dosyası yolu");

        var command = new Command("export-json", "Tarama sonucunu JSON olarak dışa aktarır") { outputOption };

        command.SetHandler(async (string output) =>
        {
            var services = Program.BuildServices();
            var devices = await services.Scanner.ScanAsync();

            var dto = new Core.Dtos.ScanResultDto
            {
                Devices = devices,
                ScannedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(output, json);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Exported {devices.Count} devices to: {output}");
            Console.ResetColor();
        }, outputOption);

        return command;
    }
}
