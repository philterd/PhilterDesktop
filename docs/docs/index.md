# Philter Desktop

**Philter Desktop** is a Windows application for redacting personally identifiable information
(PII) from plain text, Microsoft Word, and PDF documents. Add files to the redaction queue, choose
a policy that describes what to redact and how, and Philter Desktop produces redacted copies of
your documents — leaving the originals untouched.

It is powered by the [Phileas](https://github.com/philterd/phileas-net) redaction engine.

## What it does

- Redacts **plain text (`.txt`)**, **Microsoft Word (`.docx`)**, and **PDF (`.pdf`)** documents.
- Detects a wide range of PII — names, email addresses, phone numbers, SSNs, credit cards,
  addresses, dates, and more (see [Supported Filters](supported-filters.md)).
- Lets you control **what** is redacted and **how** it is replaced using
  [policies](policies.md) and [filter strategies](filter-strategies.md).
- Keeps replacements consistent across documents using [contexts](contexts.md).
- Processes files in the background with a live status view.

## How it works

1. **Add documents** to the redaction queue (button or drag-and-drop).
2. Each document is assigned a **policy** (which PII to find) and a **context** (for consistent
   replacement).
3. Philter Desktop redacts each file and writes a `*_redacted` copy to your chosen
   [output location](settings.md).

## Next steps

- [Getting Started](getting-started.md) — install and run Philter Desktop.
- [Redacting Documents](redacting-documents.md) — add and process files.
- [Policies](policies.md) — define what gets redacted.
