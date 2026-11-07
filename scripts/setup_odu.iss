; setup_odu.iss
; Inno Setup Script for OpenDriverUpdater (WinUI 3 App only).
;
; SECURITY POLICY:
;   - This installer never installs any certificate or root CA.
;   - Public release binaries must be Authenticode-signed BEFORE packaging with
;     a trusted identity (SignPath Foundation or an OV certificate); local =
;     scripts/release.ps1. A self-signed certificate must never be distributed
;     and must never be added to the user's Trusted Root store.
;   - Version alignment: this file mirrors Directory.Build.props
;     (<VersionPrefix>). The pre-release suffix (alpha.1) is not representable
;     in Inno's AppVersion and is tracked as the GitHub pre-release tag
;     (v0.1.0-alpha.1).

[Setup]
AppId={{D3E8C946-4297-4E5B-8C9E-715B5EB1D247}
AppName=OpenDriverUpdater
AppVersion=0.1.0
DefaultDirName={autopf}\OpenDriverUpdater
DefaultGroupName=OpenDriverUpdater
UninstallDisplayIcon={app}\OpenDriverUpdater.exe
Compression=lzma2
SolidCompression=yes
OutputDir=..\dist
OutputBaseFilename=OpenDriverUpdater_Setup
SetupIconFile=..\img\odu_favicon.ico
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; WinUI 3 App — Release publish output (x64, self-contained).
Source: "..\src\OpenDriverUpdater.App\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\OpenDriverUpdater"; Filename: "{app}\OpenDriverUpdater.exe"
Name: "{autodesktop}\OpenDriverUpdater"; Filename: "{app}\OpenDriverUpdater.exe"; Tasks: desktopicon

[Run]
; Launch app after install
Filename: "{app}\OpenDriverUpdater.exe"; Description: "{cm:LaunchProgram,OpenDriverUpdater}"; Flags: nowait postinstall skipifsilent
