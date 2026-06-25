# Installer

Philter Desktop is distributed as a **setup `.exe`** built with [Inno Setup](https://jrsoftware.org/)
from `PhilterDesktop.iss`. It packages a `dotnet publish` (win-x64) output into a single installer for
direct download.

**Build it:**

```powershell
# Runs the tests, publishes the app, then compiles the installer with Inno Setup's ISCC.
pwsh Installer\build-setup.ps1
# Skip the test run:
pwsh Installer\build-setup.ps1 -NoTest
# Smaller build that requires the .NET 10 Desktop Runtime on the target:
pwsh Installer\build-setup.ps1 -FrameworkDependent
```

The build runs the test suite first (a failure aborts before publishing; pass `-NoTest` to skip) and
publishes the app itself (a fresh `dotnet publish`) — no separate build step is needed.

The installer **version comes from the project** — set `<Version>` in
[`PhilterDesktop.csproj`](../PhilterDesktop/PhilterDesktop.csproj) and bump it for each release; the
build reads it back from the published exe and names the output `PhilterDesktop-Setup-<version>.exe`
(so it always matches what the app's About dialog and update check report). Pass `-Version 1.2.3` only
to override it for a one-off build.

Requires **Inno Setup 6.3+** (`ISCC.exe` on PATH or in the default install location). The setup
`.exe` is written to `Installer\Output\`.

> If running `.ps1` files is blocked on your machine, invoke it as
> `pwsh -ExecutionPolicy Bypass -File Installer\build-setup.ps1`.

**What it does:**

- Installs **per-user by default** (no admin); users can choose all-users in the wizard or via
  `/ALLUSERS`. Default build is **self-contained**, so the target needs no .NET runtime.
- Bundles everything `publish` produces: the app, native PDF libraries, and the on-device PhEye
  model under `Models\`.
- Optional tasks: a desktop icon, and **start at sign-in** — which writes the *same*
  `HKCU\…\Run` value (with `--minimized`) that the in-app "Start at sign-in" toggle uses, so the two
  stay in sync. The entry is removed on uninstall.
- Leaves user data (`%LocalAppData%\PhilterDesktop\`, including the database and logs) in place on
  uninstall.

## Signing (Azure Trusted Signing)

Distributing unsigned binaries triggers a Windows SmartScreen prompt, so **signing is on by
default.** A normal build Authenticode-signs all three artifacts with **Azure Trusted Signing**
(`signtool` + the Trusted Signing dlib):

- `PhilterDesktop.exe` — signed *before* it's packaged, so the installer ships the signed binary.
- the generated **uninstaller** and the **setup `.exe`** — signed by Inno Setup during compilation
  (via its `SignTool` directive, which `build-setup.ps1` registers).

```powershell
pwsh Installer\build-setup.ps1            # signed (default)
pwsh Installer\build-setup.ps1 -NoSign    # unsigned dev build
```

Provide the Trusted Signing account details in **any** of three ways (highest precedence first): a
script **parameter**, an **environment variable**, or a local **config file**:

| Config-file key | Parameter | Env var | Example |
|-----------------|-----------|---------|---------|
| `Endpoint` | `-SigningEndpoint` | `TRUSTED_SIGNING_ENDPOINT` | `https://eus.codesigning.azure.net/` |
| `CodeSigningAccountName` | `-SigningAccount` | `TRUSTED_SIGNING_ACCOUNT` | your Trusted Signing account name |
| `CertificateProfileName` | `-SigningProfile` | `TRUSTED_SIGNING_PROFILE` | your certificate profile name |

These are the values from the Azure portal where you created your Trusted Signing account and
certificate profile. The script writes them into the metadata JSON that `signtool` needs; you don't
create that file by hand.

The **signing dlib** (`Azure.CodeSigning.Dlib.dll`) is **auto-restored** — the script downloads the
`Microsoft.Trusted.Signing.Client` NuGet package into `Installer\tools\` (cached, and gitignored) on
the first signed build, so you don't have to install or configure it. (Pin a version with
`-TrustedSigningClientVersion`, or point at an existing copy with `-SigningDlib` /
`TRUSTED_SIGNING_DLIB`, if you prefer.)

### Local config file (no env vars needed)

Copy [`signing.local.json.example`](signing.local.json.example) to **`signing.local.json`** in this
folder and fill in your values:

```json
{
  "Endpoint": "https://eus.codesigning.azure.net/",
  "CodeSigningAccountName": "my-trusted-signing-account",
  "CertificateProfileName": "my-cert-profile"
}
```

`signing.local.json` is **gitignored**, so your account details are never committed. With it in place,
just run `pwsh Installer\build-setup.ps1`.

The dlib comes from the **`Microsoft.Trusted.Signing.Client`** NuGet package. `signtool.exe` is found
on `PATH`, under the Windows SDK, or via `-SigntoolPath`. Timestamping uses
`http://timestamp.acs.microsoft.com` (override with `-TimestampUrl`).

**Azure authentication** uses `DefaultAzureCredential`: run `az login`, or set `AZURE_TENANT_ID` /
`AZURE_CLIENT_ID` / `AZURE_CLIENT_SECRET` (or use a managed identity on a build agent) before running
the script.

Example (PowerShell), set once per session:

```powershell
$env:TRUSTED_SIGNING_ENDPOINT = "https://eus.codesigning.azure.net/"
$env:TRUSTED_SIGNING_ACCOUNT  = "my-trusted-signing-account"
$env:TRUSTED_SIGNING_PROFILE  = "my-cert-profile"
$env:TRUSTED_SIGNING_DLIB     = "C:\tools\trusted-signing\Azure.CodeSigning.Dlib.dll"
az login
pwsh Installer\build-setup.ps1
```

## Notes

- **Word redaction** uses the open-source Open XML SDK — no license key or third-party component to
  supply for distributed builds.
- The app stores its data (LiteDB database, logs) under `%LocalAppData%\PhilterDesktop\`.
