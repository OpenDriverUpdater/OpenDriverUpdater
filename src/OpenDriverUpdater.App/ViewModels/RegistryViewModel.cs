using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.App.ViewModels;

/// <summary>
/// Read-only registry report: startup programs and installed-program
/// entries that point to files that no longer exist. Nothing is changed.
/// </summary>
public partial class RegistryViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<RegistryFindingViewModel> Findings { get; } = [];

    /// <summary>App-lifetime instance so the report survives navigation.</summary>
    public static RegistryViewModel Shared => _shared ??= new RegistryViewModel();
    private static RegistryViewModel? _shared;

    private RegistryViewModel()
    {
        _ = ScanAsync();
    }

    private bool CanScan() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Reading startup and installed-program entries...";
            Findings.Clear();

            var result = await App.RegistryScanner.ScanAsync();

            var ordered = result.Findings
                .OrderBy(f => f.Kind)                                  // startup first
                .ThenBy(f => f.Entry.IsDisabled)                       // active problems first
                .ThenBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase);
            foreach (var f in ordered)
                Findings.Add(new RegistryFindingViewModel(f));

            var startup = result.Findings.Count(f => f.Kind == RegistryFindingKind.StartupEntry);
            var programs = result.Findings.Count - startup;
            var checkedText = $"Checked {result.StartupEntriesChecked} startup entries and {result.ProgramsChecked} installed programs.";
            StatusMessage = result.Findings.Count == 0
                ? $"{checkedText} Nothing points to a missing file."
                : $"{checkedText} {startup} startup entr{(startup == 1 ? "y" : "ies")} and {programs} program{(programs == 1 ? "" : "s")} point to missing files.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Registry scan failed: {ex.Message}";
            App.Logger.Error("Registry scan failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
