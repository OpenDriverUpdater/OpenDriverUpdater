using System.CommandLine;

namespace OpenDriverUpdater.Cli.Commands;

public static class InstallCommand
{
    public static Command Create()
    {
        var pathArg = new Argument<string>("path", "Kurulacak sürücü dosyasının yolu");
        var experimentalOpt = new Option<bool>("--experimental", "Enable experimental install with missing safety checks");

        var command = new Command("install", "Seçilen sürücüyü kurar") { pathArg, experimentalOpt };

        command.SetHandler(async (string path, bool experimental) =>
        {
            if (!experimental)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Install pipeline is disabled. Missing safety checks:");
                Console.WriteLine("- SHA256 verification");
                Console.WriteLine("- Authenticode validation");
                Console.WriteLine("- System restore point creation");
                Console.WriteLine("Run with --experimental to bypass this check.");
                Console.ResetColor();
                return;
            }

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
        }, pathArg, experimentalOpt);

        return command;
    }
}
