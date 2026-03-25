@echo off
REM install/deploy.bat
REM GntTools Deploy Script

set TARGET=D:\CadSupport\lisp\net
set SOURCE=..\src\GntTools.UI\bin\Release

echo === GntTools Deploy ===
echo Target: %TARGET%

if not exist %TARGET% mkdir %TARGET%

echo Copying DLLs...
copy /Y %SOURCE%\GntTools.Core.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Wtl.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Swl.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Kepco.dll %TARGET%\
copy /Y %SOURCE%\GntTools.UI.dll %TARGET%\

echo.
echo === Deploy Complete ===
echo.
echo Auto-load setup:
echo   1. Run install.reg as administrator
echo   2. Or use NETLOAD in AutoCAD: %TARGET%\GntTools.UI.dll
echo.
pause
