# Microsoft Word

Philter Desktop redacts Microsoft Word documents that end in **`.docx`**. The sensitive information is
removed from the text and the cleaned-up copy is saved as a new `.docx` file.

Redaction covers the document body and tables, headers and footers, footnotes and endnotes, comments,
and the text inside **shapes, text boxes, SmartArt, and charts**. Text a reviewer **deleted with
tracked changes** is scanned too, so detected information there is redacted rather than lingering in the
file where *Reject Changes* could bring it back.

**Charts** are scanned for their titles, labels, and the **cached data values** a chart stores (its copy
of the plotted series and category values), which is on by default and controlled in
[Settings → Microsoft Office](settings.md#microsoft-office-tab). Because a chart is only scanned as text
through your policy, a sensitive value it's built from is removed only when the policy detects it, and
redacting a cached value can change how the chart looks — so **review any charts** in the redacted copy.

Header and footer scanning covers the **text** that repeats at the top and bottom of each page (for
example "Confidential — John Doe"); it does not remove images or logos placed in a header/footer. It is
on by default and can be turned off in [Settings → Microsoft Office](settings.md#microsoft-office-tab).

Philter Desktop also checks the **address a hyperlink points to**, not just its visible text. If a
link's target holds sensitive information — an email address in a `mailto:` link, a name or ID in an
intranet or `file://` address — that target is neutralized so it can't ship in the document even though
you only see the link's wording on the page. Links whose targets contain nothing sensitive are left
working as they were.

The same applies to **field codes** — the hidden instructions behind fields such as `HYPERLINK`,
`INCLUDETEXT`, and mail-merge sources. A field keeps an instruction (for example the `mailto:` address
or a file path with a name in it) separately from the result you see on the page, so sensitive
information there is scanned and removed while the field itself is left in place. Only the sensitive text
inside the instruction is replaced; a field with nothing sensitive (such as a page number or date) is
untouched.

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
