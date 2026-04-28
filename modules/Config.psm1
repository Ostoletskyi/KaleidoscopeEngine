Set-StrictMode -Version Latest

function Get-DefaultConfig {
    return [ordered]@{
        ProjectPath                = ''
        GitRemoteUrl               = ''
        DefaultBranch              = 'main'
        ConflictStrategy           = 'manual'
        CodexCommand               = 'codex'
        IgnoreFolders              = @('.git', 'node_modules', 'bin', 'obj', 'dist', 'build', '.vs', '.idea', '.vscode')
        LogPath                    = ''
        ConfirmSafeOperations      = $true
        AutoFetchBeforeGitAnalysis = $true
    }
}

function Initialize-ConfigFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath
    )

    $directory = Split-Path -Path $ConfigPath -Parent
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    if (-not (Test-Path -LiteralPath $ConfigPath)) {
        $defaults = Get-DefaultConfig
        $defaults | ConvertTo-Json -Depth 6 | Set-Content -Path $ConfigPath -Encoding UTF8
    }
}

function Get-Config {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath
    )

    Initialize-ConfigFile -ConfigPath $ConfigPath
    $raw = Get-Content -Path $ConfigPath -Raw -Encoding UTF8
    $config = $raw | ConvertFrom-Json
    $defaults = Get-DefaultConfig

    foreach ($key in $defaults.Keys) {
        if ($null -eq $config.$key) {
            $config | Add-Member -NotePropertyName $key -NotePropertyValue $defaults[$key]
        }
    }

    return $config
}

function Save-Config {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath,
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    $Config | ConvertTo-Json -Depth 6 | Set-Content -Path $ConfigPath -Encoding UTF8
}

function Reset-ConfigToSafeDefaults {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath
    )

    $config = [pscustomobject](Get-DefaultConfig)
    Save-Config -ConfigPath $ConfigPath -Config $config
    return $config
}

function Update-ConfigValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName,
        [AllowNull()]
        $Value
    )

    $config = Get-Config -ConfigPath $ConfigPath
    if ($null -eq ($config.PSObject.Properties[$PropertyName])) {
        throw "Свойство '$PropertyName' отсутствует в конфигурации."
    }

    $config.$PropertyName = $Value
    Save-Config -ConfigPath $ConfigPath -Config $config
    return $config
}

function Get-ConfigSummaryLines {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Config
    )

    return @(
        ('ProjectPath: {0}' -f $Config.ProjectPath)
        ('GitRemoteUrl: {0}' -f $Config.GitRemoteUrl)
        ('DefaultBranch: {0}' -f $Config.DefaultBranch)
        ('ConflictStrategy: {0}' -f $Config.ConflictStrategy)
        ('CodexCommand: {0}' -f $Config.CodexCommand)
        ('IgnoreFolders: {0}' -f ($Config.IgnoreFolders -join ', '))
        ('LogPath: {0}' -f $Config.LogPath)
        ('ConfirmSafeOperations: {0}' -f $Config.ConfirmSafeOperations)
        ('AutoFetchBeforeGitAnalysis: {0}' -f $Config.AutoFetchBeforeGitAnalysis)
    )
}

Export-ModuleMember -Function *-*

