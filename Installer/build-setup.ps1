# Copyright 2026 Philterd, LLC
# Licensed under the Apache License, Version 2.0.
#
# Builds the setup .exe for Philter Desktop:
#   1. `dotnet publish` the app (win-x64),
#   2. (optional) Authenticode-sign PhilterDesktop.exe with Azure Trusted Signing,
#   3. compile Installer\PhilterDesktop.iss with Inno Setup's ISCC, and
#   4. (optional) sign the generated setup .exe.
#
# The version comes from the project's <Version> (PhilterDesktop.csproj) — bump it there and the
# installer filename follows automatically. Pass -Version only to override it for a one-off build.
#
# By default the publish is self-contained (no .NET runtime prerequisite on the target machine).
# Pass -FrameworkDependent to publish a smaller build that requires the .NET 10 Desktop Runtime.
#
# --- Code signing (Azure Trusted Signing) -------------------------------------------------------
# Signing is ON by default: this Authenticode-signs PhilterDesktop.exe, the installer, and the
# generated uninstaller via signtool + the Trusted Signing dlib. Pass -NoSign for an unsigned dev
# build. Provide the account details via parameters, environment variables, or a local config file
# (highest precedence first):
#   parameter            env var                     signing.local.json key
#   -SigningEndpoint     TRUSTED_SIGNING_ENDPOINT    Endpoint
#   -SigningAccount      TRUSTED_SIGNING_ACCOUNT     CodeSigningAccountName
#   -SigningProfile      TRUSTED_SIGNING_PROFILE     CertificateProfileName
# The Trusted Signing dlib (Azure.CodeSigning.Dlib.dll) is auto-restored from the
# Microsoft.Trusted.Signing.Client NuGet package into Installer\tools\ — no need to configure it.
# (Override with -SigningDlib / TRUSTED_SIGNING_DLIB if you want a specific copy.)
# Copy signing.local.json.example to signing.local.json (gitignored) to use the file. Azure auth
# uses DefaultAzureCredential — sign in first (az login) or set AZURE_TENANT_ID / AZURE_CLIENT_ID /
# AZURE_CLIENT_SECRET (or use a managed identity on a build agent).
#
# Usage:
#   pwsh Installer\build-setup.ps1                       # version from csproj, SIGNED
#   pwsh Installer\build-setup.ps1 -Version 1.2.3        # override the version
#   pwsh Installer\build-setup.ps1 -FrameworkDependent
#   pwsh Installer\build-setup.ps1 -NoTest               # skip the test run (tests run by default)
#   pwsh Installer\build-setup.ps1 -NoSign               # unsigned dev build

param(
    [string]$Version,
    [switch]$FrameworkDependent,
    [switch]$NoTest,
    [switch]$NoSign,
    [string]$SigningEndpoint = $env:TRUSTED_SIGNING_ENDPOINT,
    [string]$SigningAccount  = $env:TRUSTED_SIGNING_ACCOUNT,
    [string]$SigningProfile  = $env:TRUSTED_SIGNING_PROFILE,
    [string]$SigningDlib     = $env:TRUSTED_SIGNING_DLIB,
    [string]$SigntoolPath    = $env:SIGNTOOL_PATH,
    [string]$TimestampUrl    = "http://timestamp.acs.microsoft.com",
    # Version of the Microsoft.Trusted.Signing.Client package to auto-restore for the dlib
    # (empty = latest stable). Only used when -SigningDlib isn't provided another way.
    [string]$TrustedSigningClientVersion = ""
)

# Signing is the default; -NoSign opts out (for local/dev builds without Azure credentials).
$Sign = -not $NoSign

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $repo "PhilterDesktop\PhilterDesktop.csproj"

# Derive the target framework from the csproj so the publish path tracks TFM changes (e.g. the
# Windows SDK version bump for WinRT OCR) instead of being hard-coded.
$tfm = ([xml](Get-Content $proj)).Project.PropertyGroup.TargetFramework | Where-Object { $_ } | Select-Object -First 1
if (-not $tfm) { throw "Could not read <TargetFramework> from $proj" }
$publishDir = Join-Path $repo "PhilterDesktop\bin\Release\$tfm\win-x64\publish"

