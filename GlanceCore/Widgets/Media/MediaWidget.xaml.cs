using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;
using GlanceCore.Widgets;
using GlanceCore.UI.Shaders;
using System.Runtime.InteropServices;
using System.Drawing;

namespace GlanceCore.Widgets.Media;

public partial class MediaWidget : BaseWidgetWindow, INotifyPropertyChanged
{
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, int rop);
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hDC);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hDC);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr hObject);
    [DllImport("user32.dll")] static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly DispatcherTimer _timelineTimer;
    private readonly DispatcherTimer _captureTimer;

    private string _trackName = "No Media"; public string TrackName { get => _trackName; set { _trackName = value; OnPropertyChanged(); } }
    private string _artistName = "Waiting..."; public string ArtistName { get => _artistName; set { _artistName = value; OnPropertyChanged(); } }
    private BitmapImage? _albumArt; public BitmapImage? AlbumArt { get => _albumArt; set { _albumArt = value; OnPropertyChanged(); } }
    private double _trackProgress = 0; public double TrackProgress { get => _trackProgress; set { _trackProgress = value; OnPropertyChanged(); } }
    private double _trackDuration = 100; public double TrackDuration { get => _trackDuration; set { _trackDuration = value; OnPropertyChanged(); } }

    public MediaWidget()
    {
        InitializeComponent();

        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += (s, e) => UpdateBackground();
        _captureTimer.Start();

        _timelineTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timelineTimer.Tick += TimelineTimer_Tick;

        this.Loaded += (s, e) =>
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

            var cfg = Core.WidgetHost.CurrentConfig;
            var state = cfg.Widgets.GetValueOrDefault("Media_01", new Core.WidgetState());
            ApplySettings(state, cfg.LockWidgets, cfg.EnableShader);
        };

        InitializeMediaControlsAsync();
    }

    // English: Simplified ApplyShaderSettings for V1.0 Alpha stability
    public override void ApplyShaderSettings(bool enable)
    {
        if (GlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        _captureTimer.Stop();
        GlassBase.Effect = null;
        SetWindowDisplayAffinity(hwnd, WDA_NONE);

        if (enable)
        {
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
            _captureTimer.Start();
            GlassBase.Effect = new LiquidGlassEffect { Amount = -0.15 };
        }
    }

    private void UpdateBackground()
    {
        if (!this.IsVisible || this.ActualWidth <= 0 || this.ActualHeight <= 0) return;
        try
        {
            System.Windows.Point p = this.PointToScreen(new System.Windows.Point(0, 0));
            int w = (int)this.ActualWidth; int h = (int)this.ActualHeight;

            IntPtr hDesk = GetDesktopWindow(); IntPtr hSrc = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrc); IntPtr hBmp = CreateCompatibleBitmap(hSrc, w, h);
            IntPtr hOld = SelectObject(hDest, hBmp);

            BitBlt(hDest, 0, 0, w, h, hSrc, (int)p.X, (int)p.Y, 0x00CC0020);

            SelectObject(hDest, hOld);
            var bmpSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            DeleteDC(hDest); ReleaseDC(hDesk, hSrc); DeleteObject(hBmp);
            bmpSource.Freeze();
            WallpaperBrush.ImageSource = bmpSource;
        }
        catch { }
    }

    private async void InitializeMediaControlsAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += (s, a) => UpdateCurrentSession();
            UpdateCurrentSession();
        }
        catch { }
    }

    private void UpdateCurrentSession()
    {
        if (_sessionManager == null) return;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= Session_TimelinePropertiesChanged;
        }

        _currentSession = _sessionManager.GetCurrentSession();

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += Session_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += Session_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += Session_TimelinePropertiesChanged;

            _timelineTimer.Start();
            UpdateMediaInfo();
            UpdatePlaybackState();
            UpdateTimeline();
        }
        else
        {
            _timelineTimer.Stop();
            TrackName = "No Media";
            ArtistName = "Waiting...";
            AlbumArt = null;
        }
    }

    private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession s, MediaPropertiesChangedEventArgs a) => UpdateMediaInfo();
    private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession s, PlaybackInfoChangedEventArgs a) => UpdatePlaybackState();
    private void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession s, TimelinePropertiesChangedEventArgs a) => UpdateTimeline();

    private void TimelineTimer_Tick(object? sender, EventArgs e)
    {
        var session = _currentSession;
        if (session == null) return;

        try
        {
            var timeline = session.GetTimelineProperties();
            if (timeline != null)
            {
                Dispatcher.Invoke(() =>
                {
                    TrackDuration = timeline.EndTime.TotalSeconds > 0 ? timeline.EndTime.TotalSeconds : 100;
                    TrackProgress = timeline.Position.TotalSeconds;
                });
            }
        }
        catch { }
    }

    private void UpdateTimeline() => TimelineTimer_Tick(null, EventArgs.Empty);

    private async void UpdateMediaInfo()
    {
        var session = _currentSession;
        if (session == null) return;

        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            if (props == null) return;

            Dispatcher.Invoke(() =>
            {
                TrackName = string.IsNullOrEmpty(props.Title) ? "Unknown" : props.Title;
                ArtistName = string.IsNullOrEmpty(props.Artist) ? "Unknown" : props.Artist;
            });

            if (props.Thumbnail != null)
            {
                var streamRef = await props.Thumbnail.OpenReadAsync();
                var netStream = streamRef.AsStreamForRead();

                Dispatcher.Invoke(() =>
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = netStream;
                    bmp.EndInit();
                    bmp.Freeze();
                    AlbumArt = bmp;
                });
            }
        }
        catch { }
    }

    private void UpdatePlaybackState()
    {
        var session = _currentSession;
        if (session == null) return;

        try
        {
            var playbackInfo = session.GetPlaybackInfo();
            if (playbackInfo == null) return;

            Dispatcher.Invoke(() =>
            {
                IconPlayPause.Kind = (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    ? MahApps.Metro.IconPacks.PackIconLucideKind.Pause
                    : MahApps.Metro.IconPacks.PackIconLucideKind.Play;
            });
        }
        catch { }
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e) { var s = _currentSession; if (s != null) await s.TrySkipPreviousAsync(); }
    private async void BtnNext_Click(object sender, RoutedEventArgs e) { var s = _currentSession; if (s != null) await s.TrySkipNextAsync(); }
    private async void BtnPlayPause_Click(object sender, RoutedEventArgs e) { var s = _currentSession; if (s != null) await s.TryTogglePlayPauseAsync(); }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) => Core.WidgetHost.CloseWidgetExplicitly("Media_01");

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}