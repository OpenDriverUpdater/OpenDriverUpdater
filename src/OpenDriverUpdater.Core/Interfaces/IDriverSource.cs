using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;
public interface IDriverSource
{
    SourceType SourceType { get; }
    Task<IReadOnlyList<CandidateDriver>> SearchAsync(DeviceInfo device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kaynağa özgü referanstan (SourceReference) indirme URL'sini çözümler.
    /// Desteklenmiyorsa null döner.
    /// </summary>
    Task<string?> ResolveDownloadUrlAsync(string sourceReference, CancellationToken cancellationToken = default);
}
