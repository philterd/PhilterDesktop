# PhilterDesktop

A Windows desktop application for managing and testing Philter policies with integrated policy editor.

## Overview

PhilterDesktop is a Windows Forms application that provides a user-friendly interface for:
- Testing PII (Personally Identifiable Information) filtering using the Phileas library
- Creating and editing Philter filter policies with a visual policy editor
- Storing filter results in a local LiteDB database

## Architecture

### Solution Structure

The solution consists of four projects:

1. **PhilterDesktop** (.NET 10.0-windows)
   - Main Windows Forms application
   - Entry point for the desktop experience
   - Integrates the Phileas filtering engine
   - Uses LiteDB for local data persistence

2. **PhilterPolicyEditor** (.NET 10.0-windows, VB.NET)
   - Visual policy editor component
   - Provides forms for configuring filter strategies
   - Supports multiple PII entity types (emails, phone numbers, SSNs, credit cards, etc.)
   - Referenced as a library by PhilterDesktop

3. **Policies** (.NET Framework 4.8.1, C#)
   - Policy model definitions
   - Filter configuration classes
   - Shared data structures for policy management

4. **Phileas** (External dependency)
   - Core filtering engine
   - Located at: `../../phileas-net/src/Phileas/`
   - Provides the actual PII detection and filtering logic

### Key Technologies

- **.NET 10.0** - Latest .NET runtime
- **Windows Forms** - Desktop UI framework
- **LiteDB 5.0.21** - Embedded NoSQL database
- **VB.NET** - Policy editor forms
- **C#** - Main application and policy models

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

### Main Application
- **Filter Testing**: Test PII filtering with custom input text
- **Policy Management**: Create, edit, and delete filter policies
- **Result History**: View and manage previous filter results (stored in LiteDB)
- **Menu Navigation**: 
  - File ? Exit
  - Tools ? Policies (opens Policy Editor)
  - Help ? About

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

## Data Storage

The application stores filter results in a local LiteDB database:
- **Location**: `%LocalAppData%\PhilterDesktop\data.db`
- **Schema**: 
  ```csharp
  {
      ObjectId Id,
      string FilteredText,
      DateTime CreatedAt
  }
  ```

## Migration Notes (.NET Framework ? .NET 10)

This solution was recently upgraded from .NET Framework 4.8.1 to .NET 10.0. Key changes:

### BinaryFormatter Removal
- **.NET 10 disables BinaryFormatter** for security reasons
- All Windows Forms resource files (`.resx`) are compatible with the modern serialization format
- Icon and bitmap resources use `bytearray.base64` encoding instead of `binary.base64`

### VB.NET Project Updates
- Upgraded `PhilterPolicyEditor` from `net481` ? `net10.0-windows`
- Added explicit global imports to maintain VB.NET namespace resolution
- Removed `System.Data.DataSetExtensions` (legacy .NET Framework package)
- Suppressed `WFO1000` warnings (Windows Forms analyzer for code serialization)

### Known Issues Fixed
- **WinRT namespace conflict**: The top-level `Windows` namespace in WinRT conflicts with `System.Windows` shorthand in VB.NET on `net10.0-windows`. Solution: Removed global `System` import to prevent ambiguity.
- **IContainer ambiguity**: Resolved by removing conflicting global imports
- **DialogResult references**: Maintained as `DialogResult.OK` (with `System.Windows.Forms` global import)

## Development

### Project Structure
```
PhilterDesktop/
??? PhilterDesktop/          # Main WinForms app (.NET 10)
?   ??? Form1.cs            # Main form
?   ??? Program.cs          # Entry point
?   ??? LiteDbRepository.cs # Database wrapper
??? PhilterPolicyEditor/     # Policy editor library (.NET 10, VB.NET)
?   ??? PolicyEditorForm.vb # Main policy editor
?   ??? NewPolicyForm.vb    # Create new policy
?   ??? ViewJsonForm.vb     # View policy JSON
?   ??? PolicyEditor/       # Filter strategy forms
??? Policies/                # Policy models (.NET Framework 4.8.1)
    ??? Model/Policy/       # Filter definitions
```

### Adding New Filter Types

1. Add model class in `Policies/Model/Policy/Filters/`
2. Create strategy class in `Policies/Model/Policy/Filters/Strategies/`
3. Add VB form in `PhilterPolicyEditor/PolicyEditor/FilterReplacementStrategies/`
4. Update `PolicyEditorForm.vb` to include new filter checkbox and configuration

### Debugging

Enable verbose logging:
- Debug ? Windows ? Output
- Select "Debug" from dropdown
- Look for exceptions in `System.Private.CoreLib.dll`

## Contributing

This is part of the Philter ecosystem maintained by Mountain Fog, Inc.

## License

Copyright © 2021 Mountain Fog, Inc.

## Links

- [Phileas.NET Repository](https://github.com/philterd/phileas-net)
- [BinaryFormatter Migration Guide](https://aka.ms/binaryformatter)

## Troubleshooting

### Application crashes on startup
**Symptom**: `System.NotSupportedException: BinaryFormatter serialization and deserialization are disabled`

**Solution**: Ensure all projects target .NET 10.0-windows (not .NET Framework). This was resolved in the latest version.

### Build errors: 'Forms' is not a member of 'Windows'
**Symptom**: VB.NET compilation errors with namespace resolution

**Solution**: Check that `PhilterPolicyEditor.vbproj` does not include `System` in global imports (causes WinRT `Windows` namespace conflict).

### Missing database
The application automatically creates the database directory and file on first run at:
```
%LocalAppData%\PhilterDesktop\data.db
```
