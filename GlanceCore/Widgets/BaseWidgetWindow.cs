namespace GlanceCore.Widgets;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GlanceCore.Core;

public class BaseWidgetWindow : Window, System.ComponentModel.INotifyPropertyChanged
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
    public const uint WDA_NONE = 0x00000000;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    protected bool _isLocked = false;
    protected bool _shaderShouldBeEnabled = true;

    // ВОТ ЭТА СТРОКА ИСЧЕЗЛА (Верни её):
    public Core.WidgetStyleType CurrentStyle { get; protected set; } = Core.WidgetStyleType.LiquidGlass;
    // --- COLOR BINDINGS ---
    private double _bgOpacity = 1.0;
    public double BgOpacity
    {
        get => _bgOpacity;
        set
        {
            _bgOpacity = value;
            OnPropertyChanged();
            UpdateShaderIntensity(); // Вызываем при каждом сдвиге ползунка
        }
    }
    private CornerRadius _widgetCornerRadius = new CornerRadius(24);
    public CornerRadius WidgetCornerRadius { get => _widgetCornerRadius; set { _widgetCornerRadius = value; OnPropertyChanged(); } }
    protected virtual void UpdateShaderIntensity() { }
    private Brush _textColorBrush = Brushes.White;
    public Brush TextColorBrush { get => _textColorBrush; set { _textColorBrush = value; OnPropertyChanged(); } }

    private Brush _bgColorBrush = Brushes.Transparent;
    public Brush BgColorBrush { get => _bgColorBrush; set { _bgColorBrush = value; OnPropertyChanged(); } }

    private Brush _borderColorBrush = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));
    public Brush BorderColorBrush { get => _borderColorBrush; set { _borderColorBrush = value; OnPropertyChanged(); } }

    public BaseWidgetWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;

        // English: Apply settings only when window is fully loaded
        this.Loaded += (s, e) => ApplySkinSpecificVisuals();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { if (!_isLocked && e.ButtonState == MouseButtonState.Pressed) DragMove(); }
    public virtual void ApplyShaderSettings(bool enable) { }
    public virtual void RefreshCustomData() { }

    // English: New method to apply skin-specific visuals
    protected virtual void ApplySkinSpecificVisuals() { }

    // English: Refactored to use the State Object pattern. 
    // Now it takes a single WidgetState object instead of 10 different arguments. 
    public void ApplySettings(Core.WidgetState state, bool isGlobalLocked, bool isGlobalShaderEnabled)
    {
        this.BgOpacity = state.Opacity; // Временно оставляем, исправим в 3 пункте
        this.Topmost = state.IsAlwaysOnTop;
        this._isLocked = isGlobalLocked;
        this.WidgetCornerRadius = new CornerRadius(state.CornerRadius);
        this.FontFamily = new FontFamily(state.FontFamily);
        this.FontSize = state.FontSize;
        _shaderShouldBeEnabled = isGlobalShaderEnabled;

        try
        {
            var converter = new BrushConverter();
            TextColorBrush = (Brush)converter.ConvertFromString(state.TextColor)!;
            BgColorBrush = (Brush)converter.ConvertFromString(state.BgColor)!;
            BorderColorBrush = (Brush)converter.ConvertFromString(state.BorderColor)!;
        }
        catch { }

        // ИСПРАВЛЕНИЕ ПУНКТОВ 1 И 2:
        if (this.Content is FrameworkElement content)
        {
            // Применяем кастомные размеры ТОЛЬКО если они больше 0.
            // Иначе оставляем родные размеры из XAML (например, 220x260), чтобы верстка не плыла.
            if (state.CustomWidth > 0) content.Width = state.CustomWidth;
            if (state.CustomHeight > 0) content.Height = state.CustomHeight;

            // Масштабируем всё окно целиком (вместе с тенями и скруглениями)
            content.LayoutTransform = new ScaleTransform(state.Scale, state.Scale);
        }

        ApplyShaderSettings(isGlobalShaderEnabled);
    }

    // English: Handle active skin changes
    private void OnActiveSkinChanged(string newSkinId)
    {
        // Re-apply skin specific visuals when the active skin changes
        ApplySkinSpecificVisuals();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}