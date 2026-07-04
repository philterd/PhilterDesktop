# Release Testing

A short, repeatable manual checklist to run before publishing a Philter Desktop release. It is
deliberately lightweight: it confirms the **installer works on a clean Windows machine** and that the
**app redacts and verifies documents correctly**. Copy this list into the release checklist (or a release
issue) and check the boxes off for each release.

Most of the functional verification is automated by the built-in self-test (`PhilterDesktop.exe
--selftest`), so the manual steps focus on the parts a human has to judge: a clean install, the first-run
experience, a real PDF, and a couple of GUI interactions.

## Prerequisites

- A **clean** Windows 10 or 11 (x64) environment — a fresh VM snapshot, or a Windows Sandbox — with no
  prior Philter Desktop install and no development tools. (The "clean" part matters: it's what catches a
  missing runtime or bundled dependency.)
- The release installer: **`PhilterDesktop-Setup-<version>.exe`**.
- One real **PDF** with obvious PII in it (an email address and/or an SSN) for the manual PDF check, since
  the self-test does not cover PDF.

---

## Part 1 — Installer (on the clean VM)

- [ ] Copy `PhilterDesktop-Setup-<version>.exe` to the VM and run it; the installer opens without a
      SmartScreen block that can't be bypassed (if it's code-signed) and shows the correct version.
- [ ] Accept the defaults and install. It installs to `C:\Program Files\Philter Desktop` and creates a
      **Start menu** entry "Philter Desktop".
- [ ] If offered, tick the **desktop icon** task and confirm the shortcut is created.
- [ ] Finish the installer with "Launch Philter Desktop" checked — the app starts.
- [ ] On first launch, the **welcome / license (EULA)** screen appears. Decline once → the app exits.
      Launch again, accept → the main window opens and the EULA is not shown again on the next launch.
- [ ] The main window renders correctly (no missing-font or missing-DLL errors).

## Part 2 — Functional smoke (automated self-test)

- [ ] Open a terminal (PowerShell or cmd) and run the self-test from the install directory:

      cd "C:\Program Files\Philter Desktop"
      .\PhilterDesktop.exe --selftest

- [ ] It prints a PASS line for each format and ends with **`Result: PASS (6/6)`**, and the process exit
      code is `0`. (Covers txt, csv, rtf, eml, docx, xlsx end-to-end: redact + verify.)
- [ ] Confirm the shipped license agreement is current (needs network):

      .\PhilterDesktop.exe --smoketest

      It prints **`PASS: the bundled EULA matches ...`** (exit `0`). A FAIL means the installer shipped a
      stale `philterd-eula.txt` — rebuild so it re-downloads the current EULA.

## Part 3 — Functional smoke (manual GUI + PDF)

- [ ] Drag the real **PDF** onto the main window (or use *Redact Files…*), pick the default policy, and run
      it. A redacted copy (e.g. `<name>_redacted-draft.pdf`) is produced; open it and confirm the PII is
      blacked out / removed.
- [ ] Redact one more document through the UI (a `.docx` or `.xlsx`) and confirm the redacted output has
      the sensitive values removed.
- [ ] Open the redaction history / **Modify** view for a redacted item and confirm it loads (spans list
      shows), then close it.
- [ ] *(Optional)* Right-click a file in **Windows Explorer** → **Redact with Philter Desktop** and confirm
      it queues and redacts.
- [ ] *(Optional)* Add a **watched folder** in Settings, drop a file into it, and confirm it is redacted
      automatically to the configured output location.

## Part 4 — Uninstall

- [ ] Uninstall via **Settings → Apps** (or the Start menu uninstaller). It removes the program files and
      shortcuts.
- [ ] Confirm the install directory is gone. (User data — the encrypted database and settings under the
      user profile — is intentionally left in place; note it if the release is meant to remove it.)

---

## Sign-off

| Version | Tester | Date | Installer (Part 1) | Self-test (Part 2) | GUI + PDF (Part 3) | Uninstall (Part 4) | Result |
|---------|--------|------|:------------------:|:------------------:|:------------------:|:------------------:|:------:|
|         |        |      |                    |                    |                    |                    |        |

Record any deviations or failures with the version, the step, and a short note (and file an issue if it's
a real defect).
