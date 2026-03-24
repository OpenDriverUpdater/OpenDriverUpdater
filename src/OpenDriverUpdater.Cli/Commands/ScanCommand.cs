using System.CommandLine;

namespace OpenDriverUpdater.Cli.Commands;

public static class ScanCommand
{
    public static Command Create()
    {
        var command = new Command("scan", "Sistemdeki cihazları tarar ve listeler");

        command.SetHandler(async () =>
        {
            var services = Program.BuildServices();
            services.Logger.Info("Starting device scan...");

            var devices = await services.Scanner.ScanAsync();

            Output.ConsoleRenderer.RenderDeviceTable(devices);
            services.Logger.Info("Scan completed: {Count} devices found", devices.Count);
        });

        return command;
    }
}
