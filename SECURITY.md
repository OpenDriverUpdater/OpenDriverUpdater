# Security Policy

## Supported Versions

OpenDriverUpdater **has no released version yet**. Development happens on
`main`; pre-release artifacts (for example `v0.1.0-alpha.1`) are unsigned and
are intended for testing only. Do not rely on them on security-sensitive
systems.

| Version                     | Supported |
| --------------------------- | --------- |
| unreleased (`main`)         | Testing only — no security support |
| `0.1.0-alpha.1` pre-release | Not signed; manual verification via `SHA256SUMS.txt` |
| anything else               | No |

## Reporting a Vulnerability

- Report **privately** through GitHub: repository → **Security** tab →
  **Report a vulnerability**.
- Do **not** open a public issue for anything exploitable, especially around:
  - the driver install pipeline (`pnputil`),
  - download integrity,
  - Authenticode / catalog signature validation,
  - trust scoring and update recommendations.
- Expect an initial response within 7 days.
- Fixes are shipped first for `main`, then reflected in the next pre-release
  or release with a security note.

## In Scope

- The WinUI 3 desktop application.
- The `odu` CLI.
- The download/install pipeline and signature validation.

## Out of Scope (current)

- The static website (`Web/`): it is deployed separately and processes no
  user data server-side. If you found an issue in it, please still report it
  through GitHub issues.
