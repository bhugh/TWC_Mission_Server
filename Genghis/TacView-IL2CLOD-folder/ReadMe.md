These are files that must be placed in folder C:\Users\[myusername...]\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\TacView (or whatever is the equivalent in your CLoD install).

Especially important is the .csv file, which contains the mapping between aircraft/actors in CLoD and the models used in Tacview. The file is opened and read by TacviewRecorder.dll - if the file is not there, things go badly wrong.  Most aircraft/actors won't have models, most .acmi files recorded won't open in Tacview (many lines in the file will start with "," - meaning that the TacviewRecorder has lost track of that actor - there should be the actor ID followed by ","), and your Tacview may crash etc when trying to playback.

Also note that you can edit this file to change the models used in Tacview.

