# Installers

Philter Desktop can be distributed two ways:

- **MSIX** (`PhilterDesktop.Package.wapproj`) — clean install/uninstall and Store-style updates, but
  must be **signed** and is more friction to sideload. Documented below.
- **Setup .exe** (`PhilterDesktop.iss`, Inno Setup) — a traditional installer for the **unpackaged**
  build; easiest for direct download. See [Setup .exe (Inno Setup)](#setup-exe-inno-setup).

---

## Setup .exe (Inno Setup)

`PhilterDesktop.iss` packages a `dotnet publish` (win-x64) output into a single setup executable —
a good option when you're not code-signing an MSIX or need non-Store / scripted deployment.

**Build it:**

```powershell
# Publishes the app, then compiles the installer with Inno Setup's ISCC.
pwsh Installer\build-setup.ps1 -Version 1.0.0
# Smaller build that requires the .NET 10 Desktop Runtime on the target:
pwsh Installer\build-setup.ps1 -Version 1.0.0 -FrameworkDependent
```

Or use the double-clickable wrapper (no PowerShell execution-policy setup needed):

```bat
Installer\build-setup.cmd                 :: version 1.0.0, self-contained
Installer\build-setup.cmd 1.2.3           :: specific version
Installer\build-setup.cmd 1.2.3 fdd       :: framework-dependent (needs .NET runtime)
```

Requires **Inno Setup 6.3+** (`ISCC.exe` on PATH or in the default install location). The setup
`.exe` is written to `Installer\Output\`.

**What it does:**

- Installs **per-user by default** (no admin); users can choose all-users in the wizard or via
  `/ALLUSERS`. Default build is **self-contained**, so the target needs no .NET runtime.
- Bundles everything `publish` produces: the app, native PDF libraries, the on-device PhEye model
  under `Models\`, and `xceed-license.json` if present at build time.
- Optional tasks: a desktop icon, and **start at sign-in** — which writes the *same*
  `HKCU\…\Run` value (with `--minimized`) that the in-app "Start at sign-in" toggle uses, so the two
  stay in sync. The entry is removed on uninstall.
- Leaves user data (`%LocalAppData%\PhilterDesktop\`, including the database and logs) in place on
  uninstall.

> Like the MSIX, distributing the `.exe` without a code-signing certificate will trigger a Windows
> SmartScreen prompt; sign it for a smooth install experience.

---

## MSIX packaging

`PhilterDesktop.Package.wapproj` is a Windows Application Packaging Project that produces an
MSIX installer for Philter Desktop. It packages the `PhilterDesktop` app (the full-trust Win32
WinForms executable) with the manifest and visual assets here.

> **Note:** a `.wapproj` is built by **MSBuild / Visual Studio**, not the `dotnet` CLI. The
> `dotnet` build and CI build the app and tests directly; the package is built separately.

## Contents

- `Package.appxmanifest` — package identity, app entry point, capabilities, and visual elements.
  Update **Version** (`x.y.z.0`) for each release, and set **Publisher** to match your signing
  certificate's subject (e.g. `CN=Philterd, LLC`).
- `Images/` — tile/logo/splash PNGs. These are **placeholders** (a "P" on the accent color);
  replace them with real artwork at the same sizes.

## Build in Visual Studio (recommended)

> Open **`PhilterDesktop.sln`** (not `PhilterDesktop.slnx`). The packaging project is a
> `.wapproj`, which the `.slnx` solution format does not support — it's only in the classic
> `.sln`. (`PhilterDesktop.slnx` contains just the app + tests for `dotnet`/CLI use.)

1. Open `PhilterDesktop.sln`.
2. Set **PhilterDesktop.Package** as the startup project (or right-click it).
3. **Publish → Create App Packages…** and follow the wizard. It builds the MSIX and can create
   or select a signing certificate.

## Build from the command line (MSBuild)

From a *Developer Command Prompt for Visual Studio* (so `msbuild` and the packaging targets are
on the path):

```cmd
msbuild Installer\PhilterDesktop.Package.wapproj ^
  /p:Configuration=Release /p:Platform=x64 ^
  /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxBundle=Never
```

The resulting `.msix` is written under `Installer\AppPackages\`.

## Signing (required to install)

MSIX cannot be installed unless it is signed by a certificate the target machine trusts.
Signing is **not** configured here (`AppxPackageSigningEnabled=False`).

- **Testing** — create a self-signed cert whose subject matches the manifest `Publisher`, then
  sign and trust it:
  ```powershell
  New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=Philterd, LLC" `
    -CertStoreLocation "Cert:\CurrentUser\My"
  # export to a .pfx, then:
  signtool sign /fd SHA256 /a /f philterd.pfx /p <password> `
    Installer\AppPackages\...\PhilterDesktop.Package_*.msix
  ```
  Install the cert into **Local Machine → Trusted People** (or Trusted Root) before installing
  the MSIX.
- **Distribution** — sign with a real code-signing certificate (or Azure Trusted Signing), or
  publish through the Microsoft Store (which signs for you). To sign during the build instead,
  set `AppxPackageSigningEnabled=True` and provide the certificate properties in the wapproj.

## App-specific notes

- **Data**: the app stores its LiteDB under `LocalApplicationData`, which MSIX redirects to the
  package's per-user store — no code change needed.
- **Word redaction / Xceed license**: a packaged `xceed-license.json` (in the app output) is
  readable at runtime, or set the `XCEED_LICENSE_KEY` environment variable. Decide how the key is
  supplied for distributed builds (it is git-ignored and not committed).
