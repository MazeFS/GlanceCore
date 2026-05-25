namespace GlanceCore.Widgets.Weather;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

public partial class WeatherWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    private readonly DispatcherTimer _weatherTimer;
    private static readonly HttpClient _http = new();

    // --- БИНДИНГИ ---
    private string _temperature = "--°"; public string Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(); } }
    private string _cityName = "Поиск..."; public string CityName { get => _cityName; set { _cityName = value; OnPropertyChanged(); } }
    private string _weatherDesc = "Загрузка"; public string WeatherDescription { get => _weatherDesc; set { _weatherDesc = value; OnPropertyChanged(); } }
    private MahApps.Metro.IconPacks.PackIconLucideKind _currentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Cloud;
    public MahApps.Metro.IconPacks.PackIconLucideKind CurrentIcon { get => _currentIcon; set { _currentIcon = value; OnPropertyChanged(); } }

    public WeatherWidget()
    {
        InitializeComponent();

        _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _weatherTimer.Tick += async (s, e) => await FetchWeatherAsync();

        // Запускаем сбор погоды только после загрузки окна
        this.Loaded += (s, e) => {
            _weatherTimer.Start();
            _ = FetchWeatherAsync();
        };
    }

    private async Task FetchWeatherAsync()
    {
        try
        {
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("User-Agent", "GlanceCore/1.0");

            double lat = 54.71, lon = 20.51; // Fallback location
            string city = "Калининград";

            try
            {
                var ipJson = await _http.GetStringAsync("https://ipapi.co/json/");
                using var ipDoc = JsonDocument.Parse(ipJson);
                if (ipDoc.RootElement.TryGetProperty("latitude", out var latProp))
                {
                    lat = latProp.GetDouble();
                    lon = ipDoc.RootElement.GetProperty("longitude").GetDouble();
                    city = ipDoc.RootElement.GetProperty("city").GetString() ?? "Unknown";
                }
            }
            catch { }

            Dispatcher.Invoke(() => CityName = city);

            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var meteoUrl = string.Format(culture, "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m,weather_code", lat, lon);

            var weatherJson = await _http.GetStringAsync(meteoUrl);
            using var wDoc = JsonDocument.Parse(weatherJson);

            var current = wDoc.RootElement.GetProperty("current");
            double temp = current.GetProperty("temperature_2m").GetDouble();
            int code = current.GetProperty("weather_code").GetInt32();

            Dispatcher.Invoke(() =>
            {
                Temperature = $"{(int)Math.Round(temp)}°";
                ParseWmoCode(code);
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => WeatherDescription = "Ошибка API");
            System.Diagnostics.Debug.WriteLine($"Weather Error: {ex.Message}");
        }
    }

    private void ParseWmoCode(int code)
    {
        var tint = FindName("WeatherTint") as System.Windows.Controls.Border;

        switch (code)
        {
            case 0:
                WeatherDescription = "Ясно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Sun;
                if (tint != null) tint.Background = new SolidColorBrush(Color.FromArgb(120, 255, 150, 0)); break;
            case 1:
            case 2:
            case 3:
                WeatherDescription = "Облачно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudSun;
                if (tint != null) tint.Background = new SolidColorBrush(Color.FromArgb(120, 100, 120, 150)); break;
            case 45:
            case 48:
                WeatherDescription = "Туман"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudFog;
                if (tint != null) tint.Background = new SolidColorBrush(Color.FromArgb(150, 150, 150, 150)); break;
            case 51:
            case 53:
            case 55:
            case 61:
            case 63:
            case 65:
                WeatherDescription = "Дождь"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudRain;
                if (tint != null) tint.Background = new SolidColorBrush(Color.FromArgb(150, 0, 50, 100)); break;
            case 71:
            case 73:
            case 75:
            case 77:
                WeatherDescription = "Снег"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Snowflake;
                if (tint != null) tint.Background = new SolidColorBrush(Color.FromArgb(120, 200, 220, 255)); break;
            case 95:
            case 96:
            case 99:
                WeatherDescription = "Гроза"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudLightning;
                if (tint != null) tint.Background = new SolidColorBrush(Color.FromArgb(160, 50, 0, 80)); break;
            default:
                WeatherDescription = "Облачно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Cloud; break;
        }
    }

    private void RefreshWeather_Click(object sender, RoutedEventArgs e) => _ = FetchWeatherAsync();
    private void CloseWidget_Click(object sender, RoutedEventArgs e) => Core.WidgetHost.CloseWidgetExplicitly("Weather_01");

    protected override void OnClosed(EventArgs e)
    {
        _weatherTimer.Stop();
        base.OnClosed(e);
    }
}