# Plain Text and Rich Text

Philter Desktop redacts two kinds of plain and lightly formatted text documents:

- **Plain text (`.txt`)**: simple, unformatted text, like a Notepad file.
- **Rich Text (`.rtf`)**: a formatted-text format used by WordPad and many legal and records systems.

## Plain text (`.txt`)

A `.txt` file is redacted directly: the sensitive information is removed from the text and the
cleaned-up copy is saved as a new `.txt` file. There is no formatting, metadata, or hidden content to
consider.

## Rich Text (`.rtf`)

A redacted `.rtf` is **rebuilt from its main body**, so document metadata it carried (such as the author
or title) is dropped automatically, any embedded objects (such as an attached spreadsheet or Word file)
are removed so they cannot carry unredacted content into the output, and any **reviewer comments**
(annotations) are removed rather than being merged into the text. The cleaned-up copy is saved as a new
`.rtf` file that keeps the body's formatting.

!!! warning "Headers, footers, and footnotes"
    RTF redaction works on the document **body**. Content in other parts — such as **headers, footers,
    and footnotes** — may not be carried into the redacted `.rtf`, so **review the result** rather than
    assuming those parts came through. If a document keeps important information in headers, footers, or
    footnotes, save it as a **`.docx`** and redact that instead, which covers those parts. When Philter
    Desktop notices an `.rtf` with these parts, it flags the redaction for review (in the queue's
    Verification column, the watched-folder log, and on the command line).

For adding files to the queue, previewing, adjusting, verifying, and reporting on a redaction, see
[Redacting Documents](redacting-documents.md).
