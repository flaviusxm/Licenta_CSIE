# Reset AskNLearn Database from Scratch
# This script drops the database, reapplies migrations, and seeds using the Enterprise scale profile.

Write-Host "`n[DB RESET] - Cleaning and Rebuilding AskNLearn Data..." -ForegroundColor Cyan

# Ensure we are in the web project folder or specify the path
$WebProject = "app/AskNLearn.Web/AskNLearn.Web.csproj"

if (-Not (Test-Path $WebProject)) {
    Write-Host "Error: Could not find project at $WebProject" -ForegroundColor Red
    exit
}

Write-Host "`nStep 1: Terminating any running instances of the app to release DB locks..." -ForegroundColor Gray
Stop-Process -Name "AskNLearn.Web" -ErrorAction SilentlyContinue

Write-Host "`nStep 2: Building project..." -ForegroundColor Gray
dotnet build $WebProject

Write-Host "`nStep 3: Running Database Drop, Migration, and Seed (Enterprise Profile)..." -ForegroundColor Yellow
Write-Host "Please wait, this will generate 100,000+ records..." -ForegroundColor Gray

dotnet run --project $WebProject -- drop-seed

Write-Host "`n[SUCCESS] Database is fresh and ready!" -ForegroundColor Green
Write-Host "Admin: admin@asknlearn.com"
Write-Host "Pass: Test@1234!" -ForegroundColor White
Write-Host "`nYou can now start the app with: dotnet run --project $WebProject`n"
