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
;;   5. Choose your shuffle type below (random, in order, etc). Set other options, log file location, etc, according to your preference (USER SETTINGS section below).
;;   6. Place this script (or a shortcut to it) in your system startup folder so that it 
;;     auto-starts when windows starts.  You can also configure your windows account to auto-login whenever
;;     the server is turned on (Google for instructions).  Thus whenever the computer is turned on, the account 
;;     will auto-login which will start this Auto-Hotkey script, which will then auto-load Steam, Launcher, and your mission(s).  
;;     So, in short, in ordinary circumstances the server will auto-start when Windows starts and will continue 
;;     running for as long as Windows continues running.
;;   7. The script checks once per minute whether launcher.exe and steam.exe are running.  This time interval is adjustable below. Also you can use ctrl-alt-shift-j to perform the check instantly (useful for testing).
;;   8. You can manually choose the next mission to run via hotkeys Shift-Ctrl-Alt-N and Shift-Ctrl-Alt-P, which will cycle through your list of missions.

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; USER SETTINGS
;

#Include auto-start-Launcher64-settings.ahk

;
;END User Settings
;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; TODOS
;   X Check for lengthy period of no activity in Launcher.exe window & kill process in that case
;   - Send an email when it gets stuck etc
;   X Stop it from messing up the Clipboard  
;   X Add a way to set the next mission to run


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
global LauncherPID =
global nextMissionNumber =

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
  ;SendRemoteDesktopSessionToConsole()
  ;return
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

+!^n::
  if (nextMissionNumber>0)
    nextMissionNumber++
  else nextMissionNumber := 1
  if (nextMissionNumber>ArraySize(mission_list))
    nextMissionNumber := 1
  logMsgBox("Next Mission set to: " . nextMissionNumber . " (" . mission_list[nextMissionNumber] . "). Will take effect on next restart", messagebox_display_time_seconds/2)
return

+!^p::
  if (nextMissionNumber>0) 
    nextMissionNumber--
  else nextMissionNumber := ArraySize(mission_list)
  if (nextMissionNumber<1)
    nextMissionNumber := ArraySize(mission_list)
  logMsgBox("Next Mission set to: " . nextMissionNumber . " (" . mission_list[nextMissionNumber] . "). Will take effect on next restart", messagebox_display_time_seconds/2)
return

StartLauncher:
   CheckNoProgress() ; check whether launcher.exe console is frozen/needs restart
   Gosub, KillErrors ; kill any Launcher64 error windows
   sleep, 1000
   Gosub, RunSteam ; make sure steam.exe is started
   sleep, 6000
   Gosub, RunLauncher64 ; make sure launcher64.exe is started
     
Return

;;Make sure Launcher64 is running
RunLauncher64:
  DetectHiddenWindows, On
