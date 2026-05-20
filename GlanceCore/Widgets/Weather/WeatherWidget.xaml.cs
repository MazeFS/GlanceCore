namespace GlanceCore.Widgets.Weather;

using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

public partial class WeatherWidget : BaseWidgetWindow
{
    // --- WIN32 GDI API (Только для графики, остальное в BaseWidgetWindow) ---
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
    private readonly DispatcherTimer _weatherTimer;
    private static readonly HttpClient _http = new();
    private double _shaderTime = 0;

    // --- БИНДИНГИ ---
    private string _temperature = "--°"; public string Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(); } }
    private string _cityName = "Поиск..."; public string CityName { get => _cityName; set { _cityName = value; OnPropertyChanged(); } }
    private string _weatherDesc = "Загрузка"; public string WeatherDescription { get => _weatherDesc; set { _weatherDesc = value; OnPropertyChanged(); } }
    private MahApps.Metro.IconPacks.PackIconLucideKind _currentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Cloud;
    public MahApps.Metro.IconPacks.PackIconLucideKind CurrentIcon { get => _currentIcon; set { _currentIcon = value; OnPropertyChanged(); } }
    public override void ApplyShaderSettings(bool enable)
    {
        _shaderShouldBeEnabled = enable;
        ApplySkinSpecificVisuals(); // Принудительно перерисовываем виджет
    }
    public WeatherWidget()
    {
        InitializeComponent();

        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();

        _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _weatherTimer.Tick += async (s, e) => await FetchWeatherAsync();
        _weatherTimer.Start();

        _ = FetchWeatherAsync();
    }

    // English: Handles dynamic styles and strictly prevents tunnel loops
    // English: Match the protected modifier from BaseWidgetWindow
    protected override void ApplySkinSpecificVisuals()
    {
        if (GlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        // Полный сброс перед применением нового стиля
        _captureTimer.Stop();
        GlassBase.Effect = null;
        SetWindowDisplayAffinity(hwnd, WDA_NONE);
        WallpaperBrush.ImageSource = null;

        if (_shaderShouldBeEnabled)
        {
            if (CurrentStyle == Core.WidgetStyleType.Minimalism)
            {
                MainBorder.CornerRadius = new CornerRadius(12);
                GlassBase.Visibility = Visibility.Collapsed;
                MinimalBase.Visibility = Visibility.Visible;
                WeatherTint.Visibility = Visibility.Collapsed;
                GlossBevel.Visibility = Visibility.Collapsed;
            }
            else if (CurrentStyle == Core.WidgetStyleType.Retro)
            {
                MainBorder.CornerRadius = new CornerRadius(0);
                GlassBase.Visibility = Visibility.Visible;
                MinimalBase.Visibility = Visibility.Collapsed;
                WeatherTint.Visibility = Visibility.Visible;
                GlossBevel.Visibility = Visibility.Collapsed;

                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                var retroEffect = new UI.Shaders.RetroPixelEffect { PixelSize = 0.015 };
                GlassBase.Effect = retroEffect;
                _captureTimer.Start();

                CompositionTarget.Rendering += (s, e) => { _shaderTime += 0.05; retroEffect.Time = _shaderTime; };
            }
            else
            {
                // Liquid Glass
                MainBorder.CornerRadius = new CornerRadius(28);
                GlassBase.Visibility = Visibility.Visible;
                MinimalBase.Visibility = Visibility.Collapsed;
                WeatherTint.Visibility = Visibility.Visible;
                GlossBevel.Visibility = Visibility.Visible;

                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                GlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = -0.12 };
                _captureTimer.Start();
            }
        }
        else
        {
            // Fallback, если шейдеры глобально отключены
            GlassBase.Visibility = Visibility.Collapsed;
            MinimalBase.Visibility = Visibility.Visible;
            GlossBevel.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateRealtimeBackground()
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

            var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            DeleteDC(hDest); ReleaseDC(hDesk, hSrc); DeleteObject(hBmp);

            bmp.Freeze();
            WallpaperBrush.ImageSource = bmp;
        }
        catch { }
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
        switch (code)
        {
            case 0:
                WeatherDescription = "Ясно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Sun;
                WeatherTint.Background = new SolidColorBrush(Color.FromArgb(120, 255, 150, 0)); break;
            case 1:
            case 2:
            case 3:
                WeatherDescription = "Облачно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudSun;
                WeatherTint.Background = new SolidColorBrush(Color.FromArgb(120, 100, 120, 150)); break;
            case 45:
            case 48:
                WeatherDescription = "Туман"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudFog;
                WeatherTint.Background = new SolidColorBrush(Color.FromArgb(150, 150, 150, 150)); break;
            case 51:
            case 53:
            case 55:
            case 61:
            case 63:
            case 65:
                WeatherDescription = "Дождь"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudRain;
                WeatherTint.Background = new SolidColorBrush(Color.FromArgb(150, 0, 50, 100)); break;
            case 71:
            case 73:
            case 75:
            case 77:
                WeatherDescription = "Снег"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Snowflake;
                WeatherTint.Background = new SolidColorBrush(Color.FromArgb(120, 200, 220, 255)); break;
            case 95:
            case 96:
            case 99:
                WeatherDescription = "Гроза"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudLightning;
                WeatherTint.Background = new SolidColorBrush(Color.FromArgb(160, 50, 0, 80)); break;
            default:
                WeatherDescription = "Облачно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Cloud; break;
        }
    }

    private void RefreshWeather_Click(object sender, RoutedEventArgs e) => _ = FetchWeatherAsync();
    private void CloseWidget_Click(object sender, RoutedEventArgs e) => Core.WidgetHost.CloseWidgetExplicitly("Weather_01");

    protected override void OnClosed(EventArgs e)
    {
        _captureTimer.Stop();
        _weatherTimer.Stop();
        base.OnClosed(e);
    }
}