FOR /F "tokens=*" %%A IN ('CHCP') DO FOR %%B IN (%%~A) DO SET originalcp=%%B
CHCP 1252
uacpass.exe  -ondk -syes "C:\Users\Administrator\Desktop\re-logon-console.bat"
CHCP %originalcp%
