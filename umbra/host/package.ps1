# Umbra - paketleme betigi
# Hem tasinabilir (portable) zip hem de kurulum dosyasi (Setup.exe) uretir.
#
# Kullanim:
#   powershell -ExecutionPolicy Bypass -File package.ps1
#   powershell -ExecutionPolicy Bypass -File package.ps1 -Version 1.0.1

param(
  [string]$Version = "1.0.0"
)

$ErrorActionPreference = 'Stop'

$root  = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist  = Join-Path $root "dist"
$build = Join-Path $root "build"
$setupDir = (Resolve-Path (Join-Path $root "..\setup")).Path

Write-Host "== Umbra paketleme  surum $Version ==" -ForegroundColor Cyan

# ------------------------------------------------------------------ 1) derle
& (Join-Path $root "build.ps1")
if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) {
  # build.ps1 hata durumunda throw eder; buraya genelde gelinmez.
}

if (-not (Test-Path (Join-Path $dist "Umbra.exe"))) {
  throw "dist\Umbra.exe bulunamadi; derleme basarisiz."
}

# ------------------------------------------------------------------ 2) sahneleme
if (Test-Path $build) { Remove-Item $build -Recurse -Force }
$payload = Join-Path $build "payload"
New-Item -ItemType Directory -Path $payload | Out-Null

# Uygulama dosyalari (exe'nin yanindaki duzenlenebilir kopyalar dahil)
Copy-Item (Join-Path $dist "Umbra.exe") $payload -Force
foreach ($folder in @("ui", "wwwroot", "assets", "vdriver")) {
  $src = Join-Path $dist $folder
  if (Test-Path $src) { Copy-Item $src -Destination (Join-Path $payload $folder) -Recurse -Force }
}

# Belgeler
if (Test-Path (Join-Path $root "..\..\README.md"))   { Copy-Item (Join-Path $root "..\..\README.md")   (Join-Path $payload "README.md")  -Force }
if (Test-Path (Join-Path $root "..\..\LICENSE"))     { Copy-Item (Join-Path $root "..\..\LICENSE")     (Join-Path $payload "LICENSE")    -Force }
if (Test-Path (Join-Path $root "..\site"))           {
  # site klasoru pakete dahil edilmez; yalnizca uygulama dagitilir.
}

# ------------------------------------------------------------------ 3) tasinabilir zip (Umbra/ ust klasoru ile)
Add-Type -AssemblyName System.IO.Compression.FileSystem

$portableStage = Join-Path $build "portable"
$portableInner = Join-Path $portableStage "Umbra"
New-Item -ItemType Directory -Path $portableInner | Out-Null
Copy-Item (Join-Path $payload "*") $portableInner -Recurse -Force

$portableZip = Join-Path $dist ("Umbra-Portable-" + $Version + ".zip")
if (Test-Path $portableZip) { Remove-Item $portableZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($portableStage, $portableZip)

# ------------------------------------------------------------------ 4) gomulu yuk (payload.zip, kok dosyalar)
$payloadZip = Join-Path $build "payload.zip"
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($payload, $payloadZip)

# ------------------------------------------------------------------ 5) Setup.exe
$cscCandidates = @(
  "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
  "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) { throw "csc.exe bulunamadi." }
$fw  = Split-Path -Parent $csc
$wpf = Join-Path $fw "WPF"

# Surum sabitini Setup.cs'in yanina yaz.
$buildInfo = @"
namespace UmbraSetup
{
    internal static class BuildInfo
    {
        public const string Version = "$Version";
    }
}
"@
$buildInfoPath = Join-Path $setupDir "BuildInfo.g.cs"
[System.IO.File]::WriteAllText($buildInfoPath, $buildInfo, (New-Object System.Text.UTF8Encoding($false)))

$setupOut = Join-Path $dist ("Umbra-Setup-" + $Version + ".exe")
$refs = @(
  (Join-Path $fw  "System.dll"),
  (Join-Path $fw  "System.Core.dll"),
  (Join-Path $fw  "System.Xml.dll"),
  (Join-Path $fw  "System.IO.Compression.dll"),
  (Join-Path $fw  "System.Xaml.dll"),
  (Join-Path $wpf "WindowsBase.dll"),
  (Join-Path $wpf "PresentationCore.dll"),
  (Join-Path $wpf "PresentationFramework.dll")
)

$cargs = New-Object System.Collections.Generic.List[string]
$cargs.Add("/nologo")
$cargs.Add("/target:winexe")
$cargs.Add("/platform:anycpu")
$cargs.Add("/optimize+")
$cargs.Add("/codepage:65001")
$cargs.Add("/define:UMBRA_VERSION")
$cargs.Add("/out:$setupOut")
$icon = Join-Path $root "assets\umbra.ico"
if (Test-Path $icon) { $cargs.Add("/win32icon:" + $icon) }
foreach ($r in $refs) { $cargs.Add("/reference:" + $r) }
$cargs.Add("/resource:$payloadZip,UmbraSetup.Payload.zip")
$cargs.Add((Join-Path $setupDir "Setup.cs"))
$cargs.Add($buildInfoPath)

Write-Host "Kurulum derleniyor..." -ForegroundColor Cyan
& $csc $cargs.ToArray()
if ($LASTEXITCODE -ne 0) { throw ("Kurulum derlenemedi (kod " + $LASTEXITCODE + ").") }

# Gecici sahneleme dosyalarini temizle (payload.zip ve .g.cs kalsin istemiyoruz)
Remove-Item $build -Recurse -Force
Remove-Item $buildInfoPath -Force

Write-Host ""
Write-Host "Hazir:" -ForegroundColor Green
Write-Host ("  Tasinabilir : " + $portableZip + "   (" + [math]::Round((Get-Item $portableZip).Length / 1KB) + " KB)")
Write-Host ("  Kurulum     : " + $setupOut + "   (" + [math]::Round((Get-Item $setupOut).Length / 1KB) + " KB)")
