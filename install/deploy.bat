@echo off
REM install/deploy.bat
REM GntTools 배포 스크립트

set TARGET=D:\CadSupport\lisp\net
set SOURCE=..\src\GntTools.UI\bin\Release

echo === GntTools 배포 ===
echo 대상 폴더: %TARGET%

if not exist %TARGET% mkdir %TARGET%

echo DLL 복사 중...
copy /Y %SOURCE%\GntTools.Core.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Wtl.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Swl.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Kepco.dll %TARGET%\
copy /Y %SOURCE%\GntTools.UI.dll %TARGET%\

echo.
echo === 배포 완료 ===
echo.
echo 자동 로딩 설정:
echo   1. install.reg를 관리자 권한으로 실행
echo   2. 또는 AutoCAD에서 NETLOAD → %TARGET%\GntTools.UI.dll
echo.
pause
