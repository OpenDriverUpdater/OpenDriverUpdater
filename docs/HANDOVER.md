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
