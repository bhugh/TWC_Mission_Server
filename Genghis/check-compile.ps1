<#
.SYNOPSIS
    Type-checks the Genghis mission the same way the CLoD server does: one assembly per mission.

.DESCRIPTION
    CLoD compiles every .cs file that shares the mission name into ONE assembly and loads it, so this script
    type-checks  Genghis.cs + Genghis-Class-*.cs  together against the real CloD assemblies from ...\parts\core\.

    Roslyn is invoked directly (dotnet exec ...\Roslyn\bincore\csc.dll) instead of via msbuild / dotnet build,
    because the repo's TWC_Mission_Server.csproj is SDK-style with no explicit <Compile> items: it globs EVERY
    .cs under the repo, including the duplicate mission copies in Campaign21\, Tobruk_Campaign\ and Testing\,
    which redefine Mission / Calcs / CoverCalcs and produce thousands of duplicate-type errors.

    Two files are deliberately skipped - both are pre-existing problems, unrelated to this script:
      * *-initsubmission*.cs               - separate CLoD missions; they redefine Mission/Calcs (CS0101, CS0111)
      * Genghis-Class-TacviewRecorder.cs  - 'TacviewCore' is internal in the prebuilt TacviewRecorder.dll (CS0122)

    By default warnings are suppressed (-warn:0) so the VS Code Problems panel stays clean and only real
    errors show up.  Use -ShowWarnings to list them all.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1 -ShowWarnings

.NOTES
    Exit code: 0 = compiled with no errors, 1 = compile errors, 2 = could not run (no dotnet / no DLLs).
#>
[CmdletBinding()]
param(
    [switch] $ShowWarnings,
    [string] $CloDPath,
    [string] $Out = (Join-Path $env:TEMP 'GenghisCompileCheck.dll')
)

function Say($m, $c = 'Gray') { Write-Host $m -ForegroundColor $c }

# ---------------------------------------------------------------- 1. Roslyn from the .NET SDK
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) {
    Say 'ERROR: "dotnet" not found on PATH.  Install the .NET SDK from https://dotnet.microsoft.com/download' 'Red'
    exit 2
}
$sdkCandidates = @()
$sdkCandidates += (Join-Path (Split-Path (Split-Path $dotnet -Parent) -Parent) 'sdk')
$sdkCandidates += (Join-Path $env:ProgramFiles 'dotnet\sdk')
$sdkCandidates += 'C:\Program Files\dotnet\sdk'
$sdkCandidates += (Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\sdk')

$csc = $null
foreach ($d in $sdkCandidates) {
    if (-not (Test-Path $d)) { continue }
    $hit = Get-ChildItem $d -Directory -ErrorAction SilentlyContinue |
           Where-Object { Test-Path (Join-Path $_.FullName 'Roslyn\bincore\csc.dll') } |
           Sort-Object { try { [version]$_.Name } catch { [version]'0.0' } } -Descending |
           Select-Object -First 1
    if ($hit) { $csc = Join-Path $hit.FullName 'Roslyn\bincore\csc.dll'; break }
}
if (-not $csc) {
    Say 'ERROR: could not find Roslyn (Roslyn\bincore\csc.dll) in any .NET SDK.  Install the .NET SDK.' 'Red'
    exit 2
}

# ------------------------------------------------------- 2. .NET Framework reference assemblies
$refDir = $null
foreach ($v in @('v4.8', 'v4.7.2', 'v4.7.1', 'v4.7', 'v4.6.2', 'v4.6.1', 'v4.6', 'v4.5.2', 'v4.5')) {
    $d = Join-Path ${env:ProgramFiles(x86)} "Reference Assemblies\Microsoft\Framework\.NETFramework\$v"
    if (Test-Path (Join-Path $d 'mscorlib.dll')) { $refDir = $d; break }
}
if (-not $refDir) {
    Say 'ERROR: no .NET Framework reference assemblies (net4.8 etc).  Install the .NET Framework 4.8 Developer Pack.' 'Red'
    exit 2
}

# --------------------------------------------------------------------- 3. the CloD assemblies
if (-not $CloDPath) {
    $steam = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\IL-2 Sturmovik Cliffs of Dover Blitz\parts\core')
        'C:\Program Files (x86)\Steam\steamapps\common\IL-2 Sturmovik Cliffs of Dover Blitz\parts\core'
    )
    foreach ($drv in (Get-PSDrive -PSProvider FileSystem -ErrorAction SilentlyContinue)) {
        $steam += (Join-Path $drv.Root 'SteamLibrary\steamapps\common\IL-2 Sturmovik Cliffs of Dover Blitz\parts\core')
    }
    $CloDPath = $steam | Where-Object { Test-Path (Join-Path $_ 'gamePlay.dll') } | Select-Object -First 1
}
if (-not $CloDPath) {
    Say 'ERROR: could not find the CloD install.  Pass -CloDPath "<...>\IL-2 Sturmovik Cliffs of Dover Blitz\parts\core"' 'Red'
    exit 2
}

