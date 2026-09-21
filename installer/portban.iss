#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{7C2E9A14-5B6D-4F83-9A20-1D4E8C0B6F55}
AppName=ポート番
AppVersion={#AppVersion}
AppVerName=ポート番 {#AppVersion}
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName=ポート番
VersionInfoDescription=待ち受け中のポートとプロセスを表示する
DefaultDirName={localappdata}\Programs\PortBan
DefaultGroupName=ポート番
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=PortBan-Setup-{#AppVersion}
SetupIconFile=..\Assets\portban.ico
UninstallDisplayIcon={app}\PortBan.exe
UninstallDisplayName=ポート番
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにポート番を追加する"; GroupDescription: "追加のショートカット:"; Flags: unchecked

[Files]
Source: "..\bin\Release\net8.0\win-x64\publish\PortBan.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ポート番"; Filename: "{app}\PortBan.exe"
Name: "{autodesktop}\ポート番"; Filename: "{app}\PortBan.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\PortBan.exe"; Description: "ポート番を起動する"; Flags: nowait postinstall skipifsilent
