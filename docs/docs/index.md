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

## Who it's for

Philter Desktop puts powerful redaction in the hands of an **individual user**, on their own
machine. It's for the analyst, paralegal, researcher, or knowledge worker who needs to remove PII
from documents quickly and review the results before sharing them.

## What it is *not*

- **It is not a collaborative workflow or case-management tool.** There are no shared queues,
  assignments, or hand-offs between users — each person runs Philter Desktop locally on their own
  documents.
- **It is not an approval system.** Philter Desktop has no built-in review-and-sign-off stages,
  multi-party approvals, or audit trails for governance. You — the person running it — are
  responsible for reviewing redacted output before it leaves your machine. (This is why the default
  output suffix is `_redacted-draft`: a redacted file is a *draft* until you've verified it.)
- **It is not a server or service.** It is a desktop application; it doesn't expose an API or run
  as a shared backend. For programmatic or server-side redaction, use the underlying
  [Phileas](https://github.com/philterd/phileas-net) engine directly.

## How it works

1. **Add documents** to the redaction queue (button or drag-and-drop).
2. Each document is assigned a **policy** (which PII to find) and a **context** (for consistent
   replacement).
3. Philter Desktop redacts each file and writes a separate copy (named with a configurable suffix,
   default `_redacted-draft`) to your chosen [output location](settings.md). Always review redacted
   output before sharing it.

## Next steps

- [Getting Started](getting-started.md) — install and run Philter Desktop.
- [Redacting Documents](redacting-documents.md) — add and process files.
- [Policies](policies.md) — define what gets redacted.
