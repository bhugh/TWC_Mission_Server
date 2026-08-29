@echo off
setlocal enabledelayedexpansion

@echo off
:: Force the script to run from its own directory
cd /d "%~dp0"

:: Loop over all .zip.acmi files directly in this folder
set "files_found=0"
for %%F in ("*.zip.acmi") do (
    call :ProcessFile "%%~fF"
	set "files_found=1"
)
if %files_found% equ 0 (
	echo.
	echo ^>^>^>^>No files found to process, exiting...
)
goto :eof



:ProcessFile
:: This is your function. %1 grabs the first variable passed into it.
set "FILE_PATH=%~1"
set "FILE_NAME=%~nx1"
set "ERROR=0"


::set "file=Tacview-20260826-194340-COD-Genghis-Class-TacViewImplMission.zip.acmi"
echo.
echo ^>^>^>^> 1. Unzip %FILE_NAME%
:: Extract the archive using tar
tar -xf "%FILE_NAME%"
set /a "ERROR=ERRORLEVEL + ERROR"

:: Remove BOTH extensions (.zip.acmi)
:: First remove .acmi → gives "20260826-130351.zip"
set "BASE1=%FILE_NAME:~0,-5%"

:: Now remove .zip → gives "20260826-130351"
for %%A in ("%BASE1%") do set "BASE_NAME=%%~nA"

:: Build the unzipped filename
set "UNZIPPED_FILE=%BASE_NAME%.txt.acmi"

::echo Base name: %BASE_NAME%
echo.
echo ^>^>^>^> 2. Strip lines starting with ",", remove useless "XXX destroyed.", and add Coalition=Allies to Group=Player to unzipped file: %UNZIPPED_FILE%

:: Strip empty lines or lines starting with commas
findstr /V /R "^," "%UNZIPPED_FILE%" > "%UNZIPPED_FILE%-stripped.txt.acmi"
::copy "%UNZIPPED_FILE%"  "%UNZIPPED_FILE%-stripped.txt.acmi"

::powershell -Command "[System.IO.File]::WriteAllLines('%UNZIPPED_FILE%-stripped.txt.acmi', ([System.IO.File]::ReadAllLines('%UNZIPPED_FILE%') | Where-Object { $_ -notmatch '^\,' } | ForEach-Object { $_.Replace('Group.Pilot', 'Group=Pilot,Coalition=Allies') }))"
powershell -Command "[System.IO.File]::WriteAllLines('%UNZIPPED_FILE%-stripped.txt.acmi', ([System.IO.File]::ReadAllLines('%UNZIPPED_FILE%') | Where-Object { $_ -notmatch '^\,' } | Where-Object { $_ -notmatch '^0\,Event\=Message.*destroyed\.' } | ForEach-Object { $_.Replace('Group=Player', 'Group=Player,Coalition=Allies') }))"

set /a "ERROR=ERRORLEVEL + ERROR"

:: Ensure the output directory exists
if not exist "processed" mkdir "processed"

set "SHORTNAME=%BASE_NAME:~8,15%"


for /f "delims=" %%A in ('powershell -Command "[datetime]::ParseExact('%SHORTNAME%', 'yyyyMMdd-HHmmss', $null) | ForEach-Object { [TimeZoneInfo]::ConvertTimeToUtc($_, [TimeZoneInfo]::FindSystemTimeZoneById('Eastern Standard Time')).ToString('yyyy-MM-dd_HH.mm.ss') }"') do set "TEMP_TIME=%%A"

set "UTC_TIME=%TEMP_TIME:_=@%"

echo.
echo ^>^>^>^> 3. Fog-of-war processing %UNZIPPED_FILE%-stripped.txt.acmi at time %UTC_TIME%

:: Run Tacview filter
echo tacview-filter "%UNZIPPED_FILE%-stripped.txt.acmi" -o "processed\%UTC_TIME%Z.zip.acmi" --fog-of-war "Coalition=Allies 3"
tacview-filter "%UNZIPPED_FILE%-stripped.txt.acmi" -o "processed\%UTC_TIME%Z.zip.acmi" --fog-of-war "Coalition=Allies 4" --show-engagements

::tacview-filter "%UNZIPPED_FILE%-stripped.txt.acmi" -o "processed\%UTC_TIME%Z.zip.acmi" --fog-of-war "Color=Green,Group=Player 4" ::Should work I THINK but doesn't

set /a "ERROR=ERRORLEVEL + ERROR"
	
:: Rename to just the date	

::for /f "tokens=1,2,3* delims=-" %A in ('dir /b Tacview-*-COD-Genghis-Class-TacViewImplMission.zip.acmi') do ren "%A-%B-%C-%D" "%B-%C.zip.acmi"
::powershell -Command "Get-ChildItem -Path 'processed' -Filter 'Tacview-*-COD-Genghis-Class-TacViewImplMission.zip.acmi' | ForEach-Object { $parts = $_.Name -split '-'; Rename-Item $_.FullName -NewName """$($parts[1])-$($parts[2]).zip.acmi""" }"
::powershell -Command "Get-ChildItem -Path 'processed' -Filter 'Tacview-*-COD-Genghis-Class-TacViewImplMission-filtered.zip.acmi' | ForEach-Object { $p = $_.Name -split '-'; Rename-Item $_.FullName -NewName (\"$($p[1])-$($p[2]).zip.acmi\") }"

	

:: Clean up
::del "%UNZIPPED_FILE%-stripped.txt.acmi"
del %UNZIPPED_FILE%
if %ERROR% equ 0 (    
	echo.
	echo ^>^>^>^> 4.Clean up temp files ^& move orig to originals/%FILE_NAME%
	::move %FILE_NAME% originals/%FILE_NAME%
) else (
	echo ******An error occurred! The exit code sum is %ERROR% - last error %ERRORLEVEL%
	exit /b %ERRORLEVEL%
)


endlocal

:eof