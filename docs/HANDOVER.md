# OpenDriverUpdater – HANDOVER

## Decisions

1. **No DI container**. Keep the App.* composition root, but ViewModels get interfaces via constructor (no static `App.*` access inside ViewModels) so they are unit-testable.
2. **Settings file**: `%LocalAppData%\OpenDriverUpdater\settings.json` with a "version" field, atomic write (temp file + move), missing or corrupt file falls back to defaults, never crashes.
3. **UI status enum lives in the App layer**: `Detected, UpdateAvailable, UpToDate, Unverified`. The mapper is conservative: anything not provably official and high-confidence is `Unverified`. No status may imply verification. Unit tested.
4. **Core stays unchanged**. Add App-level `TrustCapabilities` (`HashVerification=false`, `SignatureValidation=false`) with comments pointing to the future Core work. Trust UI reads it.
5. **No fake progress**. Scan UI uses an indeterminate bar and honest text until Core can report real progress.
6. **Navigation keeps**: Dashboard, Devices, Updates, Logs, Settings. Do NOT add Backup or System Info pages. Frame navigation stays in code-behind for now.
7. **New or rewritten UI uses Resources.resw**. Do not mass-migrate untouched pages.

## Phase 0 Recon Notes

...

### Trust rules (Phase 0.6)
- Hardware ID exact match is a gate: no match => TrustLevel.Untrusted, candidate never recommended. Matching uses StringComparison.OrdinalIgnoreCase, device HardwareIds vs candidate SupportedHardwareIds. SUBSYS/REV-suffixed IDs are treated as exact strings; no fuzzy matching.
- IsSigned renamed to SourceReportsSigned. It is a claim, earns 0 points.
- SignatureValidated and HashVerified are new booleans, default false. TrustLevel.Verified reachable only if both true.
- Max level without verification is Trusted.
- OEM match uses case-insensitive normalized contains.
- JSON schema change: IsSigned → SourceReportsSigned.

### UI mapping
Trusted => UpdateAvailable, everything else => Unverified

## Code signing

- **Current chain is a private root CA.** `certs/OpenDriverUpdaterRoot.cer` and
  `certs/OpenDriverUpdater.cer` are self-made certificates. Windows does not trust
  them, they will show "Unknown publisher", and they must **not** be distributed
  or installed as a trusted root on end-user machines. If the private key leaks,
  every machine that trusts the root can be tricked into trusting forged signatures.
- **Do not ship `.cer` files in release artifacts.** Releases contain only the
  application zip and `SHA256SUMS.txt`. `scripts/release.ps1` does not copy
  certificates into `dist/`.
- **Options:**
  1. **SignPath Foundation (recommended for this OSS project).** Free code signing
     for OSI-licensed public repositories. Requirements include a public repo, a
     documented code-signing policy, 2FA for all organization members, and signing
     in CI (not on a local machine). The publisher name appears as
     "SignPath Foundation". Approval is not guaranteed; driver updaters may be
     reviewed as a potentially unwanted software category.
  2. **Paid OV/EV code-signing certificate.** Publisher shows the organization.
     Requires identity validation and secure key storage; CI signing must use a
     hardware token/HSM or a cloud signing service.
  3. **Unsigned alpha.** Publish with `SHA256SUMS.txt` and a clear "This build is
     not code-signed" note. A SHA-256 checksum is not a cryptographic signature
     and does not prove the publisher's identity; it only detects corruption or a
     swapped download.
- **Decision: pending owner decision.** Until a trusted signing identity is
  available, public releases must be unsigned (or not published) and must include
  `SHA256SUMS.txt`.
- `scripts/release.ps1` fails if no thumbprint/PFX is configured unless
  `-Unsigned` is passed. With `-Unsigned` it skips signing, still creates the zip
  and checksum manifest, and the release notes state that the build is not
  code-signed.

## Release gate

Phase 0.8 (test reconciliation 29 -> 35+, App.Tests in the solution run, real
Catalog research with real saved pages, MatchBasis hardening) remains open and
blocks the first public release.
