Set-StrictMode -Version Latest

function Assert-ProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    if ([string]::IsNullOrWhiteSpace($Config.ProjectPath)) {
        throw 'ProjectPath не задан.'
    }
    Assert-ProjectPathCompatibility -Path $Config.ProjectPath
    if (-not (Test-Path -LiteralPath $Config.ProjectPath -PathType Container)) {
        throw "Папка проекта не существует: $($Config.ProjectPath)"
    }
}

function Open-ProjectFolder {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    Assert-ProjectPath -Config $Config
    Write-Log -Message ('Open project folder: {0}' -f $Config.ProjectPath) -LogFile $LogFile
    $escapedPath = '"' + $Config.ProjectPath + '"'
    Start-Process -FilePath 'explorer.exe' -ArgumentList @($escapedPath)
}

function Open-ProjectPowerShell {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    Assert-ProjectPath -Config $Config
    Write-Log -Message ('Open PowerShell in: {0}' -f $Config.ProjectPath) -LogFile $LogFile
    Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoExit', '-NoLogo') -WorkingDirectory $Config.ProjectPath
}

function Start-CodexCli {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    Assert-ProjectPath -Config $Config
    $command = if ([string]::IsNullOrWhiteSpace($Config.CodexCommand)) { 'codex' } else { $Config.CodexCommand }
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Codex CLI не найден по команде '$command'. Проверьте настройку CodexCommand и PATH."
    }

    Write-Log -Message ('Start Codex CLI using command: {0}' -f $command) -LogFile $LogFile
    Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoExit', '-Command', $command) -WorkingDirectory $Config.ProjectPath
}

function Get-CodexDiagnostics {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [Parameter(Mandatory = $true)]
        [string]$LogsPath
    )

    return Get-EnvironmentDiagnostics -Config $Config -OutputPath $OutputPath -LogsPath $LogsPath
}

function Get-CodexConfigPath {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    $candidates = @(
        (Join-Path $env:USERPROFILE '.codex\config.toml')
    )

    if (-not [string]::IsNullOrWhiteSpace($Config.ProjectPath)) {
        $candidates += (Join-Path $Config.ProjectPath '.codex\config.toml')
    }

    foreach ($path in $candidates) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }

    return $null
}

function Open-CodexConfigIfFound {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $path = Get-CodexConfigPath -Config $Config
    if (-not $path) {
        throw 'Файл config.toml для Codex не найден ни в профиле пользователя, ни в проекте.'
    }

    Write-Log -Message ('Open Codex config: {0}' -f $path) -LogFile $LogFile
    Start-Process -FilePath 'notepad.exe' -ArgumentList @($path)
    return $path
}

Export-ModuleMember -Function *-*

