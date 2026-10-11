@echo off
rem ============================================================
rem  Umbra - sanal ekrani kaldirici
rem  Yonetim hakki gerekir (Umbra bu betigi "runas" ile cagirir).
rem  Cikis kodu: 0 = basarili, digerleri = hata
rem ============================================================
setlocal
title Umbra - sanal ekran kaldir
set "BIN=%~dp0bin"

if not exist "%BIN%\deviceinstaller64.exe" goto :missing
if not exist "%BIN%\deviceinstaller32.exe" goto :missing

if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
  set "INST=%BIN%\deviceinstaller64.exe"
) else (
  set "INST=%BIN%\deviceinstaller32.exe"
)

echo [Umbra] Sanal ekran kaldiriliyor...
"%INST%" enableidd 0
"%INST%" uninstall

echo [Umbra] Sanal ekran kaldirildi.
exit /b 0

:missing
echo [Umbra] Sanal ekran surucusu bulunamadi (vdriver\bin bos).
exit /b 1
