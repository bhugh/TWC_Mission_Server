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
	
	$OutputFile = "(skipped)";
	
	# Check if the file contains "Group=Player" 
	# (-Quiet makes it return a fast True/False the moment it finds the first match)
	# If no player ever entered, we don't save or process at all, just move to /originals
	if ((Select-String -Path $UnzippedFile -Pattern 'Group=Player' -Quiet)) {
    

		Write-Host "`n>>>> 2. Filtering strings and formatting player data..." -ForegroundColor Cyan

		# Step 2: High-speed streaming text modification using .NET
		# 1. Get the total line count to calculate percentages (only for progress tracking)
		$TotalLines = (Get-Content $UnzippedFile | Measure-Object).Count
		$LineCounter = 0

		try {
			# 2. Stream the file line-by-line using Get-Content
			Get-Content $UnzippedFile | ForEach-Object {
				$LineCounter++
				
				# 3. Update the progress bar every 5,000 lines to preserve CPU performance
				if ($LineCounter % 5000 -eq 0 -or $LineCounter -eq $TotalLines) {
					$Percent = [Math]::Round(($LineCounter / $TotalLines) * 100)
					Write-Progress -Activity "Stripping Tacview File" -Status "Processing line $LineCounter of $TotalLines" -PercentComplete $Percent
				}
				
				# 4. Output the current line if it passes your filters
				if ($_ -notmatch '^\,' -and $_ -notmatch '^0\,Event\=Message.*destroyed\.') {
					$_.Replace('Group=Player', 'Group=Player,Coalition=Allies')
				}
			} | Out-File -FilePath $StrippedFile -Encoding utf8 # 5. Stream directly into the destination file
			
			# Clear the progress bar when done
			Write-Progress -Activity "Stripping Tacview File" -Completed
			
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
		$OutputFileTemp = Join-Path $ScriptDir "$UtcTime`Z.zip.acmi"
		$OutputFile = Join-Path $ProcessedDir "$UtcTime`Z.zip.acmi"

		# Run Tacview Filter executable
		& ./tacview-filter.exe $StrippedFile -o $OutputFileTemp --fog-of-war "Coalition=Allies 4"
		if ($LASTEXITCODE -ne 0) { $ErrorCounter++ }
		
		#Save to a temp file and only move when complete (stops FTP from trying upload a partial file)
		Move-Item $OutputFileTemp -Destination $OutputFile -Force
		
	} else {
		Write-Host "`n>>>> 3. NO PLAYER ENTERED this mission, so no processing done/discarded." -ForegroundColor Green
	}
	
    # Step 4: Cleanup temp files if no errors occurred
	Write-Host "`n>>>> 4. Clean up temp files" -ForegroundColor Green
	if (Test-Path $UnzippedFile) { Remove-Item $UnzippedFile -Force }
	if (Test-Path $StrippedFile) { Remove-Item $StrippedFile -Force }
    if ($ErrorCounter -eq 0) {
        Write-Host "`n>>>> 5. Move original file to /originals" -ForegroundColor Green
        
        if (-not (Test-Path "originals")) { New-Item -ItemType Directory -Path "originals" | Out-Null }
        Move-Item $File.FullName -Destination "originals/$FileName" -Force
    } else {
        Write-Host "****** An error occurred processing $FileName! Error count tally is $ErrorCounter" -ForegroundColor Red
    }
	
	Write-Host "`n>>>> Finished with $FileName => $OutputFile" -ForegroundColor Red
}