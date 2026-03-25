; setup_odu.iss
; Inno Setup Script for OpenDriverUpdater
; Bundles the WinUI 3 App and automatically installs the project Root CA.

[Setup]
AppId={{D3E8C946-4297-4E5B-8C9E-715B5EB1D247}
AppName=OpenDriverUpdater
AppVersion=1.0
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
; Signed Binaries
Source: "..\src\OpenDriverUpdater.App\bin\Debug\net8.0-windows10.0.19041.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Root Certificate
Source: "..\certs\OpenDriverUpdater.cer"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\OpenDriverUpdater"; Filename: "{app}\OpenDriverUpdater.exe"
Name: "{autodesktop}\OpenDriverUpdater"; Filename: "{app}\OpenDriverUpdater.exe"; Tasks: desktopicon

[Run]
; Install Root CA into Trusted Root Certification Authorities
Filename: "certutil.exe"; Parameters: "-addstore -f ""Root"" ""{tmp}\OpenDriverUpdater.cer"""; Flags: runhidden; StatusMsg: "Installing Security Certificate..."
; Launch app after install
Filename: "{app}\OpenDriverUpdater.exe"; Description: "{cm:LaunchProgram,OpenDriverUpdater}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Optional: Remove Root CA on uninstall (be careful if other apps use it)
; Filename: "certutil.exe"; Parameters: "-delstore ""Root"" ""OpenDriverUpdater"""; Flags: runhidden
