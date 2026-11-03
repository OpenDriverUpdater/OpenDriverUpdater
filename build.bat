@echo off
echo ==============================================
echo OpenDriverUpdater Derleme Araci (Rust + Qt6)
echo ==============================================

echo.
echo [1/4] Rust Core Engine derleniyor...
cd src\core_rust
cargo build --release
cd ..\..
if %ERRORLEVEL% neq 0 (
    echo [HATA] Rust Core Engine derlemesi basarisiz!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/4] Rust CLI derleniyor...
cd src\cli_rust
cargo build --release
cd ..\..
if %ERRORLEVEL% neq 0 (
    echo [HATA] Rust CLI derlemesi basarisiz!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/4] Qt Arayuzu (GUI) hazirlaniyor ve derleniyor...
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
if %ERRORLEVEL% neq 0 (
    echo [HATA] CMake yapilandirmasi basarisiz!
    pause
    exit /b %ERRORLEVEL%
)

cmake --build build --config Release
if %ERRORLEVEL% neq 0 (
    echo [HATA] CMake (Qt GUI) derlemesi basarisiz!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [4/4] Qt Bagimliliklari (DLL) kopyalaniyor...
C:\Qt\6.8.1\msvc2022_64\bin\windeployqt.exe --qmldir src\gui_qt\qml build\src\gui_qt\Release\OpenDriverUpdaterGui.exe
if %ERRORLEVEL% neq 0 (
    echo [HATA] windeployqt basarisiz!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ==============================================
echo TUM DERLEME ISLEMLERI BASARIYLA TAMAMLANDI!
echo ==============================================
echo - UI (Arayuz): build\src\gui_qt\Release\OpenDriverUpdaterGui.exe
echo - CLI (Komut Satiri): src\cli_rust\target\release\odu.exe
echo ==============================================
pause
