#define AppName "FrioCalc"
#ifndef BetaNumber
  #define BetaNumber "3"
#endif
#ifndef SourceDir
  #error SourceDir required
#endif
#ifndef OutputDir
  #error OutputDir required
#endif
[Setup]
AppId={{BFE39269-7325-4B56-A122-8623E6CD85DF}
AppName={#AppName}
AppVersion=1.0.0-beta.{#BetaNumber}
AppPublisher=FrioCalc
VersionInfoVersion=1.0.0.{#BetaNumber}
VersionInfoDescription=FrioCalc Beta {#BetaNumber}
DefaultDirName={localappdata}\Programs\FrioCalc
DefaultGroupName=FrioCalc
UninstallDisplayIcon={app}\FrioCalc.Psychrometrics.exe
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=FrioCalc-Beta{#BetaNumber}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no
[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; Flags: unchecked
[Icons]
Name: "{userprograms}\FrioCalc"; Filename: "{app}\FrioCalc.Psychrometrics.exe"
Name: "{userdesktop}\FrioCalc"; Filename: "{app}\FrioCalc.Psychrometrics.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\FrioCalc.Psychrometrics.exe"; Description: "Abrir FrioCalc"; Flags: nowait postinstall skipifsilent
