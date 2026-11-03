using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Services;
using OpenDriverUpdater.Infrastructure.Hardware;
using OpenDriverUpdater.Infrastructure.Logging;
using OpenDriverUpdater.Infrastructure.Networking;
using OpenDriverUpdater.Infrastructure.Sources;
using OpenDriverUpdater.Infrastructure.Storage;
using OpenDriverUpdater.Infrastructure.Install;
using System;
using System.IO;
using System.Net.Http;

namespace OpenDriverUpdater.App
{
    public class App : Application
    {
        private Window? _window;

        public static ILoggerService Logger { get; private set; } = null!;
        public static IHardwareScanner Scanner { get; private set; } = null!;
        public static IDriverResolver Resolver { get; private set; } = null!;
        public static IDownloadManager DownloadManager { get; private set; } = null!;
        public static IInstaller Installer { get; private set; } = null!;
        public static IMetadataStore MetadataStore { get; private set; } = null!;

        public App()
        {
            Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            InitializeServices();
            _window = new MainWindow();
            _window.Activate();
        }

        private static void InitializeServices()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(appData, "OpenDriverUpdater", "logs");
            var dataDir = Path.Combine(appData, "OpenDriverUpdater", "data");

            Logger = new SerilogLoggerService(logDir);

            var httpClient = new HttpClient();
            Scanner = new WmiHardwareScanner(Logger);

            IDriverSource[] sources =
            [
                new MicrosoftCatalogSource(httpClient, Logger),
                new IntelDriverSource(Logger),
                new AmdDriverSource(Logger),
                new NvidiaDriverSource(Logger)
            ];

            var versionComparer = new VersionComparerService();
            var trustEvaluator = new TrustEvaluatorService();

            Resolver = new DriverResolverService(sources, versionComparer, trustEvaluator, Logger);
            MetadataStore = new JsonMetadataStore(dataDir, Logger);
            DownloadManager = new HttpDownloadManager(httpClient, Logger);
            Installer = new PnpUtilInstaller(Logger);
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            global::Microsoft.UI.Xaml.Application.Start((p) => 
            {
                var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
    }
}
