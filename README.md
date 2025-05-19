# Task Automation Tool

## Overview

Task Automation Tool is a modern C# application designed to automate business processes through a flexible, rule-based system. Built with .NET 9.0 and Avalonia UI, it offers both a command-line interface (CLI) and a graphical user interface (GUI) for managing file operations, bulk email sending, and data processing tasks. The tool emphasizes modularity, maintainability, and extensibility, following SOLID principles.

## Description of the Software

This application automates common business tasks via a configurable rule engine, supporting:

- File operations (move, copy, delete with timestamp and backup options)
- Bulk email sending with CSV-based recipient management
- Data processing for CSV files, including header extraction and row data retrieval
- Interactive CLI for rule configuration, testing, and execution
- Cross-platform GUI for visual rule management
- Comprehensive logging to a centralized directory

The application integrates modern C# practices, dependency injection, and a robust service architecture to ensure scalability and ease of maintenance.

## Purpose for Creating this Software

The project serves several purposes:

- Demonstrate advanced C# programming with modern .NET features
- Showcase a modular architecture for business automation
- Provide a practical example of integrating CLI and GUI interfaces
- Illustrate cross-platform development with Avalonia UI
- Serve as a foundation for extensible automation workflows

## Development Environment

### Tools Used to Develop the Software:

- Visual Studio 2022 Community Edition
- .NET 9.0 SDK
- Git for version control
- Avalonia UI Framework (v11.3.0)
- NuGet packages:
  - System.Text.Json for JSON serialization
  - Microsoft.Extensions.DependencyInjection for dependency injection
  - Microsoft.Extensions.Logging for logging
  - System.IO.Abstractions for file operations
  - MailKit for email functionality (optional, for BulkEmailRule)

### Programming Language Used:

- C# 11.0
- Modern C# features:
  - Pattern matching
  - Records
  - Nullable reference types
  - Implicit usings
  - Minimal APIs (for CLI)

## Useful Websites

- [C# Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [Avalonia Documentation](https://avaloniaui.net/docs/)
- [Visual Studio Documentation](https://learn.microsoft.com/en-us/visualstudio/)
- [MailKit Documentation](https://github.com/jstedfast/MailKit)
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)

## Video Link
[Task Automation Tool Video](https://youtu.be/F0LU425Zvnc)

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or later (optional)
- Windows, macOS, or Linux for cross-platform support

### Clone the Repository

```bash
git clone https://github.com/your-repo/task-automation-tool.git
cd task-automation-tool
```

### Build the App

```bash
dotnet build
```

### Run the App (CLI)

```bash
dotnet run -- <command>
```

Available commands:

- `run`: Execute all rules
- `test`: Test a specific rule
- `configure`: Interactively configure rules
- `list-rules`: List all configured rules
- `validate-rules`: Validate rule configurations
- `status`: Show rule status
- `schedule --interval <seconds>`: Schedule periodic rule execution

### Run the App (GUI)

```bash
dotnet run -- gui
```

### Configure Rules

Edit `config.rules.json` manually or use:

```bash
dotnet run -- configure
```

## Features

### File Automation (FileMoveRule)

- Move files between directories with optional timestamp suffixes
- Backup files before moving
- Filter files by extensions

### Email Automation (BulkEmailRule)

- Send bulk emails using SMTP
- Load recipients from CSV files
- Configurable via `appsettings.json`

### Data Processing (DataProcessingRule)

- Parse CSV files, returning headers and all row data
- Optional column validation
- Log parsed headers and rows for verification

### Rule System

- JSON-based rule configuration (`config.rules.json`)
- Supports multiple rule types (FileMoveRule, BulkEmailRule, DataProcessingRule)
- Dynamic rule loading and validation

### CLI Interface

- Interactive rule configuration and testing
- Command-driven execution with detailed feedback
- Supports scheduling for periodic tasks

### GUI Interface

- Cross-platform Avalonia UI
- Visual rule management and execution
- Real-time status updates

### Logging

- Centralized logging to `C:\AppLogs` (configurable)
- Configurable log levels (Info, Warning, Error)
- Console and file output

## Project Structure

```
AutomationApp/ - Main application code
├── Cli/ - Command-line interface (CommandHandler.cs)
├── Core/ - Core abstractions (AutomationEngine.cs, Logger.cs)
├── Interfaces/ - Service contracts (IEmailService.cs, IDataService.cs, IAutomationRule.cs)
├── Models/ - Data models (RuleConfig.cs, EmailConfiguration.cs)
├── Rules/ - Rule implementations (FileMoveRule.cs, BulkEmailRule.cs, DataProcessingRule.cs)
├── Services/ - Business logic (DataService.cs, EmailService.cs, FileService.cs)
└── Utils/ - Utilities (RuleConfigLoader.cs)
```

## Configuration

### appsettings.json

```json
{
  "RulesConfigPath": "config.rules.json",
  "Email": {
    "SmtpHost": "",
    "SmtpPort": 587,
    "Email": "",
    "Password": "",
    "UseSmtpSsl": true,
    "ImapHost": "",
    "ImapPort": 993,
    "UseImapSsl": true
  },
  "Logging": {
    "LogDirectory": "C:\\AppLogs",
    "LogLevel": "Info",
    "EnableConsoleOutput": true,
    "EnableFileLogging": true,
    "EnableErrorLogging": true
  }
}
```

### config.rules.json (example)

```json
[
  {
    "type": "FileMoveRule",
    "name": "movetofile_desktop",
    "source": "C:\\Users\\USER\\Desktop",
    "target": "C:\\Users\\USER\\Downloads",
    "supportedExtensions": [],
    "addTimestamp": false,
    "backupFiles": false
  },
  {
    "type": "DataProcessingRule",
    "name": "processtestdata",
    "filePath": "C:\\Users\\USER\\Desktop\\Task-Automation-Tool\\AutomationApp\\data\\testdata.csv",
    "requiredColumns": []
  }
]
```

### Sample testdata.csv

```csv
ID,Price
1,10.99
2,15.99
3,20.99
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.