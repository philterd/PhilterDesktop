# Microsoft Word

Philter Desktop redacts Microsoft Word documents that end in **`.docx`**. The sensitive information is
removed from the text and the cleaned-up copy is saved as a new `.docx` file.

Redaction covers the document body and tables, headers and footers, footnotes and endnotes, comments,
and the text inside **shapes, text boxes, SmartArt, and chart labels**. (One narrow exception: a chart's
underlying *data values* — the numbers and category names a chart is built from — aren't scanned; if a
chart is plotted directly from sensitive values, review or remove it by hand.)

Philter Desktop also checks the **address a hyperlink points to**, not just its visible text. If a
link's target holds sensitive information — an email address in a `mailto:` link, a name or ID in an
intranet or `file://` address — that target is neutralized so it can't ship in the document even though
you only see the link's wording on the page. Links whose targets contain nothing sensitive are left
working as they were.

## Hidden information is cleaned up too

For Word (`.docx`) files, Philter Desktop also cleans up hidden information in the redacted copy by
default: the document's **metadata** (author, company, title, keywords, custom properties, and the
custom XML data stores that back data-bound fields — so a field's value can't linger or refresh after
redaction), reviewer **comments**, **tracked changes**, and **hidden text**, so a "redacted" file
doesn't leak through any of those channels. You can control each of these on the
[Settings → Microsoft Office](settings.md#microsoft-office-tab) tab.

If you choose to **keep** comments rather than remove them, their text is still redacted like the rest
of the document, and — when **Remove document metadata** is on — the reviewer names on them are
anonymized (for example, to "Reviewer 1"). Either way, a comment can't ship the sensitive text or the
reviewer's identity.

## Highlighting redactions

When you redact Word documents (for example, with **Redact Folder…** or in **Redact with Preview**),
you can optionally **highlight redactions**, which marks the replacements so they are easy to spot
during review.

!!! note "Password-protected Word and Excel files"
    A Word or Excel file that has been **protected with a password** (encrypted) cannot be opened for
    redaction. If you try, Philter Desktop tells you so clearly. To redact one, open it in Word or
    Excel, remove the password (**File → Info → Protect Document / Protect Workbook**), save it, and
    then redact the unprotected copy.

For adding files to the queue, previewing, adjusting, verifying, and reporting on a redaction, see
[Redacting Documents](redacting-documents.md).
