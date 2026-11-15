using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.App.ViewModels;

/// <summary>
/// One tile on the Software page: an installed app, its update status and its Update button.
/// </summary>
public partial class SoftwareItemViewModel : ObservableObject
{
    public InstalledApp App { get; }

    public SoftwareItemViewModel(InstalledApp app)
    {
        App = app;
    }

    /// <summary>True once "Check for Updates" has run.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdateStatus), nameof(NeedsUpdate), nameof(CanUpdate), nameof(UpdateStatusText), nameof(UpdateStatusBrush), nameof(UpdateStatusTooltip))]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    private bool _isChecked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUpdate))]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsUpdate), nameof(CanUpdate), nameof(UpdateStatusText), nameof(UpdateStatusBrush), nameof(UpdateStatusTooltip))]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string _status = string.Empty;

    /// <summary>Raised when an update on this tile starts or finishes.</summary>
    public event EventHandler? BusyChanged;

    public bool HasUpdateStatus => IsChecked;
    public bool HasStatus => !string.IsNullOrEmpty(Status);

    /// <summary>Checked, a newer version exists, and it hasn't been installed yet.</summary>
    public bool NeedsUpdate => IsChecked && App.HasUpdate && !IsCompleted;

    public bool CanUpdate => NeedsUpdate && App.IsTracked && !IsBusy;

    public string SourceText => App.IsTracked ? App.Source! : "—";

    public string UpdateStatusText => !IsChecked
        ? string.Empty
        : IsCompleted ? "Up to date"
        : App.HasUpdate ? "Needs updating"
        : App.IsTracked ? "Up to date"
        : "Not tracked";

    public string UpdateStatusBrush => UpdateStatusText switch
    {
        "Needs updating" => "WarningBrush",
        "Up to date" => "SuccessBrush",
        _ => "TextBrush",
    };

    public string UpdateStatusTooltip => UpdateStatusText == "Not tracked"
        ? "winget can't match this app to a package source, so it can't tell whether a newer version exists."
        : UpdateStatusText;

    partial void OnIsBusyChanged(bool value) => BusyChanged?.Invoke(this, EventArgs.Empty);

    private bool CanRunUpdate() => CanUpdate;

    [RelayCommand(CanExecute = nameof(CanRunUpdate))]
    private async Task UpdateAsync()
    {
        try
        {
            IsBusy = true;
            HasError = false;
            Status = $"Updating to {App.AvailableVersion}... (installers may open or close the app)";

            var result = await global::OpenDriverUpdater.App.App.SoftwareUpdates.UpdateAsync(App);

            if (result.Success)
            {
                Status = result.Message;
                IsCompleted = true;
            }
            else
            {
                Status = $"Failed: {result.Message}";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            Status = $"Failed: {ex.Message}";
            HasError = true;
            global::OpenDriverUpdater.App.App.Logger.Error("Software update failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
