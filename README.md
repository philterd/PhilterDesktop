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

## License

Copyright   2026 Mountain Fog, Inc.

