; Inno Setup script for Philter Desktop.
;
; This packages a `dotnet publish` (win-x64) output folder into a single setup .exe for direct
; download. It installs per-user by default (no admin required), and its optional "start at sign-in"
; task writes the SAME HKCU\Run entry the app's own Settings toggle uses (StartupManager), so the
; two stay consistent.
;
; Build:  see Installer\build-setup.ps1  (publishes, then compiles this script with ISCC).
; Requires Inno Setup 6.3+ (for the x64compatible architecture identifier).

#define AppName "Philter Desktop"
#define Publisher "Philterd, LLC"
#define AppExe "PhilterDesktop.exe"
#define AppUrl "https://www.philterd.ai"

; Overridable on the ISCC command line: /DAppVersion=1.2.3 and /DPublishDir=<path>
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
; build-setup.ps1 always passes /DPublishDir (derived from the project's TargetFramework); this
; fallback is only used for a direct ISCC run and must match the current TFM.
#ifndef PublishDir
  #define PublishDir "..\PhilterDesktop\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif

[Setup]
; Keep AppId stable across releases so upgrades replace the prior install.
AppId={{B7E5A3D2-9C41-4E8A-A1F6-2D0C7B9E4F31}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
AppPublisherURL={#AppUrl}
AppSupportURL=https://philterd.github.io/PhilterDesktop/
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\images\PhilterDesktop.ico
OutputDir=Output
OutputBaseFilename=PhilterDesktop-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; When the build passes /DSign, sign the installer AND the generated uninstaller. The "philtersign"
; sign tool is registered on the ISCC command line by build-setup.ps1 (/Sphiltersign=...).
#ifdef Sign
SignTool=philtersign
SignedUninstaller=yes
#endif
; Per-user by default (no elevation); users may choose all-users in the dialog or via /ALLUSERS.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; .NET 10 desktop apps require Windows 10 1809+.
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start {#AppName} automatically when I sign in (runs minimized to the tray and watches folders)"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
; The entire publish output (app, dependencies, native PDF libs under runtimes\..\native that
; publish flattens to the app root, and the bundled Models\ folder).
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Optional auto-start. Same value name + "--minimized" switch as StartupManager, so the in-app
; "Start at sign-in" toggle reflects/controls this too. Removal is handled in [Code] on uninstall
; (covers the case where the user later enabled it from inside the app).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PhilterDesktop"; ValueData: """{app}\{#AppExe}"" --minimized"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Always remove the auto-start entry on uninstall, however it was set.
    RegDeleteValue(HKEY_CURRENT_USER,
      'Software\Microsoft\Windows\CurrentVersion\Run', 'PhilterDesktop');

    // Always remove the Explorer right-click ("Redact with Philter Desktop") entries, which the
    // app writes per-user when the context-menu setting is enabled.
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER,
      'Software\Classes\SystemFileAssociations\.pdf\shell\PhilterDesktop');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER,
      'Software\Classes\SystemFileAssociations\.docx\shell\PhilterDesktop');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER,
      'Software\Classes\SystemFileAssociations\.txt\shell\PhilterDesktop');
  end;

  // After the program files are gone, offer to also delete the saved data for this account
  // (policies, contexts, settings, and redaction history — including any sensitive text it
  // captured). Default is No, so an upgrade/reinstall keeps everything; a silent uninstall keeps
  // it too. The redacted output files the user already saved live elsewhere and are never touched.
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\PhilterDesktop');
    if DirExists(DataDir) then
    begin
      if MsgBox('Also remove your saved Philter Desktop data for this account?' + #13#10 + #13#10 +
                'This permanently deletes your policies, contexts, settings, and redaction history ' +
                '(including any sensitive text it captured).' + #13#10 + #13#10 +
                'Choose No to keep it, so reinstalling restores everything. Either way, the redacted ' +
                'files you already saved are not affected.',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
