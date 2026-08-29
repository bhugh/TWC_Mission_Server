# Force the script to run from its own directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

# Scan for target ACMI files
$AcmiFiles = Get-ChildItem -Path $ScriptDir -Filter "*.zip.acmi" -File

if ($AcmiFiles.Count -eq 0) {
    Write-Host "`n>>>> No files found to process, exiting..." -ForegroundColor Yellow
    Exit
}

# Ensure output directory exists
$ProcessedDir = Join-Path $ScriptDir "processed"
if (-not (Test-Path $ProcessedDir)) { New-Item -ItemType Directory -Path $ProcessedDir | Out-Null }

foreach ($File in $AcmiFiles) {
    $ErrorCounter = 0
    $FileName = $File.Name
    Write-Host "`n>>>> 1. Unzip $FileName" -ForegroundColor Cyan

    # Step 1: Extract the archive using tar
    tar -xf $FileName
    if ($LASTEXITCODE -ne 0) { $ErrorCounter++ }

    # Generate unzipped file name (e.g., Tacview-20260826-194340... -> 20260826-194340...)
    # Drops the '.zip.acmi' extension and targets the extracted internal file
    $BaseName = $File.BaseName -replace '\.zip$', ''
    $UnzippedFile = "$BaseName.txt.acmi"
    $StrippedFile = "$BaseName-stripped.txt.acmi"

    if (-not (Test-Path $UnzippedFile)) {
        Write-Host "****** Error: Extracted file $UnzippedFile not found!" -ForegroundColor Red
        continue
    }

    Write-Host "`n>>>> 2. Filtering strings and formatting player data..." -ForegroundColor Cyan

    # Step 2: High-speed streaming text modification using .NET
    try {
        [System.IO.File]::WriteAllLines(
            $StrippedFile, 
            ([System.IO.File]::ReadAllLines($UnzippedFile) | 
                Where-Object { $_ -notmatch '^\,' } | 
                Where-Object { $_ -notmatch '^0\,Event\=Message.*destroyed\.' } | 
                ForEach-Object { $_.Replace('Group=Player', 'Group=Player,Coalition=Allies') })
        )
    } catch {
        $ErrorCounter++
    }

    # Step 3: Handle Timezone conversions directly without sub-shells
    # Captures the timestamp fragment (e.g., "20260826-194340") from the base name
    if ($BaseName -match '(\d{8}-\d{6})') {
        $ShortName = $Matches[1]
    } else {
        $ShortName = "20000101-000000" # Fallback if regex match fails
    }

    try {
        $LocalTime = [datetime]::ParseExact($ShortName, 'yyyyMMdd-HHmmss', $null)
        $EstZone = [TimeZoneInfo]::FindSystemTimeZoneById('Eastern Standard Time')
        $UtcTime = [TimeZoneInfo]::ConvertTimeToUtc($LocalTime, $EstZone).ToString('yyyy-MM-dd@HH.mm.ss')
    } catch {
        $UtcTime = "UnknownTime"
        $ErrorCounter++
    }

    Write-Host "`n>>>> 3. Fog-of-war processing $StrippedFile at time $UtcTime" -ForegroundColor Cyan
    $OutputFile = Join-Path $ProcessedDir "$UtcTime`Z.zip.acmi"

    # Run Tacview Filter executable
    & tacview-filter $StrippedFile -o $OutputFile --fog-of-war "Coalition=Allies 4" --show-engagements
    if ($LASTEXITCODE -ne 0) { $ErrorCounter++ }

    # Step 4: Cleanup temp files if no errors occurred
    if ($ErrorCounter -eq 0) {
        Write-Host "`n>>>> 4. Clean up temp files" -ForegroundColor Green
        if (Test-Path $UnzippedFile) { Remove-Item $UnzippedFile -Force }
        if (Test-Path $StrippedFile) { Remove-Item $StrippedFile -Force }
        
        # Optional: Uncomment if you want to isolate originals like your old script did
        if (-not (Test-Path "originals")) { New-Item -ItemType Directory -Path "originals" | Out-Null }
        Move-Item $File.FullName -Destination "originals/$FileName" -Force
    } else {
        Write-Host "****** An error occurred processing $FileName! Error count tally is $ErrorCounter" -ForegroundColor Red
    }
}