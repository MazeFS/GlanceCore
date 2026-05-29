namespace GlanceCore.Widgets;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

public class BaseWidgetWindow : Window, INotifyPropertyChanged
{
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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    protected static extern int SystemParametersInfo(int uAction, int uParam, System.Text.StringBuilder lpvParam, int fuWinIni);
    protected string GetWidgetId()
    {
        string name = this.GetType().Name;
        if (name == "AIAssistantWidget") return "AI_01";
        if (name == "HardwareWidget") return "Hardware_01";
        if (name == "TimeWidget") return "Time_01";
        if (name == "WeatherWidget") return "Weather_01";
        if (name == "MediaWidget") return "Media_01";
        if (name == "ImageFrameWidget") return "Image_01";
        if (name == "StickyNotesWidget") return "Notes_01";
        if (name == "DateWidget") return "Date_01";
        return name.Replace("Widget", "_01");
    }
    protected bool _isLocked = false;
    protected bool _shaderShouldBeEnabled = true;

    protected DispatcherTimer? _captureTimer;
    private DispatcherTimer? _fpsResetTimer;
    private double _shaderTime = 0;
    protected FrameworkElement? BaseMainRoot => FindName("MainRoot") as FrameworkElement;
    protected ImageBrush? BaseWallpaperBrush => FindName("WallpaperBrush") as ImageBrush;
    protected Border? BaseGlassBase => FindName("GlassBase") as Border;
    protected Border? BaseMinimalBase => FindName("MinimalBase") as Border;
    protected Border? BaseGlossBevel => FindName("GlossBevel") as Border;
    protected Border? BaseBackgroundLayer => FindName("BackgroundLayer") as Border;

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
    private Effect? _textGlowEffect;
    public Effect? TextGlowEffect { get => _textGlowEffect; set { _textGlowEffect = value; OnPropertyChanged(); } }
    public BaseWidgetWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;

