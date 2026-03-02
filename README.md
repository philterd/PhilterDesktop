# Philter Desktop

A Windows desktop application for redacting text in text files, Microsoft Word files, and PDF files.

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- .NET Framework 4.8.1 Developer Pack
- Visual Studio 2022 (version 17.12 or later recommended)
- Windows 10/11

### Building the Solution

1. Clone the repository and its dependencies:
   ```bash
   git clone https://github.com/philterd/PhilterDesktop
   git clone https://github.com/philterd/phileas-net
   ```

2. Open `PhilterDesktop.sln` in Visual Studio

3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

4. Build the solution:
   ```bash
   dotnet build
   ```

### Running the Application

- Press F5 in Visual Studio, or
- Run from command line:
  ```bash
  dotnet run --project PhilterDesktop\PhilterDesktop.csproj
  ```

## Features

### Policy Editor
Supports configuration for the following PII entity types:
- Personal: First Names, Surnames, Ages
- Contact: Email Addresses, Phone Numbers, Phone Extensions
- Location: Addresses, Cities, Counties, States, State Abbreviations, Zip Codes
- Medical: Hospital Names, Hospital Abbreviations
- Financial: Credit Cards, SSNs
- Technical: IP Addresses, URLs, VINs
- Custom: Custom Dictionaries, Custom Identifiers
- Advanced: NER (Named Entity Recognition)

Each filter supports multiple strategies:
- **Redaction**: Replace with format string (e.g., `{{{REDACTED-email}}}`)
- **Static Replacement**: Replace with fixed text
- **Random Replacement**: Replace with randomly generated value
- **Conditional Filtering**: Apply filters based on conditions
- **Scope Control**: Document-level or context-level replacement

## License

Copyright 2026 Philterd, LLC.

