namespace GlanceCore.Widgets;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

public class BaseWidgetWindow : Window
{
    // English: Win32 API for screenshot invisibility
    [DllImport("user32.dll")] public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
    public const uint WDA_NONE = 0x00000000;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    protected bool _isLocked = false;
    protected bool _shaderShouldBeEnabled = true;

    public BaseWidgetWindow()
    {
        WindowStyle = WindowStyle.None; AllowsTransparency = true;
        Background = Brushes.Transparent; ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        this.Loaded += (s, e) => ApplyShaderSettings(_shaderShouldBeEnabled);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!_isLocked && e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    // English: Virtual methods to be implemented by child widgets
    public virtual void ApplyShaderSettings(bool enable) { }
    public virtual void RefreshCustomData() { }

    public void ApplySettings(double opacity, double scale, bool isLocked, bool isTopmost, string fontFamily, double fontSize, bool enableShader)
    {
        this.Opacity = opacity; this.Topmost = isTopmost; this._isLocked = isLocked;
        this.FontFamily = new FontFamily(fontFamily); this.FontSize = fontSize;
        _shaderShouldBeEnabled = enableShader;

        if (this.Content is FrameworkElement content)
        {
            content.LayoutTransform = new ScaleTransform(scale, scale);
        }
        ApplyShaderSettings(enableShader);
    }
}