# Copyright 2026 Philterd, LLC
# Licensed under the Apache License, Version 2.0.
#
# Builds the offline Philter Desktop user-guide PDF from the same markdown that powers the online
# documentation (docs\), and writes it to Installer\dist\PhilterDesktop-User-Guide.pdf. The PDF is
# then bundled into the installer by build-setup.ps1 (see philterd-website issue #469).
#
# The markdown under docs\docs\ stays the single source of truth: the online docs build from
# docs\mkdocs.yml is unchanged. This script builds from docs\mkdocs.pdf.yml, an overlay that
# inherits mkdocs.yml and adds the print-site plugin, which renders all pages into one combined
# HTML page (print_page\index.html). That single page is then converted to PDF with headless
# Microsoft Edge (or Chrome), so there are NO native PDF libraries to install.
#
# Prerequisites:
#   - Python 3 (the `py -3` launcher, or `python` / `python3` on PATH). A local virtual environment
#     is created and cached under Installer\tools\docs-venv and the docs requirements are installed
#     into it, so this does not touch any global Python install.
#   - Microsoft Edge (ships with Windows 10/11) or Google Chrome, used in headless mode to render
#     the PDF. Override the browser with -BrowserPath if it is installed somewhere non-standard.
#
# Usage:
#   pwsh Installer\build-user-guide.ps1                       # -> Installer\dist\PhilterDesktop-User-Guide.pdf
#   pwsh Installer\build-user-guide.ps1 -OutputPath C:\tmp\guide.pdf
#   pwsh Installer\build-user-guide.ps1 -BrowserPath "C:\Path\To\msedge.exe"
#   pwsh Installer\build-user-guide.ps1 -Recreate            # rebuild the cached venv from scratch

param(
    [string]$OutputPath,
    [string]$BrowserPath,
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

# Locate headless Microsoft Edge (preferred, ships with Windows) or Google Chrome.
function Resolve-Browser {
    if ($BrowserPath) {
        if (Test-Path $BrowserPath) { return $BrowserPath }
        throw "Browser not found at -BrowserPath '$BrowserPath'."
    }
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft\Edge\Application\msedge.exe'),
        (Join-Path $env:ProgramFiles          'Microsoft\Edge\Application\msedge.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Google\Chrome\Application\chrome.exe'),
        (Join-Path $env:ProgramFiles          'Google\Chrome\Application\chrome.exe'),
        (Join-Path $env:LOCALAPPDATA          'Google\Chrome\Application\chrome.exe')
    )
    foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return $c } }
    foreach ($n in @('msedge.exe', 'chrome.exe', 'msedge', 'chrome')) {
        $cmd = Get-Command $n -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
    }
    throw ("Microsoft Edge or Google Chrome is required to render the PDF but neither was found. " +
           "Install Edge/Chrome, pass -BrowserPath, or pass -NoDocs to build-setup.ps1 to skip the PDF.")
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

# Build the combined HTML page (print_page\index.html) from the markdown.
$siteDir = Join-Path ([System.IO.Path]::GetTempPath()) 'philterdesktop-userguide-site'
if (Test-Path $siteDir) { Remove-Item $siteDir -Recurse -Force }

Write-Host "Building the documentation site (print page) from $config ..."
& $venvPython -m mkdocs build -f $config -d $siteDir
if ($LASTEXITCODE -ne 0) { throw "mkdocs build failed." }

$printPage = Join-Path $siteDir 'print_page\index.html'
if (-not (Test-Path $printPage)) { throw "Combined print page not found at $printPage (is the print-site plugin enabled in $config?)." }

# Render the single HTML page to PDF with headless Edge/Chrome. No native libraries required.
$browser = Resolve-Browser
Write-Host "Rendering the PDF with $browser ..."
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
if (Test-Path $OutputPath) { Remove-Item $OutputPath -Force }

# A unique throwaway profile avoids clashing with (or handing off to) a running Edge/Chrome instance.
$profileDir = Join-Path ([System.IO.Path]::GetTempPath()) ("philterdesktop-pdf-" + [guid]::NewGuid().ToString('N'))
$fileUrl = 'file:///' + ($printPage -replace '\\', '/')

$browserArgs = @(
    '--headless=new', '--disable-gpu', '--no-pdf-header-footer',
    '--no-first-run', '--no-default-browser-check',
    '--disable-sync', '--disable-extensions', '--disable-background-networking',
    ('--user-data-dir=' + $profileDir),
    ('--print-to-pdf=' + $OutputPath),
    $fileUrl
)

# On Windows, Edge/Chrome usually returns from the launched command BEFORE the headless render has
# finished, writing the PDF from a detached process a moment later. So launch it and then wait for
# the PDF to appear and stop growing, rather than trusting the call to block.
& $browser @browserArgs 2>$null

$deadline = (Get-Date).AddSeconds(120)
$lastSize = -1
$ready = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 750
    if (Test-Path $OutputPath) {
        $size = (Get-Item $OutputPath).Length
        if ($size -gt 0 -and $size -eq $lastSize) { $ready = $true; break }  # stable, write finished
        $lastSize = $size
    }
}

Remove-Item $profileDir -Recurse -Force -ErrorAction SilentlyContinue
if (-not $ready) {
    throw ("The browser did not produce a PDF at $OutputPath within the timeout. Ensure Edge or Chrome " +
           "is installed and supports headless --print-to-pdf, or pass -BrowserPath.")
}

Remove-Item $siteDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "User-guide PDF written to $OutputPath ($([math]::Round((Get-Item $OutputPath).Length/1KB)) KB)"
