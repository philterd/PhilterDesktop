# Filter Strategies

When Philter Desktop finds a piece of sensitive information, it puts **something** in its place.
A **filter strategy** is your choice of *what* that something is. (A "filter" is one of the detectors:
the email-address detector, the name detector, and so on. A "strategy" is the rule for how that
detector replaces what it finds.)

You set a strategy by clicking the **Configure…** button next to a detector in the
[Policy Editor](policies.md). Each detector can have one strategy, or several for different
situations.

## The three ways to replace information

When you add or edit a strategy, you choose one of these three approaches:

- **Redact (black it out).** The detected text is swapped for a placeholder label. By default that
  label is `{{{REDACTED-%t}}}`, where `%t` is filled in with the type of information, so a removed
  email address appears as `{{{REDACTED-email-address}}}`. This shows *that* something was removed and
  *what kind* it was, without revealing the value. You can change this label (for example, to a simple
  `[REDACTED]`).

- **Static replacement (use a fixed word).** Every detected item of that type is replaced with the
  **same fixed text** you specify. For example, replace every detected name with `REDACTED`, or every
  Social Security number with `XXX-XX-XXXX`. Useful for a clean, uniform look.

- **Random replacement (use a realistic stand-in).** The detected item is replaced with a **made-up
  value of the same kind**, for instance a fake-but-realistic name in place of a real name. Useful when
  you want the document to still *read* naturally rather than being peppered with black boxes. The
  stand-in values are invented and do not correspond to any real person.

> **A note about PDFs.** These strategies control the replacement **text**, which appears in the
> redacted output for Word (`.docx`), text (`.txt`), rich text (`.rtf`), spreadsheet (`.xlsx`, `.csv`),
> and email (`.eml`, `.msg`) files. **PDFs work differently:** a redacted PDF is flattened to an image
> with every detected item painted over by a solid box (see [PDF](redacting-pdf.md)).
> For PDFs the choice of strategy does **not** change how the result looks; you get a solid box
> whether you picked Redact, Static replacement, or Random replacement.

## Keeping replacements consistent (Scope)

When you use **random replacement**, you can decide whether the *same* original value always gets the
*same* stand-in. For example, if "Jane Doe" appears twenty times across a set of documents, you can
have her replaced by the same invented name every time (so relationships between people stay intact)
instead of a different fake name in each spot.

This consistency works with [contexts](contexts.md). A context is the "memory" that lets the same input
map to the same replacement across all the documents you process together. See the
[Contexts](contexts.md) page for the full explanation.

## Applying a strategy only in certain situations (Conditions)

A strategy can be made **conditional**, applying only when a condition you describe is met. This is an
advanced option for fine-tuning unusual cases; most everyday redaction won't need it.

Turn on **Only apply when** and fill in the builder. You pick:

- **When**: what to test: the *Matched text*, the *Context*, the *Detected type*, the *Confidence*
  (how sure the detector is, from 0 to 1), or the *Population*.
- **Is**: how to compare. The choices depend on the field: *equals* and *does not equal* are always
  available; *starts with* is offered for the *Matched text* and *Context* fields; and numbers (like
  *Confidence* and *Population*) add *is greater than*, *is less than*, and so on. Only the comparisons
  that actually work for the chosen field are shown.
- **Value**: what to compare against. For a number, enter plain digits with an optional decimal point
  (for example `0.85`) — signs, exponents, and thousands separators aren't accepted.

As you choose, Philter Desktop shows the exact condition it will use (for example,
`confidence is greater than 0.8`). Building it this way keeps the condition always valid: it only lets
you pick comparisons and values the redaction engine understands, so a condition can never be silently
ignored (which would make the strategy apply everywhere instead of only where you intended).

These examples show the kind of fine-tuning conditions make possible:

| Scenario | Condition |
|----------|-----------|
| Black out a match only when the detector is highly confident | `confidence is greater than 0.8` |
| Use a gentler strategy on low-confidence guesses | `confidence is less than 0.4` |
| Redact your internal IDs but leave a sample value used in templates | `matched text does not equal "CASE-0000-00000"` |
| Apply a strategy only to IDs that use your prefix | `matched text starts with "CASE-"` |
| Redact names only in records that mention a patient | `context starts with "Patient"` |
| Handle one detected type differently from the rest | `detected type equals "Phone Number"` |
| Apply a strategy only to a particular data set | `population equals "EU"` |
| Leave your own organization's name unredacted | `matched text does not equal "Acme Corporation"` |

## A few helpful tips

- A detector that's turned **on** but has **no** strategy set still works; it uses the default
  redaction (replacing the text with a marker like `{{{REDACTED-SSN}}}`). When you open **Configure…**,
  that default is already listed, so you can see what will happen and change it.
- You can attach **more than one** strategy to a single detector to handle different cases
  differently.
- If you're unsure which approach to pick, **Redact** (black it out) is the safest and most common
  choice, because it makes obvious that information was removed.
