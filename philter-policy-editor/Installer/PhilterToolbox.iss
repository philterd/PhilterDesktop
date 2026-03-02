#define Version "2.0.0"
#define Name "Philter Toolbox"
#define Copyright "Copyright 2020 Mountain Fog, Inc."

[Setup]
DefaultDirname={pf}\{#Name}
AppName={#Name}
AppVersion={#Version}
AppCopyright={#Copyright}
AppId={{ED26264F-B9D2-452C-AE89-4883B47008D8}
DefaultGroupName={#Name}
AppPublisher=Mountain Fog, Inc.
AppPublisherURL=https://www.mtnfog.com
AppSupportURL=https://www.mtnfog.com/support
AppUpdatesURL=https://www.mtnfog.com
AppComments={#Name}
AppContact=support@mtnfog.com
AppSupportPhone=888-789-3894
OutputDir=./
OutputBaseFilename=philterstudio_{#Version}_setup
UninstallDisplayName={#Name}
VersionInfoVersion={#Version}
VersionInfoCompany=Mountain Fog, Inc.
VersionInfoDescription={#Name}
VersionInfoTextVersion={#Version}
VersionInfoCopyright={#Copyright}
VersionInfoProductName={#Name}
VersionInfoProductVersion={#Version}
VersionInfoProductTextVersion={#Version}
LicenseFile=LICENSE.txt
InfoAfterFile=README.txt
UninstallDisplayIcon={uninstallexe}
AppReadmeFile=README.txt

[Files]
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "RELEASE_NOTES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "philtertoolbox.ini"; DestDir: "{localappdata}\PhilterToolbox"; Flags: onlyifdoesntexist
Source: "..\PhilterToolbox\bin\Release\log4net.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\PhilterToolbox.exe"; DestDir: "{app}"; Flags: ignoreversion sign
Source: "..\PhilterToolbox\bin\Release\DiffPlex.dll"; DestDir: "{app}"
Source: "..\PhilterToolbox\bin\Release\Castle.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\Config.Net.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\DiffPlex.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\PhilterToolbox.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\RestSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\Xceed.Document.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\Xceed.Words.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\PhilterCore.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolboxCLI\bin\Release\PhilterToolboxCLI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolboxCLI\bin\Release\CommandLine.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\log4net.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PhilterToolbox\bin\Release\philter-sdk-net.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Philter Toolbox"; Filename: "{app}\PhilterToolbox.exe"; WorkingDir: "{app}"; IconFilename: "{app}\PhilterToolbox.exe"

[Run]
Filename: "{app}\PhilterToolbox.exe"; WorkingDir: "{app}"; Flags: postinstall nowait; Description: "Run Philter Toolbox"

[Registry]
; See https://stackoverflow.com/a/47745854
Root: "HKCR"; Subkey: "SystemFileAssociations\.txt\shell\philter"; ValueType: string; ValueData: "Philter"; Flags: uninsdeletekey
Root: "HKCR"; Subkey: "SystemFileAssociations\.txt\shell\philter\command"; ValueData: """{app}\PhilterToolbox.exe"" ""%1"""; Flags: uninsdeletekey
Root: "HKCR"; Subkey: "SystemFileAssociations\.docx\shell\philter"; ValueType: string; ValueData: "Philter"; Flags: uninsdeletekey
Root: "HKCR"; Subkey: "SystemFileAssociations\.docx\shell\philter\command"; ValueData: """{app}\PhilterToolbox.exe"" ""%1"""; Flags: uninsdeletekey
