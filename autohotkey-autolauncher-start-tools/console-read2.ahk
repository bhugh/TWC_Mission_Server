WinGet, pid, PID, ahk_class ConsoleWindowClass
; AttachConsole accepts a process ID.
if !DllCall("AttachConsole","uint",pid)
{
    MsgBox AttachConsole failed - error %A_LastError%.
    ExitApp
}
hConOut:=DllCall("CreateFile","str","CONOUT$","uint",0xC0000000
                    ,"uint",7,"uint",0,"uint",3,"uint",0,"uint",0)
if hConOut = -1 ; INVALID_HANDLE_VALUE
{
    MsgBox CreateFile failed - error %A_LastError%.
    ExitApp
}
; Allocate memory for a CONSOLE_SCREEN_BUFFER_INFO structure.
VarSetCapacity(info, 24, 0)

/*
; Get info about the active console screen buffer.
if !DllCall("GetConsoleScreenBufferInfo","uint",hConOut,"uint",&info)
{
    MsgBox GetConsoleScreenBufferInfo failed - error %A_LastError%.
    ExitApp
}

; Determine which section of the buffer is on display.
ConWinLeft := NumGet(info, 10, "Short")     ; info.srWindow.Left
ConWinTop := NumGet(info, 12, "Short")      ; info.srWindow.Top
ConWinRight := NumGet(info, 14, "Short")    ; info.srWindow.Right
ConWinBottom := NumGet(info, 16, "Short")   ; info.srWindow.Bottom
ConWinWidth := ConWinRight-ConWinLeft+1
ConWinHeight := ConWinBottom-ConWinTop+1

ConWinLeft := 0
ConWinTop := 0
ConWinRight := 10
ConWinBottom := 10
ConWinWidth := 40
ConWinHeight := 20

*/
; Allocate memory to read into.
VarSetCapacity(text, ConWinWidth*ConWinHeight, 0)
; Read text.
if !DllCall("ReadConsoleOutputCharacter","uint",hConOut,"str",text
            ,"uint",ConWinWidth*ConWinHeight,"uint",0,"uint*",numCharsRead)
{
    MsgBox ReadConsoleOutputCharacter failed - error %A_LastError%.
    ExitApp
}
; Finally, display the text.
MsgBox % text