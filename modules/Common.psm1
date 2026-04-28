Set-StrictMode -Version Latest

function Get-UtilityRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Initialize-ConsoleEncoding {
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [Console]::InputEncoding = $utf8
    [Console]::OutputEncoding = $utf8
    $global:OutputEncoding = $utf8
}

function Initialize-UtilityEnvironment {
    $root = Get-UtilityRoot
    $logs = Join-Path $root 'logs'
    $output = Join-Path $root 'output'
    $backup = Join-Path $root 'BACKUP'

    Ensure-Directory -Path $logs
    Ensure-Directory -Path $output
    Ensure-Directory -Path $backup

    return [pscustomobject]@{
        RootPath   = $root
        LogsPath   = $logs
        OutputPath = $output
        BackupPath = $backup
    }
}

function New-UtilityBackupArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath,
        [Parameter(Mandatory = $true)]
        [string]$BackupPath,
        [string]$LogFile
    )

    Ensure-Directory -Path $BackupPath

    $itemsToPack = @(
        'main.ps1',
        'run.bat',
        'README.md',
        '.gitignore',
        '.vsconfig',
        'Afrika - Ave Matanga.sln',
        'config',
        'docs',
        'modules',
        'output'
    )

    $resolvedItems = New-Object System.Collections.Generic.List[string]
    foreach ($item in $itemsToPack) {
        $candidate = Join-Path $RootPath $item
        if (Test-Path -LiteralPath $candidate) {
            $resolvedItems.Add($candidate) | Out-Null
        }
    }

    if ($resolvedItems.Count -eq 0) {
        throw 'Не найдены файлы для сборки backup-архива.'
    }

    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $archivePath = Join-Path $BackupPath ("projectopsutility-backup-{0}.zip" -f $timestamp)
    Write-Log -Message ("Создание backup-архива: {0}" -f $archivePath) -LogFile $LogFile

    Compress-Archive -Path $resolvedItems.ToArray() -DestinationPath $archivePath -CompressionLevel Optimal -Force
    return $archivePath
}

function Test-PathContainsCyrillic {
    param(
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    return $Path -cmatch '[А-Яа-яЁё]'
}

function Test-PathContainsSpaces {
    param(
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    return $Path.Contains(' ')
}

function Get-ProjectPathCompatibilityMessage {
    param(
        [string]$Path
    )

    if (Test-PathContainsCyrillic -Path $Path) {
        return 'В ProjectPath обнаружена кириллица. Перенесите папку проекта в путь на латинице или переименуйте каталоги, затем повторите операцию.'
    }

    return $null
}

function Assert-ProjectPathCompatibility {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $message = Get-ProjectPathCompatibilityMessage -Path $Path
    if ($message) {
        throw $message
    }
}

function Get-LogFilePath {
    param(
        [string]$ConfiguredLogPath
    )

    $environment = Initialize-UtilityEnvironment
    $basePath = if ([string]::IsNullOrWhiteSpace($ConfiguredLogPath)) { $environment.LogsPath } else { $ConfiguredLogPath }
    Ensure-Directory -Path $basePath
    return Join-Path $basePath ("utility_{0}.log" -f (Get-Date -Format 'yyyyMMdd'))
}

function Write-Log {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [ValidateSet('INFO', 'WARN', 'ERROR', 'DEBUG')]
        [string]$Level = 'INFO',
        [string]$LogFile
    )

    $line = "[{0}] [{1}] {2}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level, $Message
    if ($LogFile) {
        Add-Content -Path $LogFile -Value $line -Encoding UTF8
    }
}

function Write-Section {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Title,
        [ValidateSet('neutral', 'ok', 'attention', 'warning', 'danger', 'critical')]
        [string]$Tone = 'neutral'
    )

    $palette = Get-UiPalette -Tone $Tone
    Write-Host '========================================' -ForegroundColor $palette.Border
    Write-Host (' {0}' -f $Title) -ForegroundColor $palette.Title
    Write-Host '========================================' -ForegroundColor $palette.Border
}

