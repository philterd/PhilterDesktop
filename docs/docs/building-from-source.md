# Building from Source

Philter Desktop's source code is open source under the **Apache License, Version 2.0**, and is
published at [github.com/philterd/PhilterDesktop](https://github.com/philterd/PhilterDesktop). Anyone
may read it, modify it, and build their own copy.

This page is for developers and IT staff who want to do that. **If you just want to use Philter
Desktop, you do not need any of this**: download the official, signed installer and follow
[Getting Started](getting-started.md).

!!! note "How your own build differs from the official one"
    Building the code yourself gives you the same application, but not the same product. Your build
    is **unsigned**, so Windows SmartScreen warns the first time you run its installer; it is **not
    covered by a support subscription**; and the ***Philter*** name and logo are trademarks of
    Philterd, so the Apache license does not permit redistributing your build under that brand. See
    [Licensing & Support](licensing.md).

## What you need

- **Windows 10 or 11**, 64-bit (Intel/AMD or ARM).
- The **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)** or later.
- **Visual Studio 2022** (17.12 or later) is optional; the `dotnet` command line is enough.
- To build the setup installer as well: **[Inno Setup](https://jrsoftware.org/isinfo.php) 6.3 or
  later**, with `ISCC.exe` on your `PATH` or in its default install location.

The redaction engine, [Phileas](https://github.com/philterd/phileas-dotnet), is consumed as the
`Philterd.Phileas` NuGet package, so there is nothing else to clone or build first.

## Getting the code and building it

```bash
git clone https://github.com/philterd/PhilterDesktop
cd PhilterDesktop
dotnet build PhilterDesktop.slnx
```

`PhilterDesktop.slnx` contains two projects:

- **PhilterDesktop**: the WinForms application (user interface, data access, redaction, and the
  policy editor).
- **PhilterDesktop.Tests**: the xUnit test suite.

Microsoft Word (`.docx`) redaction uses the open-source
[Open XML SDK](https://github.com/dotnet/Open-XML-SDK), so there is no license key and no
third-party component to obtain. Every supported format redacts out of the box in a build you make
yourself, exactly as it does in the official build.

## Running it

Press **F5** in Visual Studio, or run it from the command line:

```bash
dotnet run --project PhilterDesktop/PhilterDesktop.csproj
```

## On-device name detection

The optional **AI Detection → Names** filter uses a bundled
[PhEye](https://github.com/philterd/phileas-net) GLiNER model (about 90 MB) that finds person names
on-device, with no network call at redaction time. The model is **not stored in git**; it is
downloaded at build time.

- **Release builds** download it automatically if it is missing.
- **Debug builds and CI** skip the download so builds stay fast and work offline. Philter Desktop
  notices it is absent and simply disables name detection; everything else still works.

To use name detection from a Debug build, fetch the model first:

```powershell
pwsh scripts/download-pheye-model.ps1
```

or force the download as part of a build:

```bash
dotnet build -p:DownloadPhEyeModel=true
```

Full details are in
[`PhilterDesktop/Models/README.md`](https://github.com/philterd/PhilterDesktop/blob/main/PhilterDesktop/Models/README.md).

## Running the tests

```bash
dotnet test PhilterDesktop.Tests/PhilterDesktop.Tests.csproj
```

The suite covers the data layer (LiteDB repositories), the redaction service, Word redaction, the
contract between the policy editor and the redaction engine, and form-construction smoke tests.
On-device name-detection tests are skipped when the PhEye model has not been bundled. The same
build-and-test flow runs in continuous integration on every push and pull request
(`.github/workflows/ci.yml`).

### The built-in self-test

The application can check itself against a small corpus it generates at runtime (nothing is
bundled), covering every supported text-based format and verifying that each output is free of
residual PII:

```
PhilterDesktop.exe --selftest
```

It prints `Result: PASS (n/n)` and exits with code `0` on success, so it can be used as a release
gate against either a build or an installed copy. PDF is not covered by the self-test and is
verified by hand.

A separate check confirms that the license agreement bundled with the installer still matches the
live copy published at `https://philterd.ai/philterd-eula.txt` (this one needs a network
connection, and exits `0` on a match):

```
PhilterDesktop.exe --smoketest
```

Before publishing a release, work through the manual checklist in
[`RELEASE_TESTING.md`](https://github.com/philterd/PhilterDesktop/blob/main/RELEASE_TESTING.md),
which covers installing on a clean Windows machine and smoke-testing the application.

## Building the setup installer

The distributable setup program is built with Inno Setup:

```powershell
pwsh Installer\build-setup.ps1
```

This runs the tests, publishes a **native build for each processor architecture** (`win-x64` and
`win-arm64`), and compiles both into the single `PhilterDesktop-Setup-<version>.exe` that installs
whichever one matches the machine. The finished installer is written to `Installer\Output\`.

The version number comes from `<Version>` in `PhilterDesktop/PhilterDesktop.csproj`, so bump it
there for a release rather than passing it on the command line.

For the full set of options, including single-architecture developer builds, skipping the test run,
framework-dependent builds, and code signing, see
[`Installer/README.md`](https://github.com/philterd/PhilterDesktop/blob/main/Installer/README.md).

## Where to go from here

- **[Getting Started](getting-started.md)**: installing and using the application.
- **[Licensing & Support](licensing.md)**: how the open-source code and the paid official product
  fit together.
