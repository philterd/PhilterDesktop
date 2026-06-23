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

Redaction never modifies your original file. A new copy is written with a `_redacted` suffix
(for example, `report.docx` → `report_redacted.docx`) to your configured
[output location](settings.md).

## Managing the queue

**Double-click** a **Completed** item to open its redacted file. You can also right-click a row
(or use the toolbar) to:

- **Open redacted file** / **Open original file**
- **Remove**, **Remove completed**, or **Remove all**
- **Refresh** the list

Bulk removals ask for confirmation before clearing items.

## What gets redacted

What Philter Desktop looks for, and how it replaces it, is determined entirely by the
**policy** assigned to each document. See [Policies](policies.md) and
[Filter Strategies](filter-strategies.md).
