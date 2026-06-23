# Supported Filters

Philter Desktop can detect and redact the following types of PII. Enable the ones you need in the
[Policy Editor](policies.md); each can be tuned with [filter strategies](filter-strategies.md).

## Personal

- First Name
- Surname
- Age
- **Names (on-device AI)** — see [AI name detection](#ai-name-detection) below

## Contact

- Email Address
- Phone Number
- Phone Number Extension

## Location

- City
- County
- State
- State Abbreviation
- Zip Code
- Street Address

## Financial

- Credit Card
- Bank Routing Number
- IBAN Code
- Bitcoin Address
- Currency

## Identifiers

- SSN (Social Security Number)
- Driver's License
- Passport Number
- VIN (Vehicle Identification Number)
- Tracking Number

## Technical

- IP Address
- MAC Address
- URL

## Medical

- Hospital

## Other

- Date

## AI name detection

The **Names (on-device AI)** filter (in the **Personal** group) detects person names using a
bundled machine-learning model ([PhEye](https://github.com/philterd/phileas-net)) that runs
entirely on your computer with no network call. Names are the one PII type that genuinely needs a
model, because they have no fixed format and depend on context; the pattern-based filters above
handle the rest. See [Policies → AI name detection](policies.md#ai-name-detection).

## Custom Identifiers

Define your own regular-expression patterns to redact organization-specific identifiers. See
[Policies → Custom Identifiers](policies.md#custom-identifiers).

> The available filters are provided by the underlying
> [Phileas](https://github.com/philterd/phileas-net) engine, so this list may grow over time. The
> Policy Editor always shows the filters available in your installed version.
