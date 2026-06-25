# Redacting Documents

This is the page you'll come back to most often. It explains how to add documents, how Philter
Desktop cleans them up, how to look at exactly what was removed, and how to make adjustments if the
program removed too much or too little.

The heart of the program is the **redaction queue** — the list that fills most of the main window.
Think of it as your worklist: every document you hand to Philter Desktop appears here as a row, along
with its current status, the policy (set of rules) being used, and the context (the consistency
setting). Documents you've already finished stay in the list too, so you can re-open them, compare
them, or fine-tune them later.

## Which kinds of documents you can redact

Philter Desktop works with three common document types:

- **Plain text** — files ending in `.txt` (simple, unformatted text, like a Notepad file).
- **Microsoft Word** — files ending in `.docx`.
- **PDF** — files ending in `.pdf`.

> **One important thing about PDFs.** When Philter Desktop redacts a PDF, it does something
> deliberately thorough: it turns each page into a flattened **picture** of the page with the
> sensitive information painted over. The benefit is that the removed information is *truly gone* —
> there is no hidden text layer left behind that someone could copy, search, or recover. (You may
> have heard stories of "redacted" PDFs where the black boxes could be copied off to reveal the text
> underneath; that cannot happen here.) The trade-off is that the cleaned-up PDF behaves like a
> scanned document: you can read it and print it, but you can no longer select or search its text.
> For most legal and confidentiality purposes, this is exactly what you want.

## Adding documents to the queue

There are two ways to add files:

- **The Redact button.** Click **Redact** on the toolbar. You'll be asked which **policy** (rule set)
  and which **context** (consistency setting) to use, and then you choose the files you want to add.
  This is the way to go when you want to pick specific rules for a particular batch of documents.
- **Drag and drop.** Drag one or more files from a Windows folder and drop them straight onto the
  Philter Desktop window. Files added this way use the **default** policy and the **default** context.
  This is the quickest way when the standard rules are fine.

## What happens after you add a document

Philter Desktop redacts documents automatically, on its own, in the background — you don't have to
press a "go" button. Each row's **Status** column tells you where that document is in the process:

| Status | What it means |
|--------|---------------|
| **Pending** | The document is in line, waiting its turn. |
| **Processing** | The document is being cleaned up right now. |
| **Completed** | The document was cleaned up successfully and the copy is ready. |
| **Failed** | Something prevented the cleanup — for example, the original file was moved or deleted, or the chosen policy no longer exists. |

Whatever happens, **your original document is never changed.** Philter Desktop always writes the
result to a **new, separate copy**. That copy is given a name based on the original plus a label —
by default the label is `_redacted-draft`, so `report.docx` becomes `report_redacted-draft.docx` —
and it's saved to the location you've chosen in [Settings](settings.md). The word "draft" in that
name is a gentle reminder that you should still review the file before relying on it.

## Redact with Preview: see the result before you commit

Sometimes you'd rather **look at the result first** and only save it once you're happy. That's what
**Redact with Preview** is for. It's best for working carefully on a single document at a time.

Start it with the **Preview** button on the toolbar (next to **Redact**), or by right-clicking and
choosing **Redact with Preview…**. It works with `.txt`, `.docx`, and `.pdf` files. You pick the
file, choose a **policy** and a **context**, and Philter Desktop shows you **what the cleaned-up file
will look like before a single thing is written to disk**:

- **Plain text (`.txt`)** — a live, side-by-side comparison of the original and the cleaned-up
  result, plus an editable list of every redaction. You can add, change, or remove individual
  redactions right there before saving.
