<#
.SYNOPSIS
    Test that the offline MiKTeX bundle renders TikZ WITHOUT Internet.

.DESCRIPTION
    Compiles a small TikZ document using the exact same preamble as TH_ExamGen,
    converts it to PNG (300 DPI) with pdftoppm, and writes the result to -OutDir.
    Open the PNG to confirm shapes AND Vietnamese accents render correctly.

.PARAMETER BinDir
    MiKTeX bin folder. Empty = auto-detect inside .\bundle.

.PARAMETER OutDir
    Where to write the test PNG. Default: <script folder>\test-output

.PARAMETER Show
    Open the produced PNG in the default viewer.

.EXAMPLE
    .\Test-TikzOffline.ps1 -Show
#>
[CmdletBinding()]
param(
    [string]$BinDir,
    [string]$OutDir = (Join-Path $PSScriptRoot 'test-output'),
    [switch]$Show
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

if (-not $BinDir) {
    $found = Get-ChildItem -Path (Join-Path $PSScriptRoot 'bundle') -Recurse -Filter 'pdflatex.exe' -ErrorAction SilentlyContinue |
             Select-Object -First 1
    if ($found) { $BinDir = $found.DirectoryName }
}
if (-not $BinDir) {
    $txt = Join-Path $PSScriptRoot 'bundle\binpath.txt'
    if (Test-Path $txt) { $BinDir = (Get-Content $txt -Raw).Trim() }
}
if (-not $BinDir) { Die "Khong tim thay thu muc bin. Hay chi dinh -BinDir <...>" }
$BinDir = [System.IO.Path]::GetFullPath($BinDir)
if (-not (Test-Path (Join-Path $BinDir 'pdflatex.exe'))) { Die "Khong thay pdflatex.exe trong: $BinDir" }

# Vietnamese accents via LaTeX macros (dung nhu app xuat ra), de kiem tra font T1.
$tex = @'
\documentclass[tikz,border=6pt]{standalone}
\usepackage[T1]{fontenc}
\usepackage[utf8]{inputenc}
\usepackage{tikz}
\usepackage{graphicx}
\usepackage{amsmath,amssymb}
\usetikzlibrary{calc,shapes.geometric,arrows,patterns,positioning,decorations.pathreplacing}
\providecommand{\ohorn}{o\kern-0.08em\raisebox{0.5ex}{\scalebox{0.6}{'}}}
\providecommand{\Ohorn}{O\kern-0.08em\raisebox{0.5ex}{\scalebox{0.6}{'}}}
\providecommand{\uhorn}{u\kern-0.08em\raisebox{0.5ex}{\scalebox{0.6}{'}}}
\providecommand{\Uhorn}{U\kern-0.08em\raisebox{0.5ex}{\scalebox{0.6}{'}}}
\begin{document}
\begin{tikzpicture}
  \draw[thick] (0,0) rectangle (4,2);
  \node at (2,1) {h\`{i}nh ch\~{\uhorn{}} nh\d{\^{a}}t};
  \node[draw,circle,minimum size=1.2cm] at (6,1) {};
  \node at (6,1) {h\`{i}nh tr\`{o}n};
  \draw (8,0) -- (10,0) -- (9,2) -- cycle;
  \node[below] at (9,0) {tam gi\'{a}c};
  \node at (4,-1) {$\dfrac{1}{2}$ \; \^{a} \'{e} \`{o} \~{u} \dj{} \uhorn{} \ohorn{}};
\end{tikzpicture}
\end{document}
'@

$work = Join-Path ([System.IO.Path]::GetTempPath()) ('thexam_test_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $work | Out-Null
Set-Content -Path (Join-Path $work 'test.tex') -Value $tex -Encoding UTF8

Info "Bien dich (pdflatex) ..."
Push-Location $work
try {
    for ($i = 1; $i -le 2; $i++) {
        Invoke-Native -Exe (Join-Path $BinDir 'pdflatex.exe') `
                      -ExeArgs @('-interaction=nonstopmode','test.tex') `
                      -LogPath (Join-Path $work "run$i.log") | Out-Null
    }
} finally { Pop-Location }

if (-not (Test-Path (Join-Path $work 'test.pdf'))) {
    Warn "pdflatex KHONG tao duoc PDF. Xem log: $work\run2.log"
    Get-Content (Join-Path $work 'run2.log') -Tail 30 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    $_" }
    Die "Test THAT BAI."
}
Ok "pdflatex OK."

Info "Chuyen PDF -> PNG (pdftoppm -r 300) ..."
Push-Location $work
try {
    Invoke-Native -Exe (Join-Path $BinDir 'pdftoppm.exe') `
                  -ExeArgs @('-png','-r','300','test.pdf','out') `
                  -LogPath (Join-Path $work 'ppm.log') | Out-Null
} finally { Pop-Location }

$png = Get-ChildItem $work -Filter 'out*.png' -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $png) {
    Warn "pdftoppm KHONG tao duoc PNG. Xem log: $work\ppm.log"
    Die "Test THAT BAI (thieu pdftoppm?)."
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$dest = Join-Path $OutDir 'tikz-test.png'
Copy-Item $png.FullName $dest -Force
Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Ok "TEST THANH CONG. Anh: $dest"
Write-Host "Hay mo anh de kiem tra hinh ve + dau tieng Viet." -ForegroundColor Yellow
if ($Show) { Start-Process $dest }
