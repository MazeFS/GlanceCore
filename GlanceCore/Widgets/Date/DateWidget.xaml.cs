namespace GlanceCore.Widgets.Date;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

public partial class DateWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    private readonly DispatcherTimer _timer;
    private Brush _dayColorBrush = Brushes.White; public Brush DayColorBrush { get => _dayColorBrush; set { _dayColorBrush = value; OnPropertyChanged(); } }
    private Brush _dateColorBrush = Brushes.White; public Brush DateColorBrush { get => _dateColorBrush; set { _dateColorBrush = value; OnPropertyChanged(); } }
    private Brush _timeColorBrush = Brushes.White; public Brush TimeColorBrush { get => _timeColorBrush; set { _timeColorBrush = value; OnPropertyChanged(); } }
    private string _dayOfWeek = ""; public string DayOfWeek { get => _dayOfWeek; set { _dayOfWeek = value; OnPropertyChanged(); } }
    private string _dateString = ""; public string DateString { get => _dateString; set { _dateString = value; OnPropertyChanged(); } }
    private string _timeString = ""; public string TimeString { get => _timeString; set { _timeString = value; OnPropertyChanged(); } }

    private double _dayFontSize = 12.0; public double DayFontSize { get => _dayFontSize; set { _dayFontSize = value; OnPropertyChanged(); } }
    private double _dateFontSize = 12.0; public double DateFontSize { get => _dateFontSize; set { _dateFontSize = value; OnPropertyChanged(); } }
    private double _timeFontSize = 20.0; public double TimeFontSize { get => _timeFontSize; set { _timeFontSize = value; OnPropertyChanged(); } }

    public DateWidget()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) => UpdateDateTime();
        _timer.Start();

        UpdateDateTime();
    }

    private void UpdateDateTime()
    {
        var now = DateTime.Now;
        var culture = new CultureInfo("en-US");
        DayOfWeek = $"|{now.ToString("dddd", culture).ToUpper()}|";
        DateString = now.ToString("dd MMMM yyyy", culture).ToUpper();
        TimeString = now.ToString("HH:mm");
    }

    protected override void ApplySkinSpecificVisuals()
    {
        base.ApplySkinSpecificVisuals();
        var state = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault("Date_01");
        if (state != null) ApplyDateSettings(state);
    }

    private void ApplyDateSettings(Core.WidgetState state)
    {
        if (DayOfWeekText != null) DayOfWeekText.Visibility = state.ShowDayOfWeek ? Visibility.Visible : Visibility.Collapsed;
        if (DateText != null) DateText.Visibility = state.ShowDate ? Visibility.Visible : Visibility.Collapsed;
        if (TimeText != null) TimeText.Visibility = state.ShowTime ? Visibility.Visible : Visibility.Collapsed;

        DayFontSize = state.DateDayFontSize;
        DateFontSize = state.DateDateFontSize;
        TimeFontSize = state.DateTimeFontSize;

        try
        {
            var conv = new BrushConverter();
            DayColorBrush = (Brush)conv.ConvertFromString(state.DateDayColor)!;
            DateColorBrush = (Brush)conv.ConvertFromString(state.DateDateColor)!;
            TimeColorBrush = (Brush)conv.ConvertFromString(state.DateTimeColor)!;
        }
        catch { }

        if (BackgroundLayer != null)
        {
            BackgroundLayer.Visibility = state.ShowPlate ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }
}