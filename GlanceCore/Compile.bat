@echo off
set "FXC_PATH=C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x86\fxc.exe"

if exist "UI\Shaders\LiquidGlass.ps" del "UI\Shaders\LiquidGlass.ps"
if exist "UI\Shaders\RetroPixel.ps" del "UI\Shaders\RetroPixel.ps"
if exist "UI\Shaders\Neon.ps" del "UI\Shaders\Neon.ps"

"%FXC_PATH%" /T ps_3_0 /E main /Fo"UI\Shaders\LiquidGlass.ps" "UI\Shaders\LiquidGlass.fx"
"%FXC_PATH%" /T ps_3_0 /E main /Fo"UI\Shaders\RetroPixel.ps" "UI\Shaders\RetroPixel.fx"
"%FXC_PATH%" /T ps_3_0 /E main /Fo"UI\Shaders\Neon.ps" "UI\Shaders\Neon.fx"

pause