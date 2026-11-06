namespace OpenDriverUpdater.Core.Models;

public sealed record VerificationResult(bool HashVerified, bool SignatureValidated);
