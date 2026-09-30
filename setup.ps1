<#
.SYNOPSIS
    Installs what this project needs on Windows: the .NET 10 SDK, SQL Server Express LocalDB,
    and the EF Core command-line tool (dotnet-ef). Anything already installed is skipped.

.DESCRIPTION
    Uses winget, which comes with Windows 10 and 11 as part of "App Installer".
    Installing LocalDB needs administrator rights, so Windows asks for permission at that step.
    Report A needs LocalDB 2017 or later. If only an older LocalDB is found, the script asks
    before installing a newer one, and again before switching the MSSQLLocalDB instance to it.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\setup.ps1
#>

$ErrorActionPreference = 'Stop'

function Write-Step([string] $message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

# Installers update the saved PATH, but a running PowerShell window keeps the copy it started with.
# Reloading it lets this script use tools it has just installed.
function Update-SessionPath {
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
                [Environment]::GetEnvironmentVariable('Path', 'User') + ';' +
                (Join-Path $env:USERPROFILE '.dotnet\tools')
}

function Test-DotNet10Sdk {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { return $false }
    return [bool] (dotnet --list-sdks | Select-String '^10\.')
}

# LocalDB versions installed on this machine, oldest first (13.0 is SQL Server 2016, 17.0 is 2025)
function Get-LocalDbVersions {
    $key = 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions'
    if (-not (Test-Path $key)) { return @() }
    return @(Get-ChildItem $key | ForEach-Object { [version] $_.PSChildName } | Sort-Object)
}

# The version an existing LocalDB instance runs, or $null if it doesn't exist (yet)
function Get-LocalDbInstanceVersion([string] $name) {
    if (-not (Get-Command sqllocaldb -ErrorAction SilentlyContinue)) { return $null }
    $ErrorActionPreference = 'Continue'
    $info = (sqllocaldb info $name 2>&1) | Out-String
    $match = [regex]::Match($info, '\b\d+\.\d+\.\d+\.\d+\b')
    if ($match.Success) { return [version] $match.Value }
    return $null
}

function Read-YesNo([string] $question) {
    return (Read-Host "$question [Y/N]") -match '^\s*[Yy]'
}

# winget has no LocalDB package, so this downloads the SQL Server Express installer through winget
# (which verifies its hash), uses the installer's "LocalDB only" download mode to fetch SqlLocalDB.msi,
# and installs that silently.
function Install-LocalDb {
    $work = Join-Path $env:TEMP 'article-api-setup'
    New-Item -ItemType Directory -Force -Path $work | Out-Null

    Write-Host 'Downloading the SQL Server Express installer...'
    winget download --id Microsoft.SQLServer.2025.Express --exact --download-directory (Join-Path $work 'installer') --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) { throw "Downloading the SQL Server Express installer failed (winget exit code $LASTEXITCODE)." }
    $installer = Get-ChildItem (Join-Path $work 'installer') -Filter '*.exe' | Select-Object -First 1

    Write-Host 'Downloading LocalDB...'
    $download = Start-Process -FilePath $installer.FullName -Wait -PassThru -ArgumentList @(
        '/ACTION=Download', '/MEDIATYPE=LocalDB', "/MEDIAPATH=`"$(Join-Path $work 'media')`"", '/QUIET')
    if ($download.ExitCode -ne 0) { throw "Downloading LocalDB failed (exit code $($download.ExitCode))." }
    $msi = Get-ChildItem (Join-Path $work 'media') -Recurse -Filter 'SqlLocalDB.msi' | Select-Object -First 1

    Write-Host 'Installing LocalDB (Windows will ask for administrator permission)...'
    $install = Start-Process -FilePath 'msiexec.exe' -Verb RunAs -Wait -PassThru -ArgumentList @(
        '/i', "`"$($msi.FullName)`"", '/qn', 'IACCEPTSQLLOCALDBLICENSETERMS=YES')
    # 3010 means success, with a restart recommended
    if ($install.ExitCode -ne 0 -and $install.ExitCode -ne 3010) { throw "Installing LocalDB failed (msiexec exit code $($install.ExitCode))." }
    Update-SessionPath
    Write-Host 'Installed.'
}

function Test-DotNetEf {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { return $false }
    return [bool] (dotnet tool list --global | Select-String '^dotnet-ef\s')
}

Update-SessionPath

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Host "winget was not found. Install 'App Installer' from the Microsoft Store and run this script again," -ForegroundColor Red
    Write-Host "or install the requirements listed in README.md by hand." -ForegroundColor Red
    exit 1
}

