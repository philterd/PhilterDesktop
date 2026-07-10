# Release Notes

All notable changes to Philter Desktop are recorded here, newest first.

## Unreleased

- **Internal:** Word (`.docx`) and Excel (`.xlsx`) redaction now runs from the shared Phileas library
  (`Phileas.Services.Office`) instead of app-local code. No change to redaction behavior or output.

## 1.0.0 — 2026-07-04

Initial release of Philter Desktop — an offline Windows application for finding and redacting personally
identifiable information (PII) in PDF, Word (`.docx`), Excel (`.xlsx`), text, CSV, RTF, and email
(`.eml`/`.msg`) files, entirely on your own machine with nothing sent to the cloud. Detection is driven by
configurable policies (powered by the Philterd Phileas engine), and redaction runs one file at a time, in
batches, from a watched folder, at the command line, or from the Windows Explorer right-click menu.

**Highlights**

- **Offline, on-device redaction** across PDF, Word, Excel, text, CSV, RTF, and email — no cloud, no data
  leaves your machine.
- **Configurable detection** through redaction policies and contexts, with on-device name detection
  (bundled model) and OCR for scanned PDFs.
- **Flexible workflows** — single file, batch queue, watched folders, command line, and an Explorer
  right-click menu, backed by an **encrypted local history** you can review, modify, and re-apply.
- **Hidden-data scrubbing** in Office and PDF files: document metadata, comments, tracked changes, hidden
  text, print headers and footers, charts and their cached values, formula and pivot caches, text boxes
  and shapes, Word field codes, and embedded workbooks/objects — and it **warns when a file contains
  content it could not inspect** (such as an image or an opaque embedded object).
- **Verification pass** re-scans redacted output for residual detectable PII.
- **Self-contained installer** — bundles the .NET 10 runtime, so no separate runtime install is required.

**Important:** redaction relies on your policy detecting the sensitive values, and uses statistical and
machine-learning methods that can miss information or over-flag it. Always have a qualified person review
every redacted document before sharing or relying on it.
