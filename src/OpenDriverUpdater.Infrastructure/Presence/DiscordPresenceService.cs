using System;
using OpenDriverUpdater.Core.Interfaces;
using DiscordRPC;

namespace OpenDriverUpdater.Infrastructure.Presence;

/// <summary>
/// Discord Rich Presence (RPC) uygulaması.
///
/// Windows üzerinde Discord istemcisiyle named pipe üzerinden iletişim kurar.
/// Discord istemcisi yoksa veya uygulama kimliği yapılandırılmamışsa fail-safe olarak
/// hiçbir işlem yapmaz. Gizlilik politikası gereği varsayılan olarak kapalıdır;
/// yalnızca ODU_DISCORD_APP_ID ortam değişkeni ayarlandığında etkinleştirilir.
///
/// Gönderilen veri yalnızca "ne üzerinde çalışılıyor" türünde varlık durumu (presence)
/// içerdir; donanım kimliği, sürüm numarası veya kişisel veri gönderilmez.
/// </summary>
public sealed class DiscordPresenceService : IDiscordPresence, IDisposable
{
    private readonly DiscordRpcClient _client;
    private readonly string? _applicationId;
    private bool _enabled;
    private bool _disposed;

    public DiscordPresenceService(string? applicationId, bool enabled)
    {
        _applicationId = applicationId;
        _client = new DiscordRpcClient(applicationId ?? string.Empty);
        _enabled = false;

        if (enabled && OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(applicationId))
        {
            try
            {
                _client.Initialize();
                _enabled = true;
            }
            catch
            {
                // Discord istemcisi yoksa veya başlatılamazsa sessizce devre dışı bırak.
                _enabled = false;
            }
        }
    }

    public bool IsEnabled
    {
        get => _enabled && !_disposed;
        set
        {
            if (_disposed) return;

            if (value && !string.IsNullOrWhiteSpace(_applicationId) && OperatingSystem.IsWindows())
            {
                try
                {
                    _client.Initialize();
                    _enabled = true;
                }
                catch
                {
                    _enabled = false;
                }
            }
            else
            {
                _enabled = false;
            }
        }
    }

    public void SetPresence(string details, string state)
    {
        if (!IsEnabled) return;

        try
        {
            _client.SetPresence(new RichPresence
            {
                Details = details,
                State = state,
            });
        }
        catch
        {
            // Discord ile iletişim hatası — sessizce yoksay.
        }
    }

    public void ClearPresence()
    {
        if (!IsEnabled) return;

        try
        {
            _client.ClearPresence();
        }
        catch
        {
            // Sessizce yoksay.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _enabled = false;

        try
        {
            _client.ClearPresence();
            _client.Dispose();
        }
        catch
        {
            // Sessizce yoksay.
        }
    }
}
