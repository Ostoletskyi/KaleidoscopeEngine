$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$utilityRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$moduleRoot = Join-Path $utilityRoot 'modules'

Import-Module (Join-Path $moduleRoot 'Common.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $moduleRoot 'Config.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $moduleRoot 'OperationPreview.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $moduleRoot 'Diagnostics.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $moduleRoot 'Menu.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $moduleRoot 'GitTools.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $moduleRoot 'CodexTools.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $moduleRoot 'MapTools.psm1') -Force -DisableNameChecking

Initialize-ConsoleEncoding
$script:Environment = Initialize-UtilityEnvironment
$script:ConfigPath = Join-Path $utilityRoot 'config\config.json'
$script:Config = Get-Config -ConfigPath $script:ConfigPath
$script:LogFile = Get-LogFilePath -ConfiguredLogPath $script:Config.LogPath
$script:CurrentBreadcrumb = 'Главное меню'
$script:MenuStack = New-Object System.Collections.ArrayList
$script:SessionState = [pscustomobject]@{
    LastRecommendedAction    = $null
    RecommendedExecuted      = $false
    RecommendedActionHistory = New-Object System.Collections.ArrayList
}

Write-Log -Message 'Utility started.' -LogFile $script:LogFile

function Refresh-ConfigState {
    $script:Config = Get-Config -ConfigPath $script:ConfigPath
    $script:LogFile = Get-LogFilePath -ConfiguredLogPath $script:Config.LogPath
}

function Get-HeaderBranch {
    try {
        return Get-CurrentGitBranch -Config $script:Config -LogFile $script:LogFile
    }
    catch {
        return $null
    }
}

function Set-UiContext {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Breadcrumb,
        [Parameter(Mandatory = $true)]
        [object[]]$MenuStack
    )

    $script:CurrentBreadcrumb = $Breadcrumb
    $script:MenuStack = New-Object System.Collections.ArrayList
    foreach ($item in $MenuStack) {
        [void]$script:MenuStack.Add($item)
    }
}

function New-MenuPanel {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Title,
        [Parameter(Mandatory = $true)]
        [string[]]$Lines,
        [string]$AnchorChoice
    )

    return [pscustomobject]@{
        Title        = $Title
        Lines        = $Lines
        AnchorChoice = $AnchorChoice
    }
}

function Get-MenuLineIndex {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Lines,
        [Parameter(Mandatory = $true)]
        [string]$Choice
    )

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match ('^{0}\.' -f [regex]::Escape($Choice))) {
            return $i
        }
    }
    return 0
}

function Draw-PanelBox {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Left,
        [Parameter(Mandatory = $true)]
        [int]$Top,
        [Parameter(Mandatory = $true)]
        [int]$Width,
        [Parameter(Mandatory = $true)]
        [string]$Title,
        [Parameter(Mandatory = $true)]
        [string[]]$Lines,
        [string]$HighlightedChoice,
        [bool]$IsActive = $false
    )

    $innerWidth = $Width - 4
    $topBorder = '+' + ('=' * ($Width - 2)) + '+'
    $dividerBorder = '+' + ('-' * ($Width - 2)) + '+'
    $bottomBorder = $dividerBorder
    $borderColor = if ($IsActive) { 'DarkCyan' } else { 'DarkGray' }
    $titleColor = if ($IsActive) { 'Cyan' } else { 'Gray' }
    $lineColor = if ($IsActive) { 'Gray' } else { 'DarkGray' }
    $fillColor = if ($IsActive) { 'DarkCyan' } else { 'Black' }

    Write-At -Left $Left -Top $Top -Text $topBorder -ForegroundColor $borderColor
    Write-At -Left $Left -Top ($Top + 1) -Text ('| ' + (Get-FittedConsoleText -Text $Title.ToUpperInvariant() -Width $innerWidth) + ' |') -ForegroundColor $titleColor
    Write-At -Left $Left -Top ($Top + 2) -Text $dividerBorder -ForegroundColor $borderColor

    for ($lineIndex = 0; $lineIndex -lt $Lines.Count; $lineIndex++) {
        $line = $Lines[$lineIndex]
        $choice = $null
        if ($line -match '^(\d+)\.') {
            $choice = $matches[1]
        }

        $color = if ($choice -and $choice -eq $HighlightedChoice -and $IsActive) { 'Yellow' } else { $lineColor }
        $rowText = '| ' + (Get-FittedConsoleText -Text $line -Width $innerWidth) + ' |'
        Write-At -Left $Left -Top ($Top + 3 + $lineIndex) -Text $rowText -ForegroundColor $color
    }

    Write-At -Left $Left -Top ($Top + 3 + $Lines.Count) -Text $bottomBorder -ForegroundColor $borderColor
}

function Draw-HorizontalConnector {
    param(
        [Parameter(Mandatory = $true)]
        [int]$FromX,
        [Parameter(Mandatory = $true)]
        [int]$ToX,
        [Parameter(Mandatory = $true)]
        [int]$Y
    )

    if ($ToX -le $FromX) {
        return
    }

    $length = $ToX - $FromX
    if ($length -le 1) {
        return
    }

    $dots = '.' * ($length - 1)
    Write-At -Left $FromX -Top $Y -Text ($dots + '>') -ForegroundColor DarkGray
}

function Render-MenuStack {
    param(
        [int]$StartTop = 8
    )

    if ($script:MenuStack.Count -eq 0) {
        return $StartTop
    }

    if (-not (Test-ConsoleCursorAvailable)) {
        foreach ($panel in $script:MenuStack) {
            Write-CardHeader -Title $panel.Title -Tone $(if ($panel -eq $script:MenuStack[$script:MenuStack.Count - 1]) { 'neutral' } else { 'attention' }) -Width 44
            foreach ($line in $panel.Lines) {
                Write-Host (' {0}' -f $line) -ForegroundColor $(if ($panel -eq $script:MenuStack[$script:MenuStack.Count - 1]) { 'Gray' } else { 'DarkGray' })
            }
            if ($panel -ne $script:MenuStack[$script:MenuStack.Count - 1]) {
                Write-Host '....................>' -ForegroundColor DarkGray
            }
        }
        return ($StartTop + ($script:MenuStack.Count * 4))
    }

    $panelGap = 6
    $baseX = 2
    $baseY = [Math]::Max(0, $StartTop)
    $windowWidth = [Math]::Max(90, [Console]::WindowWidth)
    $availableWidth = $windowWidth - $baseX - 2 - (($script:MenuStack.Count - 1) * $panelGap)
    $panelWidth = [Math]::Floor($availableWidth / $script:MenuStack.Count)
    $panelWidth = [Math]::Max(28, [Math]::Min(44, $panelWidth))
    $maxBottom = $baseY
    $previousPanelBounds = $null

    for ($index = 0; $index -lt $script:MenuStack.Count; $index++) {
        $panel = $script:MenuStack[$index]
        $x = $baseX + ($index * ($panelWidth + $panelGap))
        $y = $baseY

        if ($previousPanelBounds -and $panel.AnchorChoice) {
            $anchorIndex = Get-MenuLineIndex -Lines $previousPanelBounds.Lines -Choice $panel.AnchorChoice
            $anchorRow = $previousPanelBounds.TopY + 3 + $anchorIndex
            $y = [Math]::Max($baseY, $anchorRow - 1)
            Draw-HorizontalConnector -FromX ($previousPanelBounds.TopX + $panelWidth) -ToX ($x - 1) -Y $anchorRow
        }

        $childAnchorChoice = $null
        if ($index -lt ($script:MenuStack.Count - 1)) {
            $childAnchorChoice = $script:MenuStack[$index + 1].AnchorChoice
        }
        $isActivePanel = ($index -eq ($script:MenuStack.Count - 1))
        Draw-PanelBox -Left $x -Top $y -Width $panelWidth -Title $panel.Title -Lines $panel.Lines -HighlightedChoice $childAnchorChoice -IsActive $isActivePanel

        $panelBottom = $y + 3 + $panel.Lines.Count
        if ($panelBottom -gt $maxBottom) {
            $maxBottom = $panelBottom
        }

        $previousPanelBounds = [pscustomobject]@{
            TopX   = $x
            TopY   = $y
            Lines  = $panel.Lines
            Bottom = $panelBottom
        }
    }

    return ($maxBottom + 2)
}

