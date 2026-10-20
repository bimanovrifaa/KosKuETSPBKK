@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
  echo .NET SDK belum ditemukan.
  echo Instal .NET 8 SDK terlebih dahulu, lalu jalankan file ini kembali.
  echo https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
echo Menjalankan KosKu. Restore NuGet pertama kali memerlukan koneksi internet.
dotnet run --project "KosKu\KosKu.csproj"
if errorlevel 1 (
  echo.
  echo KosKu berhenti dengan error. Periksa pesan di atas.
  pause
)
endlocal
