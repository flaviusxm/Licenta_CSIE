Write-Host "Seeding Database..." -ForegroundColor Cyan
dotnet run --project AskNLearn.Web -- seeddb
if ($LASTEXITCODE -eq 0) {
    Write-Host "Database Seeded Successfully!" -ForegroundColor Green
} else {
    Write-Host "Seeding Failed!" -ForegroundColor Red
}
