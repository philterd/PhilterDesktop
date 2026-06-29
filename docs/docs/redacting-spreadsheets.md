# Spreadsheets

Philter Desktop redacts Excel files that end in **`.xlsx`** and comma-separated files that end in
**`.csv`**.

## How spreadsheet redaction works

Spreadsheets are redacted **cell by cell**: Philter Desktop looks at each cell on its own and removes
the sensitive information it finds there, leaving the rest of the table (the layout, the columns, the
numbers, the formulas) intact. An Excel file stays `.xlsx` and a CSV stays `.csv`. (For Excel,
**formulas** are left alone, since their value is calculated rather than stored.)

!!! note "Redacted CSVs are safe to open in a spreadsheet"
    A `.csv` is just text, so a spreadsheet program will treat any cell that begins with `=`, `+`, `-`,
    or `@` as a **formula** and run it when the file is opened, a behavior (called "CSV injection") that
    a malicious source file could use to attack whoever you share the redacted copy with. Philter
    Desktop neutralizes this automatically: in a redacted `.csv`, any such cell is written so that
    spreadsheets open it as **plain text** instead of running it (you may notice a leading apostrophe on
    those few cells). Excel `.xlsx` files aren't affected by this: their cells are explicitly typed, so
    a text value is never mistaken for a formula.

## A limitation to understand

Philter Desktop recognizes sensitive information partly from the **words around it**, and a cell often
holds a value by itself, with no surrounding sentence for context. Patterns with a fixed shape (Social
Security numbers, email addresses, account numbers) are still caught reliably. But a value such as a
lone first name in a cell (for example, "April") has nothing around it to signal that it is a name, so
automatic name detection is **much weaker** on bare cells than on ordinary paragraphs of writing.

Numbers are also handled differently. A value a spreadsheet stores as a *number*, such as an account
number or an ID typed as plain digits, is **not scanned** for sensitive information, because Philter
Desktop leaves numbers exactly as they are (so totals and calculations are not disturbed). Sensitive
values that look like text, such as an SSN written with dashes (`123-45-6789`), are still detected. If
a column holds sensitive **numeric** IDs, remove it with whole-column redaction, described next.

## Whole-column redaction

For these reasons, spreadsheets have an additional tool: **whole-column redaction**. Use the **Redact
Spreadsheet…** action (on the **Redact** button's arrow menu, or by right-clicking a spreadsheet in the
queue). It opens a small window where you choose the **policy** and **context** as usual, and also see
a **list of the file's columns** (with their headers). Tick any column whose contents should be removed
**entirely** (for example a "Name" column, a "Patient ID" column, or an "Account" column), and every
**data cell** in that column is cleared, regardless of whether the detector would have flagged it. The
column's **header label is kept** (so the table stays readable; only the values below it are removed).
Columns you don't tick are still cleaned the normal way (detected sensitive values removed). This is the
dependable way to handle columns of names and identifiers.

When you click **Redact**, the spreadsheet is **added to the queue** with your choices and the window
closes; it's then redacted in the background like any other document and appears in the main list with
its status.

!!! note "Whole-column removal is only offered by Redact Spreadsheet…"
    When you redact a spreadsheet through the ordinary queue, drag-and-drop, a
    [watched folder](watched-folders.md), or the command line, Philter Desktop runs **detection on
    every cell** (no column is fully cleared, because those routes don't ask you any questions). To pick
    whole columns to remove, use **Redact Spreadsheet…**.

Spreadsheets are not available in **Redact with Preview**; redact them the ordinary way and review the
cleaned-up copy afterward. For adjusting, verifying, and reporting on a redaction, see
[Redacting Documents](redacting-documents.md).