# ----- Code-signing helpers ---------------------------------------------------------------------

function Resolve-Signtool {
    if ($SigntoolPath -and (Test-Path $SigntoolPath)) { return $SigntoolPath }
    $cmd = (Get-Command signtool.exe -ErrorAction SilentlyContinue).Source
    if ($cmd) { return $cmd }
    $found = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($found) { return $found.FullName }
    throw "signtool.exe not found. Install the Windows SDK, add it to PATH, or pass -SigntoolPath."
}

# Resolves Azure.CodeSigning.Dlib.dll, auto-restoring the Microsoft.Trusted.Signing.Client NuGet
# package into Installer\tools\ (cached between builds) if it isn't already present.
function Resolve-SigningDlib {
    $pkg = 'microsoft.trusted.signing.client'
    $toolsRoot = Join-Path $PSScriptRoot 'tools\trusted-signing'

    # Reuse a previously restored copy unless a specific version was requested.
    if (-not $TrustedSigningClientVersion) {
        $cached = Get-ChildItem (Join-Path $toolsRoot '*\bin\x64\Azure.CodeSigning.Dlib.dll') -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($cached) { return $cached.FullName }
    }

    $version = $TrustedSigningClientVersion
    if (-not $version) {
        $index = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$pkg/index.json"
        $version = $index.versions | Where-Object { $_ -notmatch '-' } | Select-Object -Last 1
        if (-not $version) { throw "Could not determine the latest $pkg version from nuget.org." }
    }

    $dest = Join-Path $toolsRoot $version
    $dll = Join-Path $dest 'bin\x64\Azure.CodeSigning.Dlib.dll'
    if (-not (Test-Path $dll)) {
        Write-Host "Restoring $pkg $version into $dest ..."
        if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        $nupkg = Join-Path ([System.IO.Path]::GetTempPath()) "$pkg.$version.nupkg"
        Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/$pkg/$version/$pkg.$version.nupkg" -OutFile $nupkg
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($nupkg, $dest)
        Remove-Item $nupkg -ErrorAction SilentlyContinue
    }
    if (-not (Test-Path $dll)) { throw "Azure.CodeSigning.Dlib.dll not found after restoring $pkg $version." }
    return $dll
}

# Fill any signing values not given via parameter/env var from the local config file (gitignored).
# See signing.local.json.example for the format. Parameters and env vars take precedence.
$signingConfigPath = Join-Path $PSScriptRoot 'signing.local.json'
if ($Sign -and (Test-Path $signingConfigPath)) {
    Write-Host "Reading signing settings from $signingConfigPath"
    $cfg = Get-Content $signingConfigPath -Raw | ConvertFrom-Json
    if (-not $SigningEndpoint) { $SigningEndpoint = $cfg.Endpoint }
    if (-not $SigningAccount)  { $SigningAccount  = $cfg.CodeSigningAccountName }
    if (-not $SigningProfile)  { $SigningProfile  = $cfg.CertificateProfileName }
    if (-not $SigntoolPath -and $cfg.SigntoolPath) { $SigntoolPath = $cfg.SigntoolPath }
}

# Best-effort check for a credential DefaultAzureCredential can use. Without one, the Trusted Signing
# dlib can't fetch the certificate and signtool falls back to the local store - which usually fails
# with a confusing "Multiple certificates were found" error rather than naming the real problem.
function Test-AzureCredentialAvailable {
    if ($env:AZURE_CLIENT_ID -and $env:AZURE_TENANT_ID -and $env:AZURE_CLIENT_SECRET) { return $true } # service principal
    if ($env:IDENTITY_ENDPOINT -or $env:MSI_ENDPOINT) { return $true }                                  # managed identity
    if (Get-Command az -ErrorAction SilentlyContinue) {
        try { & az account show 1>$null 2>$null; if ($LASTEXITCODE -eq 0) { return $true } } catch { }   # az login
    }
    return $false
}

