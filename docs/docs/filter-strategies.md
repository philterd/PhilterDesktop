# Filter Strategies

A **filter strategy** controls *how* a detected piece of PII is replaced. Each enabled filter in
a [policy](policies.md) can have one or more strategies; configure them with the **Configure…**
button next to the filter.

## Replacement types

When you add or edit a strategy you choose one of:

- **Redact** — replace the detected text with a redaction format string. The default is
  `{{{REDACTED-%t}}}`, where `%t` is replaced by the filter type (for example,
  `{{{REDACTED-email-address}}}`). You can customize this format.
- **Static replacement** — replace the detected text with a fixed value you specify (for example,
  replace every detected name with `REDACTED`).
- **Random replacement** — replace the detected text with a randomly generated value of the same
  kind.

## Scope (consistent replacement)

For random replacement you can choose whether the same original value is replaced **consistently
across document contexts**. Combined with [contexts](contexts.md), this lets the same input (say,
a particular name) map to the same replacement everywhere it appears.

## Conditions

A strategy can be made **conditional** so it only applies when a condition you specify is met.
Enable the conditional option and enter the condition when adding or editing the strategy.

## Tips

- A filter that is enabled but has **no** strategy still redacts using the default behavior.
- You can add multiple strategies to a single filter to handle different cases.
