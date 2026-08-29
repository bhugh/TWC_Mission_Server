@echo off
setlocal enabledelayedexpansion

@echo off
:: Force the script to run from its own directory
cd /d "%~dp0"

:: Loop over all .zip.acmi files directly in this folder
for %%F in ("*.zip.acmi") do (
    call :ProcessFile "%%~fF"
)
goto :eof


:ProcessFile
:: This is your function. %1 grabs the first variable passed into it.
set "FILE_PATH=%~1"
set "FILE_NAME=%~nx1"


::set "file=Tacview-20260826-194340-COD-Genghis-Class-TacViewImplMission.zip.acmi"

:: Extract the archive using tar
tar -xf "%FILE_NAME%"

:: Remove BOTH extensions (.zip.acmi)
:: First remove .acmi → gives "20260826-130351.zip"
set "BASE1=%FILE_NAME:~0,-5%"

:: Now remove .zip → gives "20260826-130351"
for %%A in ("%BASE1%") do set "BASE_NAME=%%~nA"

:: Build the unzipped filename
set "UNZIPPED_FILE=%BASE_NAME%.txt.acmi"

echo Base name: %BASE_NAME%
echo Unzipped file: %UNZIPPED_FILE%

:: Strip empty lines or lines starting with commas
findstr /V /R "^," "%UNZIPPED_FILE%" > "%UNZIPPED_FILE%-stripped.txt.acmi"

:: Ensure the output directory exists
if not exist "processed" mkdir "processed"

set "SHORTNAME=%BASE_NAME:~8,15%"


for /f "delims=" %%A in ('powershell -Command "[datetime]::ParseExact('%SHORTNAME%', 'yyyyMMdd-HHmmss', $null) | ForEach-Object { [TimeZoneInfo]::ConvertTimeToUtc($_, [TimeZoneInfo]::FindSystemTimeZoneById('Eastern Standard Time')).ToString('yyyy-MM-dd_HH.mm.ss') }"') do set "TEMP_TIME=%%A"

set "UTC_TIME=%TEMP_TIME:_= at %"


echo %UTC_TIME%

:: Run Tacview filter
tacview-filter "temp-stripped.txt.acmi" ^
    -o "processed\%UTC_TIME% UTC.zip.acmi" ^
    --fog-of-war "Color=Green,Group=Player 4"
	
:: Rename to just the date	

::for /f "tokens=1,2,3* delims=-" %A in ('dir /b Tacview-*-COD-Genghis-Class-TacViewImplMission.zip.acmi') do ren "%A-%B-%C-%D" "%B-%C.zip.acmi"
::powershell -Command "Get-ChildItem -Path 'processed' -Filter 'Tacview-*-COD-Genghis-Class-TacViewImplMission.zip.acmi' | ForEach-Object { $parts = $_.Name -split '-'; Rename-Item $_.FullName -NewName """$($parts[1])-$($parts[2]).zip.acmi""" }"
::powershell -Command "Get-ChildItem -Path 'processed' -Filter 'Tacview-*-COD-Genghis-Class-TacViewImplMission-filtered.zip.acmi' | ForEach-Object { $p = $_.Name -split '-'; Rename-Item $_.FullName -NewName (\"$($p[1])-$($p[2]).zip.acmi\") }"

	

:: Clean up
::del "%UNZIPPED_FILE%-stripped.txt.acmi"
del %UNZIPPED_FILE%
move %FILE_NAME% originals/%FILE_NAME%

endlocal

:eof