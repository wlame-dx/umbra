# Umbra - derleme betigi
# Windows ile gelen .NET Framework C# derleyicisini kullanir; ek kurulum gerekmez.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$cscCandidates = @(
  "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
  "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) { throw "csc.exe bulunamadi. .NET Framework 4.x kurulu olmali." }

$fw  = Split-Path -Parent $csc
$wpf = Join-Path $fw "WPF"

$outDir = Join-Path $root "dist"
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir | Out-Null

$refs = @(
  (Join-Path $fw  "System.dll"),
  (Join-Path $fw  "System.Core.dll"),
  (Join-Path $fw  "System.Xml.dll"),
  (Join-Path $fw  "System.Web.Extensions.dll"),
  (Join-Path $fw  "System.Xaml.dll"),
  (Join-Path $wpf "WindowsBase.dll"),
  (Join-Path $wpf "PresentationCore.dll"),
  (Join-Path $wpf "PresentationFramework.dll")
)
foreach ($r in $refs) { if (-not (Test-Path $r)) { throw "Referans bulunamadi: $r" } }

$sources = Get-ChildItem (Join-Path $root "src") -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
if (-not $sources) { throw "Kaynak dosya bulunamadi (src klasoru bos)." }

$cargs = New-Object System.Collections.Generic.List[string]
$cargs.Add("/nologo")
$cargs.Add("/target:winexe")
$cargs.Add("/platform:anycpu")
$cargs.Add("/optimize+")
$cargs.Add("/codepage:65001")
$cargs.Add("/win32manifest:" + (Join-Path $root "app.manifest"))
$cargs.Add("/out:" + (Join-Path $outDir "Umbra.exe"))
$icon = Join-Path $root "assets\umbra.ico"
if (Test-Path $icon) { $cargs.Add("/win32icon:" + $icon) }
else { Write-Host "UYARI: assets\umbra.ico yok; tools\make-icon.ps1 calistirilmali." -ForegroundColor Yellow }
foreach ($r in $refs) { $cargs.Add("/reference:" + $r) }
$cargs.Add("/resource:" + (Join-Path $root "ui\MainWindow.xaml") + ",ui.MainWindow.xaml")
Get-ChildItem (Join-Path $root "wwwroot") -File | ForEach-Object {
  $cargs.Add("/resource:" + $_.FullName + ",wwwroot." + $_.Name)
}
if (Test-Path (Join-Path $root "assets")) {
  Get-ChildItem (Join-Path $root "assets") -File | ForEach-Object {
    $cargs.Add("/resource:" + $_.FullName + ",assets." + $_.Name)
  }
}
foreach ($s in $sources) { $cargs.Add($s) }

Write-Host "Derleniyor..." -ForegroundColor Cyan
& $csc $cargs.ToArray()
if ($LASTEXITCODE -ne 0) { throw ("Derleme basarisiz (kod " + $LASTEXITCODE + ").") }

# Duzenlenebilir kopyalar (exe calisirken once bunlari kullanir, yoksa gomulu surumu kullanir)
Copy-Item (Join-Path $root "ui")      -Destination (Join-Path $outDir "ui")      -Recurse -Force
Copy-Item (Join-Path $root "wwwroot") -Destination (Join-Path $outDir "wwwroot") -Recurse -Force
if (Test-Path (Join-Path $root "assets")) {
  Copy-Item (Join-Path $root "assets") -Destination (Join-Path $outDir "assets") -Recurse -Force
}

# Sanal ekran yardimci betikleri (varsa) exe'nin yanina kopyalanir
if (Test-Path (Join-Path $root "vdriver")) {
  Copy-Item (Join-Path $root "vdriver") -Destination (Join-Path $outDir "vdriver") -Recurse -Force
}

Write-Host ("Tamamlandi: " + (Join-Path $outDir "Umbra.exe")) -ForegroundColor Green
