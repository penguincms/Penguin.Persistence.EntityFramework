@echo off
setlocal enabledelayedexpansion
set /a ArgCount = 0
for %%a in (%*) do (
  set /a ArgCount += 1
  set [File.!ArgCount!]=%%~a
)
for /l %%i in (1, 1, %ArgCount%) do (
   echo ![File.%%i]!
   dotnet nuget push "![File.%%i]!" -k oy2djdb2ot63bwnpqyga236xnexr2msehd7pyvxgi2idhm -s https://api.nuget.org/v3/index.json
)
pause