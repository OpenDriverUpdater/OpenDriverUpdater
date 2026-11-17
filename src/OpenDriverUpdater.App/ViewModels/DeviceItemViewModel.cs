using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.App.ViewModels;

/// <summary>
/// One tile on the Devices page: the scanned device plus its update state.
/// </summary>
public partial class DeviceItemViewModel : ObservableObject
{
    public DeviceInfo Device { get; }

    public DeviceItemViewModel(DeviceInfo device)
    {
        Device = device;
    }

    /// <summary>True once "Check for Updates" has run for this device.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateStatusText))]
    [NotifyPropertyChangedFor(nameof(UpdateStatusBrush))]
    [NotifyPropertyChangedFor(nameof(HasUpdateStatus))]
    private bool _isChecked;

    /// <summary>The pending update for this device, or null if it is up to date.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsUpdate))]
    [NotifyPropertyChangedFor(nameof(UpdateStatusText))]
    [NotifyPropertyChangedFor(nameof(UpdateStatusBrush))]
    private RecommendationViewModel? _update;

    public bool HasUpdateStatus => IsChecked;

    /// <summary>Raised when an install on this tile starts or finishes.</summary>
    public event EventHandler? BusyChanged;

    /// <summary>An update was found and has not been installed yet.</summary>
    public bool NeedsUpdate => Update is { IsCompleted: false };

    public string UpdateStatusText => !IsChecked
        ? string.Empty
        : Update switch
        {
            null => "Up to date",
            { IsCompleted: true, Status: "Restart Required" } => "Restart required",
            { IsCompleted: true } => "Up to date",
            _ => "Needs updating",
        };

    public string UpdateStatusBrush => UpdateStatusText switch
    {
        "Needs updating" => "WarningBrush",
        "Restart required" => "WarningBrush",
        _ => "SuccessBrush",
    };

    partial void OnUpdateChanging(RecommendationViewModel? value)
    {
        if (Update is not null)
            Update.PropertyChanged -= OnUpdatePropertyChanged;
    }

    partial void OnUpdateChanged(RecommendationViewModel? value)
    {
        if (value is not null)
            value.PropertyChanged += OnUpdatePropertyChanged;
    }

    private void OnUpdatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RecommendationViewModel.IsBusy))
            BusyChanged?.Invoke(this, EventArgs.Empty);

        if (e.PropertyName is nameof(RecommendationViewModel.IsCompleted) or nameof(RecommendationViewModel.Status))
        {
            OnPropertyChanged(nameof(NeedsUpdate));
            OnPropertyChanged(nameof(UpdateStatusText));
            OnPropertyChanged(nameof(UpdateStatusBrush));
        }
    }
}
