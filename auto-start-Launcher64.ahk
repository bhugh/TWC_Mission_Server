;;This script ensures that Steam & Cliffs of Dover Launcher are started and running
;;and closes various error windows if they arise.  It will load a sequence of missions in
;;the order you choose or randomly shuffle them etc.
;;
;;It checks for Steam & CloD Launcher every 60 seconds (configurable) and will re-start them as necessary.  
;;It will also check whenever you manually press ctrl-alt-shift-J, which is helpful for testing purposes.
;;It can be configured to automatically start upon Windows startup, thus auto-starting your server and 
;;keeping it running indefinitely whenever Windows is running.
;;
;;To use it, You'll need to 
;;   1. Install AutoHotKey
;;   2. Update the location of your Steam.exe, Launcher64.exe, and other particulars in the script below (initialization section)
;;   3. Make a server-XXX.cmd file for each mission you want to run, and put it in your main CloD docs directory (   C:\Users\XXX\Documents\1C SoftClub\il-2 sturmovik cliffs of dover or similar).  The .cmd file should include all setup commands for your mission, missload of the missiong, and "battle start".  Like this:

;;           battle stop
;;           file clod-server-settings-outside-views-unlimited-ammo.cmd
;;           missLoad "missions\Multi\Fatal\jake45\jake.mis"
;;           battle start
;;   4. List the FILE NAMES of each of these mission server-XXX.cmd files in the mission_list array  below.
;;   5. Choose your shuffle type below (random, in order, etc). Set other options according to your preference (USER SETTINGS section below).
;;   6. Place this script (or a shortcut to it) in your system startup folder so that it 
;;     auto-starts when windows starts.  You can also configure your windows account to auto-login whenever
;;     the server is turned on (Google for instructions).  Thus whenever the computer is turned on, the account 
;;     will auto-login which will start this Auto-Hotkey script, which will then auto-load Steam, Launcher, and your mission(s).  
;;     So, in short, in ordinary circumstances the server will auto-start when Windows starts and will continue 
;;     running for as long as Windows continues running.
;;   7. The script checks once per minute whether launcher.exe and steam.exe are running.  This time interval is adjustable below. Also you can use ctrl-alt-shift-j to perform the check instantly (useful for testing).

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; USER SETTINGS
;
global mission_list := ["server-jake.cmd", "server-jake2.cmd", "server-jake3.cmd"]
global shuffle_type := "random" ; can be "random" "unique random" "in order" "in order random start" - see explanation below
global no_repeats := true ; random & unique random shuffles can lead to missions being immediately repeated - this prevents the mission repeats if possible
global avoid_skips := true ; if the mission fails to load it will try the same mission again until successful, if this is set to true.  If false, when the mission load fails it skips to the next mission according to shuffle_type

;;;;;;;;;;;; shuffle_type explanation ;;;;;;;;;;;;;;;;;;;;;;;
 ; "random" - selects randomly from your entire mission_list each time. Missions may be repeated (every mission has an equal chance of being selected each time) 
 ; "unique random" - shuffles your list into random order but plays every mission on the list once before repeating any missions (then it shuffles the list again, plays through them all again, etc) 
 ; "in order" - just plays the missions in the order you put them, starting with the first file.  After the last mission it will start again with the first. 
 ; "in order random start" - plays the missions in order but picks the first mission randomly, rather than starting with the first file on the list
 ; Note that you can fairly easily code & use your own custom shuffle_type if you like

global launcher_fullpath := "C:\Games\SteamLibrary\steamapps\common\IL-2 Sturmovik Cliffs of Dover Blitz\Launcher64.exe"
global launcher_startdir :=  "C:\Games\SteamLibrary\steamapps\common\IL-2 Sturmovik Cliffs of Dover Blitz\"
global launcher_ahk_exe := "Launcher64.exe"  ; exe name of process as reported by Window Spy - CASE SENSITIVE

global steam_fullpath := "C:\Program Files (x86)\Steam\Steam.exe"
global steam_startdir :=  "C:\Program Files (x86)\Steam\"
global steam_ahk_exe := "Steam.exe"  ; exe name of process as reported by Window Spy - CASE SENSITIVE

