<#
.SYNOPSIS
  Test, publish, optionally sign, package and release OpenDriverUpdater.

.DESCRIPTION
  No secret lives in this file or in the repository. Signing uses ONE of:
    1) $env:ODU_SIGN_THUMBPRINT  certificate already imported into Cert:\CurrentUser\My (preferred)
    2) $env:ODU_PFX_PATH         path to a .pfx OUTSIDE the repo; the password is prompted as a
                                 SecureString, the cert is imported, nothing is stored.

  If neither is configured, the script fails unless -Unsigned is passed. With -Unsigned it
  skips Authenticode signing, still creates the zip and SHA256SUMS.txt, and the release notes
  state that the build is not code-signed.

  The version comes from Directory.Build.props (VersionPrefix + VersionSuffix), the single
  source of truth. Needs: .NET 8 SDK, Windows SDK (signtool) for signed builds, GitHub CLI
  (gh auth login, once).

.EXAMPLE
  .\scripts\release.ps1 -DryRun -Unsigned   # unsigned alpha, everything except tag/release
  .\scripts\release.ps1 -DryRun             # signed, everything except tag/release
  .\scripts\release.ps1                     # full release
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$Unsigned
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

$app  = 'OpenDriverUpdater'
$proj = 'src\OpenDriverUpdater.App\OpenDriverUpdater.App.csproj'
$out  = 'publish\win-x64'

function Step($m) { Write-Host "`n== $m" -ForegroundColor Cyan }
function Run($exe, [string[]]$a) { & $exe @a; if ($LASTEXITCODE -ne 0) { throw "$exe failed (exit $LASTEXITCODE)" } }

Step 'Version'
$version = (& dotnet msbuild $proj -getProperty:Version).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.]+)?$') {
  throw "Invalid version '$version'. Set VersionPrefix/VersionSuffix in Directory.Build.props (e.g. 0.1.0 + alpha.1)."
}
$tag = "v$version"
Write-Host "$app $tag"

Step 'Git preconditions'
if (git status --porcelain) { throw 'Working tree is not clean. Commit or stash first.' }
Run git @('fetch', 'origin', '--tags', '--quiet')
if ((git rev-parse --abbrev-ref HEAD) -ne 'main') { throw 'Releases are made from main only.' }
if ((git rev-parse HEAD) -ne (git rev-parse origin/main)) { throw 'main differs from origin/main. Push or pull first.' }
if (git tag -l $tag) { throw "Tag $tag already exists." }
if (-not $DryRun) { Run gh @('auth', 'status') }

Step 'Tests'
Run dotnet @('test', 'OpenDriverUpdater.sln', '-c', 'Release')

Step 'Publish'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
Run dotnet @('publish', $proj, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', "-p:Version=$version", '-o', $out)

Step 'Sign'
if ($Unsigned) {
  Write-Warning 'Unsigned build requested. Authenticode signing is skipped.'
}
else {
  $thumb = $env:ODU_SIGN_THUMBPRINT
  if (-not $thumb -and $env:ODU_PFX_PATH) {
    $pfxPassword = Read-Host 'PFX password' -AsSecureString
    $thumb = (Import-PfxCertificate -FilePath $env:ODU_PFX_PATH -CertStoreLocation Cert:\CurrentUser\My -Password $pfxPassword).Thumbprint
  }
  if (-not $thumb) {
    throw 'No code-signing certificate configured. Set ODU_SIGN_THUMBPRINT (or ODU_PFX_PATH), or pass -Unsigned for an unsigned alpha build.'
  }

  $signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.*\x64\signtool.exe" |
    Sort-Object { [version]$_.Directory.Parent.Name } | Select-Object -Last 1
  if (-not $signtool) { throw 'signtool.exe not found. Install the Windows SDK.' }

  $files = Get-ChildItem $out -Recurse -Include "$app*.exe", "$app*.dll"
  if (-not $files -or $files.Count -eq 0) {
    throw "No publish outputs matching $app*.exe/$app*.dll found under $out. Refusing to claim a signed build."
  }
  foreach ($f in $files) {
    Run $signtool.FullName @('sign', '/sha1', $thumb, '/fd', 'sha256', '/tr', 'http://timestamp.digicert.com', '/td', 'sha256', $f.FullName)
    Run $signtool.FullName @('verify', '/pa', $f.FullName)
  }
}

Step 'Package'
if (Test-Path dist) { Remove-Item dist -Recurse -Force }
New-Item dist -ItemType Directory | Out-Null
$zip = "dist\$app-$tag-win-x64.zip"
Compress-Archive -Path "$out\*" -DestinationPath $zip

# SHA256SUMS.txt covers exactly the published zip asset. It is not a replacement
# for code signing; it only lets users detect a corrupted or swapped download.
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()
Set-Content dist\SHA256SUMS.txt "$hash  $(Split-Path $zip -Leaf)" -Encoding ascii
Get-Content dist\SHA256SUMS.txt

if ($DryRun) {
  Write-Host "`nDry run finished. Artifacts are in dist\. Nothing was tagged, pushed or published." -ForegroundColor Yellow
  return
}

Step 'GitHub release'
$flags = @()
if ($version -match '-') { $flags += '--prerelease' }
if ($Unsigned) {
  $notes = 'This build is not code-signed. Verify it with SHA256SUMS.txt.'
} else {
  $notes = 'This build is code-signed. Verify the Authenticode signature and the SHA-256 checksum in SHA256SUMS.txt.'
}
$sha = git rev-parse HEAD
$releaseArgs = @(
  'release', 'create', $tag, $zip, 'dist\SHA256SUMS.txt',
  '--target', $sha, '--title', "$app $tag", '--notes', $notes
) + $flags
Run gh $releaseArgs
Write-Host "Released $tag" -ForegroundColor Green
