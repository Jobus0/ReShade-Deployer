# Stop the script if any command fails
$ErrorActionPreference = "Stop"

# 1. Ensure ReportGenerator is installed locally
Write-Host "Restoring .NET tools..." -ForegroundColor Cyan
dotnet tool restore

# 2. Clean old reports
if (Test-Path "TestResults") { 
    Remove-Item -Recurse -Force "TestResults" 
}

# 3. Run tests
Write-Host "Running tests..." -ForegroundColor Cyan
dotnet test --collect:"Code Coverage;Format=cobertura" --results-directory TestResults

# 4. Generate report using the local tool (prefixed with 'dotnet')
Write-Host "Generating HTML report..." -ForegroundColor Cyan
dotnet reportgenerator `
    -reports:"TestResults/**/*.cobertura.xml" `
    -targetdir:"TestResults/CoverageReport" `
    -reporttypes:Html

# 5. Open report
Start-Process "TestResults/CoverageReport/index.html"
