@echo off
setlocal
chcp 65001 >nul
set SCRIPT_DIR=%~dp0
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%main.ps1"
if errorlevel 1 (
    echo.
    echo Application exited with an error.
    pause
)
endlocal
