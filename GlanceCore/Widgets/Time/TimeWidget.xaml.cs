namespace GlanceCore.Widgets.Time;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

public partial class TimeWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr d, int dx, int dy, int w, int h, IntPtr s, int sx, int sy, int r);
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern IntPtr GetWindowDC(IntPtr w);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr w, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);

    private readonly DispatcherTimer _captureTimer;
    private readonly DispatcherTimer _clockTimer;

    private string _hours = "00"; public string Hours { get => _hours; set { _hours = value; OnPropertyChanged(); } }
    private double _separatorOpacity = 0.6;
    public double SeparatorOpacity { get => _separatorOpacity; set { _separatorOpacity = value; OnPropertyChanged(); } }
    private string _minutes = "00"; public string Minutes { get => _minutes; set { _minutes = value; OnPropertyChanged(); } }
    private string _seconds = "00"; public string Seconds { get => _seconds; set { _seconds = value; OnPropertyChanged(); } }
    private string _separator = ":"; public string Separator { get => _separator; set { _separator = value; OnPropertyChanged(); } }

    public TimeWidget()
    {
        InitializeComponent();

        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _clockTimer.Tick += (s, e) => UpdateTime();
        _clockTimer.Start();

        UpdateTime();
    }

    private void UpdateTime()
    {
        var now = DateTime.Now;
        Hours = now.ToString("HH");
        Minutes = now.ToString("mm");
        Seconds = now.ToString("ss");

        // Эффект моргания: первые 500 миллисекунд каждой секунды разделитель горит (0.6), затем гаснет (0.0)
        SeparatorOpacity = (now.Millisecond < 500) ? 0.6 : 0.0;
    }

    public override void ApplyShaderSettings(bool enable)
    {
        _shaderShouldBeEnabled = enable;
        ApplySkinSpecificVisuals();
    }

    protected override void ApplySkinSpecificVisuals()
    {
        if (GlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        _captureTimer.Stop();
        GlassBase.Effect = null;
        SetWindowDisplayAffinity(hwnd, WDA_NONE);
        WallpaperBrush.ImageSource = null;

        if (_shaderShouldBeEnabled)
        {
            BackgroundLayer.CornerRadius = WidgetCornerRadius;
            GlassBase.Visibility = Visibility.Visible;
            MinimalBase.Visibility = Visibility.Collapsed;
            GlossBevel.Visibility = Visibility.Visible;

            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
            GlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = -0.15 * BgOpacity };
            _captureTimer.Start();
        }
        else
        {
            GlassBase.Visibility = Visibility.Collapsed;
            MinimalBase.Visibility = Visibility.Visible;
            GlossBevel.Visibility = Visibility.Collapsed;
        }

        var state = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault("Time_01");
        if (state != null) ApplyTimeSettings(state);
    }

    private void ApplyTimeSettings(Core.WidgetState state)
    {
        // Применяем разделитель
        Separator = state.TimeSeparator == "Пробел" ? " " : state.TimeSeparator;

        // Применяем секунды
        SecondsText.Visibility = state.ShowSeconds ? Visibility.Visible : Visibility.Collapsed;

        // Вертикальный или Горизонтальный режим
        if (state.IsVerticalTime)
        {
            TimePanel.Orientation = Orientation.Vertical;
            Separator1.Visibility = Visibility.Collapsed; // В вертикальном режиме разделители прячем
            Separator2.Visibility = Visibility.Collapsed;
        }
        else
        {
            TimePanel.Orientation = Orientation.Horizontal;
            Separator1.Visibility = Visibility.Visible;
            Separator2.Visibility = state.ShowSeconds ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    protected override void UpdateShaderIntensity()
    {
        if (GlassBase?.Effect is UI.Shaders.LiquidGlassEffect glass) glass.Amount = -0.15 * BgOpacity;
    }

    private void UpdateRealtimeBackground()
    {
        if (!this.IsVisible || MainRoot == null || MainRoot.ActualWidth <= 0 || MainRoot.ActualHeight <= 0) return;
        if (PresentationSource.FromVisual(MainRoot) == null) return;

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

    protected override void OnClosed(EventArgs e)
    {
        _captureTimer.Stop();
        _clockTimer.Stop();
        base.OnClosed(e);
    }
}