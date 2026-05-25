namespace GlanceCore.Widgets.Time;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

public partial class TimeWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    private readonly DispatcherTimer _clockTimer;

    private string _hours = "00"; public string Hours { get => _hours; set { _hours = value; OnPropertyChanged(); } }
    private string _minutes = "00"; public string Minutes { get => _minutes; set { _minutes = value; OnPropertyChanged(); } }
    private string _seconds = "00"; public string Seconds { get => _seconds; set { _seconds = value; OnPropertyChanged(); } }
    private string _separator = ":"; public string Separator { get => _separator; set { _separator = value; OnPropertyChanged(); } }
    private double _separatorOpacity = 0.6; public double SeparatorOpacity { get => _separatorOpacity; set { _separatorOpacity = value; OnPropertyChanged(); } }

    public TimeWidget()
    {
        InitializeComponent();

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
        SeparatorOpacity = (now.Millisecond < 500) ? 0.6 : 0.0;
    }

    protected override void ApplySkinSpecificVisuals()
    {
        base.ApplySkinSpecificVisuals();
        var state = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault("Time_01");
        if (state != null) ApplyTimeSettings(state);
    }

    private void ApplyTimeSettings(Core.WidgetState state)
    {
        Separator = state.TimeSeparator == "Пробел" ? " " : state.TimeSeparator;

        if (FindName("SecondsText") is TextBlock secText)
            secText.Visibility = state.ShowSeconds ? Visibility.Visible : Visibility.Collapsed;

        if (FindName("TimePanel") is StackPanel panel && FindName("Separator1") is UIElement s1 && FindName("Separator2") is UIElement s2)
        {
            if (state.IsVerticalTime)
            {
                panel.Orientation = Orientation.Vertical;
                s1.Visibility = Visibility.Collapsed;
                s2.Visibility = Visibility.Collapsed;
            }
            else
            {
                panel.Orientation = Orientation.Horizontal;
                s1.Visibility = Visibility.Visible;
                s2.Visibility = state.ShowSeconds ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _clockTimer.Stop();
        base.OnClosed(e);
    }
}