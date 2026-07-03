# Spreadsheets

Philter Desktop redacts Excel files that end in **`.xlsx`** and comma-separated files that end in
**`.csv`**.

## How spreadsheet redaction works

Spreadsheets are redacted **cell by cell**: Philter Desktop looks at each cell on its own and removes
the sensitive information it finds there, leaving the rest of the table (the layout, the columns, the
numbers, the formulas) intact. An Excel file stays `.xlsx` and a CSV stays `.csv`. (For Excel, a
**formula** itself is left in place, since its value is calculated rather than typed in.)

An Excel formula does, however, keep a **cached copy of its last result** inside the file, which can
duplicate sensitive information from a cell you just redacted. By default Philter Desktop handles this:
a formula whose cached result holds detected sensitive information is turned into a static redacted
value, and other formula caches are cleared so Excel recomputes them when you open the file. You can turn
this off in [Settings → Microsoft Office](settings.md#microsoft-office-tab); see that page for the
trade-off (a tool that reads the file without recalculating would see empty formula results).

For Excel, **cell comments** — both the classic comments and the newer **threaded comments** (with their
author names) — are scanned and redacted too, so sensitive information tucked into a comment isn't left
behind. **Embedded charts** are scanned as well — their titles, labels, and the **cached data values**
a chart keeps (its copy of the plotted series and category values, which would otherwise remain after
the source cells are redacted); redacting a cached value can change how the chart looks, so review charts
in the output. **Text boxes and shapes** drawn on a sheet are scanned too — the free text inside them is
redacted like any cell (text only; a picture placed on the sheet is not read). **Pivot tables** keep a
hidden copy of their source data (the pivot cache); its cached values are scanned and redacted as well,
and the pivot is set to refresh from the redacted source when the file is next opened in Excel. This is
on by default and can be turned off in
[Settings → Microsoft Office](settings.md#microsoft-office-tab). A workbook can also **embed another
file** (Insert → Object): an embedded Excel or Word document is redacted in place, while an object Philter
Desktop can't read is removed by default (or kept with a warning — see the same settings tab). The **print header and
footer** — the text set to appear at the top and bottom of each
printed page (for example "Confidential — John Doe") — is scanned and redacted too. **Only text is
redacted** there: an image or logo placed in a header/footer is left as it is, and Excel field codes
(page number, date, file name) are preserved. This is on by default and can be turned off in
[Settings → Microsoft Office](settings.md#microsoft-office-tab). (This is the printed page header, not
the column header row in the sheet.)

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
Security numbers, email addresses, account numbers) are still caught reliably. To help with the rest,
Philter Desktop uses each column's **header** as context when scanning that column's cells — so a value
under a "First Name" or "SSN" header is more likely to be recognized than the same value with no label.

Even so, two limits remain. A bare value under a vague or missing header (for example a lone first name)
can still be missed, so automatic name detection is weaker on bare cells than on ordinary paragraphs.
And because each cell is scanned **on its own**, sensitive information **split across columns** (for
example a first name in one column and a last name in the next) is not seen as a single item. For
columns you know are sensitive, **whole-column redaction** (described next) remains the most dependable
option.

Numbers are scanned too. A value a spreadsheet stores as a *number* — such as a Social Security number,
phone number, or account number typed as plain digits (for example `123456789`) — is run through
detection just like text and removed when it matches. A redacted number becomes text in the cleaned-up
copy (so the replacement is visible), while ordinary numbers that aren't sensitive (quantities, totals,
IDs that match nothing) are left exactly as they are, so calculations aren't disturbed. One caveat:
detection sees the value a cell *stores*, which can differ from what a cell's format *displays* — for
example a date kept as an internal serial number, or digits shown through a custom format. For columns
of identifiers, **whole-column redaction** (described next) remains the most dependable option.

## Whole-column redaction

For these reasons, spreadsheets have an additional tool: **whole-column redaction**. Use the **Redact
Spreadsheet…** action (on the **Redact** button's arrow menu, or by right-clicking a spreadsheet in the
queue). It opens a small window where you choose the **policy** and **context** as usual. For an Excel
file you also pick the **worksheet** to redact: redaction through this window targets **one worksheet at
a time**, and the column list below shows **that worksheet's** columns (choosing a different worksheet
reloads the list). You then see a **list of the columns** (with their headers). Tick any column whose contents should be removed
**entirely** (for example a "Name" column, a "Patient ID" column, or an "Account" column), and every
**data cell** in that column is cleared, regardless of whether the detector would have flagged it. The
column's **header label is kept** (so the table stays readable; only the values below it are removed).
Columns you don't tick are still cleaned the normal way (detected sensitive values removed). This is the
dependable way to handle columns of names and identifiers.

When you click **Redact**, the spreadsheet is **added to the queue** with your choices and the window
closes; it's then redacted in the background like any other document and appears in the main list with
its status.

!!! note "Whole-column removal and worksheet choice are only offered by Redact Spreadsheet…"
    When you redact a spreadsheet through the ordinary queue, drag-and-drop, a
    [watched folder](watched-folders.md), or the command line, Philter Desktop runs **detection on
    every cell of every worksheet** (no column is fully cleared and no single sheet is chosen, because
    those routes don't ask you any questions). To pick a **worksheet** and **whole columns** to remove,
    use **Redact Spreadsheet…**.

Spreadsheets are not available in **Redact with Preview**; redact them the ordinary way and review the
cleaned-up copy afterward. For adjusting, verifying, and reporting on a redaction, see
[Redacting Documents](redacting-documents.md).
