Set-StrictMode -Version Latest

function Show-AppHeader {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$CurrentBranch,
        [string]$Breadcrumb
    )

    Write-Host ''
    Write-CardHeader -Title 'Project Ops Utility' -Tone neutral -Width 68
    $projectPathText = if ([string]::IsNullOrWhiteSpace($Config.ProjectPath)) { 'not set' } else { $Config.ProjectPath }
    $pathCompatibilityMessage = Get-ProjectPathCompatibilityMessage -Path $Config.ProjectPath
    $pathHasSpaces = Test-PathContainsSpaces -Path $Config.ProjectPath

    Write-InfoPair -Label 'Project' -Value $projectPathText -LabelColor DarkGray -ValueColor Gray
    Write-InfoPair -Label 'Remote' -Value $(if ([string]::IsNullOrWhiteSpace($Config.GitRemoteUrl)) { 'not set' } else { $Config.GitRemoteUrl }) -LabelColor DarkGray -ValueColor Gray
    Write-InfoPair -Label 'Branch' -Value $(if ([string]::IsNullOrWhiteSpace($CurrentBranch)) { 'unknown' } else { $CurrentBranch }) -LabelColor DarkGray -ValueColor Gray
    if ($pathCompatibilityMessage) {
        Write-InfoPair -Label 'Path' -Value ('WARN - {0}' -f $pathCompatibilityMessage) -LabelColor DarkGray -ValueColor Yellow
    }
    elseif ($pathHasSpaces) {
        Write-InfoPair -Label 'Path' -Value 'INFO - путь содержит пробелы, используется безопасное открытие.' -LabelColor DarkGray -ValueColor DarkYellow
    }
    else {
        Write-InfoPair -Label 'Path' -Value 'OK' -LabelColor DarkGray -ValueColor Green
    }
    if (-not [string]::IsNullOrWhiteSpace($Breadcrumb)) {
        Write-InfoPair -Label 'Route' -Value $Breadcrumb -LabelColor DarkGray -ValueColor Cyan
    }
    Write-Host ('+' + ('-' * 66) + '+') -ForegroundColor DarkCyan
}

function Show-MainMenuOptions {
    foreach ($line in (Get-MainMenuLines)) {
        Write-Host $line
    }
}

function Show-SettingsMenu {
    foreach ($line in (Get-SettingsMenuLines)) {
        Write-Host $line
    }
}

function Show-GitMenu {
    foreach ($line in (Get-GitMenuLines)) {
        Write-Host $line
    }
}

function Show-ConflictMenu {
    foreach ($line in (Get-ConflictMenuLines)) {
        Write-Host $line
    }
}

function Show-CodexMenu {
    foreach ($line in (Get-CodexMenuLines)) {
        Write-Host $line
    }
}

function Show-MapMenu {
    foreach ($line in (Get-MapMenuLines)) {
        Write-Host $line
    }
}

function Get-MainMenuLines {
    return @(
        '1. Настройки'
        '2. Работа с Git'
        '3. Работа с Codex'
        '4. Файловая карта проекта'
        '5. Диагностика среды'
        '6. Собрать backup утилиты'
        '0. Выход'
    )
}

function Get-SettingsMenuLines {
    return @(
        '1. Настроить Git-адрес репозитория'
        '2. Указать директорию проекта'
        '3. Указать ветку по умолчанию'
        '4. Указать стратегию конфликтов по умолчанию'
        '5. Указать путь/команду Codex CLI'
        '6. Проверить и показать текущую конфигурацию'
        '7. Скан и автоконфигурация'
        '8. Автоустановка среды (Chocolatey)'
        '9. Сбросить конфиг к безопасным значениям'
        '0. Назад'
    )
}

function Get-GitMenuLines {
    return @(
        '1. Анализ состояния проекта и рекомендации'
        '2. Push'
        '3. Pull'
        '4. Сырой git status'
        '5. Commit'
        '6. Авторазрешение конфликтов'
        '7. Log'
        '8. Проверить remotes / branch'
        '9. Создать backup branch'
        '10. Инициация Git-репозитория'
        '0. Назад'
    )
}

function Get-ConflictMenuLines {
    return @(
        '1. Принять локальные версии (ours)'
        '2. Принять удалённые версии (theirs)'
        '3. Показать список конфликтных файлов'
        '4. Создать backup branch перед разрешением'
        '0. Назад'
    )
}

function Get-CodexMenuLines {
    return @(
        '1. Открыть папку проекта'
        '2. Запустить PowerShell в папке проекта'
        '3. Запустить Codex CLI'
        '4. Проверить требования среды для Codex'
        '5. Открыть конфиг Codex, если найден'
        '0. Назад'
    )
}

function Get-MapMenuLines {
    return @(
        '1. Создать TREE.txt'
        '2. Создать FILEMAP.md'
        '3. Создать PROJECT_INFO.md'
        '4. Создать ASSISTANT_GUIDE.md'
        '5. Создать TODO_PROJECT.md'
        '6. Создать всё сразу'
        '0. Назад'
    )
}

Export-ModuleMember -Function *-*

