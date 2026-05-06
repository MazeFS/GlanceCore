namespace GlanceCore.Widgets;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

public class BaseWidgetWindow : Window
{
    private bool _isLocked = false;

    public BaseWidgetWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight; // Let content define size
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!_isLocked && e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    // English: Updated signature to handle typography
    public void ApplySettings(double opacity, double scale, bool isLocked, bool isTopmost, string fontFamily, double fontSize, bool disableShaderForScreenshots)
    {
        this.Opacity = opacity;
        this.Topmost = isTopmost;
        this._isLocked = isLocked;

        // English: Apply typography to the whole window (inherited by children)
        this.FontFamily = new FontFamily(fontFamily);
        this.FontSize = fontSize;

        if (this.Content is FrameworkElement content)
        {
            content.LayoutTransform = new ScaleTransform(scale, scale);
        }

        ApplyShaderSettings(disableShaderForScreenshots);
    }

    // English: Virtual method for derived classes to handle shader settings
    protected virtual void ApplyShaderSettings(bool disableShaderForScreenshots) { }
}