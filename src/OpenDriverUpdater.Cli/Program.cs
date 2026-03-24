using System.CommandLine;
using OpenDriverUpdater.Cli.Commands;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Services;
using OpenDriverUpdater.Infrastructure.Hardware;
using OpenDriverUpdater.Infrastructure.Logging;
using OpenDriverUpdater.Infrastructure.Networking;
using OpenDriverUpdater.Infrastructure.Sources;
using OpenDriverUpdater.Infrastructure.Storage;

namespace OpenDriverUpdater.Cli;

/// <summary>
/// OpenDriverUpdater CLI giriş noktası.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("OpenDriverUpdater — Open-source Windows driver updater CLI")
        {
            ScanCommand.Create(),
            ListDevicesCommand.Create(),
            CheckUpdatesCommand.Create(),
            DownloadCommand.Create(),
            InstallCommand.Create(),
            ExportJsonCommand.Create()
        };

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Basit DI — servis oluşturucu.
    /// </summary>
    public static ServiceContainer BuildServices()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenDriverUpdater", "logs");

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenDriverUpdater", "data");

        var logger = new SerilogLoggerService(logDir);
        var httpClient = new HttpClient();

        var scanner = new WmiHardwareScanner(logger);
        var versionComparer = new VersionComparerService();
        var trustEvaluator = new TrustEvaluatorService();

        IDriverSource[] sources =
        [
            new MicrosoftCatalogSource(httpClient, logger),
            new IntelDriverSource(logger),
            new AmdDriverSource(logger),
            new NvidiaDriverSource(logger)
        ];

        var resolver = new DriverResolverService(sources, versionComparer, trustEvaluator, logger);
        var metadataStore = new JsonMetadataStore(dataDir, logger);
        var downloadManager = new HttpDownloadManager(httpClient, logger);

        return new ServiceContainer(logger, scanner, resolver, downloadManager, metadataStore);
    }
}

/// <summary>
/// Servis konteyneri — CLI komutlarına DI sağlar.
/// </summary>
public sealed record ServiceContainer(
    ILoggerService Logger,
    IHardwareScanner Scanner,
    IDriverResolver Resolver,
    IDownloadManager DownloadManager,
    IMetadataStore MetadataStore);
