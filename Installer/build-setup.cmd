@echo off
rem ============================================================================
rem  Build the Philter Desktop setup .exe (unpackaged / Inno Setup).
rem
rem  Thin wrapper around build-setup.ps1 so it can be double-clicked or run from
rem  cmd without changing PowerShell execution policy. It publishes the app
rem  (win-x64) and compiles Installer\PhilterDesktop.iss with Inno Setup's ISCC.
rem
rem  Usage:
rem    build-setup.cmd                 (version 1.0.0, self-contained)
rem    build-setup.cmd 1.2.3           (specific version)
rem    build-setup.cmd 1.2.3 fdd       (framework-dependent: needs .NET runtime)
rem
rem  Output: Installer\Output\PhilterDesktop-Setup-<version>.exe
rem ============================================================================
setlocal

set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=1.0.0"

set "FDD="
if /I "%~2"=="fdd" set "FDD=-FrameworkDependent"
if /I "%~2"=="framework-dependent" set "FDD=-FrameworkDependent"

rem Prefer PowerShell 7 (pwsh); fall back to Windows PowerShell.
where pwsh >nul 2>nul
if %ERRORLEVEL%==0 (
  set "PS=pwsh"
) else (
  set "PS=powershell"
)

"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-setup.ps1" -Version %VERSION% %FDD%
set "RC=%ERRORLEVEL%"

if not "%RC%"=="0" (
  echo.
  echo Build FAILED with exit code %RC%.
) else (
  echo.
  echo Build succeeded. See "%~dp0Output".
)

endlocal & exit /b %RC%
