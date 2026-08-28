This directory contains the running files for the TWC "Twin Grandsons of Khan" server.

They should be placed in a directory like: *C:\Users\..yourusernametc..\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Fatal\Genghis\Fresh Input File*

Then run the genghis.mis file within your Launcher64.exe -server

It will probably run if placed in another subdirectory within C:\Users\..yourusernametc..\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\

Requires setting up a number of directories near the top of genghis.cs and Genghis-Class-StatsMission.cs. Among other things this sets up the location of stats.ini, where a lot more directories, passwords, default values, etc need to be set up.  Most of these can be set up in Genghis-Class-StatsMission.cs but are better handled in stats.ini.

Radar & stats are uploaded via FTP to a web server somewhere - that server also requires certain files which are included here.

Similarly for Tacview - a directory where files are saved by tacviewrecorder.dll and scripts there for processing the files.  The main script runs these scripts and then later, transfers the files via FTP to a different directory on the same server. parts/core/TacviewRecorder.dll is the location for the .dll.  A copy of the required .dll should be in this distribution as well.

The TWCClodMission communicator is the other required .dll. parts/core/CloDMissionCommunicator.dll.  A separate directory has the full build info & source files for that.

In genghis.cs several passwords etc for the radar access must be set up.  Search for "password" and "SECRET".

Even if you don't run the mission server per se, the code has all sorts of examples showing how to do things programmatically within the IL2 Cliffs of Dover - Blitz scripting and mission-building system.
