# Filter Strategies

When Philter Desktop finds a piece of sensitive information, it puts **something** in its place.
A **filter strategy** is your choice of *what* that something is. (A "filter" is one of the detectors:
the email-address detector, the name detector, and so on. A "strategy" is the rule for how that
detector replaces what it finds.)

You set a strategy by clicking the **Configure…** button next to a detector in the
[Policy Editor](policies.md). Each detector can have one strategy, or several for different
situations.

## Choosing a strategy

When you add or edit a strategy, pick one from the **Strategy** list. The list only shows the
strategies that work for that detector (the date strategies, for example, only appear for dates).

- **Redact.** The detected text is swapped for a placeholder label. By default that label is
  `{{{REDACTED-%t}}}`, where `%t` is filled in with the type of information, so a removed email address
  appears as `{{{REDACTED-email-address}}}`. This shows *that* something was removed and *what kind*
  it was, without revealing the value. You can change this label (for example, to `[REDACTED]`).
- **Replace with a fixed value.** Every detected item of that type is replaced with the **same text**
  you specify, for example `XXX-XX-XXXX` for every Social Security number.
- **Replace with a random value.** The detected item is replaced with a **made-up value of the same
  kind**, such as a fake-but-realistic name, so the document still reads naturally. The stand-in values
  are invented and do not correspond to any real person.
- **Mask.** Each character is replaced, for example with `*`. The mask can match the length of the
  value or use a fixed length.
- **Keep the last 4 characters.** For example, `4111-1111-1111-1111` becomes `1111`.
- **Keep some characters, mask the rest.** Keeps a number of characters at the start or end and masks
  the others, for example `************1111`.
- **Abbreviate to initials.** For example, *John Smith* becomes *JS*.
- **Replace with a SHA-256 hash.** The same value always gets the same hash, so you can still tell
  matching values apart. Tick **Add a random salt** if the same value should get a different hash each
  time.
- **Encrypt** and **Encrypt, keeping the format.** The value is encrypted, so someone with the key can
  recover the original. "Keeping the format" leaves a card number shaped like a card number. The key is
  never stored in the policy: you enter the name of an **environment variable** that holds it, and set
  that variable on each computer that redacts with the policy. If the variable isn't set, redaction
  stops with an error rather than leaving the value in place.
- **Replace from a lookup table.** Replace specific values with the replacement you give each one, and
  choose another strategy for values that aren't in the table.

For **dates**, three more strategies are available:

- **Shift the date.** Moves each date by a fixed number of years, months, and days, or by a random
  amount.
- **Describe relative to today.** For example, *36 years 9 months ago*.
- **Keep only the year.** For example, *01/15/1990* becomes *1990*.

> **A note about PDFs.** These strategies control the replacement **text**, which appears in the
> redacted output for Word (`.docx`), text (`.txt`), rich text (`.rtf`), spreadsheet (`.xlsx`, `.csv`),
> and email (`.eml`, `.msg`) files. **PDFs work differently:** a redacted PDF is flattened to an image
> with every detected item painted over by a solid box (see [PDF](redacting-pdf.md)).
> For PDFs the choice of strategy does **not** change how the result looks; you get a solid box
> whatever strategy you picked.

## Keeping replacements consistent (Scope)

When you use **Replace with a random value** or a lookup table, you can decide whether the *same*
original value always gets the *same* stand-in. For example, if "Jane Doe" appears twenty times across
a set of documents, you can have her replaced by the same invented name every time (so relationships
between people stay intact) instead of a different fake name in each spot.

This consistency works with [contexts](contexts.md). A context is the "memory" that lets the same input
map to the same replacement across all the documents you process together. See the
[Contexts](contexts.md) page for the full explanation.

## Applying a strategy only in certain situations (Conditions)

A strategy can be made **conditional**, applying only when a condition you describe is met. This is an
advanced option for fine-tuning unusual cases; most everyday redaction won't need it.

Turn on **Only apply when** and fill in the builder. You pick:

- **When**: what to test: the *Matched text*, the *Context*, the *Confidence* (how sure the detector
  is, from 0 to 1), or the *Population*.
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

If a policy has a condition the redaction engine can't read (for example, from an older version or an
imported file), Philter Desktop removes it when you open or import the policy and tells you which ones.
Those conditions already applied their strategy to every match, so redaction doesn't change.

These examples show the kind of fine-tuning conditions make possible:

| Scenario | Condition |
|----------|-----------|
| Black out a match only when the detector is highly confident | `confidence is greater than 0.8` |
| Use a gentler strategy on low-confidence guesses | `confidence is less than 0.4` |
| Redact your internal IDs but leave a sample value used in templates | `matched text does not equal "CASE-0000-00000"` |
| Apply a strategy only to IDs that use your prefix | `matched text starts with "CASE-"` |
| Redact names only in records that mention a patient | `context starts with "Patient"` |
| Apply a strategy only to a particular data set | `population equals "EU"` |
| Leave your own organization's name unredacted | `matched text does not equal "Acme Corporation"` |

## A few helpful tips

- A detector that's turned **on** but has **no** strategy set still works; it uses the default
  redaction (replacing the text with a marker like `{{{REDACTED-ssn}}}`). When you open **Configure…**,
  that default is already listed, so you can see what will happen and change it.
- You can attach **more than one** strategy to a single detector to handle different cases
  differently.
- If you're unsure which approach to pick, **Redact** (black it out) is the safest and most common
  choice, because it makes obvious that information was removed.
