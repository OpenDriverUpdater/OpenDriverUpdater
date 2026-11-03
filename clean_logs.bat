@echo off
echo Gereksiz log dosyalari ve loglar temizleniyor...
del /q build_log_*.txt 2>nul
del /q app_log.txt 2>nul
del /q scripts\build_log_*.txt 2>nul
echo.
echo Temizlik tamamlandi!
pause