        int idleFps = Core.WidgetHost.CurrentConfig.IdleFps;
        if (idleFps <= 0) idleFps = 15;
        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000.0 / idleFps) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();

        _fpsResetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _fpsResetTimer.Tick += (s, e) => {
            int currentIdleFps = Core.WidgetHost.CurrentConfig.IdleFps;
            if (currentIdleFps <= 0) currentIdleFps = 15;
            if (_captureTimer != null) _captureTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / currentIdleFps);
            _fpsResetTimer.Stop();
        };

        this.LocationChanged += (s, e) => {
            int movingFps = Core.WidgetHost.CurrentConfig.MovingFps;
            if (movingFps <= 0) movingFps = 60;
            if (_captureTimer != null) _captureTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / movingFps);
            _fpsResetTimer?.Stop();
            _fpsResetTimer?.Start();
        };

        this.SizeChanged += (s, e) => UpdateClipping();
        this.Loaded += (s, e) => ApplySkinSpecificVisuals();
    }

    protected void UpdateRealtimeBackground()
    {
        if (!this.IsVisible || BaseGlassBase == null || BaseGlassBase.ActualWidth <= 0) return;
        if (PresentationSource.FromVisual(BaseGlassBase) == null || BaseWallpaperBrush == null) return;
        try
        {
            var p = BaseGlassBase.PointToScreen(new Point(0, 0));
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            var rect = BaseGlassBase.TransformToAncestor(this).TransformBounds(new Rect(0, 0, BaseGlassBase.ActualWidth, BaseGlassBase.ActualHeight));
            int w = (int)Math.Round(rect.Width * dpiX);
            int h = (int)Math.Round(rect.Height * dpiY);
            if (w <= 0 || h <= 0) return;
            IntPtr hDesk = GetDesktopWindow(); IntPtr hSrc = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrc); IntPtr hBmp = CreateCompatibleBitmap(hSrc, w, h);
            IntPtr hOld = SelectObject(hDest, hBmp);
            BitBlt(hDest, 0, 0, w, h, hSrc, (int)p.X, (int)p.Y, 0x00CC0020);
            SelectObject(hDest, hOld);
            var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmp.Freeze();
            DeleteDC(hDest); ReleaseDC(hDesk, hSrc); DeleteObject(hBmp);
            BaseWallpaperBrush.ImageSource = bmp;
        }
        catch { }
    }
    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        _shaderTime += 0.05;
        if (BaseGlassBase?.Effect is UI.Shaders.RetroPixelEffect retro) retro.Time = _shaderTime;
        if (BaseGlassBase?.Effect is UI.Shaders.NeonEffect neon) neon.Time = _shaderTime;
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) { if (!_isLocked && e.ButtonState == MouseButtonState.Pressed) DragMove(); }

    public virtual void ApplyShaderSettings(bool enable)
    {
        _shaderShouldBeEnabled = enable;
        if (BaseGlassBase == null) return;
        ApplySkinSpecificVisuals();
    }

    protected double GetAdjustedAmount()
    {
        double width = BaseMainRoot?.ActualWidth ?? 220.0;
        if (width <= 0) width = 220.0;
        return Math.Clamp(-30.0 / width, -0.22, -0.06) * BgOpacity;
    }

    private void UpdateClipping()
    {
        if (BaseBackgroundLayer == null || BaseMainRoot == null || BaseMainRoot.ActualWidth <= 0) return;

        var clip = new RectangleGeometry();
        clip.RadiusX = WidgetCornerRadius.TopLeft;
        clip.RadiusY = WidgetCornerRadius.TopLeft;
        clip.Rect = new Rect(0, 0, BaseMainRoot.ActualWidth, BaseMainRoot.ActualHeight);

        BaseBackgroundLayer.Clip = clip;

        if (FindName("ImageBorder") is UIElement imgBorder)
        {
            imgBorder.Clip = clip;
        }
    }
    protected virtual void ApplySkinSpecificVisuals()
    {
        if (BaseGlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        _captureTimer.Stop();
        BaseGlassBase.Effect = null;
        SetWindowDisplayAffinity(hwnd, WDA_NONE);
        if (BaseWallpaperBrush != null) BaseWallpaperBrush.ImageSource = null;

        string skinId = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault(GetWidgetId())?.SkinId ?? "LiquidGlass";

        if (_shaderShouldBeEnabled)
        {
            if (BaseBackgroundLayer != null) BaseBackgroundLayer.CornerRadius = WidgetCornerRadius;

            if (skinId == "Minimalism")
            {
                TextGlowEffect = null;
                BaseGlassBase.Visibility = Visibility.Collapsed;
                if (BaseMinimalBase != null) BaseMinimalBase.Visibility = Visibility.Visible;
                if (BaseGlossBevel != null) BaseGlossBevel.Visibility = Visibility.Collapsed;
                UpdateClipping();
                return;
            }

            BaseGlassBase.Visibility = Visibility.Visible;
            if (BaseMinimalBase != null) BaseMinimalBase.Visibility = Visibility.Collapsed;
            if (BaseGlossBevel != null) BaseGlossBevel.Visibility = Visibility.Visible;

            bool isStreamer = Core.WidgetHost.CurrentConfig.StreamerMode;

            if (skinId == "Retro")
            {
                TextGlowEffect = null;
                BaseGlassBase.Effect = new UI.Shaders.RetroPixelEffect { PixelSize = 0.008 };
            }
            else if (skinId == "Neon")
            {
                Color glowColor = Colors.Purple;
                if (BgColorBrush is SolidColorBrush scb) glowColor = scb.Color;
                BaseGlassBase.Effect = new UI.Shaders.NeonEffect { NeonColor = glowColor };
                TextGlowEffect = new System.Windows.Media.Effects.DropShadowEffect { Color = glowColor, BlurRadius = 15, ShadowDepth = 0, Opacity = 0.9 };
            }
            else
            {
                TextGlowEffect = null;
                BaseGlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = GetAdjustedAmount() };
            }

            if (isStreamer)
            {
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                Dispatcher.BeginInvoke(new Action(() => {
                    UpdateRealtimeBackground();
                    SetWindowDisplayAffinity(hwnd, WDA_NONE);
                }), DispatcherPriority.Render);
            }
            else
            {
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                _captureTimer.Start();
            }

            UpdateClipping();
        }
        else
        {
            TextGlowEffect = null;
            BaseGlassBase.Visibility = Visibility.Collapsed;
            if (BaseMinimalBase != null) BaseMinimalBase.Visibility = Visibility.Visible;
            if (BaseGlossBevel != null) BaseGlossBevel.Visibility = Visibility.Collapsed;
        }
    }


    public virtual void ApplySettings(Core.WidgetState state, bool isGlobalLocked, bool isGlobalShaderEnabled)
    {
        this.BgOpacity = state.Opacity;
        this.Topmost = state.IsAlwaysOnTop;
        this._isLocked = isGlobalLocked;
        if (!string.IsNullOrEmpty(state.FontFamily))
        {
            if (state.FontFamily.Contains("#"))
            {
                this.FontFamily = new FontFamily(new Uri(AppDomain.CurrentDomain.BaseDirectory), state.FontFamily);
            }
            else
            {
                this.FontFamily = new FontFamily(state.FontFamily);
            }
        }
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
        if (BaseBackgroundLayer != null)
        {
            BaseBackgroundLayer.BorderThickness = state.ShowBorder ? new Thickness(1.5) : new Thickness(0);
        }
        if (this.Content is FrameworkElement content)
        {
            if (state.CustomWidth > 0) content.Width = state.CustomWidth;
            if (state.CustomHeight > 0) content.Height = state.CustomHeight;
            content.LayoutTransform = new ScaleTransform(state.Scale, state.Scale);
        }
        ApplyShaderSettings(isGlobalShaderEnabled);
    }

    protected virtual void UpdateShaderIntensity()
    {
        if (BaseGlassBase?.Effect is UI.Shaders.LiquidGlassEffect glass) glass.Amount = GetAdjustedAmount();
    }

    public virtual void RefreshCustomData() { }

    protected override void OnClosed(EventArgs e)
    {
        CompositionTarget.Rendering -= OnCompositionTargetRendering;
        _captureTimer?.Stop();
        _fpsResetTimer?.Stop();
        base.OnClosed(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}