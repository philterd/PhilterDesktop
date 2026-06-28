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

Philter Desktop works with these document types:

- **Plain text** — files ending in `.txt` (simple, unformatted text, like a Notepad file).
- **Microsoft Word** — files ending in `.docx`.
- **PDF** — files ending in `.pdf`.
- **Rich Text** — files ending in `.rtf` (a formatted-text format used by WordPad and many legal and
  records systems).
- **Spreadsheets** — Excel files ending in `.xlsx` and comma-separated files ending in `.csv`.
- **Email** — files ending in `.eml` (the standard email format used by most mail programs) and
  `.msg` (the format Microsoft Outlook uses when you save or drag out a message).

!!! note "Password-protected Word and Excel files"
    A Word or Excel file that has been **protected with a password** (encrypted) cannot be opened for
    redaction. If you try, Philter Desktop tells you so clearly. To redact one, open it in Word or
    Excel, remove the password (**File → Info → Protect Document / Protect Workbook**), save it, and
    then redact the unprotected copy.

> **One important thing about PDFs.** When Philter Desktop redacts a PDF, it does something
> deliberately thorough: it turns each page into a flattened **picture** of the page with the
> sensitive information painted over. The benefit is that the removed information is *truly gone* —
> there is no hidden text layer left behind that someone could copy, search, or recover. (You may
> have heard stories of "redacted" PDFs where the black boxes could be copied off to reveal the text
> underneath; that cannot happen here.) The trade-off is that the cleaned-up PDF behaves like a
> scanned document: you can read it and print it, but you can no longer select or search its text.
> For most legal and confidentiality purposes, this is exactly what you want. One consequence: because
> each item is simply painted over with a solid box, your [filter strategy](filter-strategies.md) choice
> (the replacement text) does **not** change how a redacted PDF looks — PDFs always get solid boxes.

### Redacting email (and why `.msg` comes out as `.eml`)

When Philter Desktop redacts an email, it cleans up the **subject line**, the **From / To / Cc**
addresses, and the **message body** (both plain-text and HTML versions), according to your policy —
the same way it handles any other document.

!!! warning "Attachments are not redacted"
    Philter Desktop redacts the email **message itself** — the subject, the addresses, and the body. It
    does **not** open, inspect, or redact **attachments** (a PDF, Word file, spreadsheet, image, or
    anything else carried along with the email). Those are copied through **unchanged**, so any
    sensitive information inside an attachment is left exactly as it was. If an email has attachments
    that need redacting, **save each one out as its own file and redact it separately** (a `.pdf`,
    `.docx`, or `.txt` attachment can go straight through Philter Desktop). Always review the finished
    email **and** its attachments before sharing it.

There's one thing to know about Outlook `.msg` files: **a redacted `.msg` is saved as an `.eml` file.**
So redacting `message.msg` produces `message_redacted-draft.eml`, not a `.msg`.

Why the change in file type? `.msg` is Microsoft's own private, undocumented Outlook format. Philter
Desktop can reliably **read** a `.msg` and pull out everything it needs to redact, but rebuilding a
brand-new `.msg` file faithfully — without quietly dropping or corrupting parts of the message — is not
something it can guarantee. Rather than hand you a `.msg` that *might* be subtly broken, it writes the
redacted result as **`.eml`**, the universal email format. An `.eml` file is a complete, standalone
copy of the email that **opens in Outlook** (just double-click it) as well as in Apple Mail,
Thunderbird, Gmail's import, and essentially every other mail program. Nothing is lost in the
redaction itself — only the container file type changes, in exchange for a result you can trust.

> `.eml` files redact to `.eml`, and everything else keeps its original file type as usual — only
> `.msg` changes.

### Redacting spreadsheets (`.xlsx`, `.csv`)

Spreadsheets are redacted **cell by cell**: Philter Desktop looks at each cell on its own and removes
the sensitive information it finds there, leaving the rest of the table — the layout, the columns, the
numbers, the formulas — intact. An Excel file stays `.xlsx` and a CSV stays `.csv`. (For Excel,
**formulas** are left alone, since their value is calculated rather than stored.)

