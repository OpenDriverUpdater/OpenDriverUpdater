# Generate-SigningCert.ps1
# Creates a high-quality self-signed Code Signing certificate for OpenDriverUpdater.

$certDir = Join-Path $PSScriptRoot "..\certs"
if (!(Test-Path $certDir)) { New-Item -ItemType Directory -Path $certDir }

$subject = "CN=OpenDriverUpdater, O=OpenDriverUpdater, OU=Security"

Write-Host "--- Generating Code Signing Certificate ---" -ForegroundColor Cyan

# Remove old certs with same subject if they exist (clean slate)
Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $subject } | Remove-Item -ErrorAction SilentlyContinue

$cert = New-SelfSignedCertificate -Type CodeSigningCert `
    -Subject $subject `
    -KeyExportPolicy Exportable `
    -HashAlgorithm sha256 -KeyLength 2048 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(10)

# Export as .CER (for users to trust)
$cerPath = Join-Path $certDir "OpenDriverUpdater.cer"
Export-Certificate -Cert $cert -FilePath $cerPath
Write-Host "Certificate exported to: $cerPath" -ForegroundColor Green

# Export as .PFX (for build machines)
$pfxPath = Join-Path $certDir "OpenDriverUpdater.pfx"
$password = ConvertTo-SecureString -String "odu123" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password
Write-Host "Private Key (PFX) exported to: $pfxPath (Password: odu123)" -ForegroundColor Green

Write-Host "`nIMPORTANT: To trust these signatures locally, double-click 'OpenDriverUpdater.cer' and install it into 'Trusted Root Certification Authorities'." -ForegroundColor Yellow