;  Process, Exist, launcher64.exe 
  If (!WinExist("ahk_exe " . launcher_ahk_exe)) 

 {
   Run, %launcher_fullpath% -server, % launcher_startdir,, LauncherPID
   CheckNoProgress_count := 0
   
   mission := ChooseNextMission(mission_list, shuffle_type)
   
   ;sleep, 3000
   ;SetControlDelay -1
   ;ControlSend, , % "f " . mission . "{enter}", % "ahk_exe " . launcher_ahk_exe
         
     
   SetKeyDelay 20,20 ; Slowing key delay seems to help with recognition of keystrokes & you may need to slow it further depending on your exact system
   ; WinWait, % "ahk_exe " . launcher_ahk_exe
   WinWait, % "ahk_pid " . LauncherPID
   WinActivateCheckConsoleActive("ahk_pid " . LauncherPID)
   Sleep, 3000
   Send % "{enter}f " . mission . "{enter}" ; it might be smart to send {enter} BEFORE the command as well to clear any extraneous keystrokes
   if (display_messageboxes) 
     ;MsgBox,,, % "Loading Mission: " . mission,% messagebox_display_time_seconds
     logMsgBox("Loading Mission: " . mission . "`n`nTo manually choose next mission: Shift-Ctrl-Alt-N or Shift-Ctrl-Alt-P",messagebox_display_time_seconds)
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
  WinActivateCheckConsoleActive("ahk_exe " . launcher_ahk_exe)
  If (WinExist("Error ahk_exe " . launcher_ahk_exe) || WinExist("Steam Connection ahk_exe " . launcher_ahk_exe)  || WinExist("Steam Connection Error ahk_exe " . launcher_ahk_exe) || WinExist("Microsoft .NET Framework ahk_exe " . launcher_ahk_exe ) || WinExist("ahk_exe WerFault.exe")) {
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
  OldClipboard := ClipboardAll
  Clipboard:=""
  If (WinExist("ahk_exe " . launcher_ahk_exe)) {
       ;WinActivate
       ;Send ^a{enter}

       ;MsgBox,,, % clipboard,2
       ;ControlSend, , ^a^a{enter}, % "ahk_exe " . launcher_ahk_exe
       ;WinActivate, % "ahk_exe " . launcher_ahk_exe
       ;WinWait, % "ahk_exe " . launcher_ahk_exe
       ;Send ^a{enter}
       ;SendInput ^a{enter}
       ;WinActivate, % "ahk_exe " . launcher_ahk_exe
       WinActivateCheckConsoleActive("ahk_exe " . launcher_ahk_exe)
       WinID := WinActive("ahk_exe " . launcher_ahk_exe)
       if (! WinID) 
         logMsgBox("No window for ahk_exe " . launcher_ahk_exe, 5)
       else
         logMsgBox("Window OK for ahk_exe " . launcher_ahk_exe, 5)
       SetKeyDelay 20,20
       ControlSend, ahk_parent, ^a^a{enter}, % "ahk_exe " . launcher_ahk_exe
       ClipWait, 1                  
  }
  ;MsgBox,,, % Clipboard,2
  C := Clipboard
  Clipboard := OldClipboard
  OldClipboard =
   

   ; missStartedCheck_string is set in initialization section; usually "Battle Begins!"        
   If InStr(C, missStartedCheck_string) 
   {
     
     if (display_messageboxes) 
        ;MsgBox, , ,% "Battle Begins! Mission started successfully! (Found '" . missStartedCheck_string . "' in the Launcher.exe window)", % messagebox_display_time_seconds
        logMsgBox("Battle Begins! Mission started successfully! (Found '" . missStartedCheck_string . "' in the Launcher.exe window)", messagebox_display_time_seconds)
     missLoaded := true   
     steamRestart_count := 0
   }
   Else 
   {         
     missStartedCheck++
     
     ;;10X5 seconds with no "Battle Begins!" means we close launcher & steam & try it all again
     If (missStartedCheck > missStartedCheck_times) 
     {
        SendRemoteDesktopSessionToConsole()
        if (display_messageboxes)
           ;MsgBox , , , Mission failed to start - restarting Launcher, % messagebox_display_time_seconds/2
           logMsgBox("Mission failed to start - restarting Launcher", messagebox_display_time_seconds/2)
        If (WinExist("ahk_exe " . launcher_ahk_exe)) { 
               ;WinActivate, % "ahk_exe " . launcher_ahk_exe
               WinActivateCheckConsoleActive("ahk_exe " . launcher_ahk_exe)               
               SetKeyDelay 20,20                   
               ControlSend, ahk_parent, {enter}battle stop{enter}
               sleep, 7000
               ControlSend, ahk_parent, {enter}exit{enter}
               sleep, 250            
               WinClose
               sleep, 250
               missLoaded := false
        }  
        steamRestart_count++
        if (steamRestart_count >= steamRestart_number) { 
           if (display_messageboxes)
                ;MsgBox , , , Mission failed to start repeatedly - restarting Steam, % messagebox_display_time_seconds/2 
                logMsgBox("Mission failed to start repeatedly - restarting Steam", messagebox_display_time_seconds/2) 
           ; sendMail("ToEmail@gmail.com","YourPW","FromEmail@gmail.com","This is the subject line.","This is the email body.  Return keys must happen inline.`n`nUse the new line for that.","C:\file.txt")     
           If (WinExist("ahk_exe " . steam_ahk_exe)) {
              ;WinActivate, % "ahk_exe " . steam_ahk_exe 
              WinActivateCheckConsoleActive("ahk_exe " . steam_ahk_exe )
              WinClose, % "ahk_exe " . steam_ahk_exe
              Process, close, % steam_ahk_exe ; WinClose doesn't seem to work reliably with Steam.exe?
           }
           steamRestart_count := 0
        }                              
     } 
     Else { ;; no "Battle Begins!" yet but we'll keep checking
         If (missStartedCheck = missStartedCheck_times + 1) { 
             SendRemoteDesktopSessionToConsole()
         }
        if (display_messageboxes) 
           ;MsgBox, , , No '%missStartedCheck_string%' found yet - still checking (%missStartedCheck% times), % messagebox_display_time_seconds/5
           logMsgBox("No '" . missStartedCheck_string . "' found yet - still checking (" . missStartedCheck . " times)", messagebox_display_time_seconds/5)
           logMsgBox("Clipboard contents: `r`n" . C, messagebox_display_time_seconds/15)
        Gosub, CheckMissionStartedSub
     }
   }
 
  
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
  OldClipboard := ClipboardAll
  Clipboard:=""
  If (WinExist("ahk_exe " . launcher_ahk_exe)) {
       ;WinActivate, % "ahk_exe " . launcher_ahk_exe
       WinActivateCheckConsoleActive("ahk_exe " . launcher_ahk_exe)
       SetKeyDelay 20,20
       ControlSend, ahk_parent, ^a^a{enter}, % "ahk_exe " . launcher_ahk_exe
       ClipWait, 1
   }
   
   ; if the Launcher.exe console text is exactly the same as last time AND
   ; it's a non-empty string then we increment the 'no change' counter
   if (Clipboard = CheckNoProgress_clipboard && StrLen(Clipboard) > 0) 
     CheckNoProgress_count++
   else CheckNoProgress_count := 0
    
  gotoNextMission := false  
  If (StrLen(setnextMissionStartString)>0 && StrLen(setnextMissionEndString)>0 && InStr(Clipboard, setnextMissionStartString)) {
    nextMiss := StringBetween( ClipBoard, setnextMissionStartString, setnextMissionEndString ) 
    if (nextMiss is integer && nextMiss <= ArraySize(mission_list) && nextMiss >= 1  && nextMiss != nextMissionNumber) {
        nextMissionNumber := nextMiss      
        logMsgBox("Next Mission set to: " . nextMiss . " (" . mission_list[nextMiss] . "). Will take effect on next restart", messagebox_display_time_seconds/2)
    }
    
  }
    
     
   CheckNoProgress_clipboard := Clipboard
   Clipboard := OldClipboard
   OldClipboard =
   if (gotoNextMission){
    
   }
   
    If (CheckNoProgress_count + 1 = CheckNoProgress_limit) { 
             SendRemoteDesktopSessionToConsole()
    }
    
   if (CheckNoProgress_count >= CheckNoProgress_limit) {
        SendRemoteDesktopSessionToConsole()
        if (display_messageboxes)
           ;MsgBox , , , % "Launcher console frozen (checked " . CheckNoProgress_count . " times) - restarting" , % messagebox_display_time_seconds/2
           logMsgBox("Launcher console frozen (checked " . CheckNoProgress_count . " times) - restarting", messagebox_display_time_seconds/2)
        CheckNoProgress_count := 0
        If (WinExist("ahk_exe " . launcher_ahk_exe)) { 
               ;WinActivate, % "ahk_exe " . launcher_ahk_exe
               WinActivateCheckConsoleActive("ahk_exe " . launcher_ahk_exe)
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
  
  ;someone has selected a nextMission by number so we'll use that  
  if ( nextMissionNumber is integer && (nextMissionNumber <= ArraySize(mission_list1) && nextMissionNumber >= 1)) {
        mission := mission_list1[nextMissionNumber]
        nextMissionNumber =
  } else {
    
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
    
logMsgBox(text, time)
  {
    SysGet, SessionRS, 4096 ; SM_REMOTESESSION
    SysGet, SessionRC, 8193 ; SM_REMOTECONTROL
    text := text . " (" . SessionRS . " " . SessionRC . ")"
    MsgBox,,Auto Start Launcher64,% text,% time
    if (StrLen(logfile_fullpath)>0) {
      FormatTime, CurrentDateTime,, % "yyyy/MM/dd HH:mm:ss"

      FileAppend,% CurrentDateTime . ": " . text . "`r`n",% logfile_fullpath
      FileGetSize, FileSize, % logfile_fullpath
      if (FileSize > 100000)
        FileMove, % logfile_fullpath, % logfile_fullpath . "-old"        
    }
  }

  ;return the string between two other strings, using the LAST occurence of the NeedleStart in the haystack String
  StringBetween( String, NeedleStart, NeedleEnd="" ) {
    StringGetPos, pos, String, % NeedleStart, R
    If ( ErrorLevel )
         Return ""
    StringTrimLeft, String, String, pos + StrLen( NeedleStart )
    If ( NeedleEnd = "" )
        Return String
    StringGetPos, pos, String, % NeedleEnd
    If ( ErrorLevel )
        Return ""
    StringLeft, String, String, pos
    Return String
}

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;
; SendRemoteDesktopSessionToConsole() 
;
; So when Remote Desktop disconnects or is minimized, it disconnects the console. That means the Windows runs in a sort 
; of "no window/no console" state and a lot of AutoHotKey commands stop working.  If this happens we can run the
; following function which reconnets terminal IDs 0 thru 9 to the console.  Note that this will forcefully
; disconnect any existing RDP connections. 
;
;However, runnin tscon requires a UAC prompt. This is a good solution except that running tscon.exe typically requires a UAC click. Which is not going to go well in ahk, since it can't make that click.

;You could disable all UAC prompts for your system but below is a different method.
;#1. You create a batch file to run the tscon for the various needed IDs, as you outline above.
;#2. You make a shortcut that uses windows scheduler to run that specific batch file without needing UAC. You can use UAC pass freeware program to do this easily; other possibilities outlined here https://www.techgainer.com/...
;#3. We call schtasks.exe with that scheduled item as below.
;#4. AARRRHGGGGHHHH!!!!
;Your account will (probably?) need administrator level permissions to do this successfully.
;
; RDP users can stop this behavior from happening when their RDP client is minimized by this procedure:
; 1. Locate any of the following Registry keys:

; HKEY_CURRENT_USER/Software/Microsoft/Terminal Server Client(if you want to change the RDC settings for your user account)
; 2. Create a new DWORD value in this key named RemoteDesktop_SuppressWhenMinimized. Specify 2 as the value data.
;
SendRemoteDesktopSessionToConsole() {
  logMsgBox("Closing remote desktop sessions", messagebox_display_time_seconds/5)
  ;Loop 10
  ; Run % "C:\Windows\System32\tscon.exe " . (A_Index-1) . " /dest:console",,HIDE ; this doesn't bec. UAC
   
  ;Run C:\Windows\System32\tscon.exe 0 /dest:console,,HIDE ; also doesn't work because UAC
  Run, C:\Windows\System32\schtasks.exe /run /tn "UAC pass\re-logon-console",,HIDE UseErrorLevel

}

WinActivateCheckConsoleActive(winstring) {
       Winset, Top,, % winstring
       WinActivate, % winstring
       WinID := WinActive(winstring)
       if (! WinID) { ; no window, indicating windows is running in the "gui-less" mode because RDP has been minimized or disconnected
         logMsgBox("No window for " . winstring . " - currently in gui-less windows, reverting RDP session to console", 5)
         SendRemoteDesktopSessionToConsole()
         WinActivate, winstring         
       }  
       else {
         ;logMsgBox("Window OK for " . winstring . " - no problem with gui-less windows", 5)
       }  

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
