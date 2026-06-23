# Philter Desktop

A Windows desktop application for redacting personally identifiable information (PII) from
plain text (`.txt`) and Microsoft Word (`.docx`) documents.

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

### Word (.docx) redaction license

Word redaction uses [Xceed Words for .NET](https://xceed.com/), which requires a license.
Supply your key in one of two ways (both are git-ignored / outside source control):

- Create `PhilterDesktop/xceed-license.json` (see `xceed-license.example.json`):
  ```json
  { "XceedLicenseKey": "your-key-here" }
  ```
- Or set the `XCEED_LICENSE_KEY` environment variable.

Without a key, plain-text redaction still works; Word redaction runs in Xceed trial mode.

## Features

- **Redaction queue** — add `.txt`/`.docx` files (button or drag-and-drop); they are redacted
  in the background with live, color-coded status.
- **Policy editor** — enable PII filters and configure their replacement strategies. Filters are
  discovered automatically from the Phileas model and grouped by category (Personal, Contact,
  Location, Financial, Identifiers, Technical, Medical, …), with a search box and an
  enabled-count indicator.
- **Contexts** — manage redaction contexts for consistent replacement.
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

The suite covers the data layer (LiteDB repositories), the redaction service, license-key
resolution, Word redaction, the editor↔engine policy contract, and form construction smoke
tests. Word-redaction tests are skipped automatically when no Xceed license is configured.
The same build-and-test flow runs in CI on every push and pull request
(`.github/workflows/ci.yml`).

## License

Copyright 2026 Philterd, LLC.
