# ESurfingTray 一键编译脚本
# 使用 Windows 自带的 .NET Framework csc.exe，无需安装 SDK
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot

# 定位 csc.exe（64 位优先）
$csc64 = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$csc32 = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$csc = if (Test-Path $csc64) { $csc64 } elseif (Test-Path $csc32) { $csc32 } else { throw '未找到 .NET Framework csc.exe' }

$out  = Join-Path $root 'ESurfingTray.exe'
$cs   = Join-Path $root 'Program.cs'
$icon = Join-Path $root 'icon.ico'
$open = Join-Path $root 'open.png'
$exit = Join-Path $root 'exit.png'

$cscArgs = @(
    '/nologo', '/target:winexe', "/out:$out",
    "/win32icon:$icon",
    "/resource:$icon,ESurfingTray.icon.ico",
    "/resource:$open,ESurfingTray.open.png",
    "/resource:$exit,ESurfingTray.exit.png",
    '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll',
    $cs
)

Write-Host "编译 ESurfingTray..."
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码 $LASTEXITCODE" }

$size = [math]::Round((Get-Item $out).Length / 1KB, 1)
Write-Host "编译成功: $out ($size KB)"
