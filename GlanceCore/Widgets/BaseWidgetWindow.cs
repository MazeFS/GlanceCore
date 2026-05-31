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
    [DllImport("user32.dll", SetLastError = true)] protected static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
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
    private bool _useStaticWallpaperFallback = false;
    private bool _lastStreamerMode = false;
    protected DispatcherTimer? _captureTimer;
    private DispatcherTimer? _fpsResetTimer;
    private double _shaderTime = 0;
    protected FrameworkElement? BaseMainRoot => FindName("MainRoot") as FrameworkElement;
    protected ImageBrush? BaseWallpaperBrush => BaseGlassBase?.Background as ImageBrush;
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
            if (_useStaticWallpaperFallback) UpdateWin10WallpaperOffset();
            int movingFps = Core.WidgetHost.CurrentConfig.MovingFps;
            if (movingFps <= 0) movingFps = 60;
            if (_captureTimer != null) _captureTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / movingFps);
            _fpsResetTimer?.Stop();
            _fpsResetTimer?.Start();
        };
        CompositionTarget.Rendering += OnCompositionTargetRendering;
        this.SizeChanged += (s, e) => UpdateClipping();
        this.Loaded += (s, e) => ApplySkinSpecificVisuals();
    }
    public void LoadStaticWallpaper()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string path = System.IO.Path.Combine(appData, @"Microsoft\Windows\Themes\TranscodedWallpaper");
            if (System.IO.File.Exists(path) && BaseWallpaperBrush != null)
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                BaseWallpaperBrush.ImageSource = bmp;
                BaseWallpaperBrush.ViewboxUnits = BrushMappingMode.Absolute;
                UpdateWin10WallpaperOffset();
            }
        }
        catch { }
    }

    public void UpdateWin10WallpaperOffset()
    {
        if (BaseWallpaperBrush == null || BaseWallpaperBrush.ImageSource == null || ActualWidth <= 0) return;
        var src = PresentationSource.FromVisual(this);
        double dx = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dy = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        var rect = BaseGlassBase.TransformToAncestor(this).TransformBounds(new Rect(0, 0, BaseGlassBase.ActualWidth, BaseGlassBase.ActualHeight));
        BaseWallpaperBrush.Viewbox = new Rect(Left * dx, Top * dy, rect.Width * dx, rect.Height * dy);
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

            IntPtr hDesk = GetDesktopWindow();
            IntPtr hSrc = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrc);
            IntPtr hBmp = CreateCompatibleBitmap(hSrc, w, h);
            IntPtr hOld = SelectObject(hDest, hBmp);

            BitBlt(hDest, 0, 0, w, h, hSrc, (int)p.X, (int)p.Y, 0x00CC0020);
            SelectObject(hDest, hOld);

            var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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

        if (BaseGlassBase?.Effect != null)
        {
            var type = BaseGlassBase.Effect.GetType();
            var prop = type.GetProperty("Time");
            if (prop != null)
            {
                prop.SetValue(BaseGlassBase.Effect, _shaderTime);
            }
        }
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
        if (BaseWallpaperBrush != null && BaseWallpaperBrush.ViewboxUnits == BrushMappingMode.RelativeToBoundingBox) BaseWallpaperBrush.ImageSource = null;

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
            bool success = true;

            if (!isStreamer)
            {
                success = SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                _useStaticWallpaperFallback = !success;
            }
            else
            {
                _useStaticWallpaperFallback = false;
            }

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

            if (_useStaticWallpaperFallback)
            {
                LoadStaticWallpaper();
            }
            else if (isStreamer)
            {
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                Dispatcher.BeginInvoke(new Action(async () => {
                    await System.Threading.Tasks.Task.Delay(100);
                    UpdateRealtimeBackground();
                    SetWindowDisplayAffinity(hwnd, WDA_NONE);
                }), DispatcherPriority.Render);
            }
            else
            {
                if (BaseWallpaperBrush != null)
                {
                    BaseWallpaperBrush.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    BaseWallpaperBrush.Viewbox = new Rect(0, 0, 1, 1);
                }
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
        this.FontFamily = new FontFamily(state.FontFamily);
        this.FontSize = state.FontSize;
        this.WidgetCornerRadius = new CornerRadius(state.CornerRadius);

        if (this.Content is FrameworkElement content)
        {
            if (state.CustomWidth > 0) content.Width = state.CustomWidth;
            if (state.CustomHeight > 0) content.Height = state.CustomHeight;
            content.LayoutTransform = new ScaleTransform(state.Scale, state.Scale);
        }

        if (BaseBackgroundLayer != null)
        {
            BaseBackgroundLayer.BorderThickness = state.ShowBorder ? new Thickness(1.5) : new Thickness(0);
        }

        string currentSkinId = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault(GetWidgetId())?.SkinId ?? "LiquidGlass";
        bool isStreamerMode = Core.WidgetHost.CurrentConfig.StreamerMode;

        bool needReapply = _shaderShouldBeEnabled != isGlobalShaderEnabled || BaseGlassBase?.Effect == null || _lastStreamerMode != isStreamerMode;
        _lastStreamerMode = isStreamerMode;

        if (BaseGlassBase?.Effect != null)
        {
            string currentEffectName = BaseGlassBase.Effect.GetType().Name;
            if (currentSkinId == "Retro" && currentEffectName != "RetroPixelEffect") needReapply = true;
            else if (currentSkinId == "Neon" && currentEffectName != "NeonEffect") needReapply = true;
            else if (currentSkinId == "LiquidGlass" && currentEffectName != "LiquidGlassEffect") needReapply = true;
            else if (currentSkinId == "Minimalism") needReapply = true;
        }

        if (needReapply)
        {
            ApplyShaderSettings(isGlobalShaderEnabled);
        }
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