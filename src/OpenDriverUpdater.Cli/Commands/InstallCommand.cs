using System.CommandLine;

namespace OpenDriverUpdater.Cli.Commands;

public static class InstallCommand
{
    public static Command Create()
    {
        var pathArg = new Argument<string>("path", "Kurulacak sürücü dosyasının yolu");

        var command = new Command("install", "Seçilen sürücüyü kurar") { pathArg };

        command.SetHandler(async (string path) =>
        {
            var services = Program.BuildServices();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Driver installation requires administrator privileges.");
            Console.ResetColor();

            var package = new Core.Models.DownloadedPackage
            {
                FilePath = path
            };

            // IInstaller doğrudan CLI'dan kullanılmaz, ama burada kolaylık olsun diye:
            var installer = new Infrastructure.Install.PnpUtilInstaller(services.Logger);
            var result = await installer.InstallAsync(package);

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Installation successful. {(result.RequiresRestart ? "Restart required." : "")}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Installation failed: {result.Message}");
            }
            Console.ResetColor();
        }, pathArg);

        return command;
    }
}
