@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1" %*
if errorlevel 1 (
  echo.
  echo A instalacao falhou. Leia a mensagem acima e tente novamente.
  pause
)
