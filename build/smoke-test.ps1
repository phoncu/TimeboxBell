<#
.SYNOPSIS
    Publikuje konzolovou aplikaci a ověří, že publikovaný .exe vypíše rozvrh zvonění.
.DESCRIPTION
    Spustí TimeboxBell.Console.exe --list-schedule s časovým limitem a zkontroluje návratový kód
    a všech šest řádků výchozího rozvrhu. Nic nepřehrává a nečeká na skutečné zvonění.
.PARAMETER SkipPublish
    Nepublikovat znovu, jen otestovat existující .exe ve výstupní složce.
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $OutputDir = (Join-Path $PSScriptRoot '..\artifacts\publish\console'),
    [switch] $SkipPublish,
    [int] $TimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'src\TimeboxBell.Console\TimeboxBell.Console.csproj'

if (-not $SkipPublish) {
    dotnet publish $project -c $Configuration -o $OutputDir --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish selhal ($LASTEXITCODE)." }
}

$exe = Join-Path $OutputDir 'TimeboxBell.Console.exe'
if (-not (Test-Path $exe)) { throw "Publikovaný soubor neexistuje: $exe" }

$psi = [System.Diagnostics.ProcessStartInfo]::new($exe, '--list-schedule')
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8

$process = [System.Diagnostics.Process]::Start($psi)
$stdoutTask = $process.StandardOutput.ReadToEndAsync()
$stderrTask = $process.StandardError.ReadToEndAsync()
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    $process.Kill()
    throw "Aplikace neskončila do $TimeoutSeconds s – --list-schedule nemá čekat na zvonění."
}
$stdout = $stdoutTask.GetAwaiter().GetResult()
$stderr = $stderrTask.GetAwaiter().GetResult()

Write-Host $stdout
$failures = @()
if ($process.ExitCode -ne 0) { $failures += "Návratový kód $($process.ExitCode), čekán 0. stderr: $stderr" }

$expected = [ordered]@{
    '00' = 'NewHalfHour'
    '23' = 'Warning'
    '25' = 'PomodoroEnd'
    '30' = 'NewHalfHour'
    '53' = 'Warning'
    '55' = 'PomodoroEnd'
}
$lines = @($stdout -split "`r?`n" | Where-Object { $_ -match '^\s*:\d\d\s' })
if ($lines.Count -ne $expected.Count) {
    $failures += "Rozvrh má $($lines.Count) řádků, čekáno $($expected.Count)."
}
foreach ($minute in $expected.Keys) {
    $pattern = "^\s*:$minute\s+$($expected[$minute])\s+.+\.wav\s*$"
    if (-not ($lines | Where-Object { $_ -match $pattern })) {
        $failures += "Chybí řádek ':$minute $($expected[$minute]) … .wav'."
    }
}
if ($stdout -match 'běží') { $failures += 'Výpis rozvrhu spustil zvonění.' }

if ($failures) {
    $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    exit 1
}
Write-Host 'Smoke test prošel.' -ForegroundColor Green
