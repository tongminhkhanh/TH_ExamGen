<#
.SYNOPSIS
    Build an OFFLINE MiKTeX bundle so TH_ExamGen can draw TikZ figures without Internet.

.DESCRIPTION
    Run this script on a machine that HAS Internet (and enough free disk space).
    It will:
      1. Download the MiKTeX installer bootstrapper (miktexsetup).
      2. Install MiKTeX Portable (no registry writes, no PATH changes) into -TargetDir.
      3. Warm up: compile a sample TikZ document identical to what the app uses,
         so MiKTeX downloads every required package and font.
      4. Make sure pdftoppm.exe is present (needed to convert PDF -> PNG).
      5. Turn OFF package auto-install (AutoInstall=0) so offline runs never hang.
      6. Verify again: recompile with auto-install off; it must still succeed.

    Then copy the resulting folder to a USB stick, copy it onto the school machine,
    and run Use-TikzOffline.ps1 there to point the app at this bundle.

.PARAMETER TargetDir
    Where to place the MiKTeX bundle. Default: <script folder>\bundle\miktex
    Prefer a drive/USB with lots of space (needs ~1.5 GB while building).

.PARAMETER PackageSet
    essential | basic | complete. Default 'basic' (stable, sufficient).

.PARAMETER Mirror
    MiKTeX package repository URL (empty = pick a nearby mirror automatically).

.PARAMETER SkipWarmup
    Skip the warm-up step (NOT recommended: the bundle will not work offline).

.EXAMPLE
    .\Build-TikzOffline.ps1
    .\Build-TikzOffline.ps1 -TargetDir E:\TH_ExamGen_TikZ\miktex -PackageSet basic
#>
[CmdletBinding()]
param(
    [string]$TargetDir = (Join-Path $PSScriptRoot 'bundle\miktex'),
    [ValidateSet('essential','basic','complete')]
    [string]$PackageSet = 'basic',
    [string]$Mirror = '',
    [string]$CacheDir = (Join-Path $PSScriptRoot '.cache'),
    [switch]$SkipWarmup
)

$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

function Info($m) { Write-Host "[*] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "[+] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "[!] $m" -ForegroundColor Yellow }
function Die($m)  { Write-Host "[x] $m" -ForegroundColor Red; exit 1 }

# --- Run a native program without letting its stderr abort the script.
# MiKTeX prints harmless warnings on stderr ("security risk: running with
# elevated privileges", "not checked for updates"). Because the script sets
# $ErrorActionPreference = 'Stop', PowerShell would treat those warnings as
# fatal errors and kill the build. This wrapper runs the program with a local
# 'Continue' preference, collects its output, and returns its exit code.
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

