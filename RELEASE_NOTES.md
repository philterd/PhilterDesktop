# Release Notes

All notable changes to Philter Desktop are recorded here, newest first.

## 1.1.1 — 2026-09-08

- **Changed:** there is now **one installer for every PC**. The separate Intel/AMD (x64) and ARM64
  downloads introduced in 1.1.0 are gone; a single `PhilterDesktop-Setup-<version>.exe` contains both
  native builds and installs the one that matches your PC, so there is nothing to choose based on your
  hardware. Installing it over an existing copy switches architecture cleanly if you have moved to an
  ARM PC.
- **Changed:** Philter Desktop now appears in **Add/Remove Programs** as just *Philter Desktop* rather
  than *Philter Desktop version x.y.z*. The version still shows in that list's own Version column.
- **Fixed:** an **unattended uninstall** (`/VERYSILENT`) no longer stops on the "also remove your saved
  data?" question and waits for someone to click it. It now skips the question entirely and **always
  keeps** your policies, contexts, settings, and history. Previously a scripted uninstall could hang,
  or delete that data if the dialog was answered Yes.
- **Fixed:** a rare failure reading the encrypted database's key when several Philter Desktop
  processes touched it at once (for example the app, a command-line redaction, and an Explorer
  right-click starting together). Changing your passphrase while another process was reading the key
  could also fail, and in the worst case a process could conclude no key existed and generate a
  second one, leaving the database unreadable. Key-file reads and writes are now contention-safe.
- **New:** Philter Desktop can be **installed without the wizard**, for setting up several PCs or
  installing from a script: `PhilterDesktop-Setup-<version>.exe /VERYSILENT /SUPPRESSMSGBOXES
  /CURRENTUSER`. See *Getting Started* in the documentation for the full list of options.

## 1.1.0 — 2026-08-05

- **New:** a **native Windows on ARM (ARM64)** build. Philter Desktop now ships **separate Intel/AMD (x64)
  and ARM64 installers**; the ARM64 build runs natively on ARM PCs (such as Copilot+ / Snapdragon
  laptops) instead of under x64 emulation, which is what lets on-device name detection work there.
- **New:** the **Policy Editor** now offers the **EIN (Employer Identification Number)** identifier, the
  U.S. federal business tax ID (format *NN-NNNNNNN*). An option restricts matches to prefixes the IRS
  actually issues (off by default, so any correctly formatted value matches).
- **New:** a **PhEye** tab in the Policy Editor for on-device AI models. The built-in person-names model
  now lives here, and you can **add your own local (GLiNER) models** — point Philter Desktop at a model
  folder and list the entity types it should detect.
- **New:** in **Modify Redaction**, a **Show low-confidence (not redacted)** option lists entities the
  on-device AI detected but left in because their confidence fell below the threshold, so you can review
  the near-misses.
- **New (experimental):** a **Redact recurring images** option on the Settings → PDF tab blacks out images
  that repeat across a PDF's pages — logos, watermarks, and similar recurring graphics — wherever they
  appear, without a fixed region. Off by default; covers raster images only and may over- or under-cover,
  so review the output.
- **New:** PDF fixed regions now accept an **all-but-the-first-page** range — enter `2-` (page 2 to the
  end) to cover a logo or footer that appears on every page except the cover.
- **Improved:** the **Modify Redaction** list now shows each item's detection **confidence** and lets you
  **sort by any column** (click a heading; click again to reverse).
- **Improved:** the Policy Editor window is now resizable and opens larger.
- **Fixed:** the main window now always opens **centered** instead of reappearing in a stale, possibly
  off-screen position.
- **Fixed:** the Modify Redaction window's **Type** column now shows each redaction's entity type
  (e.g. *First Name*, *Email Address*) instead of a generic "Detected".
- **Fixed:** the **Save** and **Cancel** buttons on the Settings window no longer overlap the tab
  control — they now sit below it.
- **Fixed:** a rare error while creating the encrypted local database key if several Philter Desktop
  processes started for the very first time at exactly the same moment.
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
