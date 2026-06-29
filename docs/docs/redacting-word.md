# Microsoft Word

Philter Desktop redacts Microsoft Word documents that end in **`.docx`**. The sensitive information is
removed from the text and the cleaned-up copy is saved as a new `.docx` file.

## Hidden information is cleaned up too

For Word (`.docx`) files, Philter Desktop also cleans up hidden information in the redacted copy by
default: the document's **metadata** (author, company, title, keywords, custom properties), reviewer
**comments**, **tracked changes**, and **hidden text**, so a "redacted" file doesn't quietly leak
through any of those channels. You can control each of these on the
[Settings → Microsoft Office](settings.md#microsoft-office-tab) tab.

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
