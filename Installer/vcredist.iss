// Microsoft Visual C++ Runtime for ONNX Runtime (on-device name detection), included by
// PhilterDesktop.iss. build-setup.ps1 passes /DVCRedistDir (the verified redistributables) and
// /DVCRedistMinMajor + /DVCRedistMinMinor (the oldest runtime the bundled DLLs accept). Without them,
// this adds nothing. Expects GetCurrentArch from the including script.
// Included inside the [Code] section, so these are Pascal comments.

#ifdef VCRedistDir

[Files]
Source: "{#VCRedistDir}\vc_redist.arm64.exe"; Flags: dontcopy
Source: "{#VCRedistDir}\vc_redist.x64.exe"; Flags: dontcopy

[Code]
const
  // Oldest runtime the bundled native DLLs accept (build-setup.ps1 reads it from their linker version).
  VCRedistMinMajor = {#VCRedistMinMajor};
  VCRedistMinMinor = {#VCRedistMinMinor};
  // vc_redist exit codes.
  VCRedistOk = 0;
  VCRedistRestartRequired = 3010;
  VCRedistNewerInstalled = 1638;
  ErrorCancelled = 1223;

var
  VCRedistNeedsRestart: Boolean;

// Whether one registry view records this architecture's runtime at the minimum version or later.
function VCRuntimeRecorded(RootKey: Integer): Boolean;
var
  Key: string;
  Installed, Major, Minor: Cardinal;
begin
  if IsArm64 then
    Key := 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\ARM64'
  else
    Key := 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64';
  Result := RegQueryDWordValue(RootKey, Key, 'Installed', Installed) and (Installed = 1) and
            RegQueryDWordValue(RootKey, Key, 'Major', Major) and
            RegQueryDWordValue(RootKey, Key, 'Minor', Minor) and
            ((Major > VCRedistMinMajor) or ((Major = VCRedistMinMajor) and (Minor >= VCRedistMinMinor)));
end;

// The redistributable records itself in the 64-bit view, the 32-bit view, or both.
function VCRuntimeInstalled: Boolean;
begin
  Result := VCRuntimeRecorded(HKLM64) or VCRuntimeRecorded(HKLM32);
end;

procedure WarnVCRuntimeMissing(const Reason: string);
begin
  Log('VC++ Runtime not installed: ' + Reason);
  // Never prompt an unattended install; the log records it.
  if not WizardSilent then
    SuppressibleMsgBox('The Microsoft Visual C++ Runtime, which {#AppName} needs for on-device name ' +
      'detection, was not installed (' + Reason + ').' + #13#10 + #13#10 +
      'Setup will finish installing {#AppName}, but redacting with a policy that detects names will ' +
      'fail until the runtime is installed. Install it from ' +
      'https://aka.ms/vs/17/release/vc_redist.' + GetCurrentArch('') + '.exe, then restart {#AppName}.',
      mbInformation, MB_OK, IDOK);
end;

// Runs the redistributable elevated (UAC) and returns its exit code. ShellExec doesn't report exit
// codes, so cmd writes it to a file; -1 if that file can't be read. Started is False (and Code the
// error) if it couldn't be launched, such as when approval was declined.
function RunElevated(const Path, Params: string; var Code: Integer): Boolean;
var
  CodeFile: string;
  Text: AnsiString;
begin
  CodeFile := ExpandConstant('{tmp}\vc_redist-exitcode.txt');
  DeleteFile(CodeFile);
  // cmd strips the outer quotes; /v:on reads errorlevel after the redistributable exits.
  Result := ShellExec('runas', ExpandConstant('{cmd}'),
    '/v:on /c ""' + Path + '" ' + Params + ' & >"' + CodeFile + '" echo !errorlevel!"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
  if Result then
  begin
    if LoadStringFromFile(CodeFile, Text) then
      Code := StrToIntDef(Trim(String(Text)), -1)
    else
      Code := -1;
  end;
end;

// Installs the runtime matching this machine only when it is missing or too old. A per-user install
// asks for elevation (UAC) just for this step; a silent per-user install can't, so it skips and logs.
procedure InstallVCRuntimeIfNeeded;
var
  FileName, Path, Params: string;
  Code: Integer;
  Started: Boolean;
begin
  if VCRuntimeInstalled then
  begin
    Log('VC++ Runtime is already installed and current.');
    exit;
  end;
  if not IsAdmin and WizardSilent then
  begin
    WarnVCRuntimeMissing('a silent per-user install cannot ask for administrator approval');
    exit;
  end;

  FileName := 'vc_redist.' + GetCurrentArch('') + '.exe';
  ExtractTemporaryFile(FileName);
  Path := ExpandConstant('{tmp}\' + FileName);
  Params := '/install /quiet /norestart';
  WizardForm.StatusLabel.Caption := 'Installing the Microsoft Visual C++ Runtime...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    if IsAdmin then
      Started := Exec(Path, Params, '', SW_HIDE, ewWaitUntilTerminated, Code)
    else
      Started := RunElevated(Path, Params, Code);
  finally
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;

  // Exit code unknown: judge by whether the runtime is now recorded.
  if Started and (Code = -1) then
  begin
    if VCRuntimeInstalled then
      Code := VCRedistOk;
    Log('VC++ Runtime exit code unavailable; installed now: ' + IntToStr(Ord(Code = VCRedistOk)));
  end;

  if not Started then
  begin
    if Code = ErrorCancelled then
      WarnVCRuntimeMissing('administrator approval was declined')
    else
      WarnVCRuntimeMissing('it could not be started: ' + SysErrorMessage(Code));
  end
  else if Code = VCRedistRestartRequired then
  begin
    Log('VC++ Runtime installed; Windows needs a restart.');
    VCRedistNeedsRestart := True;
  end
  else if (Code = VCRedistOk) or (Code = VCRedistNewerInstalled) then
    Log('VC++ Runtime installed (exit code ' + IntToStr(Code) + ').')
  else
    WarnVCRuntimeMissing('its installer returned error ' + IntToStr(Code));
end;

// At ssInstall, not ssPostInstall: Setup asks NeedRestart during the install, so a later step can't
// request the restart.
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    InstallVCRuntimeIfNeeded;
end;

// A runtime upgrade that replaced files in use finishes at the next restart; until then the app may
// load the old copy.
function NeedRestart: Boolean;
begin
  Result := VCRedistNeedsRestart;
end;

#endif
