# Plain Text and Rich Text

Philter Desktop redacts two kinds of plain and lightly formatted text documents:

- **Plain text (`.txt`)**: simple, unformatted text, like a Notepad file.
- **Rich Text (`.rtf`)**: a formatted-text format used by WordPad and many legal and records systems.

## Plain text (`.txt`)

A `.txt` file is redacted directly: the sensitive information is removed from the text and the
cleaned-up copy is saved as a new `.txt` file. There is no formatting, metadata, or hidden content to
consider.

## Rich Text (`.rtf`)

A redacted `.rtf` is **rebuilt from its visible content**, so document metadata it carried (such as the
author or title) is dropped automatically, and any embedded objects (such as an attached spreadsheet or
Word file) are removed so they cannot carry unredacted content into the output. The cleaned-up copy is
saved as a new `.rtf` file with its formatting preserved.

For adding files to the queue, previewing, adjusting, verifying, and reporting on a redaction, see
[Redacting Documents](redacting-documents.md).
