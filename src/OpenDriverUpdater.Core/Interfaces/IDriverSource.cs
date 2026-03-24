using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Sürücü kaynak arayüzü — her vendor için bir implementasyon.
/// </summary>
public interface IDriverSource
{
    SourceType SourceType { get; }
    Task<IReadOnlyList<CandidateDriver>> SearchAsync(DeviceInfo device, CancellationToken cancellationToken = default);
}
