param(
    [string]$IsoPath,
    [string]$WorkDir,
    [string]$DataRoot,
    [switch]$NoShortcuts,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$runtime = Join-Path $root 'Runtime'

function Show-Result([string]$message, [bool]$error = $false) {
    Write-Host $message -ForegroundColor $(if ($error) { 'Red' } else { 'Green' })
    if (-not $Quiet) {
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.MessageBox]::Show($message, 'SplitscreenFix', 'OK', $(if ($error) { 'Error' } else { 'Information' })) | Out-Null
    }
}

function Select-EclipseIso {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = 'Selecione Super Mario Eclipse v1.1.0.iso (nao a ISO original de Sunshine)'
    $dialog.Filter = 'Imagem ISO (*.iso)|*.iso'
    $dialog.CheckFileExists = $true
    $dialog.Multiselect = $false
    if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { return $null }
    return $dialog.FileName
}

try {
    if (-not [Environment]::Is64BitOperatingSystem) { throw 'Este pacote exige Windows de 64 bits.' }
    foreach ($file in @('SME-SplitscreenLauncher.exe','Iniciar_Eclipse_Multiplayer_Controles.ps1',
                      'Iniciar_Eclipse_Solo.ps1','SplitscreenFix.Common.ps1')) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $file) -PathType Leaf)) {
            throw "Arquivo do pacote ausente: $file. Extraia o ZIP inteiro antes de instalar."
        }
    }
    foreach ($file in @('sms-rebind.exe','SDL2.dll','libgcc_s_seh-1.dll',
                      'libwinpthread-1.dll','libstdc++-6.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $runtime $file) -PathType Leaf)) {
            throw "Arquivo do jogo ausente: Runtime\$file. Extraia o ZIP inteiro."
        }
    }
    if ((Get-Item -LiteralPath (Join-Path $runtime 'sms-rebind.exe')).Length -lt 8MB) {
        throw 'sms-rebind.exe parece incompleto. Extraia o ZIP novamente.'
    }
    $manifestPath = Join-Path $root 'manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw 'manifest.json ausente. Extraia o ZIP inteiro.'
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($item in $manifest.Files) {
        $relative = [string]$item.Path
        if ($relative -match '(^[\\/]|\.\.|:)' -or -not $relative) { throw 'Manifesto de arquivos invalido.' }
        $full = Join-Path $root $relative
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "Arquivo ausente: $relative" }
        $stream = [IO.File]::OpenRead($full)
        try {
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $actual = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '') }
            finally { $sha.Dispose() }
        } finally { $stream.Dispose() }
        if ($actual -ne [string]$item.Sha256) {
            throw "Arquivo danificado ou alterado: $relative. Extraia o ZIP novamente."
        }
    }

    if (-not $IsoPath) {
        $candidates = @(
            (Join-Path $root 'rom\Super Mario Eclipse v1.1.0.iso'),
            (Join-Path $root 'mods\eclipse\Super Mario Eclipse v1.1.0.iso')

        )
        $IsoPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    }
    if (-not $IsoPath -and -not $Quiet) { $IsoPath = Select-EclipseIso }
    if (-not $IsoPath) { throw 'ISO nao selecionada. Use Setup-Install.cmd e escolha Super Mario Eclipse v1.1.0.iso.' }
    $IsoPath = [IO.Path]::GetFullPath($IsoPath)
    if (-not (Test-Path -LiteralPath $IsoPath -PathType Leaf)) { throw "ISO nao encontrada: $IsoPath" }
    $iso = Get-Item -LiteralPath $IsoPath
    if ($iso.Name -notmatch 'Eclipse' -or $iso.Name -notmatch '1[.]1[.]0' -or $iso.Length -lt 1GB) {
        throw 'Selecione a ISO modificada Super Mario Eclipse v1.1.0.iso. A ISO original de Sunshine ou outra versao nao foi validada para este pacote.'
    }

    if (-not $WorkDir) {
        $parent = Split-Path -Parent $IsoPath
        if ((Split-Path -Leaf $parent) -ieq 'eclipse' -and
            (Split-Path -Leaf (Split-Path -Parent $parent)) -ieq 'mods') {
            $WorkDir = Split-Path -Parent (Split-Path -Parent $parent)
        } else { $WorkDir = $root }
    }
    $WorkDir = [IO.Path]::GetFullPath($WorkDir)
    if (-not (Test-Path -LiteralPath $WorkDir -PathType Container)) { throw "Pasta de trabalho nao encontrada: $WorkDir" }

    if (-not $DataRoot) {
        $DataRoot = Join-Path $env:LOCALAPPDATA 'SplitscreenFix\Data'
    }
    $DataRoot = [IO.Path]::GetFullPath($DataRoot)
    New-Item -ItemType Directory -Force -Path $DataRoot | Out-Null
    # Preserve earlier test saves that lived under TEMP, but never overwrite
    # an existing destination save or remove the original.
    $oldData = Join-Path $env:TEMP 'SMS-Eclipse-Multiplayer-Test'
    if ([IO.Path]::GetFullPath($oldData) -ne $DataRoot) {
        foreach ($role in @('mario','luigi')) {
            $oldSave = Join-Path $oldData ("save-" + $role)
            $newSave = Join-Path $DataRoot ("save-" + $role)
            if ((Test-Path -LiteralPath $oldSave -PathType Container) -and
                -not (Test-Path -LiteralPath $newSave)) {
                Copy-Item -LiteralPath $oldSave -Destination $newSave -Recurse
            }
        }
    }

    $config = [ordered]@{
        Format = 1
        IsoPath = $IsoPath
        WorkDir = $WorkDir
        DataRoot = $DataRoot
        Port = 24002
    }
    $configPath = Join-Path $root 'SplitscreenFix.config.json'
    $tempPath = $configPath + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    [IO.File]::WriteAllText($tempPath, ($config | ConvertTo-Json -Depth 3), (New-Object Text.UTF8Encoding($false)))
    try {
        if (Test-Path -LiteralPath $configPath) {
            $backup = $configPath + '.backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,6)
            [IO.File]::Replace($tempPath, $configPath, $backup)
        } else { [IO.File]::Move($tempPath, $configPath) }
    } finally { if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath } }

    if (-not $NoShortcuts) {
        $desktop = [Environment]::GetFolderPath('Desktop')
        $shell = New-Object -ComObject WScript.Shell
        $launcher = $shell.CreateShortcut((Join-Path $desktop 'SplitscreenFix - Mario e Luigi.lnk'))
        $launcher.TargetPath = Join-Path $root 'SME-SplitscreenLauncher.exe'
        $launcher.WorkingDirectory = $root
        $launcher.Description = 'Super Mario Eclipse: multiplayer e troca de controles ao vivo'
        $launcher.Save()
        foreach ($player in 1,2) {
            $solo = $shell.CreateShortcut((Join-Path $desktop "SplitscreenFix - Jogador $player solo.lnk"))
            $solo.TargetPath = Join-Path $PSHOME 'powershell.exe'
            $solo.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $root 'Iniciar_Eclipse_Solo.ps1') + '" -Jogador ' + $player
            $solo.WorkingDirectory = $root
            $solo.Description = "Super Mario Eclipse: Jogador $player solo"
            $solo.Save()
        }
    }
    Show-Result "Instalacao pronta. Abra 'SplitscreenFix - Mario e Luigi' na area de trabalho. ISO: $IsoPath. Saves: $DataRoot."
} catch {
    Show-Result ("Falha na instalacao: " + $_.Exception.Message) $true
    exit 1
}
