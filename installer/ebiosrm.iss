; Installeur Windows pour EBIOS RM (Inno Setup).
;
; Prerequis : avoir lance  build\build-desktop.ps1 -Rid win-x64
; puis compiler ce script avec Inno Setup :  ISCC.exe installer\ebiosrm.iss
; Resultat : installer\output\EbiosRM-Setup.exe

#define AppName "EBIOS RM"
#define AppVersion GetEnv("EBIOSRM_VERSION")
#if AppVersion == ""
  #define AppVersion "0.0.0"
#endif
#define AppExe "EbiosRM.exe"
#define SourceDir "..\build\output\win-x64"

[Setup]
AppId={{9E6F5B2A-4C31-4E2D-9B7A-EB105RM00001}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=EBIOS RM
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Installation par utilisateur : aucune invite UAC.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=EbiosRM-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Désinstaller {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Lancer {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Ne supprime PAS les donnees utilisateur (%LOCALAPPDATA%\EbiosRM) : c'est
; volontaire, l'utilisateur garde ses etudes en cas de reinstallation.
Type: filesandordirs; Name: "{app}\wwwroot"
