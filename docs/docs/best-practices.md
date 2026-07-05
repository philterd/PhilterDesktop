# Best Practices

This page collects practical advice for getting the most effective results from Philter Desktop. It is
a companion to the feature pages, gathered in one place so you can build a reliable redaction habit.

!!! warning "Advice, not a guarantee"
    Redaction is **statistical**: Philter Desktop recognizes many kinds of sensitive information, but it
    can miss something unusual or remove too much. The suggestions here **improve** your results — they do
    **not** guarantee that every piece of sensitive information is found or removed. **Always review a
    redacted file before you rely on it or share it.** You are responsible for the final document. See
    [Redaction Accuracy](mistakes.md) to understand why redaction can make mistakes and how to reduce them.

## Always review the redacted draft

Redacted files are saved with a **`_redacted-draft`** suffix on purpose: the result is a **draft to
check**, not a finished product. Open it, read it, and confirm the sensitive information is gone and that
nothing you needed was over-removed. Treat the draft as a starting point for your own review.

## Let verification and the report help you check

- Keep **[verification](settings.md#verifying-redactions-automatically)** on. After each redaction Philter Desktop re-scans the
  finished file and warns you if it still detects something that looks sensitive. A clean result is
  reassuring but is **not** a guarantee — it only means the detector found nothing on the second pass.
- Generate the **redaction report** for a record of what was removed (by type and count) and the file
  hashes. It is safe to file alongside the redacted copy because it contains **no original text**.

## Resolve tracked changes, comments, and hidden content in Word

Word documents often carry information that isn't visible on screen but is still in the file:

- **Tracked changes (revisions).** Deleted-but-tracked text is stored in the document and can be brought
  back with *Reject Changes*. Philter Desktop redacts this hidden text and, by default, also **accepts and
  removes** tracked changes — but the cleanest habit is to **resolve tracked changes yourself** (accept or
  reject all) **before** redacting, so you know exactly what the document contains.
- **Comments, hidden text, and document metadata.** Keep the **[Microsoft Office](settings.md#microsoft-office-tab)**
  scrubbing options on so reviewer comments, hidden text, author/company properties, and header/footer PII
  are handled. Turn them off only if you have a specific reason.

## Handle email attachments deliberately

Philter Desktop redacts the email **message** (subject, addresses, body) but does **not** open or redact
**attachments**. If an email has attachments:

- **Save each attachment out and redact it on its own** (a `.pdf`, `.docx`, `.xlsx`, or `.txt` attachment
  goes straight through Philter Desktop), **or**
- turn on **Remove attachments from redacted email** in [Settings → Email](settings.md#email-tab) to drop
  them entirely.

Also consider removing the **Date** and **identity headers** (Bcc, Reply-To, …) in the same settings when
those shouldn't ship. Always review the finished email **and** its attachments before sharing.

## PDFs: know what redaction does

- When Philter Desktop redacts a PDF it **flattens each page to an image**, so the removed text has no
  recoverable text layer. This is strong, but it also means the output is no longer selectable text.
- **Scanned PDFs** are pictures of text. Turn on **OCR** ([Settings → PDF](settings.md#pdf-tab)) so their
  text can be read and redacted — then review carefully, since OCR can miss low-quality scans or
  handwriting.
- For things a detector can't find — a **signature, photo, logo, or a form field that's always in the same
  spot** — use **[PDF regions](redacting-pdf.md)** to always black out that fixed area.

## Spreadsheets: redact whole columns when values stand alone

A single cell often holds a bare value (a name or ID) with no surrounding sentence for context, which is
harder to detect. When a column is entirely sensitive (names, account numbers, an ID column), **tick it
for whole-column redaction** instead of relying on per-cell detection. Remember that Excel redaction works
on **one worksheet at a time**, and values stored as numbers are only removed when their column is selected.

## Choose and test your policy

- Use a **[policy](policies.md)** that targets the information types your documents actually contain, and
  **test it on a few representative sample documents** before running a large batch or a watched folder.
- Use a **[context](contexts.md)** when you want the same value to be replaced consistently (the same name
  always becomes the same stand-in).
- Watch for information **split across cells, columns, or runs** — each is scanned on its own, so a value
  broken into pieces can be missed. Review those cases by hand.

## Make sure name detection is available

Person-name detection runs on an **on-device model**. If it isn't installed, names may remain in the
output — verification will tell you when a redaction ran without it. Confirm name detection is available
before relying on it for documents whose main risk is people's names.

## Keep originals and drafts separate

- Keep your **original files** and the **`_redacted-draft`** copies apart, and don't overwrite an original
  with a redacted version.
- For **[watched folders](watched-folders.md)**, give each folder its **own** output directory (Philter
  Desktop enforces this) so redacted files can't overwrite each other, and keep output folders **outside**
  any watched folder.

## Treat the Explanation export as sensitive

The **Export Explanation (JSON)** feature includes the **original detected text** so you can audit what was
found. That file therefore contains the very information you're redacting — **store and share it as
carefully as the original document**. The redaction **report** (above) is the safe-to-share summary; the
explanation is not.

## Review large or complex documents more closely

Big or unusually structured files (many tables, text boxes, embedded objects, or mixed content) are the
ones most worth a careful manual pass. When in doubt, review more, not less.
