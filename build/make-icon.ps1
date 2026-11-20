<#
.SYNOPSIS
    Vygeneruje src/TimeboxBell.ico – ikonu TimeboxBell.exe a TimeboxBell.Console.exe.
.DESCRIPTION
    Kreslí stejný motiv jako ikona v oznamovací oblasti (src/TimeboxBell.Tray/TrayIcons.cs):
    zelený ciferník s bílými ručičkami na :25 (konec pomodora). Větší velikosti mají navíc
    bílý okraj a rysky hodin. Každá velikost se kreslí zvlášť (ne zmenšováním), aby byla ostrá;
    snímky se ukládají jako PNG uvnitř .ico (Windows Vista a novější).
#>
[CmdletBinding()]
param(
    [string] $OutputPath = (Join-Path $PSScriptRoot '..\src\TimeboxBell.ico'),
    [int[]] $Sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-Frame([int] $size) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)

        $s = [double] $size
        $c = $s / 2
        $green = [System.Drawing.Color]::FromArgb(46, 125, 50)
        $darkGreen = [System.Drawing.Color]::FromArgb(27, 94, 32)
        $white = [System.Drawing.Color]::White

        # Tmavší obrys, aby ikona nesplynula se zelenou nebo světlou tapetou.
        $margin = [Math]::Max(0.5, $s * 0.02)
        $outer = [System.Drawing.RectangleF]::new($margin, $margin, $s - 2 * $margin, $s - 2 * $margin)
        $brush = [System.Drawing.SolidBrush]::new($darkGreen)
        $g.FillEllipse($brush, $outer); $brush.Dispose()

        # Bílý prstenec od 32 px výš; malé velikosti mají jen plný ciferník.
        $faceInset = $margin + [Math]::Max(1.0, $s * 0.035)
        if ($size -ge 32) {
            $ring = $margin + $s * 0.04
            $r = [System.Drawing.RectangleF]::new($ring, $ring, $s - 2 * $ring, $s - 2 * $ring)
            $brush = [System.Drawing.SolidBrush]::new($white)
            $g.FillEllipse($brush, $r); $brush.Dispose()
            $faceInset = $ring + $s * 0.05
        }
        $face = [System.Drawing.RectangleF]::new($faceInset, $faceInset, $s - 2 * $faceInset, $s - 2 * $faceInset)
        $brush = [System.Drawing.SolidBrush]::new($green)
        $g.FillEllipse($brush, $face); $brush.Dispose()
        $faceRadius = $face.Width / 2

        # Rysky: od 48 px všech 12, od 24 px jen 12/3/6/9.
        if ($size -ge 24) {
            $tickPen = [System.Drawing.Pen]::new($white, [single][Math]::Max(1.0, $s * 0.035))
            $tickPen.StartCap = $tickPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $step = if ($size -ge 48) { 1 } else { 3 }
            for ($h = 0; $h -lt 12; $h += $step) {
                $angle = $h * [Math]::PI / 6
                $major = ($h % 3) -eq 0
                $len = if ($major) { 0.18 } else { 0.10 }
                $r1 = $faceRadius * 0.86; $r2 = $faceRadius * (0.86 - $len)
                $g.DrawLine($tickPen,
                    [single]($c + $r1 * [Math]::Sin($angle)), [single]($c - $r1 * [Math]::Cos($angle)),
                    [single]($c + $r2 * [Math]::Sin($angle)), [single]($c - $r2 * [Math]::Cos($angle)))
            }
            $tickPen.Dispose()
        }

        # Ručičky na :25 – minutová na 25 minutách, hodinová krátká k pravému dolnímu rohu (jako v oznamovací oblasti).
        $handWidth = [single][Math]::Max(1.6, $s * 0.085)
        $pen = [System.Drawing.Pen]::new($white, $handWidth)
        $pen.StartCap = $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $minuteLen = $faceRadius * $(if ($size -ge 24) { 0.62 } else { 0.72 })
        $g.DrawLine($pen, [single]$c, [single]$c, [single]$c, [single]($c - $minuteLen))
        $hourAngle = 25 * [Math]::PI / 30 # 25 minut na ciferníku
        $hourLen = $faceRadius * 0.48
        $g.DrawLine($pen, [single]$c, [single]$c,
            [single]($c + $hourLen * [Math]::Sin($hourAngle)), [single]($c - $hourLen * [Math]::Cos($hourAngle)))
        $pen.Dispose()

        if ($size -ge 48) {
            $dot = $handWidth * 1.3
            $brush = [System.Drawing.SolidBrush]::new($white)
            $g.FillEllipse($brush, [single]($c - $dot / 2), [single]($c - $dot / 2), [single]$dot, [single]$dot)
            $brush.Dispose()
        }
    }
    finally { $g.Dispose() }

    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    return , $stream.ToArray()
}

$frames = foreach ($size in $Sizes) { , @{ Size = $size; Png = (New-Frame $size) } }

# Formát ICO: ICONDIR (6 B) + ICONDIRENTRY (16 B) za každý snímek + data snímků.
$out = [System.IO.MemoryStream]::new()
$w = [System.IO.BinaryWriter]::new($out)
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($f in $frames) {
    $dim = if ($f.Size -ge 256) { 0 } else { $f.Size } # 0 znamená 256
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$f.Png.Length); $w.Write([uint32]$offset)
    $offset += $f.Png.Length
}
foreach ($f in $frames) { $w.Write($f.Png) }
$w.Flush()

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.File]::WriteAllBytes($OutputPath, $out.ToArray())
Write-Host "Uloženo $OutputPath ($($frames.Count) velikostí: $($Sizes -join ', ') px, $($out.Length) B)"