global missStartedCheck_times := 7 ; number of times to check whether the mission has loaded successfully (by looking for "Battle Begins" or similar)
global missStartedCheck_delay := 6000 ; delay (in milliseconds) between checks for "Battle Begins".  So if you check 10X and delay 5000 ms each time you'll wait 50 seconds altogether before giving up on the successful mission load & trying to reload launcher.  
global missStartedCheck_string := "Battle begins!" ; Note that this is NOT CASE SENSITIVE
global messagebox_display_time_seconds := 15 ; Time to display message boxes 
global display_messageboxes := true ; whether or not to display messageboxes/progress
global steamRestart_number := 5 ; When a mission doesn't start, we kill launcher & retry.  After X times doing this, we also kill Steam & restart it.
;Notes: It often takes 30-60 seconds for a mission to fully load.  The method to check whether the mission has successfully started is as bit flakey so it is better to check a few times rather than just wait e.g. 60 seconds then check once.
;Steam sometimes has updates that take a fairly long time to complete.  If the process is killed the steam updates won't complete and they will just have to re-start again next time Steam is loaded.  So it doesn't pay to kill/restart Steam too rapidly, as you can end up in a cycle where it is never able to complete the update.  Probably minimum of 3-4 minutes between Steam kills is reasonable, but it depends on your system, internet connection, etc.
;Note that the script stops while message boxes are displayed, which affects the timing scheme outlined above.

global CheckNoProgress_enabled := true ; whether or not to check for progress/change in the Launcher.exe console.  If no change/progress in the console after a specified # of checks the window will be closed.
global CheckNoProgress_limit := 10 ; The Launcher window will be checked once every check_interval_ms.  If the console window remains unchanged for CheckNoProgress_limit times in a row, then the window will be closed & Launcher restarted.  This is designed to catch hung servers & the like, but be sure your _limit is high enough a quiet server isn't inadvertantly shut down.     

global check_interval_ms := 60000 ; time between checks for steam.exe/launcher.exe, in milliseconds.  60000 ms/60 seconds is about right.

;
;END User Settings
;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; TODOS
;   X Check for lengthy period of no activity in Launcher.exe window & kill process in that case
;   - Send an email when it gets stuck etc
;   X Stop it from messing up the Clipboard   

#NoEnv  ; Recommended for performance and compatibility with future AutoHotkey releases.
; #Warn  ; Enable warnings to assist with detecting common errors.
SendMode Input  ; Recommended for new scripts due to its superior speed and reliability.
SetWorkingDir %A_ScriptDir%  ; Ensures a consistent starting directory.

;a few needed global variables
global OChoice_place := 1
global UR_Array := []
global UR_Original_Array := []
global previous_mission
global steamRestart_count := 0
global missLoaded := true 
global CheckNoProgress_clipboard := ""
global CheckNoProgress_count := 0 

; Initialize various shuffle types, for those that need initialization
If ( shuffle_type = "unique random" ) 
 Initialize_Unique_RChoice(mission_list)
If ( shuffle_type = "in order" ) 
 Initialize_OChoice_First(mission_list)
If ( shuffle_type = "in order random start" ) 
 Initialize_OChoice_Random(mission_list)

;Run the launcher checker once immediately, then every 60 seconds thereafter
#Persistent
SetTimer, StartLauncher, % check_interval_ms
Gosub, StartLauncher

return

+!^j::
  ; You can un-comment things commented out here in order to test them via ctrl-alt-shift-j
  ;mission := ChooseNextMission(mission_list, shuffle_type)
  ;MsgBox,,, % "(Sample/Test) Next Mission: " . mission,5 
  
  ;If (WinExist("ahk_exe " . steam_ahk_exe)) {
  ;            WinActivate, % "ahk_exe " . steam_ahk_exe 
  ;            WinClose, % "ahk_exe " . steam_ahk_exe
  ;            Process, close, % steam_ahk_exe
  ;         }  
  Gosub, StartLauncher
    
  ;Gosub, CheckMissionStarted
  ;sleep, 1000
  ;ControlSend, , f server-jake.cmd{enter}, % "ahk_exe " . launcher_ahk_exe

Return

