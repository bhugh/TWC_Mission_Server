@echo off
setlocal enabledelayedexpansion

:: ==========================================
:: CONFIGURATION - CHANGE THESE DETAILS
:: ==========================================
set "FTP_SERVER=brenthugh.com"
set "FTP_USER=mobikefed"
set "FTP_PASS=1Spoke$#$"
set "FTP_REMOTE_DIR=/brenthugh.com/twc/tacview"
:: ==========================================

:: Setup target directories
if not exist "processed" mkdir "processed"
if not exist "originals" mkdir "originals"

echo Starting ACMI batch processing...

:: ----------------------------------------------------
:: PHASE 1: LOOP & PROCESS ALL *.zip.acmi FILES
:: ----------------------------------------------------
for %%F in (*.zip.acmi) do (
    echo.
    echo Processing ZIP archive: %%F
    
    :: Extract the inner txt.acmi file
    tar -xf "%%F"
    
   :: Strip .acmi to get "myfile.zip", then strip .zip to get "myfile"
    set "BASE_NAME=%%~nF"
    set "BASE_NAME=!BASE_NAME:.zip=!"
    set "UNZIPPED_FILE=!BASE_NAME!.txt.acmi"
    echo Unzipped name: !UNZIPPED_FILE!
	:: pause
    
    if exist "!UNZIPPED_FILE!" (
        :: Strip the leading commas
        findstr /V /R "^," "!UNZIPPED_FILE!" > "temp-stripped.txt.acmi"
        
        :: Run through your filtering utility (saving straight to the processed folder)
        tacview-filter "temp-stripped.txt.acmi" -o "processed\%%~nF-filtered.zip.acmi" --fog-of-war "Color=Green,Group=Player 4"
        
        :: Clean up intermediate unzipped and temporary files
        del "!UNZIPPED_FILE!"
        del "temp-stripped.txt.acmi"
        
        :: Move original file out of the way
        move "%%F" "originals\"
    ) else (
        echo Error: Could not find extracted text file for %%F
    )
)

:: ----------------------------------------------------
:: PHASE 2: LOOP & PROCESS ALL *.txt.acmi FILES
:: ----------------------------------------------------
for %%F in (*.txt.acmi) do (
    :: Ensure we don't accidentally grab something we are working on
    if not "%%F"=="temp-stripped.txt.acmi" (
        echo.
        echo Processing TXT file: %%F
        
        :: Strip the leading commas
        findstr /V /R "^," "%%F" > "temp-stripped.txt.acmi"
        
        :: Filter and save to processed folder
        tacview-filter "temp-stripped.txt.acmi" -o "processed\%%~nF-filtered.zip.acmi" --fog-of-war "Color=Green,Group=Player 4"
        
        :: Clean up intermediate file
        del "temp-stripped.txt.acmi"
        
        :: Move original file out of the way
        move "%%F" "originals\"
    )
)

:: ----------------------------------------------------
:: PHASE 3: UPLOAD TO FTP
:: ----------------------------------------------------
echo.
echo Preparing FTP Upload...

:: Generate a temporary text file with FTP commands
set "FTP_CMD_FILE=temp_ftp_commands.txt"
echo open %FTP_SERVER% > "%FTP_CMD_FILE%"
echo %FTP_USER%>> "%FTP_CMD_FILE%"
echo %FTP_PASS%>> "%FTP_CMD_FILE%"
echo cd %FTP_REMOTE_DIR%>> "%FTP_CMD_FILE%"
echo binary>> "%FTP_CMD_FILE%"

:: Append 'mput' command for every file inside the processed folder
for %%P in (processed\*.zip.acmi) do (
    echo put "processed\%%~nxP">> "%FTP_CMD_FILE%"
)
echo quit>> "%FTP_CMD_FILE%"

:: Execute the Windows native FTP client using the generated script
echo Uploading files to FTP...
ftp -s:"%FTP_CMD_FILE%"

:: Clean up the temporary FTP script for security (removes passwords)
del "%FTP_CMD_FILE%"

echo.
echo Process Complete. Originals moved to \originals, results uploaded from \processed.
pause