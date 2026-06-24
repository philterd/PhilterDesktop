# Redacting Documents

The main window shows the **redaction queue** — the list of documents to be (or already)
redacted, with their status, policy, and context.

## Supported file types

- **Plain text** — `.txt`
- **Microsoft Word** — `.docx` (requires a [Word redaction license](settings.md#word-redaction-license))
- **PDF** — `.pdf`

> **PDF redaction is image-based.** Each page of a redacted PDF is rasterized to an image with the
> detected PII covered, so the output has **no recoverable text layer** — nothing can be copied or
> searched from it. (This also means the redacted PDF is not text-selectable.)

## Adding documents

There are two ways to add files:

- **Redact button** — click **Redact** on the toolbar, choose a **policy** and **context**, then
  select the files to add.
- **Drag and drop** — drag `.txt` or `.docx` files from File Explorer directly onto the window.
  Dropped files use the **default** policy and context.

## How redaction runs

Documents are redacted automatically in the background. Each row's **Status** shows progress:

| Status | Meaning |
|--------|---------|
| **Pending** | Queued, waiting to be processed |
| **Processing** | Currently being redacted |
| **Completed** | Redacted successfully |
| **Failed** | Could not be redacted (e.g., file moved, or policy missing) |

Redaction never modifies your original file. A new copy is written with the configured suffix
(default `_redacted-draft`, e.g. `report.docx` → `report_redacted-draft.docx`) to your configured
[output location](settings.md).

## Managing the queue

**Double-click** a **Completed** item to open its redacted file. You can also right-click a row
(or use the toolbar) to:

- **Open redacted file** / **Open original file**
- **Modify Redaction…** — review and change what was redacted (see below)
- **Remove**, **Remove completed**, or **Remove all**
- **Refresh** the list

Bulk removals ask for confirmation before clearing items.

## Modifying a redaction

After a document is redacted, the exact set of redactions is saved, so you can adjust them without
re-running the whole process. Right-click a **Completed** document and choose **Modify Redaction…**.

The dialog shows:

- a **Versions** tree — the original redaction is **Version 1**; and
- the **list of redactions** for the selected version (type, original text, replacement, the
  **start/stop** character positions, and location), which you can edit.

**Working with versions:**

- **New Version** creates a new version by copying the latest version's redactions, so you can make
  changes without losing earlier work (Version 5 starts from Version 4).
- **Delete** removes the selected version. **Version 1 cannot be deleted.**
- Selecting a version shows its redactions; your edits are saved to that version.

**Version 1 is read-only** — it preserves the original automatic redaction, so its list can't be
edited or deleted. Click **New Version** to make changes.

**Editing the selected version's redactions** (double-click a redaction to open it for editing):

- **Remove** a redaction — e.g. to stop redacting one specific name;
- **Edit** a redaction's replacement text; for redactions you added, you can also change its
  position; and
- **Add** a new redaction **by position**, then give it a replacement. The position fields depend on
  the document type:
    - text (`.txt`) — the **start** and **stop** character offsets;
    - Word (`.docx`) — the **paragraph** plus the start/stop offsets within it; and
    - PDF — the **page** and a bounding box.

Redactions are located by **position**, not by matching their text — so detected and added
redactions alike are re-applied to the source by their stored start/stop (and paragraph or page),
not by searching for the original text.

Click **Redact** to apply the selected version's redactions to the **original source** document,
writing that version's output file (using the configured suffix — Version 1 →
`report_redacted-draft.docx`, later versions → `report_redacted-draft_2.docx`, etc.). The original
file must still exist at its original location, since
the redaction is produced from it. The redacted document opens automatically when it's done.

## Comparing the original and redacted file

To confirm exactly what was redacted, right-click a **Completed** item and choose **View Diff…**
(enabled for `.txt` and `.pdf` documents). The original is shown on the **left** ("Before") and the
redacted output on the **right** ("After"). The view is read-only and never changes either file.

### Text files (`.txt`)

A line-by-line diff with the lines kept aligned and color-coded:

- **Red** — text that was removed,
- **Green** — text that was added (the replacements), and
- **Yellow** — lines that were changed.

Long lines **wrap**, and both panes scroll together.

### PDF files (`.pdf`)

A **side-by-side page view**: each page of the original is rendered next to the same page of the
redacted PDF, so you can visually confirm the redactions. Use **Previous/Next** to move through the
pages. (Because redacted PDFs are image-based, this is a visual comparison rather than a text diff.)

## Command-line (headless) redaction

Philter Desktop can redact files from the command line without opening the window — useful for
scripts and automation. Pass one or more files (and, optionally, a policy and context name):

```
PhilterDesktop.exe /p mypolicy /c mycontext file1.pdf file2.pdf file3.pdf
```

- **`/p`, `--policy`** — the redaction policy name to use. Optional; defaults to the **default** policy.
- **`/c`, `--context`** — the redaction context name to use. Optional; defaults to the **default** context.
- **`/h`, `--help`** — show usage.

Each file is redacted to a copy with the configured suffix (default `_redacted-draft`, honoring the
output-location [setting](settings.md)); originals are never changed. Supported types are `.txt`, `.docx`, and
`.pdf`. The exit code is **0** when everything succeeded, **1** if one or more files failed, and
**2** for a usage error or an unknown policy.

> Run it from a terminal (cmd/PowerShell) to see per-file results. The command-line mode does not
> open the main window, and it works even while Philter Desktop is already running (the database is
> opened in shared mode), so it's suitable for use from the Windows Explorer right-click menu.

For a no-typing way to redact from File Explorer, turn on the
[Explorer right-click menu](settings.md#explorer-right-click-menu) in Settings — right-clicking files
opens a dialog to choose the policy/context and adds them to the redaction queue (rather than running
headlessly like the command above).

## What gets redacted

What Philter Desktop looks for, and how it replaces it, is determined entirely by the
**policy** assigned to each document. See [Policies](policies.md) and
[Filter Strategies](filter-strategies.md).
