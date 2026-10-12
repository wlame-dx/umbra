@echo off
rem ============================================================
rem  Umbra - sanal ekrani kaldirici
rem  Yonetim hakki gerekir (Umbra bu betigi "runas" ile cagirir).
rem  Cikis kodu: 0 = basarili, digerleri = hata
rem ============================================================
setlocal enabledelayedexpansion
title Umbra - sanal ekran kaldir
set "BIN=%~dp0bin"

rem --- A) deviceinstaller varsa ---
if exist "%BIN%\deviceinstaller64.exe" goto :deviceinstaller
if exist "%BIN%\deviceinstaller32.exe" goto :deviceinstaller

rem --- B) inf varsa pnputil ile kaldir ---
if exist "%BIN%\*.inf" goto :pnputil
goto :missing

:deviceinstaller
if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
  set "INST=%BIN%\deviceinstaller64.exe"
) else (
  set "INST=%BIN%\deviceinstaller32.exe"
)

echo [Umbra] Sanal ekran kaldiriliyor (deviceinstaller)...
"%INST%" enableidd 0
"%INST%" uninstall

echo [Umbra] Sanal ekran kaldirildi.
exit /b 0

:pnputil
echo [Umbra] Sanal ekran kaldiriliyor (pnputil)...
for %%F in ("%BIN%\*.inf") do (
  pnputil /delete-driver "%%~nxF" /uninstall /force >nul 2>&1
)
echo [Umbra] Sanal ekran kaldirildi.
exit /b 0

:missing
echo [Umbra] Sanal ekran surucusu bulunamadi (vdriver\bin bos).
exit /b 1
