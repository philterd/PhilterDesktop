# Copyright 2026 Philterd, LLC
# Licensed under the Apache License, Version 2.0.
#
# Builds the setup .exe for Philter Desktop:
#   1. `dotnet publish` the app for each target RID (win-x64 and win-arm64),
#   2. (optional) Authenticode-sign PhilterDesktop.exe with Azure Trusted Signing,
#   3. compile Installer\PhilterDesktop.iss with Inno Setup's ISCC, and
#   4. (optional) sign the generated setup .exe.
#
# The result is ONE installer covering both architectures: PhilterDesktop-Setup-<version>.exe. It
# carries both publish trees and installs the native build matching the machine, so users never pick
# based on their hardware. The win-arm64 build lets Windows-on-ARM run natively instead of under x64
# emulation (where the native ONNX Runtime fails to initialize); it cross-compiles fine on an x64
# build machine.
#
# Pass -Runtime to publish just one RID (e.g. -Runtime win-arm64) for a faster dev iteration. That
# skips the installer, which needs both trees; the message says so.
#
# The version comes from the project's <Version> (PhilterDesktop.csproj) - bump it there and the
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
# Microsoft.Trusted.Signing.Client NuGet package into Installer\tools\ - no need to configure it.
# (Override with -SigningDlib / TRUSTED_SIGNING_DLIB if you want a specific copy.)
# Copy signing.local.json.example to signing.local.json (gitignored) to use the file. Azure auth
# uses DefaultAzureCredential - sign in first (az login) or set AZURE_TENANT_ID / AZURE_CLIENT_ID /
# AZURE_CLIENT_SECRET (or use a managed identity on a build agent).
#
# Usage:
#   pwsh Installer\build-setup.ps1                       # version from csproj, SIGNED, one installer
#   pwsh Installer\build-setup.ps1 -Runtime win-arm64    # publish only arm64 (no installer)
#   pwsh Installer\build-setup.ps1 -Runtime win-x64      # publish only x64 (no installer)
#   pwsh Installer\build-setup.ps1 -Version 1.2.3        # override the version
#   pwsh Installer\build-setup.ps1 -FrameworkDependent
#   pwsh Installer\build-setup.ps1 -NoTest               # skip the test run (tests run by default)
#   pwsh Installer\build-setup.ps1 -NoSign               # unsigned dev build

param(
    [string]$Version,
    # RIDs to publish. Defaults to both, which is what the single multi-arch installer needs. Pass
    # one RID to publish only that architecture (no installer is produced in that case).
    [ValidateSet('win-x64', 'win-arm64')]
    [string[]]$Runtime = @('win-x64', 'win-arm64'),
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
# Per-RID paths and Inno Setup architecture identifiers are computed inside the build loop below.

# signtool.exe and the Trusted Signing dlib run on the BUILD MACHINE, so they must match the host
# architecture (independent of the target $Runtime). Cross-compiling win-arm64 on an x64 host still
# signs with the x64 tools; building on an arm64 host needs the arm64 tools.
$hostArch = switch ($env:PROCESSOR_ARCHITECTURE) {
    'ARM64' { 'arm64' }
    default { 'x64' }  # AMD64 (and x86 WOW64, where the x64 tools still work)
}

# ----- Code-signing helpers ---------------------------------------------------------------------

# Reads a PE file's machine type ('x86', 'x64', 'arm64'), used to keep signtool and the dlib on the
# same architecture. Returns 'unknown' rather than throwing, so a surprise never blocks a build.
function Get-PeMachine {
    param([Parameter(Mandatory)][string]$Path)
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        try {
            $reader = New-Object System.IO.BinaryReader($stream)
            $stream.Position = 0x3C
            $stream.Position = $reader.ReadInt32() + 4
            switch ($reader.ReadUInt16()) {
                0x8664  { 'x64' }
                0xAA64  { 'arm64' }
                0x014C  { 'x86' }
                default { 'unknown' }
            }
        }
        finally { $stream.Dispose() }
    }
    catch { 'unknown' }
}