# --- TikZ preamble exactly as used by TH_ExamGen (TikzRenderService.NormalizeTikzDocument) ---
$WarmupTex = @'
\documentclass[tikz,border=4pt]{standalone}
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
  % rectangle + fraction + Vietnamese accents (horn/breve/circumflex/dot-below)
  \draw (0,0) rectangle (4,2);
  \node at (2,1) {$\frac{1}{2}$ \; h\`{i}nh ch\~{\uhorn{}} nh\d{\^{a}}t \uhorn{} \ohorn{} \dj{}};
  % circle and diamond (shapes.geometric)
  \node[draw,circle,minimum size=1cm] at (6,1) {};
  \node[draw,diamond] at (8,1) {A};
  % arrow (arrows)
  \draw[->,>=stealth] (0,-1) -- (2,-1);
  % fill pattern (patterns)
  \draw[pattern=north east lines] (0,-3) rectangle (2,-2);
  % brace (decorations.pathreplacing)
  \draw[decorate,decoration={brace,amplitude=5pt}] (0,-4) -- (2,-4);
  % positioning + calc
  \node (A1) at (10,0) {A};
  \node[below=1cm of A1] (A2) {B};
  \draw (A1) -- (A2);
  \coordinate (P) at (14,1);
  \draw let \p1=(P) in (14,0) circle (\x1);
  % triangle
  \draw (16,0) -- (18,0) -- (17,2) -- cycle;
\end{tikzpicture}
\end{document}
'@

# ------------------------------------------------------------------ checks
$absTarget = [System.IO.Path]::GetFullPath($TargetDir)

Info "Target folder: $absTarget"

$qualifier = (Split-Path -Qualifier $absTarget)
try {
    $drive   = Get-PSDrive ($qualifier.TrimEnd(':'))
    $freeGB  = [math]::Round($drive.Free / 1GB, 2)
    Info "Drive $qualifier free space: $freeGB GB"
    if ($freeGB -lt 1.5) {
        Warn "WARNING: only $freeGB GB free. A 'basic' bundle needs ~1.5 GB while building."
        Warn "Use -TargetDir pointing to another drive/USB, or free up space first."
    }
} catch { Warn "Could not read free space for $qualifier" }

# ------------------------------------------------------- download bootstrapper
New-Item -ItemType Directory -Force -Path $CacheDir | Out-Null
$bootExe = Join-Path $CacheDir 'miktexsetup_standalone.exe'

if (-not (Test-Path $bootExe)) {
    $zip = Join-Path $CacheDir 'miktexsetup-x64.zip'
    Info "Downloading miktexsetup from miktex.org ..."
    Invoke-WebRequest -Uri 'https://miktex.org/download/win/miktexsetup-x64.zip' `
                      -OutFile $zip -UseBasicParsing -TimeoutSec 120
    Expand-Archive -Path $zip -DestinationPath $CacheDir -Force
}
if (Test-Path $bootExe) { Ok "Bootstrapper: $bootExe" }
else { Die "Could not obtain miktexsetup. Check your Internet connection." }

# ------------------------------------------------------- install MiKTeX portable
New-Item -ItemType Directory -Force -Path $absTarget | Out-Null

# Idempotent: if MiKTeX is already installed here, skip the (slow) download/install.
$existing = Get-ChildItem -Path $absTarget -Recurse -Filter 'pdflatex.exe' -ErrorAction SilentlyContinue |
            Select-Object -First 1
if ($existing) {
    Ok "MiKTeX already present in target - skipping download/install."
} else {
    $commonArgs = @(
        "--portable=$absTarget",
        "--package-set=$PackageSet",
        "--modify-path=no",
        "--use-registry=no"
    )
    if ($Mirror) { $commonArgs += "--remote-package-repository=$Mirror" }

    Info "Step 1/2: download packages (package-set=$PackageSet) ..."
    $rc = Invoke-Native -Exe $bootExe -ExeArgs ($commonArgs + 'download')
    if ($rc -ne 0) { Die "Step 'download' failed (exit $rc)." }

    Info "Step 2/2: install ..."
    $rc = Invoke-Native -Exe $bootExe -ExeArgs ($commonArgs + 'install')
    if ($rc -ne 0) { Die "Step 'install' failed (exit $rc)." }
}

# ------------------------------------------------------- locate bin folder
$pdflatex = Get-ChildItem -Path $absTarget -Recurse -Filter 'pdflatex.exe' -ErrorAction SilentlyContinue |
            Select-Object -First 1
if (-not $pdflatex) { Die "Could not find pdflatex.exe under $absTarget." }
$bin = $pdflatex.DirectoryName
Ok "bin folder: $bin"

