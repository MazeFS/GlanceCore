namespace GlanceCore.Widgets.Media;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;

public partial class MediaWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly DispatcherTimer _timelineTimer;

    private string _trackName = "No Media"; public string TrackName { get => _trackName; set { _trackName = value; OnPropertyChanged(); } }
    private string _artistName = "Waiting..."; public string ArtistName { get => _artistName; set { _artistName = value; OnPropertyChanged(); } }
    private BitmapImage? _albumArt; public BitmapImage? AlbumArt { get => _albumArt; set { _albumArt = value; OnPropertyChanged(); } }
    private double _trackProgress = 0; public double TrackProgress { get => _trackProgress; set { _trackProgress = value; OnPropertyChanged(); } }
    private double _trackDuration = 100; public double TrackDuration { get => _trackDuration; set { _trackDuration = value; OnPropertyChanged(); } }

    public MediaWidget()
    {
        InitializeComponent();

        _timelineTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timelineTimer.Tick += TimelineTimer_Tick;

        InitializeMediaControlsAsync();
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

    protected override void OnClosed(EventArgs e)
    {
        _timelineTimer.Stop();
        base.OnClosed(e);
    }
}