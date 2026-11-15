using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenDriverUpdater.App.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly string _logDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenDriverUpdater", "logs");

    public ObservableCollection<string> LogEntries { get; } = [];

    public LogsViewModel()
    {
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        LogEntries.Clear();

        try
        {
            if (!Directory.Exists(_logDir)) return;

            var latestLog = Directory.GetFiles(_logDir, "odu-*.log")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (latestLog is null) return;

            // Serilog keeps today's log open for writing, so open it with
            // FileShare.ReadWrite (File.ReadAllLines would demand exclusive-write
            // sharing and fail with "being used by another process").
            var lines = new List<string>();
            await using (var stream = new FileStream(latestLog, FileMode.Open, FileAccess.Read,
                             FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) is not null)
                    lines.Add(line);
            }

            foreach (var line in lines.TakeLast(500))
                LogEntries.Add(line);
        }
        catch (Exception ex)
        {
            LogEntries.Add($"Error loading logs: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(_logDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = _logDir,
                UseShellExecute = true
            });
        }
        catch { /* silent */ }
    }
}
