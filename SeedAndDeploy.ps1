<#
.SYNOPSIS
    Genereaza baza de date local si o deployeaza pe serverul tinta
.DESCRIPTION
    Acest script:
    1. Ruleaza seed-ul local cu volume mari
    2. Face backup la baza de date
    3. Copiaza backup-ul pe server
    4. Restaureaza baza de date pe server
.PARAMETER TargetServer
    Serverul SQL tinta (ex: "SERVER\INSTANCE" sau "localhost")
.PARAMETER TargetDatabase
    Numele bazei de date pe server (default: "asknlearn")
.PARAMETER LocalOnly
    Genereaza doar local, fara deploy pe server
.PARAMETER DeployOnly
    Doar deployeaza un backup existent pe server (fara seed)
.PARAMETER BackupPath
    Unde se salveaza backup-ul (default: Desktop)
#>

param(
    [string]$TargetServer = "",
    [string]$TargetDatabase = "asknlearn",
    [switch]$LocalOnly,
    [switch]$DeployOnly,
    [string]$BackupPath = "",
    [string]$LocalConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=asknlearn;Trusted_Connection=True;",
    [string]$TargetConnectionString = "",
    [PSCredential]$TargetCredential = $null
)

# ─── INCARCARE CONFIGURARE ──────────────────────────────────────────────────
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$appSettingsPath = Join-Path $scriptDir "app\AskNLearn.Web\appsettings.Development.json"
$webProjectPath = Join-Path $scriptDir "app\AskNLearn.Web"