There's an important thing to understand about spreadsheets. Philter Desktop recognizes sensitive
information partly from the **words around it** — and a cell often holds a value all by itself, with no
surrounding sentence for context. Patterns with a fixed shape (Social Security numbers, email
addresses, account numbers) are still caught reliably. But something like a lone first name in a cell
— "April" — has nothing around it to signal that it's a name, so automatic name detection is **much
weaker** on bare cells than on ordinary paragraphs of writing.

There's also a point about **numbers**: a value a spreadsheet stores as a *number* — say an account
number or an ID typed as plain digits — is **not scanned** for sensitive information, because Philter
Desktop leaves numbers exactly as they are (so totals and calculations aren't disturbed). Sensitive
values that look like text, such as an SSN written with dashes (`123-45-6789`), are still detected. If
a column holds sensitive **numeric** IDs, remove it with whole-column redaction, described next.

That's why spreadsheets have an extra tool: **whole-column redaction**. Use the **Redact
Spreadsheet…** action — on the **Redact** button's arrow menu, or by right-clicking a spreadsheet in
the queue. It opens a small window where you choose the **policy** and **context** as usual, and also
see a **list of the file's columns** (with their headers). Tick any column whose contents should be
removed **entirely** — for example a "Name" column, a "Patient ID" column, or an "Account" column —
and every **data cell** in that column is cleared, regardless of whether the detector would have
flagged it. The column's **header label is kept** (so the table stays readable — only the values below
it are removed). Columns you don't tick are still cleaned the normal way (detected sensitive values
removed). This is the dependable way to handle columns of names and identifiers.

When you click **Redact**, the spreadsheet is **added to the queue** with your choices and the window
closes; it's then redacted in the background like any other document, and you'll see it appear in the
main list with its status.

> When you redact a spreadsheet through the ordinary queue, drag-and-drop, a [watched
> folder](watched-folders.md), or the command line, Philter Desktop runs **detection on every cell**
> (no column is fully cleared, because those routes don't ask you any questions). To pick whole columns
> to remove, use **Redact Spreadsheet…**.

## Adding documents to the queue

There are two ways to add files:

- **The Redact button.** Click **Redact** on the toolbar. You'll be asked which **policy** (rule set)
  and which **context** (consistency setting) to use, and then you choose the files you want to add.
  This is the way to go when you want to pick specific rules for a particular batch of documents.
  The **Redact** button also has a small **arrow** beside it; clicking the arrow opens a short menu
  with alternatives — **Redact with Preview…** (see the result before saving), **Find & Redact…**
  (remove specific words from one file), **Redact Spreadsheet…** (redact an `.xlsx`/`.csv` with
  optional whole-column removal), and **Redact Folder…** (add every supported file in a folder at
  once). These are described later on this page.
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
| **Failed** | Something prevented the cleanup — for example, the original file was moved or deleted, the file was open in another program, or the chosen policy no longer exists. |

If a document shows **Failed**, Philter Desktop remembers *why*. **Hover your mouse over the failed
row** to see the reason in a small pop-up, or right-click the row and choose **View Details…**, where
the reason appears as a **"Why it failed"** line. The reasons are written in plain language — for
example, *"…could not save 'report.docx' because it is open in another program (such as Microsoft
Word or a PDF viewer). Please close it and try again."* Once you've fixed the cause, you can add the
document again to retry it.

Whatever happens, **your original document is never changed.** Philter Desktop always writes the
result to a **new, separate copy**. That copy is given a name based on the original plus a label —
by default the label is `_redacted-draft`, so `report.docx` becomes `report_redacted-draft.docx` —
and it's saved to the location you've chosen in [Settings](settings.md). The word "draft" in that
name is a gentle reminder that you should still review the file before relying on it.