# ------------------------------------------------------------------- 4. the mission's own files
$skip = @(
    'Genghis-initsubmission-BurnBabyBurn.cs'        # separate CLoD mission (redefines Mission/Calcs)
    'CheckPlayerSignedUp-Genghis-initsubmission.cs' # idem
    'Genghis-Class-TacviewRecorder.cs'              # TacviewCore is internal in TacviewRecorder.dll
)
$sources = Get-ChildItem -Path $PSScriptRoot -Filter '*.cs' |
           Where-Object { $skip -notcontains $_.Name } |
           ForEach-Object { $_.FullName }
if (-not $sources) { Say "ERROR: no .cs files found in $PSScriptRoot" 'Red'; exit 2 }

# ------------------------------------------------------------------------ 5. build the arg list
$cscArgs = @()
foreach ($n in @('mscorlib', 'System', 'System.Core', 'System.Xml', 'System.Data', 'System.Drawing',
                 'System.Runtime.Serialization', 'System.Xaml', 'WindowsBase', 'PresentationCore',
                 'PresentationFramework', 'System.Management', 'Microsoft.CSharp')) {
    $p = Join-Path $refDir "$n.dll"
    if (Test-Path $p) { $cscArgs += "/r:$p" }
}
foreach ($n in @('maddox', 'part', 'GameWorld', 'gamePlay', 'gamePages', 'CloDMissionCommunicator',
                 'Strategy', 'core', 'HostView', 'Campaign', 'antlr.runtime', 'WSteam')) {
    $p = Join-Path $CloDPath "$n.dll"
    if (Test-Path $p) { $cscArgs += "/r:$p" }
}
$tv = Join-Path $PSScriptRoot 'TacviewRecorder.dll'
if (Test-Path $tv) { $cscArgs += "/r:$tv" }

$cscArgs += '/nologo'
$cscArgs += '/target:library'
$cscArgs += '/nostdlib+'      # csc is run bare, so there are no implicit framework references
$cscArgs += '/unsafe'
if (-not $ShowWarnings) { $cscArgs += '/warn:0' }
if (Test-Path $Out) { Remove-Item $Out -Force }
$cscArgs += "/out:$Out"
$cscArgs += $sources

# --------------------------------------------------------------------- 6. compile and report
Say ''
Say '=================================================================='
Say ' Genghis mission compile check   (CLoD: one assembly per mission)'
Say '=================================================================='
Say " files      : $($sources.Count)"
Say " csc        : $csc"
Say " framework  : $refDir"
Say " CloD       : $CloDPath"
Say " output     : $Out"
if (-not $ShowWarnings) { Say ' warnings   : suppressed (re-run with -ShowWarnings to list them)' }
Say '------------------------------------------------------------------'

$raw = & $dotnet exec $csc @cscArgs 2>&1
$errors   = @($raw | Where-Object { "$_" -match ':\s+error\s+CS' })
$warnings = @($raw | Where-Object { "$_" -match ':\s+warning\s+CS' })

if ($errors.Count) {
    Say ''
    Say " ERRORS ($($errors.Count)):" 'Red'
    $errors | ForEach-Object { Write-Host "   $_" -ForegroundColor 'Red' }
}
if ($errors.Count) { Say " ERRORS   : $($errors.Count)" 'Red' } else { Say " ERRORS   : 0" 'Green' }
if ($ShowWarnings) { Say " WARNINGS : $($warnings.Count)" 'Yellow' }

if ($ShowWarnings -and $warnings.Count) {
    Say ''
    Say ' warnings by file:' 'Yellow'
    $warnings | ForEach-Object {
        if ("$_" -match '^(.*?)\(\d+,\d+\):') { $matches[1] }
    } | Group-Object | Sort-Object Count -Descending | ForEach-Object { Say ("   {0,-45} {1}" -f $_.Name, $_.Count) 'DarkGray' }
    Say ''
    Say ' all warnings:' 'Yellow'
    $warnings | Select-Object -First 200 | ForEach-Object { Write-Host "   $_" -ForegroundColor 'DarkYellow' }
    if ($warnings.Count -gt 200) { Say "   ... and $($warnings.Count - 200) more" 'DarkGray' }
}

Say '------------------------------------------------------------------'
if ($errors.Count -eq 0) {
    Say ' RESULT   : OK - the mission compiles with no errors' 'Green'
    exit 0
}
Say ' RESULT   : FAILED - fix the errors above' 'Red'
exit 1