StartLauncher:
      
   Gosub, KillErrors ; kill any Launcher64 error windows
   sleep, 1000
   Gosub, RunSteam ; make sure steam.exe is started
   sleep, 6000
   Gosub, RunLauncher64 ; make sure launcher64.exe is started
   CheckNoProgress() ; check whether launcher.exe console is frozen/needs restart
     
Return

;;Make sure Launcher64 is running
RunLauncher64:
  DetectHiddenWindows, On
;  Process, Exist, launcher64.exe 
  If (!WinExist("ahk_exe " . launcher_ahk_exe)) 

 {
   Run, %launcher_fullpath% -server, % launcher_startdir
   CheckNoProgress_count := 0
   
   mission := ChooseNextMission(mission_list, shuffle_type)
   
   ;sleep, 3000
   ;SetControlDelay -1
   ;ControlSend, , % "f " . mission . "{enter}", % "ahk_exe " . launcher_ahk_exe
         
     
   SetKeyDelay 20,20 ; Slowing key delay seems to help with recognition of keystrokes & you may need to slow it further depending on your exact system
   WinWait, % "ahk_exe " . launcher_ahk_exe
   Send % "f " . mission . "{enter}" ; it might be smart to send {enter} BEFORE the command as well to clear any extraneous keystrokes
   if (display_messageboxes) 
     MsgBox,,, % "Loading Mission: " . mission,% messagebox_display_time_seconds
   Gosub, CheckMissionStarted
   
  }
Return

;;Make sure Steam is running
RunSteam:
  DetectHiddenWindows, On
  If (!WinExist("ahk_exe " . steam_ahk_exe)) 

  {
    Run, %steam_fullpath%, % steam_startdir 
  }
Return

;;Kill any error windows
KillErrors:
  DetectHiddenWindows, On
  ;;This is a list of error windows that have been observed; we use ahk_exe plus window title to identify them.  These can be discovered using Window Spy.  New error windows can be added to this list so they can be auto-closed when they occur.
  ;; TO DO: Abstract this & list to the error window parameters in an array
  ;; in the initialization section so it can be more easily customized
  If (WinExist("Error ahk_exe " . launcher_ahk_exe) || WinExist("Steam Connection ahk_exe " . launcher_ahk_exe)  || WinExist("Steam Connection Error ahk_exe " . launcher_ahk_exe) || WinExist("Microsoft .NET Framework ahk_exe " . launcher_ahk_exe)) {
     WinClose
     sleep, 250
     Gosub, KillErrors ; Check to see if any more error windows are open & kill all if they exist
  }

Return

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; CheckMissionStarted
;
; Once Launcher is loaded & the f server-XXX.cmd command is sent, this routine
; takes over & checks whether or not the mission is actually succesfully loaded
; and started.  If not it takes actions depending on what happened/how severe.
;

CheckMissionStarted:
  global missStartedCheck := 0  
  Gosub, CheckMissionStartedSub
Return