function Get-StatusColor {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('OK', 'WARN', 'FAIL', 'INFO', 'ATTN', 'CRIT')]
        [string]$Status
    )

    switch ($Status) {
        'OK' { return 'Green' }
        'WARN' { return 'Yellow' }
        'FAIL' { return 'Red' }
        'INFO' { return 'Cyan' }
        'ATTN' { return 'DarkYellow' }
        'CRIT' { return 'Magenta' }
    }
}

function Show-Separator {
    Write-Host '----------------------------------------' -ForegroundColor DarkGray
}

function Write-InfoPair {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Label,
        [Parameter(Mandatory = $true)]
        [string]$Value,
        [ConsoleColor]$LabelColor = [ConsoleColor]::DarkGray,
        [ConsoleColor]$ValueColor = [ConsoleColor]::Gray
    )

    Write-Host (' {0,-12}: ' -f $Label) -NoNewline -ForegroundColor $LabelColor
    Write-Host $Value -ForegroundColor $ValueColor
}

function Write-BulletList {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Items,
        [ConsoleColor]$BulletColor = [ConsoleColor]::DarkGray,
        [ConsoleColor]$TextColor = [ConsoleColor]::Gray
    )

    foreach ($item in $Items) {
        Write-Host ' - ' -NoNewline -ForegroundColor $BulletColor
        Write-Host $item -ForegroundColor $TextColor
    }
}

function Write-CardHeader {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Title,
        [ValidateSet('neutral', 'ok', 'attention', 'warning', 'danger', 'critical')]
        [string]$Tone = 'neutral',
        [int]$Width = 66
    )

    $palette = Get-UiPalette -Tone $Tone
    $safeWidth = [Math]::Max(30, $Width)
    $line = '+' + ('-' * ($safeWidth - 2)) + '+'
    $titleText = Get-FittedConsoleText -Text $Title.ToUpperInvariant() -Width ($safeWidth - 4)
    Write-Host $line -ForegroundColor $palette.Border
    Write-Host ('| ' + $titleText + ' |') -ForegroundColor $palette.Title
    Write-Host $line -ForegroundColor $palette.Border
}

function Get-FittedConsoleText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [int]$Width
    )

    if ($Width -le 0) {
        return ''
    }

    if ($Text.Length -le $Width) {
        return $Text.PadRight($Width)
    }

    if ($Width -le 3) {
        return $Text.Substring(0, $Width)
    }

    return ($Text.Substring(0, $Width - 3) + '...')
}

function Write-At {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Left,
        [Parameter(Mandatory = $true)]
        [int]$Top,
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [ConsoleColor]$ForegroundColor = [ConsoleColor]::Gray
    )

    $safeLeft = [Math]::Max(0, $Left)
    $safeTop = [Math]::Max(0, $Top)
    try {
        $safeLeft = [Math]::Min($safeLeft, [Math]::Max(0, [Console]::BufferWidth - 1))
        $safeTop = [Math]::Min($safeTop, [Math]::Max(0, [Console]::BufferHeight - 1))
        [Console]::SetCursorPosition($safeLeft, $safeTop)
        Write-Host $Text -ForegroundColor $ForegroundColor
    }
    catch {
        Write-Host $Text -ForegroundColor $ForegroundColor
    }
}

function Test-ConsoleCursorAvailable {
    try {
        $null = [Console]::CursorLeft
        return $true
    }
    catch {
        return $false
    }
}

function Write-StatusLine {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('OK', 'WARN', 'FAIL', 'INFO', 'ATTN', 'CRIT')]
        [string]$Status,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $color = Get-StatusColor -Status $Status

    Write-Host ("[{0}] " -f $Status) -NoNewline -ForegroundColor $color
    Write-Host $Message
}

function Get-UiPalette {
    param(
        [ValidateSet('neutral', 'ok', 'attention', 'warning', 'danger', 'critical')]
        [string]$Tone = 'neutral'
    )

    switch ($Tone) {
        'ok' {
            return [pscustomobject]@{
                Title  = 'Green'
                Border = 'DarkGreen'
            }
        }
        'attention' {
            return [pscustomobject]@{
                Title  = 'DarkYellow'
                Border = 'Yellow'
            }
        }
        'warning' {
            return [pscustomobject]@{
                Title  = 'Yellow'
                Border = 'DarkYellow'
            }
        }
        'danger' {
            return [pscustomobject]@{
                Title  = 'Red'
                Border = 'DarkRed'
            }
        }
        'critical' {
            return [pscustomobject]@{
                Title  = 'Magenta'
                Border = 'DarkMagenta'
            }
        }
        default {
            return [pscustomobject]@{
                Title  = 'Cyan'
                Border = 'DarkCyan'
            }
        }
    }
}

