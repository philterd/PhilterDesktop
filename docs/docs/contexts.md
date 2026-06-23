# Contexts

A **context** groups redactions so that replacements can be applied **consistently**. When two
documents are redacted under the same context, a given original value can be replaced with the
same value in both — which is useful when you need redacted documents to remain internally
consistent (for example, the same person should map to the same placeholder across a set of
files).

A **default** context is created automatically. You can create additional contexts and choose one
when adding documents to the redaction queue.

## Managing contexts

Open the **Contexts** window from the main toolbar to:

- **New Context** — create a context.
- **Delete** — remove a context.
- **Empty** — clear the stored replacements for a context without deleting it.

## How contexts are used

- When you add documents with the **Redact** button, you pick a context for them.
- Files added by drag-and-drop use the **default** context.
- Whether replacements are actually shared across documents depends on the
  [filter strategy](filter-strategies.md) — random replacement with **context** scope reuses
  values within the same context.
