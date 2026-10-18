param(
    [ValidateSet('Both', 'Single', 'Client')]
    [string]$Mode = 'Both',
    [ValidatePattern('^[0-9A-Fa-f]{4}:[0-9A-Fa-f]{4}$')][string]$HostController = '054c:09cc',
    [ValidatePattern('^[0-9A-Fa-f]{4}:[0-9A-Fa-f]{4}$')][string]$ClientController = '054c:05c4',
    [ValidateSet('Keep', 'Stretch')][string]$Aspect = 'Keep',
    [ValidateSet('TopBottom', 'SideBySide')][string]$Layout = 'TopBottom',
    [switch]$Borderless
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SplitscreenFix.Common.ps1')
$config = Get-SplitFixConfig
$sourceRoot = [string]$config.WorkDir
$exe = $SplitFixExe
$dataRoot = [string]$config.DataRoot
$disc = [string]$config.IsoPath
$port = [int]$config.ValidatedPort
$bindRoot = Join-Path $env:LOCALAPPDATA 'SuperMarioEclipse\pad-bindings'
$logRoot = Join-Path $dataRoot ('logs-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

function Fail([string]$message) {
    Write-Host "ERRO: $message" -ForegroundColor Red
    Write-Host "Pasta de logs: $logRoot" -ForegroundColor Yellow
    exit 1
}

function Check-Startup($process, [string]$role) {
    Start-Sleep -Seconds 10
    $process.Refresh()
    if (-not $process.HasExited) {
        Write-Host "$role continua aberto (PID $($process.Id))." -ForegroundColor Green
        return $true
    }

    Write-Host "$role FECHOU. ExitCode: $($process.ExitCode)" -ForegroundColor Red
    foreach ($suffix in @('stderr', 'stdout')) {
        $file = Join-Path $logRoot "$role.$suffix.log"
        if ((Test-Path -LiteralPath $file) -and (Get-Item -LiteralPath $file).Length -gt 0) {
            Write-Host "--- $role / $suffix (ultimas 70 linhas) ---" -ForegroundColor Yellow
            Get-Content -LiteralPath $file -Tail 70
        }
    }
    Write-Host "Logs completos em $logRoot" -ForegroundColor Yellow
    return $false
}

function Wait-HostReady($process) {
    $file = Join-Path $logRoot 'Host.stderr.log'
    Write-Host 'Aguardando Mario entrar em uma fase antes de iniciar Luigi...' -ForegroundColor Yellow
    for ($i = 0; $i -lt 900; $i++) {
        $process.Refresh()
        if ($process.HasExited) { Fail "Host fechou antes de entrar na fase (ExitCode $($process.ExitCode))." }
        if ((Test-Path -LiteralPath $file) -and
            (Select-String -LiteralPath $file -Pattern '\[multiplayer\] session [0-9]+: area ' -Quiet)) {
            Write-Host 'Host dentro da fase; iniciando Luigi.' -ForegroundColor Green
            return
        }
        Start-Sleep -Seconds 2
    }
    Fail 'Tempo de espera esgotado. Entre em uma fase com Mario e execute novamente com -Mode Client.'
}

function Check-Connection {
    $file = Join-Path $logRoot 'Client.stderr.log'
    for ($i = 0; $i -lt 30; $i++) {
        if (Test-Path -LiteralPath $file) {
            if (Select-String -LiteralPath $file -Pattern ("Connected to 127\.0\.0\.1:$port as player 2") -Quiet) {
                Write-Host 'Conexao confirmada: Luigi entrou como jogador 2.' -ForegroundColor Green
                return
            }
            if (Select-String -LiteralPath $file -Pattern 'the host refused:|Connection failed|Failed to connect' -Quiet) {
                Fail "Client nao conectou. Veja $file"
            }
        }
        Start-Sleep -Seconds 1
    }
    Fail "Client abriu, mas nao confirmou conexao em 30 segundos. Veja $file"
}

Set-SplitFixRuntimePath
Write-Host "ISO Eclipse: $disc" -ForegroundColor Cyan
Write-Host "Logs: $logRoot" -ForegroundColor Cyan

$env:SMS_DISC_IMAGE = $disc
$env:SMS_FULLSCREEN = '0'
$env:SMS_BACKGROUND_INPUT = '1'
$env:SMS_MULTIPLAYER_PORT = [string]$port
$env:SMS_MULTIPLAYER_MAX_PLAYERS = '2'
$env:SMS_GX_GL_THREAD = '0'  # modo conservador para depurar inicializacao grafica
$env:SMS_ASPECT = $Aspect.ToLowerInvariant()

Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System.Runtime.InteropServices;
public static class EclipseMetrics {
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
}
'@
$screenArea = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
if ($Layout -eq 'TopBottom') {
    # Each window's content is the desktop's half-height minus its title/frame.
    # The port expands the camera for this aspect, without stretching pixels.
    $borderX = 0
    $decoration = 0
    if (-not $Borderless) {
        $paddedBorder = [EclipseMetrics]::GetSystemMetrics(92) # SM_CXPADDEDBORDER
        $borderX = 2 * ([EclipseMetrics]::GetSystemMetrics(32) + $paddedBorder)
        $decoration = [EclipseMetrics]::GetSystemMetrics(4) +
            2 * ([EclipseMetrics]::GetSystemMetrics(33) + $paddedBorder)
    }
    $pictureHeight = [math]::Max(100, [math]::Floor($screenArea.Height / 2) - $decoration)
    $wideAspect = [math]::Min(4.0, ($screenArea.Width - $borderX) / $pictureHeight)
    $env:SMS_WIDESCREEN = $wideAspect.ToString('0.000', [System.Globalization.CultureInfo]::InvariantCulture)
    $env:SMS_WIDESCREEN_HUD = 'edges'
    Write-Host "Camera ultralarga: $env:SMS_WIDESCREEN`:1; imagem: $Aspect" -ForegroundColor Cyan
} else {
    $env:SMS_WIDESCREEN = '16:9'
}
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class EclipseWindowTools {
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool SetWindowText(IntPtr hWnd, string text);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] public static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongW")] public static extern int SetWindowLong(IntPtr hWnd, int index, int value);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int w, int h, uint flags);
}
'@
function Set-Borderless($process) {
    if (-not $Borderless) { return }
    $h = $process.MainWindowHandle
    $style = [EclipseWindowTools]::GetWindowLong($h, -16) # GWL_STYLE
    $withoutFrame = $style -band (-bnot 0x00C40000) # WS_CAPTION | WS_THICKFRAME
    [EclipseWindowTools]::SetWindowLong($h, -16, $withoutFrame) | Out-Null
    [EclipseWindowTools]::SetWindowPos($h, [IntPtr]::Zero, 0, 0, 0, 0, 0x27) | Out-Null # FRAMECHANGED, no move/size/z
}
function Set-WindowRole($process, [string]$role) {
    for ($i = 0; $i -lt 20; $i++) {
        $process.Refresh()
        if ($process.HasExited) { return }
        if ($process.MainWindowHandle -ne 0) {
            [EclipseWindowTools]::SetWindowText($process.MainWindowHandle, $role) | Out-Null
            return
        }
        Start-Sleep -Milliseconds 500
    }
}
function Position-RoleWindow($process, [string]$role) {
    $area = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    if ($Layout -eq 'TopBottom') {
        $w = $area.Width; $h = [int][math]::Floor($area.Height / 2)
        $x = $area.X; $y = $area.Y + $(if ($role -eq 'Client') { $h } else { 0 })
    } else {
        $w = [int][math]::Floor($area.Width / 2); $h = $area.Height
        $x = $area.X + $(if ($role -eq 'Client') { $w } else { 0 }); $y = $area.Y
    }
    $process.Refresh()
    if ($process.MainWindowHandle -ne 0) {
        Set-Borderless $process
        [EclipseWindowTools]::MoveWindow($process.MainWindowHandle, $x, $y, $w, $h, $true) | Out-Null
    }
}
function Arrange-Windows($hostProcess, $clientProcess) {
    $area = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    if ($Layout -eq 'TopBottom') {
        $w = $area.Width; $h = [int][math]::Floor($area.Height / 2)
        [EclipseWindowTools]::MoveWindow($hostProcess.MainWindowHandle, $area.X, $area.Y, $w, $h, $true) | Out-Null
        [EclipseWindowTools]::MoveWindow($clientProcess.MainWindowHandle, $area.X, ($area.Y + $h), $w, $h, $true) | Out-Null
    } else {
        $w = [int][math]::Floor($area.Width / 2); $h = $area.Height
        [EclipseWindowTools]::MoveWindow($hostProcess.MainWindowHandle, $area.X, $area.Y, $w, $h, $true) | Out-Null
        [EclipseWindowTools]::MoveWindow($clientProcess.MainWindowHandle, ($area.X + $w), $area.Y, $w, $h, $true) | Out-Null
    }
    Write-Host "Janelas organizadas: $Layout; imagem: $Aspect" -ForegroundColor Green
}

function Start-SMS([string]$role, [string]$multiplayerMode, [string]$playerName, [string]$controller) {
    New-Item -ItemType Directory -Force -Path $bindRoot | Out-Null
    $bindFile = Join-Path $bindRoot ($role.ToLowerInvariant() + '.txt')
    [System.IO.File]::WriteAllText($bindFile, $controller.ToLowerInvariant() + "`n", [System.Text.Encoding]::ASCII)
    Remove-Item -LiteralPath ($bindFile + '.ack') -ErrorAction SilentlyContinue
    $env:SMS_PAD_BIND_PATH = $bindFile
    Remove-Item Env:SDL_GAMECONTROLLER_IGNORE_DEVICES_EXCEPT -ErrorAction SilentlyContinue
    Write-Host "$role controle dinamico: $controller ($bindFile)" -ForegroundColor Cyan
    $saveDir = Join-Path $dataRoot ("save-" + $playerName.ToLowerInvariant())
    New-Item -ItemType Directory -Force -Path $saveDir | Out-Null
    $env:SMS_SAVE_DIR = $saveDir
    $env:SMS_MULTIPLAYER = $multiplayerMode
    $env:SMS_MULTIPLAYER_NAME = $playerName
    if ($multiplayerMode -eq 'join') {
        $env:SMS_MULTIPLAYER_ADDRESS = "127.0.0.1:$port"
    } else {
        Remove-Item Env:SMS_MULTIPLAYER_ADDRESS -ErrorAction SilentlyContinue
    }

    # Trabalha a partir da raiz do projeto, com a ISO passada explicitamente.
    # Redireciona os dois fluxos para diagnosticar fechamentos imediatos.
    $arguments = '"' + $disc + '"'
    $process = Start-Process -FilePath $exe -WorkingDirectory $sourceRoot -ArgumentList $arguments `
        -RedirectStandardOutput (Join-Path $logRoot "$role.stdout.log") `
        -RedirectStandardError (Join-Path $logRoot "$role.stderr.log") -PassThru
    Write-Host "$role iniciado (PID $($process.Id)). Aguardando 10 segundos..." -ForegroundColor Cyan
    if ($role -eq 'Host') { Set-WindowRole $process 'Jogador 1 - Mario (Host)' }
    else { Set-WindowRole $process 'Jogador 2 - Luigi (Client)' }
    Position-RoleWindow $process $role
    return $process
}

try {
    if ($Mode -eq 'Client') {
        $clientProcess = Start-SMS 'Client' 'join' 'Luigi' $ClientController
        if (-not (Check-Startup $clientProcess 'Client')) { exit 3 }
        Check-Connection
        exit 0
    }
    $hostProcess = Start-SMS 'Host' 'host' 'Mario' $HostController
    if (-not (Check-Startup $hostProcess 'Host')) {
        Write-Host 'Cliente nao iniciado: primeiro corrija a falha do HOST.' -ForegroundColor Yellow
        exit 2
    }
    if ($Mode -eq 'Single') {
        Write-Host 'Modo Single: apenas o host foi iniciado para diagnostico.' -ForegroundColor Green
        exit 0
    }

    Wait-HostReady $hostProcess
    $clientProcess = Start-SMS 'Client' 'join' 'Luigi' $ClientController
    if (-not (Check-Startup $clientProcess 'Client')) { exit 3 }
    Check-Connection
    Arrange-Windows $hostProcess $clientProcess
    Write-Host "Logs: $logRoot"
} catch {
    Fail $_.Exception.Message
}