function Pause-Console {
    param(
        [string]$Message = 'Нажмите Enter для продолжения'
    )

    Write-Host ''
    Read-Host $Message | Out-Null
}

function Read-MenuChoice {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$AllowedChoices,
        [string]$Prompt = 'Выберите пункт'
    )

    while ($true) {
        $value = Read-Host $Prompt
        if ($AllowedChoices -contains $value) {
            return $value
        }
        Write-StatusLine -Status WARN -Message ('Недопустимый выбор: {0}' -f $value)
    }
}

function Test-CommandAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command
    )

    return $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

function ConvertTo-CommandArgumentText {
    param(
        [string[]]$Arguments = @()
    )

    if (-not $Arguments -or $Arguments.Count -eq 0) {
        return ''
    }

    $escapedArguments = foreach ($argument in $Arguments) {
        if ($null -eq $argument) {
            '""'
            continue
        }

        if ($argument.Length -eq 0) {
            '""'
            continue
        }

        if ($argument -notmatch '[\s"]') {
            $argument
            continue
        }

        '"' + (($argument -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
    }

    return ($escapedArguments -join ' ')
}

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory,
        [string]$LogFile,
        [switch]$IgnoreExitCode
    )

    $argumentText = ConvertTo-CommandArgumentText -Arguments $Arguments
    Write-Log -Message ("Command: {0} {1}" -f $FilePath, $argumentText) -LogFile $LogFile

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = $argumentText
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    if ($WorkingDirectory) {
        $psi.WorkingDirectory = $WorkingDirectory
    }

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi

    [void]$process.Start()

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while (-not $process.HasExited) {
        $elapsed = Format-ElapsedTime -Elapsed $stopwatch.Elapsed
        Write-Host ("`rТаймер команды: {0}" -f $elapsed) -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Milliseconds 250
    }
    $stopwatch.Stop()
    Write-Host ("`rТаймер команды: {0}" -f (Format-ElapsedTime -Elapsed $stopwatch.Elapsed)) -ForegroundColor DarkGray

    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()

    $result = [pscustomobject]@{
        ExitCode = $process.ExitCode
        StdOut   = ($stdout -replace "`r`n$", '')
        StdErr   = ($stderr -replace "`r`n$", '')
        Success  = ($process.ExitCode -eq 0)
        Duration = $stopwatch.Elapsed
    }

    if (-not $result.Success -and -not $IgnoreExitCode) {
        $message = if ($result.StdErr) { $result.StdErr } else { "Команда завершилась с кодом $($result.ExitCode)." }
        throw $message
    }

    return $result
}

function Format-ElapsedTime {
    param(
        [Parameter(Mandatory = $true)]
        [TimeSpan]$Elapsed
    )

    return '{0:00}:{1:00}:{2:00}' -f [int]$Elapsed.TotalHours, $Elapsed.Minutes, $Elapsed.Seconds
}

function Invoke-ScriptActionSafely {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action,
        [Parameter(Mandatory = $true)]
        [string]$ActionName,
        [string]$LogFile
    )

    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        Write-Log -Message ("Action started: {0}" -f $ActionName) -LogFile $LogFile
        $result = & $Action
        $stopwatch.Stop()
        Write-Log -Message ("Action completed: {0}" -f $ActionName) -LogFile $LogFile
        return [pscustomobject]@{
            Success = $true
            Result  = $result
            Error   = $null
            Duration = $stopwatch.Elapsed
        }
    }
    catch {
        Write-Log -Message ("Action failed: {0}. {1}" -f $ActionName, $_.Exception.Message) -Level ERROR -LogFile $LogFile
        return [pscustomobject]@{
            Success = $false
            Result  = $null
            Error   = $_.Exception.Message
            Duration = [TimeSpan]::Zero
        }
    }
}

Export-ModuleMember -Function *-*

