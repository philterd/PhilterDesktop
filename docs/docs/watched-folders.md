# Watched Folders (Automatic, Hands-Off Redaction)

A **watched folder** is a folder that Philter Desktop keeps an eye on for you. Whenever a new
`.txt`, `.docx`, or `.pdf` file shows up in that folder, Philter Desktop notices it, redacts it
automatically, and saves the cleaned-up copy to an output folder you've chosen — all without you
having to add anything to the queue by hand.

This turns redaction into a "drop box" that runs itself. For example, you could point a watched folder
at the place where your scanner saves documents, at your Downloads folder, or at a shared network
folder where colleagues drop files — and from then on, **everything that lands there gets cleaned up
automatically**. It's ideal for a steady stream of documents that all need the same treatment.

## Setting up a watched folder

Open **Settings** from the main toolbar, go to the **Watched Folder** tab, and click **Add…**. You'll
be asked to fill in a few things:

- **Folder to watch** — the folder you want Philter Desktop to monitor for new files.
- **Policy** — the [policy](policies.md) (set of rules) that decides what to remove and how, for
  files in this folder.
- **Context** — the [context](contexts.md) (consistency setting) to use for these files.
- **File types** — which of **PDF (.pdf)**, **Word (.docx)**, and **Text (.txt)** you want redacted
  in this folder. You must pick at least one; any other kinds of files in the folder are simply
  ignored.
- **Highlight redactions in Word (.docx) documents** — when checked, the replacements in cleaned-up
  Word documents are highlighted, making them easy to spot when you review the file.
- **Show a notification when a file is redacted** — when checked, a small pop-up appears near the
  clock as files in this folder are cleaned up (several at once are combined into one summary, and
  failures are reported too). Clicking the pop-up opens the output folder. These pop-ups are held back
  while the main Philter Desktop window is open and in front of you, so they don't get in your way.
  Because this is set per folder, you can keep a busy folder quiet while still being notified about
  others.
- **Include subfolders** — when checked, files inside folders *within* the watched folder are
  monitored too, and the cleaned-up output mirrors that folder structure (so two files with the same
  name in different subfolders don't overwrite each other). If you turn this on, the output folder
  must be **outside** the watched folder. Hidden and system files and folders are skipped.
- **Output folder** — where the cleaned-up copies are saved. This must be a **different** folder from
  the one being watched (otherwise the cleaned-up files would themselves look like new files to
  redact).

Each watched folder has its **own** policy, context, highlight setting, and output folder. That means
you can watch several folders at once, each with completely different rules — for instance, one folder
for medical records and another for financial documents, each handled its own way.

To change a watched folder's settings later, select it and click **Edit…**. To stop watching a folder,
select it and click **Remove**. Any changes you make to the watched-folder list take effect right
away.

## How the automatic redaction behaves

A few details worth knowing about how watching works:

- **Which files are handled:** only `.txt`, `.docx`, and `.pdf` files are redacted; everything else
  in the folder is left alone.
- **What the copies are named:** cleaned-up copies are saved to the output folder with the usual
  label (by default `_redacted-draft`, so `invoice.pdf` becomes `invoice_redacted-draft.pdf`). Your
  originals are never changed.
- **Files already in the folder:** when Philter Desktop starts up, it also picks up files that were
  *already* sitting in a watched folder — not just files added afterward.
- **No redacting the same file twice:** the cleaned-up output files are themselves ignored, so they're
  never redacted again, and a file that's already been redacted is skipped unless it changes.
- **Large or still-arriving files:** if a file is still being copied or downloaded, Philter Desktop
  waits until it has fully finished arriving before redacting it, so it never works on a half-written
  file.

## The activity log

Every watched folder keeps its own **activity log** so you can see exactly what's been happening and
confirm nothing was missed. On the **Watched Folder** tab, select a folder and click **View Log…**.
With timestamps, the log shows:

- when a file was **found**,
- when it was **redacted**, and **where** the cleaned-up copy was saved,
- when a file was **skipped** (because it had already been redacted), and
- any **errors** (shown in red).

The log window has a **Refresh** button to load the latest activity and a **Clear Log** button to
empty it. Entries stay until you clear the log or remove the folder, and anything older than **30
days** is removed automatically to keep the log tidy.

## Working quietly in the background (the system tray)

So that it can keep watching your folders without a window cluttering your screen, Philter Desktop can
tuck itself away into the **Windows system tray** — the row of small icons near the clock:

- **Closing the window** (clicking the **X**) does **not** quit the program; it just hides it to the
  tray, and watching keeps running. The first time this happens, a pop-up explains it so you're not
  caught off guard.
- **Double-click the tray icon** to bring the main window back.
- **Right-click the tray icon** for a short menu:
    - **Open Philter Desktop** — reopen the window.
    - **Pause watching / Resume watching** — temporarily stop or restart automatic monitoring.
    - **Exit** — fully close the program and stop watching.

As long as Philter Desktop is running — even when it's only sitting in the tray — all of your watched
folders are being monitored.

## Starting automatically when you sign in

To have Philter Desktop watch your folders even after a restart, turn on **Start Philter Desktop at
sign-in** on the **Watched Folder** tab. After that, it launches on its own whenever you sign in to
Windows and picks up watching right where it left off.

- In the version you run yourself, this is a simple on/off setting you control with that checkbox.
- In the **Microsoft Store** version, automatic startup is managed by Windows. The checkbox shows you
  the current state, but to turn it on or off you go to **Task Manager → Startup apps**.

## Things to keep in mind

- Watching only happens while **you are signed in** to Windows. For unattended, always-on redaction on
  a server (running even when nobody is logged in), you would need a Windows service, which Philter
  Desktop does not currently provide.
- The output folder must always be different from the folder being watched.
- Just like [PDFs you redact by hand](redacting-documents.md), redacted PDFs from a watched folder are
  flattened to images, so the removed text is truly gone and cannot be recovered.
