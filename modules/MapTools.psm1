Set-StrictMode -Version Latest

function Assert-MapGenerationReady {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    if ([string]::IsNullOrWhiteSpace($Config.ProjectPath)) {
        throw 'ProjectPath не задан.'
    }
    Assert-ProjectPathCompatibility -Path $Config.ProjectPath
    if (-not (Test-Path -LiteralPath $Config.ProjectPath -PathType Container)) {
        throw "Папка проекта не существует: $($Config.ProjectPath)"
    }
    if (-not (Test-Path -LiteralPath $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }
}

function Get-ProjectItems {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    $ignore = @($Config.IgnoreFolders)
    return Get-ChildItem -LiteralPath $Config.ProjectPath -Force -Recurse -ErrorAction SilentlyContinue | Where-Object {
        $relative = $_.FullName.Substring($Config.ProjectPath.Length).TrimStart('\')
        foreach ($folder in $ignore) {
            if ($relative -eq $folder -or $relative.StartsWith($folder + '\')) {
                return $false
            }
        }
        return $true
    }
}

function Get-RelativeProjectPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,
        [Parameter(Mandatory = $true)]
        [string]$FullPath
    )

    return $FullPath.Substring($BasePath.Length).TrimStart('\')
}

function Get-FilePurposeHint {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$File
    )

    switch -Regex ($File.Extension.ToLowerInvariant()) {
        '\.ps1|\.psm1|\.bat' { return 'Скрипт или точка автоматизации.' }
        '\.json|\.toml|\.yml|\.yaml|\.xml|\.config' { return 'Конфигурационный файл.' }
        '\.md|\.txt' { return 'Документация или служебный текст.' }
        '\.cs|\.js|\.ts|\.tsx|\.jsx|\.java|\.py|\.cpp|\.h' { return 'Исходный код или модуль приложения.' }
        '\.sln|\.csproj|\.vcxproj' { return 'Проектный или сборочный файл.' }
        default { return 'Назначение требует уточнения.' }
    }
}

function New-TreeFile {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [string]$LogFile
    )

    Assert-MapGenerationReady -Config $Config -OutputPath $OutputPath
    $items = Get-ProjectItems -Config $Config | Sort-Object FullName
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add($Config.ProjectPath) | Out-Null

    foreach ($item in $items) {
        $relative = Get-RelativeProjectPath -BasePath $Config.ProjectPath -FullPath $item.FullName
        $depth = ([regex]::Matches($relative, '\\')).Count
        $prefix = ('  ' * ($depth + 1))
        $label = if ($item.PSIsContainer) { '[D] ' + $item.Name } else { '[F] ' + $item.Name }
        $lines.Add($prefix + $label) | Out-Null
    }

    $path = Join-Path $OutputPath 'TREE.txt'
    Set-Content -Path $path -Value $lines -Encoding UTF8
    Write-Log -Message ('Generated {0}' -f $path) -LogFile $LogFile
    return $path
}

