<#
.SYNOPSIS
    Publikuje konzolovou aplikaci a ověří, že publikovaný .exe vypíše rozvrh zvonění.
.DESCRIPTION
    Spustí TimeboxBell.Console.exe --list-schedule s časovým limitem a zkontroluje návratový kód
    a všech šest řádků výchozího rozvrhu. Pak ověří, že běží jen jedna instance: druhé spuštění
    skončí kódem 3, výpis rozvrhu funguje dál a po zabití první instance jde spustit nová.
    Na skutečné zvonění nečeká; TimeboxBell během testu nesmí běžet.
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

function Start-App([string] $arguments) {
    $psi = [System.Diagnostics.ProcessStartInfo]::new($exe, $arguments)
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    $process = [System.Diagnostics.Process]::Start($psi)
    [pscustomobject]@{
        Process = $process
        Stdout  = $process.StandardOutput.ReadToEndAsync()
        Stderr  = $process.StandardError.ReadToEndAsync()
    }
}

# Spustí aplikaci, počká na konec a vrátí návratový kód a výstup.
function Invoke-App([string] $arguments) {
    $app = Start-App $arguments
    if (-not $app.Process.WaitForExit($TimeoutSeconds * 1000)) {
        $app.Process.Kill()
        throw "TimeboxBell.Console $arguments neskončil do $TimeoutSeconds s."
    }
    [pscustomobject]@{
        ExitCode = $app.Process.ExitCode
        Stdout   = $app.Stdout.GetAwaiter().GetResult()
        Stderr   = $app.Stderr.GetAwaiter().GetResult()
    }
}

# Spustí zvonění a ověří, že po pár sekundách pořád běží (nezastavila ho jiná instance).
function Start-Ringing {
    $app = Start-App ''
    if ($app.Process.WaitForExit(3000)) {
        $stderr = $app.Stderr.GetAwaiter().GetResult()
        throw "Zvonění skončilo hned po spuštění (kód $($app.Process.ExitCode)): $stderr Neběží už TimeboxBell? Před testem ho ukončete."
    }
    $app.Process
}

function Stop-Ringing([System.Diagnostics.Process] $process) {
    if (-not $process.HasExited) {
        $process.Kill() # i zabitý proces musí uvolnit místo pro další instanci
        $process.WaitForExit()
    }
}

$failures = @()

# 1) Výpis rozvrhu
$result = Invoke-App '--list-schedule'
$stdout = $result.Stdout
Write-Host $stdout
if ($result.ExitCode -ne 0) { $failures += "Návratový kód $($result.ExitCode), čekán 0. stderr: $($result.Stderr)" }

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

# 2) Jen jedna instance: druhá se nerozběhne, výpis rozvrhu jde dál, po zabití první se nová rozběhne
Write-Host 'Kontrola jediné instance…'
$first = Start-Ringing
try {
    $second = Invoke-App ''
    if ($second.ExitCode -ne 3) {
        $failures += "Druhá instance skončila kódem $($second.ExitCode), čekán 3."
    }
    if ($second.Stderr -notmatch 'už běží') {
        $failures += "Druhá instance nevypsala, že TimeboxBell už běží. stderr: $($second.Stderr)"
    }

    $list = Invoke-App '--list-schedule'
    if ($list.ExitCode -ne 0) {
        $failures += "--list-schedule při běžící instanci skončil kódem $($list.ExitCode), čekán 0."
    }
}
finally {
    Stop-Ringing $first
}

try {
    $third = Start-Ringing
    Stop-Ringing $third
}
catch {
    $failures += "Po zabití první instance nejde spustit nová: $_"
}

if ($failures) {
    $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    exit 1
}
Write-Host 'Smoke test prošel.' -ForegroundColor Green
