@echo off
rem ============================================================
rem  Umbra - sanal ekran olusturucu
rem  Yonetim hakki gerekir (Umbra bu betigi "runas" ile cagirir).
rem
rem  Bu betik kernel surucusu ICERMEZ. Acik kaynak bir IDD (Indirect
rem  Display Driver) araci bekler. Ornegin "usbmmidd_v2" dosyalarini
rem  bu klasordeki "bin" klasorune kopyalayin:
rem
rem     vdriver\bin\deviceinstaller64.exe
rem     vdriver\bin\usbmmidd.inf
rem     vdriver\bin\usbmmidd.sys  (ve diger dosyalar)
rem
rem  Cikis kodu: 0 = basarili, digerleri = hata
rem ============================================================
setlocal
title Umbra - sanal ekran
set "BIN=%~dp0bin"

if not exist "%BIN%\deviceinstaller64.exe" goto :missing
if not exist "%BIN%\deviceinstaller32.exe" goto :missing

if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
  set "INST=%BIN%\deviceinstaller64.exe"
) else (
  set "INST=%BIN%\deviceinstaller32.exe"
)

echo [Umbra] Sanal ekran surucusu kuruluyor...
"%INST%" install
"%INST%" enableidd 1
if errorlevel 1 (
  echo [Umbra] enableidd basarisiz oldu.
  exit /b 1
)

echo [Umbra] Sanal ekran etkinlestirildi.
exit /b 0

:missing
echo [Umbra] Sanal ekran surucusu bulunamadi.
echo.
echo   vdriver\bin  klasorune usbmmidd_v2 dosyalarini kopyalayin.
echo   Indir: https://github.com/ge9/IddSampleDriver veya
echo          https://github.com/itsmikethetech/Virtual-Display-Driver
echo.
exit /b 1
