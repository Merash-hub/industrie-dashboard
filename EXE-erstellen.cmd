@echo off
REM ---------------------------------------------------------------------------
REM  Erstellt eine eigenstaendige IndustrieDashboard.exe.
REM  Ergebnis laeuft per Doppelklick auf jedem Windows-64-Bit-Rechner,
REM  auch ohne installiertes .NET.
REM
REM  Einmalige Voraussetzung: das .NET 10 SDK (der Compiler).
REM  Installation:  winget install Microsoft.DotNet.SDK.10
REM ---------------------------------------------------------------------------
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 goto KEINSDK

dotnet --list-sdks >nul 2>nul
if errorlevel 1 goto KEINSDK

echo.
echo ===========================================================
echo  Erstelle eigenstaendige Anwendung.
echo  Das dauert beim ersten Mal einige Minuten, bitte warten.
echo ===========================================================
echo.

dotnet publish src\IndustrieDashboard.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if errorlevel 1 goto FEHLER

set "AUSGABE=%~dp0src\IndustrieDashboard.App\bin\Release\net10.0-windows\win-x64\publish"

echo.
echo ===========================================================
echo  Fertig.
echo.
echo  Die Datei IndustrieDashboard.exe liegt hier:
echo  %AUSGABE%
echo.
echo  Diese eine Datei ist die komplette Anwendung. Sie laeuft
echo  per Doppelklick und laesst sich auf andere Rechner kopieren.
echo ===========================================================
echo.
explorer "%AUSGABE%"
pause
exit /b 0

:KEINSDK
echo.
echo ===========================================================
echo  FEHLER: Es wurde kein .NET SDK gefunden.
echo.
echo  Das SDK ist der Compiler und wird einmalig benoetigt,
echo  um die exe zu erzeugen. Installation in PowerShell:
echo.
echo      winget install Microsoft.DotNet.SDK.10
echo.
echo  Danach dieses Fenster schliessen, ein NEUES Fenster
echo  oeffnen und diese Datei erneut starten.
echo ===========================================================
echo.
pause
exit /b 1

:FEHLER
echo.
echo ===========================================================
echo  Der Build ist fehlgeschlagen.
echo  Bitte die rot markierten Meldungen oben kopieren.
echo ===========================================================
echo.
pause
exit /b 1
