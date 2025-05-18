# Automated Smoke Test Script for Task Automation Tool

# Configuration
$projectPath = "C:\Users\USER\Desktop\Task-Automation-Tool\AutomationApp"
$testConfigPath = Join-Path $projectPath "test-config.json"
$testSourceDir = "C:\Users\USER\Desktop\Task-Automation-Tool\Source"
$testTargetDir = "C:\Users\USER\Desktop\Task-Automation-Tool\Target"
$testFile = "testfile.pdf"

# Create test directories if they don't exist
if (-not (Test-Path $testSourceDir)) {
    New-Item -ItemType Directory -Path $testSourceDir -Force
    Write-Host "Created source directory: $testSourceDir" -ForegroundColor Green
}

if (-not (Test-Path $testTargetDir)) {
    New-Item -ItemType Directory -Path $testTargetDir -Force
    Write-Host "Created target directory: $testTargetDir" -ForegroundColor Green
}

# Create logs directory for application
$logDir = Join-Path $projectPath "logs"
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force
    Write-Host "Created logs directory: $logDir" -ForegroundColor Green
}

# Create test file
$testFileName = "testfile.pdf"
$testFilePath = Join-Path $testSourceDir $testFileName
if (-not (Test-Path $testFilePath)) {
    $fileContent = "This is a test PDF file.`nCreated for Task Automation Tool testing."
    $fileContent | Set-Content -Path $testFilePath -Encoding UTF8
    Write-Host "Created test file: $testFilePath" -ForegroundColor Green
}

# Create test configuration
$testConfig = @{
    Version = "1.0"
    Logging = @{
        Level = "Debug"
        Console = @{
            Enabled = $true
        }
        File = @{
            Enabled = $true
            Path = "logs/utomation.log"
        }
    }
    Email = @{
        SmtpHost = "smtp.example.com"
        SmtpPort = 587
        UseSmtpSsl = $true
        Email = "your-email@example.com"
        Password = "your-password"
        ImapHost = "imap.example.com"
        ImapPort = 993
        UseImapSsl = $true
    }
    Services = @{
        FileService = @{
            Enabled = $true
            HashAlgorithm = "SHA256"
            MaxRetries = 3
            RetryDelayMs = 1000
        }
    }
    Rules = @(
        @{
            Type = "FileMoveRule"
            Name = "MovePDFsToSorted"
            Enabled = $true
            Settings = @{
                source = "C:\Watch"
                target = "C:\Sorted"
                supportedExtensions = @(".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt")
                addTimestamp = $false
                backupFiles = $false
            }
        }
    )
} | ConvertTo-Json -Depth 10

# Write test configuration
$testConfig | Set-Content $testConfigPath

# Function to run command and capture output
function Run-Command {
    param(
        [string]$Command,
        [string]$WorkingDirectory
    )
    
    $dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Path
    if (-not $dotnetPath) {
        throw "dotnet command not found in PATH"
    }

    Push-Location $WorkingDirectory
    try {
        $output = & $dotnetPath $Command
        return @{
            Output = $output
            Success = $?
        }
    } finally {
        Pop-Location
    }
}

# Run smoke test
Write-Host "Starting Smoke Test..." -ForegroundColor Green

# 1. Build the project (optional - uncomment to build before run)
# Write-Host "Building project..." -ForegroundColor Yellow
# $buildResult = Run-Command -Command "build" -WorkingDirectory $projectPath
# if (-not $buildResult.Success) {
#     Write-Host "Build failed:" -ForegroundColor Red
#     Write-Host $buildResult.Output
#     exit 1
# }

# 2. Run CLI with test config
Write-Host "Running CLI with test config..." -ForegroundColor Yellow
$cliResult = Run-Command -Command "run" -WorkingDirectory $projectPath

if (-not $cliResult.Success) {
    Write-Host "CLI execution failed:" -ForegroundColor Red
    Write-Host $cliResult.Output
    exit 1
}

# 3. Verify file was moved
Write-Host "Verifying file move..." -ForegroundColor Yellow
$targetFilePath = Join-Path $testTargetDir $testFileName
if (Test-Path $targetFilePath) {
    Write-Host "File move verified successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "File move verification failed: File not found in target directory" -ForegroundColor Red
    exit 1
}

# 4. Clean up
Write-Host "Cleaning up test files..." -ForegroundColor Yellow
Remove-Item -Path $testConfigPath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $testFilePath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $targetFilePath -Force -ErrorAction SilentlyContinue

Write-Host "Smoke test completed successfully!" -ForegroundColor Green
