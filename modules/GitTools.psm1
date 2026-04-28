Set-StrictMode -Version Latest

function Assert-ProjectDirectoryAvailable {
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

function Test-GitRepositoryPresent {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    if ([string]::IsNullOrWhiteSpace($Config.ProjectPath)) {
        return $false
    }

    return Test-Path -LiteralPath (Join-Path $Config.ProjectPath '.git')
}

function Assert-GitRepository {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    Assert-ProjectDirectoryAvailable -Config $Config
    if (-not (Test-GitRepositoryPresent -Config $Config)) {
        throw 'Указанная папка не является git-репозиторием.'
    }
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [string]$LogFile,
        [switch]$IgnoreExitCode
    )

    Assert-GitRepository -Config $Config
    return Invoke-ExternalCommand -FilePath 'git' -Arguments $Arguments -WorkingDirectory $Config.ProjectPath -LogFile $LogFile -IgnoreExitCode:$IgnoreExitCode
}

function Get-CurrentGitBranch {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    try {
        $result = Invoke-Git -Config $Config -Arguments @('branch', '--show-current') -LogFile $LogFile -IgnoreExitCode
        if ($result.Success -and -not [string]::IsNullOrWhiteSpace($result.StdOut)) {
            return $result.StdOut.Trim()
        }
    }
    catch {}

    try {
        $result = Invoke-Git -Config $Config -Arguments @('symbolic-ref', '--short', 'HEAD') -LogFile $LogFile -IgnoreExitCode
        if ($result.Success -and -not [string]::IsNullOrWhiteSpace($result.StdOut)) {
            return $result.StdOut.Trim()
        }
    }
    catch {}

    try {
        $result = Invoke-Git -Config $Config -Arguments @('rev-parse', '--abbrev-ref', 'HEAD') -LogFile $LogFile -IgnoreExitCode
        if ($result.Success -and -not [string]::IsNullOrWhiteSpace($result.StdOut)) {
            return $result.StdOut.Trim()
        }
    }
    catch {}

    return $null
}

function Get-GitRemoteUrlActual {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    try {
        $result = Invoke-Git -Config $Config -Arguments @('remote', 'get-url', 'origin') -LogFile $LogFile
        return $result.StdOut.Trim()
    }
    catch {
        return $Config.GitRemoteUrl
    }
}

function Ensure-GitOriginRemote {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $existingRemote = Invoke-Git -Config $Config -Arguments @('remote', 'get-url', 'origin') -LogFile $LogFile -IgnoreExitCode
    if ($existingRemote.Success -and -not [string]::IsNullOrWhiteSpace($existingRemote.StdOut)) {
        return $existingRemote.StdOut.Trim()
    }

    if ([string]::IsNullOrWhiteSpace($Config.GitRemoteUrl)) {
        throw 'Remote origin не настроен, и GitRemoteUrl отсутствует в конфиге. Сначала задайте GitRemoteUrl или добавьте remote origin вручную.'
    }

    Invoke-Git -Config $Config -Arguments @('remote', 'add', 'origin', $Config.GitRemoteUrl) -LogFile $LogFile | Out-Null
    return $Config.GitRemoteUrl.Trim()
}

