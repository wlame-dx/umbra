# Umbra - hizli calistirma
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe  = Join-Path $root "dist\Umbra.exe"
if (-not (Test-Path $exe)) {
  Write-Host "Onbellekte yok, once derleniyor..." -ForegroundColor Yellow
  & (Join-Path $root "build.ps1")
}
Start-Process -FilePath $exe
