# Settings

Open **Settings** from the main toolbar to control where redacted files go and how the
application behaves. The dialog has two tabs: **General** and **Watched Folder**.

## General

The **General** tab holds the output-location, logging, and Explorer right-click-menu settings below.

## Output location

Choose where redacted copies are written:

- **Original location** — write the redacted file next to the source document.
- **Custom folder** — write all redacted files to a folder you specify.

Redacted files are always written as a **new copy**; your originals are never modified.

The **Redacted file name suffix** is added to the copy's name (before the extension). It defaults to
**`_redacted-draft`** — for example `report.docx` → `report_redacted-draft.docx`. The default is
deliberately *not* just `_redacted`: redaction is statistical and can miss things, so the name should
not imply the file is verified safe. The "draft" wording is a reminder to **review the output before
sharing it**. You can change the suffix to suit your own convention (an empty value resets it to the
default).

## Logging

Enable **logging** to record application activity to a log file (useful for troubleshooting). Use
**Open Log File** to view it. Logging is off by default.

## Explorer right-click menu

Enable **Add "Redact with Philter Desktop" to the Explorer right-click menu** to put a redaction
command on the context menu for `.pdf`, `.docx`, and `.txt` files in Windows Explorer.

With it on, select one or more of those files, right-click, and choose **Redact with Philter
Desktop**. A dialog lists the selected files and lets you pick the **policy** and **context** to use
(and whether to highlight Word redactions). Clicking **Redact** adds the files to the Philter Desktop
[redaction queue](redacting-documents.md), where they are processed like any other queued document —
Philter Desktop starts automatically if it isn't already running. Selecting **several files at once**
opens a single dialog for the whole selection.

Turning the option **on** writes the entries to your per-user registry; turning it **off** removes
them. The setting takes effect immediately (no need to click Save), and uninstalling Philter Desktop
always removes the entries.

The option is off by default, and it is unavailable in Microsoft Store (MSIX) builds, where Explorer
integration is managed by the package instead. (For scripting or to redact without the dialog, use
the [command line](redacting-documents.md#command-line-headless-redaction) directly.)

## Watched folders

The **Watched Folder** tab lets you set up folders that Philter Desktop continuously monitors and
redacts automatically, and turn on starting at sign-in. Philter Desktop runs from the system tray
so monitoring continues even when the window is closed. See the dedicated
[Watched Folders](watched-folders.md) page for full details.

## Data storage and encryption

Philter Desktop keeps its policies, contexts, settings, the redaction queue, and the saved
redaction history (including the detected text from [Modify Redaction](redacting-documents.md#modifying-a-redaction))
in a local database under your user profile (`%LocalAppData%\PhilterDesktop\`).

Because that history can contain personal data, the database is **encrypted at rest** (AES). The
encryption key is generated on first run and protected with **Windows DPAPI** scoped to your user
account, so the database can only be opened by **you, on that machine** — no password to enter, and
the key file is useless if copied elsewhere. An existing database from an earlier version is
encrypted automatically the first time the updated app runs.

> This protects the data from being read off disk by another user or from a stolen copy of the file.
> It does not protect against software running as your own Windows account.

To delete all saved redaction history, use **File → Clear Redaction History…** on the main window.
(This removes the stored versions and spans, including the detected text; it does not delete any
redacted output files already written to disk.)

## Word redaction

Redacting Microsoft Word (`.docx`) documents uses the open-source
[Open XML SDK](https://github.com/dotnet/Open-XML-SDK). No license key or third-party component is
required — all supported formats (`.txt`, `.docx`, `.pdf`) redact out of the box.
