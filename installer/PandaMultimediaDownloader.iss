; Inno Setup script for Panda Multimedia Downloader.
; Built by build.ps1 -Installer (or: iscc /DAppVersion=1.0.0 installer\PandaMultimediaDownloader.iss)
; Expects the publish output in ..\dist\PandaMultimediaDownloader.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\dist\PandaMultimediaDownloader"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

#define AppName "Panda Multimedia Downloader"
#define AppExe "PandaMultimediaDownloader.exe"

[Setup]
AppId={{6F1E2B7C-3D4A-4E8B-9C21-5A7D0B3E9F42}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=Erkan Kaplan
AppPublisherURL=https://github.com/kaplanerkan/PandaMultimediaDownloader
AppSupportURL=https://github.com/kaplanerkan/PandaMultimediaDownloader/issues
; The app writes settings.json, tools\ and downloaded\ next to its exe, so it must live in a
; user-writable folder. Per-user install under %LOCALAPPDATA%\Programs: no admin rights needed.
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\{#AppName}
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=PandaMultimediaDownloader-v{#AppVersion}-win-x64-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
CloseApplications=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
; Bundled yt-dlp / ffmpeg / deno. Optional: without them the app downloads the tools on first run.
Source: "{#SourceDir}\tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Runtime files created by the app. downloaded\ is user data and is deliberately left in place.
Type: filesandordirs; Name: "{app}\tools"
Type: files; Name: "{app}\settings.json"