function Render-CurrentScreen {
    param(
        [string]$PanelTitle,
        [scriptblock]$Body
    )

    Clear-Host
    Refresh-ConfigState
    $branch = Get-HeaderBranch
    Show-AppHeader -Config $script:Config -CurrentBranch $branch -Breadcrumb $script:CurrentBreadcrumb
    $menuTop = 8
    if (Test-ConsoleCursorAvailable) {
        $menuTop = [Console]::CursorTop + 1
    }
    $contentTop = Render-MenuStack -StartTop $menuTop
    if (Test-ConsoleCursorAvailable) {
        $safeContentTop = [Math]::Min([Math]::Max(0, $contentTop), [Math]::Max(0, [Console]::BufferHeight - 1))
        [Console]::SetCursorPosition(0, $safeContentTop)
    }
    if ($PanelTitle) {
        Write-CardHeader -Title $PanelTitle -Tone neutral -Width 68
    }
    if ($Body) {
        & $Body
    }
}

function Get-MainMenuStack {
    return @(
        (New-MenuPanel -Title 'Главное меню' -Lines (Get-MainMenuLines) -AnchorChoice '')
    )
}

function Get-SettingsMenuStack {
    param(
        [string]$ParentChoice = '1'
    )
    return @(
        (New-MenuPanel -Title 'Главное меню' -Lines (Get-MainMenuLines) -AnchorChoice '')
        (New-MenuPanel -Title 'Настройки' -Lines (Get-SettingsMenuLines) -AnchorChoice $ParentChoice)
    )
}

function Get-GitMenuStack {
    param(
        [string]$ParentChoice = '2'
    )
    return @(
        (New-MenuPanel -Title 'Главное меню' -Lines (Get-MainMenuLines) -AnchorChoice '')
        (New-MenuPanel -Title 'Работа с Git' -Lines (Get-GitMenuLines) -AnchorChoice $ParentChoice)
    )
}

function Get-ConflictMenuStack {
    param(
        [string]$ParentChoice = '6'
    )
    return @(
        (New-MenuPanel -Title 'Главное меню' -Lines (Get-MainMenuLines) -AnchorChoice '')
        (New-MenuPanel -Title 'Работа с Git' -Lines (Get-GitMenuLines) -AnchorChoice '2')
        (New-MenuPanel -Title 'Авторазрешение конфликтов' -Lines (Get-ConflictMenuLines) -AnchorChoice $ParentChoice)
    )
}

function Get-CodexMenuStack {
    param(
        [string]$ParentChoice = '3'
    )
    return @(
        (New-MenuPanel -Title 'Главное меню' -Lines (Get-MainMenuLines) -AnchorChoice '')
        (New-MenuPanel -Title 'Работа с Codex' -Lines (Get-CodexMenuLines) -AnchorChoice $ParentChoice)
    )
}

function Get-MapMenuStack {
    param(
        [string]$ParentChoice = '4'
    )
    return @(
        (New-MenuPanel -Title 'Главное меню' -Lines (Get-MainMenuLines) -AnchorChoice '')
        (New-MenuPanel -Title 'Файловая карта проекта' -Lines (Get-MapMenuLines) -AnchorChoice $ParentChoice)
    )
}

function Show-OperationResult {
    param(
        [bool]$Success,
        [string]$Message,
        [string]$NextStep,
        [TimeSpan]$Duration = [TimeSpan]::Zero
    )

    Write-Host ''
    Write-CardHeader -Title 'Итог операции' -Tone $(if ($Success) { 'ok' } else { 'critical' }) -Width 68
    if ($Success) {
        Write-StatusLine -Status OK -Message $Message
    }
    else {
        Write-StatusLine -Status CRIT -Message $Message
    }

    if ($NextStep) {
        Write-StatusLine -Status INFO -Message ('Рекомендуется далее: {0}' -f $NextStep)
    }
    if ($Duration -gt [TimeSpan]::Zero) {
        Write-StatusLine -Status INFO -Message ('Таймер операции: {0}' -f (Format-ElapsedTime -Elapsed $Duration))
    }
}

function New-Operation {
    param(
        [string]$Id,
        [string]$Name,
        [string]$Level,
        [string]$Description,
        [string[]]$WhatWillBeDone,
        [string[]]$ExpectedResult,
        [string[]]$Risks,
        [string]$RecommendedStepText,
        [string]$RecommendedActionId,
        [string]$RecommendedActionName
    )

    return [pscustomobject]@{
        Id                    = $Id
        Name                  = $Name
        Level                 = $Level
        Description           = $Description
        WhatWillBeDone        = $WhatWillBeDone
        ExpectedResult        = $ExpectedResult
        Risks                 = $Risks
        RecommendedStepText   = $RecommendedStepText
        RecommendedActionId   = $RecommendedActionId
        RecommendedActionName = $RecommendedActionName
    }
}