if (Test-Path $appSettingsPath) {
    try {
        $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        $rawConnString = $appSettings.ConnectionStrings.DefaultConnection
        
        if ($rawConnString) {
            # Parsare connection string pentru TargetServer daca nu e specificat
            if ([string]::IsNullOrEmpty($TargetServer)) {
                if ($rawConnString -match "Server=([^;]+)") { $TargetServer = $Matches[1] }
                elseif ($rawConnString -match "Data Source=([^;]+)") { $TargetServer = $Matches[1] }
            }
            
            # Parsare TargetDatabase daca e cel default
            if ($TargetDatabase -eq "asknlearn") {
                if ($rawConnString -match "Database=([^;]+)") { $TargetDatabase = $Matches[1] }
                elseif ($rawConnString -match "Initial Catalog=([^;]+)") { $TargetDatabase = $Matches[1] }
            }

            # Setare TargetConnectionString daca nu e specificat
            if ([string]::IsNullOrEmpty($TargetConnectionString)) {
                $TargetConnectionString = $rawConnString
                # Ne asiguram ca are TrustServerCertificate=True pentru servere de test
                if ($TargetConnectionString -notmatch "TrustServerCertificate") {
                    $TargetConnectionString += ";TrustServerCertificate=True"
                }
            }
        }
    } catch {
        Write-Host "  ! Atentie: Nu am putut citi appsettings.Development.json: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Configurare
$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

if ([string]::IsNullOrEmpty($BackupPath)) {
    $BackupPath = [Environment]::GetFolderPath("Desktop") + "\asknlearn_seeded_$timestamp.bak"
}

Write-Host @"
----------------------------------------------------------------------
           SEED & DEPLOY AUTOMAT - ASK N LEARN                    
----------------------------------------------------------------------
  LocalOnly:      $($LocalOnly.ToString())
  DeployOnly:     $($DeployOnly.ToString())
  Backup Path:    $BackupPath
  Target Server:  $TargetServer
  Target DB:      $TargetDatabase
----------------------------------------------------------------------
"@ -ForegroundColor Cyan

# Functie pentru executare SQL
function Invoke-SqlCommand {
    param(
        [string]$ConnectionString,
        [string]$Query,
        [string]$Database = "master"
    )
    
    try {
        # Inlocuim baza de date existenta in connection string cu cea dorita (ex: master)
        $cleanConnString = $ConnectionString
        if ($cleanConnString -match "(Database|Initial Catalog)=([^;]+)") {
            $cleanConnString = $cleanConnString -replace "(Database|Initial Catalog)=([^;]+)", "Database=$Database"
        } elseif ($cleanConnString -notmatch "Database=") {
            if ($cleanConnString -notmatch ";$") { $cleanConnString += ";" }
            $cleanConnString += "Database=$Database"
        }

        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = $cleanConnString
        
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Query
        $cmd.CommandTimeout = 600
        $result = $cmd.ExecuteNonQuery()
        $conn.Close()
        return $result
    }
    catch {
        Write-Host "  [X] Eroare SQL: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

# Functie pentru backup
function Backup-Database {
    param(
        [string]$ConnectionString,
        [string]$DatabaseName,
        [string]$BackupFile
    )
    
    Write-Host "`n[BACKUP] Creare backup pentru '$DatabaseName'..." -ForegroundColor Yellow
    
    $backupDir = Split-Path -Parent $BackupFile
    if (!(Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }
    
    $query = @"
BACKUP DATABASE [$DatabaseName] 
TO DISK = '$BackupFile' 
WITH COMPRESSION, INIT, STATS = 10;
"@
    
    try {
        Invoke-SqlCommand -ConnectionString $ConnectionString -Query $query -Database "master"
        $fileSize = [math]::Round((Get-Item $BackupFile).Length / 1MB, 2)
        Write-Host "  [V] Backup creat: $BackupFile ($fileSize MB)" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  [X] Eroare la backup: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Functie pentru restore
function Restore-Database {
    param(
        [string]$ConnectionString,
        [string]$DatabaseName,
        [string]$BackupFile
    )
    
    Write-Host "`n[RESTORE] Restaurare '$DatabaseName' din backup..." -ForegroundColor Yellow
    
    # Verifica daca fisierul exista
    if (!(Test-Path $BackupFile)) {
        Write-Host "  [X] Fisierul de backup nu exista: $BackupFile" -ForegroundColor Red
        return $false
    }
    
    # Gaseste locatia fisierelor de date pe server (folosind master)
    $cleanConnString = $ConnectionString
    if ($cleanConnString -match "(Database|Initial Catalog)=([^;]+)") {
        $cleanConnString = $cleanConnString -replace "(Database|Initial Catalog)=([^;]+)", "Database=master"
    } elseif ($cleanConnString -notmatch "Database=") {
        if ($cleanConnString -notmatch ";$") { $cleanConnString += ";" }
        $cleanConnString += "Database=master"
    }

    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $cleanConnString
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT SERVERPROPERTY('InstanceDefaultDataPath') AS DataPath"
    $dataPath = $cmd.ExecuteScalar()
    $conn.Close()
    
    if ([string]::IsNullOrEmpty($dataPath)) {
        $dataPath = "C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\"
    }
    
    # Obtine lista fisierelor din backup
    $fileListQuery = "RESTORE FILELISTONLY FROM DISK = '$BackupFile'"
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $fileListQuery
    $reader = $cmd.ExecuteReader()
    $files = @()
    while ($reader.Read()) {
        $files += @{
            LogicalName = $reader["LogicalName"]
            PhysicalName = $reader["PhysicalName"]
            Type = $reader["Type"]
        }
    }
    $reader.Close()
    $conn.Close()
    
    # Construieste comanda RESTORE cu MOVE
    $moveClauses = @()
    foreach ($file in $files) {
        $fileName = [System.IO.Path]::GetFileName($file.PhysicalName)
        $newPath = Join-Path $dataPath $fileName
        $moveClauses += ", MOVE '$($file.LogicalName)' TO '$newPath'"
    }
    $moveString = $moveClauses -join " "
    
    $restoreQuery = @"
RESTORE DATABASE [$DatabaseName] 
FROM DISK = '$BackupFile' 
WITH REPLACE, RECOVERY, STATS = 10 $moveString;
"@
    
    try {
        # Inchide conexiunile active
        $killQuery = @"
USE master;
IF EXISTS (SELECT name FROM sys.databases WHERE name = '$DatabaseName')
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
"@
        Invoke-SqlCommand -ConnectionString $ConnectionString -Query $killQuery
        
        # Restaureaza
        Invoke-SqlCommand -ConnectionString $ConnectionString -Query $restoreQuery
        
        # Seteaza multi-user
        $multiUserQuery = "ALTER DATABASE [$DatabaseName] SET MULTI_USER;"
        Invoke-SqlCommand -ConnectionString $ConnectionString -Query $multiUserQuery
        
        Write-Host "  [V] Restaurare completa!" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  [X] Eroare la restaurare: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Functie pentru seed local
function Start-LocalSeed {
    Write-Host "`n[SEED] Generare date locale..." -ForegroundColor Yellow
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        # Setam variabila de mediu pentru ca seed-ul sa se faca LOCAL
        $oldConn = $env:ConnectionStrings__DefaultConnection
        $env:ConnectionStrings__DefaultConnection = $LocalConnectionString
        
        Write-Host "  Folosind baza de date locala: $LocalConnectionString" -ForegroundColor Gray

        # Ruleaza dotnet run pe proiectul Web
        $process = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$webProjectPath`" -- drop-seed" -WorkingDirectory $scriptDir -PassThru -NoNewWindow -Wait
        
        # Restauram variabila de mediu
        $env:ConnectionStrings__DefaultConnection = $oldConn
        
        if ($process.ExitCode -ne 0) {
            Write-Host "  [X] Eroare la seed! Exit code: $($process.ExitCode)" -ForegroundColor Red
            return $false
        }
        
        $stopwatch.Stop()
        Write-Host "  [V] Seed complet in $($stopwatch.Elapsed.TotalMinutes.ToString('F1')) minute!" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "  [X] Eroare la seed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Functie pentru copierea backup-ului pe server
function Copy-BackupToServer {
    param(
        [string]$SourcePath,
        [string]$TargetServer,
        [PSCredential]$Credential
    )
    
    Write-Host "`n[COPY] Copiere backup pe server..." -ForegroundColor Yellow
    
    $targetPath = "\\$TargetServer\C`$\Temp\asknlearn_seeded.bak"
    
    try {
        if ($Credential) {
            $networkCred = $Credential.GetNetworkCredential()
            $pswd = $networkCred.Password
            $user = $networkCred.UserName
            net use "\\$TargetServer\C`$" $pswd /USER:$user /PERSISTENT:NO | Out-Null
        }
        
        Copy-Item -Path $SourcePath -Destination $targetPath -Force
        
        if ($Credential) {
            net use "\\$TargetServer\C`$" /DELETE | Out-Null
        }
        
        Write-Host "  [V] Copiat pe server: $targetPath" -ForegroundColor Green
        return $targetPath
    }
    catch {
        Write-Host "  [X] Eroare la copiere: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Copiati manual '$SourcePath' pe server in 'C:\Temp\'" -ForegroundColor Yellow
        return $null
    }
}

# --- EXECUTIE ---

if ($DeployOnly) {
    if ([string]::IsNullOrEmpty($TargetServer)) {
        Write-Host "`n[ERROR] Trebuie specificat -TargetServer pentru deploy!" -ForegroundColor Red
        exit 1
    }
    
    if ([string]::IsNullOrEmpty($TargetConnectionString)) {
        if ($TargetCredential) {
            $user = $TargetCredential.UserName
            $pass = $TargetCredential.GetNetworkCredential().Password
            $TargetConnectionString = "Server=$TargetServer;Database=master;User Id=$user;Password=$pass;TrustServerCertificate=True;"
        } else {
            $TargetConnectionString = "Server=$TargetServer;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
        }
    }
    
    $success = Restore-Database -ConnectionString $TargetConnectionString -DatabaseName $TargetDatabase -BackupFile $BackupPath
    exit
}

if (-not $LocalOnly -and [string]::IsNullOrEmpty($TargetServer)) {
    Write-Host "`n[WARNING] Nu s-a specificat -TargetServer. Se va face doar seed local." -ForegroundColor Yellow
    $LocalOnly = $true
}

# Seed local
if (Start-LocalSeed) {
    # Backup local
    $localDbName = "asknlearn"
    if ($LocalConnectionString -match "Database=([^;]+)") { $localDbName = $Matches[1] }
    elseif ($LocalConnectionString -match "Initial Catalog=([^;]+)") { $localDbName = $Matches[1] }

    if (Backup-Database -ConnectionString $LocalConnectionString -DatabaseName $localDbName -BackupFile $BackupPath) {
        if ($LocalOnly) {
            Write-Host "`n[V] SEED LOCAL COMPLET!" -ForegroundColor Green
            exit
        }

        # Copiaza si Restaureaza
        $remoteBackupPath = Copy-BackupToServer -SourcePath $BackupPath -TargetServer $TargetServer -Credential $TargetCredential
        if ($remoteBackupPath) {
            if ([string]::IsNullOrEmpty($TargetConnectionString)) {
                if ($TargetCredential) {
                    $user = $TargetCredential.UserName
                    $pass = $TargetCredential.GetNetworkCredential().Password
                    $TargetConnectionString = "Server=$TargetServer;Database=master;User Id=$user;Password=$pass;TrustServerCertificate=True;"
                } else {
                    $TargetConnectionString = "Server=$TargetServer;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
                }
            }
            Restore-Database -ConnectionString $TargetConnectionString -DatabaseName $TargetDatabase -BackupFile $remoteBackupPath
        }
    }
}