# signtool must match the Trusted Signing dlib's architecture. A 32-bit signtool cannot load the
# 64-bit dlib: it ignores /dlib WITHOUT reporting anything, falls back to searching the local
# certificate store, and dies with "Multiple certificates were found" - which reads like an Azure
# auth failure but is not one. The Windows SDK puts an x86 signtool on PATH by default, so the SDK
# copy for the build host is preferred over PATH.
function Resolve-Signtool {
    if ($SigntoolPath -and (Test-Path $SigntoolPath)) { return $SigntoolPath }
    foreach ($arch in @($hostArch, 'x64') | Select-Object -Unique) {
        $found = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\$arch\signtool.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    $cmd = (Get-Command signtool.exe -ErrorAction SilentlyContinue).Source
    if ($cmd) { return $cmd }
    throw "signtool.exe not found. Install the Windows SDK, add it to PATH, or pass -SigntoolPath."
}

# Resolves Azure.CodeSigning.Dlib.dll, auto-restoring the Microsoft.Trusted.Signing.Client NuGet
# package into Installer\tools\ (cached between builds) if it isn't already present.
function Resolve-SigningDlib {
    $pkg = 'microsoft.trusted.signing.client'
    $toolsRoot = Join-Path $PSScriptRoot 'tools\trusted-signing'

    # The dlib is loaded INTO signtool, so it must match the signtool binary's architecture, not just
    # the host's (an x86 signtool cannot load the x64 dlib). $script:ToolArch is set from the resolved
    # signtool; fall back to x64, which the package always contains.
    function Select-DlibUnder([string]$Root) {
        foreach ($arch in @($script:ToolArch, 'x64') | Select-Object -Unique) {
            $hit = Get-ChildItem (Join-Path $Root "bin\$arch\Azure.CodeSigning.Dlib.dll") -ErrorAction SilentlyContinue |
                Select-Object -First 1
            if ($hit) { return $hit.FullName }
        }
        return $null
    }

    # Reuse a previously restored copy unless a specific version was requested.
    if (-not $TrustedSigningClientVersion) {
        $cachedVersionDir = Get-ChildItem $toolsRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending | Select-Object -First 1
        if ($cachedVersionDir) {
            $cached = Select-DlibUnder $cachedVersionDir.FullName
            if ($cached) { return $cached }
        }
    }

    $version = $TrustedSigningClientVersion
    if (-not $version) {
        $index = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$pkg/index.json"
        $version = $index.versions | Where-Object { $_ -notmatch '-' } | Select-Object -Last 1
        if (-not $version) { throw "Could not determine the latest $pkg version from nuget.org." }
    }

    $dest = Join-Path $toolsRoot $version
    if (-not (Select-DlibUnder $dest)) {
        Write-Host "Restoring $pkg $version into $dest ..."
        if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        $nupkg = Join-Path ([System.IO.Path]::GetTempPath()) "$pkg.$version.nupkg"
        Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/$pkg/$version/$pkg.$version.nupkg" -OutFile $nupkg
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($nupkg, $dest)
        Remove-Item $nupkg -ErrorAction SilentlyContinue
    }
    $dll = Select-DlibUnder $dest
    if (-not $dll) { throw "Azure.CodeSigning.Dlib.dll not found after restoring $pkg $version." }
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
$script:ToolArch = $null
$script:SigningMetadata = $null
if ($Sign) {
    foreach ($pair in @(
        @{ Name = '-SigningEndpoint / TRUSTED_SIGNING_ENDPOINT'; Value = $SigningEndpoint },
        @{ Name = '-SigningAccount / TRUSTED_SIGNING_ACCOUNT';   Value = $SigningAccount  },
        @{ Name = '-SigningProfile / TRUSTED_SIGNING_PROFILE';   Value = $SigningProfile  })) {
        if (-not $pair.Value) { throw "Signing requested (-Sign) but $($pair.Name) is not set." }
    }

    # Resolve signtool FIRST: its architecture decides which dlib to restore, since the dlib is loaded
    # into signtool's own process.
    $script:Signtool = Resolve-Signtool
    $script:ToolArch = Get-PeMachine $script:Signtool
    if ($script:ToolArch -eq 'unknown') { $script:ToolArch = $hostArch }

    # The dlib is auto-restored from NuGet unless an explicit path was provided.
    if (-not $SigningDlib) { $SigningDlib = Resolve-SigningDlib }
    if (-not (Test-Path $SigningDlib)) { throw "Trusted Signing dlib not found: $SigningDlib" }

    # Last line of defence, and the one that matters when -SigntoolPath / -SigningDlib are passed by
    # hand. Without it the mismatch surfaces only as signtool's misleading certificate-selection error.
    $dlibArch = Get-PeMachine $SigningDlib
    if ($dlibArch -ne 'unknown' -and $dlibArch -ne $script:ToolArch) {
        throw ("Architecture mismatch: signtool is $($script:ToolArch) ($script:Signtool) but the Trusted " +
            "Signing dlib is $dlibArch ($SigningDlib). signtool loads the dlib into its own process, so a " +
            "mismatch makes it silently ignore /dlib, fall back to the local certificate store, and fail " +
            "with 'Multiple certificates were found'. Use the $dlibArch signtool (or pass a $($script:ToolArch) " +
            "dlib with -SigningDlib).")
    }

    if (-not (Test-AzureCredentialAvailable)) {
        Write-Warning ("No Azure sign-in detected. Azure Trusted Signing needs a credential: run 'az login', " +
            "or set AZURE_TENANT_ID / AZURE_CLIENT_ID / AZURE_CLIENT_SECRET. Without one, signing fails with a " +
            "misleading 'Multiple certificates were found' error. (Use -NoSign for an unsigned dev build.)")
    }

    $script:SigningMetadata = Join-Path ([System.IO.Path]::GetTempPath()) "philter-trusted-signing.json"
    $metadataJson = [ordered]@{
        Endpoint               = $SigningEndpoint
        CodeSigningAccountName = $SigningAccount
        CertificateProfileName = $SigningProfile
    } | ConvertTo-Json
    # Write UTF-8 WITHOUT a BOM - the Trusted Signing dlib's JSON parser rejects a leading BOM
    # ("'0xEF' is an invalid start of a value"). Set-Content -Encoding utf8 adds a BOM in PS 5.1.
    [System.IO.File]::WriteAllText($script:SigningMetadata, $metadataJson, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Code signing enabled (Azure Trusted Signing) via $script:Signtool [$script:ToolArch]"
}

function Invoke-Sign {
    param([Parameter(Mandatory)][string]$Path)
    if (-not $Sign) { return }
    Write-Host "Signing $Path ..."
    # Add /debug here for detailed Trusted Signing diagnostics (auth, role, endpoint) when troubleshooting.
    & $script:Signtool sign /v /fd SHA256 /tr $TimestampUrl /td SHA256 `
        /dlib $SigningDlib /dmdf $script:SigningMetadata $Path
    if ($LASTEXITCODE -ne 0) {
        throw ("signtool failed for $Path. If the output mentions 'Multiple certificates were found', the dlib " +
            "was never used and signtool fell back to the local certificate store: do NOT retry with /a or " +
            "/sha1 (that signs with an unrelated local certificate). Look for the 'Trusted Signing' banner in " +
            "the output above - if it is missing the dlib did not load, and if it is present but signing " +
            "failed, Azure could not authenticate, so run 'az login' (or set AZURE_TENANT_ID / AZURE_CLIENT_ID " +
            "/ AZURE_CLIENT_SECRET) and retry. Use -NoSign for an unsigned build.")
    }
}

# ----- Publish ----------------------------------------------------------------------------------

# Guard: the in-app update check compares versions with System.Version (Major.Minor.Build) and can
# only handle plain numeric versions - a pre-release/metadata suffix like 1.2.0-beta or 1.2.0+meta
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

# ----- Compile the installer --------------------------------------------------------------------

# Locate the Inno Setup compiler (ISCC.exe) once, up front, so a missing compiler fails fast before
# any (slow) publish work.
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

$outputDir = Join-Path $PSScriptRoot 'Output'
$publishDirs = @{}

# Publish once per requested RID (both arches by default). Tests and signing setup already ran once
# above; each iteration publishes, verifies the model/EULA, and signs the app exe. The installer is
# compiled once, after the loop, from both publish trees.
foreach ($rid in $Runtime) {
    # Short arch tag, used as the key for the publish dir handed to ISCC.
    $archLabel  = if ($rid -eq 'win-arm64') { 'arm64' } else { 'x64' }
    $publishDir = Join-Path $repo "PhilterDesktop\bin\Release\$tfm\$rid\publish"

    Write-Host "Publishing PhilterDesktop ($rid, self-contained=$selfContained)..."
    $publishArgs = @('-c', 'Release', '-r', $rid, '--self-contained', $selfContained)
    # Only force a version when overriding; otherwise the project's <Version> is used.
    if ($Version) { $publishArgs += "-p:Version=$Version" }
    dotnet publish $proj @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid." }

    # Read the version back from the built exe so it always matches what the app reports (About dialog /
    # update check), unless explicitly overridden. Both arches build from the same <Version>, so once
    # $Version is set (below) it is reused for the remaining RIDs.
    $exe = Join-Path $publishDir "PhilterDesktop.exe"
    if (-not (Test-Path $exe)) { throw "Published exe not found at $exe" }

    # The bundled on-device model is the ONLY thing that redacts person names, so an installer without it
    # would silently ship names unredacted (only a per-run warning is shown). The Release publish downloads
    # it; verify it actually landed in the publish output before packaging, and fail loudly if not.
    $modelDir = Join-Path $publishDir "Models\ph-eye-pii-en-xsmall"
    foreach ($modelFile in @('model.onnx', 'gliner_config.json', 'spm.model')) {
        $modelPath = Join-Path $modelDir $modelFile
        if (-not (Test-Path $modelPath)) {
            throw "PhEye name-detection model file missing from the publish output: $modelPath. The installer must bundle the model or person names ship unredacted. Re-run the Release publish (network required to download it)."
        }
    }
    Write-Host "Verified PhEye name-detection model is present in the publish output."

    # Refresh the EULA the app shows from the live copy on philterd.ai, overwriting the checked-in snapshot in
    # the publish output. A network failure is non-fatal: the bundled snapshot ships instead (the app reads
    # whichever philterd-eula.txt is next to the exe).
    $eulaUrl = 'https://philterd.ai/philterd-eula.txt'
    $eulaPath = Join-Path $publishDir 'philterd-eula.txt'
    try {
        Invoke-WebRequest -Uri $eulaUrl -OutFile $eulaPath -UseBasicParsing -TimeoutSec 30
        Write-Host "Downloaded the current EULA from $eulaUrl."
    }
    catch {
        Write-Warning "Could not download the EULA from $eulaUrl ($($_.Exception.Message)); shipping the bundled snapshot."
    }
    if (-not (Test-Path $eulaPath)) {
        throw "No EULA file in the publish output ($eulaPath) - the app would show only a fallback pointer. Ensure Resources\philterd-eula.txt is present, or fix the download."
    }

    if (-not $Version) {
        $fileVersion = [version]((Get-Item $exe).VersionInfo.FileVersion)
        $Version = "{0}.{1}.{2}" -f $fileVersion.Major, $fileVersion.Minor, $fileVersion.Build
    }
    # Final guard, covering the version read back from the project's <Version>.
    Assert-ComparableVersion $Version
    Write-Host "Installer version: $Version ($archLabel)"

    # Sign the app exe BEFORE packaging, so the installer ships the signed binary.
    Invoke-Sign -Path $exe

    $publishDirs[$archLabel] = $publishDir
    Write-Host "Published $archLabel to $publishDir"
}

# The single installer carries both architectures, so it can only be built when both were published.
# A single-RID run is a dev shortcut; say plainly that it stops after the publish.
if (-not ($publishDirs.ContainsKey('x64') -and $publishDirs.ContainsKey('arm64'))) {
    Write-Host ""
    Write-Warning ("Only $($Runtime -join ', ') was published, so no installer was built: the single " +
        "installer packages both win-x64 and win-arm64. Re-run without -Runtime to build it.")
    return
}

# Clear previous installers (any version) so Output holds only the one just built.
if (Test-Path $outputDir) {
    Write-Host "Clearing previous installers in $outputDir ..."
    Remove-Item (Join-Path $outputDir "PhilterDesktop-Setup-*.exe") -Force -ErrorAction SilentlyContinue
}

# Pass both TFM/RID-derived publish dirs so the .iss packages the right folders (its built-in defaults
# are fixed paths that go stale whenever the target framework changes).
$isccArgs = @(
    "/DAppVersion=$Version",
    "/DPublishDirX64=$($publishDirs['x64'])",
    "/DPublishDirArm64=$($publishDirs['arm64'])")
if ($Sign) {
    # Register the "philtersign" sign tool with ISCC and enable the SignTool directive in the .iss
    # (/DSign). Inno then signs the installer AND the embedded uninstaller. $q (a literal quote) and
    # $f (the file being signed) are Inno tokens - single-quoted here so PowerShell leaves them alone.
    $signToolCmd = '$q' + $script:Signtool + '$q sign /v /fd SHA256 /tr ' + $TimestampUrl +
        ' /td SHA256 /dlib $q' + $SigningDlib + '$q /dmdf $q' + $script:SigningMetadata + '$q $f'
    $isccArgs += "/DSign"
    $isccArgs += "/Sphiltersign=$signToolCmd"
}
$isccArgs += (Join-Path $PSScriptRoot "PhilterDesktop.iss")

Write-Host "Compiling the multi-arch installer with $iscc ..."
& $iscc @isccArgs
if ($LASTEXITCODE -ne 0) { throw "ISCC failed." }

# The installer and uninstaller are signed by ISCC (via the SignTool directive) when $Sign is set.
$setupExe = Join-Path $outputDir "PhilterDesktop-Setup-$Version.exe"
if (-not (Test-Path $setupExe)) { throw "Setup .exe not found at $setupExe" }

Write-Host ""
Write-Host "Done. Built $([System.IO.Path]::GetFileName($setupExe)) ($([math]::Round((Get-Item $setupExe).Length / 1MB)) MB, x64 + arm64) in $outputDir"