For Word (`.docx`) files, Philter Desktop also cleans up hidden information in the redacted copy by
default — the document's **metadata** (author, company, title, keywords, custom properties), reviewer
**comments**, **tracked changes**, and **hidden text** — so a "redacted" file doesn't quietly leak
through any of those channels. You can control each of these on the
[Settings → Microsoft Word](settings.md#microsoft-word-tab) tab.

## Redact a whole folder at once

When you have a folder full of documents — a discovery dump, a case folder, a batch of exports — you
don't have to add the files one by one. Click the small **arrow** beside the **Redact** button and
choose **Redact Folder…**.

In the dialog:

1. **Choose the folder** with **Browse…** (or type/paste its path).
2. Tick **Include files in subfolders** if you also want the documents inside any folders within it.
3. Pick the **policy** and the **context** to use for the whole batch.
4. Optionally tick **Highlight redactions in Word (.docx) documents**.

As soon as you choose a folder, Philter Desktop scans it and tells you exactly what it found — for
example, *"12 files will be added to the redaction queue (5 .pdf, 4 .docx, 3 .txt)."* — so you can
sanity-check the batch before committing. Click **Add to Queue** and every supported file is added to
the redaction queue and cleaned up the same way as any other document, with its own **Pending →
Processing → Completed** (or **Failed**) status. Nothing about a watched folder is set up, and the
folder is not monitored afterward — this is a one-time action.

A few things worth knowing:

- **Only supported file types are picked up.** Files Philter Desktop can't redact (images, archives,
  and so on) are simply skipped, and the count tells you how many files will actually be processed.
- **Previous results are left alone.** Files that already look like a redacted copy (their name ends
  with your redacted-file label, `_redacted-draft` by default) are skipped, so running **Redact
  Folder…** again won't re-redact the drafts it made last time.
- **Per-file results are visible.** If one file can't be cleaned up (for example, a password-protected
  Word document), that single row shows **Failed** with the reason — the rest of the batch still
  completes.
