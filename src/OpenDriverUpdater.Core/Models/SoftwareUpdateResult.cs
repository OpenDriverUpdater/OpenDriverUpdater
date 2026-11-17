namespace OpenDriverUpdater.Core.Models;

/// <summary>Outcome of updating one application.</summary>
public sealed record SoftwareUpdateResult(bool Success, string Message, int ExitCode = 0);
