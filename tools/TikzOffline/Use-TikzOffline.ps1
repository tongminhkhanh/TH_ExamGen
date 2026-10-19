<#
.SYNOPSIS
    Tro TH_ExamGen dung bo MiKTeX offline (thay vi phai co Internet/Kroki).

.DESCRIPTION
    Ghi duong dan thu muc bin cua MiKTeX vao 'MiktexBinPath' trong appsettings.json
    cua ung dung, de app render TikZ bang pdflatex + pdftoppm cuc bo.

    Thu muc bin phai chua CA HAI file: pdflatex.exe va pdftoppm.exe.

.PARAMETER AppDir
    Thu muc chua TH_ExamGen.UI.exe va appsettings.json (thuong la thu muc 'publish').

.PARAMETER BinDir
    Thu muc bin cua MiKTeX. Bo trong = tu tim trong .\bundle.

.PARAMETER Revert
    Xoa cau hinh (dat MiktexBinPath = "") => app quay ve dung Kroki (can Internet).

.EXAMPLE
    .\Use-TikzOffline.ps1 -AppDir 'C:\Users\Teacher\Desktop\TH_ExamGen\publish'
    .\Use-TikzOffline.ps1 -AppDir . -BinDir 'D:\TH_ExamGen_TikZ\bundle\miktex\texmfs\install\miktex\bin\x64'
    .\Use-TikzOffline.ps1 -AppDir . -Revert
#>
[CmdletBinding()]
param(
    [string]$AppDir,
    [string]$BinDir,
    [switch]$Revert
)

$ErrorActionPreference = 'Stop'

function Info($m) { Write-Host "[*] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "[+] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "[!] $m" -ForegroundColor Yellow }
function Die($m)  { Write-Host "[x] $m" -ForegroundColor Red; exit 1 }

# --- Run a native program without letting its stderr abort the script.
# MiKTeX prints harmless warnings on stderr ("security risk: running with
# elevated privileges"); with $ErrorActionPreference = 'Stop' PowerShell would
# treat those as fatal. Returns the exit code.
function Invoke-Native {
    param([string]$Exe, [string[]]$ExeArgs, [string]$LogPath)
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($LogPath) { & $Exe @ExeArgs *> $LogPath }
        else          { & $Exe @ExeArgs 2>&1 | ForEach-Object { Write-Host $_ } }
    } finally { $ErrorActionPreference = $prev }
    return $LASTEXITCODE
}

# ------------------------------------------------- tim appsettings.json
function Find-AppSettings([string]$dir) {
    if ([string]::IsNullOrWhiteSpace($dir)) { return $null }
    $p = Join-Path $dir 'appsettings.json'
    if (Test-Path $p) { return $p }
    return $null
}

if ($AppDir) {
    $settingsPath = Find-AppSettings $AppDir
    if (-not $settingsPath) { Die "Khong thay appsettings.json trong '$AppDir'." }
} else {
    # Thu tim trong vai noi quen thuoc
    $candidates = @(
        $PSScriptRoot,
        (Join-Path $PSScriptRoot '..'),
        (Join-Path $PSScriptRoot '..\publish'),
        (Join-Path $PSScriptRoot 'publish'),
        (Split-Path $PSScriptRoot -Parent),
        'C:\Users\Administrator\Desktop\D\publish'
    ) | Where-Object { $_ }
    $settingsPath = $null
    foreach ($c in $candidates) {
        $settingsPath = Find-AppSettings $c
        if ($settingsPath) { break }
    }
    if (-not $settingsPath) {
        Die "Khong tu tim thay appsettings.json. Hay chay lai voi -AppDir <thu muc publish>."
    }
}
Info "appsettings.json: $settingsPath"

# ------------------------------------------------- doc JSON
$json = Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8
try { $obj = $json | ConvertFrom-Json } catch { Die "appsettings.json khong phai JSON hop le." }

# ------------------------------------------------- che do khoi phuc
if ($Revert) {
    $old = $obj.MiktexBinPath
    $obj.MiktexBinPath = ''
    ($obj | ConvertTo-Json -Depth 20) | Set-Content -LiteralPath $settingsPath -Encoding UTF8
    Ok "Da xoa cau hinh (truoc do: '$old'). App se dung Kroki (can Internet) khi render."
    exit 0
}

# ------------------------------------------------- xac dinh BinDir
if (-not $BinDir) {
    $found = Get-ChildItem -Path (Join-Path $PSScriptRoot 'bundle') -Recurse -Filter 'pdflatex.exe' -ErrorAction SilentlyContinue |
             Select-Object -First 1
    if ($found) { $BinDir = $found.DirectoryName }
}
if (-not $BinDir) {
    $txt = Join-Path $PSScriptRoot 'bundle\binpath.txt'
    if (Test-Path $txt) { $BinDir = (Get-Content $txt -Raw).Trim() }
}
if (-not $BinDir) { Die "Khong tim thay thu muc bin MiKTeX. Hay chi dinh -BinDir <...>" }

$BinDir = [System.IO.Path]::GetFullPath($BinDir)
if (-not (Test-Path $BinDir)) { Die "Thu muc bin khong ton tai: $BinDir" }

$pdflatex = Join-Path $BinDir 'pdflatex.exe'
$pdftoppm = Join-Path $BinDir 'pdftoppm.exe'
if (-not (Test-Path $pdflatex)) { Die "KHONG thay pdflatex.exe trong: $BinDir" }
if (-not (Test-Path $pdftoppm)) {
    Warn "KHONG thay pdftoppm.exe trong: $BinDir"
    Warn "App can CA HAI. Xem lai Build-TikzOffline.ps1 (buoc poppler)."
    Die  "Thieu pdftoppm.exe."
}
Ok "pdflatex.exe + pdftoppm.exe: OK"

# ------------------------------------------------- cap nhat FNDB (sau khi doi o dia/thu muc)
# MiKTeX luu chi muc ten file; khi copy sang o/thu muc khac can cap nhat lai (cuc bo, khong can mang).
$initexmf = Join-Path $BinDir 'initexmf.exe'
if (Test-Path $initexmf) {
    Info "Cap nhat co so du lieu ten file (initexmf --update-fndb) ..."
    $fndbLog = Join-Path ([System.IO.Path]::GetTempPath()) 'thexam_fndb.log'
    $rc = Invoke-Native -Exe $initexmf -ExeArgs @('--update-fndb') -LogPath $fndbLog
    if ($rc -eq 0) { Ok "FNDB da cap nhat." }
    else {
        Warn "Khong cap nhat duoc FNDB (exit $rc - khong nghiem trong)."
        Get-Content $fndbLog -Tail 10 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    $_" }
    }
}

# ------------------------------------------------- ghi cau hinh
$old = $obj.MiktexBinPath
$obj.MiktexBinPath = $BinDir
($obj | ConvertTo-Json -Depth 20) | Set-Content -LiteralPath $settingsPath -Encoding UTF8

Write-Host ""
Ok "Da tro TH_ExamGen vao bo MiKTeX offline."
Write-Host "    MiktexBinPath (cu): $old" -ForegroundColor White
Write-Host "    MiktexBinPath (moi): $BinDir" -ForegroundColor White
Write-Host ""
Write-Host "Mo lai TH_ExamGen de ap dung. Tu nay render TikZ KHONG can Internet." -ForegroundColor Yellow
Write-Host "(Neu muon quay lai dung Kroki: chay lai script nay voi -Revert)" -ForegroundColor DarkGray
