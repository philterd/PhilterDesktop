# Filter Strategies (How Information Gets Replaced)

When Philter Desktop finds a piece of sensitive information, it has to put **something** in its place.
A **filter strategy** is simply your choice of *what* that something is. (Remember that a "filter" is
just one of the detectors — the email-address detector, the name detector, and so on. A "strategy" is
the rule for how that particular detector replaces what it finds.)

You set a strategy by clicking the **Configure…** button next to a detector in the
[Policy Editor](policies.md). Each detector can have one strategy, or even several for different
situations.

## The three ways to replace information

When you add or edit a strategy, you choose one of these three approaches:

- **Redact (black it out).** The detected text is swapped for a placeholder label. By default that
  label is `{{{REDACTED-%t}}}`, where the `%t` part is automatically filled in with the type of
  information — so a removed email address would appear as `{{{REDACTED-email-address}}}` in the
  result. This makes the cleaned-up document clearly show *that* something was removed and *what kind*
  of thing it was, without revealing the actual value. You can change this label to whatever wording
  you prefer (for example, a simple `[REDACTED]`).

- **Static replacement (use a fixed word).** Every detected item of that type is replaced with the
  **same fixed text** that you specify. For example, you could replace every detected name with the
  single word `REDACTED`, or replace every Social Security number with `XXX-XX-XXXX`. This is useful
  when you want a clean, uniform look.

- **Random replacement (use a realistic stand-in).** The detected item is replaced with a **made-up
  value of the same kind** — a fake-but-realistic name in place of a real name, for instance. This is
  useful when you want the cleaned-up document to still *read* naturally, with believable
  placeholders, rather than being peppered with black boxes. The stand-in values are invented; they
  do not correspond to any real person.

> **A note about PDFs.** These strategies control the replacement **text**, which appears in the
> redacted output for Word (`.docx`), text (`.txt`), rich text (`.rtf`), spreadsheet (`.xlsx`, `.csv`),
> and email (`.eml`, `.msg`) files. **PDFs work differently:** a redacted PDF is flattened to an image
> with every detected item painted over by a solid box (see [Redacting Documents](redacting-documents.md)).
> So for PDFs the choice of strategy does **not** change how the result looks — you get a solid box in
> every case, whether you picked Redact, Static replacement, or Random replacement.

## Keeping replacements consistent (Scope)

When you use **random replacement**, you can decide whether the *same* original value should always
get the *same* stand-in. For example, if "Jane Doe" appears twenty times across a set of documents,
you can have her replaced by the same invented name every single time — so the documents still make
sense and the relationships between people stay intact — instead of getting a different fake name in
each spot.

This consistency works hand in hand with [contexts](contexts.md). A context is the "memory" that lets
the same input map to the same replacement across all the documents you process together. See the
[Contexts](contexts.md) page for the full explanation.

## Applying a strategy only in certain situations (Conditions)

A strategy can be made **conditional**, meaning it only kicks in when a condition you describe is met.
This is an advanced option for fine-tuning unusual cases; for most everyday redaction you won't need it.

To use it, turn on **Only apply when** and fill in the simple builder — you pick:

- **When** — what to test: the *Matched text*, the *Context*, the *Detected type*, the *Confidence*
  (how sure the detector is, from 0 to 1), or the *Population*.
- **Is** — how to compare it: *equals*, *does not equal*, *starts with*, and (for numbers like
  confidence) *is greater than*, *is less than*, and so on.
- **Value** — what to compare against.

As you choose, Philter Desktop shows the exact condition it will use (for example,
`confidence is greater than 0.8`). Building it this way means the condition is always valid — you can't
accidentally mistype one.

## A few helpful tips

- A detector that's turned **on** but has **no** strategy set still works — it uses the default
  redaction (replacing the text with a marker like `{{{REDACTED-SSN}}}`). When you open **Configure…**,
  you'll see that default already listed, so you can see exactly what will happen and change it if you
  like.
- You can attach **more than one** strategy to a single detector to handle different cases
  differently.
- If you're unsure which approach to pick, **Redact** (black it out) is the safest and most common
  choice, because it makes it obvious that information was removed.
