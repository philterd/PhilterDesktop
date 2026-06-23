# Settings

Open **Settings** from the main toolbar to control where redacted files go and how the
application behaves. The dialog has two tabs: **General** and **Watched Folder**.

## General

The **General** tab holds the output-location and logging settings below.

## Output location

Choose where redacted copies are written:

- **Original location** — write the redacted file next to the source document.
- **Custom folder** — write all redacted files to a folder you specify.

Redacted files are always written as a new copy with a `_redacted` suffix; your originals are
never modified.

## Logging

Enable **logging** to record application activity to a log file (useful for troubleshooting). Use
**Open Log File** to view it. Logging is off by default.

## Watched folders

The **Watched Folder** tab lets you set up folders that Philter Desktop continuously monitors and
redacts automatically, and turn on starting at sign-in. Philter Desktop runs from the system tray
so monitoring continues even when the window is closed. See the dedicated
[Watched Folders](watched-folders.md) page for full details.

## Word redaction license

Redacting Microsoft Word (`.docx`) documents uses
[Xceed Words for .NET](https://xceed.com/), which requires a license key. Plain-text redaction
does **not** require a license.

If a Word document is redacted without a license configured, redaction still runs but in the
library's **trial mode**. Provide your license key in one of these ways:

- Place an `xceed-license.json` file next to the application, containing:
  ```json
  { "XceedLicenseKey": "your-key-here" }
  ```
- Or set the `XCEED_LICENSE_KEY` environment variable.

Installed builds of Philter Desktop typically include the key already; this is only relevant if
you build or run the application yourself.
