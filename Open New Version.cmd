@echo off
setlocal
set "GAME=%~dp0RoxxParkingIndia\Builds\Windows-0.6.1\RoxxParkingIndia.exe"
if not exist "%GAME%" (
  echo Version 0.6.1 has not finished building. See RoxxParkingIndia\Logs\Windows.log.
  pause
  exit /b 1
)
start "" "%GAME%" -force-d3d11 -logFile "%~dp0RoxxParkingIndia\Logs\WindowsPlayer-0.6.1.log"
