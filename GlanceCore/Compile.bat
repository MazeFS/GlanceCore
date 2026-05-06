@echo off
set "FXC_PATH=C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x86\fxc.exe"

echo [1/2] Cleaning old shader...
if exist "UI\Shaders\LiquidGlass.ps" del "UI\Shaders\LiquidGlass.ps"

echo [2/2] Compiling LiquidGlass.fx...
"%FXC_PATH%" /T ps_3_0 /E main /Fo"UI\Shaders\LiquidGlass.ps" "UI\Shaders\LiquidGlass.fx"

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] Shader compiled successfully!
) else (
    echo [ERROR] Compilation failed with code %ERRORLEVEL%
)
pause