function Get-GitConflictFiles {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $result = Invoke-Git -Config $Config -Arguments @('diff', '--name-only', '--diff-filter=U') -LogFile $LogFile
    if ([string]::IsNullOrWhiteSpace($result.StdOut)) {
        return @()
    }
    return @($result.StdOut -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-GitStatusPorcelain {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $result = Invoke-Git -Config $Config -Arguments @('status', '--porcelain') -LogFile $LogFile
    if ([string]::IsNullOrWhiteSpace($result.StdOut)) {
        return @()
    }
    return @($result.StdOut -split "`r?`n" | Where-Object { $_ -ne '' })
}

function Get-GitLastCommit {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $result = Invoke-Git -Config $Config -Arguments @('log', '-1', '--pretty=format:%h %s') -LogFile $LogFile -IgnoreExitCode
    if ($result.Success -and $result.StdOut) {
        return $result.StdOut.Trim()
    }
    return 'Коммиты не найдены.'
}

function Get-GitCommitCount {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $result = Invoke-Git -Config $Config -Arguments @('rev-list', '--count', 'HEAD') -LogFile $LogFile -IgnoreExitCode
    if ($result.Success -and $result.StdOut -match '^\d+$') {
        return [int]$result.StdOut.Trim()
    }

    return 0
}

function New-GitNeutralAutoCommitMessage {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$Context = 'emergency commit',
        [string]$LogFile
    )

    $commitNumber = (Get-GitCommitCount -Config $Config -LogFile $LogFile) + 1
    $resolvedContext = Resolve-GitAutoCommitContext -Context $Context
    return ('auto/{0:0000} {1} - {2}' -f $commitNumber, (Get-Date -Format 'yyyy-MM-dd HH:mm'), $resolvedContext)
}

function Resolve-GitAutoCommitContext {
    param(
        [string]$Context
    )

    switch ($Context) {
        'safe service commit' { return 'manual save point before repository operation' }
        'repository bootstrap commit' { return 'initial repository bootstrap and baseline snapshot' }
        'conflict-ours' { return 'conflict resolution using local version (ours)' }
        'conflict-theirs' { return 'conflict resolution using remote version (theirs)' }
        'pre-pull-save' { return 'local safety snapshot before sync or pull' }
        'pre-push-save' { return 'local safety snapshot before push' }
        default { return $Context }
    }
}

function Get-GitAheadBehind {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $upstream = Invoke-Git -Config $Config -Arguments @('rev-parse', '--abbrev-ref', '--symbolic-full-name', '@{u}') -LogFile $LogFile -IgnoreExitCode
    if (-not $upstream.Success) {
        return [pscustomobject]@{
            HasUpstream = $false
            Upstream    = ''
            Ahead       = 0
            Behind      = 0
        }
    }

    $counts = Invoke-Git -Config $Config -Arguments @('rev-list', '--left-right', '--count', 'HEAD...@{u}') -LogFile $LogFile
    $parts = $counts.StdOut.Trim() -split '\s+'
    return [pscustomobject]@{
        HasUpstream = $true
        Upstream    = $upstream.StdOut.Trim()
        Ahead       = [int]$parts[0]
        Behind      = [int]$parts[1]
    }
}

function Get-GitWorkingTreeStats {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$StatusLines,
        [string[]]$ConflictFiles
    )

    $modified = 0
    $staged = 0
    $untracked = 0

    foreach ($line in $StatusLines) {
        if ($line.Length -lt 2) {
            continue
        }

        $x = $line.Substring(0, 1)
        $y = $line.Substring(1, 1)

        if ($line.StartsWith('??')) {
            $untracked++
            continue
        }

        if ($x -ne ' ' -and $x -ne '?') {
            $staged++
        }
        if ($y -ne ' ') {
            $modified++
        }
    }

    return [pscustomobject]@{
        Modified  = $modified
        Staged    = $staged
        Untracked = $untracked
        Conflicts = $ConflictFiles.Count
    }
}

function Get-GitScenario {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$TreeStats,
        [Parameter(Mandatory = $true)]
        [psobject]$AheadBehind
    )

    $hasLocalChanges = ($TreeStats.Modified + $TreeStats.Staged + $TreeStats.Untracked) -gt 0

    if ($TreeStats.Conflicts -gt 0) { return 'CONFLICTS_PRESENT' }
    if (-not $AheadBehind.HasUpstream) { return 'NO_UPSTREAM' }
    if ($hasLocalChanges -and $AheadBehind.Behind -gt 0) { return 'DIRTY_AND_REMOTE_AHEAD' }
    if ($hasLocalChanges -and $AheadBehind.Ahead -eq 0 -and $AheadBehind.Behind -eq 0) { return 'LOCAL_CHANGES_ONLY' }
    if ($AheadBehind.Ahead -gt 0 -and $AheadBehind.Behind -gt 0) { return 'DIVERGED' }
    if ($AheadBehind.Ahead -gt 0 -and $AheadBehind.Behind -eq 0) { return 'LOCAL_AHEAD' }
    if ($AheadBehind.Behind -gt 0 -and $AheadBehind.Ahead -eq 0) { return 'REMOTE_AHEAD' }
    if (-not $hasLocalChanges -and $AheadBehind.Ahead -eq 0 -and $AheadBehind.Behind -eq 0) { return 'CLEAN_SYNCED' }

    return 'LOCAL_CHANGES_ONLY'
}

