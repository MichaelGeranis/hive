@echo off
setlocal enabledelayedexpansion

REM Build script for Hive backend
REM Creates self-contained executable for Windows

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set API_PROJECT=%PROJECT_ROOT%\src\Hive.Api\Hive.Api.csproj
set OUTPUT_DIR=%PROJECT_ROOT%\src\Hive.Desktop\backend

echo Building Hive backend...
echo Project: %API_PROJECT%
echo Output: %OUTPUT_DIR%

REM Clean previous builds
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

set RID=win-x64
if not "%1"=="" set RID=%1

echo.
echo === Building for %RID% ===

set TARGET_DIR=%OUTPUT_DIR%\%RID%

dotnet publish "%API_PROJECT%" ^
    --configuration Release ^
    --runtime %RID% ^
    --self-contained true ^
    --output "%TARGET_DIR%" ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true

if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

echo Built: %TARGET_DIR%
dir "%TARGET_DIR%\Hive.Api.exe"

echo.
echo Build complete!
