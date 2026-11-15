# Frequently Asked Questions

Common questions about Philter Desktop. For full detail, follow the links into the rest of the
documentation.

## How do I get Philter Desktop?

The **official, signed build** is **free to download** from [philterd.ai](https://philterd.ai),
and free for personal use and evaluation; using it for commercial purposes requires a subscription.
Because the software is open source under the Apache License 2.0, you can also read the
[source code](https://github.com/philterd/PhilterDesktop) and build it yourself. See
[Licensing & Support](licensing.md) for how the free open-source software and the paid official product
fit together.

## Do I need a license?

You need a license if you use Philter Desktop **for work**: anything connected to your job, your
business, or your clients, patients, or customers, or done for an organization, including nonprofits
and government, even if you work alone and on your own computer. **Personal use** (your own or your
household's documents, outside of work) is free, and so is **evaluation**, for as long as you need,
until you start using Philter Desktop or its redacted files in your work. See
[When you need a subscription](licensing.md#when-you-need-a-subscription) for examples.

## What do I get, and what happens after I purchase?

Subscribers receive the **same official signed build** as everyone else; there are no separate downloads
or license keys. To record your license in Philter Desktop, choose **Help → Manage License…** and enter
the email address you bought it with (see [Entering your work
license](licensing.md#entering-your-work-license)). The subscription permits **commercial use** of the
official build and covers **support** from the team that makes it and **maintained releases**. Keep your
receipt as proof of purchase. See [Licensing & Support](licensing.md).

## Do my documents leave my computer?

No. All detection and redaction happen **locally on your Windows machine**, and Philter Desktop works
fully offline. Nothing about your documents is uploaded or sent over the internet.

Philter Desktop's network use is an **update check**, plus a monthly **license check** on computers
where someone has entered a work license (see [Licensing &
Support](licensing.md#a-note-for-it-departments)). The update check downloads a small version file from
philterd.ai to see whether a new version is available. It checks when you first open its window, about
once a day while it keeps running in the background (for example, watching folders from the system
tray), and when you choose **Help → Check for Updates…**. The request tells philterd.ai which version of
Philter Desktop you have, which helps Philterd see which versions are in use. It sends nothing else: no
documents, no identifiers, and no information about what you redact. Like any web request, philterd.ai
also sees your computer's IP address. You can turn the automatic checks off on the [Notifications
tab](settings.md#notifications-tab) in Settings, and IT departments can turn them off for everyone (see
[Licensing & Support](licensing.md#a-note-for-it-departments)).

## Can you see my documents?

No. Your files stay on your own device throughout detection and redaction, so Philterd cannot access
their contents.

## Is it open source?

Philter Desktop is released under the **Apache License 2.0**, so you can audit the
[source code](https://github.com/philterd/PhilterDesktop) and build it yourself at no cost. The
official signed build is free for personal use and evaluation; a subscription is required for
commercial use and includes support and maintained releases. See
[Licensing & Support](licensing.md).

## What file types does it redact?

PDF (`.pdf`), Microsoft Word (`.docx`), plain text (`.txt`), rich text (`.rtf`), spreadsheets
(`.xlsx` and `.csv`), and email (`.eml` and `.msg`). Each type has its own page under
[Redacting Documents](redacting-documents.md).

## Can it redact scanned PDFs?

Scanned PDFs are read with **on-device OCR** that runs entirely on your computer, so the text in them
can be detected and redacted. OCR is best-effort: its accuracy depends on scan quality, and it does not
read handwriting, so reviewing the result matters even more here. See [PDF](redacting-pdf.md).

## Can I control what gets redacted?

Philter Desktop uses a configurable **policy** engine: you choose which kinds of information to detect,
add your own terms and [custom identifiers](policies.md#redacting-your-own-special-identifiers-custom-identifiers),
and decide how matches are replaced with [filter strategies](filter-strategies.md). See
[Redaction Policies](policies.md).

## Does it remove hidden data and metadata?

For Word documents, Philter Desktop can also clean up **metadata** (author, company, title, and so
on), **comments**, **tracked changes**, and **hidden text**, and for email it can strip identifying
**headers**. These cleanups are on by default and can be turned off in Settings. See
[Microsoft Word](redacting-word.md) and [Email](redacting-email.md).

## How do I know the redaction worked?

You can **preview** the result before saving, have Philter Desktop **re-scan** the finished file to
check for anything still detectable, generate a **redaction report**, and view a **before-and-after**
comparison. These tools reduce the chance of something slipping through, but automated detection is
probabilistic, so always give an important document a **human review** as well. See
[Checking the result for anything missed](redacting-documents.md#checking-the-result-for-anything-missed-leftover-check).

## Is the redaction permanent?

What Philter Desktop redacts is genuinely removed, not merely hidden from view: a redacted **PDF is
flattened to an image**, so no recoverable text layer remains, and in other formats the content is
**replaced** rather than just covered over. (This concerns what was redacted; whether everything
sensitive was caught is a separate question, so still review the result.) Your original file is never
changed: the result is always written to a new copy. See [PDF](redacting-pdf.md).

## Can I automate redaction?

You can use [watched folders](watched-folders.md), the
[command line](redacting-documents.md#for-advanced-users-and-it-redacting-from-a-command-line), or the
[Windows Explorer right-click menu](settings.md#explorer-right-click-menu) for batch or individual
processing.

## Is my data stored securely on my computer?

Philter Desktop's local data is **encrypted at rest** and tied to your Windows account on that
machine, with optional passphrase protection for stronger control. See
[Settings → Security](settings.md#security-tab-protecting-your-stored-information).

## What are the system requirements?

Windows 10 (version 1809, build 17763) or later, or Windows 11, 64-bit. See
[Getting Started](getting-started.md).