function Get-GitScenarioText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Scenario
    )

    switch ($Scenario) {
        'CLEAN_SYNCED' { return 'Рабочее дерево чистое, локальная и удалённая ветки синхронизированы.' }
        'LOCAL_CHANGES_ONLY' { return 'Есть локальные незакоммиченные изменения. Удалённая ветка не опережает локальную.' }
        'LOCAL_AHEAD' { return 'Локальная ветка содержит коммиты, которые ещё не отправлены на remote.' }
        'REMOTE_AHEAD' { return 'Удалённая ветка содержит новые коммиты, которых нет локально.' }
        'DIVERGED' { return 'Локальная и удалённая ветки разошлись: есть уникальные коммиты с обеих сторон.' }
        'CONFLICTS_PRESENT' { return 'В репозитории есть конфликтующие файлы. Нужен контролируемый сценарий разрешения.' }
        'DIRTY_AND_REMOTE_AHEAD' { return 'Есть незакоммиченные изменения, и одновременно удалённая ветка опережает локальную.' }
        'NO_UPSTREAM' { return 'Для текущей ветки не настроен upstream.' }
        default { return 'Состояние требует ручной проверки.' }
    }
}

function Get-GitRecommendedActionId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Scenario
    )

    switch ($Scenario) {
        'LOCAL_CHANGES_ONLY' { return 'git.commit' }
        'LOCAL_AHEAD' { return 'git.push' }
        'REMOTE_AHEAD' { return 'git.pull' }
        'DIVERGED' { return 'git.backupBranch' }
        'CONFLICTS_PRESENT' { return 'git.conflicts' }
        'DIRTY_AND_REMOTE_AHEAD' { return 'git.commit' }
        'NO_UPSTREAM' { return 'git.remotes' }
        default { return $null }
    }
}

function Get-GitRecommendedActionName {
    param(
        [string]$ActionId
    )

    switch ($ActionId) {
        'git.commit' { return 'Commit' }
        'git.push' { return 'Push' }
        'git.pull' { return 'Pull' }
        'git.backupBranch' { return 'Create backup branch' }
        'git.conflicts' { return 'Conflict resolution' }
        'git.remotes' { return 'Check remotes and branch' }
        default { return '' }
    }
}

function Get-GitAnalysis {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    Assert-GitRepository -Config $Config

    if ($Config.AutoFetchBeforeGitAnalysis) {
        Invoke-Git -Config $Config -Arguments @('fetch', '--all', '--prune') -LogFile $LogFile -IgnoreExitCode | Out-Null
    }

    $branch = Get-CurrentGitBranch -Config $Config -LogFile $LogFile
    $remote = Get-GitRemoteUrlActual -Config $Config -LogFile $LogFile
    $statusLines = @(Get-GitStatusPorcelain -Config $Config -LogFile $LogFile)
    $conflictFiles = @(Get-GitConflictFiles -Config $Config -LogFile $LogFile)
    $treeStats = Get-GitWorkingTreeStats -StatusLines $statusLines -ConflictFiles $conflictFiles
    $aheadBehind = Get-GitAheadBehind -Config $Config -LogFile $LogFile
    $lastCommit = Get-GitLastCommit -Config $Config -LogFile $LogFile
    $scenario = Get-GitScenario -TreeStats $treeStats -AheadBehind $aheadBehind
    $recommendedActionId = Get-GitRecommendedActionId -Scenario $scenario

    return [pscustomobject]@{
        ProjectPath           = $Config.ProjectPath
        Branch                = $branch
        Remote                = $remote
        Upstream              = $aheadBehind.Upstream
        HasUpstream           = $aheadBehind.HasUpstream
        WorkingTree           = $treeStats
        Ahead                 = $aheadBehind.Ahead
        Behind                = $aheadBehind.Behind
        LastCommit            = $lastCommit
        Scenario              = $scenario
        Diagnosis             = Get-GitScenarioText -Scenario $scenario
        RecommendedActionId   = $recommendedActionId
        RecommendedActionName = Get-GitRecommendedActionName -ActionId $recommendedActionId
    }
}

