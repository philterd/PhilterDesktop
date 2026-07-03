# Release Notes

All notable changes to Philter Desktop are recorded here, newest first.

## 1.0.0

Initial release of Philter Desktop — an offline Windows application for finding and redacting personally
identifiable information (PII) in PDF, Word (`.docx`), Excel (`.xlsx`), text, CSV, RTF, and email
(`.eml`/`.msg`) files, entirely on your own machine with nothing sent to the cloud. Detection is driven by
configurable policies (powered by the Philterd Phileas engine), and redaction can be run one file at a
time, in batches, from a watched folder, at the command line, or from the Windows Explorer right-click
menu, with an encrypted local history you can review, modify, and re-apply. Beyond visible text, this
release also scrubs many of the places sensitive data hides in Office and PDF files — document metadata,
comments, tracked changes, hidden text, print headers and footers, charts and their cached values, formula
and pivot caches, text boxes and shapes, Word field codes, and embedded workbooks and objects — and warns
you when a file contains content it could not inspect (such as an image or an object it can't open).
Redaction depends on your policy detecting the sensitive values, so always review each redacted document
before sharing it.