function Invoke-ManagedOperation {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Operation,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,
        [scriptblock]$RecommendedAction,
        [switch]$ReanalyzeGitAfterRecommended
    )

    Render-CurrentScreen -PanelTitle 'Предпросмотр операции'
    $choice = Show-OperationPreview -Operation $Operation -SessionState $script:SessionState -Breadcrumb $script:CurrentBreadcrumb
    if ($choice -eq '0') {
        Show-OperationResult -Success $true -Message 'Операция отменена пользователем.' -NextStep 'Вернуться в меню.'
        Pause-Console
        return
    }

    if ($choice -eq '2') {
        if (-not $RecommendedAction) {
            Show-OperationResult -Success $false -Message 'Рекомендуемое действие недоступно.' -NextStep 'Повторить операцию позже.'
            Pause-Console
            return
        }

        Register-RecommendedActionExecution -SessionState $script:SessionState -ActionId $Operation.RecommendedActionId
        Render-CurrentScreen -PanelTitle ('Выполнение: {0}' -f $Operation.RecommendedActionName)
        $recommendedResult = Invoke-ScriptActionSafely -Action $RecommendedAction -ActionName $Operation.RecommendedActionName -LogFile $script:LogFile
        Show-OperationResult -Success $recommendedResult.Success -Message $(if ($recommendedResult.Success) { 'Рекомендуемое действие выполнено.' } else { $recommendedResult.Error }) -NextStep 'Проверить обновлённое состояние.' -Duration $recommendedResult.Duration
        if ($ReanalyzeGitAfterRecommended) {
            try {
                $analysis = Get-GitAnalysis -Config $script:Config -LogFile $script:LogFile
                Show-GitAnalysis -Analysis $analysis
            }
            catch {
                Write-StatusLine -Status WARN -Message ('Повторный анализ не выполнен: {0}' -f $_.Exception.Message)
            }
        }
        Pause-Console
        return
    }

    Reset-RecommendationState -SessionState $script:SessionState
    Render-CurrentScreen -PanelTitle ('Выполнение: {0}' -f $Operation.Name)
    $result = Invoke-ScriptActionSafely -Action $Action -ActionName $Operation.Name -LogFile $script:LogFile
    if ($result.Success) {
        Show-OperationResult -Success $true -Message 'Операция успешно завершена.' -NextStep 'Продолжить работу или выполнить следующий рекомендованный шаг.' -Duration $result.Duration
        if ($null -ne $result.Result) {
            if ($result.Result -is [string]) {
                Write-Host $result.Result
            }
            elseif ($result.Result.PSObject.Properties['StdOut']) {
                if ($result.Result.StdOut) {
                    Write-Host $result.Result.StdOut
                }
                if ($result.Result.StdErr) {
                    Write-Host $result.Result.StdErr -ForegroundColor Yellow
                }
            }
        }
    }
    else {
        Show-OperationResult -Success $false -Message $result.Error -NextStep 'Исправить причину ошибки и повторить.' -Duration $result.Duration
    }
    Pause-Console
}

function Get-CommitMessageFromUser {
    Write-Host ''
    Write-Host '1. Ввести сообщение вручную'
    Write-Host '2. Использовать авто-сообщение'
    $choice = Read-MenuChoice -AllowedChoices @('1', '2', '0')
    if ($choice -eq '0') {
        return $null
    }
    if ($choice -eq '1') {
        return Read-Host 'Введите сообщение commit'
    }
    return (New-GitNeutralAutoCommitMessage -Config $script:Config -Context 'pre-pull-save' -LogFile $script:LogFile)
}

function Invoke-SettingsScanOperation {
    $operation = New-Operation -Id 'config.scan' -Name 'Скан и автоконфигурация' -Level 'CHANGE' -Description 'Автоматически подготавливает базовую конфигурацию утилиты для быстрого старта на основе директории, в которой находится сама утилита.' -WhatWillBeDone @(
        'ProjectPath будет установлен в директорию утилиты.'
        'Будут созданы и проверены каталоги logs и output.'
        'Будут автоматически заполнены DefaultBranch, CodexCommand и LogPath.'
        'Будет выполнена базовая диагностика среды.'
        'Будет отдельно проверено, задан ли GitRemoteUrl.'
    ) -ExpectedResult @(
        'Конфигурация будет готова к первому использованию без ручного ввода второстепенных параметров.'
        'Пользователь сразу увидит, чего ещё не хватает для полноценной работы.'
    ) -Risks @(
        'ProjectPath будет заменён на директорию утилиты.'
        'GitRemoteUrl не будет выдумываться автоматически и останется пустым, если не был задан ранее.'
        'Если путь к утилите содержит кириллицу, операция будет остановлена.'
    ) -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null

    Invoke-ManagedOperation -Operation $operation -Action {
        $detectedProjectPath = $utilityRoot
        Assert-ProjectPathCompatibility -Path $detectedProjectPath

        $environment = Initialize-UtilityEnvironment
        $script:Environment = $environment
        $config = Get-Config -ConfigPath $script:ConfigPath
        $config.ProjectPath = $detectedProjectPath
        $config.LogPath = $environment.LogsPath
        $config.CodexCommand = if ([string]::IsNullOrWhiteSpace($config.CodexCommand)) { 'codex' } else { $config.CodexCommand }
        $config.ConflictStrategy = if ([string]::IsNullOrWhiteSpace($config.ConflictStrategy)) { 'manual' } else { $config.ConflictStrategy }
        $config.ConfirmSafeOperations = $true
        $config.AutoFetchBeforeGitAnalysis = $true

        $defaultBranch = 'main'
        if ((Test-Path -LiteralPath (Join-Path $detectedProjectPath '.git')) -and (Get-Command git -ErrorAction SilentlyContinue)) {
            try {
                $detectedBranch = (& git -C $detectedProjectPath rev-parse --abbrev-ref HEAD) 2>$null
                if (-not [string]::IsNullOrWhiteSpace($detectedBranch)) {
                    $defaultBranch = $detectedBranch.Trim()
                }
            }
            catch {}
        }
        elseif (-not [string]::IsNullOrWhiteSpace($config.DefaultBranch)) {
            $defaultBranch = $config.DefaultBranch
        }
        $config.DefaultBranch = $defaultBranch

        Save-Config -ConfigPath $script:ConfigPath -Config $config
        Refresh-ConfigState

        $diagnostics = Get-EnvironmentDiagnostics -Config $script:Config -OutputPath $script:Environment.OutputPath -LogsPath $script:Environment.LogsPath
        $bootstrapPlan = Get-EnvironmentBootstrapPlan -Config $script:Config

        $lines = New-Object System.Collections.Generic.List[string]
        $lines.Add(("ProjectPath => {0}" -f $script:Config.ProjectPath)) | Out-Null
        $lines.Add(("DefaultBranch => {0}" -f $script:Config.DefaultBranch)) | Out-Null
        $lines.Add(("CodexCommand => {0}" -f $script:Config.CodexCommand)) | Out-Null
        $lines.Add(("LogPath => {0}" -f $script:Config.LogPath)) | Out-Null
        if ([string]::IsNullOrWhiteSpace($script:Config.GitRemoteUrl)) {
            $lines.Add('GitRemoteUrl => not set (нужно указать вручную)') | Out-Null
        }
        else {
            $lines.Add(("GitRemoteUrl => {0}" -f $script:Config.GitRemoteUrl)) | Out-Null
        }
        $lines.Add('') | Out-Null
        $lines.Add('Диагностика:') | Out-Null
        foreach ($item in $diagnostics) {
            $lines.Add(("[{0}] {1}: {2}" -f $item.Status, $item.Name, $item.Detail)) | Out-Null
        }
        if ($bootstrapPlan.RequiresAction) {
            $lines.Add('') | Out-Null
            $lines.Add('Для полной подготовки среды доступен пункт: Автоустановка среды (Chocolatey).') | Out-Null
        }
        if (Test-PathContainsSpaces -Path $script:Config.ProjectPath) {
            $lines.Add('') | Out-Null
            $lines.Add('INFO: путь проекта содержит пробелы. Для поддерживаемых сценариев утилита использует безопасное открытие.') | Out-Null
        }

        return ($lines -join [Environment]::NewLine)
    }
}

