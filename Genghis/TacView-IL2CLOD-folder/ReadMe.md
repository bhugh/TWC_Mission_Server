These are files that must be placed in folder C:\Users\[myusername...]\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\TacView (or whatever is the equivalent in your CLoD install).

Especially important is the .csv file, which contains the mapping between aircraft/actors in CLoD and the models used in Tacview. The file is opened and read by TacviewRecorder.dll - if the file is not there, things go badly wrong.  Most aircraft/actors won't have models, most .acmi files recorded won't open in Tacview (many lines in the file will start with "," - meaning that the TacviewRecorder has lost track of that actor - there should be the actor ID followed by ","), and your Tacview may crash etc when trying to playback.

Also note that you can edit this file to change the models used in Tacview.

Note that these files, and TacviewRecorder.dll are from from JDU/FlyBy's TacviewRecorder 2.0 - more info at 
  https://www.youtube.com/watch?v=agcQxz1CHm0 
  https://forum.il2sturmovik.com/topic/88789-tacview-recorder-20-add-on-is-available/
  https://drive.google.com/drive/folders/1t7_Ekt4fcp5lT7vz9W8QaY9O09JzbJwF

In addition to the files in this directory in the correct location, you will need:
 * The TacviewRecorder.dll (installed in the Steam ...../core directory)
 * Genghis-Class-TacviewRecorderImpl.cs in .../Fresh Input File
 * The ..../Fresh Input File/tacview directory and scripts within it (found in a separate directory in this repository)
 * The files (and follow the instructions) found in ../Genghis/Tacview-web elsewhere in this distribution

