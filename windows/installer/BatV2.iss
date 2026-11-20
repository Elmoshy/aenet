; Inno Setup script. Built by the GitHub Action. Output: installer\Output\BatV2-Setup.exe
#define AppName "BatV2"
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
AppId={{B7A1D1E2-4C7A-4F1B-9A55-3E0A6C1F8B21}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\BatV2.exe
UninstallDisplayName={#AppName}
SetupIconFile=..\icon.ico
OutputDir=Output
OutputBaseFilename=BatV2-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
; per-user install by default (no admin prompt). The wizard lets you pick all users
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "..\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\BatV2.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\BatV2.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\BatV2.exe"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; stop the app (Windows then kills xray with it) and make sure the system proxy is not left on
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM BatV2.exe"; Flags: runhidden; RunOnceId: "KillApp"
Filename: "{sys}\reg.exe"; Parameters: "add ""HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings"" /v ProxyEnable /t REG_DWORD /d 0 /f"; Flags: runhidden; RunOnceId: "ProxyOff"

[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := True;
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) or (Release < 528040) then
    Result := MsgBox('BatV2 needs .NET Framework 4.8. Windows 11 and recent Windows 10 already have it.' + #13#10 +
      'If the app does not start, update Windows or install it from Microsoft.' + #13#10#13#10 +
      'Continue anyway?', mbConfirmation, MB_YESNO) = IDYES;
end;
