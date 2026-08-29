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
	
	
	#if (Test-Path $UnzippedFile) { Write-Host $UnzippedFile exists }
	#if (Test-Path $StrippedFile) { Write-Host $StrippedFile exists }
	
	#if (Test-Path $UnzippedFile) { Remove-Item $UnzippedFile -Force }
	#if (Test-Path $StrippedFile) { Remove-Item $StrippedFile -Force }
	
	#if (Test-Path $UnzippedFile) { Write-Host $UnzippedFile exists }
	#if (Test-Path $StrippedFile) { Write-Host $StrippedFile exists }
	
	# remove these in case they exist from previous runs
	Remove-Item $UnzippedFile -Force -ErrorAction SilentlyContinue
	Remove-Item $StrippedFile -Force -ErrorAction SilentlyContinue
	
	

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
	
	$FinalOutputFile = "(skipped)"
	$NoPlayer = ""
	
	#################################################
	# Check if the file contains "Group=Player" 
	#
	# (-Quiet makes it return a fast True/False the moment it finds the first match)
	# If no player ever entered, we don't save or process at all, just move to /originals
	if ((Select-String -Path $UnzippedFile -Pattern 'Group=Player' -Quiet)) {
    

		Write-Host "`n>>>> 2. Filtering strings and formatting player data..." -ForegroundColor Cyan

		###############################
		# Remove any lines starting with "," and also replace "Group=Player" with "Group=Player,Coalition=Allies"
		# .NET filtering
		#
		# High-speed streaming text modification using .NET
		# 1. Fast file-size calculation for progress tracking (instead of slow line counting)
		$FileLength = (Get-Item $UnzippedFile).Length
		$BytesProcessed = 0

		try {
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
			Write-Progress -Activity "Processing Large Tacview File" -Completed

		} catch {
			Write-Host "`n>>>> ERROR Processing Group=Player filter, this file won't be processed correctly: $FileName" -ForegroundColor Cyan
			# Safety cleanup in case of a crash midway through
			if ($null -ne $Reader) { $Reader.Close() }
			if ($null -ne $Writer) { $Writer.Close() }
	    		$ErrorCounter++
		}
		
		#################################################################################
		# The original filename is long and in US Eastern Time.  This converts to UTC/Zulu
		# and makes a shorter filename consisting of just the date.
		#
		#
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
		
		if (Test-Path $UnzippedFile) { Write-Host $UnzippedFile exists }
		if (Test-Path $StrippedFile) { Write-Host $StrippedFile exists }
			

		##############################################################
		# RUN TACVIEW-FILTER to filter to "FOG OF WAR"
		#
		Write-Host "`n>>>> 3. Fog-of-war processing $StrippedFile at time $UtcTime" -ForegroundColor Cyan
		$OutputFileTemp1 = Join-Path $ScriptDir "$UtcTime`Z_fog.txt.acmi"
		$OutputFileTemp2 = Join-Path $ScriptDir "$UtcTime`Z.txt.acmi"
		#$OutputFileTemp3 = Join-Path $ScriptDir "$UtcTime`Z.txt.acmi"
		$FinalOutputFile = Join-Path $ProcessedDir "$UtcTime`Z.zip.acmi"
		
		# remove them in case they still exist from previous run
		Remove-Item $OutputFileTemp1 -Force -ErrorAction SilentlyContinue
		Remove-Item $OutputFileTemp2 -Force -ErrorAction SilentlyContinue

		# Run Tacview Filter executable
		# --text or --compressed are the options for txt vs zip
		& ./tacview-filter.exe $StrippedFile -o $OutputFileTemp1 --fog-of-war "Coalition=Allies 4" --text
		if ($LASTEXITCODE -ne 0) { $ErrorCounter++ }
		
		if (Test-Path $UnzippedFile) { Write-Host $UnzippedFile exists }
		if (Test-Path $StrippedFile) { Write-Host $StrippedFile exists }
		if (Test-Path $OutputFileTemp1) { Write-Host $OutputFileTemp1 exists }
		
		##########################################################
		# STRIP OUT MANY SPURIOUS 'DESTROYED' MESSAGES - .NET filtering
		#
		Write-Host "`n>>>> 4. Removing many spurious 'destroyed' messages" -ForegroundColor Cyan
		# Setup file size parameters for tracking progress
		$FileLength = (Get-Item $OutputFileTemp1).Length

		
		# ============================================================================
		# PHASE 1: PRE-SCAN THE FILE FOR ACTIVE OBJECT IDs
		# ============================================================================
		# A HashSet allows instant lookups among hundreds of thousands of entries
		$ActiveObjectIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

		try {
			$Reader = [System.IO.StreamReader]::new($OutputFileTemp1)
			$LineCounter = 0
			$BytesProcessed = 0

			while (($Line = $Reader.ReadLine()) -ne $null) {
				$LineCounter++
				$BytesProcessed += [System.Text.Encoding]::UTF8.GetByteCount($Line) + 2

				# Every 50,000 lines, update progress bar
				if ($LineCounter % 50000 -eq 0) {
					$Percent = [Math]::Round(($BytesProcessed / $FileLength) * 50) # Scale to 50% max for Phase 1
					Write-Progress -Activity "Phase 1/2: Cataloging Object IDs" -Status "Scanning line $LineCounter ($Percent%)" -PercentComplete $Percent
				}

				# Skip headers or empty lines
				if ([string]::IsNullOrWhiteSpace($Line) -or $Line.StartsWith('0,') -or $Line.StartsWith(',') -or $Line.StartsWith('-')) { continue }

				# Extract Hex ID (everything before the first comma)
				$CommaIndex = $Line.IndexOf(',')
				if ($CommaIndex -gt 0) {
					$ObjId = $Line.Substring(0, $CommaIndex).Trim()
					
					### If the actor is NOT fog-of-war stripped, remember this ID
					#if ($Line -notmatch 'Group=Player') {
						[void]$ActiveObjectIds.Add($ObjId)
					#}
				}
			}
			$Reader.Close()
		} catch {
			if ($null -ne $Reader) { $Reader.Close() }
			Write-Error "Failed during Phase 1: $_"
		}

		# ============================================================================
		# PHASE 2: WRITE CLEANED FILE & FILTER MEANINGLESS MESSAGES
		# ============================================================================
		try {
			$Reader = [System.IO.StreamReader]::new($OutputFileTemp1)
			$Writer = [System.IO.StreamWriter]::new($OutputFileTemp2)
			$LineCounter = 0
			$BytesProcessed = 0

			while (($Line = $Reader.ReadLine()) -ne $null) {
				$LineCounter++
				$BytesProcessed += [System.Text.Encoding]::UTF8.GetByteCount($Line) + 2

				if ($LineCounter % 50000 -eq 0) {
					$Percent = 50 + [Math]::Round(($BytesProcessed / $FileLength) * 50) # Scale from 50% to 100%
					Write-Progress -Activity "Phase 2/2: Writing Filtered File" -Status "Processing line $LineCounter ($Percent%)" -PercentComplete $Percent
				}


				# 2. Check for "destroyed" messages
				if ($Line -match '^0\,Event\=Message\|(?<ID>[A-Fa-f0-9]+)\|destroyed.*') {
					$TargetId = $Matches['ID']
					
					# If the ID inside the message was stripped out or never existed, DROP THE MESSAGE
					if (-not $ActiveObjectIds.Contains($TargetId)) {
						continue 
					}
				}
				
				$Writer.WriteLine($Line)

			}

			$Reader.Close()
			$Writer.Close()
			Write-Progress -Activity "Phase 2/2: Writing Filtered File" -Completed

		} catch {
			if ($null -ne $Reader) { $Reader.Close() }
			if ($null -ne $Writer) { $Writer.Close() }
			$ErrorCounter++
		}
		
		# Compress the temp file into a temporary standard zip archive
		# (-Force ensures it overwrites any leftover stale zip files)
		Compress-Archive -Path $OutputFileTemp2 -DestinationPath "temp_archive.zip" -Force

		# 3. Rename/Move the zip file to your custom dual-extension target path
		Move-Item -Path "temp_archive.zip" -Destination $FinalOutputFile -Force		
				
		
	} else {
		Write-Host "`n>>>> 3. NO PLAYER ENTERED this mission, so no processing done/discarded." -ForegroundColor Green
		$NoPlayer = "_noplayer"
	}
	
	####################################################################
	#CLEANUP AND MOVE ORIGINAL FILES TO DIR /originals
	#
    # Step 4: Cleanup temp files if no errors occurred
	Write-Host "`n>>>> 4. Clean up temp files, originals to /originals directory" -ForegroundColor Green
		
	Remove-Item $UnzippedFile -Force -ErrorAction SilentlyContinue
	Remove-Item $StrippedFile -Force -ErrorAction SilentlyContinue	
	Remove-Item $OutputFileTemp1 -Force -ErrorAction SilentlyContinue
	Remove-Item $OutputFileTemp2 -Force -ErrorAction SilentlyContinue
	
    if ($ErrorCounter -eq 0) {
        Write-Host "`n>>>> 5. Move original file to /originals" -ForegroundColor Green
        
        if (-not (Test-Path "originals")) { New-Item -ItemType Directory -Path "originals" | Out-Null }
        Move-Item $File.FullName -Destination "originals/$FileName$NoPlayer" -Force
    } else {
        Write-Host "****** An ERROR occurred processing $FileName! Error count tally is $ErrorCounter" -ForegroundColor Red
		Write-Host "****** Leaving file in dir for future processing" -ForegroundColor DarkYellow
    }
	
	Write-Host "`n>>>> Finished with $FileName => $FinalOutputFile" -ForegroundColor Red
}