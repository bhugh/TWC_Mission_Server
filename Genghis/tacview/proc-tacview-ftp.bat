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