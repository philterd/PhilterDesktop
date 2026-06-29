# Contexts

A **context** keeps your replacements **consistent across a set of related documents**.

Consider redacting fifty documents in a single matter where you've chosen to replace real names with
realistic stand-in names. Without a context, "Jane Doe" might become "Mary Smith" in one document,
"Susan Jones" in the next, and "Linda Brown" in a third, because each replacement is generated
separately. The documents would no longer line up, making it impossible to follow who's who.

A **context** fixes this by acting as a shared memory: when you redact all fifty documents under the
same context, "Jane Doe" becomes the *same* stand-in name in **every** document. The set stays
internally consistent, relationships between people are preserved, and the redacted documents
remain usable as a group.

A ready-made context named **default** is created automatically. You can create additional
contexts and choose which one to use when you add documents to the queue: for example, one context
per case or per matter, so each matter's documents stay consistent within themselves but don't
get mixed up with another matter's.

## When would I use contexts?

Contexts matter most when you replace sensitive information with **realistic stand-in values** (rather
than blacking it out) and you are redacting **more than one related document**. Common situations:

- **A set of documents in one case or matter.** Use a single context for all of them so the same
  person, account, or identifier is replaced with the same stand-in everywhere.
- **Documents that reference each other.** When an email thread, a contract, and its exhibits all
  mention the same parties, a shared context keeps those references pointing to the same stand-in
  values, so relationships between people remain clear after redaction.
- **Keeping separate matters apart.** Use a **different** context for each case or client so one
  matter's replacements never mix with another's.
- **Starting fresh.** To reuse a context without its remembered replacements,
  use **Empty** to clear the memory while keeping the context.

If you are **blacking out** information (the **Redact** strategy) or redacting a **single**
document, consistency across documents doesn't apply, so the **default** context is all you need.

## Managing your contexts

Open the **Contexts** window from the main toolbar. From there you can:

![The Contexts window in Philter Desktop, listing the available contexts](img/contexts.png)

*The Contexts window: create per-case or per-matter contexts so related documents stay consistent.*

- **New Context**: create a new context (for instance, one for a new case).
- **Delete**: remove a context you no longer need.
- **Empty**: clear the remembered replacements for a context **without** deleting the context
  itself, to start the consistency "memory" fresh while keeping the context.

## How contexts come into play

- When you add documents using the **Redact** button, you choose which context to use for them.
- Documents added by **drag-and-drop** use the **default** context.
- Whether replacements are shared from one document to the next depends on the
  [filter strategy](filter-strategies.md). Specifically, **random replacement** set to
  reuse values within a context is what makes the same original turn into the same stand-in
  everywhere. (When blacking information out, consistency isn't a concern: every
  redaction becomes the same placeholder anyway.)
