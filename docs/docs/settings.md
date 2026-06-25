# Settings

Open **Settings** from the main toolbar to control where your cleaned-up files are saved and how the
program behaves. The Settings window is divided into three tabs: **General**, **Watched Folder**, and
**Security**. This page walks through each of them.

## General tab

The **General** tab is where you set the output location (where cleaned-up files go), turn logging on
or off, and switch on the Windows Explorer right-click menu. Each of these is explained below.

## Where your cleaned-up files are saved (Output location)

You choose where Philter Desktop puts the cleaned-up copies it creates:

- **Original location** — the cleaned-up copy is saved in the **same folder** as the document it came
  from.
- **Custom folder** — every cleaned-up copy is saved into **one folder that you pick**, no matter
  where the original came from. This is handy if you want all your redacted output collected in a
  single, well-known place.

Either way, the cleaned-up file is always a **new copy** — your original document is never changed.

You can also set the **Redacted file name suffix** — the label that's added to the end of each
cleaned-up file's name (just before the file extension). By default it is **`_redacted-draft`**, so
`report.docx` becomes `report_redacted-draft.docx`.

Why "draft," and not simply "redacted"? Because **automatic redaction can miss things.** No automated
tool is perfect, and the law often treats these documents as high-stakes. Naming the file a "draft"
is a built-in reminder that the file still needs **your review** before you share it — it should never
be assumed safe just because the computer produced it. You're welcome to change the suffix to match
your own naming habits. (If you ever clear the box and leave it empty, it resets to the default.)

## Logging

Turning on **logging** tells Philter Desktop to keep a running record of what it does in a log file.
This is mainly useful if something goes wrong and you (or technical support) need to look into it. The
**Open Log File** button shows you the log. Logging is **off** by default, and you can safely leave it
off for normal use.

Use **Clear Log File** to permanently delete that log. The log records application activity for
troubleshooting (errors and the names of files that were processed) — it does **not** contain the
detected or redacted text from your documents — but since the file names themselves can be sensitive,
you can wipe it whenever you like. A new log starts the next time something is logged.

## Explorer right-click menu

This option lets you redact files **straight from a Windows folder**, without opening Philter Desktop
first.

Turn on **Add "Redact with Philter Desktop" to the Explorer right-click menu**. Once it's on, you can
go to any folder, select one or more `.pdf`, `.docx`, or `.txt` files, **right-click** them, and
choose **Redact with Philter Desktop**. A small window appears listing the files you picked and
letting you choose the **policy** and **context** to use (and whether to highlight redactions in Word
documents). When you click **Redact**, the files are handed to Philter Desktop's
[redaction queue](redacting-documents.md) and processed just like any other document — and Philter
Desktop starts up on its own if it isn't already running. If you select several files at once, you get
a single window for the whole group.

A few practical notes:

- Switching this option **on** adds the right-click command for your user account; switching it
  **off** removes it again. The change happens **immediately** — there's no need to click a separate
  Save button.
- Uninstalling Philter Desktop always cleans up the right-click command for you.
- The option is **off** by default.
- If you're a technical user who'd rather automate redaction without the pop-up window, you can use
  the [command line](redacting-documents.md#for-advanced-users-and-it-redacting-from-a-command-line)
  directly.

## Watched Folder tab

The **Watched Folder** tab lets you set up folders that Philter Desktop **watches automatically** —
any document dropped into a watched folder is redacted on its own, without you adding it to the queue
by hand. This tab is also where you turn on having Philter Desktop start automatically when you sign
in to Windows, and it lets the program keep working quietly from the system tray (the small icons near
the clock) even when its window is closed. This feature has its own detailed page; see
[Watched Folders](watched-folders.md).

## Security tab — protecting your stored information

This tab is worth understanding if you handle confidential material, because it's about keeping the
information Philter Desktop stores on your computer safe. The explanations below go into some detail on
purpose.

### What Philter Desktop stores, and why it's protected

To do its job, Philter Desktop keeps some information on your computer: your policies, your contexts,
your settings, the list of documents in your queue, and the **redaction history** (the record of what
was redacted, which — through the [Modify Redaction](redacting-documents.md#adjusting-what-was-removed-modify-redaction)
feature — can include the actual sensitive text that was found). All of this lives in a small private
file in your personal Windows profile.

Because that history can contain personal information, Philter Desktop **scrambles the file so it
can't be read by anyone who simply opens it.** (The technical term is that the file is **encrypted at
rest** — meaning it's stored in a locked, unreadable form whenever it's sitting on your disk.) The
"key" that unlocks it is created automatically the first time you run the program and is then locked
to **your Windows account on that specific computer**, using a protection feature built into Windows
itself. In plain terms: by default you don't have to enter any password, but the file can only be
opened by **you, signed in to your own account, on your own machine** — a copy of the file taken to
another computer would be useless. If you're upgrading from an older version, your existing data is
locked down automatically the first time the new version runs.

> This protects your data from someone reading it off the disk directly, or from a stolen copy of the
> file. It does **not** protect against other software that's already running under your own Windows
> account — for that, use the passphrase option described next.

### Adding a passphrase for stronger protection

By default, your stored information is tied to your Windows account, as just described. For an extra
layer of protection — one that even other software running as *you* can't get past — the **Security**
tab lets you require a **passphrase** (a password) before the program will open:

- Check **Require a passphrase to open the database** and choose a passphrase (at least 8 characters,
  typed twice to confirm). From then on, Philter Desktop will ask you for it every time it starts.
- Use **Change Passphrase…** to change it later (you'll confirm the current one first).
- Uncheck the box to remove passphrase protection and go back to the standard Windows-account
  protection.

How your passphrase is handled is important: **the passphrase itself is never saved anywhere.** The
program only keeps enough scrambled information to *check* that what you type is correct — it never
stores the passphrase in a form anyone could read. Turning the passphrase on or off, or changing it,
takes effect **instantly** and never requires re-processing your data, so it's safe to switch on or
off whenever you like.

> **Please choose your passphrase carefully and store it somewhere safe.** If you forget it, **there
> is no way to recover your stored information** — that's the whole point of strong protection, but it
> means the responsibility is yours. Also note that, because the program must be unlocked before it
> can do anything, you'll be asked for the passphrase even when Philter Desktop starts automatically
> at sign-in (which keeps your watched folders working).

### Clearing your saved redaction history

If you want to wipe the stored history of what's been redacted, use **File → Clear Redaction History…**
from the main window. This erases the saved versions and redaction lists — including any sensitive
text they captured — and **removes the completed documents from the queue list**. It does **not**
delete the cleaned-up files you've already saved to disk; those remain wherever you saved them, and any
documents still being processed are left in place.
