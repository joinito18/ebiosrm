# Construit l'application de bureau : frontend embarque + backend .NET publie
# en executable autonome (aucune installation de .NET / Node requise chez
# l'utilisateur final).
#
#   .\build\build-desktop.ps1 [-Rid win-x64]
#
# Rid : win-x64 (defaut), linux-x64, osx-x64, osx-arm64.
# Resultat : build\output\<rid>\
param(
    [string]$Rid = "win-x64"
)
$ErrorActionPreference = "Stop"

$Racine = Split-Path -Parent $PSScriptRoot
$Sortie = Join-Path $Racine "build\output\$Rid"
$Api    = Join-Path $Racine "src\EbiosRM.Api"

Write-Host "==> Frontend (VITE_API_BASE=/api/v1)"
Push-Location (Join-Path $Racine "frontend")
npm ci
$env:VITE_API_BASE = "/api/v1"
npm run build
Pop-Location

Write-Host "==> Copie du frontend dans wwwroot"
$WwwRoot = Join-Path $Api "wwwroot"
if (Test-Path $WwwRoot) { Remove-Item -Recurse -Force $WwwRoot }
New-Item -ItemType Directory -Path $WwwRoot | Out-Null
Copy-Item -Recurse -Force (Join-Path $Racine "frontend\dist\*") $WwwRoot

Write-Host "==> Publication backend ($Rid, autonome, fichier unique)"
if (Test-Path $Sortie) { Remove-Item -Recurse -Force $Sortie }
dotnet publish (Join-Path $Api "EbiosRM.Api.csproj") `
    -c Release -r $Rid --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $Sortie
if ($LASTEXITCODE -ne 0) { throw "dotnet publish a echoue" }

Remove-Item -Force (Join-Path $Sortie "appsettings.Development.json"), (Join-Path $Sortie "web.config") -ErrorAction SilentlyContinue

# Renomme l'executable hote en "EbiosRM" (sans incidence pour une publication
# fichier unique : le nom de l'exe est independant du nom de l'assembly).
if ($Rid -like "win-*") {
    Move-Item -Force (Join-Path $Sortie "EbiosRM.Api.exe") (Join-Path $Sortie "EbiosRM.exe")
} else {
    Move-Item -Force (Join-Path $Sortie "EbiosRM.Api") (Join-Path $Sortie "EbiosRM")
}

Write-Host ""
Write-Host "==> OK : $Sortie"
Get-ChildItem $Sortie | Format-Table Name, Length
