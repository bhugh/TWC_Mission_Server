The TacviewRecorder files are created by Genghis-Class-TacviewRecorderImpl.cs in directory ..../Fresh Input File/tacview.

The PowerShell script process-tacview.ps1 in this directory (runs best with PowerShell 7+, but probably with PowerShell 5+) processes the .zip.acmi file created by TacviewRecorder for Fog-of-War and other details, and readies it for upload.  It is called by Genghis-Class-TacviewRecorderImpl.cs about a minute after each mission starts, but can also be run by hand at any time.   -stats.cs then uploads anything found in the .../tacview/processed directory via FTP periodically in the web directory & FTP server specified in stats.ini, ...../twc/tacview (radar files go to ..../twc/radar).  

The script processes any .zip.acmi file found in .../tacview and moves the original file to .../tacview/originals after processing, saving the processed/filtered file to .../tacview/processed.  The FTP upload script finds any file in .../tacview/processed, uploads it via FTP, and moves any file uploaded via FTP to .../tacview/uploaded.

Other .exe files, .bat, .ps1 in this folder are various helper scripts (particularly tacview-filter.exe, which handles fog-of-war filtering) used in the main .ps1, or other versions or types of filtering useful for various purposes.

Also to make TacviewRecorder work correctly, you need the files found in IL2CLOD folder/Tacview (in a separate folder here).
In addition to this, the TacviewRecorder.dll (installed in the Steam ...../core directory), Genghis-Class-TacviewRecorderImpl.cs, the ..../Fresh Input File/tacview directory and scripts within it (found in a separate directory in this repository), you'll need the files found in .../IL2CLOD dir/Tacview - especially the .csv file found there.  Those files are also in a separate directory in this repository.