CheckMissionStartedSub:
  sleep, missStartedCheck_delay
  DetectHiddenWindows, On
  OldClipboard := Clipboard
  Clipboard:=""
  If (WinExist("ahk_exe " . launcher_ahk_exe)) {
       ;WinActivate
       ;Send ^a{enter}

       ;MsgBox,,, % clipboard,2
       ;ControlSend, , ^a^a{enter}, % "ahk_exe " . launcher_ahk_exe
       WinActivate, % "ahk_exe " . launcher_ahk_exe
       ;WinWait, % "ahk_exe " . launcher_ahk_exe
       ;Send ^a{enter}
       ;SendInput ^a{enter}
       SetKeyDelay 20,20
       ControlSend, ahk_parent, ^a^a{enter}, % "ahk_exe " . launcher_ahk_exe
       ClipWait, 1
   }
   ;MsgBox,,, % Clipboard,2

   ; missStartedCheck_string is set in initialization section; usually "Battle Begins!"        
   If InStr(Clipboard, missStartedCheck_string) 
   {
     
     if (display_messageboxes) 
        MsgBox, , ,% "Battle Begins! Mission started successfully! (Found '" . missStartedCheck_string . "' in the Launcher.exe window)", % messagebox_display_time_seconds
     missLoaded := true   
     steamRestart_count := 0
   }
   Else 
   {         
     missStartedCheck++
     
     ;;10X5 seconds with no "Battle Begins!" means we close launcher & steam & try it all again
     If (missStartedCheck > missStartedCheck_times) 
     {
        
        if (display_messageboxes)
           MsgBox , , , Mission failed to start - restarting Launcher, % messagebox_display_time_seconds/2
        If (WinExist("ahk_exe " . launcher_ahk_exe)) { 
               WinActivate, % "ahk_exe " . launcher_ahk_exe
               SetKeyDelay 20,20                   
               ControlSend, ahk_parent, {enter}battle stop{enter}
               sleep, 7000
               ControlSend, ahk_parent, {enter}exit{enter}
               sleep, 250            
               WinClose
               sleep, 250
        }  
        steamRestart_count++
        if (steamRestart_count >= steamRestart_number) { 
           if (display_messageboxes)
                MsgBox , , , Mission failed to start repeatedly - restarting Steam, % messagebox_display_time_seconds/2 
           ; sendMail("ToEmail@gmail.com","YourPW","FromEmail@gmail.com","This is the subject line.","This is the email body.  Return keys must happen inline.`n`nUse the new line for that.","C:\file.txt")     
           If (WinExist("ahk_exe " . steam_ahk_exe)) {
              WinActivate, % "ahk_exe " . steam_ahk_exe 
              WinClose, % "ahk_exe " . steam_ahk_exe
              Process, close, % steam_ahk_exe ; WinClose doesn't seem to work reliably with Steam.exe?
           }
           steamRestart_count := 0
        }                              
     } 
     Else { ;; no "Battle Begins!" yet but we'll keep checking
        if (display_messageboxes) 
           MsgBox, , , No '%missStartedCheck_string%' found yet - still checking (%missStartedCheck% times), % messagebox_display_time_seconds/5
        Gosub, CheckMissionStartedSub
     }
   }
   Clipboard := OldClipboard
  
Return  

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; CheckNoProgress
;
; Checks whether Launcher.exe console has had any activity.
; No activity for an extended period & we assume it is dead & re-start
;

CheckNoProgress(){
  if (!CheckNoProgress_enabled) return
  DetectHiddenWindows, On
  OldClipboard := Clipboard
  Clipboard:=""
  If (WinExist("ahk_exe " . launcher_ahk_exe)) {
       WinActivate, % "ahk_exe " . launcher_ahk_exe
       SetKeyDelay 20,20
       ControlSend, ahk_parent, ^a^a{enter}, % "ahk_exe " . launcher_ahk_exe
       ClipWait, 1
   }
   
   ; if the Launcher.exe console text is exactly the same as last time AND
   ; it's a non-empty string then we increment the 'no change' counter
   if (Clipboard = CheckNoProgress_clipboard && StrLen(Clipboard) > 0) 
     CheckNoProgress_count++
   else CheckNoProgress_count := 0
     
   CheckNoProgress_clipboard := Clipboard
   Clipboard := OldClipboard
      
   if (CheckNoProgress_count >= CheckNoProgress_limit) {
        if (display_messageboxes)
           MsgBox , , , % "Launcher console frozen (checked " . CheckNoProgress_count . " times) - restarting" , % messagebox_display_time_seconds/2
        CheckNoProgress_count := 0
        If (WinExist("ahk_exe " . launcher_ahk_exe)) { 
               WinActivate, % "ahk_exe " . launcher_ahk_exe
               SetKeyDelay 20,20                   
               ControlSend, ahk_parent, {enter}battle stop{enter}
               sleep, 7000
               ControlSend, ahk_parent, {enter}exit{enter}
               sleep, 250            
               WinClose
               sleep, 250
        }   
   
   }   
   


}

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; SELECTING THE NEXT MISSION
;
; Routines to select the next mission to play using various methods
;
ChooseNextMission(mission_list1, shuffle_type1) {

  ;If the mission wasn't successfully loaded the last time
  ;we just try that same mission again
  if (avoid_skips && !missLoaded && HasVal(mission_list1, mission)) 
    return, mission
     
  Loop, 10 {
   mission := RChoice(mission_list1) ; RChoice is the default
   If ( shuffle_type1 = "unique random" ) 
     mission := Unique_RChoice()
   If ( shuffle_type1 = "in order" ) 
     mission := OChoice(mission_list1)
   If ( shuffle_type1 = "in order random start" ) 
     mission := OChoice(mission_list1)
   
   ; avoid repeating the same mission 2X in a row if the 
   ; no_repeats setting is TRUE.  Loop out after 10X just
   ; in case it is impossible to avoid the repeat.
   if (!no_repeats || mission != previous_mission)
      break      
  }
   previous_mission := mission
   missLoaded := false   
   return, mission                
}

