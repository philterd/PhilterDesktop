# Email

Philter Desktop redacts email files that end in **`.eml`** (the standard email format used by most mail
programs) and **`.msg`** (the format Microsoft Outlook uses when you save or drag out a message).

## What gets redacted

When Philter Desktop redacts an email, it cleans up the **subject line**, the **From / To / Cc**
addresses, and the **message body**, according to your policy, the same way it handles any other
document. This covers the plain-text and HTML versions of the body; an Outlook message whose body is
only **rich text (RTF)** is recovered as text and redacted too, rather than being dropped. If the email
**forwards or embeds another message** (a `message/rfc822` part), that nested message is redacted the
same way — its own **Subject, From / To / Cc**, and body are cleaned, and (when the header options below
are on) its technical and identity headers are stripped too. It also (by default) strips the **technical headers** that
would otherwise reveal the sender's IP, mail program, and the server delivery trail, and the **common
identity headers** (**Bcc** for blind-copy recipients, **Reply-To**, **Sender**, and **Resent-…**) that
aren't part of the visible From / To / Cc fields and so wouldn't otherwise be redacted. Both are on by
default and can be turned off in [Settings → Email](settings.md#email-tab). If you turn off the identity-
header option, those headers (including **Bcc**) are **kept as-is and not redacted**, so their addresses
will remain in the output — review them before sharing.

You can also choose to **remove the Date header** (the email's send time). This is **off by default** —
the send date is usually wanted and isn't personally identifying on its own — but if you need to remove
it, turn it on in [Settings → Email](settings.md#email-tab). When on, the Date header is **dropped
outright**, so the send time is removed no matter how it is formatted (this does not rely on the policy
recognizing a date). A timestamp can still appear inside the delivery trail; leave the technical-header
option on to strip that too.

!!! warning "Attachments and images are not redacted"
    Philter Desktop redacts the email **message itself**: the subject, the addresses, and the body text.
    It does **not** open, inspect, or redact **attachments** (a PDF, Word file, spreadsheet, image, or
    anything else carried along with the email), and it does **not** read text inside **images** —
    including **inline images** embedded in the body, such as a logo, a scanned signature, or a pasted
    screenshot. By default both are copied through **unchanged**, so sensitive information inside an
    attachment, or shown only inside an image, is left exactly as it was. If an email has attachments
    that need redacting, **save each one out as its own file and redact it separately** (a `.pdf`,
    `.docx`, or `.txt` attachment can go straight through Philter Desktop). Always review the finished
    email **and** its attachments before sharing it.

    If you would rather the redacted email carry **no attachments at all**, turn on **Remove attachments
    from redacted email** in [Settings → Email](settings.md#email-tab). When on, attachments are
    **deleted entirely — not redacted**: their content is never inspected, and the attached files (and
    their filenames, which can themselves reveal information such as `john_smith_ssn.pdf`) are removed
    from the output. This option is **off by default**. Underneath it, **Also remove inline images**
    (available only when the option above is on) does the same for the pictures embedded in the body:
    they are deleted and their `cid:` references are neutralized. Leave it off to keep images such as a
    corporate logo.

    Whenever a redacted email still carries **attachments or inline images**, **verification adds a
    warning** reminding you that their content wasn't inspected — so review the original before sharing.

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
