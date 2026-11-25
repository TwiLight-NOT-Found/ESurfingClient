$ErrorActionPreference = 'Stop'
try { Add-Type -AssemblyName System.Drawing } catch { Add-Type -AssemblyName System.Drawing.Common }

$dir = Split-Path -Parent $PSScriptRoot
$src = Join-Path $PSScriptRoot 'icon256.png'
$dst = Join-Path $dir 'icon.ico'

$bmp = [System.Drawing.Bitmap]::FromFile($src)
Write-Host ("源图: {0}x{1} 格式={2}" -f $bmp.Width, $bmp.Height, $bmp.PixelFormat)

# 四角 alpha 验证透明背景
$a1 = $bmp.GetPixel(0,0).A
$a2 = $bmp.GetPixel(255,0).A
$a3 = $bmp.GetPixel(0,255).A
$a4 = $bmp.GetPixel(255,255).A
$c = $bmp.GetPixel(128,128)
Write-Host ("四角 alpha: {0},{1},{2},{3}  中心像素 R={4} G={5} B={6} A={7}" -f $a1,$a2,$a3,$a4,$c.R,$c.G,$c.B,$c.A)

$sizes = @(16,20,24,32,40,48,64,128,256)
$pngs = @()
foreach($s in $sizes){
    $nb = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($nb)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($bmp, 0, 0, $s, $s)
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $nb.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,$ms.ToArray()
    $ms.Dispose()
    $nb.Dispose()
}

$count = $sizes.Count
$headerSize = 6
$entrySize = 16
$offset = $headerSize + $entrySize * $count

$offsets = @()
$cur = $offset
foreach($p in $pngs){ $offsets += $cur; $cur += $p.Length }

$ms2 = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms2)
$bw.Write([UInt16]0)     # reserved
$bw.Write([UInt16]1)     # type = icon
$bw.Write([UInt16]$count)
for($i=0; $i -lt $count; $i++){
    $s = $sizes[$i]
    $w = if($s -ge 256){0}else{$s}
    $h = if($s -ge 256){0}else{$s}
    $bw.Write([Byte]$w)
    $bw.Write([Byte]$h)
    $bw.Write([Byte]0)          # color count
    $bw.Write([Byte]0)          # reserved
    $bw.Write([UInt16]1)        # planes
    $bw.Write([UInt16]32)       # bit count
    $bw.Write([UInt32]$pngs[$i].Length)
    $bw.Write([UInt32]$offsets[$i])
}
foreach($p in $pngs){ $bw.Write($p) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($dst, $ms2.ToArray())
$bw.Dispose(); $ms2.Dispose(); $bmp.Dispose()
Write-Host ("ICO 生成成功: {0} 字节, 包含 {1} 个尺寸 {2}" -f (Get-Item $dst).Length, $count, ($sizes -join ','))
