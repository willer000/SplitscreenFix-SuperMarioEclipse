@echo off
setlocal
cd /d "%~dp0"
if not exist "Instalar.cmd" (
  echo Error / Erro: Instalar.cmd not found / nao encontrado.
  pause
  exit /b 1
)
call "%~dp0Instalar.cmd"
exit /b %errorlevel%
