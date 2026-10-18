param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(1, 2)]
    [int]$Jogador,
    [switch]$Janela,
    [ValidateSet(0,1,2)][int]$SoloSaveProfile = 0,
    [ValidatePattern('^[0-9A-Fa-f]{4}:[0-9A-Fa-f]{4}$')][string]$Controller
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SplitscreenFix.Common.ps1')
Add-Type -AssemblyName System.Windows.Forms

try {
    $config = Get-SplitFixConfig
    $sourceRoot = [string]$config.WorkDir
    $exe = $SplitFixExe
    $disc = [string]$config.IsoPath
    $dataRoot = [string]$config.DataRoot
    $role = if ($Jogador -eq 1) { 'mario' } else { 'luigi' }
    $bindRole = if ($Jogador -eq 1) { 'host' } else { 'client' }
    $bindDir = Join-Path $env:LOCALAPPDATA 'SuperMarioEclipse\pad-bindings'
    $bindFile = Join-Path $bindDir ($bindRole + '.txt')
    $explicitController = [bool]$Controller
    if (-not $Controller -and (Test-Path -LiteralPath $bindFile)) {
        $remembered = (Get-Content -LiteralPath $bindFile -Raw).Trim()
        if ($remembered -match '^[0-9A-Fa-f]{4}:[0-9A-Fa-f]{4}$') { $Controller = $remembered }
    }
    if (-not $explicitController) { $Controller = Get-SplitFixFirstController -Preferred $Controller }
    if (-not $Controller) { throw 'Nenhum controle SDL foi encontrado. Ligue um controle, abra o launcher para identifica-lo e tente novamente.' }
    $saveDir = Join-Path $dataRoot "save-$role"
    $logDir = Join-Path $dataRoot ("solo-$role-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,6))

    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Jogo nao encontrado: $exe" }
    if (-not (Test-Path -LiteralPath $disc -PathType Leaf)) { throw "ISO Eclipse nao encontrada: $disc" }
    if ((Get-Process sms,sms-rebind -ErrorAction SilentlyContinue).Count -gt 0) {
        throw 'O jogo ja esta aberto. Feche as janelas atuais antes de iniciar um perfil solo para proteger os saves.'
    }

    New-Item -ItemType Directory -Force -Path $saveDir, $logDir | Out-Null
    Set-SplitFixRuntimePath
    $env:SMS_DISC_IMAGE = $disc
    $env:SMS_SAVE_DIR = $saveDir
# _SPLITFIX_SOLO_SAVE_PROFILE_V1_
# Override ONLY the solo save directory. Player 1 controller binding is left unchanged.
if ($SoloSaveProfile -eq 1 -or $SoloSaveProfile -eq 2) {
    $sfDirectory = $PSScriptRoot
    $sfConfig = Join-Path $sfDirectory 'SplitscreenFix.config.json'
    if (-not (Test-Path -LiteralPath $sfConfig -PathType Leaf)) {
        $sfConfig = Join-Path (Split-Path -Parent $sfDirectory) 'SplitscreenFix.config.json'
    }
    if (-not (Test-Path -LiteralPath $sfConfig -PathType Leaf)) {
        throw "SplitscreenFix.config.json nao encontrado; nao vou arriscar misturar os saves."
    }
    $sfSettings = Get-Content -LiteralPath $sfConfig -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not $sfSettings.DataRoot -or -not [IO.Path]::IsPathRooted([string]$sfSettings.DataRoot)) {
        throw 'DataRoot ausente ou invalido no SplitscreenFix.config.json.'
    }
    $sfSaveFolder = if ($SoloSaveProfile -eq 2) { 'save-luigi' } else { 'save-mario' }
    $env:SMS_SAVE_DIR = Join-Path ([string]$sfSettings.DataRoot) $sfSaveFolder
    New-Item -ItemType Directory -Path $env:SMS_SAVE_DIR -Force | Out-Null
    Write-Host ("Solo save profile: {0}; path: {1}" -f $SoloSaveProfile, $env:SMS_SAVE_DIR)
}
    $env:SMS_FULLSCREEN = if ($Janela) { '0' } else { '1' }
    $env:SMS_WIDESCREEN = '16:9'
    $env:SMS_ASPECT = 'keep'
    $env:SMS_BACKGROUND_INPUT = '0'
    $env:SMS_GX_GL_THREAD = '0'
    New-Item -ItemType Directory -Force -Path $bindDir | Out-Null
    [System.IO.File]::WriteAllText($bindFile, $Controller.ToLowerInvariant() + "`n", [System.Text.Encoding]::ASCII)
    Remove-Item -LiteralPath ($bindFile + '.ack') -ErrorAction SilentlyContinue
    $env:SMS_PAD_BIND_PATH = $bindFile
    Remove-Item Env:SDL_GAMECONTROLLER_IGNORE_DEVICES_EXCEPT -ErrorAction SilentlyContinue
    foreach ($name in @('SMS_MULTIPLAYER','SMS_MULTIPLAYER_ADDRESS','SMS_MULTIPLAYER_NAME',
                       'SMS_MULTIPLAYER_PORT','SMS_MULTIPLAYER_MAX_PLAYERS')) {
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }

    $stdout = Join-Path $logDir 'stdout.log'
    $stderr = Join-Path $logDir 'stderr.log'
    $process = Start-Process -FilePath $exe -WorkingDirectory $sourceRoot -ArgumentList ('"' + $disc + '"') `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
    Start-Sleep -Seconds 8
    $process.Refresh()
    if ($process.HasExited) {
        throw "O jogo fechou ao iniciar (codigo $($process.ExitCode)). Veja $stderr"
    }
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoloWindowTitle {
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool SetWindowText(IntPtr h, string title);
}
'@
    $title = if ($Jogador -eq 1) { 'Jogador 1 - Mario (solo)' } else { 'Jogador 2 - Luigi (solo)' }
    if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
        [SoloWindowTitle]::SetWindowText($process.MainWindowHandle, $title) | Out-Null
    }
    Write-Host "Jogador $Jogador iniciado (PID $($process.Id)); controle $Controller; save $saveDir" -ForegroundColor Green
    Write-Host "Logs: $logDir"
} catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Super Mario Eclipse - Solo', 'OK', 'Error') | Out-Null
    exit 1
}
