; Inno Setup script for Philter Desktop.
;
; This packages the `dotnet publish` output into a single setup .exe for direct download. It
; installs per-user by default (no admin required), and its optional "start at sign-in" task writes
; the SAME HKCU\Run entry the app's own Settings toggle uses (StartupManager), so the two stay
; consistent.
;
; ONE installer covers BOTH architectures. build-setup.ps1 publishes win-x64 and win-arm64 and
; passes both publish dirs; the [Files] entries below pick the matching set at install time, so
; users never choose based on their hardware. The arch-neutral payload (the PhEye model, ~89 MB) is
; stored once rather than per arch.
;
; Build:  see Installer\build-setup.ps1  (publishes both RIDs, then compiles this script with ISCC).
; Requires Inno Setup 6.3+ (for the x64compatible / arm64 architecture identifiers).

#define AppName "Philter Desktop"
#define Publisher "Philterd, LLC"
#define AppExe "PhilterDesktop.exe"
#define AppUrl "https://www.philterd.ai"

; Overridable on the ISCC command line: /DAppVersion=1.2.3 and /DPublishDirX64 /DPublishDirArm64.
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
; build-setup.ps1 always passes both publish dirs (derived from the project's TargetFramework and
; RIDs); these fallbacks are only used for a direct ISCC run and must match the current TFM.
#ifndef PublishDirX64
  #define PublishDirX64 "..\PhilterDesktop\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif
#ifndef PublishDirArm64
  #define PublishDirArm64 "..\PhilterDesktop\bin\Release\net10.0-windows10.0.19041.0\win-arm64\publish"
#endif
; The arch-neutral files are byte-identical in both publish dirs; ship a single copy of them.
#define SharedDir PublishDirX64

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
; Without this, Add/Remove Programs lists "Philter Desktop version 1.1.0" as the name. The version
; still shows in its own Version column (from AppVersion), so repeating it in the name is noise.
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\images\PhilterDesktop.ico
OutputDir=Output
OutputBaseFilename=PhilterDesktop-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Show the Philterd Commercial License Agreement (EULA) and require acceptance before installing. The file
; is the one build-setup.ps1 downloads from philterd.ai into the publish dir (a checked-in snapshot is
; present as a fallback), so it always exists when ISCC runs.
LicenseFile={#SharedDir}\philterd-eula.txt
; When the build passes /DSign, sign the installer AND the generated uninstaller. The "philtersign"
; sign tool is registered on the ISCC command line by build-setup.ps1 (/Sphiltersign=...).
#ifdef Sign
SignTool=philtersign
SignedUninstaller=yes
#endif
; Per-user by default (no elevation); users may choose all-users in the dialog or via /ALLUSERS.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
; Runs on both Intel/AMD (x64) and Windows-on-ARM (arm64), installing the native build for each.
ArchitecturesAllowed=x64compatible or arm64
ArchitecturesInstallIn64BitMode=x64compatible or arm64
; .NET 10 desktop apps require Windows 10 1809+.
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start {#AppName} automatically when I sign in (runs minimized to the tray and watches folders)"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
; Arch-neutral payload, stored once: the bundled PhEye name-detection model and the EULA text.
Source: "{#SharedDir}\Models\*"; DestDir: "{app}\Models"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SharedDir}\philterd-eula.txt"; DestDir: "{app}"; Flags: ignoreversion

; Arch-specific payload: the app, its dependencies, and the native libs (ONNX Runtime, PDFium,
; SkiaSharp) that publish flattens to the app root. Test IsArm64 FIRST: on Windows-on-ARM
; IsX64Compatible is also true (x64 emulation), so "not IsArm64" is the correct x64 guard.
Source: "{#PublishDirArm64}\*"; DestDir: "{app}"; Excludes: "Models\*,philterd-eula.txt"; Check: IsArm64; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PublishDirX64}\*"; DestDir: "{app}"; Excludes: "Models\*,philterd-eula.txt"; Check: not IsArm64; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Publish flattens the native libraries to the app root, so the copy step overwrites them by name and
; nothing is left stale. The exception is the handful of files whose NAME carries the architecture
; (Microsoft.DiaSymReader.Native.<arch>.dll, mscordaccore_<arch>_<arch>_<ver>.dll): installing the
; other architecture over an existing install would leave the previous one's copies behind forever.
; Remove the opposite architecture's files first. These patterns cannot match the set about to be
; installed, and this runs on every install, so it also cleans up installs made before this existed.
Type: files; Name: "{app}\*.arm64.dll"; Check: not IsArm64
Type: files; Name: "{app}\*_arm64_*.dll"; Check: not IsArm64
Type: files; Name: "{app}\*.amd64.dll"; Check: IsArm64
Type: files; Name: "{app}\*_amd64_*.dll"; Check: IsArm64

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Record which architecture was installed so a later run can detect an arch switch and clear the
; stale binaries first (see PrepareToInstall). HKA follows the install mode: HKLM for all-users,
; HKCU for per-user.
Root: HKA; Subkey: "Software\Philterd\Philter Desktop"; ValueType: string; ValueName: "InstalledArch"; ValueData: "{code:GetCurrentArch}"; Flags: uninsdeletevalue uninsdeletekeyifempty

; Optional auto-start. Same value name + "--minimized" switch as StartupManager, so the in-app
; "Start at sign-in" toggle reflects/controls this too. Removal is handled in [Code] on uninstall
; (covers the case where the user later enabled it from inside the app).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PhilterDesktop"; ValueData: """{app}\{#AppExe}"" --minimized"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// The architecture this run installs, recorded in the registry so support can tell which build a
// machine got. Mirrors the IsArm64-first order used in [Files]: on Windows-on-ARM IsX64Compatible
// is also true, so IsArm64 must be what decides.
function GetCurrentArch(Param: string): string;
begin
  if IsArm64 then
    Result := 'arm64'
  else
    Result := 'x64';
end;

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
  // (policies, contexts, settings, and redaction history, including any sensitive text it
  // captured). Default is No, so an upgrade/reinstall keeps everything. The redacted output files
  // the user already saved live elsewhere and are never touched.
  //
  // An unattended uninstall NEVER asks and never deletes. Two guards, because either alone leaves a
  // hole: plain MsgBox ignores /SUPPRESSMSGBOXES and displays anyway, so a scripted uninstall stalls
  // on a dialog until someone clicks (and loses the data if they click Yes), while SuppressibleMsgBox
  // alone would still prompt under a bare /VERYSILENT.
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\PhilterDesktop');
    if DirExists(DataDir) and not UninstallSilent then
    begin
      if SuppressibleMsgBox('Also remove your saved Philter Desktop data for this account?' + #13#10 + #13#10 +
                'This permanently deletes your policies, contexts, settings, and redaction history ' +
                '(including any sensitive text it captured).' + #13#10 + #13#10 +
                'Choose No to keep it, so reinstalling restores everything. Either way, the redacted ' +
                'files you already saved are not affected.',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2, IDNO) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
