using System.CommandLine;

namespace OpenDriverUpdater.Cli.Commands;

public static class CheckUpdatesCommand
{
    public static Command Create()
    {
        var command = new Command("check-updates", "Güncelleme kontrolü yapar");

        command.SetHandler(async () =>
        {
            var services = Program.BuildServices();
            services.Logger.Info("Checking for driver updates...");

            var devices = await services.Scanner.ScanAsync();
            var recommendations = await services.Resolver.ResolveAsync(devices);

            Output.ConsoleRenderer.RenderRecommendations(recommendations);
        });

        return command;
    }
}
