# Installer

Philter Desktop is distributed as a **setup `.exe`** built with [Inno Setup](https://jrsoftware.org/)
from `PhilterDesktop.iss`. It packages a `dotnet publish` output into a single installer for direct
download.

**One installer, both architectures.** Philter Desktop ships a **native build per CPU
architecture** — `win-x64` (Intel/AMD) and `win-arm64` (Windows on ARM) — but both travel in a
**single download**, `PhilterDesktop-Setup-<version>.exe`. The installer detects the machine and
installs only the matching build, so users never pick based on their hardware. A native arm64 build
matters because on Windows-on-ARM the x64 build runs under emulation, where the native ONNX Runtime
(used by on-device name detection) fails to initialize; the arm64 build runs natively and avoids
that. The architecture-specific native libraries (ONNX Runtime, PDFium, SkiaSharp) all ship arm64
binaries, so a `win-arm64` self-contained publish cross-compiles fine on an x64 machine.

Carrying both builds costs download size. The arch-neutral payload — most of it the ~89 MB PhEye
model — is stored **once**, so the combined installer is roughly 200 MB rather than double a
single-arch build. The two Visual C++ Runtime installers add about 35 MB.

**Build it:**

```powershell
# Runs the tests, publishes both arches, then compiles the installer with Inno Setup's ISCC.
pwsh Installer\build-setup.ps1
# Publish a single arch for a faster dev loop (no installer is produced):
pwsh Installer\build-setup.ps1 -Runtime win-arm64
# Skip the test run:
pwsh Installer\build-setup.ps1 -NoTest
# Smaller build that requires the .NET 10 Desktop Runtime on the target:
pwsh Installer\build-setup.ps1 -FrameworkDependent
```

Tests and signing setup run once, the publish runs per arch, and packaging runs once at the end. The
installer needs **both** publish trees, so `-Runtime` with a single RID stops after publishing and
says so.

The build runs the test suite first (a failure aborts before publishing; pass `-NoTest` to skip) and
publishes the app itself (a fresh `dotnet publish`) — no separate build step is needed.

The installer **version comes from the project** — set `<Version>` in
[`PhilterDesktop.csproj`](../PhilterDesktop/PhilterDesktop.csproj) and bump it for each release; the
build reads it back from the published exe and names the output
`PhilterDesktop-Setup-<version>.exe` (so it always matches what the app's About dialog and update
check report). Pass `-Version 1.2.3` only to override it for a one-off build.

Requires **Inno Setup 6.3+** (`ISCC.exe` on PATH or in the default install location). The setup
`.exe` is written to `Installer\Output\`.

> If running `.ps1` files is blocked on your machine, invoke it as
> `pwsh -ExecutionPolicy Bypass -File Installer\build-setup.ps1`.

**What it does:**

- Installs **per-user by default** (no admin); users can choose all-users in the wizard or via
  `/ALLUSERS`. Default build is **self-contained**, so the target needs no .NET runtime.
- Bundles everything `publish` produces: the app, native PDF libraries, and the on-device PhEye
  model under `Models\`.
- Installs the **native build for the machine**: the arm64 files on Windows-on-ARM, the x64 files
  everywhere else, and records which under `Software\Philterd\Philter Desktop` (`InstalledArch`).
  Publish flattens the native libraries to the app root, so switching architecture overwrites them by
  name; the only files that would survive are the ones whose *name* carries the architecture
  (`Microsoft.DiaSymReader.Native.<arch>.dll`, `mscordaccore_<arch>_<arch>_<ver>.dll`), which an
  `[InstallDelete]` removes for the opposite architecture on every install.
- Installs the **Microsoft Visual C++ Runtime** when the machine needs it. ONNX Runtime (on-device
  name detection) links against it, and a clean Windows install may not have it. See
  [Visual C++ Runtime](#visual-c-runtime) below.
- Lists itself in Add/Remove Programs as just **Philter Desktop** (`UninstallDisplayName`); the
  version shows in that list's own Version column.
- One optional task: a desktop icon (ticked in the wizard; a silent install adds it only when
  `/TASKS` or `/MERGETASKS` names `desktopicon`). **Start at sign-in** isn't offered here: it's the
  app's Settings toggle, which writes `HKCU\…\Run` with `--minimized`. Uninstalling removes that entry
  however it was set.
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
under the Windows SDK for the build host's architecture, then on `PATH`, or via `-SigntoolPath`; the
dlib is then chosen to **match that signtool's architecture**. Timestamping uses
`http://timestamp.acs.microsoft.com` (override with `-TimestampUrl`).

> **If signing fails with "Multiple certificates were found":** the dlib was never loaded and
> `signtool` fell back to searching your local certificate store. Check the output for the
> **`Trusted Signing`** banner. If it is missing, signtool and the dlib are on different
> architectures (the SDK installs a **32-bit** `signtool.exe` on `PATH`, and it cannot load the
> 64-bit dlib) - the build script now prefers the SDK copy matching the host and refuses a mismatch
> up front. If the banner is present but signing still failed, it is an Azure authentication problem:
> run `az login`. **Do not** follow signtool's suggestion to add `/a` or `/sha1`; that signs the build
> with an unrelated self-signed certificate from your store instead of your publisher identity.

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

## Visual C++ Runtime

ONNX Runtime's native DLLs import the Visual C++ Runtime (`vcruntime140.dll`, `msvcp140.dll`, and
friends). Without it, on-device name detection can't load on a clean PC. The installer bundles
Microsoft's redistributables for both architectures and runs the matching one only when needed.

