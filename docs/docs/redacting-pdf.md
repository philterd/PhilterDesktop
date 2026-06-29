# PDF

Philter Desktop redacts PDF files that end in **`.pdf`**.

## How PDF redaction works

When Philter Desktop redacts a PDF, it does something deliberately thorough: it turns each page into a
flattened **picture** of the page with the sensitive information painted over. The benefit is that the
removed information is *truly gone*: there is no hidden text layer left behind that someone could copy,
search, or recover. (You may have heard of "redacted" PDFs where the black boxes could be copied off to
reveal the text underneath; that cannot happen here.) The trade-off is that the cleaned-up PDF behaves
like a scanned document: you can read it and print it, but you can no longer select or search its text.
For most legal and confidentiality purposes, this is exactly what you want.

Because each item is simply painted over with a solid box, your
[filter strategy](filter-strategies.md) choice (the replacement text) does **not** change how a
redacted PDF looks: PDFs always get solid boxes.

## Scanned PDFs

Some PDFs are just *pictures* of pages (for example, a document that was scanned), with no real text
inside, so on their own there is nothing for Philter Desktop to detect. Philter Desktop can still
handle these: when the **Read scanned PDF pages with OCR** option is on (it is by default), it
recognizes the text on scanned pages **on your own computer** (nothing is uploaded) so the sensitive
information can be found and removed. OCR is slower and is best-effort: it can miss low-quality scans
and does not read handwriting, so reviewing the result matters even more here, and the
[Modify Redaction](redacting-documents.md#adjusting-what-was-removed-modify-redaction) tools let you
cover anything it missed. You can turn this off or fine-tune it on the
[Settings → PDF](settings.md#pdf-tab) tab.

For adding files to the queue, previewing, adjusting, verifying, and reporting on a redaction, see
[Redacting Documents](redacting-documents.md).