function Invoke-EnvironmentAutoInstallOperation {
    $operation = New-Operation -Id 'settings.autoinstall' -Name 'Автоустановка среды через Chocolatey' -Level 'RISK' -Description 'Запускает повышенное окно PowerShell для установки недостающих компонентов среды через Chocolatey.' -WhatWillBeDone @(
        'Будет определено, каких инструментов не хватает: Chocolatey, git, Node.js, npm, Codex CLI.'
        'Если Chocolatey отсутствует, будет запущен его bootstrap-install.'
        'Через Chocolatey будут установлены недостающие пакеты.'
        'При необходимости будет выполнен npm install -g @openai/codex.'
    ) -ExpectedResult @(
        'Среда будет автоматически доведена до рабочего состояния для git/node/codex-сценариев.'
        'После завершения останется перезапустить текущую консоль или утилиту.'
    ) -Risks @(
        'Требуются права администратора и доступ в интернет.'
        'Будет открыто повышенное окно PowerShell.'
        'Установка внешних пакетов меняет состояние системы.'
    ) -RecommendedStepText 'Сначала выполнить Скан и автоконфигурацию, чтобы увидеть текущее состояние среды.' -RecommendedActionId 'settings.scan' -RecommendedActionName 'Scan and autoconfigure'

    Invoke-ManagedOperation -Operation $operation -Action {
        $plan = Get-EnvironmentBootstrapPlan -Config $script:Config
        Start-EnvironmentBootstrapInstaller -Plan $plan -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-SettingsScanOperation
    }
}

function Invoke-ConfigEditOperation {
    param(
        [string]$PropertyName,
        [string]$Prompt,
        [string]$Name,
        [string]$Description
    )

    $operation = New-Operation -Id "config.$PropertyName" -Name $Name -Level 'CHANGE' -Description $Description -WhatWillBeDone @(
        "Будет обновлено значение $PropertyName в config.json."
        'Новое значение начнёт использоваться сразу после сохранения.'
    ) -ExpectedResult @(
        'Конфигурация проекта будет обновлена.'
    ) -Risks @(
        'Некорректное значение может нарушить часть сценариев.'
        'Изменение не проверяет внешнюю доступность ресурсов, кроме локальных путей в явных случаях.'
    ) -RecommendedStepText 'Проверить корректность вводимого значения.' -RecommendedActionId $null -RecommendedActionName $null

    Invoke-ManagedOperation -Operation $operation -Action {
        $value = Read-Host $Prompt
        if ($PropertyName -eq 'ProjectPath' -and -not [string]::IsNullOrWhiteSpace($value) -and -not (Test-Path -LiteralPath $value -PathType Container)) {
            throw "Указанный путь не существует: $value"
        }
        if ($PropertyName -eq 'ProjectPath' -and -not [string]::IsNullOrWhiteSpace($value)) {
            Assert-ProjectPathCompatibility -Path $value
        }
        if ($PropertyName -eq 'ConflictStrategy' -and ($value -notin @('manual', 'ours', 'theirs'))) {
            throw 'Допустимые значения ConflictStrategy: manual, ours, theirs.'
        }
        Update-ConfigValue -ConfigPath $script:ConfigPath -PropertyName $PropertyName -Value $value | Out-Null
        Refresh-ConfigState
        return 'Конфигурация обновлена.'
    }
}

