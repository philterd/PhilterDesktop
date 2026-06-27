# Copyright 2026 Philterd, LLC
# Licensed under the Apache License, Version 2.0.
#
# Builds the offline Philter Desktop user-guide PDF from the same markdown that powers the online
# documentation (docs\), and writes it to Installer\dist\PhilterDesktop-User-Guide.pdf. The PDF is
# then bundled into the installer by build-setup.ps1 (see philterd-website issue #469).
#
# The markdown under docs\docs\ stays the single source of truth: the online docs build from
# docs\mkdocs.yml is unchanged. This script builds from docs\mkdocs.pdf.yml, an overlay that
# inherits mkdocs.yml and only adds the mkdocs-to-pdf plugin.
#
# Prerequisites:
#   - Python 3 (the `py -3` launcher, or `python` / `python3` on PATH). A local virtual environment
#     is created and cached under Installer\tools\docs-venv and the docs requirements are installed
#     into it, so this does not touch any global Python install.
#   - WeasyPrint's native libraries. mkdocs-to-pdf renders with WeasyPrint, which needs GTK / Pango /
#     cairo. On Windows, install them once per the WeasyPrint docs:
#       https://doc.courtbouillon.org/weasyprint/stable/first_steps.html#windows
#     (Linux/macOS usually have these already or via the system package manager.)
#
# Usage:
#   pwsh Installer\build-user-guide.ps1                       # -> Installer\dist\PhilterDesktop-User-Guide.pdf
#   pwsh Installer\build-user-guide.ps1 -OutputPath C:\tmp\guide.pdf
#   pwsh Installer\build-user-guide.ps1 -Recreate            # rebuild the cached venv from scratch

param(
    [string]$OutputPath,
    [switch]$Recreate
)

$ErrorActionPreference = 'Stop'

$repo    = Split-Path -Parent $PSScriptRoot
$docsDir = Join-Path $repo 'docs'
$config  = Join-Path $docsDir 'mkdocs.pdf.yml'
$reqs    = Join-Path $docsDir 'requirements-pdf.txt'

if (-not $OutputPath) { $OutputPath = Join-Path $PSScriptRoot 'dist\PhilterDesktop-User-Guide.pdf' }
foreach ($p in @($config, $reqs)) {
    if (-not (Test-Path $p)) { throw "Expected docs file not found: $p" }
}

# Locate a base Python interpreter to create the venv with.
function Resolve-Python {
    $py = Get-Command py -ErrorAction SilentlyContinue
    if ($py) { return @{ Exe = $py.Source; Args = @('-3') } }
    foreach ($name in @('python', 'python3')) {
        $cmd = Get-Command $name -ErrorAction SilentlyContinue
        if ($cmd) { return @{ Exe = $cmd.Source; Args = @() } }
    }
    throw ("Python 3 not found. Install it from https://www.python.org/downloads/ (or the Microsoft " +
           "Store) so the user-guide PDF can be built, or pass -NoDocs to build-setup.ps1 to skip it.")
}

# Create (or reuse) a cached virtual environment with the docs requirements installed.
$venv = Join-Path $PSScriptRoot 'tools\docs-venv'
if ($Recreate -and (Test-Path $venv)) { Remove-Item $venv -Recurse -Force }
$venvPython = Join-Path $venv 'Scripts\python.exe'   # Windows layout
if (-not (Test-Path $venvPython)) { $venvPython = Join-Path $venv 'bin\python' }  # POSIX fallback

if (-not (Test-Path $venvPython)) {
    $base = Resolve-Python
    Write-Host "Creating docs virtual environment in $venv ..."
    & $base.Exe @($base.Args + @('-m', 'venv', $venv))
    if ($LASTEXITCODE -ne 0) { throw "Failed to create the Python virtual environment." }
    $venvPython = Join-Path $venv 'Scripts\python.exe'
    if (-not (Test-Path $venvPython)) { $venvPython = Join-Path $venv 'bin\python' }
    & $venvPython -m pip install --quiet --upgrade pip
}

Write-Host "Installing documentation requirements ($reqs) ..."
& $venvPython -m pip install --quiet -r $reqs
if ($LASTEXITCODE -ne 0) { throw "pip install of the docs requirements failed." }

# Build the PDF. mkdocs-to-pdf only renders the PDF when ENABLE_PDF_EXPORT is set, so the overlay
# config is otherwise a normal (fast) site build.
$siteDir = Join-Path ([System.IO.Path]::GetTempPath()) 'philterdesktop-userguide-site'
if (Test-Path $siteDir) { Remove-Item $siteDir -Recurse -Force }

Write-Host "Building the user-guide PDF from $config ..."
$env:ENABLE_PDF_EXPORT = '1'
try {
    & $venvPython -m mkdocs build -f $config -d $siteDir
    if ($LASTEXITCODE -ne 0) { throw "mkdocs build failed." }
} catch {
    throw ("Building the user-guide PDF failed: $($_.Exception.Message)`n" +
           "If the error mentions a missing library such as 'libgobject', 'pango', or 'cairo', " +
           "WeasyPrint's native dependencies are not installed. See " +
           "https://doc.courtbouillon.org/weasyprint/stable/first_steps.html#windows " +
           "(or pass -NoDocs to build-setup.ps1 to skip the PDF).")
} finally {
    Remove-Item Env:\ENABLE_PDF_EXPORT -ErrorAction SilentlyContinue
}

$builtPdf = Join-Path $siteDir 'PhilterDesktop-User-Guide.pdf'
if (-not (Test-Path $builtPdf)) { throw "mkdocs reported success but no PDF was produced at $builtPdf." }

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
Copy-Item $builtPdf $OutputPath -Force
Remove-Item $siteDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "User-guide PDF written to $OutputPath"
