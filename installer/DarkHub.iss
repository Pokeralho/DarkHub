#ifndef AppVersion
#define AppVersion "1.1.9"
#endif

[Setup]
AppId=DarkHub
AppName=DarkHub Uninstaller
AppVersion={#AppVersion}
AppVerName=DarkHub v{#AppVersion}
AppPublisher=DarkHub
AppPublisherURL=https://github.com/Pokeralho/DarkHub
AppSupportURL=https://github.com/Pokeralho/DarkHub/issues
AppUpdatesURL=https://github.com/Pokeralho/DarkHub/releases
DefaultDirName={commonpf32}\DarkHub
DefaultGroupName=DarkHub
DisableProgramGroupPage=yes
OutputDir=..\redist\installer
OutputBaseFilename=DarkHub.Setup
SetupIconFile=..\assets\DarkHub.ico
UninstallDisplayIcon={app}\DarkHub.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
MinVersion=10.0
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\redist\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "logs\*"
Source: "..\scripts\Install-DotNetDesktopRuntime.ps1"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsDotNetDesktopRuntime

[Icons]
Name: "{group}\DarkHub"; Filename: "{app}\DarkHub.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall DarkHub"; Filename: "{uninstallexe}"
Name: "{autodesktop}\DarkHub"; Filename: "{app}\DarkHub.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File ""{tmp}\Install-DotNetDesktopRuntime.ps1"""; StatusMsg: "Installing .NET 8 Desktop Runtime..."; Flags: runhidden waituntilterminated; Check: NeedsDotNetDesktopRuntime
Filename: "{app}\DarkHub.exe"; Description: "{cm:LaunchProgram,DarkHub}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetDesktopRuntimeInstalled(): Boolean;
var
  FindRec: TFindRec;
begin
  Result := FindFirst(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App\8.*'), FindRec);
  if Result then
    FindClose(FindRec);
end;

function NeedsDotNetDesktopRuntime(): Boolean;
begin
  Result := not IsDotNetDesktopRuntimeInstalled();
end;
