$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $PSScriptRoot
$edge = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
$tmp = Join-Path $dir 'edge-tmp'
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

$icons = @(
  @{ name='open'; color='#2563EB'; svg='<path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/>' },
  @{ name='exit'; color='#DC2626'; svg='<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/>' }
)

foreach($ic in $icons){
    $html = "<!DOCTYPE html><html><head><meta charset='utf-8'><style>html,body{margin:0;padding:0;background:transparent;width:20px;height:20px;overflow:hidden}svg{width:20px;height:20px;display:block}</style></head><body><svg viewBox='0 0 24 24' fill='none' stroke='$($ic.color)' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' width='20' height='20'>$($ic.svg)</svg></body></html>"
    $hpath = Join-Path $tmp ($ic.name + '.html')
    $ppath = Join-Path $dir ($ic.name + '.png')
    [System.IO.File]::WriteAllText($hpath, $html)
    $uri = "file:///" + ($hpath -replace '\\','/')
    & $edge --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 --default-background-color=00000000 --window-size=20,20 --user-data-dir="$tmp\prof-$($ic.name)" --no-first-run --screenshot="$ppath" "$uri" 2>&1 | Out-Null
    Start-Sleep -Milliseconds 500
}
Start-Sleep -Seconds 1
foreach($ic in $icons){
    $ppath = Join-Path $dir ($ic.name + '.png')
    if(Test-Path $ppath){ Write-Host ("{0}: {1} 字节" -f $ic.name, (Get-Item $ppath).Length) } else { Write-Host ("{0}: 失败" -f $ic.name) }
}
