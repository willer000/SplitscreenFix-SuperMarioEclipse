# Shared validation for the portable launcher. Installation never needs admin.
$ErrorActionPreference = 'Stop'
$SplitFixRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$SplitFixConfigPath = Join-Path $SplitFixRoot 'SplitscreenFix.config.json'
$SplitFixRuntime = Join-Path $SplitFixRoot 'Runtime'
$SplitFixExe = Join-Path $SplitFixRuntime 'sms-rebind.exe'

function Get-SplitFixConfig {
    if (-not (Test-Path -LiteralPath $SplitFixConfigPath -PathType Leaf)) {
        throw "Configuracao ausente. Execute Instalar.cmd na pasta $SplitFixRoot."
    }
    $config = Get-Content -LiteralPath $SplitFixConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($name in @('IsoPath','WorkDir','DataRoot')) {
        if (-not $config.$name -or -not [IO.Path]::IsPathRooted([string]$config.$name)) {
            throw "Configuracao invalida: $name. Execute Instalar.cmd novamente."
        }
    }
    if (-not (Test-Path -LiteralPath $config.IsoPath -PathType Leaf)) {
        throw "ISO nao encontrada: $($config.IsoPath). Execute Instalar.cmd novamente."
    }
    if (-not (Test-Path -LiteralPath $config.WorkDir -PathType Container)) {
        throw "Pasta de trabalho nao encontrada: $($config.WorkDir). Execute Instalar.cmd novamente."
    }
    if (-not (Test-Path -LiteralPath $SplitFixExe -PathType Leaf)) {
        throw "Jogo atualizado nao encontrado: $SplitFixExe. Extraia o pacote completo."
    }
    foreach ($dll in @('SDL2.dll','libgcc_s_seh-1.dll','libwinpthread-1.dll','libstdc++-6.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $SplitFixRuntime $dll) -PathType Leaf)) {
            throw "DLL ausente: $dll. Extraia o pacote completo."
        }
    }
    $port = 24002
    if ($config.Port) { $port = [int]$config.Port }
    if ($port -lt 1024 -or $port -gt 65535) { throw 'Porta multiplayer invalida no arquivo de configuracao.' }
    $config | Add-Member -NotePropertyName ValidatedPort -NotePropertyValue $port -Force
    return $config
}

function Set-SplitFixRuntimePath {
    # Runtime DLLs are shipped beside sms-rebind.exe; MSYS2 is not required.
    $env:PATH = "$SplitFixRuntime;$env:PATH"
}

function Get-SplitFixFirstController([string]$Preferred) {
    Set-SplitFixRuntimePath
    if (-not ('SplitFixControllerProbe' -as [type])) {
        Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SplitFixControllerProbe {
    [DllImport("SDL2.dll")] public static extern int SDL_Init(uint flags);
    [DllImport("SDL2.dll")] public static extern void SDL_Quit();
    [DllImport("SDL2.dll")] public static extern int SDL_NumJoysticks();
    [DllImport("SDL2.dll")] public static extern int SDL_IsGameController(int index);
    [DllImport("SDL2.dll")] public static extern ushort SDL_JoystickGetDeviceVendor(int index);
    [DllImport("SDL2.dll")] public static extern ushort SDL_JoystickGetDeviceProduct(int index);
}
'@
    }
    if ([SplitFixControllerProbe]::SDL_Init(0x2000) -ne 0) { return $null }
    try {
        $found = @()
        for ($i=0; $i -lt [SplitFixControllerProbe]::SDL_NumJoysticks(); $i++) {
            if ([SplitFixControllerProbe]::SDL_IsGameController($i) -eq 0) { continue }
            $vendorId = [SplitFixControllerProbe]::SDL_JoystickGetDeviceVendor($i)
            $productId = [SplitFixControllerProbe]::SDL_JoystickGetDeviceProduct($i)
            if ($vendorId -ne 0 -and $productId -ne 0) { $found += ('{0:x4}:{1:x4}' -f $vendorId,$productId) }
        }
        if ($Preferred -and $found -contains $Preferred.ToLowerInvariant()) { return $Preferred.ToLowerInvariant() }
        if ($found.Count -gt 0) { return $found[0] }
    } finally { [SplitFixControllerProbe]::SDL_Quit() }
    return $null
}
