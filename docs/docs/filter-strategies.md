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
This is an advanced option for fine-tuning unusual cases. To use it, turn on the conditional option
and enter your condition when you add or edit the strategy. For most everyday redaction you won't need
this.

## A few helpful tips

- A detector that's turned **on** but has **no** strategy set still works — it just uses the standard
  black-it-out behavior. So you never have to configure a strategy unless you specifically want
  something other than the default.
- You can attach **more than one** strategy to a single detector to handle different cases
  differently.
- If you're unsure which approach to pick, **Redact** (black it out) is the safest and most common
  choice, because it makes it obvious that information was removed.
