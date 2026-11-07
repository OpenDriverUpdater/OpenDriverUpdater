# Generate-SigningCert.ps1
# Creates a self-signed code-signing certificate for LOCAL DEVELOPMENT/TESTING ONLY.
#
# This certificate is NOT a publicly trusted code-signing certificate. Do not
# distribute it, do not add it to a release, and do not instruct end users to
# install it as a trusted root. A private root CA must never be shipped with the
# application: if its private key leaks, every machine that trusts the root can
# be tricked into trusting a forged signature.
#
# For public releases use SignPath Foundation (OSS) or a paid OV certificate.

# Keep development certificates outside the repository so a local .cer/.pfx can
# never be committed by accident.
$certDir = Join-Path $env:LOCALAPPDATA "OpenDriverUpdater\dev-certs"
if (!(Test-Path $certDir)) { New-Item -ItemType Directory -Path $certDir -Force }

$subject = "CN=OpenDriverUpdater Local Dev, O=OpenDriverUpdater, OU=Local Only"

Write-Host "--- Generating LOCAL DEVELOPMENT code-signing certificate ---" -ForegroundColor Cyan
Write-Host "This certificate must never be used for a public release." -ForegroundColor Red

# Remove old dev certs with same subject if they exist (clean slate)
Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $subject } | Remove-Item -ErrorAction SilentlyContinue

$cert = New-SelfSignedCertificate -Type CodeSigningCert `
    -Subject $subject `
    -KeyExportPolicy Exportable `
    -HashAlgorithm sha256 -KeyLength 2048 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(1)

# Export as .CER only for local test inspection; never ship this file.
$cerPath = Join-Path $certDir "OpenDriverUpdater.cer"
Export-Certificate -Cert $cert -FilePath $cerPath
Write-Host "Local test certificate exported to: $cerPath (do not distribute)" -ForegroundColor Yellow

# Export as .PFX for local build machines. The password is prompted and never stored.
$pfxPath = Join-Path $certDir "OpenDriverUpdater.pfx"
$password = Read-Host "PFX password" -AsSecureString
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password
Write-Host "Local private key exported to: $pfxPath (password not stored)" -ForegroundColor Yellow

Write-Host "`nWARNING: This is a self-signed local-development certificate." -ForegroundColor Red
Write-Host "Do NOT distribute it and do NOT install it into 'Trusted Root Certification Authorities' on user machines." -ForegroundColor Red
Write-Host "Use SignPath Foundation (for OSS) or a paid OV certificate for public releases." -ForegroundColor Red
