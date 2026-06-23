# Policies

A **policy** defines which types of PII to redact and how to replace them. Every document in the
queue is redacted according to its assigned policy. A **default** policy is created for you, and
you can create as many additional policies as you need.

Open the editor from the **Policies** button on the main toolbar.

## The Policy Editor

The editor has a toolbar and a set of filters grouped by category:

- **Policy selector** — choose which policy to edit.
- **New / Save / Save As / Delete** — manage policies.
- **View JSON** — see the raw policy definition (with a copy-to-clipboard button).
- **Search** — quickly find a filter by name.
- **Filter groups** — Personal, Contact, Location, Financial, Identifiers, Technical, Medical,
  and more, each containing the available PII filters.

The status bar shows how many filters are enabled (e.g., *"6 of 27 filters enabled"*).

## Enabling a filter

1. Check the box next to a filter (for example, **SSN** or **Email Address**) to enable it.
2. A **Configure…** button appears — click it to set the
   [replacement strategies](filter-strategies.md) for that filter.

A filter with no explicit strategy uses a sensible default (redaction). See
[Supported Filters](supported-filters.md) for the full list.

## Saving changes

- **Save** updates the selected policy.
- **Save As** creates a new policy from the current settings.
- If you switch policies or close the editor with unsaved changes, Philter Desktop asks whether
  you want to save first.

The **default** policy cannot be deleted.

## AI name detection

The **Personal** group contains **Names (on-device AI)**. Check it to detect person names with a
bundled machine-learning model that runs entirely on your computer — no text ever leaves the
machine. This complements the pattern-based filters: names have no fixed format and depend on
context, so a model finds them more reliably than a rule could.

There is nothing to configure — enable the checkbox and save the policy. The first document
redacted with it loads the model (a brief one-time delay); subsequent documents are fast.

> Installed builds of Philter Desktop include the model already. If you build or run the
> application yourself, the model is downloaded automatically on a Release build (or run
> `scripts/download-pheye-model.ps1`). If the model is not present, the option simply has no effect.

## Custom Identifiers

In addition to the built-in PII types, **Custom Identifiers** let you redact text that matches
your own regular expressions — useful for organization-specific identifiers such as account or
case numbers. Enable **Custom Identifiers**, click **Configure…**, and add each pattern with a
classification (label).
