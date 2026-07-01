# Email

Philter Desktop redacts email files that end in **`.eml`** (the standard email format used by most mail
programs) and **`.msg`** (the format Microsoft Outlook uses when you save or drag out a message).

## What gets redacted

When Philter Desktop redacts an email, it cleans up the **subject line**, the **From / To / Cc**
addresses, and the **message body**, according to your policy, the same way it handles any other
document. This covers the plain-text and HTML versions of the body; an Outlook message whose body is
only **rich text (RTF)** is recovered as text and redacted too, rather than being dropped. It also (by default) strips the **technical headers** that
would otherwise reveal the sender's IP, mail program, and the server delivery trail, and the **common
identity headers** (**Bcc** for blind-copy recipients, **Reply-To**, **Sender**, and **Resent-…**) that
aren't part of the visible From / To / Cc fields and so wouldn't otherwise be redacted. Both are on by
default and can be turned off in [Settings → Email](settings.md#email-tab). If you turn off the identity-
header option, those headers (including **Bcc**) are **kept as-is and not redacted**, so their addresses
will remain in the output — review them before sharing.

!!! warning "Attachments are not redacted"
    Philter Desktop redacts the email **message itself**: the subject, the addresses, and the body. It
    does **not** open, inspect, or redact **attachments** (a PDF, Word file, spreadsheet, image, or
    anything else carried along with the email). Those are copied through **unchanged**, so any
    sensitive information inside an attachment is left exactly as it was. If an email has attachments
    that need redacting, **save each one out as its own file and redact it separately** (a `.pdf`,
    `.docx`, or `.txt` attachment can go straight through Philter Desktop). Always review the finished
    email **and** its attachments before sharing it.

## Outlook `.msg` files become `.eml`

Outlook `.msg` files are saved in a different format after redaction: **a redacted `.msg` is saved as
an `.eml` file.** Redacting `message.msg` therefore produces `message_redacted-draft.eml`, not a `.msg`.

The format changes because `.msg` is Microsoft's own private, undocumented Outlook format. Philter
Desktop can reliably **read** a `.msg` and extract everything it needs to redact, but it cannot
guarantee that a rebuilt `.msg` file would faithfully preserve every part of the message. Rather than
produce a `.msg` that might be subtly corrupted, it writes the redacted result as **`.eml`**, the
universal email format. An `.eml` file is a complete, standalone copy of the email that **opens in
Outlook** (double-click it) as well as in Apple Mail, Thunderbird, Gmail's import, and essentially
every other mail program. Nothing is lost in the redaction itself; only the container file type
changes.

`.eml` files redact to `.eml`, and everything else keeps its original file type as usual; only `.msg`
changes.

For adding files to the queue, previewing, adjusting, verifying, and reporting on a redaction, see
[Redacting Documents](redacting-documents.md).
