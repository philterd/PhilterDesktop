# Policies

A **policy** is simply a **saved set of rules** that tells Philter Desktop two things: *which kinds of
sensitive information to look for*, and *how to replace each kind* when it's found. You can think of a
policy as a recipe, or as a checklist of "remove this, remove that." Every document you redact is
handled according to whichever policy you've chosen for it.

You don't have to build a policy before you start — a ready-made policy named **default** is created
for you, and it works well for everyday redaction. But as your needs become more specific, you'll
likely want to create your own. For example, you might keep one policy for **court filings**, another
for **medical records** in a personal-injury matter, and another for **financial documents** in
discovery — each one tuned to remove exactly what that kind of document requires. You can create as
many policies as you like.

To work with policies, click the **Policies** button on the main toolbar. This opens the **Policy
Editor**.

## A tour of the Policy Editor

The editor has a toolbar across the top and, below it, a long list of the kinds of information Philter
Desktop can detect, organized into groups:

- **Policy selector** — a drop-down for choosing which policy you want to look at or change.
- **New / Save / Save As / Delete** — the buttons for managing your policies (covered below).
- **View JSON** — shows the policy's underlying definition in its raw, technical form, with a button
  to copy it. (JSON is just a plain-text format that software uses to store settings. You will almost
  never need this; it's there for troubleshooting or for sharing a policy's exact definition with
  technical support.)
- **Search** — a box for quickly finding a particular type of information by name.
- **The groups of detectors** — categories such as Personal, Contact, Location, Financial,
  Identifiers, Technical, and Medical, each holding the specific kinds of information you can choose
  to remove.

At the bottom, a status line tells you how many detectors are currently turned on — for example,
*"6 of 27 filters enabled."* (The word **filter** is just another word for one of these detectors:
"the email-address filter," "the Social Security number filter," and so on.)

## Turning on what you want removed

1. Find the kind of information you want to remove — say, **SSN** (Social Security number) or **Email
   Address** — and **check the box** next to it. That turns it on.
2. When you check a box, a **Configure…** button appears beside it. Clicking it lets you choose
   *how* that information should be replaced — for example, blacked out entirely, or swapped for a
   stand-in label. Those choices are explained on the [Filter Strategies](filter-strategies.md) page.

You do **not** have to configure every detector. A detector that's turned on but not configured simply
uses a sensible default (it blacks the information out). So the quickest way to build a policy is to
check the boxes for everything you want removed and save — you can always come back and fine-tune the
replacements later.

For the complete list of what Philter Desktop can detect, see [Supported Filters](supported-filters.md).

## Saving your work

- **Save** updates the policy you're currently editing with your changes.
- **Save As** takes your current settings and saves them as a **new** policy under a new name —
  handy when you want to base a new policy on an existing one without changing the original.
- If you switch to a different policy, or close the editor, while you have unsaved changes, Philter
  Desktop will stop and ask whether you'd like to save first, so you won't lose anything by accident.

The **default** policy cannot be deleted, so you'll always have a working starting point.

## The "never redact" list (Ignore List)

Sometimes Philter Desktop will correctly detect something but you'd rather **keep it**. A common
example: your own firm's name, a public office address, or a court's phone number might look like the
kind of contact information that would normally be removed — but you want it left in place.

Click **Ignore List** on the toolbar and type in those terms, **one per line**. Anything on this list
is left alone, even when a detector would otherwise have removed it.

The matching works on the **whole detected value** (so a detected item that exactly equals one of
your terms is left untouched), and it ignores capitalization unless you check **Match case**. Your
ignore list is saved as part of the policy, so it applies to every document you redact with that
policy.

## The "always redact" list (Always Redact)

This is the mirror image of the ignore list. Sometimes there's a word or phrase you want **always
removed**, even though it isn't a standard type of personal information that Philter Desktop would
recognize on its own — for example, a confidential project codename, an internal matter number, or a
particular name that's sensitive in your case.

Click **Always Redact** on the toolbar and type in those terms, **one per line**. Anything on this
list is removed wherever it appears.

Matching ignores capitalization. These terms are saved as part of the policy and apply to every
document you redact with it.

## Detecting names with on-device AI

In the **Personal** group you'll find an entry called **Names (on-device AI)**. Checking it turns on
a smart, names-specific detector.

Why is this separate from the other detectors? Most sensitive information has a recognizable shape —
a Social Security number is always nine digits in a familiar pattern, an email address always has an
"@" in it — so a simple rule can spot it. **Names are different.** "April," "Hope," and "Mason" can
be names or ordinary words; whether something is a name depends on the surrounding sentence. To handle
this, Philter Desktop uses a small piece of **artificial intelligence** — a trained model that reads
the context and decides what's a name and what isn't, much more reliably than a fixed rule could.

The most important point for confidentiality: **this AI runs entirely on your own computer.** No part
of your document is uploaded or sent over the internet. Nothing leaves your machine.

There's nothing to configure — just check the box and save the policy. The first document you redact
after turning it on takes a moment longer while the program loads the model; after that, it's quick.

> Installed copies of Philter Desktop already include this names model, so it just works. (If you are
> a technical user running the program from source code, the model is downloaded automatically when
> you make a Release build, or you can run the included `scripts/download-pheye-model.ps1`. If the
> model isn't present for some reason, checking the box simply has no effect.)

## Redacting your own special identifiers (Custom Identifiers)

Beyond the built-in types, you can teach Philter Desktop to remove information that follows **your
organization's own format** — for instance, a case number like `CASE-2024-00123`, a client matter
ID, or an internal account number.

To do this, enable **Custom Identifiers**, click **Configure…**, and describe the pattern you want to
match, along with a label to call it. Describing a pattern uses something called a **regular
expression**, which is a compact way of saying "match text that looks like *this*" (for example,
"the letters CASE, then a dash, then four digits, then a dash, then five digits"). Regular
expressions are a technical skill; if you're not comfortable writing one, ask a technical colleague,
or use the **Always Redact** list above for specific words and phrases, which needs no special
syntax.
