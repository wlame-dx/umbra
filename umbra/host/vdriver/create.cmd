@echo off
rem ============================================================
rem  Umbra - sanal ekran olusturucu
rem  Yonetim hakki gerekir (Umbra bu betigi "runas" ile cagirir).
rem
rem  Bu betik kernel surucusu ICERMEZ. Acik kaynak bir IDD (Indirect
rem  Display Driver) araci bekler. Desteklenen iki yerlesim:
rem
rem  A) usbmmidd_v2 tarzi  -> vdriver\bin\deviceinstaller64.exe
rem  B) Virtual-Display-Driver tarzi -> vdriver\bin\*.inf + *.sys (+ *.cer)
rem
rem  Cikis kodu: 0 = basarili, digerleri = hata
rem ============================================================
setlocal enabledelayedexpansion
title Umbra - sanal ekran
set "BIN=%~dp0bin"

rem --- A) deviceinstaller varsa ---
if exist "%BIN%\deviceinstaller64.exe" goto :deviceinstaller
if exist "%BIN%\deviceinstaller32.exe" goto :deviceinstaller

rem --- B) inf varsa pnputil ile kur ---
if exist "%BIN%\*.inf" goto :pnputil
goto :missing

:deviceinstaller
if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
  set "INST=%BIN%\deviceinstaller64.exe"
) else (
  set "INST=%BIN%\deviceinstaller32.exe"
)

echo [Umbra] Sanal ekran surucusu kuruluyor (deviceinstaller)...
"%INST%" install
"%INST%" enableidd 1
if errorlevel 1 (
  echo [Umbra] Sanal ekran etkinlestirilemedi.
  exit /b 1
)
echo [Umbra] Sanal ekran etkinlestirildi.
exit /b 0

:pnputil
echo [Umbra] Sanal ekran surucusu kuruluyor (pnputil)...

rem Sertifikayi guvenilir kok deposuna ekle (varsa)
if exist "%BIN%\*.cer" (
  for %%C in ("%BIN%\*.cer") do (
    echo [Umbra] Sertifika guvenilir kok deposuna ekleniyor: %%~nxC
    certutil -addstore -f Root "%%~fC" >nul 2>&1
  )
)

set "ADDED=0"
for %%F in ("%BIN%\*.inf") do (
  echo [Umbra] Surucu ekleniyor: %%~nxF
  pnputil /add-driver "%%~fF" /install
  if !errorlevel! equ 0 set "ADDED=1"
)

if "!ADDED!"=="0" (
  echo [Umbra] Surucu eklenemedi.
  exit /b 1
)
echo [Umbra] Sanal ekran surucusu kuruldu.
echo [Umbra] NOT: Bazi suruculer kurulumdan sonra bir kez yeniden baslatma isteyebilir.
exit /b 0

:missing
echo [Umbra] Sanal ekran surucusu bulunamadi.
echo.
echo   vdriver\bin klasorune acik kaynak bir IDD surucusu kopyalayin.
echo   Kolay yol: vdriver\get-driver.ps1 betigini calistirin.
echo.
echo   Indirme adresleri:
echo     https://github.com/itsmikethetech/Virtual-Display-Driver
echo     https://github.com/ge9/IddSampleDriver
echo.
exit /b 1
