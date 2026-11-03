@echo off
setlocal enabledelayedexpansion

echo ==============================================
echo  OpenDriverUpdater GitHub Upload Tool
echo ==============================================
echo.

:: Get current branch
set BRANCH=
for /f "tokens=*" %%i in ('git branch --show-current 2^>nul') do set BRANCH=%%i
if "%BRANCH%"=="" set BRANCH=main

echo Active Branch: %BRANCH%
echo.

:: Check for uncommitted changes
set HAS_CHANGES=0
git status --porcelain > "%TEMP%\odu_git_status.tmp" 2>nul
for %%A in ("%TEMP%\odu_git_status.tmp") do (
    if %%~zA gtr 0 set HAS_CHANGES=1
)
del /q "%TEMP%\odu_git_status.tmp" 2>nul

if "%HAS_CHANGES%"=="1" (
    echo [1/3] Changes detected.
    set COMMIT_MSG=%*
    if "!COMMIT_MSG!"=="" (
        set /p COMMIT_MSG="Enter commit message (Press Enter for auto): "
    )
    if "!COMMIT_MSG!"=="" (
        set COMMIT_MSG=chore: update (%date% %time%)
    )
    echo Staging and committing files...
    git add .
    git commit -m "!COMMIT_MSG!"
) else (
    echo [1/3] Working tree clean (no new uncommitted changes).
)

echo.
echo [2/3] Pulling latest changes from GitHub (pull --rebase)...
git pull --rebase origin %BRANCH%
if %ERRORLEVEL% neq 0 (
    echo.
    echo ==============================================
    echo [ERROR] Pull/Rebase failed or conflicts detected!
    echo ==============================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Pushing changes to GitHub...
git push -u origin %BRANCH%
if %ERRORLEVEL% neq 0 (
    echo.
    echo ==============================================
    echo [ERROR] Git push failed!
    echo ==============================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ==============================================
echo [SUCCESS] Successfully uploaded to GitHub!
echo ==============================================
pause