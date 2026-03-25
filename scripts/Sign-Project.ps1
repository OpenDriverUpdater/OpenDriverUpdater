# Sign-Project.ps1
# Automates signing of built DLLs and EXEs using signtool.exe and the project certificate.

param(
    [string]$Configuration = "Debug"
)

$binDir = Join-Path $PSScriptRoot "..\src\OpenDriverUpdater.App\bin\$Configuration\net8.0-windows10.0.19041.0"
if (!(Test-Path $binDir)) { 
    Write-Error "Binary directory not found: $binDir. Please build the project first."
    return
}

$certSubject = "OpenDriverUpdater"
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"

if (!(Test-Path $signtool)) {
    # Try finding via environment
    $signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if (!$signtool) {
        Write-Error "SignTool.exe not found. Please install Windows SDK."
        return
    }
}

$filesToSign = Get-ChildItem -Path $binDir -Include "*.exe", "*.dll" -Recurse

Write-Host "Signing files in $binDir..." -ForegroundColor Cyan

foreach ($file in $filesToSign) {
    Write-Host "Signing $($file.Name)..."
    & $signtool sign /a /n "$certSubject" /t http://timestamp.digicert.com /fd sha256 /v $file.FullName
}

Write-Host "Signing complete." -ForegroundColor Green
