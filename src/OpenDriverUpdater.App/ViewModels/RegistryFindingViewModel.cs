using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;
using Windows.ApplicationModel.DataTransfer;

namespace OpenDriverUpdater.App.ViewModels;

/// <summary>
/// One tile on the Registry Report page. Read-only: its actions open the
/// Windows settings page where the item can be dealt with, or copy text.
/// </summary>
public partial class RegistryFindingViewModel : ObservableObject
{
    public RegistryFinding Finding { get; }

    public RegistryFindingViewModel(RegistryFinding finding)
    {
        Finding = finding;
    }

    [ObservableProperty]
    private string? _copyFeedback;

    public string Name => Finding.Name;

    public string KindText => Finding.Kind switch
    {
        RegistryFindingKind.StartupEntry => Finding.Entry.IsDisabled
            ? "Startup program · already turned off"
            : "Startup program",
        _ => "Installed program · uninstaller missing",
    };

    public string Glyph => Finding.Kind == RegistryFindingKind.StartupEntry ? "" : "";

    public string MissingPath => Finding.MissingPath;
    public string KeyPath => Finding.Entry.KeyPath;
    public string ValueText => $"{Finding.Entry.ValueName} = {Finding.Entry.Command}";

    public string SettingsButtonText => Finding.Kind == RegistryFindingKind.StartupEntry
        ? "Open Startup apps"
        : "Open Installed apps";

    public string Advice => Finding.Kind switch
    {
        RegistryFindingKind.StartupEntry when Finding.Entry.IsDisabled =>
            "Already turned off, so it does nothing at sign-in. It's safe to leave as is.",
        RegistryFindingKind.StartupEntry =>
            "Windows tries to start this at sign-in and fails. Turn it off in Settings > Apps > Startup (or Task Manager > Startup apps). If you reinstall the program, it may start working again.",
        _ =>
            "This program's uninstaller is gone, so uninstalling it from Settings may fail. Try reinstalling the program and then uninstalling it, or use Microsoft's \"Program Install and Uninstall\" troubleshooter.",
    };

    [RelayCommand]
    private void OpenSettings()
    {
        var uri = Finding.Kind == RegistryFindingKind.StartupEntry
            ? "ms-settings:startupapps"
            : "ms-settings:appsfeatures";
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            global::OpenDriverUpdater.App.App.Logger.Error("Couldn't open {Uri}", ex, uri);
        }
    }

    [RelayCommand]
    private void CopyRegistryPath()
    {
        try
        {
            var package = new DataPackage();
            package.SetText(KeyPath);
            Clipboard.SetContent(package);
            CopyFeedback = "Registry path copied";
        }
        catch (Exception ex)
        {
            CopyFeedback = "Couldn't copy";
            global::OpenDriverUpdater.App.App.Logger.Error("Clipboard copy failed", ex);
        }
    }
}
