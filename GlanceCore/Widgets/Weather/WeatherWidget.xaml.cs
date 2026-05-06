using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GlanceCore.Widgets;
using GlanceCore.UI.Shaders;

namespace GlanceCore.Widgets.Weather;

public partial class WeatherWidget : BaseWidgetWindow, INotifyPropertyChanged
{
    // --- WIN32 API ---
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr d, int dx, int dy, int w, int h, IntPtr s, int sx, int sy, int r); [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern IntPtr GetWindowDC(IntPtr w);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr w, IntPtr dc); [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc); [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h); [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj); [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc); [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj); [DllImport("user32.dll")] static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    private readonly DispatcherTimer _captureTimer;
    private readonly DispatcherTimer _weatherTimer;
    private static readonly HttpClient _http = new();

    // --- BINDINGS ---
    private string _temperature = "--°"; public string Temperature { get => _temperature; set { _temperature = value; OnPropertyChanged(); } }
    private string _cityName = "Поиск..."; public string CityName { get => _cityName; set { _cityName = value; OnPropertyChanged(); } }
    private string _weatherDesc = "Загрузка"; public string WeatherDescription { get => _weatherDesc; set { _weatherDesc = value; OnPropertyChanged(); } }
    private MahApps.Metro.IconPacks.PackIconLucideKind _currentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Cloud;
    public MahApps.Metro.IconPacks.PackIconLucideKind CurrentIcon { get => _currentIcon; set { _currentIcon = value; OnPropertyChanged(); } }

    public WeatherWidget()
    {
        InitializeComponent();

        // 1. Capture Timer for Shader
        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();
        _captureTimer.Start();

        // 2. Setup Safe Startup & Settings
        this.Loaded += (s, e) =>
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

            var cfg = Core.WidgetHost.CurrentConfig;
            var state = cfg.Widgets.GetValueOrDefault("Weather_01", new Core.WidgetState());
            ApplySettings(state.Opacity, state.Scale, cfg.LockWidgets, state.IsAlwaysOnTop, state.FontFamily, state.FontSize, cfg.EnableShader);
        };

        // 3. Weather Refresh Timer (Every 30 mins)
        _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _weatherTimer.Tick += async (s, e) => await FetchWeatherAsync();
        _weatherTimer.Start();

        // Initial Fetch
        _ = FetchWeatherAsync();
    }

    public override void ApplyShaderSettings(bool enable)
    {
        if (GlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        if (enable)
        {
            // 1. Включаем невидимость
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

            // 2. Включаем шейдер
            if (GlassBase.Effect == null)
                GlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = -0.05 }; // Для Media поставь -0.15

            // 3. Запускаем захват экрана
            _captureTimer.Start();
        }
        else
        {
            // 1. Останавливаем захват экрана (чтобы не было туннеля)
            _captureTimer.Stop();

            // 2. Убираем шейдер
            GlassBase.Effect = null;

            // 3. Разрешаем Windows видеть окно для скриншота
            SetWindowDisplayAffinity(hwnd, WDA_NONE);

            // 4. Очищаем фон, чтобы он стал прозрачным/черным для чистого скрина
            WallpaperBrush.ImageSource = null;
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
            var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

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

            double lat = 54.71; // Дефолт (Калининград), если API упадет
            double lon = 20.51;
            string city = "Калининград";

            // --- ШАГ 1: ГЕОЛОКАЦИЯ ---
            try
            {
                // Используем HTTPS версию проверенного сервиса
                var ipJson = await _http.GetStringAsync("https://ipapi.co/json/");
                using var ipDoc = JsonDocument.Parse(ipJson);
                var root = ipDoc.RootElement;

                if (root.TryGetProperty("latitude", out var latProp))
                {
                    lat = latProp.GetDouble();
                    lon = root.GetProperty("longitude").GetDouble();
                    city = root.GetProperty("city").GetString() ?? "Unknown";
                }
            }
            catch { /* Если геолокация не ответила, используем дефолт */ }

            // Показываем город сразу
            Dispatcher.Invoke(() => CityName = city);

            // --- ШАГ 2: ПОГОДА (Open-Meteo) ---
            // Используем InvariantCulture, чтобы координаты ВСЕГДА были через точку (54.71)
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var meteoUrl = string.Format(culture,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current_weather=true",
                lat, lon);

            var weatherJson = await _http.GetStringAsync(meteoUrl);
            using var wDoc = JsonDocument.Parse(weatherJson);

            if (wDoc.RootElement.TryGetProperty("current_weather", out var current))
            {
                double temp = current.GetProperty("temperature").GetDouble();
                int code = current.GetProperty("weathercode").GetInt32();

                Dispatcher.Invoke(() =>
                {
                    Temperature = $"{(int)Math.Round(temp)}°";
                    ParseWmoCode(code);
                });
            }
            else
            {
                Dispatcher.Invoke(() => WeatherDescription = "Нет данных");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather Fatal: {ex.Message}");
            Dispatcher.Invoke(() => WeatherDescription = "Ошибка сети");
        }
    }

    private void ParseWmoCode(int code)
    {
        // English: Explicitly use System.Windows.Media.Color to avoid conflict with System.Drawing
        switch (code)
        {
            case 0:
                WeatherDescription = "Ясно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Sun;
                WeatherTint.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 255, 150, 0)); break;
            case 1:
            case 2:
            case 3:
                WeatherDescription = "Облачно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudSun;
                WeatherTint.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 100, 120, 150)); break;
            case 45:
            case 48:
                WeatherDescription = "Туман"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudFog;
                WeatherTint.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 150, 150, 150)); break;
            case 51:
            case 53:
            case 55:
            case 61:
            case 63:
            case 65:
                WeatherDescription = "Дождь"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudRain;
                WeatherTint.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 0, 50, 100)); break;
            case 71:
            case 73:
            case 75:
            case 77:
                WeatherDescription = "Снег"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Snowflake;
                WeatherTint.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 200, 220, 255)); break;
            case 95:
            case 96:
            case 99:
                WeatherDescription = "Гроза"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.CloudLightning;
                WeatherTint.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(160, 50, 0, 80)); break;
            default:
                WeatherDescription = "Облачно"; CurrentIcon = MahApps.Metro.IconPacks.PackIconLucideKind.Cloud; break;
        }
    }
    private void RefreshWeather_Click(object sender, RoutedEventArgs e) => _ = FetchWeatherAsync();

    // English: Proper cleanup and syncing with Hub
    private void CloseWidget_Click(object sender, RoutedEventArgs e) => Core.WidgetHost.CloseWidgetExplicitly("Weather_01");

    protected override void OnClosed(EventArgs e)
    {
        _captureTimer.Stop();
        _weatherTimer.Stop();
        base.OnClosed(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}