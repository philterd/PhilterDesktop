# Watched Folders

A **watched folder** is a folder that Philter Desktop continuously monitors. When a new `.txt`,
`.docx`, or `.pdf` file appears in it, Philter Desktop automatically redacts the file and writes the
redacted copy to that folder's output directory — no need to add files to the queue by hand.

This is ideal for "drop box" workflows: point a watched folder at a scan location, a downloads
folder, or a shared network folder, and every document that lands there is redacted automatically.

## Adding a watched folder

Open **Settings** from the main toolbar and select the **Watched Folder** tab, then click
**Add…**. The dialog asks for:

- **Folder to watch** — the folder to monitor for new files.
- **Policy** — the [policy](policies.md) that decides which PII to redact and how.
- **Context** — the [redaction context](contexts.md) used for consistent replacements.
- **File types** — which of **PDF (.pdf)**, **Word (.docx)**, and **Text (.txt)** to redact in this
  folder. At least one must be selected; other file types in the folder are ignored.
- **Highlight redactions in Word (.docx) documents** — when checked, replacement text in redacted
  Word documents is highlighted for easier review.
- **Show a notification when a file is redacted** — when checked, a system-tray notification appears
  as files in this folder are redacted (bursts are combined into a single summary; failures are
  reported too). Clicking it opens the output folder. Notifications are suppressed while the main
  window is open and in front. This is set per folder, so you can keep noisy folders quiet.
- **Include subfolders** — when checked, files in subfolders are watched too, and the redacted
  output mirrors the source subfolder structure under the output folder (so files with the same name
  in different subfolders don't collide). When this is on, the output folder must be **outside** the
  watched folder. Hidden and system files/folders are skipped.
- **Output folder** — where the redacted copies are written. This must be **different** from the
  watched folder.

Each watched folder has its own policy, context, highlight setting, and output folder, so you can
monitor several folders with different rules at once.

To change a watched folder's settings later, select it and click **Edit…**. To stop monitoring a
folder, select it and click **Remove**. Changes to the watched-folder list take effect immediately.

## How redaction works

- **File types:** only `.txt`, `.docx`, and `.pdf` files are redacted; other files are ignored.
- **Output naming:** redacted copies are written to the output folder with the configured suffix
  (default `_redacted-draft`, e.g. `invoice.pdf` becomes `invoice_redacted-draft.pdf`). Your
  originals are never modified.
- **Existing files:** files already present in a watched folder when Philter Desktop starts are
  picked up too, not just files added afterward.
- **No re-redaction:** the redacted output files are ignored, so they are never redacted again, and
  a file that has already been redacted is skipped unless it changes.
- **Large files:** Philter Desktop waits until a file has finished being written (copied or
  downloaded) before redacting it.

## Activity log

Each watched folder keeps its own **activity log** so you can confirm what happened. Select a
folder on the **Watched Folder** tab and click **View Log…** to see, with timestamps:

- when a file was **found**,
- when it was **redacted** and **where** the redacted copy was written,
- when a file was **skipped** (already redacted), and
- any **errors** (shown in red).

The dialog has **Refresh** to re-read the latest activity and **Clear Log** to empty it. Entries are
timestamped and kept until you clear the log or remove the folder; entries older than **30 days** are
removed automatically.

## Running in the background (system tray)

So that monitoring can continue without keeping a window open, Philter Desktop runs from the
**Windows system tray**:

- **Closing the window** (the **X**) hides Philter Desktop to the tray instead of exiting —
  watching keeps running. The first time this happens, a notification explains it.
- **Double-click the tray icon** to reopen the main window.
- **Right-click the tray icon** for:
    - **Open Philter Desktop** — reopen the window.
    - **Pause watching / Resume watching** — temporarily stop or restart monitoring.
    - **Exit** — fully close the application and stop watching.

While Philter Desktop is running (even hidden in the tray), all watched folders are monitored.

## Starting automatically at sign-in

To have Philter Desktop watch your folders across reboots, enable **Start Philter Desktop at
sign-in** on the **Watched Folder** tab. It then launches automatically when you sign in and
resumes watching.

- On a build you run yourself, this is a per-user setting you control with that checkbox.
- On an **installed (MSIX) build**, auto-start is managed by Windows. The checkbox shows the current
  state; turn it on or off under **Task Manager → Startup apps**.

## Notes and limitations

- Watching runs only while you are **signed in** to Windows. For unattended, always-on redaction on
  a server, a Windows service would be required (not currently provided).
- The output folder must be different from the watched folder.
- Redacted PDFs are image-based (the output has no recoverable text layer), the same as
  [manual PDF redaction](redacting-documents.md).
