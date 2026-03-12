Write-Host "Running Database Migrations..." -ForegroundColor Cyan
dotnet run --project AskNLearn.Web -- migratedb
if ($LASTEXITCODE -eq 0) {
    Write-Host "Migrations Applied Successfully!" -ForegroundColor Green
} else {
    Write-Host "Migrations Failed!" -ForegroundColor Red
}
