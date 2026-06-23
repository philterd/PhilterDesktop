# Supported Filters

Philter Desktop can detect and redact the following types of PII. Enable the ones you need in the
[Policy Editor](policies.md); each can be tuned with [filter strategies](filter-strategies.md).

## Personal

- First Name
- Surname
- Age

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

## Custom Identifiers

Define your own regular-expression patterns to redact organization-specific identifiers. See
[Policies → Custom Identifiers](policies.md#custom-identifiers).

> The available filters are provided by the underlying
> [Phileas](https://github.com/philterd/phileas-net) engine, so this list may grow over time. The
> Policy Editor always shows the filters available in your installed version.
