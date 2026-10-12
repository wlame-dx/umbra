# Umbra - sanal ekran surucusu indirici
#
# Acik kaynak "Virtual Display Driver" (VDD) surucusunu indirir ve
# vdriver\bin klasorune yerlestirir. Boylece Umbra yayina baslarken
# bu surucuyu kurup sanal (ikinci) ekrani acabilir.
#
# Kullanim:
#   powershell -ExecutionPolicy Bypass -File get-driver.ps1
#
# NOT: Surucu kurulumu yonetici hakki gerektirir ve Windows bir sertifika
# onayi isteyebilir. Bu betik yalnizca dosyalari indirir; kurulumu Umbra
# yayina baslarken kendisi yapar (create.cmd).

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$bin  = Join-Path $here 'bin'

New-Item -ItemType Directory -Path $bin -Force | Out-Null

Write-Host "[Umbra] Surucu surumu araniyor..." -ForegroundColor Cyan

$api = 'https://api.github.com/repos/itsmikethetech/Virtual-Display-Driver/releases/latest'
$headers = @{ 'User-Agent' = 'Umbra-Setup' }

try {
    $release = Invoke-RestMethod -Uri $api -Headers $headers -UseBasicParsing
} catch {
    Write-Host "Surum bilgisi alinamadi: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Elle indirme: https://github.com/itsmikethetech/Virtual-Display-Driver/releases"
    exit 1
}

$asset = $release.assets | Where-Object { $_.name -like '*.zip' } | Select-Object -First 1
if (-not $asset) {
    Write-Host "Uygun bir zip bulunamadi. Elle indirme:" -ForegroundColor Red
    Write-Host $release.html_url
    exit 1
}

$zip = Join-Path $env:TEMP $asset.name
Write-Host "[Umbra] Indiriliyor: $($asset.name)" -ForegroundColor Cyan
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing

$tmp = Join-Path $env:TEMP ('umbra-vdd-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
Expand-Archive -Path $zip -DestinationPath $tmp -Force

Write-Host "[Umbra] Dosyalar kopyalaniyor..." -ForegroundColor Cyan
$patterns = @('*.inf', '*.sys', '*.cer', '*.cat', '*.dll', '*.pdb', '*.xml')
foreach ($p in $patterns) {
    Get-ChildItem -Path $tmp -Recurse -Filter $p -File -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item $_.FullName -Destination $bin -Force
    }
}

Remove-Item $zip -Force -ErrorAction SilentlyContinue
Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue

if (-not (Get-ChildItem -Path $bin -Filter '*.inf' -File -ErrorAction SilentlyContinue)) {
    Write-Host "Kopyalama sonrasi .inf bulunamadi. Surucu paketi farkli olabilir." -ForegroundColor Yellow
    exit 1
}

Write-Host "[Umbra] Tamam. Surucu dosyalari: $bin" -ForegroundColor Green
Write-Host "[Umbra] Simdi Umbra'yi acip 'Sanal ekrani otomatik olustur' secenegiyle yayini baslatin." -ForegroundColor Green
