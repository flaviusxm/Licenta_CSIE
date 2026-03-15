Write-Host "Dropping and Re-seeding Database..." -ForegroundColor Cyan
dotnet run --project AskNLearn.Web -- drop-seed
if ($LASTEXITCODE -eq 0) {
    Write-Host "Database Refreshed and Seeded Successfully!" -ForegroundColor Green
} else {
    Write-Host "Database Refresh Failed!" -ForegroundColor Red
}
