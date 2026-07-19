[CmdletBinding()]
param(
    [string]$SourceXlsx
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($SourceXlsx)) {
    $SourceXlsx = Join-Path $projectRoot 'SourceData/kkami_datatable_ver03.xlsx'
}

$sourcePath = [System.IO.Path]::GetFullPath($SourceXlsx)
$toolProject = Join-Path $projectRoot 'Tools/Kkami.DataTools/Kkami.DataTools.csproj'
$validationRoot = Join-Path $projectRoot 'Temp/KkamiDataValidation'
$rawOutput = Join-Path $validationRoot 'Raw'
$runtimeOutput = Join-Path $validationRoot 'Runtime'

New-Item -ItemType Directory -Force -Path $rawOutput, $runtimeOutput | Out-Null

& dotnet run --project $toolProject -- export $sourcePath --output $rawOutput --runtime-output $runtimeOutput
if ($LASTEXITCODE -ne 0) {
    throw "Kkami.DataTools failed with exit code $LASTEXITCODE."
}

$expectedRows = [ordered]@{
    'currency.csv'  = 6
    'piece.csv'     = 10
    'stage.csv'     = 50
    'skilltree.csv' = 28
    'stringkey.csv' = 28
    'chat.csv'      = 26
    'res_vfx.csv'   = 10
}

foreach ($entry in $expectedRows.GetEnumerator()) {
    $csvPath = Join-Path $runtimeOutput $entry.Key
    if (-not (Test-Path -LiteralPath $csvPath -PathType Leaf)) {
        throw "Runtime CSV was not generated: $($entry.Key)"
    }

    $rows = @(Import-Csv -LiteralPath $csvPath)
    if ($rows.Count -ne $entry.Value) {
        throw "$($entry.Key) row count mismatch. Expected $($entry.Value), got $($rows.Count)."
    }
}

$pieceRows = @(Import-Csv -LiteralPath (Join-Path $runtimeOutput 'piece.csv'))
$missingImages = @($pieceRows | Where-Object { [string]::IsNullOrWhiteSpace($_.pieceimg_id) })
if ($missingImages.Count -gt 0) {
    throw "piece.csv contains $($missingImages.Count) empty pieceimg_id value(s)."
}

$stageRows = @(Import-Csv -LiteralPath (Join-Path $runtimeOutput 'stage.csv'))
$weightColumns = @(
    'piece_10001_weight',
    'piece_10002_weight',
    'piece_10003_weight',
    'piece_10004_weight',
    'piece_10005_weight'
)
$culture = [System.Globalization.CultureInfo]::InvariantCulture
foreach ($stage in $stageRows) {
    $weightSum = 0.0
    foreach ($column in $weightColumns) {
        $weightSum += [double]::Parse($stage.$column, $culture)
    }

    if ([Math]::Abs($weightSum - 1.0) -gt 0.000001) {
        throw "stage.csv stage_id=$($stage.stage_id) weight sum is $weightSum instead of 1.0."
    }
}

$bossStages = @($stageRows | Where-Object { -not [string]::IsNullOrWhiteSpace($_.boss_id) })
$expectedBossStages = @('40010', '40020', '40030', '40040', '40050')
$actualBossStages = @($bossStages | ForEach-Object { $_.stage_id })
if ((Compare-Object -ReferenceObject $expectedBossStages -DifferenceObject $actualBossStages).Count -gt 0) {
    throw "Boss-stage mapping mismatch. Expected $($expectedBossStages -join ', '), got $($actualBossStages -join ', ')."
}

Write-Host "Kkami data validation passed: $($expectedRows.Count) tables, 158 rows, 50 stage weight sums, 5 boss stages."
Write-Host "Validation output: $validationRoot"
