;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; USER SETTINGS
;
; Will be included in auto-start-Launcher64.ahk
;

global mission_list := ["server-campaign.cmd"]
global shuffle_type := "random" ; can be "random" "unique random" "in order" "in order random start" - see explanation below
global no_repeats := true ; random & unique random shuffles can lead to missions being immediately repeated - this prevents the mission repeats if possible
global avoid_skips := true ; if the mission fails to load it will try the same mission again until successful, if this is set to true.  If false, when the mission load fails it skips to the next mission according to shuffle_type

;;;;;;;;;;;; shuffle_type explanation ;;;;;;;;;;;;;;;;;;;;;;;
 ; "random" - selects randomly from your entire mission_list each time. Missions may be repeated (every mission has an equal chance of being selected each time) 
 ; "unique random" - shuffles your list into random order but plays every mission on the list once before repeating any missions (then it shuffles the list again, plays through them all again, etc) 
 ; "in order" - just plays the missions in the order you put them, starting with the first file.  After the last mission it will start again with the first. 
 ; "in order random start" - plays the missions in order but picks the first mission randomly, rather than starting with the first file on the list
 ; Note that you can fairly easily code & use your own custom shuffle_type if you like

global launcher_fullpath := "C:\Program Files (x86)\Steam\steamapps\common\IL-2 Sturmovik Cliffs of Dover Blitz\Launcher64.exe"
global launcher_startdir :=  "C:\Program Files (x86)\Steam\steamapps\common\IL-2 Sturmovik Cliffs of Dover Blitz\"
global launcher_ahk_exe := "Launcher64.exe"  ; exe name of process as reported by Window Spy - CASE SENSITIVE

global steam_fullpath := "C:\Program Files (x86)\Steam\Steam.exe"
global steam_startdir :=  "C:\Program Files (x86)\Steam\"
global steam_ahk_exe := "Steam.exe"  ; exe name of process as reported by Window Spy - CASE SENSITIVE

global logfile_fullpath := "C:\Users\Administrator\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\autostart-launch64-logfile.txt" ; Leave blank if you want no logfile saved

global missStartedCheck_times := 3 ; number of times to check whether the mission has loaded successfully (by looking for "Battle Begins" or similar)
global missStartedCheck_delay := 6000 ; delay (in milliseconds) between checks for "Battle Begins".  So if you check 10X and delay 5000 ms each time you'll wait 50 seconds altogether before giving up on the successful mission load & trying to reload launcher.  
global missStartedCheck_string := "Battle begins!" ; Note that this is NOT CASE SENSITIVE
global messagebox_display_time_seconds := 10 ; Time to display message boxes 
global display_messageboxes := true ; whether or not to display messageboxes/progress
global steamRestart_number := 5 ; When a mission doesn't start, we kill launcher & retry.  After X times doing this, we also kill Steam & restart it.
;Notes: It often takes 30-60 seconds for a mission to fully load.  The method to check whether the mission has successfully started is as bit flakey so it is better to check a few times rather than just wait e.g. 60 seconds then check once.
;Steam sometimes has updates that take a fairly long time to complete.  If the process is killed the steam updates won't complete and they will just have to re-start again next time Steam is loaded.  So it doesn't pay to kill/restart Steam too rapidly, as you can end up in a cycle where it is never able to complete the update.  Probably minimum of 3-4 minutes between Steam kills is reasonable, but it depends on your system, internet connection, etc.
;Note that the script stops while message boxes are displayed, which affects the timing scheme outlined above.

global CheckNoProgress_enabled := true ; whether or not to check for progress/change in the Launcher.exe console.  If no change/progress in the console after a specified # of checks the window will be closed.
global CheckNoProgress_limit := 10 ; The Launcher window will be checked once every check_interval_ms.  If the console window remains unchanged for CheckNoProgress_limit times in a row, then the window will be closed & Launcher restarted.  This is designed to catch hung servers & the like, but be sure your _limit is high enough a quiet server isn't inadvertantly shut down.     

global check_interval_ms := 60000 ; time between checks for steam.exe/launcher.exe, in milliseconds.  60000 ms/60 seconds is about right.

global setnextMissionStartString := "<nextmission>" ; 
global setnextMissionEndString := "</nextmission>" ; if these two strings appear in the console with a number in between them then we will set the mission number specified as the next mission next time the server restarts.  IE <nextmission>3</nextmission> will set the 3rd mission in the server_list as the next mission to run

;
;END User Settings
;