function Show-GitAnalysis {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Analysis
    )

    $tone = switch ($Analysis.Scenario) {
        'CLEAN_SYNCED' { 'ok' }
        'LOCAL_AHEAD' { 'attention' }
        'REMOTE_AHEAD' { 'warning' }
        'DIVERGED' { 'danger' }
        'CONFLICTS_PRESENT' { 'critical' }
        'DIRTY_AND_REMOTE_AHEAD' { 'danger' }
        'NO_UPSTREAM' { 'warning' }
        default { 'neutral' }
    }
    Write-Host ''
    Write-CardHeader -Title 'Анализ состояния Git' -Tone $tone -Width 68
    Write-InfoPair -Label 'Проект' -Value $Analysis.ProjectPath -LabelColor DarkGray -ValueColor Gray
    Write-InfoPair -Label 'Ветка' -Value $Analysis.Branch -LabelColor DarkGray -ValueColor Gray
    Write-InfoPair -Label 'Remote' -Value $Analysis.Remote -LabelColor DarkGray -ValueColor Gray
    Write-Host ''
    Write-Host ' Рабочее дерево:' -ForegroundColor Cyan
    Write-BulletList -Items @(
        ('modified: {0}' -f $Analysis.WorkingTree.Modified)
        ('staged: {0}' -f $Analysis.WorkingTree.Staged)
        ('untracked: {0}' -f $Analysis.WorkingTree.Untracked)
        ('conflicts: {0}' -f $Analysis.WorkingTree.Conflicts)
    )
    Write-Host ''
    Write-Host ' Синхронизация:' -ForegroundColor Cyan
    Write-BulletList -Items @(
        ('ahead: {0}' -f $Analysis.Ahead)
        ('behind: {0}' -f $Analysis.Behind)
    )
    Write-Host ''
    Write-Host ' Последний коммит:' -ForegroundColor Cyan
    Write-Host (' {0}' -f $Analysis.LastCommit) -ForegroundColor Gray
    Write-Host ''
    $summaryColor = switch ($tone) {
        'ok' { 'Green' }
        'attention' { 'DarkYellow' }
        'warning' { 'Yellow' }
        'danger' { 'Red' }
        'critical' { 'Magenta' }
        default { 'Cyan' }
    }
    Write-Host ' ИТОГ:' -ForegroundColor $summaryColor
    Write-Host (' {0}' -f $Analysis.Diagnosis) -ForegroundColor Gray
    Write-Host ''
    Write-Host ' РЕКОМЕНДАЦИЯ:' -ForegroundColor $summaryColor
    if ($Analysis.RecommendedActionName) {
        Write-Host (' {0}' -f $Analysis.RecommendedActionName) -ForegroundColor Gray
    }
    else {
        Write-Host ' Явного следующего действия не требуется.' -ForegroundColor Gray
    }
    Write-Host ('+' + ('-' * 66) + '+') -ForegroundColor (Get-UiPalette -Tone $tone).Border
}

function Invoke-GitStatusRaw {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    return Invoke-Git -Config $Config -Arguments @('status') -LogFile $LogFile
}

