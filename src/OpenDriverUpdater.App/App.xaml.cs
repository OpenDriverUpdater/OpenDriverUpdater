using Microsoft.UI.Xaml;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Services;
using OpenDriverUpdater.Infrastructure.Hardware;
using OpenDriverUpdater.Infrastructure.Logging;
using OpenDriverUpdater.Infrastructure.Networking;
using OpenDriverUpdater.Infrastructure.Presence;
using OpenDriverUpdater.Infrastructure.Sources;
using OpenDriverUpdater.Infrastructure.Storage;

namespace OpenDriverUpdater.App;

public partial class App : Application
{
    private Window? _window;

    // ── Services (simple DI — Phase 2'de Microsoft.Extensions.DependencyInjection geçilecek) ──
    public static ILoggerService Logger { get; private set; } = null!;
    public static IHardwareScanner Scanner { get; private set; } = null!;
    public static IDriverResolver Resolver { get; private set; } = null!;
    public static IDownloadManager DownloadManager { get; private set; } = null!;
    public static IInstaller Installer { get; private set; } = null!;
    public static IMetadataStore MetadataStore { get; private set; } = null!;
    public static IReadOnlyList<IDriverSource> DriverSources { get; private set; } = null!;
    public static IDiscordPresence DiscordPresence { get; private set; } = null!;

    /// <summary>
    /// Discord Developer Portal Application ID / Client ID.
    /// Bu uygulama kimliği Discord Rich Presence'ı etkinleştirmek için gereklidir.
    /// Gizli değildir; Discord bot/istemci kimliğidir.
    /// </summary>
    private const string DiscordApplicationId = "1568505926086955078";

    public App()
    {
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        InitializeServices();

        _window = new MainWindow();
        _window.Closed += OnMainWindowClosed;
        _window.Activate();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        // Uygulama kapanınca Discord durumunu temizle ve bağlantıyı kapat.
        if (DiscordPresence is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static void InitializeServices()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(appData, "OpenDriverUpdater", "logs");
        var dataDir = Path.Combine(appData, "OpenDriverUpdater", "data");

        Logger = new SerilogLoggerService(logDir);

        var httpClient = new HttpClient();

        var downloadClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
        })
        {
            Timeout = TimeSpan.FromMinutes(10),
        };

        Scanner = new WmiHardwareScanner(Logger);

        IDriverSource[] sources =
        [
            new MicrosoftCatalogSource(httpClient, Logger),
            new IntelDriverSource(Logger),
            new AmdDriverSource(Logger),
            new NvidiaDriverSource(Logger)
        ];

        DriverSources = sources;

        var versionComparer = new VersionComparerService();
        var trustEvaluator = new TrustEvaluatorService();

        Resolver = new DriverResolverService(sources, versionComparer, trustEvaluator, Logger);
        var dbPath = Path.Combine(dataDir, "metadata.db");
        MetadataStore = new OpenDriverUpdater.Infrastructure.Storage.SqliteMetadataStore(dbPath, Logger);
        DownloadManager = new HttpDownloadManager(downloadClient, Logger);
        Installer = new OpenDriverUpdater.Infrastructure.Install.PnpUtilInstaller(Logger);

        // Discord Rich Presence — varsayılan olarak etkin (otomatik başlar, kapanınca kapanır).
        // İsteğe bağlı override: ODU_DISCORD_APP_ID ortam değişkeni.
        var discordAppId = Environment.GetEnvironmentVariable("ODU_DISCORD_APP_ID")
            ?? DiscordApplicationId;
        DiscordPresence = new DiscordPresenceService(discordAppId, enabled: true);
    }
}