; Unique_RChoice: PLAYS MISSIONS IN RANDOM ORDER BUT WITH NO REPEATS UNTIL ALL MISSIONS HAVE RUN ONCE
; Call Initialize_Unique_RChoice(Array) first to initialize
; Then call Unique_RChoice() to get the string with the mission name each time thereafter

Initialize_Unique_RChoice(Array){
  UR_Array := Array.Clone()
  UR_Original_Array := Array.Clone()
}

Unique_RChoice(){
	If (ArraySize(UR_Array)=0)
		UR_Array := UR_Original_Array.Clone()

	If (ArraySize(UR_Array) = 0)
		return, "ERROR"
	else {	
	     Random,Rand,1,% ArraySize(UR_Array)
	     Ret := UR_Array[Rand]
	     UR_Array.Remove(Rand)
	     return, Ret
  }
	
}

; RChoice: PLAYS MISSIONS IN PURELY RANDOM ORDER THAT MAY REPEAT
; Then call RChoice(Array) to get the string with the mission name


RChoice(Array){
	If (ArraySize(Array) = 0)
		return, "ERROR"
	else {
	
	     Random,Rand,1,% ArraySize(Array)
	     Ret := Array[Rand]	     
	     return, Ret
  }
}


; OChoice: PLAYS MISSIONS IN ORDER
; Call Initialize_OChoice_First(Array) OR Initialize_OChoice_Random(Array) at the beginning to initialize
; Then call OChoice(Array) to get the string with the mission name each time thereafter
; Call Initialize_OChoice_First(Array) will start with first mission listed while
; Initialize_OChoice_Random(Array) will start with a randomly selected mission
; after that the missions will follow in order 


Initialize_OChoice_First(Array){

  OChoice_place := 1 ; start with the first element
  
}

Initialize_OChoice_Random(Array){

  OChoice_place := 1 ; start with the first element

	If (ArraySize(Array) = 0 )
		return, 0
	else {
	
	     Random,Rand,1,% ArraySize(Array)	     	     
	     OChoice_place := Rand
  }  
}

OChoice(Array){
	If (Array.MaxIndex() = 0 || Array.MaxIndex() = "")
		return, "ERROR"
	else {
	

	     Ret := Array[OChoice_place]
       OChoice_place++
       if (OChoice_place > ArraySize(Array))
          OChoice_place := Array.MinIndex()	     
	     return, Ret
  }
}

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; Utility functions
;

ArraySize(Array){
  count :=0
  for key, value in Array
    count++
  return, count  
}

HasVal(haystack, needle) {
	if !(IsObject(haystack)) || (haystack.Length() = 0)
		return 0
	for index, value in haystack
		if (value = needle)
			return index
	return 0
}

sendMail(emailToAddress,emailPass,emailFromAddress,emailSubject,emailMessage, fileLocation)
	{
        mailsendlocation := A_MyDocuments
	IfNotExist, %mailsendlocation%\mailsend1.17b12.exe
		URLDownloadToFile, https://mailsend.googlecode.com/files/mailsend1.17b12.exe, %mailsendlocation%\mailsend1.17b12.exe
	Run, %mailsendlocation%\mailsend1.17b12.exe -to %emailToAddress% -from %emailFromAddress% -ssl -smtp smtp.gmail.com -port 465 -sub "%emailSubject%" -M "%emailMessage%" +cc +bc -q -auth-plain -user "%emailFromAddress%" -pass "%emailPass%" -attach "%fileLocation%",, Hide
	}


;;;;;;;;;;;;;;;;;;;;;;
;Error WINDOWS from Launcher
; Error
; ahk_class #32770
; ahk_exe Launcher64.exe
; TEXT:
; OK
; SteamGameServer_Init call failed
;
;Steam Connection
;ahk_class #32770
;ahk_exe Launcher64.exe
;OK
;Server got logged out of Steam, Exiting