function Show-ConfigOperation {
    $operation = New-Operation -Id 'config.show' -Name 'Показать конфигурацию' -Level 'INFO' -Description 'Показывает текущий JSON-конфиг утилиты в человекочитаемом виде.' -WhatWillBeDone @(
        'Будет загружен config.json.'
        'На экран будет выведено текущее состояние настроек.'
    ) -ExpectedResult @('Можно проверить, что ProjectPath, GitRemoteUrl и остальные значения заданы корректно.') -Risks @('Операция только читает данные.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null

    Invoke-ManagedOperation -Operation $operation -Action {
        Refresh-ConfigState
        Write-Section -Title 'ТЕКУЩАЯ КОНФИГУРАЦИЯ'
        foreach ($line in (Get-ConfigSummaryLines -Config $script:Config)) {
            Write-Host $line
        }
    }
}

function Reset-ConfigOperation {
    $operation = New-Operation -Id 'config.reset' -Name 'Сбросить конфиг' -Level 'RISK' -Description 'Сбрасывает config.json к безопасным значениям по умолчанию.' -WhatWillBeDone @(
        'Текущие пользовательские значения будут перезаписаны.'
        'Будут сохранены безопасные дефолты.'
    ) -ExpectedResult @('Утилита вернётся в предсказуемое состояние.') -Risks @('Потребуется заново указать путь проекта и другие пользовательские параметры.') -RecommendedStepText 'Сначала посмотреть текущую конфигурацию.' -RecommendedActionId 'config.show' -RecommendedActionName 'Show config'

    Invoke-ManagedOperation -Operation $operation -Action {
        Reset-ConfigToSafeDefaults -ConfigPath $script:ConfigPath | Out-Null
        Refresh-ConfigState
        return 'Конфигурация сброшена к безопасным значениям.'
    } -RecommendedAction {
        Show-ConfigOperation
    }
}

function Invoke-GitAnalysisOperation {
    $analysis = Get-GitAnalysis -Config $script:Config -LogFile $script:LogFile
    $operation = New-Operation -Id 'git.analysis' -Name 'Анализ Git-состояния' -Level 'INFO' -Description 'Проводит интерпретированный анализ состояния git-репозитория и выдаёт понятную рекомендацию.' -WhatWillBeDone @(
        'Будет проверено наличие репозитория и при необходимости выполнен fetch.'
        'Будут собраны branch, ahead/behind, состояние файлов и последний commit.'
        'Состояние будет классифицировано по сценарию.'
    ) -ExpectedResult @(
        'Пользователь увидит диагноз без необходимости читать сырой porcelain-вывод.'
        'Будет сформирована рекомендация следующего шага.'
    ) -Risks @(
        'При включённом AutoFetchBeforeGitAnalysis операция обращается к remote.'
    ) -RecommendedStepText 'Нет.' -RecommendedActionId $analysis.RecommendedActionId -RecommendedActionName $analysis.RecommendedActionName

    Invoke-ManagedOperation -Operation $operation -Action {
        $currentAnalysis = Get-GitAnalysis -Config $script:Config -LogFile $script:LogFile
        Show-GitAnalysis -Analysis $currentAnalysis
    } -RecommendedAction {
        Invoke-AppAction -ActionId $analysis.RecommendedActionId
    } -ReanalyzeGitAfterRecommended
}

function Invoke-GitPushOperation {
    $analysis = $null
    try { $analysis = Get-GitAnalysis -Config $script:Config -LogFile $script:LogFile } catch {}
    $recommendedId = if ($analysis) { $analysis.RecommendedActionId } else { 'git.analysis' }
    $recommendedName = if ($analysis -and $analysis.RecommendedActionName) { $analysis.RecommendedActionName } else { 'Analyze git state' }

    $operation = New-Operation -Id 'git.push' -Name 'Git Push' -Level 'CHANGE' -Description 'Отправляет текущую локальную ветку на remote origin.' -WhatWillBeDone @(
        'Будет определена текущая ветка.'
        'Если в репозитории ещё нет commit или есть незакоммиченные изменения, будет выполнен автокоммит.'
        'Будет выполнен push -u origin <branch>.'
    ) -ExpectedResult @('Удалённый репозиторий получит локальные коммиты.') -Risks @(
        'Автокоммит добавит в историю все текущие изменения через git add -A.'
        'Push может быть отклонён, если remote опережает локальную ветку.'
        'Операция меняет удалённое состояние репозитория.'
    ) -RecommendedStepText 'Сначала выполнить анализ Git-состояния.' -RecommendedActionId $recommendedId -RecommendedActionName $recommendedName

    Invoke-ManagedOperation -Operation $operation -Action {
        Invoke-GitPushSafe -Config $script:Config -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-AppAction -ActionId $recommendedId
    } -ReanalyzeGitAfterRecommended
}

function Invoke-GitPullOperation {
    $operation = New-Operation -Id 'git.pull' -Name 'Git Pull' -Level 'RISK' -Description 'Получает изменения из remote только fast-forward сценарием.' -WhatWillBeDone @(
        'Будет выполнена проверка грязного рабочего дерева.'
        'Будет выполнен git pull --ff-only.'
    ) -ExpectedResult @('Локальная ветка обновится без скрытого merge-коммита.') -Risks @(
        'При локальных изменениях операция будет остановлена.'
        'Если fast-forward невозможен, потребуется контролируемый merge или backup branch.'
    ) -RecommendedStepText 'Сначала выполнить commit локальных изменений или анализ Git-состояния.' -RecommendedActionId 'git.analysis' -RecommendedActionName 'Analyze git state'

    Invoke-ManagedOperation -Operation $operation -Action {
        Invoke-GitPullSafe -Config $script:Config -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-GitAnalysisOperation
    } -ReanalyzeGitAfterRecommended
}

function Invoke-GitRawStatusOperation {
    $operation = New-Operation -Id 'git.rawstatus' -Name 'Raw git status' -Level 'INFO' -Description 'Показывает исходный вывод git status без интерпретации.' -WhatWillBeDone @(
        'Будет выполнена команда git status.'
    ) -ExpectedResult @('Можно увидеть сырой статус для дополнительной проверки.') -Risks @('Операция только читает состояние репозитория.') -RecommendedStepText 'Если нужна рекомендация, сначала использовать анализ Git-состояния.' -RecommendedActionId 'git.analysis' -RecommendedActionName 'Analyze git state'

    Invoke-ManagedOperation -Operation $operation -Action {
        Invoke-GitStatusRaw -Config $script:Config -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-GitAnalysisOperation
    } -ReanalyzeGitAfterRecommended
}

function Invoke-GitCommitOperation {
    $operation = New-Operation -Id 'git.commit' -Name 'Git Commit' -Level 'CHANGE' -Description 'Сохраняет текущие изменения через add + commit.' -WhatWillBeDone @(
        'Будут проиндексированы изменения через git add -A.'
        'Будет создан commit с введённым или авто-сообщением.'
    ) -ExpectedResult @('Локальные изменения будут зафиксированы в истории Git.') -Risks @(
        'Commit не отправляет изменения на remote.'
        'Будут включены все локальные изменения, попавшие под add -A.'
    ) -RecommendedStepText 'Сначала оценить состояние через анализ Git.' -RecommendedActionId 'git.analysis' -RecommendedActionName 'Analyze git state'

    Invoke-ManagedOperation -Operation $operation -Action {
        $message = Get-CommitMessageFromUser
        if ($null -eq $message) {
            return 'Commit отменён пользователем.'
        }
        if ([string]::IsNullOrWhiteSpace($message)) {
            throw 'Сообщение commit не может быть пустым.'
        }
        Invoke-GitCommitSafe -Config $script:Config -Message $message -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-GitAnalysisOperation
    } -ReanalyzeGitAfterRecommended
}

function Invoke-GitInitOperation {
    $operation = New-Operation -Id 'git.init' -Name 'Инициация Git-репозитория' -Level 'RISK' -Description 'Инициализирует новый Git-репозиторий в папке проекта и выполняет первичную базовую настройку.' -WhatWillBeDone @(
        'Будет выполнен git init в директории проекта.'
        'Будет выполнен git add . для текущих файлов.'
        'При наличии изменений будет создан первичный служебный commit в едином формате именования.'
        'При наличии DefaultBranch ветка будет переименована.'
        'При наличии GitRemoteUrl будет добавлен remote origin.'
    ) -ExpectedResult @(
        'Проект получит локальный git-репозиторий с первичным commit.'
        'При наличии remote можно будет перейти к первому push.'
    ) -Risks @(
        'Операция меняет состояние папки проекта и создаёт каталог .git.'
        'Если git user.name/user.email не настроены, commit завершится ошибкой и это будет показано явно.'
        'Если репозиторий уже существует, операция будет остановлена.'
    ) -RecommendedStepText 'Сначала проверить ProjectPath, DefaultBranch и GitRemoteUrl в настройках.' -RecommendedActionId 'config.show' -RecommendedActionName 'Show config'

    Invoke-ManagedOperation -Operation $operation -Action {
        Initialize-GitRepositoryBootstrap -Config $script:Config -LogFile $script:LogFile
    } -RecommendedAction {
        Show-ConfigOperation
    }
}

function Invoke-GitConflictMenu {
    Set-UiContext -Breadcrumb 'Главное меню > Работа с Git > Авторазрешение конфликтов' -MenuStack (Get-ConflictMenuStack)
    while ($true) {
        Render-CurrentScreen -PanelTitle 'Выберите действие'
        $choice = Read-MenuChoice -AllowedChoices @('1', '2', '3', '4', '0')
        switch ($choice) {
            '1' {
                $operation = New-Operation -Id 'git.resolve.ours' -Name 'Resolve conflicts with ours' -Level 'RISK' -Description 'Для всех конфликтных файлов будет принята локальная версия.' -WhatWillBeDone @(
                    'Для каждого conflict-файла будет выполнен git checkout --ours.'
                    'Файлы будут добавлены в индекс.'
                    'Будет создан commit разрешения конфликтов.'
                ) -ExpectedResult @('Конфликты будут закрыты локальной версией файлов.') -Risks @(
                    'Изменения из remote для конфликтующих файлов будут потеряны.'
                    'Нужно понимать последствия перед запуском.'
                ) -RecommendedStepText 'Создать backup branch перед разрешением конфликтов.' -RecommendedActionId 'git.backupBranch' -RecommendedActionName 'Create backup branch'

                Invoke-ManagedOperation -Operation $operation -Action {
                    Resolve-GitConflictsByStrategy -Config $script:Config -Strategy 'ours' -LogFile $script:LogFile
                } -RecommendedAction {
                    Invoke-GitBackupBranchOperation
                } -ReanalyzeGitAfterRecommended
            }
            '2' {
                $operation = New-Operation -Id 'git.resolve.theirs' -Name 'Resolve conflicts with theirs' -Level 'RISK' -Description 'Для всех конфликтных файлов будет принята удалённая версия.' -WhatWillBeDone @(
                    'Для каждого conflict-файла будет выполнен git checkout --theirs.'
                    'Файлы будут добавлены в индекс.'
                    'Будет создан commit разрешения конфликтов.'
                ) -ExpectedResult @('Конфликты будут закрыты удалённой версией файлов.') -Risks @(
                    'Локальные изменения в конфликтующих файлах будут потеряны.'
                    'Нужно понимать последствия перед запуском.'
                ) -RecommendedStepText 'Создать backup branch перед разрешением конфликтов.' -RecommendedActionId 'git.backupBranch' -RecommendedActionName 'Create backup branch'

                Invoke-ManagedOperation -Operation $operation -Action {
                    Resolve-GitConflictsByStrategy -Config $script:Config -Strategy 'theirs' -LogFile $script:LogFile
                } -RecommendedAction {
                    Invoke-GitBackupBranchOperation
                } -ReanalyzeGitAfterRecommended
            }
            '3' {
                $operation = New-Operation -Id 'git.conflicts.list' -Name 'Show conflict files' -Level 'INFO' -Description 'Показывает список конфликтующих файлов.' -WhatWillBeDone @('Будет выполнен поиск файлов со статусом unmerged.') -ExpectedResult @('Пользователь увидит, какие файлы требуют решения.') -Risks @('Операция только читает состояние репозитория.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null
                Invoke-ManagedOperation -Operation $operation -Action {
                    $files = @(Get-GitConflictFiles -Config $script:Config -LogFile $script:LogFile)
                    if ($files.Count -eq 0) {
                        return 'Конфликтующие файлы не найдены.'
                    }
                    Write-Host ''
                    foreach ($file in $files) {
                        Write-Host ('- {0}' -f $file)
                    }
                }
            }
            '4' { Invoke-GitBackupBranchOperation }
            '0' {
                Set-UiContext -Breadcrumb 'Главное меню > Работа с Git' -MenuStack (Get-GitMenuStack)
                return
            }
        }
    }
}

function Invoke-GitLogOperation {
    $operation = New-Operation -Id 'git.log' -Name 'Git Log' -Level 'INFO' -Description 'Показывает последние 10 коммитов.' -WhatWillBeDone @(
        'Будет выполнен git log --oneline -10.'
    ) -ExpectedResult @('Можно быстро оценить недавнюю историю коммитов.') -Risks @('Операция только читает историю.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null

    Invoke-ManagedOperation -Operation $operation -Action {
        Get-GitLogPreview -Config $script:Config -LogFile $script:LogFile
    }
}

function Invoke-GitRemotesOperation {
    $operation = New-Operation -Id 'git.remotes' -Name 'Check remotes and branch' -Level 'INFO' -Description 'Показывает текущую ветку и список remotes.' -WhatWillBeDone @(
        'Будут запрошены current branch и git remote -v.'
    ) -ExpectedResult @('Можно проверить upstream и соответствие настроек remote.') -Risks @('Операция только читает состояние git.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null

    Invoke-ManagedOperation -Operation $operation -Action {
        $data = Get-GitRemotesAndBranch -Config $script:Config -LogFile $script:LogFile
        Write-Host ('Branch: {0}' -f $data.Branch)
        Write-Host $data.Remotes
    }
}

function Invoke-GitBackupBranchOperation {
    $operation = New-Operation -Id 'git.backupBranch' -Name 'Create backup branch' -Level 'CHANGE' -Description 'Создаёт backup branch от текущего состояния HEAD.' -WhatWillBeDone @(
        'Будет сформировано имя вида backup/pre-merge-YYYYMMDD-HHmmss.'
        'Будет создана новая ветка без переключения на неё.'
    ) -ExpectedResult @('Появится точка возврата перед рискованной операцией.') -Risks @('Ветка создаётся локально и не отправляется на remote автоматически.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null

    Invoke-ManagedOperation -Operation $operation -Action {
        $name = New-GitBackupBranch -Config $script:Config -LogFile $script:LogFile
        return ('Создана backup branch: {0}' -f $name)
    }
}

function Invoke-CodexOpenFolderOperation {
    $operation = New-Operation -Id 'codex.openfolder' -Name 'Open project folder' -Level 'SAFE' -Description 'Открывает папку проекта в Explorer.' -WhatWillBeDone @(
        'Будет запущен explorer.exe с директорией проекта.'
    ) -ExpectedResult @('Папка проекта откроется в проводнике.') -Risks @('Операция не меняет проект.') -RecommendedStepText 'Убедиться, что ProjectPath настроен.' -RecommendedActionId 'config.ProjectPath' -RecommendedActionName 'Set project path'
    Invoke-ManagedOperation -Operation $operation -Action {
        Open-ProjectFolder -Config $script:Config -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-ConfigEditOperation -PropertyName 'ProjectPath' -Prompt 'Введите путь к проекту' -Name 'Указать директорию проекта' -Description 'Сохраняет директорию проекта.'
    }
}

function Invoke-CodexOpenShellOperation {
    $operation = New-Operation -Id 'codex.openshell' -Name 'Open PowerShell in project' -Level 'SAFE' -Description 'Открывает новое окно PowerShell в директории проекта.' -WhatWillBeDone @('Будет запущен powershell.exe -NoExit в папке проекта.') -ExpectedResult @('Можно выполнять команды вручную в контексте проекта.') -Risks @('Операция не меняет проект сама по себе.') -RecommendedStepText 'Убедиться, что ProjectPath настроен.' -RecommendedActionId 'config.ProjectPath' -RecommendedActionName 'Set project path'
    Invoke-ManagedOperation -Operation $operation -Action {
        Open-ProjectPowerShell -Config $script:Config -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-ConfigEditOperation -PropertyName 'ProjectPath' -Prompt 'Введите путь к проекту' -Name 'Указать директорию проекта' -Description 'Сохраняет директорию проекта.'
    }
}

function Invoke-CodexCliOperation {
    $operation = New-Operation -Id 'codex.cli' -Name 'Start Codex CLI' -Level 'CHANGE' -Description 'Запускает Codex CLI в директории проекта.' -WhatWillBeDone @(
        'Будет проверено наличие команды Codex.'
        'Будет открыто новое окно PowerShell в папке проекта с запуском Codex CLI.'
    ) -ExpectedResult @('Codex CLI откроется в контексте проекта.') -Risks @(
        'Если CodexCommand указан неверно или CLI не установлен, операция завершится ошибкой.'
    ) -RecommendedStepText 'Проверить требования среды для Codex.' -RecommendedActionId 'codex.diagnostics' -RecommendedActionName 'Check Codex requirements'
    Invoke-ManagedOperation -Operation $operation -Action {
        Start-CodexCli -Config $script:Config -LogFile $script:LogFile
    } -RecommendedAction {
        Invoke-CodexDiagnosticsOperation
    }
}

function Invoke-CodexDiagnosticsOperation {
    $operation = New-Operation -Id 'codex.diagnostics' -Name 'Codex environment diagnostics' -Level 'INFO' -Description 'Проверяет наличие git, node, npm, codex и доступность проекта.' -WhatWillBeDone @(
        'Будет выполнена локальная диагностика среды.'
        'Будет выведен понятный статус по каждому пункту.'
    ) -ExpectedResult @('Пользователь увидит, чего не хватает для работы с Codex.') -Risks @('Операция только читает состояние системы.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null
    Invoke-ManagedOperation -Operation $operation -Action {
        $items = Get-CodexDiagnostics -Config $script:Config -OutputPath $script:Environment.OutputPath -LogsPath $script:Environment.LogsPath
        Show-DiagnosticsReport -Diagnostics $items
    }
}

function Invoke-CodexConfigOperation {
    $operation = New-Operation -Id 'codex.config' -Name 'Open Codex config' -Level 'SAFE' -Description 'Открывает найденный config.toml для Codex в Notepad.' -WhatWillBeDone @(
        'Будет выполнен поиск `%USERPROFILE%\.codex\config.toml` и `<ProjectPath>\.codex\config.toml`.'
        'Найденный файл откроется в Notepad.'
    ) -ExpectedResult @('Можно проверить или скорректировать конфиг Codex.') -Risks @('Файл не изменяется автоматически.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null
    Invoke-ManagedOperation -Operation $operation -Action {
        $path = Open-CodexConfigIfFound -Config $script:Config -LogFile $script:LogFile
        return ('Открыт файл: {0}' -f $path)
    }
}

function Invoke-MapOperation {
    param(
        [string]$ActionId
    )

    switch ($ActionId) {
        'map.tree' {
            $operation = New-Operation -Id 'map.tree' -Name 'Generate TREE.txt' -Level 'CHANGE' -Description 'Создаёт текстовое дерево проекта.' -WhatWillBeDone @(
                'Будет выполнен рекурсивный обход папки проекта.'
                'Игнорируемые директории будут пропущены.'
                'Результат будет сохранён в output\TREE.txt.'
            ) -ExpectedResult @('Появится текстовая структура каталога проекта.') -Risks @('Очень большие проекты могут генерироваться заметно дольше.') -RecommendedStepText 'Убедиться, что ProjectPath настроен.' -RecommendedActionId 'config.ProjectPath' -RecommendedActionName 'Set project path'
            Invoke-ManagedOperation -Operation $operation -Action {
                New-TreeFile -Config $script:Config -OutputPath $script:Environment.OutputPath -LogFile $script:LogFile
            } -RecommendedAction {
                Invoke-ConfigEditOperation -PropertyName 'ProjectPath' -Prompt 'Введите путь к проекту' -Name 'Указать директорию проекта' -Description 'Сохраняет директорию проекта.'
            }
        }
        'map.filemap' {
            $operation = New-Operation -Id 'map.filemap' -Name 'Generate FILEMAP.md' -Level 'CHANGE' -Description 'Создаёт читаемую карту файлов проекта.' -WhatWillBeDone @(
                'Будет выполнен рекурсивный обход проекта с учётом IgnoreFolders.'
                'Для файлов будут добавлены базовые подсказки по назначению.'
            ) -ExpectedResult @('В output появится FILEMAP.md.') -Risks @('Подсказки о назначении файлов являются консервативными и не заменяют ручную проверку.') -RecommendedStepText 'Сначала сгенерировать TREE.txt при первом ознакомлении.' -RecommendedActionId 'map.tree' -RecommendedActionName 'Generate TREE.txt'
            Invoke-ManagedOperation -Operation $operation -Action {
                New-FileMapFile -Config $script:Config -OutputPath $script:Environment.OutputPath -LogFile $script:LogFile
            } -RecommendedAction {
                New-TreeFile -Config $script:Config -OutputPath $script:Environment.OutputPath -LogFile $script:LogFile
            }
        }
        'map.projectinfo' {
            $operation = New-Operation -Id 'map.projectinfo' -Name 'Generate PROJECT_INFO.md' -Level 'CHANGE' -Description 'Создаёт служебный снимок по проекту и среде.' -WhatWillBeDone @(
                'Будут собраны путь, дата, ветка, remote и базовые сведения об окружении.'
            ) -ExpectedResult @('В output появится PROJECT_INFO.md.') -Risks @('Информация зависит от доступности git и настроек проекта.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null
            Invoke-ManagedOperation -Operation $operation -Action {
                New-ProjectInfoFile -Config $script:Config -OutputPath $script:Environment.OutputPath -LogFile $script:LogFile
            }
        }
        'map.assistant' {
            $operation = New-Operation -Id 'map.assistant' -Name 'Generate ASSISTANT_GUIDE.md' -Level 'CHANGE' -Description 'Создаёт памятку для AI/Codex по структуре и правилам работы с проектом.' -WhatWillBeDone @(
                'Будет сформирован служебный markdown-файл с инструкциями и точками входа.'
            ) -ExpectedResult @('В output появится ASSISTANT_GUIDE.md.') -Risks @('Файл является отправной точкой и может потребовать ручной адаптации под конкретный проект.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null
            Invoke-ManagedOperation -Operation $operation -Action {
                New-AssistantGuideFile -Config $script:Config -OutputPath $script:Environment.OutputPath -LogFile $script:LogFile
            }
        }
        'map.todo' {
            $operation = New-Operation -Id 'map.todo' -Name 'Generate TODO_PROJECT.md' -Level 'CHANGE' -Description 'Создаёт каркас TODO-файла по проекту.' -WhatWillBeDone @(
                'Будет создан markdown-файл с заготовкой задач.'
            ) -ExpectedResult @('В output появится TODO_PROJECT.md.') -Risks @('Содержимое нужно адаптировать под реальный roadmap.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null
            Invoke-ManagedOperation -Operation $operation -Action {
                New-TodoProjectFile -Config $script:Config -OutputPath $script:Environment.OutputPath -LogFile $script:LogFile
            }
        }
        'map.all' {
            $operation = New-Operation -Id 'map.all' -Name 'Generate all map files' -Level 'CHANGE' -Description 'Генерирует весь набор артефактов карты проекта.' -WhatWillBeDone @(
                'Будут созданы TREE.txt, FILEMAP.md, PROJECT_INFO.md, ASSISTANT_GUIDE.md и TODO_PROJECT.md.'
            ) -ExpectedResult @('Папка output будет заполнена полным комплектом служебных файлов.') -Risks @('На крупных проектах операция выполняется дольше, чем генерация одного файла.') -RecommendedStepText 'Убедиться, что путь проекта указан корректно.' -RecommendedActionId 'config.ProjectPath' -RecommendedActionName 'Set project path'
            Invoke-ManagedOperation -Operation $operation -Action {
                New-AllProjectMapFiles -Config $script:Config -OutputPath $script:Environment.OutputPath -LogFile $script:LogFile
            } -RecommendedAction {
                Invoke-ConfigEditOperation -PropertyName 'ProjectPath' -Prompt 'Введите путь к проекту' -Name 'Указать директорию проекта' -Description 'Сохраняет директорию проекта.'
            }
        }
    }
}

function Invoke-EnvironmentDiagnosticsOperation {
    $operation = New-Operation -Id 'diagnostics.environment' -Name 'Environment diagnostics' -Level 'INFO' -Description 'Показывает статус PowerShell, git, node, npm, codex, путей проекта, output и logs.' -WhatWillBeDone @(
        'Будут локально проверены ключевые требования среды.'
    ) -ExpectedResult @('Можно быстро увидеть, что готово, а что требует настройки.') -Risks @('Операция только читает состояние.') -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null
    Invoke-ManagedOperation -Operation $operation -Action {
        $items = Get-EnvironmentDiagnostics -Config $script:Config -OutputPath $script:Environment.OutputPath -LogsPath $script:Environment.LogsPath
        Show-DiagnosticsReport -Diagnostics $items
    }
}

function Invoke-UtilityBackupBuildOperation {
    $operation = New-Operation -Id 'backup.build' -Name 'Сборка backup утилиты' -Level 'CHANGE' -Description 'Собирает zip-архив самой утилиты в директории BACKUP.' -WhatWillBeDone @(
        'Будет подготовлен архив с основными файлами утилиты: main.ps1, modules, config, docs, output и служебными файлами.'
        'Архив будет сохранён в папке BACKUP с timestamp в имени.'
    ) -ExpectedResult @('Появится новый zip-архив, пригодный для ручного восстановления утилиты.') -Risks @(
        'Архив не включает временные и тяжёлые рабочие директории вроде .git, Library, obj и logs.'
    ) -RecommendedStepText 'Нет.' -RecommendedActionId $null -RecommendedActionName $null

    Invoke-ManagedOperation -Operation $operation -Action {
        $archivePath = New-UtilityBackupArchive -RootPath $script:Environment.RootPath -BackupPath $script:Environment.BackupPath -LogFile $script:LogFile
        return ('Backup собран: {0}' -f $archivePath)
    }
}

function Invoke-AppAction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ActionId
    )

    switch ($ActionId) {
        'git.commit' { Invoke-GitCommitOperation }
        'git.init' { Invoke-GitInitOperation }
        'git.push' { Invoke-GitPushOperation }
        'git.pull' { Invoke-GitPullOperation }
        'git.backupBranch' { Invoke-GitBackupBranchOperation }
        'git.conflicts' { Invoke-GitConflictMenu }
        'git.remotes' { Invoke-GitRemotesOperation }
        'git.analysis' { Invoke-GitAnalysisOperation }
        'backup.build' { Invoke-UtilityBackupBuildOperation }
        'codex.diagnostics' { Invoke-CodexDiagnosticsOperation }
        'config.show' { Show-ConfigOperation }
        'settings.scan' { Invoke-SettingsScanOperation }
        default { throw "Неизвестный ActionId: $ActionId" }
    }
}

function Show-SettingsMenuLoop {
    Set-UiContext -Breadcrumb 'Главное меню > Настройки' -MenuStack (Get-SettingsMenuStack)
    while ($true) {
        Render-CurrentScreen -PanelTitle 'Выберите действие'
        $choice = Read-MenuChoice -AllowedChoices @('1', '2', '3', '4', '5', '6', '7', '8', '9', '0')
        switch ($choice) {
            '1' { Invoke-ConfigEditOperation -PropertyName 'GitRemoteUrl' -Prompt 'Введите Git remote URL' -Name 'Настроить Git-адрес репозитория' -Description 'Сохраняет URL удалённого репозитория в конфиге.' }
            '2' { Invoke-ConfigEditOperation -PropertyName 'ProjectPath' -Prompt 'Введите путь к директории проекта' -Name 'Указать директорию проекта' -Description 'Сохраняет директорию проекта в конфиге.' }
            '3' { Invoke-ConfigEditOperation -PropertyName 'DefaultBranch' -Prompt 'Введите ветку по умолчанию' -Name 'Указать ветку по умолчанию' -Description 'Сохраняет default branch для справочной логики.' }
            '4' { Invoke-ConfigEditOperation -PropertyName 'ConflictStrategy' -Prompt 'Введите ConflictStrategy (manual/ours/theirs)' -Name 'Указать стратегию конфликтов' -Description 'Сохраняет стратегию конфликтов по умолчанию.' }
            '5' { Invoke-ConfigEditOperation -PropertyName 'CodexCommand' -Prompt 'Введите команду Codex CLI' -Name 'Указать путь/команду Codex CLI' -Description 'Сохраняет команду запуска Codex CLI.' }
            '6' { Show-ConfigOperation }
            '7' { Invoke-SettingsScanOperation }
            '8' { Invoke-EnvironmentAutoInstallOperation }
            '9' { Reset-ConfigOperation }
            '0' { return }
        }
    }
}

function Show-GitMenuLoop {
    Set-UiContext -Breadcrumb 'Главное меню > Работа с Git' -MenuStack (Get-GitMenuStack)
    while ($true) {
        Render-CurrentScreen -PanelTitle 'Выберите действие'
        $choice = Read-MenuChoice -AllowedChoices @('1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '0')
        switch ($choice) {
            '1' { Invoke-GitAnalysisOperation }
            '2' { Invoke-GitPushOperation }
            '3' { Invoke-GitPullOperation }
            '4' { Invoke-GitRawStatusOperation }
            '5' { Invoke-GitCommitOperation }
            '6' { Invoke-GitConflictMenu }
            '7' { Invoke-GitLogOperation }
            '8' { Invoke-GitRemotesOperation }
            '9' { Invoke-GitBackupBranchOperation }
            '10' { Invoke-GitInitOperation }
            '0' { return }
        }
    }
}

function Show-CodexMenuLoop {
    Set-UiContext -Breadcrumb 'Главное меню > Работа с Codex' -MenuStack (Get-CodexMenuStack)
    while ($true) {
        Render-CurrentScreen -PanelTitle 'Выберите действие'
        $choice = Read-MenuChoice -AllowedChoices @('1', '2', '3', '4', '5', '0')
        switch ($choice) {
            '1' { Invoke-CodexOpenFolderOperation }
            '2' { Invoke-CodexOpenShellOperation }
            '3' { Invoke-CodexCliOperation }
            '4' { Invoke-CodexDiagnosticsOperation }
            '5' { Invoke-CodexConfigOperation }
            '0' { return }
        }
    }
}

function Show-MapMenuLoop {
    Set-UiContext -Breadcrumb 'Главное меню > Файловая карта проекта' -MenuStack (Get-MapMenuStack)
    while ($true) {
        Render-CurrentScreen -PanelTitle 'Выберите действие'
        $choice = Read-MenuChoice -AllowedChoices @('1', '2', '3', '4', '5', '6', '0')
        switch ($choice) {
            '1' { Invoke-MapOperation -ActionId 'map.tree' }
            '2' { Invoke-MapOperation -ActionId 'map.filemap' }
            '3' { Invoke-MapOperation -ActionId 'map.projectinfo' }
            '4' { Invoke-MapOperation -ActionId 'map.assistant' }
            '5' { Invoke-MapOperation -ActionId 'map.todo' }
            '6' { Invoke-MapOperation -ActionId 'map.all' }
            '0' { return }
        }
    }
}

function Start-Utility {
    Set-UiContext -Breadcrumb 'Главное меню' -MenuStack (Get-MainMenuStack)
    while ($true) {
        Render-CurrentScreen -PanelTitle 'Выберите действие'
        $choice = Read-MenuChoice -AllowedChoices @('1', '2', '3', '4', '5', '6', '0')
        switch ($choice) {
            '1' { Show-SettingsMenuLoop; Set-UiContext -Breadcrumb 'Главное меню' -MenuStack (Get-MainMenuStack) }
            '2' { Show-GitMenuLoop; Set-UiContext -Breadcrumb 'Главное меню' -MenuStack (Get-MainMenuStack) }
            '3' { Show-CodexMenuLoop; Set-UiContext -Breadcrumb 'Главное меню' -MenuStack (Get-MainMenuStack) }
            '4' { Show-MapMenuLoop; Set-UiContext -Breadcrumb 'Главное меню' -MenuStack (Get-MainMenuStack) }
            '5' { Invoke-EnvironmentDiagnosticsOperation }
            '6' { Invoke-UtilityBackupBuildOperation }
            '0' {
                Write-Log -Message 'Utility finished by user.' -LogFile $script:LogFile
                return
            }
        }
    }
}

try {
    Start-Utility
}
catch {
    Write-Log -Message ('Fatal error: {0}' -f $_.Exception.Message) -Level ERROR -LogFile $script:LogFile
    Write-StatusLine -Status FAIL -Message $_.Exception.Message
    Pause-Console
}

