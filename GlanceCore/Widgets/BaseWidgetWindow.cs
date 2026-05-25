namespace GlanceCore.Widgets;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

public class BaseWidgetWindow : Window, INotifyPropertyChanged
{
    // --- GDI+ И DWM (ЕДИНАЯ БАЗА ДЛЯ ВСЕХ ПЛАГИНОВ) ---
    [DllImport("user32.dll")] protected static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
    protected const uint WDA_NONE = 0x00000000;
    protected const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr d, int dx, int dy, int w, int h, IntPtr s, int sx, int sy, int r);
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr w);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr w, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);

    protected bool _isLocked = false;
    protected bool _shaderShouldBeEnabled = true;

    // Таймеры и динамический FPS
    protected DispatcherTimer? _captureTimer;
    private DispatcherTimer? _fpsResetTimer;

    // Автоматический поиск XAML-элементов
    protected FrameworkElement? MainRoot => FindName("MainRoot") as FrameworkElement;
    protected ImageBrush? WallpaperBrush => FindName("WallpaperBrush") as ImageBrush;
    protected Border? GlassBase => FindName("GlassBase") as Border;
    protected Border? MinimalBase => FindName("MinimalBase") as Border;
    protected Border? GlossBevel => FindName("GlossBevel") as Border;
    protected Border? BackgroundLayer => FindName("BackgroundLayer") as Border;

    // --- COLOR BINDINGS ---
    private CornerRadius _widgetCornerRadius = new CornerRadius(24);
    public CornerRadius WidgetCornerRadius { get => _widgetCornerRadius; set { _widgetCornerRadius = value; OnPropertyChanged(); } }
    private double _bgOpacity = 1.0;
    public double BgOpacity { get => _bgOpacity; set { _bgOpacity = value; OnPropertyChanged(); UpdateShaderIntensity(); } }
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

        // ПУНКТ 3: УМНЫЙ ТРОТТЛИНГ (15 FPS в простое, 60 FPS при движении)
        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(66) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();

        _fpsResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _fpsResetTimer.Tick += (s, e) => {
            if (_captureTimer != null) _captureTimer.Interval = TimeSpan.FromMilliseconds(66);
            _fpsResetTimer.Stop();
        };

        this.LocationChanged += (s, e) => {
            if (_captureTimer != null) _captureTimer.Interval = TimeSpan.FromMilliseconds(16);
            _fpsResetTimer?.Stop();
            _fpsResetTimer?.Start();
        };

        this.Loaded += (s, e) => ApplySkinSpecificVisuals();
    }

    protected void UpdateRealtimeBackground()
    {
        if (!this.IsVisible || MainRoot == null || MainRoot.ActualWidth <= 0) return;
        if (PresentationSource.FromVisual(MainRoot) == null || WallpaperBrush == null) return;
        try
        {
            var p = MainRoot.PointToScreen(new Point(0, 0));
            int w = (int)MainRoot.ActualWidth; int h = (int)MainRoot.ActualHeight;
            IntPtr hDesk = GetDesktopWindow(); IntPtr hSrc = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrc); IntPtr hBmp = CreateCompatibleBitmap(hSrc, w, h);
            IntPtr hOld = SelectObject(hDest, hBmp);

            BitBlt(hDest, 0, 0, w, h, hSrc, (int)p.X, (int)p.Y, 0x00CC0020);
            SelectObject(hDest, hOld);

            var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmp.Freeze();
            DeleteDC(hDest); ReleaseDC(hDesk, hSrc); DeleteObject(hBmp);
            WallpaperBrush.ImageSource = bmp;
        }
        catch { }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { if (!_isLocked && e.ButtonState == MouseButtonState.Pressed) DragMove(); }

    public virtual void ApplyShaderSettings(bool enable)
    {
        _shaderShouldBeEnabled = enable;
        ApplySkinSpecificVisuals();
    }

    protected double GetAdjustedAmount()
    {
        double width = MainRoot?.ActualWidth ?? 220.0;
        if (width <= 0) width = 220.0;

        double baseAmount = -30.0 / width;
        return Math.Clamp(baseAmount, -0.22, -0.06) * BgOpacity;
    }

    protected virtual void ApplySkinSpecificVisuals()
    {
        if (GlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        _captureTimer.Stop();
        GlassBase.Effect = null;
        SetWindowDisplayAffinity(hwnd, WDA_NONE);
        if (WallpaperBrush != null) WallpaperBrush.ImageSource = null;

        if (_shaderShouldBeEnabled)
        {
            if (BackgroundLayer != null) BackgroundLayer.CornerRadius = WidgetCornerRadius;
            GlassBase.Visibility = Visibility.Visible;
            if (MinimalBase != null) MinimalBase.Visibility = Visibility.Collapsed;
            if (GlossBevel != null) GlossBevel.Visibility = Visibility.Visible;

            bool isStreamerMode = Core.WidgetHost.CurrentConfig.StreamerMode;

            if (isStreamerMode)
            {
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                Dispatcher.BeginInvoke(new Action(() => {
                    UpdateRealtimeBackground();
                    SetWindowDisplayAffinity(hwnd, WDA_NONE);
                    GlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = GetAdjustedAmount() };
                }), DispatcherPriority.Render);
            }
            else
            {
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                GlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = GetAdjustedAmount() };
                _captureTimer.Start();
            }
        }
        else
        {
            GlassBase.Visibility = Visibility.Collapsed;
            if (MinimalBase != null) MinimalBase.Visibility = Visibility.Visible;
            if (GlossBevel != null) GlossBevel.Visibility = Visibility.Collapsed;
        }
    }

    protected virtual void UpdateShaderIntensity()
    {
        if (GlassBase?.Effect is UI.Shaders.LiquidGlassEffect glass)
        {
            glass.Amount = GetAdjustedAmount();
        }
    }

    public virtual void ApplySettings(Core.WidgetState state, bool isGlobalLocked, bool isGlobalShaderEnabled)
    {
        this.BgOpacity = state.Opacity;
        this.Topmost = state.IsAlwaysOnTop;
        this._isLocked = isGlobalLocked;
        this.FontFamily = new FontFamily(state.FontFamily);
        this.FontSize = state.FontSize;
        this.WidgetCornerRadius = new CornerRadius(state.CornerRadius);
        _shaderShouldBeEnabled = isGlobalShaderEnabled;

        try
        {
            var converter = new BrushConverter();
            TextColorBrush = (Brush)converter.ConvertFromString(state.TextColor)!;
            BgColorBrush = (Brush)converter.ConvertFromString(state.BgColor)!;
            BorderColorBrush = (Brush)converter.ConvertFromString(state.BorderColor)!;
        }
        catch { }

        if (this.Content is FrameworkElement content)
        {
            if (state.CustomWidth > 0) content.Width = state.CustomWidth;
            if (state.CustomHeight > 0) content.Height = state.CustomHeight;
            content.LayoutTransform = new ScaleTransform(state.Scale, state.Scale);
        }
        ApplyShaderSettings(isGlobalShaderEnabled);
    }


    public virtual void RefreshCustomData() { }

    protected override void OnClosed(EventArgs e)
    {
        _captureTimer?.Stop();
        _fpsResetTimer?.Stop();
        base.OnClosed(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}