- **Microsoft Word (`.docx`)** — a paragraph-by-paragraph comparison showing what will be removed,
  with an editable list of redactions and an optional **highlight** setting (which marks the
  replacements so they're easy to spot during review). This preview shows you the redacted **text**;
  it is not a full picture-perfect rendering of the finished Word page.
- **PDF (`.pdf`)** — the cleaned-up PDF shown side by side with the original, with zoom controls and
  scrolling that keeps both sides lined up.

If you change the policy or the context while previewing, Philter Desktop re-checks the document and
updates the preview. **Nothing is saved until you click "Save Redacted File"**, at which point you
choose where to put it. Once saved, the document is added to your queue (marked **Completed**), so you
can re-open it, compare it, or adjust it later just like any other finished document.

Preview is the "look first, then save" approach. The ordinary queue, the [watched
folders](watched-folders.md) feature, and the command line (described near the end of this page) are
the better choices when you need to clean up **many documents at once**.

## Working with the queue

Once a document shows **Completed**, you have several options:

- **Double-click** the row to open the cleaned-up file immediately.
- **Right-click** the row (or use the toolbar) to:
    - **Open redacted file** — open the cleaned-up copy.
    - **Open original file** — open the untouched original.
    - **View Details…** — see a summary of that document: the original file name, the cleaned-up file
      name, the policy and context used, how many redactions were made, and when it was done.
    - **View Diff…** — see a precise before-and-after comparison (explained below).
    - **Modify Redaction…** — review and adjust exactly what was removed (explained below).
    - **Remove**, **Remove completed**, or **Remove all** — take items off the list.
    - **Refresh** — update the list.

When you remove several items at once, Philter Desktop asks you to confirm first, so you can't clear
your list by accident.

## Adjusting what was removed (Modify Redaction)

Automatic redaction is thorough, but you are always in charge. After a document is cleaned up, Philter
Desktop **remembers the exact list of things it removed**, so you can fine-tune that list without
having to start over. For instance, you might want to *stop* hiding a name that's actually fine to
keep, or you might want to hide one extra item the program didn't catch.

To do this, right-click a **Completed** document and choose **Modify Redaction…**.

The window that opens shows two things:

- A **Versions** list. The very first, automatic cleanup is called **Version 1**. Think of versions
  like saved drafts: each one is a snapshot of a particular set of redaction choices.
- The **list of redactions** for whichever version you've selected. For each item you'll see its
  type, the original text, what it was replaced with, the exact character positions it covers, and
  where in the document it sits.

### How versions work

- **New Version** makes a fresh version by copying the most recent one. This lets you experiment
  freely: your earlier work is preserved, so you can always go back. (Creating a new version from
  Version 4, for example, gives you a Version 5 that starts out identical to 4.)
- **Delete** removes the version you've selected — but **Version 1 can never be deleted**, because it
  preserves the original automatic result.
- Selecting a version shows its list of redactions, and any edits you make are saved to that version.

**Version 1 is read-only.** Because it's the permanent record of the original automatic cleanup, you
cannot edit or delete its list. If you want to make changes, click **New Version** first and edit
that.

### Editing a version's list of redactions

To open a single redaction for editing, **double-click it** in the list. You can:

- **Remove** a redaction — for example, to stop hiding one particular name that's safe to keep.
- **Edit** what a redaction is replaced with (and, for redactions you added yourself, where it sits
  in the document).
- **Add** a brand-new redaction. You add it **by position** — that is, by telling Philter Desktop
  *where* in the document to redact — and then give it the replacement text. What "position" means
  depends on the document type:
    - **Plain text (`.txt`)** — the starting and ending character positions.
    - **Microsoft Word (`.docx`)** — which paragraph, plus the start and end positions within that
      paragraph.
    - **PDF (`.pdf`)** — which page, plus a rectangle on the page.

A helpful detail: redactions are tracked **by their position in the document**, not by searching for
their text. That means every redaction — whether Philter Desktop found it automatically or you added
it by hand — is re-applied to the exact spot it belongs, even if the same words appear elsewhere in
the document.

When your list looks right, click **Redact** to produce the cleaned-up file from that version. Philter
Desktop re-applies that version's redactions to your **original document** and writes a fresh copy
(Version 1 produces `report_redacted-draft.docx`, later versions add a number, like
`report_redacted-draft_2.docx`). Because the file is built from the original, **the original document
must still be in its original location.** The finished document opens automatically when it's ready.

## Comparing the original and the cleaned-up copy (View Diff)

Before you rely on a redacted document, it's wise to confirm exactly what changed. Right-click a
**Completed** item and choose **View Diff…** (available for `.txt`, `.docx`, and `.pdf` files). This
opens a **before-and-after comparison**: the original on the **left** ("Before") and the cleaned-up
copy on the **right** ("After"). This view is **read-only** — looking at it never changes either
file.

### Text and Word documents (`.txt`, `.docx`)

You'll see a line-by-line comparison with the matching lines kept side by side and color-coded so
changes jump out:

- **Red** marks text that was **removed**.
- **Green** marks text that was **added** (the replacements that went in).
- **Yellow** marks lines that were **changed**.

Long lines wrap so you can read them in full, and the two sides scroll together. For Word documents,
the comparison looks at the **paragraphs of text** in the document (one paragraph per line) — the
actual words Philter Desktop worked on — rather than the page's fonts, spacing, or layout.

### PDF documents (`.pdf`)

You'll see the pages **side by side as pictures**: each page of the original next to the same page of
the cleaned-up copy, so you can visually confirm every redaction. Use **Previous** and **Next** to
move through the pages, the **Fit**, **100%**, **+**, and **−** buttons to zoom, and (when zoomed in)
both sides scroll together. Because redacted PDFs are flattened to images, this is a visual
comparison rather than a word-by-word text comparison.

## For advanced users and IT: redacting from a command line

> **This section is optional and aimed at technical users.** If phrases like "command prompt" or
> "script" aren't part of your day, you can skip it entirely — everything Philter Desktop does is
> available through the normal window. This option exists mainly so that an IT department can automate
> redaction.

Philter Desktop can redact files without opening its window at all, which is handy for automation. You
run it from a command prompt (Command Prompt or PowerShell) and hand it one or more files, optionally
naming a policy and context:

```
PhilterDesktop.exe /p mypolicy /c mycontext file1.pdf file2.pdf file3.pdf
```

- **`/p`** or **`--policy`** — the name of the policy (rule set) to use. Optional; if you leave it
  off, the **default** policy is used.
- **`/c`** or **`--context`** — the name of the context (consistency setting) to use. Optional; if
  you leave it off, the **default** context is used.
- **`/h`** or **`--help`** — show a short usage reminder.

Each file is cleaned up into a copy with the usual `_redacted-draft` label (and saved according to
your output-location [setting](settings.md)); originals are never changed. The supported types are the
same as always: `.txt`, `.docx`, and `.pdf`. When it finishes, it reports a result code: **0** means
everything succeeded, **1** means at least one file failed, and **2** means there was a mistake in
how the command was typed or an unknown policy was named.

> Run it from a terminal window to see the result for each file. This mode never opens the main
> window, and it works even when Philter Desktop is already open, which is what makes it suitable for
> the Windows Explorer right-click menu described below.

If you'd like a **no-typing** way to redact straight from a Windows folder, turn on the
[Explorer right-click menu](settings.md#explorer-right-click-menu) in Settings. With it on, you can
right-click files in any folder, and Philter Desktop opens a friendly dialog where you choose the
policy and context and add the files to the queue — no command typing required.

## A reminder about what gets removed

Everything Philter Desktop looks for, and the way it replaces what it finds, is decided by the
**policy** assigned to each document. To change what's removed or how it's replaced, see
[Policies](policies.md) and [Filter Strategies](filter-strategies.md).
