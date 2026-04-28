Set-StrictMode -Version Latest

function Show-OperationPreview {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Operation,
        [Parameter(Mandatory = $true)]
        [psobject]$SessionState,
        [string]$Breadcrumb
    )

    $recommendedAlreadyUsed = $false
    if ($Operation.RecommendedActionId) {
        $recommendedAlreadyUsed = $SessionState.RecommendedActionHistory -contains $Operation.RecommendedActionId
    }

    $tone = switch ($Operation.Level) {
        'SAFE' { 'ok' }
        'INFO' { 'neutral' }
        'CHANGE' { 'attention' }
        'RISK' { 'danger' }
        default { 'neutral' }
    }
    $palette = Get-UiPalette -Tone $tone

    if ($Breadcrumb) {
        Write-InfoPair -Label 'Контекст' -Value $Breadcrumb -LabelColor DarkGray -ValueColor Gray
        Write-Host ''
    }
    Write-CardHeader -Title ('Операция: {0}' -f $Operation.Name) -Tone $tone -Width 68
    Write-InfoPair -Label 'Уровень' -Value $Operation.Level -LabelColor DarkGray -ValueColor $palette.Title
    Write-Host ' Описание:' -ForegroundColor $palette.Title
    Write-Host (' {0}' -f $Operation.Description) -ForegroundColor Gray
    Write-Host ''
    Write-Host ' Что будет сделано:' -ForegroundColor $palette.Title
    Write-BulletList -Items $Operation.WhatWillBeDone
    Write-Host ''
    Write-Host ' Ожидаемый результат:' -ForegroundColor $palette.Title
    Write-BulletList -Items $Operation.ExpectedResult
    Write-Host ''
    Write-Host ' Риски и ограничения:' -ForegroundColor $palette.Title
    Write-BulletList -Items $Operation.Risks
    Write-Host ''
    Write-Host ' Рекомендуемый предыдущий шаг:' -ForegroundColor $palette.Title
    if ($Operation.RecommendedStepText) {
        Write-BulletList -Items @($Operation.RecommendedStepText)
    }
    else {
        Write-BulletList -Items @('Нет обязательного предварительного шага.')
    }
    Write-Host ''
    Write-Host ' Выберите действие:' -ForegroundColor $palette.Title
    Write-Host ' 1. Выполнить текущую операцию'

    if ($Operation.RecommendedActionId) {
        $line = ' 2. Выполнить рекомендуемое действие ({0})' -f $Operation.RecommendedActionName
        if ($recommendedAlreadyUsed) {
            Write-Host ($line + ' (Выполнено)') -ForegroundColor DarkGray
        }
        else {
            Write-Host $line
        }
    }

    Write-Host ' 0. Отмена'
    Write-Host ('+' + ('-' * 66) + '+') -ForegroundColor $palette.Border

    $allowed = @('1', '0')
    if ($Operation.RecommendedActionId) {
        $allowed += '2'
    }

    while ($true) {
        $choice = Read-Host 'Выберите действие'
        if ($allowed -notcontains $choice) {
            Write-Host 'Недопустимый выбор.' -ForegroundColor Yellow
            continue
        }

        if ($choice -eq '2' -and $recommendedAlreadyUsed) {
            Write-Host 'Рекомендуемое действие уже выполнялось в этой логической цепочке и повтор отключён.' -ForegroundColor DarkGray
            continue
        }

        return $choice
    }
}

function Register-RecommendedActionExecution {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$SessionState,
        [Parameter(Mandatory = $true)]
        [string]$ActionId
    )

    $SessionState.LastRecommendedAction = $ActionId
    $SessionState.RecommendedExecuted = $true
    if ($SessionState.RecommendedActionHistory -notcontains $ActionId) {
        [void]$SessionState.RecommendedActionHistory.Add($ActionId)
    }
}

function Reset-RecommendationState {
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$SessionState
    )

    $SessionState.LastRecommendedAction = $null
    $SessionState.RecommendedExecuted = $false
    $SessionState.RecommendedActionHistory = New-Object System.Collections.ArrayList
}

Export-ModuleMember -Function *-*