**At build time**, `build-setup.ps1`:

- Downloads `vc_redist.x64.exe` and `vc_redist.arm64.exe` from the versioned Microsoft URLs pinned in
  [`vcredist.json`](vcredist.json), and **fails the build** unless each matches its pinned SHA-256
  and carries a valid Microsoft signature. They are cached under `Installer\tools\vcredist\`
  (gitignored), never committed.
- Works out the oldest runtime the published DLLs accept: the newest linker version among those that
  import the runtime (a runtime must be at least as new as the toolset that built the binary;
  14.44 for ONNX Runtime 1.26). It fails if the pinned redistributable is older than that.
- Passes the redistributables and that minimum to ISCC (`/DVCRedistDir`, `/DVCRedistMinMajor`,
  `/DVCRedistMinMinor`). A direct ISCC run without them builds an installer that doesn't bundle the
  runtime.

**At install time**, [`vcredist.iss`](vcredist.iss):

- Runs at the start of the install step (`ssInstall`), before the app's files are copied. It can't
  run later: Setup asks `NeedRestart` during that step, so a restart request after it is lost.
- Reads `HKLM\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\{ARM64|x64}` (both registry views).
  If `Installed` = 1 and the version is at least the minimum, it does nothing.
- Otherwise it extracts the matching redistributable (arm64 on Windows on ARM, x64 elsewhere) and runs
  it with `/install /quiet /norestart`. An all-users or elevated install runs it directly; a per-user
  install asks for administrator approval (UAC) just for this step. `ShellExec` doesn't return exit
  codes, so the elevated run goes through `cmd`, which writes the code to a file; if that can't be
  read, the registry is checked again instead.
- Exit code 0 or 1638 (a newer runtime is already there) is success. 3010 tells Setup a restart is
  needed, so the wizard offers one at the end. (A `/VERYSILENT` install restarts on its own in that
  case unless `/NORESTART` is passed.)
- If approval is declined or the redistributable fails, Philter Desktop is still installed; the
  wizard explains that redacting with name detection will fail until the runtime is installed, and
  links to it. (Without the runtime, ONNX Runtime can't load, so those redactions fail with an error;
  the app doesn't yet detect this and fall back.) Unattended installs never show this,
  and only log it.
- A **silent per-user** install (`/VERYSILENT /CURRENTUSER`) can't ask for approval, so it skips the
  runtime and logs why. For unattended deployment to PCs that may lack the runtime, install with
  `/ALLUSERS` from an elevated prompt, or deploy the runtime separately first.

**Updating the pins:** download the new redistributable from
`https://aka.ms/vs/17/release/vc_redist.<arch>.exe`, note the versioned URL it redirects to, and record
that URL, its SHA-256, and the version in `vcredist.json`. The tests check that the pinned version is
new enough for the ONNX Runtime the project references.

Microsoft's Visual Studio license terms allow distributing the Visual C++ Redistributable with an
application that needs it.

## Notes

- **Word redaction** uses the open-source Open XML SDK — no license key or third-party component to
  supply for distributed builds.
- The app stores its data (LiteDB database, logs) under `%LocalAppData%\PhilterDesktop\`.
