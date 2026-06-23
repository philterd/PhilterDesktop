# Copyright 2026 Philterd, LLC
# Licensed under the Apache License, Version 2.0.
#
# Builds the unpackaged setup .exe for Philter Desktop:
#   1. `dotnet publish` the app (win-x64), and
#   2. compile Installer\PhilterDesktop.iss with Inno Setup's ISCC.
#
# By default the publish is self-contained (no .NET runtime prerequisite on the target machine).
# Pass -FrameworkDependent to publish a smaller build that requires the .NET 10 Desktop Runtime.
#
# Usage:
#   pwsh Installer\build-setup.ps1 -Version 1.0.0
#   pwsh Installer\build-setup.ps1 -Version 1.0.0 -FrameworkDependent

param(
    [string]$Version = "1.0.0",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $repo "PhilterDesktop\PhilterDesktop.csproj"

$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
Write-Host "Publishing PhilterDesktop (win-x64, self-contained=$selfContained)..."
dotnet publish $proj -c Release -r win-x64 --self-contained $selfContained
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

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

Write-Host "Compiling installer with $iscc ..."
& $iscc "/DAppVersion=$Version" (Join-Path $PSScriptRoot "PhilterDesktop.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC failed." }

Write-Host "Done. Setup .exe is in $(Join-Path $PSScriptRoot 'Output')."
