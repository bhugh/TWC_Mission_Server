# Force the script to run from its own directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

# Scan for target ACMI files
$AcmiFiles = Get-ChildItem -Path $ScriptDir -Filter "Tacview-*-COD-Genghis-Class-TacViewImplMission.zip.acmi" -File

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
	
	#Zipped interior .txt.acmi file always has same basename as .zip.acmi
	
	$BaseName = $File.BaseName -replace '\.zip$', ''
	$UnzippedFile = "$ScriptDir\$BaseName.txt.acmi"
	$StrippedFile = "$ScriptDir\$BaseName-stripped.txt.acmi"
	Write-Host "Filenames $BaseName : $UnzippedFile : $StrippedFile : $ScriptDir" -ForegroundColor Green
	
	
	if (Test-Path $UnzippedFile) { Write-Host $UnzippedFile exists }
	if (Test-Path $StrippedFile) { Write-Host $StrippedFile exists }
	
	if (Test-Path $UnzippedFile) { Remove-Item $UnzippedFile -Force }
	if (Test-Path $StrippedFile) { Remove-Item $StrippedFile -Force }
	
	if (Test-Path $UnzippedFile) { Write-Host $UnzippedFile exists }
	if (Test-Path $StrippedFile) { Write-Host $StrippedFile exists }
	
	

    # Step 1: Extract the archive using tar
    tar -xf $FileName
    if ($LASTEXITCODE -ne 0) { 
		$ErrorCounter++ 
		Write-Host "Error unzipping $Filename, skipping" -ForegroundColor Red
		continue
	}

	if (Test-Path $UnzippedFile) { Write-Host $UnzippedFile exists }
	if (Test-Path $StrippedFile) { Write-Host $StrippedFile exists }
	
	
    # Generate unzipped file name (e.g., Tacview-20260826-194340... -> 20260826-194340...)
    # Drops the '.zip.acmi' extension and targets the extracted internal file
    
    
    
		
	

    if (-not (Test-Path $UnzippedFile)) {
        Write-Host "****** Error: Extracted file $UnzippedFile not found!" -ForegroundColor Red
        continue
    }
	
	$OutputFile = "(skipped)"
	$NoPlayer = ""
	
	# Check if the file contains "Group=Player" 
	# (-Quiet makes it return a fast True/False the moment it finds the first match)
	# If no player ever entered, we don't save or process at all, just move to /originals
	if ((Select-String -Path $UnzippedFile -Pattern 'Group=Player' -Quiet)) {
    

		Write-Host "`n>>>> 2. Filtering strings and formatting player data..." -ForegroundColor Cyan

		# Step 2: High-speed streaming text modification using .NET
		# 1. Fast file-size calculation for progress tracking (instead of slow line counting)
		$FileLength = (Get-Item $UnzippedFile).Length
		$BytesProcessed = 0

		#try {
			# 2. Open ultra-fast .NET streams for reading and writing
			$Reader = [System.IO.StreamReader]::new($UnzippedFile)
			$Writer = [System.IO.StreamWriter]::new($StrippedFile)
			
			$LineCounter = 0

			# 3. Read the file line-by-line at the hardware level
			while (($Line = $Reader.ReadLine()) -ne $null) {
				$LineCounter++
				
				# Track raw bytes processed to calculate accurate percentage instantly
				$BytesProcessed += [System.Text.Encoding]::UTF8.GetByteCount($Line) + 2 # +2 for newline characters

				# 4. Update the visual progress bar every 25,000 lines (tuned for high speed)
				if ($LineCounter % 25000 -eq 0) {
					$Percent = [Math]::Round(($BytesProcessed / $FileLength) * 100)
					if ($Percent -gt 100) { $Percent = 100 } # Clamp cap
					
					Write-Progress -Activity "Processing Large Tacview File" -Status "Processed $LineCounter lines ($Percent%)" -PercentComplete $Percent
				}

				# 5. Apply your filters and write instantly to disk
				if ($Line -notmatch '^\,' -and $Line -notmatch '^0\,Event\=Message.*destroyed\.') {
					$CleanLine = $Line.Replace('Group=Player', 'Group=Player,Coalition=Allies')
					$Writer.WriteLine($CleanLine)
				}
			}

			# 6. Always close the files to save changes and free memory
			$Reader.Close()
			$Writer.Close()
			
			# Finalize progress bar display
			Write-Progress -Activity "Stripping 5M+ Line Tacview File" -Completed

		#} catch {
			# Safety cleanup in case of a crash midway through
		#	if ($null -ne $Reader) { $Reader.Close() }
		#	if ($null -ne $Writer) { $Writer.Close() }
	    #		$ErrorCounter++
		#}

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
		$NoPlayer = "_noplayer"
	}
	
    # Step 4: Cleanup temp files if no errors occurred
	Write-Host "`n>>>> 4. Clean up temp files" -ForegroundColor Green
	if (Test-Path $UnzippedFile) { Remove-Item $UnzippedFile -Force }
	if (Test-Path $StrippedFile) { Remove-Item $StrippedFile -Force }
    if ($ErrorCounter -eq 0) {
        Write-Host "`n>>>> 5. Move original file to /originals" -ForegroundColor Green
        
        if (-not (Test-Path "originals")) { New-Item -ItemType Directory -Path "originals" | Out-Null }
        Move-Item $File.FullName -Destination "originals/$FileName$NoPlayer" -Force
    } else {
        Write-Host "****** An error occurred processing $FileName! Error count tally is $ErrorCounter" -ForegroundColor Red
    }
	
	Write-Host "`n>>>> Finished with $FileName => $OutputFile" -ForegroundColor Red
}