- **Your originals are never changed.** As always, each cleaned-up copy is written to a new, separate
  file (see [What happens after you add a document](#what-happens-after-you-add-a-document) above).

## Redact with Preview: see the result before you commit

Sometimes you'd rather **look at the result first** and only save it once you're happy. That's what
**Redact with Preview** is for. It's best for working carefully on a single document at a time.

Start it from the **Redact** button on the toolbar: click the small **arrow** beside it and choose
**Redact with Preview…** (or right-click a document and choose the same). It works with `.txt`,
`.rtf`, `.docx`, `.pdf`, and email (`.eml` and `.msg`) files. (Spreadsheets — `.xlsx` and `.csv` —
don't have a preview yet; redact them the ordinary way and review the cleaned-up copy afterward.)
You pick the file, choose a **policy** and a **context**, and Philter Desktop shows you **what the
cleaned-up file will look like before a single thing is written to disk**:

- **Plain text (`.txt`) and rich text (`.rtf`)** — a live, side-by-side comparison of the original
  and the cleaned-up result, plus an editable list of every redaction. You can add, change, or remove
  individual redactions right there before saving. (Rich-text formatting is preserved in the saved
  file; the preview compares the visible text.)
    - **Redact something the detector missed by selecting it.** Switch to the **Select text to redact**
      tab, highlight the words you want removed, and click **Redact selection**. The redaction is added
      to the list (marked **Added**), shows up in the before/after comparison, and is applied when you
      save — and it appears in the [redaction report](#generating-a-redaction-report-a-shareable-certificate)
      as a user-added redaction. You can remove it again from the list like any other. (Prefer typing
      exact positions? **Add…** still lets you enter a start and end offset by hand.)
- **Microsoft Word (`.docx`)** — a paragraph-by-paragraph comparison showing what will be removed,
  with an editable list of redactions and an optional **highlight** setting (which marks the
  replacements so they're easy to spot during review). This preview shows you the redacted **text**;
  it is not a full picture-perfect rendering of the finished Word page.
    - **Redact something the detector missed by selecting it.** Switch to the **Select text to redact**
      tab, highlight the words you want removed, and click **Redact selection**. The redaction joins the
      list (marked **Added**), appears in the comparison and the [report](#generating-a-redaction-report-a-shareable-certificate)
      as a user-added redaction, and can be removed again like any other. A selection that crosses
      paragraphs is split into one redaction per paragraph. (**Add…** still lets you type an exact
      paragraph and offset by hand.)
- **PDF (`.pdf`)** — the cleaned-up PDF shown side by side with the original, with zoom controls and
  scrolling that keeps both sides lined up.
- **Email (`.eml` and `.msg`)** — a field-by-field comparison (subject, addresses, and body) showing
  what will be removed, with an editable list of redactions. Each redaction is anchored to a specific
  field; you can change its replacement or remove it. Outlook `.msg` files are saved as standard `.eml`.
    - **Redact something the detector missed in the body by selecting it.** Switch to the **Select text
      to redact** tab (which shows the message body), highlight the words you want removed, and click
      **Redact selection**. It joins the list (marked **Added**) and the
      [report](#generating-a-redaction-report-a-shareable-certificate) as a user-added redaction, and
      can be removed again. Manual selection applies to **body** text; the subject and address headers
      aren't hand-edited (their detected matches are still removed automatically).

If you change the policy or the context while previewing, Philter Desktop re-checks the document and
updates the preview. **Nothing is saved until you click "Save Redacted File"**, at which point you
choose where to put it. Once saved, the document is added to your queue (marked **Completed**), so you
can re-open it, compare it, or adjust it later just like any other finished document.

Preview is the "look first, then save" approach. The ordinary queue, the [watched
folders](watched-folders.md) feature, and the command line (described near the end of this page) are
the better choices when you need to clean up **many documents at once**.

## Find & Redact: remove specific words, no policy needed

Sometimes you don't need a whole policy — you just want to **strike a few specific words or phrases**
out of one document. Maybe it's a particular name, a project codename, or a phrase your client asked
you to remove. For that there's **Find & Redact**.

Open it from the **Redact** button's **arrow** menu on the toolbar and choose **Find & Redact…** (or
right-click a document in the queue and choose the same — if you'd already selected a document, its
path is filled in for you). A small window opens where you:

1. Choose the **document** to redact (`.txt`, `.docx`, `.pdf`, `.rtf`, `.xlsx`, `.csv`, `.eml`, or `.msg`).
2. Type the **exact terms** to remove, **one per line** — or click **Import from file…** to load them
   from a `.txt` or single-column `.csv` file.
3. Click **Redact**.

Philter Desktop removes every occurrence of those terms (ignoring capitalization) and saves a redacted
copy alongside the original, then offers to open the containing folder. It doesn't touch your policies,
your queue, or your history — it's a quick, self-contained one-off.

This is the right tool when you know **exactly** what text to remove. When you instead want Philter
Desktop to *find* sensitive information by its kind — every Social Security number, every email
address, every name — use a [policy](policies.md) with the ordinary **Redact** or **Redact with
Preview** actions above. And for terms you want removed in *every* redaction, not just this one, use
the global [Lists](policies.md#lists-that-apply-to-every-policy-the-lists-button) instead.

## Working with the queue

Once a document shows **Completed**, you have several options:

- **Double-click** the row to open the cleaned-up file immediately.
- **Right-click** the row to:
    - **Open redacted file** — open the cleaned-up copy.
    - **Open original file** — open the untouched original.
    - **Open containing folder** — show the cleaned-up file selected in File Explorer.
    - **View Details…** — see a summary of that document: the original file name, the cleaned-up file
      name, the policy and context used, how many redactions were made, when it was done, and how long
      the redaction took ("Time to redact").
    - **View Diff…** — see a precise before-and-after comparison (explained below).
    - **Modify Redaction…** — review and adjust exactly what was removed (explained below).
    - **Export Explanation (JSON)…** — save a detailed report of *why* each item was removed
      (explained below).
    - **Remove**, **Remove completed**, or **Remove all** — take items off the list.
    - **Refresh** — reload the list.

The list updates itself as documents are processed, so you rarely need to refresh by hand. A
**Refresh** button is also on the toolbar (and **F5** refreshes too) — handy when documents were added
from the [command line](#for-advanced-users-and-it-redacting-from-a-command-line) or the
[Explorer right-click menu](settings.md#explorer-right-click-menu) while Philter Desktop was open.

When you remove several items at once, Philter Desktop asks you to confirm first, so you can't clear
your list by accident.

**Keyboard shortcuts:** **F5** refreshes the list, **Delete** removes the selected document, **Enter**
opens a completed document's redacted file, and **Ctrl+O** adds files.

### Finding a document in a long list

When you have many documents in the queue, two tools help you find the one you want:

- **Filter box.** Just above the list is a box labelled *Filter by file name, status, policy, or
  context*. Start typing and the list immediately narrows to only the documents that match what you
  typed — for example, type part of a file name, or type `failed` to see only the documents that
  didn't finish. The status bar shows how many documents are being shown (for example, *Showing 3 of
  40 documents*). Clear the box (or press **Esc** while typing in it) to see the whole queue again.
- **Sorting.** Click any column heading — **File Name**, **Status**, **Policy**, **Context**, or
  **Verification** — to sort the list by that column. Click the same heading again to reverse the order. A small arrow on
  the heading shows which column is sorted and in which direction. (Sorting by **Status** groups the
  documents in the natural order of work: pending, then processing, then completed, then failed.)

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
    - **Plain text (`.txt`) and Rich Text (`.rtf`)** — the starting and ending character positions.
    - **Microsoft Word (`.docx`)** — which paragraph, plus the start and end positions within that
      paragraph.
    - **PDF (`.pdf`)** — which page, plus a rectangle on the page.

!!! note "Spreadsheets and email: review and adjust, but no manual *Add*"
    For **spreadsheets (`.xlsx`, `.csv`)** and **email (`.eml`, `.msg`)**, each redaction belongs to a
    specific **cell** or **email field** rather than a position you can point to. You can still
    **review** the redactions, **remove** ones you'd rather keep, **change the replacement text**, and
    **Redact** to regenerate the file — but **Add** is turned off for these formats (there's no
    cell-by-cell "add here" to choose). If you need to remove the entire contents of a column in a
    spreadsheet, use **Redact Spreadsheet…** and tick that column instead. The location column shows
    which **Cell** or **Field** each redaction sits in.

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
**Completed** item and choose **View Diff…** (available for `.txt`, `.docx`, `.pdf`, `.csv`, and
`.eml` files). This opens a **before-and-after comparison**: the original on the **left** ("Before")
and the cleaned-up copy on the **right** ("After"). This view is **read-only** — looking at it never
changes either file.

> **Large files.** Comparing very large files would be slow and memory-hungry, so **View Diff is
> turned off for files over 10 MB** (the menu item is greyed out and labelled "file too large"). You
> can still open the original and redacted files directly to review them.

### Text, Word, CSV, and email documents (`.txt`, `.docx`, `.csv`, `.eml`)

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

## Exporting an explanation of a redaction (JSON)

If you ever need to show your work — for a colleague, a reviewer, an audit, or just your own
records — you can export a detailed **explanation** of a finished redaction. Right-click a **Completed**
document and choose **Export Explanation (JSON)…**.

This saves a `.json` file that lists, for **every item Philter Desktop removed**:

- **What** it was — the original text, and what it was replaced with.
- **Why** it was flagged — the detector that matched (for example, an email-address or Social Security
  number detector), how confident the detector was, and (for rule-based detectors) the pattern it
  matched and the words surrounding it.
- **Where** it was — the position in the document (character position and paragraph for text and Word
  files; the page and location for PDFs).

A `.json` file is a plain-text format that's easy for other programs to read, and is also readable by
a person who knows where to look. (Items you added by hand in **Modify Redaction** are included too,
marked as user-added.)

!!! danger "The explanation file contains the original sensitive text"
    Because the report lists the **original, un-redacted text** of everything that was found — along
    with the surrounding words — the explanation file is **just as sensitive as the original
    document**. Philter Desktop reminds you of this before it saves. Store it somewhere secure, treat
    it with the same care as the original, and **never** hand it out in place of the redacted copy.

## Checking the result for anything missed (Verification)

The worst outcome for a redaction tool is a **false negative** — sensitive text the policy missed that
ends up in the "clean" copy. To guard against that, Philter Desktop can **verify its own work**: after
redacting, it re-opens the **finished output file** and runs the detector over it again, looking for
anything that still matches.

This runs **automatically** after each redaction (you can turn it off in
[Settings → Security](settings.md)), and you can run it any time by right-clicking a **Completed**
document and choosing **Verify Redaction**.

- If nothing is found, Philter Desktop tells you the output **passed verification**.
- If something is found, it's surfaced **loudly**: a count of how many items may remain, plus a list
  showing each item's **type, the text that's still present, and where it is**. Fix it by adjusting the
  policy and redacting again, or by using **Modify Redaction**, before you share the file.

### Two ways to scan: same policy vs. broad policy

When you right-click a finished document, **Verify Redaction** offers two choices:

- **With same policy** — re-scans using the **same policy that redacted the document**. This is the
  best check that the redaction *actually took effect*: it confirms that everything the policy was
  meant to remove is genuinely gone from the saved file. (It can't find a *kind* of information the
  policy never looked for.)
- **With broad policy** — re-scans with **every built-in detector turned on**. This can surface kinds
  of information your redaction policy didn't cover (for example, a phone number when your policy only
  removed email addresses). Because it looks for everything, it may also flag things you **chose not to
  redact** — so treat its findings as prompts to review, not as mistakes.

The automatic check after each redaction uses whichever of these you select in
[Settings → Security](settings.md) (it uses the same policy by default).

### Seeing a document's verification result later

Each document's most recent result is remembered and shown in the **Verification** column of the queue —
**Clean**, **N may remain** (in red), **Check failed**, or **Not checked** — so you can tell at a glance
which finished documents still need attention. The same result, with the time it was checked, also
appears in **View Details** (right-click a document). Running **Verify Redaction** again refreshes it.

Verification reads the **written output**, not an in-memory copy, so it also catches any problem in how
a particular format was saved. Like everything else in Philter Desktop, it runs **entirely on your
device** — nothing is sent anywhere. The result is remembered with the document and is included in the
redaction report below.

!!! note "Verification is a safety net, not a guarantee"
    A "passed" result means nothing the **current policy** can detect remains. It can't prove a document
    is free of every possible identifier. Always give an important document a human review as well.

## Generating a redaction report (a shareable certificate)

When you need to **prove what was done** — for a case file, a client, or a compliance record — you can
generate a **redaction report**. Right-click a **Completed** document and choose **Generate Report…**.

Philter Desktop first asks whether to include a **detailed per-redaction table**, then saves the
report as a **PDF** and opens it. The report summarizes the redaction:

- The **source and redacted file names**, and a **SHA-256 fingerprint** of each file (so the report is
  tied to exactly those documents and any later change is detectable).
- The **policy** and **context** used, the **Philter Desktop version**, and the **date and time**.
- A **count of what was removed, by type** — for example, *7 Email Address, 3 Ssn* — and the total.
- The **verification result** (when verification has run): whether the output passed, or how many items
  may remain.
- If you chose the detailed table: a row per redaction with its **type, location, and replacement**.

The report is saved as a new file next to your other output; your original and redacted files are not
touched.

!!! tip "The report is safe to share — it contains no original text"
    Unlike the explanation file below, a redaction report **never includes the original, un-redacted
    text** (not even in the detailed table — that shows only the type, location, and replacement). That
    makes the report safe to file alongside, or hand out with, the redacted copy. When you need the
    actual detected text for your own secure records, use **Export Explanation (JSON)** instead.

## Exporting an explanation of a redaction (JSON)

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
same as always: `.txt`, `.docx`, `.pdf`, `.rtf`, `.xlsx`, `.csv`, `.eml`, and `.msg` (an `.msg` is
redacted to an `.eml`, as described above). When it finishes, it reports a result code: **0** means
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