# 1. The .NET 10 SDK
Write-Step 'Checking for the .NET 10 SDK'
if (Test-DotNet10Sdk) {
    Write-Host 'Already installed.'
}
else {
    Write-Host 'Installing the .NET 10 SDK...'
    winget install --id Microsoft.DotNet.SDK.10 --exact --silent --accept-package-agreements --accept-source-agreements
    Update-SessionPath
    if (-not (Test-DotNet10Sdk)) { throw 'The .NET 10 SDK installation did not succeed.' }
    Write-Host 'Installed.'
}

# 2. SQL Server Express LocalDB, 2017 or later: Report A uses STRING_AGG. The API itself also runs on 2016.
Write-Step 'Checking for SQL Server Express LocalDB'
$localDbVersions = @(Get-LocalDbVersions)
if ($localDbVersions.Count -eq 0) {
    Install-LocalDb
}
elseif (-not ($localDbVersions | Where-Object { $_.Major -ge 14 })) {
    Write-Host "Found LocalDB $($localDbVersions -join ', '). The API works with it, but Report A needs SQL Server 2017 (version 14.0) or later." -ForegroundColor Yellow
    if (Read-YesNo 'Install SQL Server 2025 LocalDB alongside it?') {
        Install-LocalDb
    }
    else {
        Write-Host 'Skipped.'
    }
}
else {
    Write-Host "Already installed (version $($localDbVersions -join ', '))."
}

# The app connects to the instance (localdb)\MSSQLLocalDB. An existing instance keeps the version it was
# created with, even after a newer LocalDB is installed, so it has to be recreated to use the newer one.
$newestLocalDb = @(Get-LocalDbVersions) | Select-Object -Last 1
$instanceVersion = Get-LocalDbInstanceVersion 'MSSQLLocalDB'
if ($newestLocalDb -and $newestLocalDb.Major -ge 14 -and $instanceVersion -and $instanceVersion.Major -lt 14) {
    Write-Host ''
    Write-Host "The instance MSSQLLocalDB still runs version $instanceVersion. To use version $newestLocalDb it has to be deleted and created again." -ForegroundColor Yellow
    Write-Host 'Its databases are detached, not deleted: their files stay on disk and can be attached again, for example in SSMS.' -ForegroundColor Yellow
    Write-Host 'Close anything using it first, such as the running API or SSMS.' -ForegroundColor Yellow
    if (Read-YesNo 'Recreate MSSQLLocalDB now?') {
        sqllocaldb stop MSSQLLocalDB | Out-Null
        sqllocaldb delete MSSQLLocalDB | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Deleting the MSSQLLocalDB instance failed. Close anything using it and run this script again.' }
        sqllocaldb create MSSQLLocalDB $newestLocalDb.ToString() -s | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Creating the MSSQLLocalDB instance on version $newestLocalDb failed." }
        Write-Host "MSSQLLocalDB now runs version $(Get-LocalDbInstanceVersion 'MSSQLLocalDB')."
    }
    else {
        Write-Host 'Skipped.'
    }
}

# 3. The EF Core command-line tool, pinned to the EF Core version the project uses
Write-Step 'Checking for the EF Core command-line tool (dotnet-ef)'
if (Test-DotNetEf) {
    Write-Host 'Already installed.'
}
else {
    Write-Host 'Installing dotnet-ef 10.0.12...'
    dotnet tool install --global dotnet-ef --version 10.0.12
    Update-SessionPath
    if (-not (Test-DotNetEf)) { throw 'Installing dotnet-ef did not succeed.' }
    Write-Host 'Installed.'
}

# The instance the app will use: the existing one, or, if none exists yet, the newest installed version
$effectiveVersion = Get-LocalDbInstanceVersion 'MSSQLLocalDB'
if (-not $effectiveVersion) { $effectiveVersion = @(Get-LocalDbVersions) | Select-Object -Last 1 }

if ($effectiveVersion -and $effectiveVersion.Major -ge 14) {
    Write-Step 'Everything is installed'
}
else {
    Write-Step 'Installed, with one limitation'
    Write-Host "MSSQLLocalDB runs version $effectiveVersion. The API works, but Report A needs SQL Server 2017 or later." -ForegroundColor Yellow
    Write-Host 'Run this script again and answer Y to upgrade.' -ForegroundColor Yellow
    Write-Host ''
}
Write-Host 'Open a new terminal (so it picks up the updated PATH), then start the API from the repository folder:'
Write-Host ''
Write-Host '    dotnet run --project ArticleApi'
Write-Host ''
Write-Host 'It listens on http://localhost:5229. See README.md for how to use it.'
