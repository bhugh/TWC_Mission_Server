;;This script ensured that CloD WatchDog is started and running
;;and thate that the "Start Watchdog" button is pressed
;;
;;It checks every 60 seconds.  
;;It will also check whenever you manually press ctrl-alt-shift-J, which is helpful for testing purposes
;;
;;To use it, You'll need to 
;;   1. Install AutoHotKey
;;   2. Update the location of your ATAG_CloDWatchdog.exe in the script below
;;   3. Place this script (or a shortcut to it) in your system startup folder so that it 
;;     auto-starts when windows starts

#NoEnv  ; Recommended for performance and compatibility with future AutoHotkey releases.
; #Warn  ; Enable warnings to assist with detecting common errors.
SendMode Input  ; Recommended for new scripts due to its superior speed and reliability.
SetWorkingDir %A_ScriptDir%  ; Ensures a consistent starting directory.

#Persistent
SetTimer, StartWatchdog, 60000
return

+!^j::
  Gosub, StartWatchdog
Return

StartWatchdog:
   Gosub, RunWatchdog ; make sure Watchdog.exe is started & window open
   sleep, 4000
   SetControlDelay -1
   ControlClick, Start Watchdog, CloD WatchDog,
   
Return

;;Make sure Watchdog is running & maximized (ie, window open & displaying, not minimized to tray)
RunWatchdog:
DetectHiddenWindows, On
Process, Exist, 4.5 ATAG_CloDWatchdog 64.exe
If (!WinExist("CloD WatchDog"))
Run, "C:\Users\twc_server3\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\4.5 CloDWatchdog 64.exe"
Else
DetectHiddenWindows, Off
If (!WinExist("CloD WatchDog"))
{
WinShow, CloD WatchDog
WinActivate, CloD WatchDog
}
Return
