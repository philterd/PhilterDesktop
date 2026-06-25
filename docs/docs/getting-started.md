# Getting Started

This page walks you through installing Philter Desktop and redacting your first document. You do not
need any technical background to follow it — if you can install a normal Windows program and open a
file, you can do everything here.

## What you need

- A computer running **Windows 10 or Windows 11**.

That's the whole list. There is no separate license to buy, no password or product key to type in,
and no add-ons to install. All three document types — plain text, Microsoft Word, and PDF — work
straight out of the box.

## Installing Philter Desktop

Philter Desktop is delivered as a single installer file. (The file ends in `.msix`, which is simply
Microsoft's modern format for installing Windows programs — you can treat it like any other installer
you've double-clicked before.)

To install:

1. Get the Philter Desktop installer file from whoever provides it to you (your IT department, your
   firm's software library, or the official download).
2. **Double-click** the file.
3. Click **Install** when Windows asks.

That's it. Philter Desktop will appear in your Start menu like any other program.

> **If Windows shows a warning about an "untrusted publisher":** this is a normal security check, not
> a sign that anything is wrong. It just means Windows hasn't yet been told to trust the digital
> signature on this particular copy of the installer. If you see this, your IT department needs to
> mark the installer's certificate as trusted on your computer before the installation will proceed.
> If you installed Philter Desktop from an official, properly signed source, you will not see this
> message at all. When in doubt, check with whoever gave you the file.

## Your first time opening the program

When you open Philter Desktop, you'll see its main window. The top of the window has a row of buttons
(a **toolbar**), and most of the window is taken up by the **redaction queue** — the list of
documents you've asked it to clean up. The first time you open the program, this list is empty,
because you haven't given it anything to do yet.

To make things easy, Philter Desktop already creates a starter **policy** named **default** (the set
of rules for what to remove) and a starter **context** named **default** (the setting that keeps
replacements consistent). Because these are ready to go, **you can redact a document immediately**,
without setting anything up first.

## Redacting your first document

1. Click the **Redact** button on the toolbar (or simply **drag a document from a folder and drop it
   onto the window**).
2. Watch the document's row in the list. Its status will move along by itself: from **Pending**
   (waiting its turn), to **Processing** (being worked on), to **Completed** (finished).
3. When it says **Completed**, your cleaned-up copy is ready. You can open it right away by
   right-clicking the row and choosing **Open redacted file**, or find it in the folder where Philter
   Desktop saves its output (see [Settings](settings.md)).

Remember: the cleaned-up copy is a **brand-new file**. Your original document is left exactly as it
was. Always open the cleaned-up copy and read through it before you send it to anyone.

## Where to go from here

Once you're comfortable with the basics, you can fine-tune how Philter Desktop works:

- Learn how to control **what** gets removed and **how** it's replaced by creating and editing
  [policies](policies.md).
- Adjust [settings](settings.md) such as **where** your cleaned-up files are saved and what they're
  named.
- Set up [watched folders](watched-folders.md) so that documents dropped into a particular folder are
  cleaned up automatically, without you lifting a finger.
