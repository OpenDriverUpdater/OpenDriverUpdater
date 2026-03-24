using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Rules;

/// <summary>
/// Sürücü kaynak öncelik kuralları.
/// </summary>
public static class SourcePriorityRules
{
    /// <summary>
    /// Varsayılan kaynak öncelik sırası.
    /// Düşük index = yüksek öncelik.
    /// </summary>
    public static readonly IReadOnlyList<Enums.SourceType> DefaultPriority =
    [
        Enums.SourceType.MicrosoftCatalog,
        Enums.SourceType.Intel,
        Enums.SourceType.Amd,
        Enums.SourceType.Nvidia,
        Enums.SourceType.Oem,
        Enums.SourceType.Community,
    ];

    /// <summary>
    /// Aynı sürüm numarasına sahip iki adaydan hangisi tercih edilmeli?
    /// Daha yüksek güven puanına sahip olan tercih edilir.
    /// Eşitlik durumunda kaynak önceliğine bakılır.
    /// </summary>
    public static CandidateDriver PreferCandidate(
        CandidateDriver a, TrustScore trustA,
        CandidateDriver b, TrustScore trustB)
    {
        if (trustA.Points != trustB.Points)
            return trustA.Points > trustB.Points ? a : b;

        var indexA = DefaultPriority.IndexOf(a.SourceType);
        var indexB = DefaultPriority.IndexOf(b.SourceType);

        return indexA <= indexB ? a : b;
    }
}

file static class ListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
    {
        for (int i = 0; i < list.Count; i++)
            if (EqualityComparer<T>.Default.Equals(list[i], item))
                return i;
        return int.MaxValue;
    }
}
