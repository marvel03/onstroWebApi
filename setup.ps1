<#
.SYNOPSIS
    Installs what this project needs on Windows: the .NET 10 SDK, SQL Server Express LocalDB,
    and the EF Core command-line tool (dotnet-ef). Anything already installed is skipped.

.DESCRIPTION
    Uses winget, which comes with Windows 10 and 11 as part of "App Installer".
    Installing LocalDB needs administrator rights, so Windows asks for permission at that step.

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

function Test-LocalDb {
    return Test-Path 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions\*'
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

# 2. SQL Server Express LocalDB
#    winget has no LocalDB package, so this downloads the SQL Server Express installer through winget
#    (which verifies its hash), uses the installer's "LocalDB only" download mode to fetch SqlLocalDB.msi,
#    and installs that silently.
Write-Step 'Checking for SQL Server Express LocalDB'
if (Test-LocalDb) {
    Write-Host 'Already installed.'
}
else {
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

Write-Step 'Everything is installed'
Write-Host 'Open a new terminal (so it picks up the updated PATH), then start the API from the repository folder:'
Write-Host ''
Write-Host '    dotnet run --project ArticleApi'
Write-Host ''
Write-Host 'It listens on http://localhost:5229. See README.md for how to use it.'
