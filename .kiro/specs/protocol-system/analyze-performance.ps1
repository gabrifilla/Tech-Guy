param([Parameter(Mandatory=$true)][string]$InputDirectory)
$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $InputDirectory).Path
$culture = [Globalization.CultureInfo]::InvariantCulture
$files = @(Get-ChildItem -LiteralPath $directory -Filter '*.csv.json' -File)
if ($files.Count -eq 0) { throw 'No completed scenario JSON files.' }
$cases = @($files | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
$baseline = @{}
foreach ($case in $cases) {
    if ($case.build -eq 'baseline') { $baseline["$($case.density)|$($case.vfx)|$($case.repetition)"] = $case }
}
$rows = foreach ($case in $cases) {
    $samples = @(Import-Csv -LiteralPath (Join-Path $directory $case.rawFile))
    if ($samples.Count -ne $case.frames) { throw "Raw sample count differs: $($case.rawFile)" }
    $means = @()
    for ($bin=0; $bin -lt 6; $bin++) {
        $start = [int][Math]::Floor($samples.Count*$bin/6)
        $end = [int][Math]::Floor($samples.Count*($bin+1)/6)
        $sum = 0.0
        for ($index=$start; $index -lt $end; $index++) { $sum += [double]::Parse($samples[$index].total_used_memory,$culture) }
        if ($end -gt $start) { $means += $sum/($end-$start) }
    }
    $monotonic = $means.Count -eq 6
    for ($index=1; $index -lt $means.Count; $index++) { if ($means[$index] -le $means[$index-1]) { $monotonic=$false } }
    $match = $baseline["$($case.density)|$($case.vfx)|$($case.repetition)"]
    $delta = $null
    if ($match) { $delta = $case.frameP95Ms - $match.frameP95Ms }
    [pscustomobject]@{
        build=$case.build; density=$case.density; vfx=$case.vfx; repetition=$case.repetition; smoke=$case.smoke
        cpuP95Ms=$case.cpuP95Ms; cpuP99Ms=$case.cpuP99Ms; cpuMeetsThresholds=($case.cpuP95Ms -le 1 -and $case.cpuP99Ms -le 2)
        wholeFrameGcP95Bytes=$case.gcP95Bytes; gcUpperBoundMeetsThreshold=($case.gcRecorderValid -and $case.gcP95Bytes -le 1024)
        frameP95Ms=$case.frameP95Ms; frameIncrementMs=$delta; frameMeetsAbsoluteThreshold=($case.frameP95Ms -le 16.7)
        frameIncrementMeetsThreshold=($null -ne $delta -and $delta -le 2)
        cpuRecorderValid=$case.cpuRecorderValid; gcRecorderValid=$case.gcRecorderValid; memoryRecorderValid=$case.memoryRecorderValid
        width=$case.width; height=$case.height; memoryFirst=$case.memoryFirst; memoryLast=$case.memoryLast
        memorySixBinMeans=$means; memoryMeansStrictlyIncreasing=$monotonic
        dropsImpact=$case.dropsImpact; dropsTick=$case.dropsTick; dropsQuery=$case.dropsQuery; dropsVfx=$case.dropsVfx; rawFile=$case.rawFile
    }
}
$progress = Get-Content -LiteralPath (Join-Path $directory 'progress.txt') -Raw
$report = [pscustomobject]@{
    directory=$directory; environment=(Get-Content -LiteralPath (Join-Path $directory 'environment.txt') -Raw)
    completedCases=$cases.Count; expectedFullMatrixCases=54
    fullMatrixCollected=($cases.Count -eq 54 -and @($cases | Where-Object smoke).Count -eq 0 -and $progress.StartsWith('complete'))
    acceptance='Pending review: whole-frame GC is a conservative bound, memory bins are diagnostic, hardware/reference and profiler attribution remain separate.'
    cases=@($rows)
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $directory 'analysis.json') -Encoding UTF8
$rows | Select-Object build,density,vfx,repetition,cpuP95Ms,cpuP99Ms,wholeFrameGcP95Bytes,frameP95Ms,frameIncrementMs,memoryMeansStrictlyIncreasing | Format-Table -AutoSize
