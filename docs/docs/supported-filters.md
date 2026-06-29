# Supported Filters

This page lists every kind of sensitive information Philter Desktop knows how to find. Each one is a
**filter** — a detector you can switch on or off. You turn on the ones you need in the
[Policy Editor](policies.md), and you can control how each one replaces what it finds using
[filter strategies](filter-strategies.md).

You don't have to use all of them. A typical policy turns on just the handful that matter for the
documents you're working with. The detectors are grouped by category to make them easy to find.

## Personal

Information about who a person is.

- **First Name** — a person's given name (e.g., *Jane*).
- **Surname** — a person's family or last name (e.g., *Doe*).
- **Age** — a stated age (e.g., *42 years old*).
- **Names (on-device AI)** — a smarter, context-aware detector for full person names, powered by
  artificial intelligence that runs entirely on your computer. Because names are tricky (the same
  word can be a name or an ordinary word depending on the sentence), this detector is more reliable
  than a simple rule. See [AI name detection](#ai-name-detection) below.

## Contact

Ways of reaching a person.

- **Email Address** — e.g., *jane.doe@example.com*.
- **Phone Number** — e.g., *(555) 123-4567*.
- **Phone Number Extension** — an extension that follows a phone number (e.g., *ext. 204*).

## Location

Where a person is or lives.

- **City**
- **County**
- **State** — the full state name (e.g., *California*).
- **State Abbreviation** — the two-letter form (e.g., *CA*).
- **Zip Code**
- **Street Address** — a street-level address (e.g., *123 Main St*).

## Financial

Money- and account-related details, the kind that often appear in discovery, bank statements, and
billing records.

- **Credit Card** — a credit-card number.
- **Bank Routing Number** — the bank-identifying number on checks and transfers.
- **IBAN Code** — an International Bank Account Number, used for international transfers.
- **Bitcoin Address** — an identifier for a cryptocurrency wallet.
- **Currency** — a monetary amount (e.g., *$5,000.00*).

## Identifiers

Official numbers that single out a specific person or thing.

- **SSN (Social Security Number)** — e.g., *123-45-6789*.
- **Driver's License** — a driver's license number.
- **Passport Number**
- **VIN (Vehicle Identification Number)** — the unique number identifying a specific vehicle.
- **Tracking Number** — a shipment/package tracking number.

## Technical

Identifiers that come from computers and networks. These often turn up in emails, logs, and digital
records produced in discovery.

- **IP Address** — the numeric address of a device on a network (e.g., *192.168.0.1*).
- **MAC Address** — a hardware identifier built into a network device.
- **URL** — a web address (e.g., *https://example.com/page*).

## Medical

- **Hospital** — the name of a hospital or medical facility, common in personal-injury and
  medical-records work.

## Other

- **Date** — a calendar date (e.g., *January 5, 2024*). Dates can be sensitive on their own —
  a date of birth, a date of treatment, or a date of incident.

## AI name detection

The **Names (on-device AI)** detector (in the **Personal** group above) finds person names using a
small, trained artificial-intelligence model (called **PhEye**) that runs **entirely on your own
computer, with no connection to the internet**. Your documents are never uploaded or sent anywhere.

Names are the one kind of information that genuinely benefits from this approach, because they have no
fixed shape and depend on the surrounding words — the simple, pattern-based detectors above handle
everything else just fine. For the full explanation and how to turn it on, see
[Policies → AI name detection](policies.md#detecting-names-with-on-device-ai).

## Your own identifiers (Custom Identifiers)

If you need to remove information that follows your organization's own format — a case number, a
client matter ID, an internal account number — you can define your own detectors. See
[Policies → Custom Identifiers](policies.md#redacting-your-own-special-identifiers-custom-identifiers).

> The list of available detectors comes from the redaction engine inside Philter Desktop, so it may
> grow over time as new versions are released. The Policy Editor always shows exactly the detectors
> available in the version you have installed, so it's the definitive, up-to-date list.
