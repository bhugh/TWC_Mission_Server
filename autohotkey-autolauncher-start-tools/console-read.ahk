WinGet, pid, PID, ahk_class ConsoleWindowClass
; AttachConsole accepts a process ID.
if !DllCall("AttachConsole","uint",pid)
{
    MsgBox AttachConsole failed - error %A_LastError%.
    ExitApp
}
; If it succeeded, console functions now operate on the target console window.
; Use CreateFile to retrieve a handle to the active console screen buffer.
hConOut:=DllCall("CreateFile","str","CONOUT$","uint",0xC0000000
                    ,"uint",7,"uint",0,"uint",3,"uint",0,"uint",0)
if hConOut = -1 ; INVALID_HANDLE_VALUE
{
    MsgBox CreateFile failed - error %A_LastError%.
    ExitApp
}
; Allocate memory for a CONSOLE_SCREEN_BUFFER_INFO structure.
VarSetCapacity(info, 24, 0)
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
; Allocate memory to read into.
VarSetCapacity(text, ConWinWidth*ConWinHeight, 0)
; Read text.
if !DllCall("ReadConsoleOutputCharacter","uint",hConOut,"str",text
            ,"uint",ConWinWidth*ConWinHeight,"uint",0,"uint*",numCharsRead)
{
    MsgBox ReadConsoleOutputCharacter failed - error %A_LastError%.
    ExitApp
}
/* Alternate, slower method:
    ; Allocate memory to read into.
    VarSetCapacity(buf, ConWinWidth*ConWinHeight*4, 0)
    ; Read an array of CHAR_INFO structures, containing text and attributes.
    ; Note: &info+10 is the address of a SMALL_RECT containing the coords we
    ; wish to read from. On success, it will receive the actual coords used.
    if !DllCall("ReadConsoleOutput","uint",hConOut,"uint",&buf
                    ,"uint",ConWinWidth|ConWinHeight<<16,"uint",0
                    ,"uint",&info+10)
    {
        MsgBox ReadConsoleOutput failed - error %A_LastError%.
        ExitApp
    }
    ; buf should now contain an array of CHAR_INFO structures.
    ; We must decode this to retrieve readable text.
    VarSetCapacity(text, ConWinWidth*ConWinHeight)
    Loop % ConWinWidth*ConWinHeight
        text .= Chr(NumGet(buf, 4*(A_Index-1), "Char"))
*/
; Optional: insert line breaks every %ConWinWidth% characters.
text := RegExReplace(text, "`a).{" ConWinWidth "}(?=.)", "$0`n")
; Finally, display the text.
MsgBox % text