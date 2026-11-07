using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.Infrastructure.Tests;

internal sealed class FakeSignatureValidator : ISignatureValidator
{
    /// <summary>null ise doğrulama her zaman true döner.</summary>
    public Func<string, bool>? OnValidate { get; set; }

    public List<string> Calls { get; } = new();

    public bool IsValidSigned(string filePath)
    {
        Calls.Add(filePath);
        return OnValidate?.Invoke(filePath) ?? true;
    }
}