# Set up signing once (validate config, locate signtool, write the Trusted Signing metadata file).
$script:Signtool = $null
$script:SigningMetadata = $null
if ($Sign) {
    foreach ($pair in @(
        @{ Name = '-SigningEndpoint / TRUSTED_SIGNING_ENDPOINT'; Value = $SigningEndpoint },
        @{ Name = '-SigningAccount / TRUSTED_SIGNING_ACCOUNT';   Value = $SigningAccount  },
        @{ Name = '-SigningProfile / TRUSTED_SIGNING_PROFILE';   Value = $SigningProfile  })) {
        if (-not $pair.Value) { throw "Signing requested (-Sign) but $($pair.Name) is not set." }
    }

    # The dlib is auto-restored from NuGet unless an explicit path was provided.
    if (-not $SigningDlib) { $SigningDlib = Resolve-SigningDlib }
    if (-not (Test-Path $SigningDlib)) { throw "Trusted Signing dlib not found: $SigningDlib" }

    if (-not (Test-AzureCredentialAvailable)) {
        Write-Warning ("No Azure sign-in detected. Azure Trusted Signing needs a credential: run 'az login', " +
            "or set AZURE_TENANT_ID / AZURE_CLIENT_ID / AZURE_CLIENT_SECRET. Without one, signing fails with a " +
            "misleading 'Multiple certificates were found' error. (Use -NoSign for an unsigned dev build.)")
    }

    $script:Signtool = Resolve-Signtool
    $script:SigningMetadata = Join-Path ([System.IO.Path]::GetTempPath()) "philter-trusted-signing.json"
    $metadataJson = [ordered]@{
        Endpoint               = $SigningEndpoint
        CodeSigningAccountName = $SigningAccount
        CertificateProfileName = $SigningProfile
    } | ConvertTo-Json
    # Write UTF-8 WITHOUT a BOM — the Trusted Signing dlib's JSON parser rejects a leading BOM
    # ("'0xEF' is an invalid start of a value"). Set-Content -Encoding utf8 adds a BOM in PS 5.1.
    [System.IO.File]::WriteAllText($script:SigningMetadata, $metadataJson, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Code signing enabled (Azure Trusted Signing) via $script:Signtool"
}

function Invoke-Sign {
    param([Parameter(Mandatory)][string]$Path)
    if (-not $Sign) { return }
    Write-Host "Signing $Path ..."
    # Add /debug here for detailed Trusted Signing diagnostics (auth, role, endpoint) when troubleshooting.
    & $script:Signtool sign /v /fd SHA256 /tr $TimestampUrl /td SHA256 `
        /dlib $SigningDlib /dmdf $script:SigningMetadata $Path
    if ($LASTEXITCODE -ne 0) {
        throw ("signtool failed for $Path. If the output mentions 'Multiple certificates were found' or " +
            "certificate selection, Azure Trusted Signing probably could not authenticate - run 'az login' " +
            "(or set AZURE_TENANT_ID / AZURE_CLIENT_ID / AZURE_CLIENT_SECRET) and retry, or use -NoSign for an " +
            "unsigned build.")
    }
}

# ----- Publish ----------------------------------------------------------------------------------

# Guard: the in-app update check compares versions with System.Version (Major.Minor.Build) and can
# only handle plain numeric versions — a pre-release/metadata suffix like 1.2.0-beta or 1.2.0+meta
# would make clients unable to tell whether the published build is newer. Refuse to build one.
function Assert-ComparableVersion {
    param([string]$V)
    if ($V -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
        throw ("Version '$V' is not a plain numeric version (e.g. 1.2.3). The in-app update check uses " +
               "System.Version and cannot compare pre-release/metadata suffixes such as '-beta' or " +
               "'+meta'. Set a numeric <Version> in PhilterDesktop.csproj, or pass -Version X.Y.Z.")
    }
}

# Validate an explicit -Version override up front, before any (slow) work happens.
if ($Version) { Assert-ComparableVersion $Version }

# Run the test suite first (unless -NoTest); a failure aborts the build before publishing.
if (-not $NoTest) {
    $testProj = Join-Path $repo "PhilterDesktop.Tests\PhilterDesktop.Tests.csproj"
    Write-Host "Running tests ($testProj) ..."
    dotnet test $testProj -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "Tests failed - aborting the build." }
}

$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
Write-Host "Publishing PhilterDesktop (win-x64, self-contained=$selfContained)..."
$publishArgs = @('-c', 'Release', '-r', 'win-x64', '--self-contained', $selfContained)
# Only force a version when overriding; otherwise the project's <Version> is used.
if ($Version) { $publishArgs += "-p:Version=$Version" }
dotnet publish $proj @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# Determine the version to stamp on the installer. Read it back from the built exe so it always
# matches what the app reports (About dialog / update check), unless explicitly overridden.
$exe = Join-Path $publishDir "PhilterDesktop.exe"
if (-not (Test-Path $exe)) { throw "Published exe not found at $exe" }
if (-not $Version) {
    $fileVersion = [version]((Get-Item $exe).VersionInfo.FileVersion)
    $Version = "{0}.{1}.{2}" -f $fileVersion.Major, $fileVersion.Minor, $fileVersion.Build
}
# Final guard, covering the version read back from the project's <Version>.
Assert-ComparableVersion $Version
Write-Host "Installer version: $Version"

# Sign the app exe BEFORE packaging, so the installer ships the signed binary.
Invoke-Sign -Path $exe

# ----- Compile the installer --------------------------------------------------------------------

# Locate the Inno Setup compiler (ISCC.exe).
$iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    foreach ($candidate in @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $candidate) { $iscc = $candidate; break }
    }
}
if (-not $iscc) {
    throw "ISCC.exe not found. Install Inno Setup 6.3+ from https://jrsoftware.org/isdl.php (or add ISCC to PATH)."
}

# Wipe the Output folder so it only ever contains the current build's installer.
$outputDir = Join-Path $PSScriptRoot 'Output'
if (Test-Path $outputDir) {
    Write-Host "Clearing previous installers in $outputDir ..."
    Remove-Item (Join-Path $outputDir '*') -Force -Recurse -ErrorAction SilentlyContinue
}

# Pass the TFM-derived publish dir so the .iss packages the right folder (its built-in default is a
# fixed path that goes stale whenever the target framework changes).
$isccArgs = @("/DAppVersion=$Version", "/DPublishDir=$publishDir")
if ($Sign) {
    # Register the "philtersign" sign tool with ISCC and enable the SignTool directive in the .iss
    # (/DSign). Inno then signs the installer AND the embedded uninstaller. $q (a literal quote) and
    # $f (the file being signed) are Inno tokens — single-quoted here so PowerShell leaves them alone.
    $signToolCmd = '$q' + $script:Signtool + '$q sign /v /fd SHA256 /tr ' + $TimestampUrl +
        ' /td SHA256 /dlib $q' + $SigningDlib + '$q /dmdf $q' + $script:SigningMetadata + '$q $f'
    $isccArgs += "/DSign"
    $isccArgs += "/Sphiltersign=$signToolCmd"
}
$isccArgs += (Join-Path $PSScriptRoot "PhilterDesktop.iss")

Write-Host "Compiling installer with $iscc ..."
& $iscc @isccArgs
if ($LASTEXITCODE -ne 0) { throw "ISCC failed." }

# The installer and uninstaller are signed by ISCC (via the SignTool directive) when $Sign is set.
$setupExe = Join-Path $outputDir "PhilterDesktop-Setup-$Version.exe"
if (-not (Test-Path $setupExe)) { throw "Setup .exe not found at $setupExe" }

Write-Host "Done. Setup .exe is in $outputDir."
