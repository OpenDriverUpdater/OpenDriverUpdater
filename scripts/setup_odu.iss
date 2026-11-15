; setup_odu.iss
; Inno Setup Script for OpenDriverUpdater (WinUI 3 App only).
;
; SECURITY POLICY:
;   - This installer never installs any certificate or root CA.
;   - Public release binaries must be Authenticode-signed BEFORE packaging with
;     a trusted identity (SignPath Foundation or an OV certificate); local =
;     scripts/release.ps1. A self-signed certificate must never be distributed
;     and must never be added to the user's Trusted Root store.
;   - Version: scripts/release.ps1 passes the version from
;     Directory.Build.props (/DAppVersion=0.1.0 /DAppVersionLabel=0.1.0-alpha.1).
;     The defaults below are only for compiling this script by hand.
;   - Input: the publish folder made by release.ps1 (publish\win-x64), or
;     by hand with:
;       dotnet publish src\OpenDriverUpdater.App\OpenDriverUpdater.App.csproj -c Release -r win-x64 --self-contained true -o publish\win-x64

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef AppVersionLabel
  #define AppVersionLabel AppVersion
#endif
#ifndef PublishDir
  #define PublishDir "..\publish\win-x64"
#endif

[Setup]
AppId={{D3E8C946-4297-4E5B-8C9E-715B5EB1D247}
AppName=OpenDriverUpdater
AppVersion={#AppVersion}
AppVerName=OpenDriverUpdater {#AppVersionLabel}
VersionInfoVersion={#AppVersion}
AppPublisher=rossgalb
AppPublisherURL=https://github.com/rossgalb/OpenDriverUpdater
AppSupportURL=https://github.com/rossgalb/OpenDriverUpdater/issues
DefaultDirName={autopf}\OpenDriverUpdater
DefaultGroupName=OpenDriverUpdater
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\OpenDriverUpdater.exe
UninstallDisplayName=OpenDriverUpdater
Compression=lzma2
SolidCompression=yes
OutputDir=..\dist
OutputBaseFilename=OpenDriverUpdater-v{#AppVersionLabel}-win-x64-setup
SetupIconFile=..\img\odu_favicon.ico
PrivilegesRequired=admin
; The app is a 64-bit, self-contained build: install to "Program Files", not "(x86)".
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
; Close a running copy before replacing its files
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; WinUI 3 App — Release publish output (x64, self-contained). Debug symbols are left out.
Source: "{#PublishDir}\*"; Excludes: "*.pdb"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\OpenDriverUpdater"; Filename: "{app}\OpenDriverUpdater.exe"
Name: "{autodesktop}\OpenDriverUpdater"; Filename: "{app}\OpenDriverUpdater.exe"; Tasks: desktopicon

[Run]
; Launch app after install
Filename: "{app}\OpenDriverUpdater.exe"; Description: "{cm:LaunchProgram,OpenDriverUpdater}"; Flags: nowait postinstall skipifsilent