function Invoke-GitPushSafe {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $branch = Get-CurrentGitBranch -Config $Config -LogFile $LogFile
    if ([string]::IsNullOrWhiteSpace($branch)) {
        throw 'Не удалось определить текущую ветку.'
    }

    $headRevision = Invoke-Git -Config $Config -Arguments @('rev-parse', '--verify', 'HEAD') -LogFile $LogFile -IgnoreExitCode
    $hasHeadCommit = $headRevision.Success
    $statusLines = @(Get-GitStatusPorcelain -Config $Config -LogFile $LogFile)

    if (-not $hasHeadCommit -and $statusLines.Count -eq 0) {
        throw 'Нечего отправлять: в репозитории ещё нет коммитов и нет локальных изменений для автокоммита.'
    }

    if (-not $hasHeadCommit -or $statusLines.Count -gt 0) {
        $message = New-GitNeutralAutoCommitMessage -Config $Config -Context 'pre-push-save' -LogFile $LogFile
        Invoke-Git -Config $Config -Arguments @('add', '-A') -LogFile $LogFile | Out-Null
        Invoke-Git -Config $Config -Arguments @('commit', '-m', $message) -LogFile $LogFile | Out-Null
    }

    Ensure-GitOriginRemote -Config $Config -LogFile $LogFile | Out-Null

    try {
        return Invoke-Git -Config $Config -Arguments @('push', '-u', 'origin', $branch) -LogFile $LogFile
    }
    catch {
        if ($_.Exception.Message -match 'rejected') {
            throw 'Push отклонён. Вероятно, удалённая ветка содержит новые коммиты. Сначала выполните анализ и контролируемый pull.'
        }
        throw
    }
}

function Invoke-GitPullSafe {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $analysis = Get-GitAnalysis -Config $Config -LogFile $LogFile
    if (($analysis.WorkingTree.Modified + $analysis.WorkingTree.Staged + $analysis.WorkingTree.Untracked) -gt 0) {
        throw 'Pull остановлен: есть локальные незакоммиченные изменения. Сначала выполните commit или другой безопасный сценарий сохранения.'
    }

    return Invoke-Git -Config $Config -Arguments @('pull', '--ff-only') -LogFile $LogFile
}

function Invoke-GitCommitSafe {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$Message,
        [string]$LogFile
    )

    $statusLines = @(Get-GitStatusPorcelain -Config $Config -LogFile $LogFile)
    if ($statusLines.Count -eq 0) {
        throw 'Нет изменений для commit.'
    }

    Invoke-Git -Config $Config -Arguments @('add', '-A') -LogFile $LogFile | Out-Null
    return Invoke-Git -Config $Config -Arguments @('commit', '-m', $Message) -LogFile $LogFile
}

function New-GitBackupBranch {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $headRevision = Invoke-Git -Config $Config -Arguments @('rev-parse', '--verify', 'HEAD') -LogFile $LogFile -IgnoreExitCode
    if (-not $headRevision.Success) {
        throw 'Нельзя создать backup branch до первого commit. Сначала выполните initial commit.'
    }

    $name = 'backup/pre-merge-{0}' -f (Get-Date -Format 'yyyyMMdd-HHmmss')
    if ($name -notmatch '^[A-Za-z0-9._/\-]+$') {
        throw 'Сформировано невалидное имя backup branch.'
    }

    Invoke-Git -Config $Config -Arguments @('branch', $name) -LogFile $LogFile | Out-Null
    return $name
}

function Get-GitLogPreview {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    return Invoke-Git -Config $Config -Arguments @('log', '--oneline', '-10') -LogFile $LogFile
}

function Get-GitRemotesAndBranch {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    $branch = Get-CurrentGitBranch -Config $Config -LogFile $LogFile
    $remotes = Invoke-Git -Config $Config -Arguments @('remote', '-v') -LogFile $LogFile
    return [pscustomobject]@{
        Branch  = $branch
        Remotes = $remotes.StdOut
    }
}

