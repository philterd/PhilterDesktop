# MSIX packaging

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

1. Open `PhilterDesktop.slnx`.
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
