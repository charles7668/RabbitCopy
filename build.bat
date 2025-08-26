@echo off
setlocal

dotnet publish "RabbitCopy.sln" -f net8.0-windows -c Release

endlocal