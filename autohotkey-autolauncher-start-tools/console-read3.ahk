
;WinActivateCheckConsoleActive("ahk_exe " . launcher_ahk_exe)
WinActivate, ahk_exe Launcher64.exe        
ClipBoard =
Send !{Space}es{Enter}                    ; select all, copy to clipboard
ClipWait 2
MsgBox, %ClipBoard%
;Send !{Space}c                            ; close console window

WinActivateCheckConsoleActive(winstring) {
       Winset, Top,, % winstring
       WinActivate, % winstring
       WinID := WinActive(winstring)
       if (! WinID) { ; no window, indicating windows is running in the "gui-less" mode because RDP has been minimized or disconnected
         ;MsgBox "No window for " . winstring . " - currently in gui-less windows, reverting RDP session to console"
         ;WinActivate, winstring         
       }  
       else {
         ;logMsgBox("Window OK for " . winstring . " - no problem with gui-less windows", 5)
       }  

}