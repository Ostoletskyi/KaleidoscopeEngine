Set-StrictMode -Version Latest

function Test-ProjectPath {
    param(
        [string]$ProjectPath
    )

    return (-not [string]::IsNullOrWhiteSpace($ProjectPath)) -and (Test-Path -LiteralPath $ProjectPath -PathType Container)
}

function Test-GitRepository {
    param(
        [string]$ProjectPath
    )

    if (-not (Test-ProjectPath -ProjectPath $ProjectPath)) {
        return $false
    }

    return Test-Path -LiteralPath (Join-Path $ProjectPath '.git')
}

function Test-WriteAccess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    try {
        if (-not (Test-Path -LiteralPath $Path)) {
            New-Item -ItemType Directory -Path $Path -Force | Out-Null
        }
        $tempFile = Join-Path $Path ("access_test_{0}.tmp" -f ([guid]::NewGuid().ToString('N')))
        Set-Content -Path $tempFile -Value 'test' -Encoding ASCII
        Remove-Item -Path $tempFile -Force
        return $true
    }
    catch {
        return $false
    }
}

function Test-ChocolateyAvailable {
    return $null -ne (Get-Command choco -ErrorAction SilentlyContinue)
}

function Get-EnvironmentBootstrapPlan {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    $codexCommand = if ([string]::IsNullOrWhiteSpace($Config.CodexCommand)) { 'codex' } else { $Config.CodexCommand }
    $missingPackages = New-Object System.Collections.Generic.List[string]
    $missingSteps = New-Object System.Collections.Generic.List[string]

    $hasChoco = Test-ChocolateyAvailable
    $hasGit = $null -ne (Get-Command git -ErrorAction SilentlyContinue)
    $hasNode = $null -ne (Get-Command node -ErrorAction SilentlyContinue)
    $hasNpm = $null -ne (Get-Command npm -ErrorAction SilentlyContinue)
    $hasCodex = $null -ne (Get-Command $codexCommand -ErrorAction SilentlyContinue)

    if (-not $hasGit) {
        $missingPackages.Add('git') | Out-Null
        $missingSteps.Add('Установить git через Chocolatey.') | Out-Null
    }
    if (-not $hasNode -or -not $hasNpm) {
        $missingPackages.Add('nodejs-lts') | Out-Null
        $missingSteps.Add('Установить Node.js LTS вместе с npm через Chocolatey.') | Out-Null
    }
    if (-not $hasCodex) {
        $missingSteps.Add('После установки Node.js выполнить npm install -g @openai/codex.') | Out-Null
    }

    return [pscustomobject]@{
        HasChocolatey = $hasChoco
        HasGit        = $hasGit
        HasNode       = $hasNode
        HasNpm        = $hasNpm
        HasCodex      = $hasCodex
        MissingPackages = @($missingPackages | Select-Object -Unique)
        MissingSteps    = @($missingSteps)
        RequiresAction  = (-not $hasGit) -or (-not $hasNode) -or (-not $hasNpm) -or (-not $hasCodex)
    }
}

function Get-EnvironmentBootstrapCommand {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Plan
    )

    $segments = New-Object System.Collections.Generic.List[string]
    $segments.Add("[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12") | Out-Null

    if (-not $Plan.HasChocolatey) {
        $segments.Add("Set-ExecutionPolicy Bypass -Scope Process -Force") | Out-Null
        $segments.Add("iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))") | Out-Null
    }

    if ($Plan.MissingPackages.Count -gt 0) {
        $segments.Add(("choco install {0} -y" -f ($Plan.MissingPackages -join ' '))) | Out-Null
    }

    if (-not $Plan.HasCodex) {
        $segments.Add("npm install -g @openai/codex") | Out-Null
    }

    $segments.Add("Write-Host ''") | Out-Null
    $segments.Add("Write-Host 'Bootstrap installation finished. Restart the utility shell if commands are still not visible.' -ForegroundColor Green") | Out-Null

    return ($segments -join '; ')
}

function Start-EnvironmentBootstrapInstaller {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Plan,
        [string]$LogFile
    )

    if (-not $Plan.RequiresAction) {
        return 'Все ключевые компоненты уже доступны. Установка не требуется.'
    }

    $command = Get-EnvironmentBootstrapCommand -Plan $Plan
    Write-Log -Message 'Launching elevated environment bootstrap installer.' -LogFile $LogFile

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList @(
        '-NoExit',
        '-ExecutionPolicy', 'Bypass',
        '-Command', $command
    ) | Out-Null

    return 'Запущено повышенное окно установки среды. После завершения рекомендуется перезапустить утилиту.'
}

