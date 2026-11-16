#define MyAppName "Spellbook"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Maiq"
#define MyAppExeName "Spellbook.exe"

[Setup]
AppId={{0BFA48CC-4D90-42BD-BB62-49E7A2165D0F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=publish\installer
OutputBaseFilename=Spellbook-Setup-v{#MyAppVersion}-win-x64
SetupIconFile=Assets\spellbook.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "publish\win-x64-v2.0.0\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir Spellbook"; Flags: nowait postinstall skipifsilent