# ------------------------------------------------------- warm-up (fetch packages/fonts)
$work = $null
if (-not $SkipWarmup) {
    Info "Warm-up: test-compiling to fetch every needed package/font ..."
    $work = Join-Path ([System.IO.Path]::GetTempPath()) ('thexam_warmup_' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    Set-Content -Path (Join-Path $work 'warmup.tex') -Value $WarmupTex -Encoding UTF8

    Push-Location $work
    try {
        for ($i = 1; $i -le 3; $i++) {
            Invoke-Native -Exe (Join-Path $bin 'pdflatex.exe') `
                          -ExeArgs @('-interaction=nonstopmode','warmup.tex') `
                          -LogPath (Join-Path $work "run$i.log") | Out-Null
        }
    } finally { Pop-Location }

    if (Test-Path (Join-Path $work 'warmup.pdf')) {
        Ok "Warm-up OK (packages/fonts fetched)."
    } else {
        Warn "Warm-up did NOT produce a PDF. See log: $work\run3.log"
        Get-Content (Join-Path $work 'run3.log') -Tail 25 -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    $_" }
        Die "Warm-up failed - this bundle will NOT render offline."
    }
}

# ------------------------------------------------------- ensure pdftoppm
if (-not (Test-Path (Join-Path $bin 'pdftoppm.exe'))) {
    Info "pdftoppm.exe not found - trying MiKTeX poppler package ..."
    $mpm = Join-Path $bin 'mpm.exe'
    if (Test-Path $mpm) {
        foreach ($pkg in @('miktex-poppler-bin-x64','miktex-poppler-bin')) {
            try { Invoke-Native -Exe $mpm -ExeArgs @("--install=$pkg") | Out-Null } catch { }
            if (Test-Path (Join-Path $bin 'pdftoppm.exe')) { break }
        }
    }
}
if (Test-Path (Join-Path $bin 'pdftoppm.exe')) {
    Ok "pdftoppm.exe present."
} else {
    Warn "pdftoppm.exe NOT found in $bin."
    Warn "Open 'MiKTeX Console' (in $bin) -> Packages -> find 'miktex-poppler-bin-x64' -> Install, then re-run this script."
}

# ------------------------------------------------------- turn OFF auto-install (offline)
Info "Disabling package auto-install (AutoInstall=0) to avoid hanging offline ..."
$rc = Invoke-Native -Exe (Join-Path $bin 'initexmf.exe') -ExeArgs @('--set-config-value=[MPM]AutoInstall=0')
if ($rc -eq 0) { Ok "AutoInstall=0 set." }
else { Warn "Could not set AutoInstall via initexmf (exit $rc; not critical if warm-up succeeded)." }

# ------------------------------------------------------- verify (simulate offline)
if ((-not $SkipWarmup) -and $work) {
    Info "Final check (auto-install is off, so nothing more can be downloaded) ..."
    Remove-Item (Join-Path $work 'warmup.pdf') -Force -ErrorAction SilentlyContinue
    Push-Location $work
    try {
        Invoke-Native -Exe (Join-Path $bin 'pdflatex.exe') `
                      -ExeArgs @('-interaction=nonstopmode','warmup.tex') `
                      -LogPath (Join-Path $work 'verify.log') | Out-Null
    } finally { Pop-Location }

    if (Test-Path (Join-Path $work 'warmup.pdf')) { Ok "Confirmed: compiles with no package download needed." }
    else { Die "Still missing packages. See $work\verify.log" }

    Info "Checking pdftoppm (PDF -> PNG 300 DPI) ..."
    Push-Location $work
    try {
        Invoke-Native -Exe (Join-Path $bin 'pdftoppm.exe') `
                      -ExeArgs @('-png','-r','300','warmup.pdf','out') `
                      -LogPath (Join-Path $work 'ppm.log') | Out-Null
    } finally { Pop-Location }
    if (Get-ChildItem $work -Filter 'out*.png' -ErrorAction SilentlyContinue) { Ok "pdftoppm OK." }
    else { Warn "pdftoppm produced no PNG. See $work\ppm.log" }

    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

# ------------------------------------------------------- result
$sizeGB = [math]::Round(((Get-ChildItem $absTarget -Recurse -File -ErrorAction SilentlyContinue |
            Measure-Object Length -Sum).Sum / 1GB), 2)

$binPathFile = Join-Path $PSScriptRoot 'bundle\binpath.txt'
New-Item -ItemType Directory -Force -Path (Split-Path $binPathFile) | Out-Null
Set-Content -Path $binPathFile -Value $bin -Encoding UTF8

Write-Host ""
Ok "DONE. The offline MiKTeX bundle is ready."
Write-Host "    bin folder (MiktexBinPath):" -ForegroundColor White
Write-Host "      $bin" -ForegroundColor White
Write-Host "    Size: $sizeGB GB" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host " 1) Copy the whole 'TikzOffline' folder to a USB stick."
Write-Host " 2) Copy it onto the school machine (e.g. D:\TH_ExamGen_TikZ)."
Write-Host " 3) On the school machine run:  .\Use-TikzOffline.ps1 -AppDir <publish folder>"
