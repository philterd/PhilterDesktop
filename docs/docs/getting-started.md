# Getting Started

## Requirements

- Windows 10 or Windows 11.
- A [Microsoft Word (`.docx`) redaction license](settings.md#word-redaction-license) is required
  only if you redact Word documents. Plain-text redaction needs no license. Installed builds of
  Philter Desktop typically include the key already; this is only relevant if you build or run the
  application yourself.

## Installing

Philter Desktop is distributed as an **MSIX** package. To install:

1. Obtain the signed `PhilterDesktop` `.msix` package.
2. Double-click the package and choose **Install**.

> If Windows reports that the package is from an untrusted publisher, the signing certificate
> must be trusted on the machine first. For internal/test builds, install the certificate into
> **Local Machine → Trusted People**, then install the package.

## First run

When you open Philter Desktop you'll see the main window with a toolbar and the **redaction
queue** (empty on first launch).

A **default** policy and a **default** context are created automatically, so you can start
redacting immediately:

1. Click **Redact** (or drag files onto the window) to add documents.
2. Watch the queue — each file moves from **Pending** → **Processing** → **Completed**.
3. Open the redacted output from the right-click menu, or find it in your
   [output location](settings.md).

From there you can tailor the redaction to your needs:

- Create and edit [policies](policies.md) to control which PII is redacted and how.
- Configure [settings](settings.md) such as where redacted files are written.
