@echo off
chcp 65001 >nul
cd /d "%~dp0"
start "Fatigue Analytics Backend" cmd /k call "%~dp0run-backend.cmd"
timeout /t 3 /nobreak >nul
if not exist "unity\VR Fatigue Analytics\Build\VRFatigueAnalytics_Updated.exe" (
  pause
  exit /b 1
)
start "" "unity\VR Fatigue Analytics\Build\VRFatigueAnalytics_Updated.exe"
