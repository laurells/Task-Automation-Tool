# Task Automation Tool

## Overview

Task Automation Tool is a comprehensive C# application designed to automate various business processes through a flexible rule-based system. It combines modern C# development practices with Avalonia UI framework for a rich user experience.

## Description of the Software

This application provides a robust framework for automating common business tasks including:
- File operations (move, copy, delete with timestamp support)
- Bulk email sending with CSV recipient management
- Data processing and validation
- Rule-based execution system
- Command-line and GUI interfaces
- Comprehensive logging and error handling

The application is built using modern C# practices and follows SOLID principles, making it maintainable and extensible.

## Purpose for Creating this Software

This project serves multiple purposes:
1. Demonstrate advanced C# programming concepts and patterns
2. Showcase integration of multiple .NET libraries and frameworks
3. Provide a practical example of a real-world business automation solution
4. Illustrate proper software architecture and design patterns
5. Demonstrate cross-platform GUI development with Avalonia

## Development Environment

### Tools Used to Develop the Software:
- Visual Studio 2022 Community Edition
- .NET 9.0 SDK
- Git for version control
- Avalonia UI Framework (v11.3.0)
- Various NuGet packages:
  - ClosedXML for Excel operations
  - CsvHelper for CSV processing
  - MailKit for email functionality
  - System.IO.Abstractions for file operations
  - Microsoft.Extensions.Logging for logging

### Programming Language Used:
- C# 11.0
- Modern C# features including:
  - Pattern matching
  - Records
  - Top-level statements
  - Implicit usings
  - Nullable reference types

## Useful Websites

- [C# Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [Avalonia Documentation](https://avaloniaui.net/docs/)
- [Visual Studio Documentation](https://learn.microsoft.com/en-us/visualstudio/)
- [MailKit Documentation](https://github.com/jstedfast/MailKit)
- [ClosedXML Documentation](https://github.com/ClosedXML/ClosedXML)

## Getting Started

1. **Prerequisites**
   - .NET 9.0 SDK
   - Visual Studio 2022 or later

2. **Build the app:**
```bash
dotnet build
```

3. **Run the app:**
```bash
dotnet run
```

4. **Launch GUI (optional):**
```bash
dotnet run gui
```

5. **Configure the app:**
```bash
dotnet run --config
```

## Features

- **File Automation**
  - Support for file moving with timestamp options
  - Backup file functionality
  - Extension-based filtering

- **Email Automation**
  - Bulk email sending
  - CSV recipient management
  - Configurable email settings

- **Data Processing**
  - File validation
  - Required column checking
  - Data transformation capabilities

- **Rule System**
  - Configurable rules through JSON
  - Support for multiple rule types
  - Flexible settings configuration

- **Logging**
  - Detailed logging system
  - Error tracking and reporting
  - Configurable log levels

## Project Structure

- `AutomationApp/` - Main application code
  - `Models/` - Data models and DTOs
  - `Rules/` - Rule engine implementation
  - `Services/` - Business logic services
  - `Utils/` - Helper utilities
  - `Cli/` - Command-line interface

- `appsettings.json` - Main configuration
- `emailsettings.json` - Email configuration
- `recipients.csv` - Sample email recipients file
- `test-config.json` - Test configuration


## License

This project is licensed under the MIT License - see the LICENSE file for details.