function Resolve-GitConflictsByStrategy {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [Parameter(Mandatory = $true)]
        [ValidateSet('ours', 'theirs')]
        [string]$Strategy,
        [string]$LogFile
    )

    $files = @(Get-GitConflictFiles -Config $Config -LogFile $LogFile)
    if ($files.Count -eq 0) {
        throw 'Конфликтующие файлы не найдены.'
    }

    foreach ($file in $files) {
        Invoke-Git -Config $Config -Arguments @('checkout', "--$Strategy", '--', $file) -LogFile $LogFile | Out-Null
        Invoke-Git -Config $Config -Arguments @('add', '--', $file) -LogFile $LogFile | Out-Null
    }

    $message = New-GitNeutralAutoCommitMessage -Config $Config -Context ("conflict-{0}" -f $Strategy) -LogFile $LogFile
    return Invoke-Git -Config $Config -Arguments @('commit', '-m', $message) -LogFile $LogFile
}

function Initialize-GitRepositoryBootstrap {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config,
        [string]$LogFile
    )

    Assert-ProjectDirectoryAvailable -Config $Config
    if (Test-GitRepositoryPresent -Config $Config) {
        throw 'В указанной папке уже существует git-репозиторий.'
    }
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw 'git не найден в PATH.'
    }

    $notes = New-Object System.Collections.Generic.List[string]

    Invoke-ExternalCommand -FilePath 'git' -Arguments @('init') -WorkingDirectory $Config.ProjectPath -LogFile $LogFile | Out-Null
    $notes.Add('Выполнен git init.') | Out-Null

    Invoke-ExternalCommand -FilePath 'git' -Arguments @('add', '.') -WorkingDirectory $Config.ProjectPath -LogFile $LogFile | Out-Null
    $notes.Add('Файлы добавлены в индекс через git add .') | Out-Null

    $status = Invoke-ExternalCommand -FilePath 'git' -Arguments @('status', '--porcelain') -WorkingDirectory $Config.ProjectPath -LogFile $LogFile
    if ($status.StdOut) {
        $initMessage = New-GitNeutralAutoCommitMessage -Config $Config -Context 'repository bootstrap commit' -LogFile $LogFile
        Invoke-ExternalCommand -FilePath 'git' -Arguments @('commit', '-m', $initMessage) -WorkingDirectory $Config.ProjectPath -LogFile $LogFile | Out-Null
        $notes.Add(("Создан первичный commit: {0}" -f $initMessage)) | Out-Null
    }
    else {
        $notes.Add('Изменений для первичного commit не обнаружено. Commit пропущен.') | Out-Null
    }

    if (-not [string]::IsNullOrWhiteSpace($Config.DefaultBranch)) {
        Invoke-ExternalCommand -FilePath 'git' -Arguments @('branch', '-M', $Config.DefaultBranch) -WorkingDirectory $Config.ProjectPath -LogFile $LogFile -IgnoreExitCode | Out-Null
        $notes.Add(("Целевая ветка установлена в {0}." -f $Config.DefaultBranch)) | Out-Null
    }

    if (-not [string]::IsNullOrWhiteSpace($Config.GitRemoteUrl)) {
        Invoke-ExternalCommand -FilePath 'git' -Arguments @('remote', 'add', 'origin', $Config.GitRemoteUrl) -WorkingDirectory $Config.ProjectPath -LogFile $LogFile | Out-Null
        $notes.Add(("Добавлен remote origin: {0}" -f $Config.GitRemoteUrl)) | Out-Null
    }
    else {
        $notes.Add('GitRemoteUrl не задан. Remote origin не добавлялся.') | Out-Null
    }

    $notes.Add('Следующий рекомендуемый шаг: выполнить push -u origin <ветка> после проверки состояния.') | Out-Null
    return ($notes -join [Environment]::NewLine)
}

Export-ModuleMember -Function *-*

