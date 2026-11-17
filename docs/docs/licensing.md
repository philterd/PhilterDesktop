# Licensing & Support

[Philter Desktop](https://philterd.ai/philter-desktop/) is made by **Philterd**, the company behind the
[Phileas](https://github.com/philterd/phileas-dotnet) redaction engine that powers it. This page
explains how the **free and open-source software** and the **paid official product** fit together.

## The short version

- **The software is free and open source.** The
  [source code](https://github.com/philterd/PhilterDesktop) is published under the **Apache License,
  Version 2.0**, so you (or your IT department) can inspect exactly what it does, and anyone may use,
  modify, and build it.
- **The official build is free for personal use and evaluation.** You can download the official,
  signed build from [philterd.ai](https://philterd.ai) and use it at no charge for personal use or
  to try it out.
- **Work use requires a subscription.** Using the official build for work (your job, your clients, or
  an organization, even if you work alone) needs a **per-user subscription** (**$199 per user, per
  year**), with an active subscription for each person who uses it that way. The subscription also
  includes **support** and **maintained updates**.
- **"Philter" is a trademark of Philterd.** Anyone may build the open-source code, but the official
  builds are distributed under the Philter brand by Philterd.
- For the current, definitive pricing and terms, see **[philterd.ai](https://philterd.ai)**.

!!! info "Reading the license agreement in the app"
    You can read the licenses at any time from inside Philter Desktop: go to **Help → About** and click
    **View licenses**. It shows both the
    [Philterd Commercial License Agreement](https://philterd.ai/philterd-eula) (the EULA) and the
    Apache License 2.0 for the open-source code, and works offline. The same About box has **Third-party
    notices** and **Third-party licenses** links: the first lists the open-source components Philter
    Desktop includes, and the second has the full text of each one's license. The installation folder
    also contains `LICENSE.txt`, `NOTICE.txt`, and `THIRD-PARTY-LICENSES.txt`.

## When you need a subscription

The official, signed build is **free to download, and free for personal use and evaluation.** You need
a **per-user subscription** ($199 per user, per year, from [philterd.ai](https://philterd.ai)) to
use the **official build for work**, with an active subscription for each person who does so.

- **Personal use (free):** redacting your own documents, or your household's, outside of any work.
- **Evaluation (free):** trying Philter Desktop to decide whether to buy it. There is no time limit,
  but evaluation ends once you start using Philter Desktop, or the redacted files it produces, in your
  work, such as sending them to a client, filing them with a court, or delivering them to a customer.
- **Work use (subscription):** anything connected to your job, your business, or your clients,
  patients, or customers, or done for an organization, including nonprofits and government. This
  applies even if you work alone and on your own computer.

For example:

| Situation | License needed? |
|---|---|
| Redacting your own medical records before sending them to an insurer | No, personal use |
| Testing Philter Desktop on sample files before deciding to buy | No, evaluation |
| A solo attorney redacting client exhibits | Yes |
| A bookkeeper redacting client statements from home | Yes |
| An employee redacting files for their job on a personal laptop | Yes |

The full definitions are in section 4.6 of the
[Philterd Commercial License Agreement](https://philterd.ai/philterd-eula). The agreement has a
version number at the top. When a new version of Philter Desktop comes with a newer agreement, Philter
Desktop shows it once the first time you open the new version, so you can review and accept the updated
terms. The subscription permits work use and includes:

- **Support**: a direct line to the people who build Philter Desktop, for setup, policy, or workflow
  questions. Email **[support@philterd.ai](mailto:support@philterd.ai)**.
- **Maintained releases**: ongoing updates, fixes, and improvements.

The **Apache license** covers the *source code*: anyone may inspect, modify, and build it, and a
technical user is always free to build and run their own copy under that license. What it does not
provide is support or the right to use Philterd's **Philter** trademark and branding. This is the
common commercial open-source model: the [source](https://github.com/philterd/PhilterDesktop) is open
for transparency, and commercial subscriptions to the official product sustain it.

To subscribe, renew, or ask about pricing for a team or firm, visit
**[philterd.ai](https://philterd.ai)**.

## Entering your work license

There's no license key to type. Instead, Philter Desktop confirms your purchase with the email address
you bought it with:

1. Choose **Help → Manage License…**.
2. Enter your purchase email and click **Send Code**. If that address has a purchase, a code is emailed
   to it within a minute or so.
3. Enter the code and click **Verify**.

The window then says **Licensed for work use**, with your email and the date your license runs
through, and the **About** box shows the same. While your computer is online, Philter Desktop checks
the license with philterd.ai about once a month, so a renewal is picked up automatically. If your
license ends or is refunded, it's removed from the computer at the next check; nothing in Philter
Desktop is ever locked either way.

**A computer that's never online:** sign in at **[account.philterd.ai](https://account.philterd.ai)** on
another computer, click **Download license file**, copy the file across, and use **Import a license
file…** in the same window.

A license covers the number of people you bought it for. Philter Desktop doesn't count or enforce seats;
please enter it only on computers used by people your purchase covers.

## A note for IT departments

Philter Desktop runs entirely on the user's machine and stores its data locally (encrypted; see
[Settings → Security](settings.md#security-tab-protecting-your-stored-information)), so there is no
server to stand up, no cloud account to manage, and no document data leaving your environment. Because
the code is open source, your security team can confirm that for themselves rather than trusting a
sealed black box. The official signed installer supports both a per-user installation (no administrator
rights required) and an all-users installation; see [Getting Started](getting-started.md). For
deployment or support questions, contact Philterd through **[philterd.ai](https://philterd.ai)**.

**Network use.** Philter Desktop makes two kinds of network request, both over HTTPS. The first is an
update check: when its window first opens, about once a day while it runs in the background, and from
**Help → Check for Updates…**. The check is an HTTPS download of
`https://philterd.ai/philterdesktop.json`, a small file listing the latest version. The request's
User-Agent header is `PhilterDesktop/<version>` (for example, `PhilterDesktop/1.4.0`), which
philterd.ai's web server logs, along with the computer's IP address, so Philterd can count the versions
in use. These logs are deleted after about 90 days. No documents, identifiers, or other usage data are
sent. To turn automatic checks off for every user on a machine, set this registry value (users can still
choose **Help → Check for Updates…** themselves):

```
HKEY_LOCAL_MACHINE\SOFTWARE\Policies\PhilterDesktop
    DisableAutoUpdateCheck  (DWORD) = 1
```

The same value under `HKEY_CURRENT_USER` turns checks off for one user.

The second is the **work license** check, and only on computers where someone has entered a license
(see [Entering your work license](#entering-your-work-license)). Entering one sends the purchase email,
and then the emailed code, to `https://account.philterd.ai/api/desktop/license/`. After that, about once
a month while online, Philter Desktop sends its signed license token to the same address to pick up
renewals. The account site keeps a log of these events, with the IP address and the email of the
purchaser, for a year, for support. A computer with no license entered never contacts it. The license
check happens on any licensed computer that's online, however the license was entered (including from a
file), and the `DisableAutoUpdateCheck` policy doesn't turn it off. A computer that never goes online
can still be licensed with a license file; the monthly check then simply never happens.

**License acceptance.** Each Windows user accepts the license agreement the first time they open
Philter Desktop, and again once for each new version. The setup program does not
ask for acceptance, so silent and managed installs need no extra steps.

## Important reminder about redacted documents

Whatever your arrangement, keep the most important point in mind: automated redaction uses statistical
and machine-learning methods, and **no automated tool can be guaranteed to catch everything.** Every
redacted document should be **reviewed by a person** before it is shared or relied upon. This is why
Philter Desktop names its output files with the word *draft* by default. Support from Philterd can help
you configure the tool well, but the final review is always yours.
