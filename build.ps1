# Build offline du plugin NINA.ShellyPower contre les assemblies installées de NINA 3.2.
# Utilise Roslyn (csc) du SDK .NET directement, sans MSBuild/NuGet (aucun ref-pack net8 requis),
# via un fichier de réponse (evite la limite de longueur de la ligne de commande Windows).
param(
    [string]$NinaDir = 'C:\Program Files\N.I.N.A. - Nighttime Imaging ''N'' Astronomy',
    [string]$Out = 'bin\Release'
)

$ErrorActionPreference = 'Stop'
$ws = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $ws $Out
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Rendre le build déterministe hors-ligne.
$env:DOTNET_CLI_HOME   = "$ws\.dotnet-home"
$env:NUGET_PACKAGES    = "$ws\.nuget"
$env:APPDATA           = "$ws\AppData"
$env:LOCALAPPDATA      = "$ws\LocalAppData"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$csc  = 'C:\Program Files\dotnet\sdk\10.0.400\Roslyn\bincore\csc.dll'
$rsp  = Join-Path $ws 'build.rsp'
$outDll = Join-Path $outDir 'NINA.ShellyPower.dll'

# Contenu du fichier de réponse.
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('-nologo')
$lines.Add('-nostdlib')
$lines.Add('-target:library')
$lines.Add("-out:`"$outDll`"")
$lines.Add('-langversion:latest')
$lines.Add('-nullable:disable')
$lines.Add('-optimize+')

# Références : DLL managées du dossier NINA (runtime net8 + NINA + dépendances).
# Les DLL natives (coreclr, hostfxr, vcruntime, etc.) sont exclues car non managées.
$refLines = New-Object System.Collections.Generic.List[string]
Get-ChildItem -Path $NinaDir -Filter '*.dll' | ForEach-Object {
    try {
        [void][System.Reflection.AssemblyName]::GetAssemblyName($_.FullName)
    } catch {
        return # DLL native : on ignore.
    }
    $refLines.Add("-reference:`"$($_.FullName)`"")
}
$refLines | ForEach-Object { $lines.Add($_) }

Get-ChildItem -Path $ws -Filter '*.cs' | ForEach-Object {
    $lines.Add("`"$($_.FullName)`"")
}

Set-Content -Path $rsp -Value ($lines -join "`n")

& 'C:\Program Files\dotnet\dotnet.exe' exec $csc "@$rsp"
if ($LASTEXITCODE -ne 0) {
    Write-Host 'BUILD FAILED' -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "OK -> $outDll" -ForegroundColor Green