function New-FileMapFile {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [string]$LogFile
    )

    Assert-MapGenerationReady -Config $Config -OutputPath $OutputPath
    $items = Get-ProjectItems -Config $Config | Sort-Object FullName
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('# FILEMAP') | Out-Null
    $lines.Add('') | Out-Null
    $lines.Add(('Project root: `{0}`' -f $Config.ProjectPath)) | Out-Null
    $lines.Add(('Generated: {0}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))) | Out-Null
    $lines.Add('') | Out-Null

    foreach ($item in $items) {
        $relative = Get-RelativeProjectPath -BasePath $Config.ProjectPath -FullPath $item.FullName
        if ($item.PSIsContainer) {
            $lines.Add(('## {0}' -f $relative)) | Out-Null
            $lines.Add('- Папка проекта или служебная директория.') | Out-Null
        }
        else {
            $hint = Get-FilePurposeHint -File $item
            $lines.Add(('- `{0}` — {1}' -f $relative, $hint)) | Out-Null
        }
    }

    $path = Join-Path $OutputPath 'FILEMAP.md'
    Set-Content -Path $path -Value $lines -Encoding UTF8
    Write-Log -Message ('Generated {0}' -f $path) -LogFile $LogFile
    return $path
}

function New-ProjectInfoFile {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [string]$LogFile
    )

    Assert-MapGenerationReady -Config $Config -OutputPath $OutputPath
    $branch = ''
    $remote = $Config.GitRemoteUrl
    $gitRepo = Test-Path -LiteralPath (Join-Path $Config.ProjectPath '.git')
    if ($gitRepo -and (Get-Command git -ErrorAction SilentlyContinue)) {
        try { $branch = (& git -C $Config.ProjectPath rev-parse --abbrev-ref HEAD) 2>$null } catch { $branch = '' }
        try { $remote = (& git -C $Config.ProjectPath remote get-url origin) 2>$null } catch { $remote = $Config.GitRemoteUrl }
    }

    $lines = @(
        '# PROJECT INFO'
        ''
        ('- ProjectPath: `{0}`' -f $Config.ProjectPath)
        ('- GeneratedAt: {0}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
        ('- GitRemote: {0}' -f $(if ($remote) { $remote } else { 'not set' }))
        ('- Branch: {0}' -f $(if ($branch) { $branch } else { 'unknown' }))
        ('- PowerShell: {0}' -f $PSVersionTable.PSVersion.ToString())
        ('- GitAvailable: {0}' -f [bool](Get-Command git -ErrorAction SilentlyContinue))
        ('- NodeAvailable: {0}' -f [bool](Get-Command node -ErrorAction SilentlyContinue))
        ('- NpmAvailable: {0}' -f [bool](Get-Command npm -ErrorAction SilentlyContinue))
        ('- CodexCommand: {0}' -f $Config.CodexCommand)
    )

    $path = Join-Path $OutputPath 'PROJECT_INFO.md'
    Set-Content -Path $path -Value $lines -Encoding UTF8
    Write-Log -Message ('Generated {0}' -f $path) -LogFile $LogFile
    return $path
}

function New-AssistantGuideFile {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [string]$LogFile
    )

    Assert-MapGenerationReady -Config $Config -OutputPath $OutputPath
    $utilityRoot = Split-Path -Parent $OutputPath
    $ignoreText = ($Config.IgnoreFolders -join ', ')
    $lines = @(
        '# ASSISTANT GUIDE'
        ''
        '## Цель'
        'Файл-памятка для AI/Codex при работе с проектом.'
        ''
        '## Что читать сначала'
        '- README.md проекта.'
        '- Конфигурационные файлы в корне проекта.'
        '- Входные точки приложения: файлы запуска, основной модуль, package.json/csproj/sln.'
        ''
        '## Где конфиг'
        ('- Utility config: `{0}`' -f (Join-Path $utilityRoot 'config\config.json'))
        '- Codex config: `%USERPROFILE%\.codex\config.toml` или `<ProjectPath>\.codex\config.toml`.'
        ''
        '## Какие папки не трогать'
        ('- {0}' -f $ignoreText)
        '- Любые каталоги сборки, кеша и внешних зависимостей без явной необходимости.'
        ''
        '## Где логи'
        ('- Utility logs: `{0}`' -f (Join-Path $utilityRoot 'logs'))
        ''
        '## Как запускать проект'
        '- Использовать штатные скрипты/команды проекта.'
        '- Для работы внутри проекта можно открыть PowerShell из меню утилиты.'
        ''
        '## Правила работы'
        '- Сначала анализировать состояние репозитория.'
        '- Не выполнять опасные git-действия без backup branch.'
        '- При неясном назначении файла не менять его без дополнительной проверки.'
    )

    $path = Join-Path $OutputPath 'ASSISTANT_GUIDE.md'
    Set-Content -Path $path -Value $lines -Encoding UTF8
    Write-Log -Message ('Generated {0}' -f $path) -LogFile $LogFile
    return $path
}

function New-TodoProjectFile {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [string]$LogFile
    )

    Assert-MapGenerationReady -Config $Config -OutputPath $OutputPath
    $lines = @(
        '# TODO PROJECT'
        ''
        '## High Priority'
        '- [ ] Подтвердить входные точки проекта.'
        '- [ ] Проверить конфигурацию окружения и запуск.'
        '- [ ] Уточнить стратегию ветвления и release flow.'
        ''
        '## Medium Priority'
        '- [ ] Актуализировать FILEMAP.md после крупных изменений.'
        '- [ ] Проверить качество логирования и обработку ошибок в проекте.'
        '- [ ] Уточнить список игнорируемых директорий.'
        ''
        '## Low Priority'
        '- [ ] Добавить проектные соглашения по коммитам.'
        '- [ ] Уточнить onboarding-документацию.'
        '- [ ] Проверить артефакты сборки на избыточность.'
    )

    $path = Join-Path $OutputPath 'TODO_PROJECT.md'
    Set-Content -Path $path -Value $lines -Encoding UTF8
    Write-Log -Message ('Generated {0}' -f $path) -LogFile $LogFile
    return $path
}

function New-AllProjectMapFiles {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [string]$LogFile
    )

    return @(
        (New-TreeFile -Config $Config -OutputPath $OutputPath -LogFile $LogFile)
        (New-FileMapFile -Config $Config -OutputPath $OutputPath -LogFile $LogFile)
        (New-ProjectInfoFile -Config $Config -OutputPath $OutputPath -LogFile $LogFile)
        (New-AssistantGuideFile -Config $Config -OutputPath $OutputPath -LogFile $LogFile)
        (New-TodoProjectFile -Config $Config -OutputPath $OutputPath -LogFile $LogFile)
    )
}

Export-ModuleMember -Function *-*

