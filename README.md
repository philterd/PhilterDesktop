# Philter Desktop

A Windows desktop application for redacting personally identifiable information (PII) from
plain text (`.txt`), Microsoft Word (`.docx`), and PDF (`.pdf`) documents.

![build-and-test](https://github.com/philterd/PhilterDesktop/actions/workflows/ci.yml/badge.svg)

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Windows 10/11
- Visual Studio 2022 (17.12 or later) — optional; the CLI is sufficient

The redaction engine ([Phileas](https://github.com/philterd/phileas-net)) is consumed as the
`Philterd.Phileas` NuGet package, so no separate clone or build is required.

### Building

```bash
git clone https://github.com/philterd/PhilterDesktop
cd PhilterDesktop
dotnet build PhilterDesktop.slnx
```

`PhilterDesktop.slnx` contains two projects:

- **PhilterDesktop** — the WinForms application (UI, data access, redaction, and the policy editor)
- **PhilterDesktop.Tests** — the xUnit test suite

To build the **MSIX installer** in Visual Studio, open **`PhilterDesktop.sln`** instead — it adds
the `Installer/PhilterDesktop.Package.wapproj` packaging project (the `.slnx` format doesn't
support `.wapproj`). See `Installer/README.md`.

### Running

- Press F5 in Visual Studio, or
- Run from the command line:
  ```bash
  dotnet run --project PhilterDesktop/PhilterDesktop.csproj
  ```

Word (`.docx`) redaction uses the open-source [Open XML SDK](https://github.com/dotnet/Open-XML-SDK)
— no license key or third-party component is required. All supported formats (`.txt`, `.docx`,
`.pdf`) redact out of the box.

## Features

- **Redaction queue** — add `.txt`/`.docx`/`.pdf` files (button or drag-and-drop); they are
  redacted in the background with live, color-coded status. PDF redaction is image-based (the
  output has no recoverable text layer).
- **Policy editor** — enable PII filters and configure their replacement strategies. Filters are
  discovered automatically from the Phileas model and grouped by category (Personal, Contact,
  Location, Financial, Identifiers, Technical, Medical, …), with a search box and an
  enabled-count indicator.
- **On-device name detection** — an optional **AI Detection → Names** filter uses a bundled
  [PhEye](https://github.com/philterd/phileas-net) GLiNER model to find person names contextually,
  running entirely on the machine with no network call. The model is downloaded at build time and
  shipped inside the installer (see [Bundled models](PhilterDesktop/Models/README.md)).
- **Highlight redactions** — optionally highlight the replacement text in redacted Word (`.docx`)
  documents for clearer visual review (a per-document option on the Redact Documents form).
- **Watched folders** — monitor folders for new `.txt`/`.docx`/`.pdf` files and redact them
  automatically to an output folder, each with its own policy, context, and highlight option
  (configured on the **Watched Folder** tab of Settings). Philter Desktop runs in the **system
  tray** (closing the window keeps it watching) and can **start at sign-in**.
- **Contexts** — manage redaction contexts for consistent replacement.
- **Command-line redaction** — redact files headlessly for scripting/automation:
  `PhilterDesktop.exe /p mypolicy /c mycontext file1.pdf file2.pdf` (policy/context optional,
  defaulting to the default policy/context). Works even while the app is running.
- **Explorer right-click menu** — an optional **"Redact with Philter Desktop"** context-menu entry
  for `.pdf`/`.docx`/`.txt` files (toggled in Settings). Right-clicking files opens a dialog to pick
  the policy/context and adds them to the redaction queue; a multi-file selection opens one dialog.
- **Settings** — output location, logging, and the redaction policy defaults.

Each filter supports multiple replacement strategies:

- **Redaction** — replace with a format string (e.g., `{{{REDACTED-%t}}}`)
- **Static replacement** — replace with fixed text
- **Random replacement** — replace with a randomly generated value
- **Conditional filtering** — apply a strategy only when a condition is met
- **Scope control** — document-level or context-level replacement

## Testing

```bash
dotnet test PhilterDesktop.Tests/PhilterDesktop.Tests.csproj
```

The suite covers the data layer (LiteDB repositories), the redaction service, Word redaction
(via the Open XML SDK — no license needed, so these always run), the editor↔engine policy
contract, and form construction smoke tests. On-device name-detection tests are skipped when the
PhEye model is not bundled.
The same build-and-test flow runs in CI on every push and pull request
(`.github/workflows/ci.yml`).

## License

Copyright 2026 Philterd, LLC.
