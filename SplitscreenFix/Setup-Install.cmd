@echo off
setlocal
cd /d "%~dp0"
if not exist "Install.cmd" (
  echo Error / Erro: Install.cmd not found / nao encontrado.
  pause
  exit /b 1
)
call "%~dp0Install.cmd" %*
exit /b %errorlevel%
