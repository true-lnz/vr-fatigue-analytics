@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Starting VR Fatigue Analytics (Live Cortex)...
echo Keep this window open while you run the Unity test.
python -m backend.server
echo.
echo Server stopped. Press any key to close.
pause >nul