function Get-EnvironmentDiagnostics {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [Parameter(Mandatory = $true)]
        [string]$LogsPath
    )

    $projectExists = Test-ProjectPath -ProjectPath $Config.ProjectPath
    $isGitRepo = Test-GitRepository -ProjectPath $Config.ProjectPath
    $codexCommand = if ([string]::IsNullOrWhiteSpace($Config.CodexCommand)) { 'codex' } else { $Config.CodexCommand }
    $pathCompatibility = Get-ProjectPathCompatibilityMessage -Path $Config.ProjectPath
    $hasChoco = Test-ChocolateyAvailable

    return @(
        [pscustomobject]@{ Name = 'PowerShell version'; Status = 'OK'; Detail = $PSVersionTable.PSVersion.ToString() }
        [pscustomobject]@{ Name = 'chocolatey available'; Status = $(if ($hasChoco) { 'OK' } else { 'WARN' }); Detail = $(if ($hasChoco) { 'Chocolatey найден' } else { 'Chocolatey не найден' }) }
        [pscustomobject]@{ Name = 'git available'; Status = $(if (Get-Command git -ErrorAction SilentlyContinue) { 'OK' } else { 'FAIL' }); Detail = $(if (Get-Command git -ErrorAction SilentlyContinue) { 'git найден' } else { 'git не найден в PATH' }) }
        [pscustomobject]@{ Name = 'node available'; Status = $(if (Get-Command node -ErrorAction SilentlyContinue) { 'OK' } else { 'WARN' }); Detail = $(if (Get-Command node -ErrorAction SilentlyContinue) { 'node найден' } else { 'node не найден' }) }
        [pscustomobject]@{ Name = 'npm available'; Status = $(if (Get-Command npm -ErrorAction SilentlyContinue) { 'OK' } else { 'WARN' }); Detail = $(if (Get-Command npm -ErrorAction SilentlyContinue) { 'npm найден' } else { 'npm не найден' }) }
        [pscustomobject]@{ Name = 'codex available'; Status = $(if (Get-Command $codexCommand -ErrorAction SilentlyContinue) { 'OK' } else { 'WARN' }); Detail = $(if (Get-Command $codexCommand -ErrorAction SilentlyContinue) { "$codexCommand найден" } else { "$codexCommand не найден" }) }
        [pscustomobject]@{ Name = 'project path exists'; Status = $(if ($projectExists) { 'OK' } else { 'FAIL' }); Detail = $(if ($projectExists) { $Config.ProjectPath } else { 'Путь проекта не задан или отсутствует' }) }
        [pscustomobject]@{ Name = 'project path compatibility'; Status = $(if ($pathCompatibility) { 'WARN' } else { 'OK' }); Detail = $(if ($pathCompatibility) { $pathCompatibility } else { 'Путь совместим с текущими ограничениями утилиты.' }) }
        [pscustomobject]@{ Name = 'project is git repo'; Status = $(if ($isGitRepo) { 'OK' } else { 'WARN' }); Detail = $(if ($isGitRepo) { 'Обнаружена папка .git' } else { 'git-репозиторий не обнаружен' }) }
        [pscustomobject]@{ Name = 'write access to output'; Status = $(if (Test-WriteAccess -Path $OutputPath) { 'OK' } else { 'FAIL' }); Detail = $OutputPath }
        [pscustomobject]@{ Name = 'write access to logs'; Status = $(if (Test-WriteAccess -Path $LogsPath) { 'OK' } else { 'FAIL' }); Detail = $LogsPath }
    )
}

function Show-DiagnosticsReport {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Diagnostics
    )

    Write-CardHeader -Title 'Диагностика среды' -Tone neutral -Width 68
    Write-Host ' Статусы:' -ForegroundColor DarkGray
    Write-Host '  [OK]   состояние в норме' -ForegroundColor Green
    Write-Host '  [INFO] нейтральная информация' -ForegroundColor Cyan
    Write-Host '  [WARN] обратить внимание' -ForegroundColor Yellow
    Write-Host '  [ATTN] повышенное внимание' -ForegroundColor DarkYellow
    Write-Host '  [FAIL] критичное отклонение' -ForegroundColor Magenta
    Write-Host ''

    foreach ($item in $Diagnostics) {
        $color = Get-StatusColor -Status $item.Status
        Write-Host ("[{0}] " -f $item.Status) -NoNewline -ForegroundColor $color
        Write-Host ('{0}: {1}' -f $item.Name, $item.Detail)
    }
}

Export-ModuleMember -Function *-*

