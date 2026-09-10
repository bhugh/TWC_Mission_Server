# TWC_Mission_Server

Several complete, running multiplayer mission/campaign servers for IL2 Cliffs of Dover - Blitz & Tobruk.

Several of the directories are complete, separate campaign servers: Genghis, Tobruk, Campaign21, M001, M002, and M002 (from most recent to oldest).

Other directories contain ancillary programs that work together with server code:

 * _radar_ has all files needed to run the radar display, which runs on a separate public-facing server (javascript/HTML/etc).
 * The folder _res_ contains the various css, javascript, etc needed in the directory where stats are uploaded (again this is a public-facing HTML page - the HTML is uploaded by the server, and it expects the items in _res_ to be in the directory where the uploads go).
 * _CLODMissionCommunicator_ is code for a .dll that allows code in Cliffs of Dover missions and submissions loaded by the initial missions, to communicate and share data.  With improvements to CLOD scripting and mission-building in recent years, the Mission Communicator .dll is no longer strictly necessary.  But all the TWC Campaign Server missions and most core .cs files use the communicator .dll, because they were built using it in earlier years.  The .dll must be placed in a directory similar to _C:\Program Files (x86)\Steam\steamapps\common\IL-2 Sturmovik Cliffs of Dover\parts\core_ along with the main CLOD game .dlls like maddox.dll, gamePlay.dll, and Strategy.dll.  The exact directory will vary depending on your Steam setup.


Even if you don't run the any of the mission servers per se, the code has all sorts of examples showing how to do things programmatically within the IL2 Cliffs of Dover - Blitz & Tobruk scripting and mission-building system.
