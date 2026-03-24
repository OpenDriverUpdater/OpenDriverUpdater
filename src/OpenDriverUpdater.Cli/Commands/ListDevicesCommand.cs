using System.CommandLine;

namespace OpenDriverUpdater.Cli.Commands;

public static class ListDevicesCommand
{
    public static Command Create()
    {
        var filterOption = new Option<string?>("--filter", "Cihaz adı veya sınıfı ile filtrele");
        var command = new Command("list-devices", "Tespit edilen cihazları listeler") { filterOption };

        command.SetHandler(async (string? filter) =>
        {
            var services = Program.BuildServices();
            var devices = await services.Scanner.ScanAsync();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                devices = devices
                    .Where(d => d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                             || d.Class.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            Output.ConsoleRenderer.RenderDeviceTable(devices);
        }, filterOption);

        return command;
    }
}
